using UnityEngine;

// 실제 현행 구조에 맞춰 필수 업그레이드만 남김
public class UpgradeManager : MonoBehaviour, ISaveable
{
    [Header("DI (GameSystem에서 주입)")]
    public CurrencyManager currencyManager;

    [Header("업그레이드 단계")]
    public int incomeUpgrade;         // 수익 증가
    public int autoIncomeUpgrade;     // 오토수익 증가
    public int clickIncomeUpgrade;    // 클릭수익 증가
    public int spawnUpgrade;          // 오토스폰 쿨다운 업그레이드
    public int spawnCountUpgrade;     // 오토스폰 한 번에 생성 수 업그레이드

    // 업그레이드 수치 반환
    public float GetIncomeMultiplier()      => 1f + incomeUpgrade * 0.2f;
    public float GetAutoIncomeMultiplier()  => 1f + autoIncomeUpgrade * 0.15f;
    public float GetClickIncomeMultiplier() => 1f + clickIncomeUpgrade * 0.5f;

    // 오토스폰 쿨타임 (ex: 2초 기준 → 업1당 20% 감소, 최소 0.3초)
    public float GetSpawnInterval()
        => Mathf.Max(2f, 5f * Mathf.Pow(0.8f, spawnUpgrade));
    // 한 번에 생성 수 (ex: 1 + 강화 단계)
    public int GetSpawnCount()
        => 1 + spawnCountUpgrade;

    // 업그레이드 비용 공식 (공통)
    public double GetUpgradeCost(int currentLevel)
        => Mathf.Pow(10, currentLevel + 1);

    // ---- 실제 업그레이드(내부 currencyManager 사용, 매개변수 없음) ----
    public bool UpgradeIncome()
        => TryUpgrade(ref incomeUpgrade);

    public bool UpgradeAutoIncome()
        => TryUpgrade(ref autoIncomeUpgrade);

    public bool UpgradeClickIncome()
        => TryUpgrade(ref clickIncomeUpgrade);

    public bool UpgradeSpawn()
        => TryUpgrade(ref spawnUpgrade);

    public bool UpgradeSpawnCount()
        => TryUpgrade(ref spawnCountUpgrade);

    // ---- 업그레이드 내부 통일 함수 ----
    private bool TryUpgrade(ref int upgradeField)
    {
        if (currencyManager == null) return false;
        double cost = GetUpgradeCost(upgradeField);
        if (currencyManager.SpendGold(cost))
        {
            upgradeField++;
            return true;
        }
        return false;
    }

    // --------- ISaveable 구현 ---------
    public void ApplyLoadedData(SaveData data)
    {
        if (data == null) return;
        incomeUpgrade      = data.incomeUpgrade;
        autoIncomeUpgrade  = data.autoIncomeUpgrade;
        clickIncomeUpgrade = data.clickIncomeUpgrade;
        spawnUpgrade       = data.spawnUpgrade;
        spawnCountUpgrade  = data.spawnCountUpgrade;
        Debug.Log("[UpgradeManager] 업그레이드 복원 완료");
    }

    public void CollectSaveData(SaveData data)
    {
        data.incomeUpgrade      = incomeUpgrade;
        data.autoIncomeUpgrade  = autoIncomeUpgrade;
        data.clickIncomeUpgrade = clickIncomeUpgrade;
        data.spawnUpgrade       = spawnUpgrade;
        data.spawnCountUpgrade  = spawnCountUpgrade;
        Debug.Log("[UpgradeManager] 업그레이드 저장 완료");
    }
}
