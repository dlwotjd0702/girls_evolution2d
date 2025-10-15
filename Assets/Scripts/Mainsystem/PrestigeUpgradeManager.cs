// PrestigeUpgradeManager.cs (NEW)
using System;
using UnityEngine;

public enum PrestigeUpgradeSlot
{
    IncomeBoost = 0,
    DoubleMergeChance = 1,
    ManualChargeSpeed = 2,
    AutoMergeInterval = 3,
    OfflineCap = 4,
    StartBoost = 5,
}

[DisallowMultipleComponent]
public class PrestigeUpgradeManager : MonoBehaviour
{
    [Header("Refs")]
    public PrestigeManager prestige;   // 환생 포인트 소모/조회 주체

    // 레벨 보관(저장 연동은 이후 SaveData v3에서 처리)
    [SerializeField] private int incomeLv = 0;
    [SerializeField] private int doubleMergeLv = 0;
    [SerializeField] private int manualChargeLv = 0;
    [SerializeField] private int autoMergeLv = 0;
    [SerializeField] private int offlineCapLv = 0;
    [SerializeField] private int startBoostLv = 0;

    public event Action OnLevelsChanged;

    // ───── 조회 API ─────
    public int  GetLevel(PrestigeUpgradeSlot s) => s switch {
        PrestigeUpgradeSlot.IncomeBoost        => incomeLv,
        PrestigeUpgradeSlot.DoubleMergeChance  => doubleMergeLv,
        PrestigeUpgradeSlot.ManualChargeSpeed  => manualChargeLv,
        PrestigeUpgradeSlot.AutoMergeInterval  => autoMergeLv,
        PrestigeUpgradeSlot.OfflineCap         => offlineCapLv,
        PrestigeUpgradeSlot.StartBoost         => startBoostLv,
        _ => 0
    };

    public int GetNextCost(PrestigeUpgradeSlot s)
    {
        int lv = GetLevel(s);
        var (baseCost, growth) = GetCostParams(s);
        // growth^lv 를 올림해 정수 비용화
        double cost = Math.Ceiling(baseCost * Math.Pow(growth, lv));
        return Mathf.Clamp((int)cost, 1, int.MaxValue);
    }

    // ───── 효과 계산기 (밸런스 규칙) ─────
    public double GetIncomeMultiplier()                => 1.0 + 0.08 * incomeLv;                 // +8%/Lv (L≤25 권장)
    public double GetDoubleMergeChance()               => Math.Min(0.50, 0.025 * doubleMergeLv);  // +2.5%/Lv (cap 50%)
    public double GetManualChargeRateMultiplier()      => 1.0 + 0.05 * manualChargeLv;            // +5%/Lv
    public double GetAutoMergeIntervalMultiplier()     => Math.Max(0.50, 1.0 - 0.03 * autoMergeLv);// -3%/Lv (floor 50%)
    public TimeSpan GetOfflineCapExtra()               => TimeSpan.FromMinutes(10 * offlineCapLv);// +10min/Lv
    public (int freeSpawnLevel, double startGoldScale) GetStartBoost()
    {
        // L<10: Lv1 무료 1회 / L≥10: Lv2 무료 1회
        int freeSpawnLv = (startBoostLv >= 10) ? 2 : 1;
        // BestAPS * 60초 * (1 + 0.1 * L) → APS는 PrestigeManager가 제공
        double k = 60.0 * (1.0 + 0.1 * startBoostLv);
        return (freeSpawnLv, k);
    }

    // ───── 구매 ─────
    public bool TryPurchase(PrestigeUpgradeSlot s)
    {
        if (!prestige) return false;
        int cost = GetNextCost(s);
        if (!prestige.TryConsumePrestigePoints(cost)) return false;

        switch (s)
        {
            case PrestigeUpgradeSlot.IncomeBoost:        incomeLv++; break;
            case PrestigeUpgradeSlot.DoubleMergeChance:  doubleMergeLv++; break;
            case PrestigeUpgradeSlot.ManualChargeSpeed:  manualChargeLv++; break;
            case PrestigeUpgradeSlot.AutoMergeInterval:  autoMergeLv++; break;
            case PrestigeUpgradeSlot.OfflineCap:         offlineCapLv++; break;
            case PrestigeUpgradeSlot.StartBoost:         startBoostLv++; break;
        }
        OnLevelsChanged?.Invoke();
        // 저장 연동은 후속 단계에서 SaveManager.SaveGame()과 함께 붙임
        return true;
    }

    // 코스트 파라미터 표
    private (int baseCost, double growth) GetCostParams(PrestigeUpgradeSlot s) => s switch {
        PrestigeUpgradeSlot.IncomeBoost        => (3, 1.35),
        PrestigeUpgradeSlot.DoubleMergeChance  => (4, 1.45),
        PrestigeUpgradeSlot.ManualChargeSpeed  => (2, 1.25),
        PrestigeUpgradeSlot.AutoMergeInterval  => (2, 1.35),
        PrestigeUpgradeSlot.OfflineCap         => (1, 1.25),
        PrestigeUpgradeSlot.StartBoost         => (3, 1.30),
        _ => (1, 1.30)
    };
}
