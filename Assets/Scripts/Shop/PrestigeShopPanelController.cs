// ====================================================================
// PrestigeShopPanelController.cs (페이샤드 제거판)
// - 포인트 라벨 갱신 제거(PrestigeManager가 전담)
// - 모든 데이터 접근은 PrestigeManager.Instance로 직접 호출
// ====================================================================
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrestigeShopPanelController : MonoBehaviour
{
    public enum ShopItemType
    {
        // 핵심 4종
        IncomeMultiplier,
        TwoStepChance,
        StartGoldMultiplier,
        PrestigePointGain,

        // 일반 상점 Plus 8종
        ManualSpawnMaxPlus,
        ManualSpawnSpeedPlus,
        AutoMergeSpeedPlus,
        AutoSpawnSpeedPlus,
        FieldMaxPlus,
        ClickBonusPlus,
        OfflineRewardPlus,
        OfflineMaxTimePlus
    }

    [Serializable]
    public class EntryUI
    {
        [Header("Config")]
        public ShopItemType type;
        [Tooltip("레벨 상한(선택). -1이면 무한.")]
        public int levelCap = -1;

        [Header("Texts (TMP)")]
        public TextMeshProUGUI combinedLabel;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI costText;

        [Header("Upgrade (Icon Only)")]
        public Button buyOrUpgradeButton;
        public Image  iconTarget;
        public Sprite upgradeIconSprite;
        public Sprite maxIconSprite;
    }

    [Header("Refs")]
    public PrestigeManager prestige;

    [Header("Reason Label")]
    [SerializeField] private TextMeshProUGUI reasonLabel;
    [SerializeField] private float reasonShowSeconds = 1.15f;
    private Coroutine _reasonRoutine;

    [Header("Entries")]
    public List<EntryUI> entries = new List<EntryUI>();

    private float _refreshTimer = 0f;
    private const float REFRESH_INTERVAL = 0.25f;

    void Awake()
    {
        if (!prestige) prestige = FindObjectOfType<PrestigeManager>(true);

        foreach (var e in entries)
        {
            var entry = e; // 클로저 안전
            if (entry.buyOrUpgradeButton != null)
                entry.buyOrUpgradeButton.onClick.AddListener(() => OnClickBuy(entry));
        }
        if (reasonLabel != null) reasonLabel.gameObject.SetActive(false);
    }

    void OnEnable(){ RefreshAll(); _refreshTimer = 0f; }
    void Update()
    {
        _refreshTimer += Time.unscaledDeltaTime;
        if (_refreshTimer >= REFRESH_INTERVAL)
        { _refreshTimer = 0f; RefreshAll(); }
    }

    // ───────────────────────── 클릭 ─────────────────────────
    void OnClickBuy(EntryUI e)
    {
        if (prestige == null) { ShowReasonTemp(LocalizationManager.GetText("시스템 미준비", "System not ready")); return; }

        int lv  = GetLevel(e.type);
        int cap = e.levelCap;
        if (cap >= 0 && lv >= cap) { ShowReasonTemp(LocalizationManager.GetText("최대 레벨입니다.", "Max level reached")); return; }

        // TwoStep 확률 상한(50%p)
        if (e.type == ShopItemType.TwoStepChance && prestige.GetTwoStepChance() >= 0.5f - 1e-6f)
        { ShowReasonTemp(LocalizationManager.GetText("최대치에 도달했습니다.", "Maximum value reached")); return; }

        int need = GetNextCost(e.type);
        int have = prestige.GetPrestigePoints();
        if (need <= 0) { ShowReasonTemp(LocalizationManager.GetText("구매 불가", "Cannot purchase")); return; }
        if (have < need){ ShowReasonTemp(LocalizationManager.GetText("포인트가 부족합니다.", "Not enough points")); return; }

        bool ok = TryBuy(e.type);
        if (!ok){ ShowReasonTemp(LocalizationManager.GetText("구매 실패", "Purchase failed")); return; }

        HideReasonImmediate();
        RefreshEntry(e);
    }

    // ───────────────────────── 갱신 ─────────────────────────
    public void RefreshAll()
    {
        foreach (var e in entries) RefreshEntry(e);
    }

    void RefreshEntry(EntryUI e)
    {
        if (e == null) return;

        if (e.combinedLabel) e.combinedLabel.text = ComposeName(e.type);

        int lv = GetLevel(e.type);
        int cap = e.levelCap;
        int cost = GetNextCost(e.type);

        bool isMaxByCap     = (cap >= 0 && lv >= cap);
        bool isMaxByEffect  = (e.type == ShopItemType.TwoStepChance) && (prestige.GetTwoStepChance() >= 0.5f - 1e-6f);
        bool isMax = isMaxByCap || isMaxByEffect;

        if (e.levelText) e.levelText.text = (cap < 0) ? $"Lv. {Mathf.Max(0, lv)}" : $"Lv. {Mathf.Clamp(lv,0,cap)} / {cap}";
        if (e.costText)
        {
            string value = ComposeValue(e.type);
            e.costText.text = (isMax || cost <= 0) ? value : $"{value}\n{cost:N0} point";
        }

        if (e.iconTarget) e.iconTarget.sprite = isMax ? e.maxIconSprite : e.upgradeIconSprite;
        if (e.buyOrUpgradeButton) e.buyOrUpgradeButton.interactable = !isMax;
    }

    // ───────────────────────── 표시 문자열 ─────────────────────────
    string ComposeName(ShopItemType t) => t switch
    {
        ShopItemType.IncomeMultiplier      => LocalizationManager.GetText("초당 수익 배수", "Income Multiplier"),
        ShopItemType.TwoStepChance         => LocalizationManager.GetText("합성 +2단 확률", "Merge +2 Chance"),
        ShopItemType.StartGoldMultiplier   => LocalizationManager.GetText("환생 시작 자금", "Prestige Start Gold"),
        ShopItemType.PrestigePointGain     => LocalizationManager.GetText(" 환생 포인트 획득", "Prestige Point Gain"),
        ShopItemType.ManualSpawnMaxPlus    => LocalizationManager.GetText("두루마리 최대치", "Scroll Max"),
        ShopItemType.ManualSpawnSpeedPlus  => LocalizationManager.GetText("두루마리 쿨다운", "Scroll Cooldown"),
        ShopItemType.AutoMergeSpeedPlus    => LocalizationManager.GetText("자동 합성 주기", "Auto Merge Interval"),
        ShopItemType.AutoSpawnSpeedPlus    => LocalizationManager.GetText("자동 소환 주기", "Auto Spawn Interval"),
        ShopItemType.FieldMaxPlus          => LocalizationManager.GetText("필드 슬롯", "Field Slots"),
        ShopItemType.ClickBonusPlus        => LocalizationManager.GetText("클릭 보너스(배수)", "Click Bonus (Multiplier)"),
        ShopItemType.OfflineRewardPlus     => LocalizationManager.GetText("오프라인 보상", "Offline Reward"),
        ShopItemType.OfflineMaxTimePlus    => LocalizationManager.GetText("오프라인 상한", "Offline Max Time"),
        _ => LocalizationManager.GetText("업그레이드", "Upgrade")
    };

    string ComposeValue(ShopItemType t)
    {
        switch (t)
        {
            case ShopItemType.IncomeMultiplier:
                { double mul = prestige.GetIncomeMultiplier(); double inc=(mul-1.0)*100.0; return $"+{inc:0.#}%"; }
            case ShopItemType.TwoStepChance:
                { float p = prestige.GetTwoStepChance(); return $"{p*100f:0.#}%"; }
            case ShopItemType.StartGoldMultiplier:
                { double mul = prestige.GetStartGoldMultiplier(); return $"x{mul:0.00}"; }
            case ShopItemType.PrestigePointGain:
                { double mul = prestige.GetPrestigePointGainMul(); double inc=(mul-1.0)*100.0; return $"+{inc:0.#}%"; }

            case ShopItemType.ManualSpawnMaxPlus:
                { int add = prestige.GetManualSpawnMaxPlus(); return $"+{add}개"; }
            case ShopItemType.ManualSpawnSpeedPlus:
                { double m = prestige.GetManualSpawnIntervalMul(); return $"-{(1.0-m)*100.0:0.#}%"; }
            case ShopItemType.AutoMergeSpeedPlus:
                { double m = prestige.GetAutoMergeIntervalMul();  return $"-{(1.0-m)*100.0:0.#}%"; }
            case ShopItemType.AutoSpawnSpeedPlus:
                { double m = prestige.GetAutoSpawnIntervalMul();  return $"-{(1.0-m)*100.0:0.#}%"; }
            case ShopItemType.FieldMaxPlus:
                { int add = prestige.GetFieldMaxPlus(); return $"+{add}칸"; }
            case ShopItemType.ClickBonusPlus:
                { double mul = prestige.GetClickBonusMul(); return $"x{mul:0.00}"; }
            case ShopItemType.OfflineRewardPlus:
                { double mul = prestige.GetOfflineRewardMul(); return $"x{mul:0.00}"; }
            case ShopItemType.OfflineMaxTimePlus:
                { int sec = prestige.GetOfflineMaxExtraSeconds(); return $"+{sec/60}분"; }
        }
        return "-";
    }

    // ───────────────────────── 매니저 연동 ─────────────────────────
    int GetLevel(ShopItemType t) => t switch
    {
        ShopItemType.IncomeMultiplier      => prestige.GetIncomeLevel(),
        ShopItemType.TwoStepChance         => prestige.GetTwoStepLevel(),
        ShopItemType.StartGoldMultiplier   => prestige.GetStartGoldLevel(),
        ShopItemType.PrestigePointGain     => prestige.GetPrestigeGainLevel(),

        ShopItemType.ManualSpawnMaxPlus    => prestige.GetPlusManualSpawnMaxLevel(),
        ShopItemType.ManualSpawnSpeedPlus  => prestige.GetPlusManualSpeedLevel(),
        ShopItemType.AutoMergeSpeedPlus    => prestige.GetPlusAutoMergeLevel(),
        ShopItemType.AutoSpawnSpeedPlus    => prestige.GetPlusAutoSpawnLevel(),
        ShopItemType.FieldMaxPlus          => prestige.GetPlusFieldMaxLevel(),
        ShopItemType.ClickBonusPlus        => prestige.GetPlusClickBonusLevel(),
        ShopItemType.OfflineRewardPlus     => prestige.GetPlusOfflineRewardLevel(),
        ShopItemType.OfflineMaxTimePlus    => prestige.GetPlusOfflineMaxTimeLevel(),
        _ => 0
    };

    int GetNextCost(ShopItemType t) => t switch
    {
        ShopItemType.IncomeMultiplier      => prestige.GetIncomeNextCost(),
        ShopItemType.TwoStepChance         => prestige.GetTwoStepNextCost(),
        ShopItemType.StartGoldMultiplier   => prestige.GetStartGoldNextCost(),
        ShopItemType.PrestigePointGain     => prestige.GetPrestigeGainNextCost(),

        ShopItemType.ManualSpawnMaxPlus    => prestige.GetPlusManualSpawnMaxCost(),
        ShopItemType.ManualSpawnSpeedPlus  => prestige.GetPlusManualSpeedCost(),
        ShopItemType.AutoMergeSpeedPlus    => prestige.GetPlusAutoMergeCost(),
        ShopItemType.AutoSpawnSpeedPlus    => prestige.GetPlusAutoSpawnCost(),
        ShopItemType.FieldMaxPlus          => prestige.GetPlusFieldMaxCost(),
        ShopItemType.ClickBonusPlus        => prestige.GetPlusClickBonusCost(),
        ShopItemType.OfflineRewardPlus     => prestige.GetPlusOfflineRewardCost(),
        ShopItemType.OfflineMaxTimePlus    => prestige.GetPlusOfflineMaxTimeCost(),
        _ => int.MaxValue
    };

    bool TryBuy(ShopItemType t) => t switch
    {
        ShopItemType.IncomeMultiplier      => prestige.TryBuyIncome(),
        ShopItemType.TwoStepChance         => prestige.TryBuyTwoStep(),
        ShopItemType.StartGoldMultiplier   => prestige.TryBuyStartGold(),
        ShopItemType.PrestigePointGain     => prestige.TryBuyPrestigeGain(),

        ShopItemType.ManualSpawnMaxPlus    => prestige.TryBuyPlusManualSpawnMax(),
        ShopItemType.ManualSpawnSpeedPlus  => prestige.TryBuyPlusManualSpeed(),
        ShopItemType.AutoMergeSpeedPlus    => prestige.TryBuyPlusAutoMerge(),
        ShopItemType.AutoSpawnSpeedPlus    => prestige.TryBuyPlusAutoSpawn(),
        ShopItemType.FieldMaxPlus          => prestige.TryBuyPlusFieldMax(),
        ShopItemType.ClickBonusPlus        => prestige.TryBuyPlusClickBonus(),
        ShopItemType.OfflineRewardPlus     => prestige.TryBuyPlusOfflineReward(),
        ShopItemType.OfflineMaxTimePlus    => prestige.TryBuyPlusOfflineMaxTime(),
        _ => false
    };

    // helpers
    void ShowReasonTemp(string msg)
    {
        if (!reasonLabel) return;
        HideReasonImmediate();
        reasonLabel.text = msg;
        reasonLabel.gameObject.SetActive(true);
        _reasonRoutine = StartCoroutine(HideReasonAfter(reasonShowSeconds));
    }
    System.Collections.IEnumerator HideReasonAfter(float sec){ yield return new WaitForSecondsRealtime(sec); HideReasonImmediate(); }
    void HideReasonImmediate(){ if(!reasonLabel) return; if(_reasonRoutine!=null){ StopCoroutine(_reasonRoutine); _reasonRoutine=null; } reasonLabel.text=""; reasonLabel.gameObject.SetActive(false); }
}
