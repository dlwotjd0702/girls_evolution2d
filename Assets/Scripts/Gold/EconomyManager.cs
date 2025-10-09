using System;
using System.Collections.Generic;
using UnityEngine;

public class EconomyManager : MonoBehaviour, ISaveable
{
    [Header("Gold")]
    [SerializeField] private double gold = 0;
    public event Action<double> OnGoldChanged;

    [Header("Idle Income")]
    [SerializeField] private bool   idleEnabled        = true;
    [SerializeField] private double idlePerSecondBase  = 0;
    [SerializeField] private double idleMultiplier     = 1.0;
    private float idleTimer = 0f;

    [Header("Click Income")]
    [SerializeField] private double clickBase       = 1;
    [SerializeField] private double clickMultiplier = 1.0;

    [Header("Spawn/Field Base")]
    [SerializeField] private int   manualSpawnMaxBase      = 3;
    [SerializeField] private float manualSpawnIntervalBase = 10f;
    [SerializeField] private int   fieldMaxCountBase       = 8;

    [Header("Spawn/Field (Derived Runtime)")]
    [SerializeField] private int   manualSpawnMax      = 3;
    [SerializeField] private float manualSpawnInterval = 10f;
    [SerializeField] private int   fieldMaxCount       = 8;

    [Header("Upgrades (levels)")]
    [SerializeField] private int manualSpawnMaxUpgrade   = 0;   // +1 당 수동 차지 최대 +1
    [SerializeField] private int manualSpawnSpeedUpgrade = 0;   // +1 당 쿨다운 0.9배
    [SerializeField] private int maxFieldCountUpgrade    = 0;   // +1 당 필드 슬롯 +2
    [SerializeField] private int clickBonusUpgrade       = 0;   // +1 당 클릭 x1.2

    public event Action OnUpgradeChanged;

    [Header("Shop Costs (기본 업그레이드)")]
    [SerializeField] private double spawnMaxBaseCost      = 100;
    [SerializeField] private double spawnMaxCostGrowth    = 2.5;
    [SerializeField] private double spawnSpeedBaseCost    = 150;
    [SerializeField] private double spawnSpeedCostGrowth  = 2.6;
    [SerializeField] private double fieldMaxBaseCost      = 250;
    [SerializeField] private double fieldMaxCostGrowth    = 2.8;

    [Header("Summon Price Growth")]
    [SerializeField] private double summonPerLevelGrowth    = 1.15; // 레벨 증가 배수
    [SerializeField] private double summonPerPurchaseGrowth = 1.07; // 동일 레벨 반복 구매 배수
    private readonly Dictionary<int,int> summonBuyCounts = new();   // level → count

    void Awake()
    {
        RecomputeDerived();
        OnUpgradeChanged?.Invoke();
    }

    void Update()
    {
        if (!idleEnabled) return;
        idleTimer += Time.unscaledDeltaTime;
        if (idleTimer >= 1f)
        {
            idleTimer -= 1f;
            var amt = idlePerSecondBase * idleMultiplier;
            if (amt > 0) AddGold(amt);
        }
    }

    // ── Gold API ──
    public double GetGold() => gold;
    public void   SetGold(double v) { gold = Math.Max(0, v); OnGoldChanged?.Invoke(gold); }
    public void   AddGold(double amount) { gold += amount; OnGoldChanged?.Invoke(gold); }
    public bool   SpendGold(double cost)
    {
        if (gold < cost) return false;
        gold -= cost; OnGoldChanged?.Invoke(gold); return true;
    }

    // ── Click/Idle config ──
    public void SetIdle(double basePerSec, double mult = 1.0) { idlePerSecondBase = basePerSec; idleMultiplier = mult; }
    public void SetClick(double baseClick, double mult = 1.0) { clickBase = baseClick; clickMultiplier = mult; }
    public void AddClickGold(double baseAmount = -1)
    {
        var amt = (baseAmount >= 0 ? baseAmount : clickBase) * clickMultiplier;
        if (amt > 0) AddGold(amt);
    }

    // ── Spawn/Field 조회 ──
    public int   GetMaxManualSpawnCount() => manualSpawnMax;
    public float GetManualSpawnInterval() => manualSpawnInterval;
    public int   GetMaxFieldCount()       => fieldMaxCount;

    // ── 기본 업그레이드 비용 ──
    public double GetSpawnMaxUpgradeCost(int nextLevel)   => spawnMaxBaseCost   * Math.Pow(spawnMaxCostGrowth,   nextLevel);
    public double GetSpawnSpeedUpgradeCost(int nextLevel) => spawnSpeedBaseCost * Math.Pow(spawnSpeedCostGrowth, nextLevel);
    public double GetFieldMaxUpgradeCost(int nextLevel)   => fieldMaxBaseCost   * Math.Pow(fieldMaxCostGrowth,   nextLevel);

    // ── 기본 업그레이드 구매 ──
    public bool TryBuySpawnMaxUpgrade()
    {
        double cost = GetSpawnMaxUpgradeCost(manualSpawnMaxUpgrade);
        if (!SpendGold(cost)) return false;
        manualSpawnMaxUpgrade++;
        RecomputeDerived(); OnUpgradeChanged?.Invoke(); return true;
    }
    public bool TryBuySpawnSpeedUpgrade()
    {
        double cost = GetSpawnSpeedUpgradeCost(manualSpawnSpeedUpgrade);
        if (!SpendGold(cost)) return false;
        manualSpawnSpeedUpgrade++;
        RecomputeDerived(); OnUpgradeChanged?.Invoke(); return true;
    }
    public bool TryBuyFieldMaxUpgrade()
    {
        double cost = GetFieldMaxUpgradeCost(maxFieldCountUpgrade);
        if (!SpendGold(cost)) return false;
        maxFieldCountUpgrade++;
        RecomputeDerived(); OnUpgradeChanged?.Invoke(); return true;
    }

    // ── 파생치 재계산 ──
    void RecomputeDerived()
    {
        manualSpawnMax      = manualSpawnMaxBase + manualSpawnMaxUpgrade;
        manualSpawnInterval = Mathf.Clamp(manualSpawnIntervalBase * Mathf.Pow(0.9f, manualSpawnSpeedUpgrade), 0.5f, 999f);
        fieldMaxCount       = fieldMaxCountBase + 2 * maxFieldCountUpgrade;
        clickMultiplier     = 1.0 + 0.2 * clickBonusUpgrade;
    }

    // ── 소환 동적 가격(레벨/구매누적) ──
    // SummonPanelController가 리플렉션으로 이 시그니처를 찾음.
    public double GetSummonCost(int level, double baseCost)
    {
        int lv = Mathf.Clamp(level, 1, TierRules.MaxLevel);
        int bought = summonBuyCounts.TryGetValue(lv, out var c) ? c : 0;
        double levelMul = Math.Pow(summonPerLevelGrowth,    Math.Max(0, lv - 1));
        double buyMul   = Math.Pow(summonPerPurchaseGrowth, Math.Max(0, bought));
        return baseCost * levelMul * buyMul;
    }

    public void RecordSummonPurchase(int level)
    {
        int lv = Mathf.Clamp(level, 1, TierRules.MaxLevel);
        if (!summonBuyCounts.ContainsKey(lv)) summonBuyCounts[lv] = 0;
        summonBuyCounts[lv]++;
    }

    // ── ISaveable ──
    public void CollectSaveData(SaveData data)
    {
        data.gold = gold;

        data.manualSpawnMaxUpgrade   = Math.Max(0, manualSpawnMaxUpgrade);
        data.manualSpawnSpeedUpgrade = Math.Max(0, manualSpawnSpeedUpgrade);
        data.maxFieldCountUpgrade    = Math.Max(0, maxFieldCountUpgrade);
        data.clickBonusUpgrade       = Math.Max(0, clickBonusUpgrade);

        // summon buy counts
        data.summonBuyCounts = new int[TierRules.MaxLevel];
        for (int lv = 1; lv <= TierRules.MaxLevel; lv++)
            data.summonBuyCounts[lv - 1] = summonBuyCounts.TryGetValue(lv, out var c) ? c : 0;
    }

    public void ApplyLoadedData(SaveData data)
    {
        SetGold(data.gold);

        manualSpawnMaxUpgrade   = Math.Max(0, data.manualSpawnMaxUpgrade);
        manualSpawnSpeedUpgrade = Math.Max(0, data.manualSpawnSpeedUpgrade);
        maxFieldCountUpgrade    = Math.Max(0, data.maxFieldCountUpgrade);
        clickBonusUpgrade       = Math.Max(0, data.clickBonusUpgrade);

        // summon buy counts
        summonBuyCounts.Clear();
        if (data.summonBuyCounts != null)
        {
            int n = Math.Min(data.summonBuyCounts.Length, TierRules.MaxLevel);
            for (int i = 0; i < n; i++)
                if (data.summonBuyCounts[i] > 0) summonBuyCounts[i + 1] = data.summonBuyCounts[i];
        }

        RecomputeDerived();
        OnUpgradeChanged?.Invoke();
    }
}
