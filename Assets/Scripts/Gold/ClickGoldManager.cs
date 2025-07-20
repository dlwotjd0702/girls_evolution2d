using UnityEngine;

public class ClickGoldManager : MonoBehaviour
{
    [Header("DI (자동주입, 수동 할당 모두 지원)")]
    public GirlFieldManager girlFieldManager;
    public CurrencyManager currencyManager;
    public UpgradeManager upgradeManager;

    [Header("설정")]
    public float clickGoldBase = 1.0f; // 클릭당 기본 수익

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverUI())
                AddClickGold();
        }
    }

    void AddClickGold()
    {
        if (currencyManager == null || upgradeManager == null) return;

        double clickGold = clickGoldBase * upgradeManager.GetClickIncomeMultiplier();

        // 확장: 미소녀 수 등 룰에 따라 추가 보상 가능
        // if (girlFieldManager != null)
        //     clickGold *= girlFieldManager.girlList.Count;

        currencyManager.AddGold(clickGold);
    }

    bool IsPointerOverUI()
    {
        // 실전 적용은 EventSystem.current.IsPointerOverGameObject() 사용
        return false;
    }
}