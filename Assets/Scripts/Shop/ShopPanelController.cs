using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanelController : MonoBehaviour
{
    // 탭에 따라 동일 엔트리를 "자동 라우팅"해서 구매/업그레이드 처리
    public enum CurrencyTab { Gold, Gem, Prestige }

    public enum ShopItemType
    {
        ManualSpawnMax,
        ManualSpawnSpeed,
        FieldMax,
        ClickBonus,
        OfflineReward,
        OfflineMaxTime,
    }

    [Serializable]
    public class EntryUI
    {
        [Header("Config")]
        public ShopItemType type;

        [Header("Texts (TMP)")]
        public TextMeshProUGUI combinedLabel; // 이름(타이틀)
        public TextMeshProUGUI levelText;     // "Lv. x / y" (Prestige 탭은 cap 없음이면 "Lv. x")
        public TextMeshProUGUI costText;      // MAX: 밸류만 / 그 외: 밸류 + \n + 코스트

        [Header("Upgrade (Icon Only)")]
        public Button buyOrUpgradeButton;     // 텍스트 없이 아이콘만
        public Image  iconTarget;
        public Sprite upgradeIconSprite;
        public Sprite maxIconSprite;          // MAX 도달 시
    }

    [Header("Routing")]
    public CurrencyTab currentTab = CurrencyTab.Gold;

    [Tooltip("보석 탭 가격 계산시: gem = ceil(goldCost * factor)")]
    public double goldToGemFactor = 0.01;

    [Header("Refs")]
    public EconomyManager economy;
    public PrestigeManager prestige; // 통합 환생 매니저(필요 시)
    private PrestigeShopManager shop; // Facade (PrestigeManager를 위임)

    [Header("Reason Label")]
    [SerializeField] private TextMeshProUGUI reasonLabel;
    [SerializeField] private float reasonShowSeconds = 1.15f;
    private Coroutine _reasonRoutine;

    [Header("Entries")]
    public List<EntryUI> entries = new List<EntryUI>();

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (economy == null)  economy  = FindObjectOfType<EconomyManager>(true);
        if (prestige == null) prestige = FindObjectOfType<PrestigeManager>(true);
        shop = PrestigeShopManager.Instance; // 없으면 null-safe로 동작

        foreach (var e in entries)
        {
            var entry = e; // 클로저 캡처 안전
            if (entry.buyOrUpgradeButton != null)
            {
                entry.buyOrUpgradeButton.onClick.RemoveAllListeners();
                entry.buyOrUpgradeButton.onClick.AddListener(() => OnClickUpgrade(entry));
            }
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

    // ─────────────────────────────────────────────────────────────
    // 외부에서 탭 전환 버튼으로 호출
    public void SwitchToGold()     { currentTab = CurrencyTab.Gold;     RefreshAll(); }
    public void SwitchToGem()      { currentTab = CurrencyTab.Gem;      RefreshAll(); }
    public void SwitchToPrestige() { currentTab = CurrencyTab.Prestige; RefreshAll(); }

    // ─────────────────────────────────────────────────────────────
    void OnClickUpgrade(EntryUI e)
    {
        if (e == null) return;

        switch (currentTab)
        {
            case CurrencyTab.Gold:
                BuyWithGold(e);
                break;

            case CurrencyTab.Gem:
                BuyWithGems(e);
                break;

            case CurrencyTab.Prestige:
                BuyWithPrestige(e);
                break;
        }
    }

    void BuyWithGold(EntryUI e)
    {
        if (economy == null) { ShowReasonTemp("시스템 미준비"); return; }

        int lv  = GetLevel(e.type, CurrencyTab.Gold);
        int cap = GetCap(e.type, CurrencyTab.Gold);
        if (cap >= 0 && lv >= cap) { ShowReasonTemp("최대 레벨입니다."); return; }

        bool ok = false;
        double need = GetNextCost(e.type, CurrencyTab.Gold);

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

    void BuyWithGems(EntryUI e)
    {
        if (economy == null) { ShowReasonTemp("시스템 미준비"); return; }

        int lv  = GetLevel(e.type, CurrencyTab.Gem);
        int cap = GetCap(e.type, CurrencyTab.Gem);
        if (cap >= 0 && lv >= cap) { ShowReasonTemp("최대 레벨입니다."); return; }

        // EconomyManager에 보석 전용 API가 있으면 호출(리플렉션)
        string method = e.type switch
        {
            ShopItemType.ManualSpawnMax   => "TryBuySpawnMaxWithGems",
            ShopItemType.ManualSpawnSpeed => "TryBuySpawnSpeedWithGems",
            ShopItemType.FieldMax         => "TryBuyFieldMaxWithGems",
            ShopItemType.ClickBonus       => "TryBuyClickBonusWithGems",
            ShopItemType.OfflineReward    => "TryBuyOfflineRewardWithGems",
            ShopItemType.OfflineMaxTime   => "TryBuyOfflineMaxTimeWithGems",
            _ => null
        };

        if (!string.IsNullOrEmpty(method))
        {
            try
            {
                var mi = economy.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Instance);
                if (mi != null && mi.ReturnType == typeof(bool))
                {
                    bool ok = (bool)mi.Invoke(economy, null);
                    if (!ok) { ShowReasonTemp("보석이 부족합니다."); return; }

                    RefreshEntry(e);
                    HideReasonImmediate();
                    return;
                }
            }
            catch { /* 무시하고 폴백 */ }
        }

        ShowReasonTemp("유료재화 구매 미구현");
    }

    void BuyWithPrestige(EntryUI e)
    {
        if (shop == null || prestige == null) { ShowReasonTemp("시스템 미준비"); return; }

        int lv  = GetLevel(e.type, CurrencyTab.Prestige);
        int cap = GetCap(e.type, CurrencyTab.Prestige);
        if (cap >= 0 && lv >= cap) { ShowReasonTemp("최대 레벨입니다."); return; }

        int need = GetPrestigeCost(e.type);
        if (GetPrestigePointsSafe() < need) { ShowReasonTemp("포인트가 부족합니다."); return; }

        bool ok = e.type switch
        {
            ShopItemType.ManualSpawnMax   => Safe(() => shop.TryBuyPlusManualSpawnMax(), false),
            ShopItemType.ManualSpawnSpeed => Safe(() => shop.TryBuyPlusManualSpeed(), false),
            ShopItemType.FieldMax         => Safe(() => shop.TryBuyPlusFieldMax(), false),
            ShopItemType.ClickBonus       => Safe(() => shop.TryBuyPlusClickBonus(), false),
            ShopItemType.OfflineReward    => Safe(() => shop.TryBuyPlusOfflineReward(), false),
            ShopItemType.OfflineMaxTime   => Safe(() => shop.TryBuyPlusOfflineMaxTime(), false),
            _ => false
        };

        if (!ok) { ShowReasonTemp("구매 실패"); return; }

        RefreshEntry(e);
        HideReasonImmediate();
    }

    // ─────────────────────────────────────────────────────────────
    public void RefreshAll()
    {
        foreach (var e in entries) RefreshEntry(e);
    }

    void RefreshEntry(EntryUI e)
    {
        if (e == null) return;

        int  lv    = GetLevel(e.type, currentTab);
        int  cap   = GetCap(e.type, currentTab);
        bool isMax = (cap >= 0 && lv >= cap);

        // ⬇ 이름(타이틀만)
        if (e.combinedLabel != null) e.combinedLabel.text = ComposeName(e.type);

        // ⬇ 레벨 라벨
        if (e.levelText != null)
        {
            if (currentTab == CurrencyTab.Prestige && cap < 0) e.levelText.text = $"Lv. {Mathf.Max(0, lv)}";
            else                                               e.levelText.text = FormatLvCap(lv, cap);
        }

        // ⬇ 코스트/밸류 라벨
        if (e.costText != null)
        {
            if (currentTab == CurrencyTab.Prestige)
            {
                int cost = GetPrestigeCost(e.type);
                string value = ComposeValuePrestige(e.type);
                e.costText.text = (isMax || cost <= 0) ? value : $"{value}\n{cost:N0}";
            }
            else if (currentTab == CurrencyTab.Gem)
            {
                double goldCost = GetNextCost(e.type, CurrencyTab.Gold);
                long gemCost = (double.IsInfinity(goldCost) || goldCost <= 0)
                    ? 0
                    : (long)Math.Max(1, Math.Ceiling(goldCost * Math.Max(1e-6, goldToGemFactor)));
                string value = ComposeValueGoldEconomy(e.type);
                e.costText.text = (isMax || gemCost <= 0) ? value : $"{value}\n{gemCost:N0}";
            }
            else // Gold
            {
                double goldCost = GetNextCost(e.type, CurrencyTab.Gold);
                string value = ComposeValueGoldEconomy(e.type);
                e.costText.text = (isMax || double.IsInfinity(goldCost)) ? value : $"{value}\n{goldCost:N0}";
            }
        }

        // ⬇ 아이콘/인터랙션
        if (e.iconTarget != null) e.iconTarget.sprite = isMax ? e.maxIconSprite : e.upgradeIconSprite;
        if (e.buyOrUpgradeButton != null) e.buyOrUpgradeButton.interactable = !isMax;
    }

    // ─────────────────────────────────────────────────────────────
    // Name / Value
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

    string ComposeValueGoldEconomy(ShopItemType t)
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

    string ComposeValuePrestige(ShopItemType t)
    {
        if (shop == null) return "-";

        switch (t)
        {
            case ShopItemType.ManualSpawnMax:
            {
                int add = Safe(() => shop.GetManualSpawnMaxPlus(), 0);
                return $"+{add}개";
            }
            case ShopItemType.ManualSpawnSpeed:
            {
                double m = Safe(() => shop.GetManualSpawnIntervalMul(), 1.0);
                return $"-{(1.0 - m) * 100.0:0.#}%";
            }
            case ShopItemType.FieldMax:
            {
                int add = Safe(() => shop.GetFieldMaxPlus(), 0);
                return $"+{add}칸";
            }
            case ShopItemType.ClickBonus:
            {
                double mul = Safe(() => shop.GetClickBonusMul(), 1.0);
                return $"x{mul:0.00}";
            }
            case ShopItemType.OfflineReward:
            {
                double mul = Safe(() => shop.GetOfflineRewardMul(), 1.0);
                return $"x{mul:0.00}";
            }
            case ShopItemType.OfflineMaxTime:
            {
                int sec = Safe(() => shop.GetOfflineMaxExtraSeconds(), 0);
                return $"+{sec/60}분";
            }
        }
        return "-";
    }

    // ─────────────────────────────────────────────────────────────
    // 레벨/캡/코스트 (탭별 라우팅)
    int GetLevel(ShopItemType t, CurrencyTab tab)
    {
        if (tab == CurrencyTab.Prestige) return GetPrestigeLevel(t);
        if (economy == null) return 0;

        return t switch
        {
            ShopItemType.ManualSpawnMax   => economy.GetSpawnMaxUpgradeLevel(),
            ShopItemType.ManualSpawnSpeed => economy.GetSpawnSpeedUpgradeLevel(),
            ShopItemType.FieldMax         => economy.GetFieldMaxUpgradeLevel(),
            ShopItemType.ClickBonus       => economy.GetClickBonusUpgradeLevel(),
            ShopItemType.OfflineReward    => economy.GetOfflineRewardUpgradeLevel(),
            ShopItemType.OfflineMaxTime   => economy.GetOfflineMaxTimeUpgradeLevel(),
            _ => 0
        };
    }

    int GetCap(ShopItemType t, CurrencyTab tab)
    {
        if (tab == CurrencyTab.Prestige) return -1; // cap 미설정(무한)
        if (economy == null) return 0;

        return t switch
        {
            ShopItemType.ManualSpawnMax   => economy.GetSpawnMaxUpgradeCap(),
            ShopItemType.ManualSpawnSpeed => economy.GetSpawnSpeedUpgradeCap(),
            ShopItemType.FieldMax         => economy.GetFieldMaxUpgradeCap(),
            ShopItemType.ClickBonus       => economy.GetClickBonusUpgradeCap(),
            ShopItemType.OfflineReward    => economy.GetOfflineRewardCap(),
            ShopItemType.OfflineMaxTime   => economy.GetOfflineMaxTimeCap(),
            _ => 0
        };
    }

    double GetNextCost(ShopItemType t, CurrencyTab tab)
    {
        if (tab == CurrencyTab.Prestige) return GetPrestigeCost(t); // 포인트 비용
        if (economy == null) return double.PositiveInfinity;

        return t switch
        {
            ShopItemType.ManualSpawnMax   => economy.GetSpawnMaxUpgradeCost(economy.GetSpawnMaxUpgradeLevel()),
            ShopItemType.ManualSpawnSpeed => economy.GetSpawnSpeedUpgradeCost(economy.GetSpawnSpeedUpgradeLevel()),
            ShopItemType.FieldMax         => economy.GetFieldMaxUpgradeCost(economy.GetFieldMaxUpgradeLevel()),
            ShopItemType.ClickBonus       => economy.GetClickBonusUpgradeCost(economy.GetClickBonusUpgradeLevel()),
            ShopItemType.OfflineReward    => economy.GetOfflineRewardUpgradeCost(economy.GetOfflineRewardUpgradeLevel()),
            ShopItemType.OfflineMaxTime   => economy.GetOfflineMaxTimeUpgradeCost(economy.GetOfflineMaxTimeUpgradeLevel()),
            _ => double.PositiveInfinity
        };
    }

    int GetPrestigeCost(ShopItemType t)
    {
        if (shop == null) return int.MaxValue;

        return t switch
        {
            ShopItemType.ManualSpawnMax   => Safe(() => shop.GetPlusManualSpawnMaxCost(),  int.MaxValue),
            ShopItemType.ManualSpawnSpeed => Safe(() => shop.GetPlusManualSpeedCost(),     int.MaxValue),
            ShopItemType.FieldMax         => Safe(() => shop.GetPlusFieldMaxCost(),        int.MaxValue),
            ShopItemType.ClickBonus       => Safe(() => shop.GetPlusClickBonusCost(),      int.MaxValue),
            ShopItemType.OfflineReward    => Safe(() => shop.GetPlusOfflineRewardCost(),   int.MaxValue),
            ShopItemType.OfflineMaxTime   => Safe(() => shop.GetPlusOfflineMaxTimeCost(),  int.MaxValue),
            _ => int.MaxValue
        };
    }

    int GetPrestigeLevel(ShopItemType t)
    {
        if (shop == null) return 0;

        string field = t switch
        {
            ShopItemType.ManualSpawnMax   => "plusManualSpawnMaxLv",
            ShopItemType.ManualSpawnSpeed => "plusManualSpawnSpeedLv",
            ShopItemType.FieldMax         => "plusFieldMaxLv",
            ShopItemType.ClickBonus       => "plusClickBonusLv",
            ShopItemType.OfflineReward    => "plusOfflineRewardLv",
            ShopItemType.OfflineMaxTime   => "plusOfflineMaxTimeLv",
            _ => null
        };
        if (string.IsNullOrEmpty(field)) return 0;

        try
        {
            var fi = typeof(PrestigeShopManager).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            if (fi != null && fi.FieldType == typeof(int)) return (int)fi.GetValue(shop);
        }
        catch { }

        return 0;
    }

    int GetPrestigePointsSafe()
    {
        try { return prestige != null ? prestige.GetPrestigePoints() : 0; } catch { return 0; }
    }

    // ─────────────────────────────────────────────────────────────
    // 표기 유틸
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

    // 안전 호출
    static T Safe<T>(Func<T> f, T fb){ try { return f(); } catch { return fb; } }
}
