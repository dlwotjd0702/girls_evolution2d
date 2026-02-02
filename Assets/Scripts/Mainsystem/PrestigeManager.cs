// ============================================================================
// PrestigeManager.cs  (DROP-IN, Shop 패널 완전 호환 + 환생 버튼 관리)
// - 환생 포인트/환생 상점 12종 통합 관리
// - SaveData v2 호환(필드명 유지; 리플렉션 저장/로드)
// - 포인트 텍스트: TextMeshProUGUI 한 개 (옵션)
// - 환생 버튼: Tier 3(=4층)에서만 노출, Final 보유 시에만 interactable
// - 환생 플로우: SetGold(0) → Economy 리셋 → Tier 0 → 시작자금 AddGold(한 번에)
//   * 모든 HUD는 값 바뀔 때만 즉시 반영됨(EconomyManager 쪽 가드와 연동)
// ============================================================================

using System;
using System.Reflection;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrestigeManager : MonoBehaviour, ISaveable
{
    public static PrestigeManager Instance { get; private set; }

    [Header("Refs")]
    public GirlFieldManager girlFieldManager;
    public EconomyManager   economy;
    public TierManager      tierManager;

    [Header("Rules")]
    public int topLevel = TierRules.MaxLevel; // 기본 25

    // ───────── 런타임 값 ─────────
    [SerializeField] private int prestigePoint = 0;
    [SerializeField] private int totalPrestigeCount = 0;

    // ───────── 상점 레벨(저장 대상) ─────────
    // 핵심 4종
    [SerializeField] private int incomeLv = 0;            // 수익 배수(+8%/Lv)
    [SerializeField] private int twoStepLv = 0;           // +2단 확률(+2.5%p/Lv, cap 20%p)
    [SerializeField] private int startGoldLv = 0;         // 시작자금 배수(+10%/Lv)
    [SerializeField] private int prestigeGainLv = 0;      // 환생포인트 획득(+10%/Lv)

    // Plus 8종 (일반 상점 항목에 대한 영구 보정)
    [SerializeField] private int plusManualSpawnMaxLv = 0;
    [SerializeField] private int plusManualSpawnSpeedLv = 0;
    [SerializeField] private int plusAutoMergeSpeedLv = 0;
    [SerializeField] private int plusAutoSpawnSpeedLv = 0;
    [SerializeField] private int plusFieldMaxLv = 0;
    [SerializeField] private int plusClickBonusLv = 0;
    [SerializeField] private int plusOfflineRewardLv = 0;
    [SerializeField] private int plusOfflineMaxTimeLv = 0;

    // ───────── 비용 곡선 ─────────
    // 리밸런싱: 25단계 기본 환생 포인트 2500 기준으로 비용 재조정
    // 골드 강화와 환생 업그레이드를 함께 고려하여 적절한 레벨 캡 설정
    // - 맥스레벨이 높은 항목은 성장률을 낮춰 과도한 증가를 방지
    [Header("Costs (Core)")]
    private int   incomeBase  = 300;  private float incomeGrow  = 1.15f;
    private int   twoStepBase = 300;  private float twoStepGrow = 1.25f;
    private int   startBase   = 300;  private float startGrow   = 1.15f;
    private int   ppgBase     = 300;  private float ppgGrow     = 1.15f;

    [Header("Costs (Plus)")]
    private int plusManualSpawnMaxBase   = 300;  private float plusManualSpawnMaxGrow   = 1.12f;
    private int plusManualSpawnSpeedBase = 300;  private float plusManualSpawnSpeedGrow = 1.12f;
    private int plusAutoMergeSpeedBase   = 600;  private float plusAutoMergeSpeedGrow   = 1.22f;
    private int plusAutoSpawnSpeedBase   = 600;  private float plusAutoSpawnSpeedGrow   = 1.22f;
    private int plusFieldMaxBase         = 300;  private float plusFieldMaxGrow         = 1.12f;
    private int plusClickBonusBase       = 300;  private float plusClickBonusGrow       = 1.12f;
    private int plusOfflineRewardBase    = 300;  private float plusOfflineRewardGrow     = 1.12f;
    private int plusOfflineMaxTimeBase   = 300;  private float plusOfflineMaxTimeGrow    = 1.12f;

    // ───────── UI (옵션) ─────────
    [Header("UI (Optional)")]
    [SerializeField] private TextMeshProUGUI prestigePointLabel;

    [Header("Prestige Button (Managed Here)")]
    [SerializeField] private Button prestigeButton;
    [SerializeField] private bool   hideWhenNotOnTop   = true;  // 4층 아닐 때 숨김
    [SerializeField] private bool   disableWhenNotReady = true; // 준비 안되면 비활성

    [Header("Prestige Confirm Panel")]
    [SerializeField] private PrestigeConfirmPanel confirmPanel; // 환생 확인 패널

    public event Action<int> OnPrestigePointsChanged;

    // 내부: 버튼 폴링(머지 직후 등 이벤트 누락 대비)
    float _btnRefreshTimer = 0f;
    const float BTN_REFRESH_INTERVAL = 0.25f;

    // ───────── Unity ─────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!girlFieldManager) girlFieldManager = FindObjectOfType<GirlFieldManager>(true);
        if (!economy)          economy          = FindObjectOfType<EconomyManager>(true);
        if (!tierManager)      tierManager      = FindObjectOfType<TierManager>(true);
        if (topLevel <= 0)     topLevel         = TierRules.MaxLevel;
    }

    void OnEnable()
    {
        if (prestigeButton)
        {
            prestigeButton.onClick.RemoveAllListeners();
            prestigeButton.onClick.AddListener(OnClickPrestigeButton);
        }

        if (tierManager != null)
        {
            tierManager.OnTierChanged -= HandleTierChanged;
            tierManager.OnTierChanged += HandleTierChanged;
        }

        RefreshPrestigeButton(true);
        UpdatePrestigePointLabel();
    }

    void OnDisable()
    {
        if (prestigeButton) prestigeButton.onClick.RemoveAllListeners();
        if (tierManager != null) tierManager.OnTierChanged -= HandleTierChanged;
    }

    void Update()
    {
        _btnRefreshTimer += Time.unscaledDeltaTime;
        if (_btnRefreshTimer >= BTN_REFRESH_INTERVAL)
        {
            _btnRefreshTimer = 0f;
            RefreshPrestigeButton();
        }
    }

    // ───────── 버튼/라벨 ─────────
    void HandleTierChanged(int _) => RefreshPrestigeButton(true);

    void OnClickPrestigeButton()
    {
        if (!IsPrestigeReady()) return;
        
        // 확인 패널이 있으면 표시, 없으면 바로 환생
        if (confirmPanel != null)
        {
            confirmPanel.Show();
        }
        else
        {
        DoPrestige();
        RefreshPrestigeButton(true);
        }
    }

    public void RefreshPrestigeButton(bool force=false)
    {
        if (!prestigeButton && !hideWhenNotOnTop) return;

        bool onTop = (tierManager != null && tierManager.CurrentTierIndex == 3);

        if (prestigeButton && hideWhenNotOnTop)
            prestigeButton.gameObject.SetActive(onTop);

        bool ready = IsPrestigeReady();

        if (prestigeButton)
        {
            if (disableWhenNotReady) prestigeButton.interactable = onTop && ready;
            else prestigeButton.interactable = onTop;
        }
    }

    void UpdatePrestigePointLabel()
    {
        if (prestigePointLabel)
            prestigePointLabel.text = $"<b>{prestigePoint:N0} pt</b>";
    }

    // ───────── 리플렉션 유틸 ─────────
    static void TrySetInt(object obj, string name, int value)
    {
        var f = obj.GetType().GetField(name, BindingFlags.Public|BindingFlags.Instance);
        if (f != null && f.FieldType == typeof(int)) f.SetValue(obj, value);
    }
    static int TryGetInt(object obj, string name, int fallback)
    {
        var f = obj.GetType().GetField(name, BindingFlags.Public|BindingFlags.Instance);
        if (f != null && f.FieldType == typeof(int)) return (int)f.GetValue(obj);
        return fallback;
    }
    static Type FindTypeByName(string name)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(name);
            if (t != null) return t;
            try { foreach (var tt in asm.GetTypes()) if (tt.Name == name) return tt; } catch {}
        }
        return null;
    }

    // ───────── 환생 준비/계산 ─────────
    bool HasAnyFinalGirl()
    {
        if (!girlFieldManager) return false;
        foreach (var g in girlFieldManager.girlList)
            if (g && g.Level >= topLevel) return true;
        return false;
    }

    public bool IsPrestigeReady() => HasAnyFinalGirl();

    // 25단계 기준 1000 포인트, 하위 단계는 1/2씩 감소
    // level25UpgradeLevel과 필드의 모든 캐릭터 레벨을 고려하여 포인트 계산
    // 골드/보석 강화 레벨에 대한 포인트도 추가 (이전 10포인트/레벨 → 지금 100포인트/레벨로 10배 증가)
    public int PreviewPrestigeGain()
    {
        if (girlFieldManager == null || economy == null) return 0;
        
        int totalPoints = 0;
        int level25UpgradeLevel = girlFieldManager.Level25UpgradeLevel;
        
        // 1. 레벨 25 포인트 계산 (level25UpgradeLevel 기준)
        // 레벨 25: 2500 포인트 × (스택 성장률)
        if (level25UpgradeLevel > 0)
        {
            const double BASE_L25 = 2500.0;
            const double L25_STACK_GROW = 1.35; // 스택 증가량이 1.2~1.5배 느낌
            double level25Points = BASE_L25 * Math.Pow(L25_STACK_GROW, Math.Max(0, level25UpgradeLevel - 1));
            totalPoints += Mathf.CeilToInt((float)level25Points);
        }
        
        // 2. 필드의 다른 레벨 캐릭터들 포인트 계산 (25단계 제외)
        foreach (var g in girlFieldManager.girlList)
        {
            if (g == null) continue;
            int level = g.Level;
            
            // 레벨 25 이상은 이미 계산했으므로 제외
            if (level >= topLevel) continue;
            
            // 하위 단계 포인트 계산: 25단계 기준 1000 포인트에서 단계 내려갈 때마다 1/2
            // 24단계: 500, 23단계: 250, 22단계: 125, ...
            int diff = topLevel - level; // 25→24: 1, 25→23: 2, ...
            double points = 2500.0 / Math.Pow(2.0, diff);
            totalPoints += Mathf.CeilToInt((float)points); // 소수점 올림 처리
        }
        
        // 3. 골드/보석 강화 레벨에 대한 포인트 추가 (10배 증가: 이전 10포인트/레벨 → 지금 100포인트/레벨)
        const int POINTS_PER_UPGRADE_LEVEL = 100; // 이전 10포인트에서 10배 증가
        
        int upgradePoints = 0;
        upgradePoints += economy.GetSpawnMaxUpgradeLevel() * POINTS_PER_UPGRADE_LEVEL;
        upgradePoints += economy.GetSpawnSpeedUpgradeLevel() * POINTS_PER_UPGRADE_LEVEL;
        upgradePoints += economy.GetFieldMaxUpgradeLevel() * POINTS_PER_UPGRADE_LEVEL;
        upgradePoints += economy.GetClickBonusUpgradeLevel() * POINTS_PER_UPGRADE_LEVEL;
        upgradePoints += economy.GetAutoMergeUpgradeLevel() * POINTS_PER_UPGRADE_LEVEL;
        upgradePoints += economy.GetAutoSpawnUpgradeLevel() * POINTS_PER_UPGRADE_LEVEL;
        upgradePoints += economy.GetOfflineRewardUpgradeLevel() * POINTS_PER_UPGRADE_LEVEL;
        upgradePoints += economy.GetOfflineMaxTimeUpgradeLevel() * POINTS_PER_UPGRADE_LEVEL;
        
        totalPoints += upgradePoints;
        
        if (totalPoints > 0)
        {
            Debug.Log($"[PrestigeManager] PreviewPrestigeGain: {totalPoints} 포인트 (level25UpgradeLevel: {level25UpgradeLevel}, 필드 캐릭터 수: {girlFieldManager.girlList.Count}, 강화 포인트: {upgradePoints})");
        }
        else
        {
            Debug.LogWarning($"[PrestigeManager] PreviewPrestigeGain: 0 포인트 반환! (level25UpgradeLevel: {level25UpgradeLevel})");
        }
        
        return totalPoints;
    }

    public int  GetPrestigePoints()      => prestigePoint;
    public int  GetTotalPrestigeCount()  => totalPrestigeCount;
    public void AddPrestigePoints(int amount)
    {
        if (amount <= 0) return;
        prestigePoint += amount;
        NotifyPointsChanged();
        SaveManager.Instance?.SaveGame();
    }

    public bool SpendPrestigePoints(int amount)
    {
        if (amount <= 0) return true;
        if (prestigePoint < amount) return false;
        prestigePoint -= amount;
        NotifyPointsChanged();
        return true;
    }

    public void DoPrestige()
    {
        if (!girlFieldManager || !economy) return;
        if (!HasAnyFinalGirl()) return;
        
        // 환생 효과음 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPrestigeSFX();
        }
        
        economy.ResetGoldUpgradesForPrestige();
        // 1) 포인트 적용
        int baseGain = PreviewPrestigeGain();
        double mulPPG = GetPrestigePointGainMul();
        // 소수점 올림 처리
        int gain = Mathf.Max(0, Mathf.CeilToInt((float)(baseGain * mulPPG)));
        
        Debug.Log($"[PrestigeManager] DoPrestige: baseGain={baseGain}, mulPPG={mulPPG:F2}, gain={gain}, 현재 포인트={prestigePoint}");
        
        prestigePoint      += gain;
        totalPrestigeCount += 1;
        NotifyPointsChanged();
        
        Debug.Log($"[PrestigeManager] DoPrestige 완료: 포인트 추가 후={prestigePoint}, 총 환생 횟수={totalPrestigeCount}");

        // (선택) 계급 연동
        try
        {
            var t = FindTypeByName("LegacyRankManager");
            var pi = t?.GetProperty("Instance", BindingFlags.Public|BindingFlags.Static);
            var inst = pi?.GetValue(null, null);
            var mi = t?.GetMethod("AddXp", BindingFlags.Public|BindingFlags.Instance);
            if (inst != null && mi != null) mi.Invoke(inst, new object[]{ baseGain });
        }
        catch {}

        // 업적 체크는 AchievementManager에서 자동으로 처리됨

        // 2) 필드 비우기 (도감 해금 정보는 유지)
        var snapshot = new List<GirlCharacter>(girlFieldManager.girlList);
        foreach (var g in snapshot) if (g != null) girlFieldManager.RemoveGirl(g);
        girlFieldManager.girlList.Clear();
        girlFieldManager.ResetLevel25Progress();
        // CurrentMaxLevel 리셋 (필드가 비워졌으므로 1로 초기화)
        if (girlFieldManager != null)
        {
            girlFieldManager.RecomputeMaxLevelAndNotify();
        }
        // discoveredLevels는 유지 (환생 시 도감 해금 정보 보존)

        // 3) 경제/티어 리셋 (HUD 즉시 0 표시)
        economy.SetGold(0);
        economy.ResetGoldUpgradesForPrestige(); // 골드로 구매한 강화 초기화
        // 보석으로 구매한 강화는 유지 (환생 시 보석 강화 보존)
        try 
        { 
            if (tierManager != null)
            {
                // 티어 언락 초기화 (0층만 해금) - 먼저 실행하여 0층이 확실히 해금된 상태로 만듦
                tierManager.ResetTierUnlocks();
                // 환생 직후 제일 낮은 계층(0층)으로 강제 이동
                tierManager.ForceSwitchTo(0);
            }
        } catch {}

        // 4) 시작 자금 지급 = (Lv1 60초 수익) × (계급 배수) × (상점 배수)
        double base1Min = economy.GetLevelIncomePerSec(1) * 60.0;

        double mulRank = 1.0;
        try
        {
            var t = FindTypeByName("LegacyRankManager");
            var pi = t?.GetProperty("Instance", BindingFlags.Public|BindingFlags.Static);
            var inst = pi?.GetValue(null, null);
            var mi = t?.GetMethod("GetStartGoldMultiplier", BindingFlags.Public|BindingFlags.Instance);
            if (inst != null && mi != null) mulRank = Convert.ToDouble(mi.Invoke(inst, null));
        }
        catch {}

        double startGold = base1Min * mulRank * GetStartGoldMultiplier();
        if (startGold > 0) economy.AddGold(startGold);

        // 환생 후 즉시 저장 (필드 비움, 골드 0, 강화 초기화 반영)
        SaveManager.Instance?.SaveGame();

        // 버튼 즉시 갱신
        RefreshPrestigeButton(true);
    }

    // ───────── 효과 쿼리(게임 적용) ─────────
    // 핵심 4종
    public double GetIncomeMultiplier()        => 1.0 + 0.08 * incomeLv;
    public float  GetTwoStepChance()           => Mathf.Min(0.20f, 0.025f * twoStepLv);
    public double GetStartGoldMultiplier()     => 1.0 + 0.10 * startGoldLv;
    public double GetPrestigePointGainMul()    => 1.0 + 0.10 * prestigeGainLv;

    // Plus 8종
    public int    GetManualSpawnMaxPlus()      => plusManualSpawnMaxLv;
    public double GetManualSpawnIntervalMul()  => Math.Max(0.50, 1.0 - 0.05 * plusManualSpawnSpeedLv);
    public double GetAutoMergeIntervalMul()    => Math.Max(0.50, 1.0 - 0.03 * plusAutoMergeSpeedLv);
    public double GetAutoSpawnIntervalMul()    => Math.Max(0.50, 1.0 - 0.03 * plusAutoSpawnSpeedLv);
    public int    GetFieldMaxPlus()            => plusFieldMaxLv;
    public double GetClickBonusMul()           => 1.0 + 0.05 * plusClickBonusLv;
    public double GetOfflineRewardMul()        => 1.0 + 0.05 * plusOfflineRewardLv;
    public int    GetOfflineMaxExtraSeconds()  => 600 * plusOfflineMaxTimeLv;

    // ───────── 비용/구매 API ─────────
    static int GrowthCost(int b, float g, int lv)
    {
        double cost = Math.Ceiling(b * Math.Pow(g, Mathf.Max(0, lv)));
        return Mathf.Max(1, (int)cost);
    }

    // 비용 조회 (핵심)
    public int GetIncomeNextCost()         => GrowthCost(incomeBase,              incomeGrow,              incomeLv);
    public int GetTwoStepNextCost()        => GrowthCost(twoStepBase,             twoStepGrow,             twoStepLv);
    public int GetStartGoldNextCost()      => GrowthCost(startBase,               startGrow,               startGoldLv);
    public int GetPrestigeGainNextCost()   => GrowthCost(ppgBase,                 ppgGrow,                 prestigeGainLv);

    // 비용 조회 (Plus)
    public int GetPlusManualSpawnMaxCost() => GrowthCost(plusManualSpawnMaxBase,  plusManualSpawnMaxGrow,  plusManualSpawnMaxLv);
    public int GetPlusManualSpeedCost()    => GrowthCost(plusManualSpawnSpeedBase,plusManualSpawnSpeedGrow,plusManualSpawnSpeedLv);
    public int GetPlusAutoMergeCost()      => GrowthCost(plusAutoMergeSpeedBase,  plusAutoMergeSpeedGrow,  plusAutoMergeSpeedLv);
    public int GetPlusAutoSpawnCost()      => GrowthCost(plusAutoSpawnSpeedBase,  plusAutoSpawnSpeedGrow,  plusAutoSpawnSpeedLv);
    public int GetPlusFieldMaxCost()       => GrowthCost(plusFieldMaxBase,        plusFieldMaxGrow,        plusFieldMaxLv);
    public int GetPlusClickBonusCost()     => GrowthCost(plusClickBonusBase,      plusClickBonusGrow,      plusClickBonusLv);
    public int GetPlusOfflineRewardCost()  => GrowthCost(plusOfflineRewardBase,   plusOfflineRewardGrow,   plusOfflineRewardLv);
    public int GetPlusOfflineMaxTimeCost() => GrowthCost(plusOfflineMaxTimeBase,  plusOfflineMaxTimeGrow,  plusOfflineMaxTimeLv);

    // 구매 (핵심)
    public bool TryBuyIncome()            => TrySpendAnd(ref incomeLv,           GetIncomeNextCost());
    public bool TryBuyTwoStep()           => TrySpendAnd(ref twoStepLv,          GetTwoStepNextCost());
    public bool TryBuyStartGold()         => TrySpendAnd(ref startGoldLv,        GetStartGoldNextCost());
    public bool TryBuyPrestigeGain()      => TrySpendAnd(ref prestigeGainLv,     GetPrestigeGainNextCost());

    // 구매 (Plus)
    public bool TryBuyPlusManualSpawnMax()=> TrySpendAnd(ref plusManualSpawnMaxLv,   GetPlusManualSpawnMaxCost());
    public bool TryBuyPlusManualSpeed()   => TrySpendAnd(ref plusManualSpawnSpeedLv, GetPlusManualSpeedCost());
    public bool TryBuyPlusAutoMerge()     => TrySpendAnd(ref plusAutoMergeSpeedLv,   GetPlusAutoMergeCost());
    public bool TryBuyPlusAutoSpawn()     => TrySpendAnd(ref plusAutoSpawnSpeedLv,   GetPlusAutoSpawnCost());
    public bool TryBuyPlusFieldMax()      => TrySpendAnd(ref plusFieldMaxLv,         GetPlusFieldMaxCost());
    public bool TryBuyPlusClickBonus()    => TrySpendAnd(ref plusClickBonusLv,       GetPlusClickBonusCost());
    public bool TryBuyPlusOfflineReward() => TrySpendAnd(ref plusOfflineRewardLv,    GetPlusOfflineRewardCost());
    public bool TryBuyPlusOfflineMaxTime()=> TrySpendAnd(ref plusOfflineMaxTimeLv,   GetPlusOfflineMaxTimeCost());

    bool TrySpendAnd(ref int levelField, int cost)
    {
        if (cost <= 0) return false;
        if (!SpendPrestigePoints(cost)) return false;
        levelField += 1;
        SaveManager.Instance?.SaveGame();
        return true;
    }

    // ───────── 레벨 Getter (PrestigeShopPanel 호환) ─────────
    public int GetIncomeLevel()              => incomeLv;
    public int GetTwoStepLevel()             => twoStepLv;
    public int GetStartGoldLevel()           => startGoldLv;
    public int GetPrestigeGainLevel()        => prestigeGainLv;
    public int GetPlusManualSpawnMaxLevel()  => plusManualSpawnMaxLv;
    public int GetPlusManualSpeedLevel()     => plusManualSpawnSpeedLv;
    public int GetPlusAutoMergeLevel()       => plusAutoMergeSpeedLv;
    public int GetPlusAutoSpawnLevel()       => plusAutoSpawnSpeedLv;
    public int GetPlusFieldMaxLevel()        => plusFieldMaxLv;
    public int GetPlusClickBonusLevel()      => plusClickBonusLv;
    public int GetPlusOfflineRewardLevel()   => plusOfflineRewardLv;
    public int GetPlusOfflineMaxTimeLevel()  => plusOfflineMaxTimeLv;

    // ───────── Save ─────────
    public void CollectSaveData(SaveData d)
    {
        d.prestigePoint      = prestigePoint;
        d.totalPrestigeCount = totalPrestigeCount;

        TrySetInt(d, "prestigeShopIncomeLv",        incomeLv);
        TrySetInt(d, "prestigeShopTwoStepLv",       twoStepLv);
        TrySetInt(d, "prestigeShopStartGoldLv",     startGoldLv);
        TrySetInt(d, "prestigeShopPrestigeGainLv",  prestigeGainLv);

        TrySetInt(d, "ppManualSpawnMaxLv",          plusManualSpawnMaxLv);
        TrySetInt(d, "ppManualSpawnSpeedLv",        plusManualSpawnSpeedLv);
        TrySetInt(d, "ppAutoMergeLv",               plusAutoMergeSpeedLv);
        TrySetInt(d, "ppAutoSpawnLv",               plusAutoSpawnSpeedLv);
        TrySetInt(d, "ppMaxFieldCountLv",           plusFieldMaxLv);
        TrySetInt(d, "ppClickBonusLv",              plusClickBonusLv);
        TrySetInt(d, "ppOfflineRewardLv",           plusOfflineRewardLv);
        TrySetInt(d, "ppOfflineMaxTimeLv",          plusOfflineMaxTimeLv);
    }

    public void ApplyLoadedData(SaveData d)
    {
        prestigePoint      = Mathf.Max(0, d.prestigePoint);
        totalPrestigeCount = Mathf.Max(0, d.totalPrestigeCount);

        incomeLv              = TryGetInt(d, "prestigeShopIncomeLv",        incomeLv);
        twoStepLv             = TryGetInt(d, "prestigeShopTwoStepLv",       twoStepLv);
        startGoldLv           = TryGetInt(d, "prestigeShopStartGoldLv",     startGoldLv);
        prestigeGainLv        = TryGetInt(d, "prestigeShopPrestigeGainLv",  prestigeGainLv);

        plusManualSpawnMaxLv  = TryGetInt(d, "ppManualSpawnMaxLv",          plusManualSpawnMaxLv);
        plusManualSpawnSpeedLv= TryGetInt(d, "ppManualSpawnSpeedLv",        plusManualSpawnSpeedLv);
        plusAutoMergeSpeedLv  = TryGetInt(d, "ppAutoMergeLv",               plusAutoMergeSpeedLv);
        plusAutoSpawnSpeedLv  = TryGetInt(d, "ppAutoSpawnLv",               plusAutoSpawnSpeedLv);
        plusFieldMaxLv        = TryGetInt(d, "ppMaxFieldCountLv",           plusFieldMaxLv);
        plusClickBonusLv      = TryGetInt(d, "ppClickBonusLv",              plusClickBonusLv);
        plusOfflineRewardLv   = TryGetInt(d, "ppOfflineRewardLv",           plusOfflineRewardLv);
        plusOfflineMaxTimeLv  = TryGetInt(d, "ppOfflineMaxTimeLv",          plusOfflineMaxTimeLv);

        NotifyPointsChanged();
        RefreshPrestigeButton(true);
    }

    // ───────── UI 라벨 ─────────
    void NotifyPointsChanged()
    {
        if (prestigePointLabel)
            prestigePointLabel.text = $"<b>{prestigePoint:N0} pt</b>";
        OnPrestigePointsChanged?.Invoke(prestigePoint);
    }
}
