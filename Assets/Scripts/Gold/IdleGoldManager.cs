using UnityEngine;

public class IdleGoldManager : MonoBehaviour
{
    [Header("DI(자동주입, 수동지원)")]
    public GirlFieldManager girlFieldManager;
    public CurrencyManager currencyManager;
    public UpgradeManager upgradeManager;

    [Header("설정")]
    public float tickInterval = 1.0f;
    private float timer = 0f;

    void Update()
    {
        if (girlFieldManager == null || currencyManager == null) return;

        timer += Time.deltaTime;
        if (timer >= tickInterval)
        {
            timer = 0;
            double totalIncome = 0;
            foreach (var girl in girlFieldManager.girlList)
                totalIncome += girl.data.incomePerSec;
          

            currencyManager.AddGold(totalIncome);
        }
    }
}