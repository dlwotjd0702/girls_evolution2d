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
        public TextMeshProUGUI combinedLabel; // ⬅ 이름 전용(타이틀만)
        public TextMeshProUGUI levelText;     // "Lv. x / y"
        public TextMeshProUGUI costText;      // MAX: 밸류만 / 그 외: 밸류 + \n + 코스트

        [Header("Upgrade (Icon Only)")]
        public Button buyOrUpgradeButton; // 텍스트 없이 이미지 아이콘만
        public Image  iconTarget;
        public Sprite upgradeIconSprite;
        public Sprite maxIconSprite;      // MAX 도달 시
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
        {
            var entry = e; // 클로저 캡처 안전
            if (entry.buyOrUpgradeButton != null)
                entry.buyOrUpgradeButton.onClick.AddListener(() => OnClickUpgrade(entry));
        }

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
    void HandleUpgradeChanged()       => RefreshAll();

    // 클릭: 전부 "강화"만
    void OnClickUpgrade(EntryUI e)
    {
        if (economy == null) { ShowReasonTemp("시스템 미준비"); return; }

        int lv  = GetLevel(e.type);
        int cap = GetCap(e.type);
        if (lv >= cap) { ShowReasonTemp("최대 레벨입니다."); return; }

        bool ok = false;
        double need = GetNextCost(e.type);

        switch (e.type)
        {
            case ShopItemType.ManualSpawnMax:   ok = economy.TryBuySpawnMaxUpgrade();       break;
            case ShopItemType.ManualSpawnSpeed: ok = economy.TryBuySpawnSpeedUpgrade();     break;
            case ShopItemType.FieldMax:         ok = economy.TryBuyFieldMaxUpgrade();       break;
            case ShopItemType.ClickBonus:       ok = economy.TryBuyClickBonusUpgrade();     break;
            case ShopItemType.OfflineReward:    ok = economy.TryBuyOfflineRewardUpgrade();  break;
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

    // ─── 갱신 ───
    public void RefreshAll()
    {
        foreach (var e in entries) RefreshEntry(e);
    }

    void RefreshEntry(EntryUI e)
    {
        if (economy == null || e == null) return;

        int  lv    = GetLevel(e.type);
        int  cap   = GetCap(e.type);
        bool isMax = (lv >= cap);
        double nextCost = GetNextCost(e.type);

        // ⬇ 이름(타이틀만) 표시
        if (e.combinedLabel != null) e.combinedLabel.text = ComposeName(e.type);

        // ⬇ 레벨 라벨: "Lv. 현재 / 최대"
        if (e.levelText != null) e.levelText.text = FormatLvCap(lv, cap);

        // ⬇ 코스트 라벨: MAX면 "밸류만", 그 외 "밸류\n코스트"
        if (e.costText != null)
            e.costText.text = ComposeCostWithValue(e.type, nextCost, isMax);

        // ⬇ 아이콘/인터랙션
        if (e.iconTarget != null)
            e.iconTarget.sprite = isMax ? e.maxIconSprite : e.upgradeIconSprite;

        if (e.buyOrUpgradeButton != null)
            e.buyOrUpgradeButton.interactable = !isMax; // MAX에서만 비활성
    }

    // ─── Name / Value / Cost 합성 ───
    string ComposeName(ShopItemType t) => t switch
    {
        ShopItemType.ManualSpawnMax   => "수동 소환 최대치",
        ShopItemType.ManualSpawnSpeed => "수동 소환 쿨다운",
        ShopItemType.FieldMax         => "필드 최대 슬롯",
        ShopItemType.ClickBonus       => "클릭 보너스",
        ShopItemType.OfflineReward    => "오프라인 보상",
        ShopItemType.OfflineMaxTime   => "오프라인 시간",
        _ => "업그레이드"
    };

    string ComposeValue(ShopItemType t)
    {
        if (economy == null) return "-";

        switch (t)
        {
            case ShopItemType.ManualSpawnMax:   return $"MAX {economy.GetMaxManualSpawnCount()}개";
            case ShopItemType.ManualSpawnSpeed: return $"쿨타임 {economy.GetManualSpawnInterval():0.0}s";
            case ShopItemType.FieldMax:         return $"필드 {economy.GetMaxFieldCount()}칸";
            case ShopItemType.ClickBonus:       return $"클릭 x{economy.GetClickBonusMultiplier():0.0}";
            case ShopItemType.OfflineReward:    return $"오프라인 x{economy.GetOfflineRewardMultiplier():0.00}";
            case ShopItemType.OfflineMaxTime:   return $"상한 {(economy.GetOfflineMaxSeconds()/3600.0):0.0}h";
        }
        return "-";
    }

    string ComposeCostWithValue(ShopItemType t, double nextCost, bool isMax)
    {
        string value = ComposeValue(t);

        // ✅ MAX면 밸류만 노출
        if (isMax || double.IsInfinity(nextCost))
            return value;

        // ✅ 그 외에는 "밸류\n코스트"
        return $"{value}\n{nextCost:N0}";
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

    // "Lv. 현재 / 최대" 포맷
    string FormatLvCap(int lv, int cap)
    {
        if (cap <= 0) return $"Lv. {lv}";
        lv = Mathf.Clamp(lv, 0, cap);
        return $"Lv.{lv}/{cap}";
    }

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
