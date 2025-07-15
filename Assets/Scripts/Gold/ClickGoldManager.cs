using UnityEngine;

public class ClickGoldManager : MonoBehaviour
{
    public CurrencyManager currencyManager;
    public UpgradeManager upgradeManager;
    public float clickGoldBase = 1.0f;
    
    void Update()
    {
        // 화면 클릭 감지
        if (Input.GetMouseButtonDown(0))
        {
            // UI 클릭이 아닌 경우에만 골드 획득
            if (!IsPointerOverUI())
            {
                AddClickGold();
            }
        }
    }
    
    void AddClickGold()
    {
        if (currencyManager != null && upgradeManager != null)
        {
            double clickGold = clickGoldBase * upgradeManager.GetClickIncomeMultiplier();
            currencyManager.AddGold(clickGold);
        }
    }
    
    bool IsPointerOverUI()
    {
        // 간단한 UI 클릭 감지 (실제로는 EventSystem.current.IsPointerOverGameObject() 사용 권장)
        return false;
    }
} 