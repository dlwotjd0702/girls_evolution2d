using UnityEngine;

public class UpgradeManager : MonoBehaviour, ISaveable
{
    [Header("DI (GameSystem에서 주입)")]
    public CurrencyManager currencyManager;

    // 업그레이드 단계별 변수
    public int clickBonusUpgrade;           // 클릭 추가 골드 업그레이드
    public int maxFieldCountUpgrade;
    public int manualSpawnMaxUpgrade;
    public int manualSpawnSpeedUpgrade;
    public int autoMergeUpgrade;
    public int autoSpawnUpgrade;

    // ---- 수치 반환 ----
    public double GetClickIncomeMultiplier()
    {
        // 기본 1.0, 업당 +1.0
        return 1.0 + clickBonusUpgrade * 1.0;
    }

    public int GetMaxFieldCount() => 8 + maxFieldCountUpgrade;
    public int GetMaxManualSpawnCount() => 3 + manualSpawnMaxUpgrade;
    public float GetManualSpawnInterval() => Mathf.Max(2f, 10f * Mathf.Pow(0.85f, manualSpawnSpeedUpgrade));
    public bool IsAutoMergeActive() => autoMergeUpgrade > 0;
    public float GetAutoMergeSpeed() => 1f + 0.5f * autoMergeUpgrade;
    public float GetAutoSpawnInterval() => Mathf.Max(1f, 8f * Mathf.Pow(0.85f, autoSpawnUpgrade));

    // ---- 업그레이드 비용 공식 ----
    public double GetUpgradeCost(int currentLevel) => Mathf.RoundToInt(25 * Mathf.Pow(1.7f, currentLevel));

    public bool UpgradeClickBonus()         => TryUpgrade(ref clickBonusUpgrade, 10); // 최대 10회
    public bool UpgradeMaxFieldCount()      => TryUpgrade(ref maxFieldCountUpgrade, 8);
    public bool UpgradeManualSpawnMax()     => TryUpgrade(ref manualSpawnMaxUpgrade, 7);
    public bool UpgradeManualSpawnSpeed()   => TryUpgrade(ref manualSpawnSpeedUpgrade, 8);
    public bool UpgradeAutoMerge()          => TryUpgrade(ref autoMergeUpgrade, 1); // On/Off
    public bool UpgradeAutoSpawn()          => TryUpgrade(ref autoSpawnUpgrade, 8);

    private bool TryUpgrade(ref int field, int max = int.MaxValue)
    {
        if (currencyManager == null) return false;
        if (field >= max) return false;
        double cost = GetUpgradeCost(field);
        if (currencyManager.SpendGold(cost))
        {
            field++;
            return true;
        }
        return false;
    }

    // --------- ISaveable 구현 ---------
    public void ApplyLoadedData(SaveData data)
    {
        if (data == null) return;
        clickBonusUpgrade        = data.clickBonusUpgrade;
        maxFieldCountUpgrade     = data.maxFieldCountUpgrade;
        manualSpawnMaxUpgrade    = data.manualSpawnMaxUpgrade;
        manualSpawnSpeedUpgrade  = data.manualSpawnSpeedUpgrade;
        autoMergeUpgrade         = data.autoMergeUpgrade;
        autoSpawnUpgrade         = data.autoSpawnUpgrade;
    }

    public void CollectSaveData(SaveData data)
    {
        data.clickBonusUpgrade        = clickBonusUpgrade;
        data.maxFieldCountUpgrade     = maxFieldCountUpgrade;
        data.manualSpawnMaxUpgrade    = manualSpawnMaxUpgrade;
        data.manualSpawnSpeedUpgrade  = manualSpawnSpeedUpgrade;
        data.autoMergeUpgrade         = autoMergeUpgrade;
        data.autoSpawnUpgrade         = autoSpawnUpgrade;
    }
}
