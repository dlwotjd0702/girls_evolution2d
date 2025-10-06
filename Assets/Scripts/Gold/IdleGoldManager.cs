// IdleGoldManager.cs (교체)
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
        if (timer < tickInterval) return;
        timer = 0f;

        double total = 0;
        var list = girlFieldManager.girlList;
        for (int i = 0; i < list.Count; i++)
        {
            var g = list[i];
            if (g == null || g.data == null) continue;
            total += g.GetIncome(); // ← 25레벨 랭크/배수 포함
        }

        // (선택) 업그레이드 배수 적용 지점 통일하려면 여기서 곱해도 됨.
        // if (upgradeManager != null) total *= upgradeManager.GetIdleIncomeMultiplier();

        if (total > 0) currencyManager.AddGold(total);
    }
}
