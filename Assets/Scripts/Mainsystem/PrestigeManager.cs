// ============================================================================
// PrestigeManager.cs  (FULL, 통합본)
// - 기존 PrestigeManager + PrestigeShopManager 기능을 하나의 컴포넌트로 통합
// - 환생 포인트 정산 / 시작자금 지급 / 씬 리셋 + "환생 상점" 12종 강화 로직/비용/구매
// - SaveData v2 호환: 기존 prestigePoint/totalPrestigeCount 사용 + 나머지는 리플렉션으로 저장/로드
// - GirlFieldManager / GirlMergeManager가 "PrestigeShopManager"를 리플렉션으로 참조하더라도
//   아래의 "PrestigeShopManager (호환용 페이사드)"가 자동 위임하므로 기존 코드 그대로 동작함.
// ============================================================================

using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

public class PrestigeManager : MonoBehaviour, ISaveable
{
    public static PrestigeManager Instance { get; private set; }

    [Header("Refs")]
    public GirlFieldManager girlFieldManager;
    public EconomyManager   economy;
    public TierManager      tierManager;

    [Header("Rules")]
    public int topLevel = TierRules.MaxLevel; // 최종 레벨(25)

    // ────────────────────────── 환생 런타임 저장값 ──────────────────────────
    [SerializeField] private int prestigePoint = 0;
    [SerializeField] private int totalPrestigeCount = 0;

    // ──────────────────────── 환생 상점: 레벨(저장 대상) ────────────────────────
    // 핵심 4종
    [SerializeField] private int incomeLv = 0;            // 전역 수익 배수(+8%/Lv)
    [SerializeField] private int twoStepLv = 0;           // 합성 +2단 확률(+2.5%p/Lv, cap 50%)
    [SerializeField] private int startGoldLv = 0;         // 시작자금 배수(+10%/Lv)
    [SerializeField] private int prestigeGainLv = 0;      // 환생 포인트 추가 획득(+10%/Lv)

    // 일반 상점 항목의 프레스티지 전용 Plus 8종
    [SerializeField] private int plusManualSpawnMaxLv = 0;    // 수동 소환 최대치 +1/Lv
    [SerializeField] private int plusManualSpawnSpeedLv = 0;  // 수동 소환 쿨다운 -5%/Lv (하한 50%)
    [SerializeField] private int plusAutoMergeSpeedLv = 0;    // 자동 합성 주기 -3%/Lv (하한 50%)
    [SerializeField] private int plusAutoSpawnSpeedLv = 0;    // 자동 소환 주기 -3%/Lv (하한 50%)
    [SerializeField] private int plusFieldMaxLv = 0;          // 필드 슬롯 +1/Lv
    [SerializeField] private int plusClickBonusLv = 0;        // 클릭 보너스 +5%/Lv (배수)
    [SerializeField] private int plusOfflineRewardLv = 0;     // 오프라인 보상 +5%/Lv (배수)
    [SerializeField] private int plusOfflineMaxTimeLv = 0;    // 오프라인 상한 +10분/Lv

    // ─────────────────────────── 비용 곡선(에디터 튜닝) ───────────────────────────
    //  Cost(lv→lv+1) = ceil(base * growth^lv)
    [Header("Costs (Core)")]
    [SerializeField] private int   incomeBase  = 3;  [SerializeField] private float incomeGrow  = 1.35f;
    [SerializeField] private int   twoStepBase = 4;  [SerializeField] private float twoStepGrow = 1.45f;
    [SerializeField] private int   startBase   = 3;  [SerializeField] private float startGrow   = 1.30f;
    [SerializeField] private int   ppgBase     = 5;  [SerializeField] private float ppgGrow     = 1.40f; // prestige point gain

    [Header("Costs (Plus)")]
    [SerializeField] private int plusManualSpawnMaxBase   = 2;  [SerializeField] private float plusManualSpawnMaxGrow   = 1.35f;
    [SerializeField] private int plusManualSpawnSpeedBase = 2;  [SerializeField] private float plusManualSpawnSpeedGrow = 1.30f;
    [SerializeField] private int plusAutoMergeSpeedBase   = 2;  [SerializeField] private float plusAutoMergeSpeedGrow   = 1.35f;
    [SerializeField] private int plusAutoSpawnSpeedBase   = 2;  [SerializeField] private float plusAutoSpawnSpeedGrow   = 1.35f;
    [SerializeField] private int plusFieldMaxBase         = 3;  [SerializeField] private float plusFieldMaxGrow         = 1.40f;
    [SerializeField] private int plusClickBonusBase       = 2;  [SerializeField] private float plusClickBonusGrow       = 1.30f;
    [SerializeField] private int plusOfflineRewardBase    = 2;  [SerializeField] private float plusOfflineRewardGrow    = 1.30f;
    [SerializeField] private int plusOfflineMaxTimeBase   = 2;  [SerializeField] private float plusOfflineMaxTimeGrow   = 1.25f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!girlFieldManager) girlFieldManager = FindObjectOfType<GirlFieldManager>(true);
        if (!economy)          economy          = FindObjectOfType<EconomyManager>(true);
        if (!tierManager)      tierManager      = FindObjectOfType<TierManager>(true);
        if (topLevel <= 0)     topLevel         = TierRules.MaxLevel;
    }

    // ───────────────────────────── 유틸(리플렉션) ─────────────────────────────
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
            try
            {
                foreach (var tt in asm.GetTypes())
                    if (tt.Name == name) return tt;
            }
            catch { }
        }
        return null;
    }

    // ───────────────────────────── 환생 계산 ─────────────────────────────
    bool HasAnyFinalGirl()
    {
        if (!girlFieldManager) return false;
        foreach (var g in girlFieldManager.girlList)
            if (g && g.Level >= topLevel) return true;
        return false;
    }

    // 25→1, 26→2, 27→3 …
    public int PreviewPrestigeGain()
    {
        int finalRank = 0;
        if (girlFieldManager != null)
        {
            foreach (var g in girlFieldManager.girlList)
                if (g && g.Level >= topLevel)
                    finalRank = Math.Max(finalRank, g.Level - (topLevel - 1));
        }
        return Math.Max(0, finalRank);
    }

    public int  GetPrestigePoints() => prestigePoint;
    public bool SpendPrestigePoints(int amount)
    {
        if (amount <= 0) return true;
        if (prestigePoint < amount) return false;
        prestigePoint -= amount;
        return true;
    }

    public void DoPrestige()
    {
        if (!girlFieldManager || !economy) return;
        if (!HasAnyFinalGirl()) return;

        // 1) 환생 포인트 및 계승 XP
        int baseGain = PreviewPrestigeGain();
        double mulPPG = GetPrestigePointGainMul();
        int gain = Mathf.Max(0, Mathf.FloorToInt((float)(baseGain * mulPPG)));

        prestigePoint      += gain;
        totalPrestigeCount += 1;

        // LegacyRankManager.Instance?.AddXp(baseGain)  (리플렉션 안전 호출)
        try
        {
            var t = FindTypeByName("LegacyRankManager");
            var pi = t?.GetProperty("Instance", BindingFlags.Public|BindingFlags.Static);
            var inst = pi?.GetValue(null, null);
            var mi = t?.GetMethod("AddXp", BindingFlags.Public|BindingFlags.Instance);
            if (inst != null && mi != null) mi.Invoke(inst, new object[]{ baseGain });
        }
        catch { }

        // 2) 필드 비우기
        var snapshot = new List<GirlCharacter>(girlFieldManager.girlList);
        foreach (var g in snapshot)
            if (g != null) girlFieldManager.RemoveGirl(g);
        girlFieldManager.girlList.Clear();

        // 3) 경제/티어 리셋
        economy.SetGold(0);
        try // economy.ResetGoldUpgradesForPrestige();
        {
            var mi = economy.GetType().GetMethod("ResetGoldUpgradesForPrestige", BindingFlags.Public|BindingFlags.Instance);
            mi?.Invoke(economy, null);
        } catch { }

        try // tierManager.SwitchTo(0)
        {
            var mi = tierManager?.GetType().GetMethod("SwitchTo", BindingFlags.Public|BindingFlags.Instance);
            mi?.Invoke(tierManager, new object[]{ 0 });
        } catch { }

        // 4) 시작 자금 지급 = (Lv1 60초 수익) × (계승 시작자금 배수) × (상점 시작자금 배수)
        double base1Min = economy.GetLevelIncomePerSec(1) * 60.0;

        double mulRank = 1.0; // LegacyRankManager.GetStartGoldMultiplier()
        try
        {
            var t = FindTypeByName("LegacyRankManager");
            var pi = t?.GetProperty("Instance", BindingFlags.Public|BindingFlags.Static);
            var inst = pi?.GetValue(null, null);
            var mi = t?.GetMethod("GetStartGoldMultiplier", BindingFlags.Public|BindingFlags.Instance);
            if (inst != null && mi != null) mulRank = Convert.ToDouble(mi.Invoke(inst, null));
        }
        catch { }

        double mulShop  = GetStartGoldMultiplier();
        double startGold = base1Min * mulRank * mulShop;
        if (startGold > 0) economy.AddGold(startGold);

        SaveManager.Instance?.SaveGame();
    }

    // ─────────────────────────── 효과 쿼리(게임 적용) ───────────────────────────
    // A) 핵심 4종
    public double GetIncomeMultiplier()        => 1.0 + 0.08 * incomeLv;
    public float  GetTwoStepChance()           => Mathf.Min(0.50f, 0.025f * twoStepLv);
    public double GetStartGoldMultiplier()     => 1.0 + 0.10 * startGoldLv;
    public double GetPrestigePointGainMul()    => 1.0 + 0.10 * prestigeGainLv;

    // B) Plus 8종
    public int    GetManualSpawnMaxPlus()      => plusManualSpawnMaxLv;                      // + count
    public double GetManualSpawnIntervalMul()  => Math.Max(0.50, 1.0 - 0.05 * plusManualSpawnSpeedLv);
    public double GetAutoMergeIntervalMul()    => Math.Max(0.50, 1.0 - 0.03 * plusAutoMergeSpeedLv);
    public double GetAutoSpawnIntervalMul()    => Math.Max(0.50, 1.0 - 0.03 * plusAutoSpawnSpeedLv);
    public int    GetFieldMaxPlus()            => plusFieldMaxLv;                            // + count
    public double GetClickBonusMul()           => 1.0 + 0.05 * plusClickBonusLv;
    public double GetOfflineRewardMul()        => 1.0 + 0.05 * plusOfflineRewardLv;
    public int    GetOfflineMaxExtraSeconds()  => 600 * plusOfflineMaxTimeLv;                // 10분 * Lv

    // ───────────────────────────── 비용 & 구매 API ─────────────────────────────
    static int GrowthCost(int b, float g, int lv)
    {
        double cost = Math.Ceiling(b * Math.Pow(g, Mathf.Max(0, lv)));
        return Mathf.Max(1, (int)cost);
    }

    // 비용 조회 (핵심 4종)
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

    // 구매 API (핵심 4종)
    public bool TryBuyIncome()            => TrySpendAnd(ref incomeLv,           GetIncomeNextCost());
    public bool TryBuyTwoStep()           => TrySpendAnd(ref twoStepLv,          GetTwoStepNextCost());
    public bool TryBuyStartGold()         => TrySpendAnd(ref startGoldLv,        GetStartGoldNextCost());
    public bool TryBuyPrestigeGain()      => TrySpendAnd(ref prestigeGainLv,     GetPrestigeGainNextCost());

    // 구매 API (Plus)
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

    // ───────────────────────────── ISaveable ─────────────────────────────
    public void CollectSaveData(SaveData d)
    {
        // 기본
        d.prestigePoint      = prestigePoint;
        d.totalPrestigeCount = totalPrestigeCount;

        // 상점 레벨(리플렉션로 저장, SaveData v2 호환)
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

        incomeLv            = TryGetInt(d, "prestigeShopIncomeLv",        incomeLv);
        twoStepLv           = TryGetInt(d, "prestigeShopTwoStepLv",       twoStepLv);
        startGoldLv         = TryGetInt(d, "prestigeShopStartGoldLv",     startGoldLv);
        prestigeGainLv      = TryGetInt(d, "prestigeShopPrestigeGainLv",  prestigeGainLv);

        plusManualSpawnMaxLv= TryGetInt(d, "ppManualSpawnMaxLv",          plusManualSpawnMaxLv);
        plusManualSpawnSpeedLv=TryGetInt(d, "ppManualSpawnSpeedLv",       plusManualSpawnSpeedLv);
        plusAutoMergeSpeedLv= TryGetInt(d, "ppAutoMergeLv",               plusAutoMergeSpeedLv);
        plusAutoSpawnSpeedLv= TryGetInt(d, "ppAutoSpawnLv",               plusAutoSpawnSpeedLv);
        plusFieldMaxLv      = TryGetInt(d, "ppMaxFieldCountLv",           plusFieldMaxLv);
        plusClickBonusLv    = TryGetInt(d, "ppClickBonusLv",              plusClickBonusLv);
        plusOfflineRewardLv = TryGetInt(d, "ppOfflineRewardLv",           plusOfflineRewardLv);
        plusOfflineMaxTimeLv= TryGetInt(d, "ppOfflineMaxTimeLv",          plusOfflineMaxTimeLv);
    }
}

// ============================================================================
// PrestigeShopManager (호환 페이사드)
// - MonoBehaviour 아님. 기존 코드가 리플렉션으로 PrestigeShopManager.Instance 를 찾을 때
//   이 클래스를 반환하여 호출을 "PrestigeManager"로 위임한다.
// - 일부 UI(예: 예전 PrestigeShopPanelController)가 비공개 필드(incomeLv 등)를
//   리플렉션으로 읽는 경우가 있어, 동명 필드를 보유하고 Instance 반환 시점에 동기화한다.
// ============================================================================
public sealed class PrestigeShopManager
{
    private static PrestigeShopManager _instance;
    public static PrestigeShopManager Instance
    {
        get
        {
            if (_instance == null) _instance = new PrestigeShopManager();
            _instance.SyncLevelsFromCore();
            return _instance;
        }
    }

    // ── 예전 패널 호환용(리플렉션용) 필드: 매번 Instance 접근 시 최신값으로 동기화 ──
    // (주의: 진짜 저장소는 PrestigeManager 내부; 이 필드는 UI 리플렉션을 위한 미러임)
    private int incomeLv, twoStepLv, startGoldLv, prestigeGainLv;
    private int plusManualSpawnMaxLv, plusManualSpawnSpeedLv, plusAutoMergeSpeedLv, plusAutoSpawnSpeedLv;
    private int plusFieldMaxLv, plusClickBonusLv, plusOfflineRewardLv, plusOfflineMaxTimeLv;

    private PrestigeManager Core => PrestigeManager.Instance;

    private void SyncLevelsFromCore()
    {
        var c = Core;
        if (c == null) return;

        // 리플렉션으로 Core의 비공개 필드 값을 읽어 미러링(필드명 동일)
        void Pull(string name, ref int dst)
        {
            try
            {
                var fi = typeof(PrestigeManager).GetField(name, BindingFlags.NonPublic|BindingFlags.Instance);
                if (fi != null && fi.FieldType == typeof(int)) dst = (int)fi.GetValue(c);
            } catch {}
        }

        Pull("incomeLv", ref incomeLv);
        Pull("twoStepLv", ref twoStepLv);
        Pull("startGoldLv", ref startGoldLv);
        Pull("prestigeGainLv", ref prestigeGainLv);

        Pull("plusManualSpawnMaxLv", ref plusManualSpawnMaxLv);
        Pull("plusManualSpawnSpeedLv", ref plusManualSpawnSpeedLv);
        Pull("plusAutoMergeSpeedLv", ref plusAutoMergeSpeedLv);
        Pull("plusAutoSpawnSpeedLv", ref plusAutoSpawnSpeedLv);
        Pull("plusFieldMaxLv", ref plusFieldMaxLv);
        Pull("plusClickBonusLv", ref plusClickBonusLv);
        Pull("plusOfflineRewardLv", ref plusOfflineRewardLv);
        Pull("plusOfflineMaxTimeLv", ref plusOfflineMaxTimeLv);
    }

    // ── 게임 적용용 Getter 위임 ──
    public double GetIncomeMultiplier()        => Core ? Core.GetIncomeMultiplier()       : 1.0;
    public float  GetTwoStepChance()           => Core ? Core.GetTwoStepChance()          : 0f;
    public double GetStartGoldMultiplier()     => Core ? Core.GetStartGoldMultiplier()    : 1.0;
    public double GetPrestigePointGainMul()    => Core ? Core.GetPrestigePointGainMul()   : 1.0;

    public int    GetManualSpawnMaxPlus()      => Core ? Core.GetManualSpawnMaxPlus()     : 0;
    public double GetManualSpawnIntervalMul()  => Core ? Core.GetManualSpawnIntervalMul() : 1.0;
    public double GetAutoMergeIntervalMul()    => Core ? Core.GetAutoMergeIntervalMul()   : 1.0;
    public double GetAutoSpawnIntervalMul()    => Core ? Core.GetAutoSpawnIntervalMul()   : 1.0;
    public int    GetFieldMaxPlus()            => Core ? Core.GetFieldMaxPlus()           : 0;
    public double GetClickBonusMul()           => Core ? Core.GetClickBonusMul()          : 1.0;
    public double GetOfflineRewardMul()        => Core ? Core.GetOfflineRewardMul()       : 1.0;
    public int    GetOfflineMaxExtraSeconds()  => Core ? Core.GetOfflineMaxExtraSeconds() : 0;

    // ── 비용/구매 위임 ──
    public int  GetIncomeNextCost()            => Core ? Core.GetIncomeNextCost()            : int.MaxValue;
    public int  GetTwoStepNextCost()           => Core ? Core.GetTwoStepNextCost()           : int.MaxValue;
    public int  GetStartGoldNextCost()         => Core ? Core.GetStartGoldNextCost()         : int.MaxValue;
    public int  GetPrestigeGainNextCost()      => Core ? Core.GetPrestigeGainNextCost()      : int.MaxValue;

    public int  GetPlusManualSpawnMaxCost()    => Core ? Core.GetPlusManualSpawnMaxCost()    : int.MaxValue;
    public int  GetPlusManualSpeedCost()       => Core ? Core.GetPlusManualSpeedCost()       : int.MaxValue;
    public int  GetPlusAutoMergeCost()         => Core ? Core.GetPlusAutoMergeCost()         : int.MaxValue;
    public int  GetPlusAutoSpawnCost()         => Core ? Core.GetPlusAutoSpawnCost()         : int.MaxValue;
    public int  GetPlusFieldMaxCost()          => Core ? Core.GetPlusFieldMaxCost()          : int.MaxValue;
    public int  GetPlusClickBonusCost()        => Core ? Core.GetPlusClickBonusCost()        : int.MaxValue;
    public int  GetPlusOfflineRewardCost()     => Core ? Core.GetPlusOfflineRewardCost()     : int.MaxValue;
    public int  GetPlusOfflineMaxTimeCost()    => Core ? Core.GetPlusOfflineMaxTimeCost()    : int.MaxValue;

    public bool TryBuyIncome()                 { var ok = Core && Core.TryBuyIncome();              if (ok) SyncLevelsFromCore(); return ok; }
    public bool TryBuyTwoStep()                { var ok = Core && Core.TryBuyTwoStep();             if (ok) SyncLevelsFromCore(); return ok; }
    public bool TryBuyStartGold()              { var ok = Core && Core.TryBuyStartGold();           if (ok) SyncLevelsFromCore(); return ok; }
    public bool TryBuyPrestigeGain()           { var ok = Core && Core.TryBuyPrestigeGain();        if (ok) SyncLevelsFromCore(); return ok; }

    public bool TryBuyPlusManualSpawnMax()     { var ok = Core && Core.TryBuyPlusManualSpawnMax();  if (ok) SyncLevelsFromCore(); return ok; }
    public bool TryBuyPlusManualSpeed()        { var ok = Core && Core.TryBuyPlusManualSpeed();     if (ok) SyncLevelsFromCore(); return ok; }
    public bool TryBuyPlusAutoMerge()          { var ok = Core && Core.TryBuyPlusAutoMerge();       if (ok) SyncLevelsFromCore(); return ok; }
    public bool TryBuyPlusAutoSpawn()          { var ok = Core && Core.TryBuyPlusAutoSpawn();       if (ok) SyncLevelsFromCore(); return ok; }
    public bool TryBuyPlusFieldMax()           { var ok = Core && Core.TryBuyPlusFieldMax();        if (ok) SyncLevelsFromCore(); return ok; }
    public bool TryBuyPlusClickBonus()         { var ok = Core && Core.TryBuyPlusClickBonus();      if (ok) SyncLevelsFromCore(); return ok; }
    public bool TryBuyPlusOfflineReward()      { var ok = Core && Core.TryBuyPlusOfflineReward();   if (ok) SyncLevelsFromCore(); return ok; }
    public bool TryBuyPlusOfflineMaxTime()     { var ok = Core && Core.TryBuyPlusOfflineMaxTime();  if (ok) SyncLevelsFromCore(); return ok; }
}
