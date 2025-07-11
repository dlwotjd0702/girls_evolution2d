using UnityEngine;

public class IdleGoldManager : MonoBehaviour
{
    public GirlFieldManager girlFieldManager;
    public CurrencyManager currencyManager;
    public UpgradeManager upgradeManager;
    public float tickInterval = 1.0f;
    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= tickInterval)
        {
            timer = 0;
            double totalIncome = 0;
            foreach (var girl in girlFieldManager.girlList)
                totalIncome += girl.evolutionData.incomePerSec;
            
            // 업그레이드 효과 적용
            if (upgradeManager != null)
            {
                totalIncome *= upgradeManager.GetIncomeMultiplier();
                totalIncome *= upgradeManager.GetAutoIncomeMultiplier();
            }
            
            currencyManager.AddGold(totalIncome);
        }
    }
}