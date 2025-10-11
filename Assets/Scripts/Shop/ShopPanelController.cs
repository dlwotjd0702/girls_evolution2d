using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanelController : MonoBehaviour
{
    public enum ShopItemType
    {
        ManualSpawnMax,
        ManualSpawnSpeed,
        FieldMax,
        ClickBonus,
        OfflineReward,
        OfflineMaxTime,
    }

    [System.Serializable]
    public class EntryUI
    {
        [Header("Config")]
        public ShopItemType type;

        [Header("Texts (TMP)")]
        public TextMeshProUGUI levelText;  // "Lv.X"
        public TextMeshProUGUI valueText;  // 현재 스탯
        public TextMeshProUGUI costText;   // 비용 (추가)

        [Header("Upgrade (Icon Only)")]
        public Button buyOrUpgradeButton;
        public Image  iconTarget;
        public Sprite upgradeIconSprite;
        public Sprite maxIconSprite;     // MAX일 때 아이콘
    }

    [Header("Refs")]
    public EconomyManager economy;

    [Header("Reason Label")]
    [SerializeField] private TextMeshProUGUI reasonLabel;
    [SerializeField] private float reasonShowSeconds = 1.15f;
    private Coroutine _reasonRoutine;

    [Header("Entries")]
    public List<EntryUI> entries = new List<EntryUI>();

    void Awake()
    {
        if (economy == null) economy = FindObjectOfType<EconomyManager>();
        foreach (var e in entries)
            if (e.buyOrUpgradeButton != null)
                e.buyOrUpgradeButton.onClick.AddListener(() => OnClickUpgrade(e));

        if (reasonLabel != null) reasonLabel.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (economy != null)
        {
            economy.OnGoldChanged    += HandleGoldChanged;
            economy.OnUpgradeChanged += HandleUpgradeChanged;
        }
        RefreshAll();
    }
    void OnDisable()
    {
        if (economy != null)
        {
            economy.OnGoldChanged    -= HandleGoldChanged;
            economy.OnUpgradeChanged -= HandleUpgradeChanged;
        }
        HideReasonImmediate();
    }
    void HandleGoldChanged(double _) => RefreshAll();
    void HandleUpgradeChanged() => RefreshAll();

    // ── 클릭: 전부 "강화"만 ──
    void OnClickUpgrade(EntryUI e)
    {
        if (economy == null) { ShowReasonTemp("시스템 미준비"); return; }

        int lv = GetLevel(e.type);
        int cap = GetCap(e.type);
        if (lv >= cap) { ShowReasonTemp("최대 레벨입니다."); return; }

        bool ok = false;
        double need = GetNextCost(e.type); // 표시용/검증용

        switch (e.type)
        {
            case ShopItemType.ManualSpawnMax:   ok = economy.TryBuySpawnMaxUpgrade();     break;
            case ShopItemType.ManualSpawnSpeed: ok = economy.TryBuySpawnSpeedUpgrade();   break;
            case ShopItemType.FieldMax:         ok = economy.TryBuyFieldMaxUpgrade();     break;
            case ShopItemType.ClickBonus:       ok = economy.TryBuyClickBonusUpgrade();   break;
            case ShopItemType.OfflineReward:    ok = economy.TryBuyOfflineRewardUpgrade();break;
            case ShopItemType.OfflineMaxTime:   ok = economy.TryBuyOfflineMaxTimeUpgrade(); break;
        }

        if (!ok)
        {
            if (economy.GetGold() < need) ShowReasonTemp("골드가 부족합니다.");
            else ShowReasonTemp("구매가 불가합니다.");
            return;
        }

        RefreshEntry(e);
        HideReasonImmediate();
    }

    // ── 갱신 ──
    public void RefreshAll()
    {
        foreach (var e in entries) RefreshEntry(e);
    }

    void RefreshEntry(EntryUI e)
    {
        if (economy == null || e == null) return;

        int lv  = GetLevel(e.type);
        int cap = GetCap(e.type);
        bool isMax = (lv >= cap);
        double nextCost = GetNextCost(e.type);

        if (e.levelText != null) e.levelText.text = $"Lv.{lv}";
        if (e.valueText != null) e.valueText.text = GetDisplayValue(e.type);
        if (e.costText  != null) e.costText.text  = double.IsInfinity(nextCost) ? "-" : $"{nextCost:N0}";

        if (e.iconTarget != null)
            e.iconTarget.sprite = isMax ? e.maxIconSprite : e.upgradeIconSprite;

        if (e.buyOrUpgradeButton != null)
            e.buyOrUpgradeButton.interactable = !isMax; // 골드 부족이어도 비활성화 X, "MAX"일 때만 비활성
    }

    // 값 표기(스탯만)
    string GetDisplayValue(ShopItemType t)
    {
        switch (t)
        {
            case ShopItemType.ManualSpawnMax:   return $"최대 {economy.GetMaxManualSpawnCount()}칸";
            case ShopItemType.ManualSpawnSpeed: return $"쿨 {economy.GetManualSpawnInterval():0.0}s";
            case ShopItemType.FieldMax:         return $"필드 {economy.GetMaxFieldCount()}칸";
            case ShopItemType.ClickBonus:       return $"클릭 x{economy.GetClickBonusMultiplier():0.0}";
            case ShopItemType.OfflineReward:    return $"오프라인 x{economy.GetOfflineRewardMultiplier():0.00}";
            case ShopItemType.OfflineMaxTime:   return $"상한 {(economy.GetOfflineMaxSeconds()/3600.0):0.0}h";
        }
        return "-";
    }

    // 레벨/캡/코스트
    int GetLevel(ShopItemType t) => t switch
    {
        ShopItemType.ManualSpawnMax   => economy.GetSpawnMaxUpgradeLevel(),
        ShopItemType.ManualSpawnSpeed => economy.GetSpawnSpeedUpgradeLevel(),
        ShopItemType.FieldMax         => economy.GetFieldMaxUpgradeLevel(),
        ShopItemType.ClickBonus       => economy.GetClickBonusUpgradeLevel(),
        ShopItemType.OfflineReward    => economy.GetOfflineRewardUpgradeLevel(),
        ShopItemType.OfflineMaxTime   => economy.GetOfflineMaxTimeUpgradeLevel(),
        _ => 0
    };
    int GetCap(ShopItemType t) => t switch
    {
        ShopItemType.ManualSpawnMax   => economy.GetSpawnMaxUpgradeCap(),
        ShopItemType.ManualSpawnSpeed => economy.GetSpawnSpeedUpgradeCap(),
        ShopItemType.FieldMax         => economy.GetFieldMaxUpgradeCap(),
        ShopItemType.ClickBonus       => economy.GetClickBonusUpgradeCap(),
        ShopItemType.OfflineReward    => economy.GetOfflineRewardCap(),
        ShopItemType.OfflineMaxTime   => economy.GetOfflineMaxTimeCap(),
        _ => 0
    };
    double GetNextCost(ShopItemType t) => t switch
    {
        ShopItemType.ManualSpawnMax   => economy.GetSpawnMaxUpgradeCost(economy.GetSpawnMaxUpgradeLevel()),
        ShopItemType.ManualSpawnSpeed => economy.GetSpawnSpeedUpgradeCost(economy.GetSpawnSpeedUpgradeLevel()),
        ShopItemType.FieldMax         => economy.GetFieldMaxUpgradeCost(economy.GetFieldMaxUpgradeLevel()),
        ShopItemType.ClickBonus       => economy.GetClickBonusUpgradeCost(economy.GetClickBonusUpgradeLevel()),
        ShopItemType.OfflineReward    => economy.GetOfflineRewardUpgradeCost(economy.GetOfflineRewardUpgradeLevel()),
        ShopItemType.OfflineMaxTime   => economy.GetOfflineMaxTimeUpgradeCost(economy.GetOfflineMaxTimeUpgradeLevel()),
        _ => double.PositiveInfinity
    };

    // Reason helpers
    void ShowReasonTemp(string msg)
    {
        if (reasonLabel == null) return;
        HideReasonImmediate();
        reasonLabel.text = msg;
        reasonLabel.gameObject.SetActive(true);
        _reasonRoutine = StartCoroutine(HideReasonAfter(reasonShowSeconds));
    }
    System.Collections.IEnumerator HideReasonAfter(float sec){ yield return new WaitForSecondsRealtime(sec); HideReasonImmediate(); }
    void HideReasonImmediate(){ if (reasonLabel==null) return; if (_reasonRoutine!=null){ StopCoroutine(_reasonRoutine); _reasonRoutine=null; } reasonLabel.text=""; reasonLabel.gameObject.SetActive(false); }
}
