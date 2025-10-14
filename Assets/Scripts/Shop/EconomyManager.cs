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
    public void   SetGold(double amount){ gold = Math.Max(0, amount); OnGoldChanged?.Invoke(gold); RefreshGoldHUD(); }
    public bool   SpendGold(double amount){
        if (amount<=0) return true;
        if (gold + 1e-9 < amount) return false;
        gold -= amount;
        OnGoldChanged?.Invoke(gold);
        RefreshGoldHUD();
        return true;
    }
    public void   AddGold(double amount){
        if (amount<=0) return;
        gold += amount;
        OnGoldChanged?.Invoke(gold);
        RefreshGoldHUD();
    }

    // ───────── UI (TMP) ─────────
    [Header("Gold UI (TMP)")]
    [SerializeField] private TextMeshProUGUI goldText;        // 현재 골드
    [SerializeField] private TextMeshProUGUI goldPerSecText;  // 초당 골드
    [SerializeField] private string goldUnitSuffix = "G";
    [SerializeField] private bool   showPlusOnPerSec = true;  // +표시 여부
    [SerializeField] private bool   hidePerSecWhenZero = true;

    // 내부: /s 추정치 (필드매니저가 EMA로 넣어줌)
    private double _goldPerSecEstimate;
    public  double GetGoldPerSecEstimate() => _goldPerSecEstimate;
    public  void   SetGoldPerSecEstimate(double v){
        _goldPerSecEstimate = Math.Max(0, v);
        RefreshGoldHUD();
    }

    void Start(){ RefreshGoldHUD(); }

    // 숫자 축약 포맷
    string FormatCompact(double v, int digits = 1)
    {
        double av = Math.Abs(v);
        string sign = v < 0 ? "-" : "";

        // 동적 포맷 문자열 구성
        string fmt = (digits <= 0) ? "0" : "0." + new string('0', digits);

        if (av < 1_000d)             return $"{sign}{av:0}";
        if (av < 1_000_000d)         return sign + (av / 1_000d).ToString(fmt) + "K";
        if (av < 1_000_000_000d)     return sign + (av / 1_000_000d).ToString(fmt) + "M";
        if (av < 1_000_000_000_000d) return sign + (av / 1_000_000_000d).ToString(fmt) + "B";
        return sign + (av / 1_000_000_000_000d).ToString(fmt) + "T";
    }


    void RefreshGoldHUD()
    {
        if (goldText)
            goldText.text = $"{FormatCompact(gold)} {goldUnitSuffix}";

        if (goldPerSecText)
        {
            bool show = !hidePerSecWhenZero || _goldPerSecEstimate > 0.0001;
            goldPerSecText.gameObject.SetActive(show);
            if (show)
            {
                string plus = showPlusOnPerSec ? "+" : "";
                goldPerSecText.text = $"{plus}{FormatCompact(_goldPerSecEstimate, 1)} {goldUnitSuffix}/s";
            }
        }
    }

    // ───────── Base (spawn/field) ─────────
    [Header("Spawn/Field Base")]
    [SerializeField] private int   baseManualSpawnMax = 3;
    [SerializeField] private float baseManualSpawnInterval = 10f;
    [SerializeField] private int   baseFieldCount = 8;

    // ───────── Balance consts (Inspector 오염 방지) ─────────
    private const double INCOME_BASE_PER_SEC   = 1.0;  // 2^(Lv-1)/s

    // 소환가: 60초치 × 누적구매 성장(1.12^n)
    private const double SUMMON_BASE_SECONDS   = 60.0;
    private const double SUMMON_BUY_GROWTH     = 1.12;

    // 상점 업그레이드(분당 소환가 페이스와 균형)
    private const double UPG_SPAWN_MAX_BASE    = 400;  private const double UPG_SPAWN_MAX_GROW    = 2.00;
    private const double UPG_SPAWN_SPEED_BASE  = 420;  private const double UPG_SPAWN_SPEED_GROW  = 2.05;
    private const double UPG_FIELD_MAX_BASE    = 500;  private const double UPG_FIELD_MAX_GROW    = 2.00;
    private const double UPG_CLICK_BONUS_BASE  = 360;  private const double UPG_CLICK_BONUS_GROW  = 2.05;

    private const double UPG_OFFLINE_REWARD_BASE  = 420; private const double UPG_OFFLINE_REWARD_GROW  = 2.00;
    private const double UPG_OFFLINE_MAXTIME_BASE = 420; private const double UPG_OFFLINE_MAXTIME_GROW = 2.00;

    // 자동(Auto)
    private const double AUTO_MERGE_BASE  = 900;  private const double AUTO_MERGE_GROW  = 2.00;
    private const double AUTO_SPAWN_BASE  = 980;  private const double AUTO_SPAWN_GROW  = 2.00;

    // 클릭 보너스
    private const double CLICK_BONUS_PER_LEVEL = 0.20;

    // 자동 간격
    [Header("Automation (intervals)")]
    [SerializeField] private float autoMergeBaseInterval = 5f;   // Lv1 기준
    [SerializeField] private float autoSpawnBaseInterval = 6f;   // Lv1 기준
    [SerializeField] private float autoPerLevelMul       = 0.9f; // 레벨당 -10%
    [SerializeField] private float autoIntervalFloor     = 0.4f; // 하한

    // Caps
    [Header("Level Caps")]
    [SerializeField] private int spawnMaxUpgradeCap     = 10;
    [SerializeField] private int spawnSpeedUpgradeCap   = 10;
    [SerializeField] private int fieldMaxUpgradeCap     = 10;
    [SerializeField] private int clickBonusUpgradeCap   = 15;
    [SerializeField] private int offlineRewardCap       = 10;
    [SerializeField] private int offlineMaxTimeCap      = 10;
    [SerializeField] private int autoMergeCap           = 10; // 0=미구매, 1..Cap
    [SerializeField] private int autoSpawnCap           = 10;

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

    // ───────── 수익/소환가 규칙 ─────────
    public double GetLevelIncomePerSec(int level)
    {
        level = Math.Max(1, level);
        return INCOME_BASE_PER_SEC * Math.Pow(2.0, level - 1);
    }

    public double GetSummonCostByMinuteRule(int level)
    {
        level = Mathf.Clamp(level, 1, 25);
        int idx = level - 1;
        double baseCost = SUMMON_BASE_SECONDS * GetLevelIncomePerSec(level);
        double byBuy    = Math.Pow(SUMMON_BUY_GROWTH, summonPurchaseCounts[idx]);
        return baseCost * byBuy;
    }
    public void RecordSummonPurchase(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, 24);
        summonPurchaseCounts[idx] = Mathf.Clamp(summonPurchaseCounts[idx] + 1, 0, int.MaxValue);
        OnUpgradeChanged?.Invoke();
    }

    // ───────── Getter들 ─────────
    public int    GetMaxManualSpawnCount() => baseManualSpawnMax + manualSpawnMaxUpgrade;
    public float  GetManualSpawnInterval() => baseManualSpawnInterval * Mathf.Pow(0.9f, manualSpawnSpeedUpgrade);
    public int    GetMaxFieldCount()       => baseFieldCount + maxFieldCountUpgrade * 2;
    public double GetClickBonusMultiplier()=> 1.0 + clickBonusUpgrade * CLICK_BONUS_PER_LEVEL;

    public bool   IsAutoMergeOn() => autoMergeOn && autoMergeUpgrade > 0;
    public bool   IsAutoSpawnOn() => autoSpawnOn && autoSpawnUpgrade > 0;
    public void   SetAutoMergeOn(bool on){ autoMergeOn = on; OnUpgradeChanged?.Invoke(); }
    public void   SetAutoSpawnOn(bool on){ autoSpawnOn = on; OnUpgradeChanged?.Invoke(); }

    public float  GetAutoMergeInterval(){ if(!IsAutoMergeOn()) return float.MaxValue; int lv=Mathf.Max(1,autoMergeUpgrade); float iv=autoMergeBaseInterval*Mathf.Pow(autoPerLevelMul, lv-1); return Mathf.Max(autoIntervalFloor, iv); }
    public float  GetAutoSpawnInterval(){ if(!IsAutoSpawnOn()) return float.MaxValue; int lv=Mathf.Max(1,autoSpawnUpgrade); float iv=autoSpawnBaseInterval*Mathf.Pow(autoPerLevelMul, lv-1); return Mathf.Max(autoIntervalFloor, iv); }

    public double GetOfflineRewardMultiplier() => 1.0 + (offlineRewardUpgrade * 0.25); // x(1+0.25*Lv)
    public double GetOfflineMaxSeconds()       => (2.0 + offlineMaxTimeUpgrade * 0.5) * 3600.0;

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
    public double GetSpawnMaxUpgradeCost(int lv)      => (lv >= spawnMaxUpgradeCap)   ? double.PositiveInfinity : UPG_SPAWN_MAX_BASE    * Math.Pow(UPG_SPAWN_MAX_GROW,    lv);
    public double GetSpawnSpeedUpgradeCost(int lv)    => (lv >= spawnSpeedUpgradeCap) ? double.PositiveInfinity : UPG_SPAWN_SPEED_BASE  * Math.Pow(UPG_SPAWN_SPEED_GROW,  lv);
    public double GetFieldMaxUpgradeCost(int lv)      => (lv >= fieldMaxUpgradeCap)   ? double.PositiveInfinity : UPG_FIELD_MAX_BASE    * Math.Pow(UPG_FIELD_MAX_GROW,    lv);
    public double GetClickBonusUpgradeCost(int lv)    => (lv >= clickBonusUpgradeCap) ? double.PositiveInfinity : UPG_CLICK_BONUS_BASE  * Math.Pow(UPG_CLICK_BONUS_GROW,  lv);
    public double GetOfflineRewardUpgradeCost(int lv) => (lv >= offlineRewardCap)     ? double.PositiveInfinity : UPG_OFFLINE_REWARD_BASE  * Math.Pow(UPG_OFFLINE_REWARD_GROW,  lv);
    public double GetOfflineMaxTimeUpgradeCost(int lv)=> (lv >= offlineMaxTimeCap)    ? double.PositiveInfinity : UPG_OFFLINE_MAXTIME_BASE * Math.Pow(UPG_OFFLINE_MAXTIME_GROW, lv);

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
        if (autoMergeUpgrade <= 0) return AUTO_MERGE_BASE;
        return AUTO_MERGE_BASE * Math.Pow(AUTO_MERGE_GROW, autoMergeUpgrade);
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
        if (autoSpawnUpgrade <= 0) return AUTO_SPAWN_BASE;
        return AUTO_SPAWN_BASE * Math.Pow(AUTO_SPAWN_GROW, autoSpawnUpgrade);
    }
    public bool   TryBuyAutoSpawnUpgrade()
    {
        if (autoSpawnUpgrade >= autoSpawnCap) return false;
        double c=GetAutoSpawnNextCost(); if(!SpendGold(c)) return false;
        autoSpawnUpgrade = Mathf.Max(0,autoSpawnUpgrade)+1; OnUpgradeChanged?.Invoke(); return true;
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
        OnUpgradeChanged?.Invoke();
        RefreshGoldHUD();
    }
}
