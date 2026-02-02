using UnityEngine;

[DisallowMultipleComponent]
public class PrestigeShopManager : MonoBehaviour
{
    public static PrestigeShopManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private PrestigeManager prestige;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (!prestige) prestige = FindObjectOfType<PrestigeManager>(true);
    }

    // ───────── Core Multipliers ─────────
    public double GetIncomeMultiplier()     => prestige ? prestige.GetIncomeMultiplier()     : 1.0;
    public float  GetTwoStepChance()        => prestige ? prestige.GetTwoStepChance()        : 0f;
    public double GetStartGoldMultiplier()  => prestige ? prestige.GetStartGoldMultiplier()  : 1.0;
    public double GetPrestigePointGainMul() => prestige ? prestige.GetPrestigePointGainMul() : 1.0;

    // ───────── Plus Effects ─────────
    public int    GetManualSpawnMaxPlus()      => prestige ? prestige.GetManualSpawnMaxPlus()     : 0;
    public double GetManualSpawnIntervalMul()  => prestige ? prestige.GetManualSpawnIntervalMul() : 1.0;
    public double GetAutoMergeIntervalMul()    => prestige ? prestige.GetAutoMergeIntervalMul()   : 1.0;
    public double GetAutoSpawnIntervalMul()    => prestige ? prestige.GetAutoSpawnIntervalMul()   : 1.0;
    public int    GetFieldMaxPlus()            => prestige ? prestige.GetFieldMaxPlus()           : 0;
    public double GetClickBonusMul()           => prestige ? prestige.GetClickBonusMul()          : 1.0;
    public double GetOfflineRewardMul()        => prestige ? prestige.GetOfflineRewardMul()       : 1.0;
    public int    GetOfflineMaxExtraSeconds()  => prestige ? prestige.GetOfflineMaxExtraSeconds() : 0;
}
