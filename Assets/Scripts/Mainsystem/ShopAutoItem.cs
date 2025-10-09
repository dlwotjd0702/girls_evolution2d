using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopAutoMergeRow : MonoBehaviour
{
    [Header("Refs")]
    public GirlFieldManager field;     // 씬의 GirlFieldManager
    public EconomyManager   economy;   // 씬의 EconomyManager

    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text levelText;
    public TMP_Text cooldownText;
    public TMP_Text costText;
    public Button   buyOrUpgradeButton;
    public TMP_Text buyOrUpgradeLabel;
    public GameObject toggleRow;
    public Button   toggleOnBtn;   // "활성화" 버튼
    public Button   toggleOffBtn;  // "비활성화" 버튼

    [Header("Title (optional)")]
    public string customTitle = "오토 합성";

    void Awake()
    {
        if (!field)   field   = FindObjectOfType<GirlFieldManager>();
        if (!economy) economy = FindObjectOfType<EconomyManager>();

        if (buyOrUpgradeButton) buyOrUpgradeButton.onClick.AddListener(OnClickBuyOrUpgrade);
        if (toggleOnBtn)  toggleOnBtn.onClick.AddListener(() => SetOn(true));
        if (toggleOffBtn) toggleOffBtn.onClick.AddListener(() => SetOn(false));
    }

    void OnEnable() => Refresh();

    void SetOn(bool on)
    {
        if (!field) return;
        field.SetAutoMergeOn(on);
        Refresh();
    }

    void OnClickBuyOrUpgrade()
    {
        if (!field || !economy) return;
        if (field.TryBuyAutoMerge(economy))
            Refresh();
    }

    public void Refresh()
    {
        if (!field) return;

        int   lv = field.GetAutoMergeLevel();
        bool  on = field.IsAutoMergeOn();
        float cd = field.GetAutoMergeInterval();
        double nextCost = field.GetAutoMergeNextCost();

        if (titleText)    titleText.text = string.IsNullOrEmpty(customTitle) ? "오토 합성" : customTitle;
        if (levelText)    levelText.text = (lv <= 0) ? "잠김" : $"Lv.{lv}";
        if (cooldownText) cooldownText.text = (lv <= 0 || cd == float.MaxValue) ? "-" : $"{cd:0.0}s";
        if (costText)     costText.text = $"{nextCost:N0}";

        bool locked = lv <= 0;
        if (toggleRow) toggleRow.SetActive(!locked);
        if (toggleOnBtn)  toggleOnBtn.gameObject.SetActive(!locked && !on);
        if (toggleOffBtn) toggleOffBtn.gameObject.SetActive(!locked && on);

        if (buyOrUpgradeLabel) buyOrUpgradeLabel.text = locked ? "구매" : "강화";
    }
}
