using UnityEngine;

// 카우에볼루션식: 강화, 수익 증가, 자동수익, 클릭 수익, 등 모든 업그레이드 요소 포괄
public class UpgradeManager : MonoBehaviour, ISaveable
{
    // 예시: 등급별 강화 단계
    public int levelUpgrade;           // 진화 레벨 업그레이드 (합성 외)
    public int incomeUpgrade;          // 기본 수익 증가
    public int autoIncomeUpgrade;      // 자동 수익(오토 수익) 업그레이드
    public int clickIncomeUpgrade;     // 클릭 시 수익 업그레이드
    public int spawnUpgrade;           // 미소녀 스폰 속도/갯수 업그레이드
    public int mergeBonusUpgrade;      // 합성 보너스(합성 시 추가 보상) 업그레이드
    public int maxGirlCountUpgrade;    // 필드 최대 미소녀 수 확장

    // 필요시 추가되는 요소
    // public int adBonusUpgrade;
    // public int offlineIncomeUpgrade;
    // public int gemIncomeUpgrade;
    // public int rareGirlChanceUpgrade;
    // ... 추가 가능

    // 업그레이드에 따른 수치 계산 (예시)
    public float GetIncomeMultiplier()
    {
        return 1f + incomeUpgrade * 0.2f; // 예시: 1업당 20% 증가
    }
    public float GetAutoIncomeMultiplier()
    {
        return 1f + autoIncomeUpgrade * 0.15f;
    }
    public float GetClickIncomeMultiplier()
    {
        return 1f + clickIncomeUpgrade * 0.5f;
    }
    public int GetMaxGirlCount()
    {
        return 8 + maxGirlCountUpgrade * 2;
    }
    
    public int GetMergeBonusUpgrade()
    {
        return mergeBonusUpgrade;
    }

    // 업그레이드 비용 계산
    public double GetUpgradeCost(int currentLevel)
    {
        return Mathf.Pow(10, currentLevel + 1); // 예시: 레벨 0 = 10, 레벨 1 = 100, 레벨 2 = 1000
    }
    
    // 실제 업그레이드 메서드 (재화 체크 등)
    public bool UpgradeIncome(CurrencyManager currencyManager)
    {
        double cost = GetUpgradeCost(incomeUpgrade);
        if (currencyManager.SpendGold(cost))
        {
            incomeUpgrade++;
            return true;
        }
        return false;
    }
    
    public bool UpgradeAutoIncome(CurrencyManager currencyManager)
    {
        double cost = GetUpgradeCost(autoIncomeUpgrade);
        if (currencyManager.SpendGold(cost))
        {
            autoIncomeUpgrade++;
            return true;
        }
        return false;
    }
    
    public bool UpgradeClickIncome(CurrencyManager currencyManager)
    {
        double cost = GetUpgradeCost(clickIncomeUpgrade);
        if (currencyManager.SpendGold(cost))
        {
            clickIncomeUpgrade++;
            return true;
        }
        return false;
    }
    
    public bool UpgradeSpawn(CurrencyManager currencyManager)
    {
        double cost = GetUpgradeCost(spawnUpgrade);
        if (currencyManager.SpendGold(cost))
        {
            spawnUpgrade++;
            return true;
        }
        return false;
    }
    
    public bool UpgradeMergeBonus(CurrencyManager currencyManager)
    {
        double cost = GetUpgradeCost(mergeBonusUpgrade);
        if (currencyManager.SpendGold(cost))
        {
            mergeBonusUpgrade++;
            return true;
        }
        return false;
    }
    
    public bool UpgradeMaxGirlCount(CurrencyManager currencyManager)
    {
        double cost = GetUpgradeCost(maxGirlCountUpgrade);
        if (currencyManager.SpendGold(cost))
        {
            maxGirlCountUpgrade++;
            return true;
        }
        return false;
    }

    // --------- ISaveable 구현 ---------
    public void ApplyLoadedData(SaveData data)
    {
        if (data == null) return;
        levelUpgrade        = data.levelUpgrade;
        incomeUpgrade       = data.incomeUpgrade;
        autoIncomeUpgrade   = data.autoIncomeUpgrade;
        clickIncomeUpgrade  = data.clickIncomeUpgrade;
        spawnUpgrade        = data.spawnUpgrade;
        mergeBonusUpgrade   = data.mergeBonusUpgrade;
        maxGirlCountUpgrade = data.maxGirlCountUpgrade;
        // 필요시 추가 필드도 복원
        Debug.Log("[UpgradeManager] 업그레이드 복원 완료");
    }

    public void CollectSaveData(SaveData data)
    {
        data.levelUpgrade        = levelUpgrade;
        data.incomeUpgrade       = incomeUpgrade;
        data.autoIncomeUpgrade   = autoIncomeUpgrade;
        data.clickIncomeUpgrade  = clickIncomeUpgrade;
        data.spawnUpgrade        = spawnUpgrade;
        data.mergeBonusUpgrade   = mergeBonusUpgrade;
        data.maxGirlCountUpgrade = maxGirlCountUpgrade;
        // 필요시 추가 필드도 저장
        Debug.Log("[UpgradeManager] 업그레이드 저장 완료");
    }
}
