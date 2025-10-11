using System;
using UnityEngine;

public class EconomyManager : MonoBehaviour, ISaveable
{
    public event Action<double> OnGoldChanged;
    public event Action         OnUpgradeChanged;

    [SerializeField] private double gold = 0;
    public double GetGold() => gold;
    public void   SetGold(double amount){ gold = Math.Max(0, amount); OnGoldChanged?.Invoke(gold); }
    public bool SpendGold(double amount){ if (amount<=0) return true; if (gold+1e-9<amount) return false; gold-=amount; OnGoldChanged?.Invoke(gold); return true; }
    public void AddGold(double amount){ if (amount<=0) return; gold+=amount; OnGoldChanged?.Invoke(gold); }

    // ───────────────── Base ─────────────────
    [Header("Spawn/Field Base")]
    [SerializeField] private int   baseManualSpawnMax = 3;
    [SerializeField] private float baseManualSpawnInterval = 10f;
    [SerializeField] private int   baseFieldCount = 8;

    [Header("Costs (non-auto)")]
    [SerializeField] private double spawnMaxCostBase   = 100;
    [SerializeField] private double spawnMaxCostGrow   = 1.8;
    [SerializeField] private double spawnSpeedCostBase = 120;
    [SerializeField] private double spawnSpeedCostGrow = 1.9;
    [SerializeField] private double fieldMaxCostBase   = 150;
    [SerializeField] private double fieldMaxCostGrow   = 1.85;
    [SerializeField] private double clickBonusCostBase = 100;
    [SerializeField] private double clickBonusCostGrow = 1.9;

    [Header("Click Bonus")]
    [SerializeField] private double clickBonusPerLevel = 0.2; // x(1+0.2*Lv)

    [Header("Automation (AutoMerge/AutoSpawn)")]
    [SerializeField] private float autoMergeBaseInterval = 5f;   // Lv1 기준
    [SerializeField] private float autoSpawnBaseInterval = 6f;   // Lv1 기준
    [SerializeField] private float autoPerLevelMul       = 0.9f; // 레벨당 -10%
    [SerializeField] private float autoIntervalFloor     = 0.4f; // 최소 제한

    [Header("Costs (auto)")]
    [SerializeField] private double autoMergeBuyCostBase   = 500; // Lv0→1
    [SerializeField] private double autoMergeUpgradeGrow   = 2.0; // Lv1→2부터
    [SerializeField] private double autoSpawnBuyCostBase   = 600;
    [SerializeField] private double autoSpawnUpgradeGrow   = 2.0;

    [Header("Offline Rewards")]
    [SerializeField] private double offlineRewardBaseMul   = 1.0; // x(1 + 0.25*Lv)
    [SerializeField] private double offlineRewardPerLevel  = 0.25;
    [SerializeField] private double offlineMaxHoursBase    = 2.0;
    [SerializeField] private double offlineMaxHoursPerLv   = 0.5;

    [Header("Costs (offline/meta)")]
    [SerializeField] private double offlineRewardCostBase  = 200;
    [SerializeField] private double offlineRewardCostGrow  = 2.0;
    [SerializeField] private double offlineMaxTimeCostBase = 220;
    [SerializeField] private double offlineMaxTimeCostGrow = 2.0;

    // ─────────────── Caps ───────────────
    [Header("Level Caps")]
    [SerializeField] private int spawnMaxUpgradeCap     = 10;
    [SerializeField] private int spawnSpeedUpgradeCap   = 10;
    [SerializeField] private int fieldMaxUpgradeCap     = 10;
    [SerializeField] private int clickBonusUpgradeCap   = 15;
    [SerializeField] private int offlineRewardCap       = 10;
    [SerializeField] private int offlineMaxTimeCap      = 10;
    [SerializeField] private int autoMergeCap           = 10; // 0=미구매, 1..Cap
    [SerializeField] private int autoSpawnCap           = 10;

    // 레벨들(세이브 대상)
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

    readonly int[] summonPurchaseCounts = new int[25];

    // ─────────────── Getters ───────────────
    public int    GetMaxManualSpawnCount() => baseManualSpawnMax + manualSpawnMaxUpgrade;
    public float  GetManualSpawnInterval() => baseManualSpawnInterval * Mathf.Pow(0.9f, manualSpawnSpeedUpgrade);
    public int    GetMaxFieldCount()       => baseFieldCount + maxFieldCountUpgrade * 2;
    public double GetClickBonusMultiplier()=> 1.0 + clickBonusUpgrade * clickBonusPerLevel;

    public bool   IsAutoMergeOn() => autoMergeOn && autoMergeUpgrade > 0;
    public bool   IsAutoSpawnOn() => autoSpawnOn && autoSpawnUpgrade > 0;
    public void   SetAutoMergeOn(bool on){ autoMergeOn = on; OnUpgradeChanged?.Invoke(); }
    public void   SetAutoSpawnOn(bool on){ autoSpawnOn = on; OnUpgradeChanged?.Invoke(); }

    public float  GetAutoMergeInterval(){ if(!IsAutoMergeOn()) return float.MaxValue; int lv=Mathf.Max(1,autoMergeUpgrade); float iv=autoMergeBaseInterval*Mathf.Pow(autoPerLevelMul, lv-1); return Mathf.Max(autoIntervalFloor, iv); }
    public float  GetAutoSpawnInterval(){ if(!IsAutoSpawnOn()) return float.MaxValue; int lv=Mathf.Max(1,autoSpawnUpgrade); float iv=autoSpawnBaseInterval*Mathf.Pow(autoPerLevelMul, lv-1); return Mathf.Max(autoIntervalFloor, iv); }

    public double GetOfflineRewardMultiplier() => offlineRewardBaseMul + (offlineRewardUpgrade * offlineRewardPerLevel);
    public double GetOfflineMaxSeconds()       => (offlineMaxHoursBase + offlineMaxTimeUpgrade * offlineMaxHoursPerLv) * 3600.0;

    // 레벨/캡 Getter (UI에서 사용)
    public int GetSpawnMaxUpgradeLevel()     => manualSpawnMaxUpgrade;
    public int GetSpawnSpeedUpgradeLevel()   => manualSpawnSpeedUpgrade;
    public int GetFieldMaxUpgradeLevel()     => maxFieldCountUpgrade;
    public int GetClickBonusUpgradeLevel()   => clickBonusUpgrade;
    public int GetOfflineRewardUpgradeLevel()=> offlineRewardUpgrade;
    public int GetOfflineMaxTimeUpgradeLevel()=> offlineMaxTimeUpgrade;
    public int GetAutoMergeUpgradeLevel()    => autoMergeUpgrade;
    public int GetAutoSpawnUpgradeLevel()    => autoSpawnUpgrade;

    public int GetSpawnMaxUpgradeCap()       => spawnMaxUpgradeCap;
    public int GetSpawnSpeedUpgradeCap()     => spawnSpeedUpgradeCap;
    public int GetFieldMaxUpgradeCap()       => fieldMaxUpgradeCap;
    public int GetClickBonusUpgradeCap()     => clickBonusUpgradeCap;
    public int GetOfflineRewardCap()         => offlineRewardCap;
    public int GetOfflineMaxTimeCap()        => offlineMaxTimeCap;
    public int GetAutoMergeCap()             => autoMergeCap;
    public int GetAutoSpawnCap()             => autoSpawnCap;

    // ─────────────── Costs (non-auto) ───────────────
    public double GetSpawnMaxUpgradeCost(int lv)   => (lv >= spawnMaxUpgradeCap)   ? double.PositiveInfinity : spawnMaxCostBase   * Math.Pow(spawnMaxCostGrow,   lv);
    public double GetSpawnSpeedUpgradeCost(int lv) => (lv >= spawnSpeedUpgradeCap) ? double.PositiveInfinity : spawnSpeedCostBase * Math.Pow(spawnSpeedCostGrow, lv);
    public double GetFieldMaxUpgradeCost(int lv)   => (lv >= fieldMaxUpgradeCap)   ? double.PositiveInfinity : fieldMaxCostBase   * Math.Pow(fieldMaxCostGrow,   lv);
    public double GetClickBonusUpgradeCost(int lv) => (lv >= clickBonusUpgradeCap) ? double.PositiveInfinity : clickBonusCostBase * Math.Pow(clickBonusCostGrow, lv);

    public bool TryBuySpawnMaxUpgrade()   { if (manualSpawnMaxUpgrade >= spawnMaxUpgradeCap) return false; double c=GetSpawnMaxUpgradeCost(manualSpawnMaxUpgrade);   if(!SpendGold(c)) return false; manualSpawnMaxUpgrade++;   OnUpgradeChanged?.Invoke(); return true; }
    public bool TryBuySpawnSpeedUpgrade() { if (manualSpawnSpeedUpgrade >= spawnSpeedUpgradeCap) return false; double c=GetSpawnSpeedUpgradeCost(manualSpawnSpeedUpgrade); if(!SpendGold(c)) return false; manualSpawnSpeedUpgrade++; OnUpgradeChanged?.Invoke(); return true; }
    public bool TryBuyFieldMaxUpgrade()   { if (maxFieldCountUpgrade >= fieldMaxUpgradeCap) return false; double c=GetFieldMaxUpgradeCost(maxFieldCountUpgrade);    if(!SpendGold(c)) return false; maxFieldCountUpgrade++;    OnUpgradeChanged?.Invoke(); return true; }
    public bool TryBuyClickBonusUpgrade() { if (clickBonusUpgrade >= clickBonusUpgradeCap) return false; double c=GetClickBonusUpgradeCost(clickBonusUpgrade);     if(!SpendGold(c)) return false; clickBonusUpgrade++;       OnUpgradeChanged?.Invoke(); return true; }

    // ─────────────── Costs (auto) ───────────────
    public double GetAutoMergeNextCost()
    {
        if (autoMergeUpgrade >= autoMergeCap) return double.PositiveInfinity;
        if (autoMergeUpgrade <= 0) return autoMergeBuyCostBase;
        int upLv = autoMergeUpgrade;
        return autoMergeBuyCostBase * Math.Pow(autoMergeUpgradeGrow, upLv);
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
        if (autoSpawnUpgrade <= 0) return autoSpawnBuyCostBase;
        int upLv = autoSpawnUpgrade;
        return autoSpawnBuyCostBase * Math.Pow(autoSpawnUpgradeGrow, upLv);
    }
    public bool   TryBuyAutoSpawnUpgrade()
    {
        if (autoSpawnUpgrade >= autoSpawnCap) return false;
        double c=GetAutoSpawnNextCost(); if(!SpendGold(c)) return false;
        autoSpawnUpgrade = Mathf.Max(0,autoSpawnUpgrade)+1; OnUpgradeChanged?.Invoke(); return true;
    }

    // ─────────────── Costs (offline/meta) ───────────────
    public double GetOfflineRewardUpgradeCost(int lv)  => (lv >= offlineRewardCap) ? double.PositiveInfinity : offlineRewardCostBase  * Math.Pow(offlineRewardCostGrow,  lv);
    public double GetOfflineMaxTimeUpgradeCost(int lv) => (lv >= offlineMaxTimeCap)? double.PositiveInfinity : offlineMaxTimeCostBase * Math.Pow(offlineMaxTimeCostGrow, lv);

    public bool   TryBuyOfflineRewardUpgrade(){ if(offlineRewardUpgrade>=offlineRewardCap) return false; double c=GetOfflineRewardUpgradeCost(offlineRewardUpgrade);  if(!SpendGold(c)) return false; offlineRewardUpgrade++;  OnUpgradeChanged?.Invoke(); return true; }
    public bool   TryBuyOfflineMaxTimeUpgrade(){ if(offlineMaxTimeUpgrade>=offlineMaxTimeCap) return false; double c=GetOfflineMaxTimeUpgradeCost(offlineMaxTimeUpgrade); if(!SpendGold(c)) return false; offlineMaxTimeUpgrade++; OnUpgradeChanged?.Invoke(); return true; }

    // ─────────────── Summon escalation ───────────────
    public double GetSummonCost(int level, double baseCost)
    {
        int idx=Mathf.Clamp(level-1,0,24);
        int bought=summonPurchaseCounts[idx];
        double byLevel = Math.Pow(1.15, Mathf.Max(0, level-1));
        double byBuy   = Math.Pow(1.08, Mathf.Max(0, bought));
        return baseCost*byLevel*byBuy;
    }
    public void RecordSummonPurchase(int level){ int idx=Mathf.Clamp(level-1,0,24); summonPurchaseCounts[idx]=Mathf.Clamp(summonPurchaseCounts[idx]+1,0,int.MaxValue); OnUpgradeChanged?.Invoke(); }

    // ─────────────── Save / Load ───────────────
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

        OnGoldChanged?.Invoke(gold); OnUpgradeChanged?.Invoke();
    }

    // ─────────────── Prestige Reset ───────────────
    public void ResetGoldUpgradesForPrestige()
    {
        manualSpawnMaxUpgrade=0; manualSpawnSpeedUpgrade=0; maxFieldCountUpgrade=0; clickBonusUpgrade=0;
        autoMergeUpgrade=0; autoSpawnUpgrade=0; autoMergeOn=false; autoSpawnOn=false;
        offlineRewardUpgrade=0; offlineMaxTimeUpgrade=0;
        Array.Clear(summonPurchaseCounts,0,summonPurchaseCounts.Length);
        OnUpgradeChanged?.Invoke();
    }
}
