using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanelController : MonoBehaviour
{
    // B안: Prestige 완전 제거 (Gold/Gem만 유지)
    public enum CurrencyTab { Gold, Gem }

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
        public TextMeshProUGUI levelText;     // "Lv. x / y"
        public TextMeshProUGUI costText;      // 밸류 + \n + 코스트 (MAX면 밸류만)

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
    [SerializeField] private PremiumCurrencyManager premiumCurrency; // 보석 관리자

    [Header("Reason Label")]
    [SerializeField] private TextMeshProUGUI reasonLabel;
    [SerializeField] private float reasonShowSeconds = 1.15f;
    private Coroutine _reasonRoutine;
    
    [Header("Insufficient Funds Panel")]
    [SerializeField] private InsufficientFundsPanel insufficientFundsPanel; // 골드 부족 패널

    [Header("Entries")]
    public List<EntryUI> entries = new List<EntryUI>();

    private const string GemAdHint = "광고 시청 보상으로 보석 5개를 받을 수 있어요!";

    void Awake()
    {
        if (economy == null) economy = FindObjectOfType<EconomyManager>(true);
        if (premiumCurrency == null) premiumCurrency = FindObjectOfType<PremiumCurrencyManager>(true);

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
    public void SwitchToGold() { currentTab = CurrencyTab.Gold; HideReasonImmediate(); RefreshAll(); }
    public void SwitchToGem()  { currentTab = CurrencyTab.Gem;  HideReasonImmediate(); RefreshAll(); }

    // ─────────────────────────────────────────────────────────────
    void OnClickUpgrade(EntryUI e)
    {
        if (e == null) return;

        switch (currentTab)
        {
            case CurrencyTab.Gold: BuyWithGold(e); break;
            case CurrencyTab.Gem:  BuyWithGems(e); break;
        }
    }

    void BuyWithGold(EntryUI e)
    {
        if (economy == null) { ShowReasonTemp("시스템 미준비"); return; }

        int lv  = GetLevel(e.type);
        int cap = GetCap(e.type);
        if (cap >= 0 && lv >= cap) { ShowReasonTemp("최대 레벨입니다."); return; }

        bool ok = false;
        double need = GetNextGoldCost(e.type);

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
            double currentGold = economy.GetGold();
            if (currentGold < need)
            {
                // 골드 부족 패널이 있으면 표시
                if (insufficientFundsPanel != null)
                {
                    insufficientFundsPanel.ShowForGoldShortage(need, currentGold);
                }
                else
                {
                    ShowReasonTemp("골드가 부족합니다.");
                }
            }
            else
            {
                ShowReasonTemp("구매가 불가합니다.");
            }
            return;
        }

        RefreshEntry(e);
        HideReasonImmediate();
    }

    void BuyWithGems(EntryUI e)
    {
        if (economy == null) { ShowReasonTemp("시스템 미준비"); return; }
        if (premiumCurrency == null) { ShowReasonTemp("보석 시스템 미준비"); return; }

        int lv  = GetLevel(e.type);
        int cap = GetCap(e.type);
        if (cap >= 0 && lv >= cap) { ShowReasonTemp("최대 레벨입니다."); return; }

        // 보석 비용 계산
        double goldCost = GetNextGoldCost(e.type);
        long gemCost = (double.IsInfinity(goldCost) || goldCost <= 0)
            ? 0
            : (long)Math.Max(1, Math.Ceiling(goldCost * Math.Max(1e-6, goldToGemFactor)));

        // 보석 차감 시도
        if (!premiumCurrency.TrySpendGems(gemCost))
        {
            // 보석 부족 시 insufficientPanel 표시
            if (insufficientFundsPanel != null)
            {
                long have = premiumCurrency.GetGems();
                insufficientFundsPanel.ShowGemShortage(gemCost, have);
            }
            else
            {
                ShowReasonTemp($"보석이 부족합니다.\n{GemAdHint}");
            }
            return;
        }

        // 보석 차감 성공 시 골드 구매 메서드 호출
        bool ok = false;
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
            // 구매 실패 시 보석 환불
            premiumCurrency.AddGems(gemCost);
            ShowReasonTemp("구매가 불가합니다.");
            return;
        }

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

        int  lv    = GetLevel(e.type);
        int  cap   = GetCap(e.type);
        bool isMax = (cap >= 0 && lv >= cap);

        if (e.combinedLabel != null) e.combinedLabel.text = ComposeName(e.type);

        if (e.levelText != null)
            e.levelText.text = FormatLvCap(lv, cap);

        if (e.costText != null)
        {
            if (currentTab == CurrencyTab.Gem)
            {
                double goldCost = GetNextGoldCost(e.type);
                long gemCost = (double.IsInfinity(goldCost) || goldCost <= 0)
                    ? 0
                    : (long)Math.Max(1, Math.Ceiling(goldCost * Math.Max(1e-6, goldToGemFactor)));
                string value = ComposeValueGoldEconomy(e.type);
                e.costText.text = (isMax || gemCost <= 0) ? value : $"{value}\n{gemCost:N0} gems";
            }
            else // Gold
            {
                double goldCost = GetNextGoldCost(e.type);
                string value = ComposeValueGoldEconomy(e.type);
                e.costText.text = (isMax || double.IsInfinity(goldCost)) ? value : $"{value}\n{goldCost:N0} G";
            }
        }

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

    // ─────────────────────────────────────────────────────────────
    int GetLevel(ShopItemType t)
    {
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

    int GetCap(ShopItemType t)
    {
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

    double GetNextGoldCost(ShopItemType t)
    {
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

    // ─────────────────────────────────────────────────────────────
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
