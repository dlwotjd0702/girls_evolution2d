// ============================================================================
// EconomyManager.cs  (DROP-IN, HUD는 "값 바뀔 때만" 즉시 갱신)
// - DOTween/Update 의존 X
// - SetGold/SpendGold/AddGold/SetGoldPerSecEstimate 에 '중복 갱신 가드' 추가
// - Prestige 보정 Getter 유지
// - ResetGoldUpgradesForPrestige() 포함 (환생 시 사용)
// ============================================================================

using System;
using UnityEngine;
using TMPro;

public class EconomyManager : MonoBehaviour, ISaveable
{
    // ───────── Events ─────────
    public event Action<double> OnGoldChanged;
    public event Action         OnUpgradeChanged;

    // ───────── Gold ─────────
    [SerializeField] private double gold = 0;

    public double GetGold() => gold;

    public void SetGold(double amount)
    {
        amount = Math.Max(0, amount);
        if (Math.Abs(gold - amount) < 1e-9) return; // 바뀔 때만
        gold = amount;
        OnGoldChanged?.Invoke(gold);
        RefreshGoldHUD();
    }

    public bool SpendGold(double amount)
    {
        if (amount <= 0) return true;
        if (gold + 1e-9 < amount) return false;
        gold -= amount;
        OnGoldChanged?.Invoke(gold);
        RefreshGoldHUD();
        
        // 통계 기록: 골드 소비
        if (PlayStatsTracker.Instance != null)
        {
            PlayStatsTracker.Instance.RecordGoldSpent(amount);
        }
        
        return true;
    }

    public void AddGold(double amount)
    {
        if (amount <= 0) return;
        // 소수점 아래 자리는 올림 처리
        amount = Math.Ceiling(amount);
        gold += amount;
        OnGoldChanged?.Invoke(gold);
        RefreshGoldHUD();
        
        // 통계 기록: 골드 획득
        if (PlayStatsTracker.Instance != null)
        {
            PlayStatsTracker.Instance.RecordGoldEarned(amount);
        }
    }

    // ───────── UI (TMP) ─────────
    [Header("Gold UI (TMP)")]
    [SerializeField] private TextMeshProUGUI goldText;        // 현재 골드
    [SerializeField] private TextMeshProUGUI goldPerSecText;  // 초당 골드
    [SerializeField] private string goldUnitSuffix = "G";
    [SerializeField] private bool   showPlusOnPerSec = true;  // +표시 여부
    [SerializeField] private bool   hidePerSecWhenZero = true;

    private static readonly string[] GoldUnitPrefixes = { "", "a", "b", "c", "d", "e", "f", "g", "h" };

    // 내부: /s 추정치 (필드매니저가 필요 시 세팅)
    private double _goldPerSecEstimate;

    public double GetGoldPerSecEstimate() => _goldPerSecEstimate;

    public void SetGoldPerSecEstimate(double v)
    {
        v = Math.Max(0, v);
        if (Math.Abs(_goldPerSecEstimate - v) < 1e-9) return; // 바뀔 때만
        _goldPerSecEstimate = v;
        RefreshGoldHUD();
    }

    void Start() { RefreshGoldHUD(); }

    // 숫자 축약 포맷
    string FormatCompact(double value, int digits = 1)
    {
        return FormatAbbrev(value, digits, goldUnitSuffix);
    }

    // 정적 메서드: 숫자 축약 포맷 (외부에서 사용)
    public static string FormatAbbrev(double value, int digits = 1, string baseUnit = "G")
    {
        baseUnit ??= string.Empty;
        double scaledValue = value;
        double abs = Math.Abs(value);
        int unitIndex = 0;

        while (abs >= 1000d && unitIndex < GoldUnitPrefixes.Length - 1)
        {
            abs /= 1000d;
            scaledValue /= 1000d;
            unitIndex++;
        }

        int decimals = Mathf.Clamp(digits, 0, 3);
        if (abs >= 100d) decimals = 0;
        else if (abs >= 10d) decimals = Math.Min(decimals, 1);
        else decimals = Math.Min(decimals, 2);

        string fmt = decimals <= 0 ? "0" : $"0.{new string('0', decimals)}";
        string sign = scaledValue < 0 ? "-" : "";
        double magnitude = Math.Abs(scaledValue);
        string number = magnitude.ToString(fmt);
        string unit = ComposeGoldUnit(unitIndex, baseUnit);

        return string.IsNullOrEmpty(unit)
            ? $"{sign}{number}"
            : $"{sign}{number} {unit}";
    }

    static string ComposeGoldUnit(int index, string baseUnit)
    {
        index = Mathf.Clamp(index, 0, GoldUnitPrefixes.Length - 1);
        string prefix = GoldUnitPrefixes[index];
        if (string.IsNullOrEmpty(baseUnit)) return prefix;
        return $"{prefix}{baseUnit}";
    }

    void RefreshGoldHUD()
    {
        if (goldText)
            goldText.text = FormatCompact(gold);

        if (goldPerSecText)
        {
            bool show = !hidePerSecWhenZero || _goldPerSecEstimate > 0.0001;
            goldPerSecText.gameObject.SetActive(show);
            if (show)
            {
                string plus = showPlusOnPerSec ? "+" : "";
                goldPerSecText.text = $"{plus}{FormatCompact(_goldPerSecEstimate, 1)}/s";
            }
        }
    }

    // ───────── Base (spawn/field) ─────────
    [Header("Spawn/Field Base")]
    private int   baseManualSpawnMax = 3;
    private float baseManualSpawnInterval = 10f;
    private int   baseFieldCount = 8;

    // ───────── Balance consts ─────────
    private const double INCOME_BASE_PER_SEC   = 1.0;  // 2^(Lv-1)/s

    // 소환 가격: 25단계 도달 시간을 약 3시간으로 조정 (기존 1시간 → 3시간)
    // 60초 → 200초로 변경하여 소환 가격을 약 3.33배로 증가
    // 200초로 설정하면 반올림 후 가격이 깔끔하게 나옴 (200G, 400G, 800G, 1600G...)
    private const double SUMMON_BASE_SECONDS   = 200.0;  // 60.0 → 200.0 (약 3.33배 증가)
    private const double SUMMON_LEVEL_GROWTH    = 2.2;    // 레벨별 기본 소환비용이 2.2배씩 증가
    private const double SUMMON_BUY_GROWTH      = 1.1;    // 같은 레벨을 반복 구매할 때마다 1.1배씩 증가

    // 골드 강화 재화 밸런싱: 기본값과 배율 조정
    // 18~20단계 수익 기준으로 10분 수익(약 1.5억)으로 max 레벨을 찍을 수 있도록 배율 조정
    // BASE는 유지하고 GROW만 조정하여 총 비용이 목표에 맞도록 함
    private const double UPG_SPAWN_MAX_BASE    = 400;  private const double UPG_SPAWN_MAX_GROW    = 2.20;
    private const double UPG_SPAWN_SPEED_BASE  = 420;  private const double UPG_SPAWN_SPEED_GROW  = 2.20;
    private const double UPG_FIELD_MAX_BASE    = 500;  private const double UPG_FIELD_MAX_GROW    = 2.20;
    private const double UPG_CLICK_BONUS_BASE  = 360;  private const double UPG_CLICK_BONUS_GROW  = 2.20;

    private const double UPG_OFFLINE_REWARD_BASE  = 420; private const double UPG_OFFLINE_REWARD_GROW  = 2.20;
    private const double UPG_OFFLINE_MAXTIME_BASE = 420; private const double UPG_OFFLINE_MAXTIME_GROW = 2.20;

    // 자동(Auto)
    private const double AUTO_MERGE_BASE  = 900;  private const double AUTO_MERGE_GROW  = 2.00;
    private const double AUTO_SPAWN_BASE  = 980;  private const double AUTO_SPAWN_GROW  = 2.00;

    // 클릭 보너스
    private const double CLICK_BONUS_PER_LEVEL = 0.01;

    // 자동 간격
    [Header("Automation (intervals)")]
    private float autoMergeBaseInterval = 50f;  // Lv1 기준 (요청: 기본 50초)
    private float autoSpawnBaseInterval = 60f;  // Lv1 기준 (요청: 기본 60초)
    private float autoPerLevelMul       = 0.9f; // 레벨당 -10%
    private float autoIntervalFloor     = 0.4f; // 하한

    // Caps
    [Header("Level Caps)")]
    private int spawnMaxUpgradeCap     = 10;
    private int spawnSpeedUpgradeCap   = 10;
    private int fieldMaxUpgradeCap     = 10;
    private int clickBonusUpgradeCap   = 15;
    private int offlineRewardCap       = 10;
    private int offlineMaxTimeCap      = 10;
    private int autoMergeCap           = 10; // 0=미구매, 1..Cap
    private int autoSpawnCap           = 10;

    // 업그레이드 레벨(세이브)
    int manualSpawnMaxUpgrade;
    int manualSpawnSpeedUpgrade;
    int maxFieldCountUpgrade;
    int clickBonusUpgrade;

    int  autoMergeUpgrade; // 0=미구매
    int  autoSpawnUpgrade; // 0=미구매
    bool autoMergeOn;
    bool autoSpawnOn;

    int offlineRewardUpgrade;
    int offlineMaxTimeUpgrade;

    // 레벨별 누적 소환 구매수
    readonly int[] summonPurchaseCounts = new int[25];
    readonly int[] gemSummonPurchaseCounts = new int[25]; // 보석 소환 횟수 추적

    // ───────── 수익/소환가 규칙 ─────────
    public double GetLevelIncomePerSec(int level)
    {
        level = Math.Max(1, level);
        return INCOME_BASE_PER_SEC * Math.Pow(2.0, level - 1);
    }

    // 가격은 항상 100원 단위로 반올림해 깔끔하게 유지
    private static double RoundToHundred(double value)
    {
        if (double.IsInfinity(value) || double.IsNaN(value)) return value;
        return Math.Round(value / 100.0) * 100.0;
    }

    public double GetSummonCostByMinuteRule(int level)
    {
        level = Mathf.Clamp(level, 1, 25);
        int idx = level - 1;
        // 레벨별 기본 소환비용: 레벨 1 기준으로 2.2배씩 증가
        double baseCost = SUMMON_BASE_SECONDS * Math.Pow(SUMMON_LEVEL_GROWTH, level - 1);
        // 같은 레벨을 반복 구매할 때마다 추가 증가
        double byBuy    = Math.Pow(SUMMON_BUY_GROWTH, summonPurchaseCounts[idx]);
        return RoundToHundred(baseCost * byBuy);
    }

    public void RecordSummonPurchase(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, 24);
        summonPurchaseCounts[idx] = Mathf.Clamp(summonPurchaseCounts[idx] + 1, 0, int.MaxValue);
        OnUpgradeChanged?.Invoke();
    }
    
    public void RecordGemSummonPurchase(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, 24);
        gemSummonPurchaseCounts[idx] = Mathf.Clamp(gemSummonPurchaseCounts[idx] + 1, 0, int.MaxValue);
        OnUpgradeChanged?.Invoke();
    }
    
    public int GetGemSummonPurchaseCount(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, 24);
        return gemSummonPurchaseCounts[idx];
    }

    // ───────── Getter들 (Prestige 보정 포함) ─────────
    public int GetMaxManualSpawnCount() {
        int v = baseManualSpawnMax + manualSpawnMaxUpgrade;
        try { v += PrestigeManager.Instance.GetManualSpawnMaxPlus(); } catch {}
        return v;
    }

    public float GetManualSpawnInterval() {
        float iv = baseManualSpawnInterval * Mathf.Pow(0.9f, manualSpawnSpeedUpgrade);
        try { iv *= (float)PrestigeManager.Instance.GetManualSpawnIntervalMul(); } catch {}
        return iv;
    }

    public int GetMaxFieldCount() {
        int v = baseFieldCount + maxFieldCountUpgrade * 2;
        try { v += PrestigeManager.Instance.GetFieldMaxPlus(); } catch {}
        return v;
    }

    public double GetClickBonusMultiplier() {
        double mul = 1.0 + clickBonusUpgrade * CLICK_BONUS_PER_LEVEL;
        try { mul *= PrestigeManager.Instance.GetClickBonusMul(); } catch {}
        return mul;
    }

    public bool IsAutoMergeOn() => autoMergeOn && autoMergeUpgrade > 0;
    public bool IsAutoSpawnOn() => autoSpawnOn && autoSpawnUpgrade > 0;
    public void SetAutoMergeOn(bool on){ if (autoMergeOn==on) return; autoMergeOn = on; OnUpgradeChanged?.Invoke(); }
    public void SetAutoSpawnOn(bool on){ if (autoSpawnOn==on) return; autoSpawnOn = on; OnUpgradeChanged?.Invoke(); }

    public float GetAutoMergeInterval(){
        if(!IsAutoMergeOn()) return float.MaxValue;
        int lv=Mathf.Max(1,autoMergeUpgrade);
        float iv=autoMergeBaseInterval*Mathf.Pow(autoPerLevelMul, lv-1);
        iv = Mathf.Max(autoIntervalFloor, iv);
        try { iv *= (float)PrestigeManager.Instance.GetAutoMergeIntervalMul(); } catch {}
        return iv;
    }

    public float GetAutoSpawnInterval(){
        if(!IsAutoSpawnOn()) return float.MaxValue;
        int lv=Mathf.Max(1,autoSpawnUpgrade);
        float iv=autoSpawnBaseInterval*Mathf.Pow(autoPerLevelMul, lv-1);
        iv = Mathf.Max(autoIntervalFloor, iv);
        try { iv *= (float)PrestigeManager.Instance.GetAutoSpawnIntervalMul(); } catch {}
        return iv;
    }

    // UI 표시용: 토글 꺼져 있어도 "현재 업그레이드 기준 이론상 간격"을 보여주기 위한 헬퍼
    public float GetAutoMergeIntervalForDisplay(){
        if (autoMergeUpgrade <= 0) return float.MaxValue;
        int lv = Mathf.Max(1, autoMergeUpgrade);
        float iv = autoMergeBaseInterval * Mathf.Pow(autoPerLevelMul, lv - 1);
        iv = Mathf.Max(autoIntervalFloor, iv);
        try { iv *= (float)PrestigeManager.Instance.GetAutoMergeIntervalMul(); } catch {}
        return iv;
    }

    public float GetAutoSpawnIntervalForDisplay(){
        if (autoSpawnUpgrade <= 0) return float.MaxValue;
        int lv = Mathf.Max(1, autoSpawnUpgrade);
        float iv = autoSpawnBaseInterval * Mathf.Pow(autoPerLevelMul, lv - 1);
        iv = Mathf.Max(autoIntervalFloor, iv);
        try { iv *= (float)PrestigeManager.Instance.GetAutoSpawnIntervalMul(); } catch {}
        return iv;
    }

    public double GetOfflineRewardMultiplier() {
        double mul = 1.0 + (offlineRewardUpgrade * 0.25);
        try { mul *= PrestigeManager.Instance.GetOfflineRewardMul(); } catch {}
        return mul;
    }

    public double GetOfflineMaxSeconds() {
        double sec = (2.0 + offlineMaxTimeUpgrade * 0.5) * 3600.0;
        try { sec += PrestigeManager.Instance.GetOfflineMaxExtraSeconds(); } catch {}
        return sec;
    }

    // 레벨/캡 Getter (UI용)
    public int GetSpawnMaxUpgradeLevel()      => manualSpawnMaxUpgrade;
    public int GetSpawnSpeedUpgradeLevel()    => manualSpawnSpeedUpgrade;
    public int GetFieldMaxUpgradeLevel()      => maxFieldCountUpgrade;
    public int GetClickBonusUpgradeLevel()    => clickBonusUpgrade;
    public int GetOfflineRewardUpgradeLevel() => offlineRewardUpgrade;
    public int GetOfflineMaxTimeUpgradeLevel()=> offlineMaxTimeUpgrade;
    public int GetAutoMergeUpgradeLevel()     => autoMergeUpgrade;
    public int GetAutoSpawnUpgradeLevel()     => autoSpawnUpgrade;

    public int GetSpawnMaxUpgradeCap()        => spawnMaxUpgradeCap;
    public int GetSpawnSpeedUpgradeCap()      => spawnSpeedUpgradeCap;
    public int GetFieldMaxUpgradeCap()        => fieldMaxUpgradeCap;
    public int GetClickBonusUpgradeCap()      => clickBonusUpgradeCap;
    public int GetOfflineRewardCap()          => offlineRewardCap;
    public int GetOfflineMaxTimeCap()         => offlineMaxTimeCap;
    public int GetAutoMergeCap()              => autoMergeCap;
    public int GetAutoSpawnCap()              => autoSpawnCap;

    // ───────── 업그레이드 비용/구매 ─────────
    public double GetSpawnMaxUpgradeCost(int lv)
        => (lv >= spawnMaxUpgradeCap)   ? double.PositiveInfinity : RoundToHundred(UPG_SPAWN_MAX_BASE    * Math.Pow(UPG_SPAWN_MAX_GROW,    lv));
    public double GetSpawnSpeedUpgradeCost(int lv)
        => (lv >= spawnSpeedUpgradeCap) ? double.PositiveInfinity : RoundToHundred(UPG_SPAWN_SPEED_BASE  * Math.Pow(UPG_SPAWN_SPEED_GROW,  lv));
    public double GetFieldMaxUpgradeCost(int lv)
        => (lv >= fieldMaxUpgradeCap)   ? double.PositiveInfinity : RoundToHundred(UPG_FIELD_MAX_BASE    * Math.Pow(UPG_FIELD_MAX_GROW,    lv));
    public double GetClickBonusUpgradeCost(int lv)
        => (lv >= clickBonusUpgradeCap) ? double.PositiveInfinity : RoundToHundred(UPG_CLICK_BONUS_BASE  * Math.Pow(UPG_CLICK_BONUS_GROW,  lv));
    public double GetOfflineRewardUpgradeCost(int lv)
        => (lv >= offlineRewardCap)     ? double.PositiveInfinity : RoundToHundred(UPG_OFFLINE_REWARD_BASE  * Math.Pow(UPG_OFFLINE_REWARD_GROW,  lv));
    public double GetOfflineMaxTimeUpgradeCost(int lv)
        => (lv >= offlineMaxTimeCap)    ? double.PositiveInfinity : RoundToHundred(UPG_OFFLINE_MAXTIME_BASE * Math.Pow(UPG_OFFLINE_MAXTIME_GROW, lv));

    public bool TryBuySpawnMaxUpgrade()   { if (manualSpawnMaxUpgrade   >= spawnMaxUpgradeCap)   return false; double c=GetSpawnMaxUpgradeCost(manualSpawnMaxUpgrade);      if(!SpendGold(c)) return false; manualSpawnMaxUpgrade++;   OnUpgradeChanged?.Invoke(); return true; }
    public bool TryBuySpawnSpeedUpgrade() { if (manualSpawnSpeedUpgrade >= spawnSpeedUpgradeCap) return false; double c=GetSpawnSpeedUpgradeCost(manualSpawnSpeedUpgrade);  if(!SpendGold(c)) return false; manualSpawnSpeedUpgrade++; OnUpgradeChanged?.Invoke(); return true; }
    public bool TryBuyFieldMaxUpgrade()   { if (maxFieldCountUpgrade    >= fieldMaxUpgradeCap)   return false; double c=GetFieldMaxUpgradeCost(maxFieldCountUpgrade);       if(!SpendGold(c)) return false; maxFieldCountUpgrade++;    OnUpgradeChanged?.Invoke(); return true; }
    public bool TryBuyClickBonusUpgrade() { if (clickBonusUpgrade       >= clickBonusUpgradeCap) return false; double c=GetClickBonusUpgradeCost(clickBonusUpgrade);        if(!SpendGold(c)) return false; clickBonusUpgrade++;       OnUpgradeChanged?.Invoke(); return true; }
    public bool TryBuyOfflineRewardUpgrade(){ if(offlineRewardUpgrade   >= offlineRewardCap)     return false; double c=GetOfflineRewardUpgradeCost(offlineRewardUpgrade);  if(!SpendGold(c)) return false; offlineRewardUpgrade++;    OnUpgradeChanged?.Invoke(); return true; }
    public bool TryBuyOfflineMaxTimeUpgrade(){ if(offlineMaxTimeUpgrade >= offlineMaxTimeCap)    return false; double c=GetOfflineMaxTimeUpgradeCost(offlineMaxTimeUpgrade);if(!SpendGold(c)) return false; offlineMaxTimeUpgrade++;  OnUpgradeChanged?.Invoke(); return true; }

    // ───────── Auto ─────────
    public double GetAutoMergeNextCost()
    {
        if (autoMergeUpgrade >= autoMergeCap) return double.PositiveInfinity;
        if (autoMergeUpgrade <= 0) return RoundToHundred(AUTO_MERGE_BASE);
        return RoundToHundred(AUTO_MERGE_BASE * Math.Pow(AUTO_MERGE_GROW, autoMergeUpgrade));
    }
    public bool   TryBuyAutoMergeUpgrade()
    {
        if (autoMergeUpgrade >= autoMergeCap) return false;
        double c=GetAutoMergeNextCost(); if(!SpendGold(c)) return false;
        autoMergeUpgrade = Mathf.Max(0,autoMergeUpgrade)+1; OnUpgradeChanged?.Invoke(); return true;
    }

    public double GetAutoSpawnNextCost()
    {
        if (autoSpawnUpgrade >= autoSpawnCap) return double.PositiveInfinity;
        if (autoSpawnUpgrade <= 0) return RoundToHundred(AUTO_SPAWN_BASE);
        return RoundToHundred(AUTO_SPAWN_BASE * Math.Pow(AUTO_SPAWN_GROW, autoSpawnUpgrade));
    }
    public bool   TryBuyAutoSpawnUpgrade()
    {
        if (autoSpawnUpgrade >= autoSpawnCap) return false;
        double c=GetAutoSpawnNextCost(); if(!SpendGold(c)) return false;
        autoSpawnUpgrade = Mathf.Max(0,autoSpawnUpgrade)+1; OnUpgradeChanged?.Invoke(); return true;
    }

    // 보석 구매 전용 메서드 (골드 차감 없이 레벨만 증가)
    public bool TryBuyAutoMergeUpgradeWithGems()
    {
        if (autoMergeUpgrade >= autoMergeCap) return false;
        autoMergeUpgrade = Mathf.Max(0, autoMergeUpgrade) + 1;
        OnUpgradeChanged?.Invoke();
        return true;
    }

    public bool TryBuyAutoSpawnUpgradeWithGems()
    {
        if (autoSpawnUpgrade >= autoSpawnCap) return false;
        autoSpawnUpgrade = Mathf.Max(0, autoSpawnUpgrade) + 1;
        OnUpgradeChanged?.Invoke();
        return true;
    }

    // ───────── Save / Load ─────────
    public void CollectSaveData(SaveData d)
    {
        d.gold = gold;

        d.manualSpawnMaxUpgrade   = manualSpawnMaxUpgrade;
        d.manualSpawnSpeedUpgrade = manualSpawnSpeedUpgrade;
        d.maxFieldCountUpgrade    = maxFieldCountUpgrade;
        d.clickBonusUpgrade       = clickBonusUpgrade;

        d.autoMergeUpgrade = autoMergeUpgrade;
        d.autoSpawnUpgrade = autoSpawnUpgrade;
        d.autoMergeOn      = autoMergeOn;
        d.autoSpawnOn      = autoSpawnOn;

        d.offlineRewardUpgrade  = offlineRewardUpgrade;
        d.offlineMaxTimeUpgrade = offlineMaxTimeUpgrade;

        if(d.summonPurchaseCounts==null || d.summonPurchaseCounts.Length!=25) d.summonPurchaseCounts=new int[25];
        Array.Copy(summonPurchaseCounts, d.summonPurchaseCounts, 25);
        
        // 보석 소환 횟수 저장
        if(d.gemSummonPurchaseCounts==null || d.gemSummonPurchaseCounts.Length!=25) d.gemSummonPurchaseCounts=new int[25];
        Array.Copy(gemSummonPurchaseCounts, d.gemSummonPurchaseCounts, 25);
    }

    public void ApplyLoadedData(SaveData d)
    {
        gold = d.gold;

        manualSpawnMaxUpgrade   = Mathf.Max(0,d.manualSpawnMaxUpgrade);
        manualSpawnSpeedUpgrade = Mathf.Max(0,d.manualSpawnSpeedUpgrade);
        maxFieldCountUpgrade    = Mathf.Max(0,d.maxFieldCountUpgrade);
        clickBonusUpgrade       = Mathf.Max(0,d.clickBonusUpgrade);

        autoMergeUpgrade = Mathf.Max(0,d.autoMergeUpgrade);
        autoSpawnUpgrade = Mathf.Max(0,d.autoSpawnUpgrade);
        autoMergeOn      = d.autoMergeOn;
        autoSpawnOn      = d.autoSpawnOn;

        offlineRewardUpgrade  = Mathf.Max(0,d.offlineRewardUpgrade);
        offlineMaxTimeUpgrade = Mathf.Max(0,d.offlineMaxTimeUpgrade);

        if(d.summonPurchaseCounts!=null && d.summonPurchaseCounts.Length==25) Array.Copy(d.summonPurchaseCounts, summonPurchaseCounts, 25);
        
        // 보석 소환 횟수 복원 (SaveData에 필드가 없으면 0으로 시작)
        if(d.gemSummonPurchaseCounts!=null && d.gemSummonPurchaseCounts.Length==25) 
            Array.Copy(d.gemSummonPurchaseCounts, gemSummonPurchaseCounts, 25);
        else
            Array.Clear(gemSummonPurchaseCounts, 0, gemSummonPurchaseCounts.Length);

        OnGoldChanged?.Invoke(gold);
        OnUpgradeChanged?.Invoke();
        RefreshGoldHUD();
    }

    // ───────── Prestige Reset ─────────
    public void ResetGoldUpgradesForPrestige()
    {
        manualSpawnMaxUpgrade=0; manualSpawnSpeedUpgrade=0; maxFieldCountUpgrade=0; clickBonusUpgrade=0;
        autoMergeUpgrade=0; autoSpawnUpgrade=0; autoMergeOn=false; autoSpawnOn=false;
        offlineRewardUpgrade=0; offlineMaxTimeUpgrade=0;
        Array.Clear(summonPurchaseCounts,0,summonPurchaseCounts.Length);
        Array.Clear(gemSummonPurchaseCounts,0,gemSummonPurchaseCounts.Length);
        OnUpgradeChanged?.Invoke();
        RefreshGoldHUD();
    }
}
