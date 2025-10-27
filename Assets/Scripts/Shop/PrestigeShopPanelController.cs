// ====================================================================
// PrestigeShopPanelController.cs (FULL, 통합 PrestigeManager/Facade 호환판)
// - PrestigeShopManager는 MonoBehaviour가 아니므로 FindObjectOfType 제거
// - Awake에서 PrestigeShopManager.Instance로 안전 획득
// - 그 외 로직/UX는 동일
// ====================================================================
using System;
using System.Collections.Generic;
using System.Reflection;
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
    public PrestigeManager     prestige;
    public PrestigeShopManager shop; // Mono가 아님(인스펙터에 안 보임). Awake에서 Instance 할당.

    [Header("Header")]
    [SerializeField] private TextMeshProUGUI pointsText;

    [Header("Reason Label")]
    [SerializeField] private TextMeshProUGUI reasonLabel;
    [SerializeField] private float reasonShowSeconds = 1.15f;
    private Coroutine _reasonRoutine;

    [Header("Entries")]
    public List<EntryUI> entries = new List<EntryUI>();

    private int _lastShownPoints = -1;
    private float _refreshTimer = 0f;
    private const float REFRESH_INTERVAL = 0.25f;

    void Awake()
    {
        if (!prestige) prestige = FindObjectOfType<PrestigeManager>(true);

        // ⬇️ PrestigeShopManager는 Facade(일반 클래스). 정적 인스턴스로 획득.
        shop = PrestigeShopManager.Instance;

        foreach (var e in entries)
        {
            var entry = e; // 클로저 안전
            if (entry.buyOrUpgradeButton != null)
                entry.buyOrUpgradeButton.onClick.AddListener(() => OnClickBuy(entry));
        }

        if (reasonLabel != null) reasonLabel.gameObject.SetActive(false);
    }

    void OnEnable(){ RefreshAll(force:true); _refreshTimer = 0f; }
    void Update()
    {
        _refreshTimer += Time.unscaledDeltaTime;
        if (_refreshTimer >= REFRESH_INTERVAL)
        { _refreshTimer = 0f; RefreshAll(); }
    }

    // ───────────────────────── 클릭 ─────────────────────────
    void OnClickBuy(EntryUI e)
    {
        if (shop == null || prestige == null) { ShowReasonTemp("시스템 미준비"); return; }

        int lv  = GetLevel(e.type);
        int cap = e.levelCap;
        if (cap >= 0 && lv >= cap) { ShowReasonTemp("최대 레벨입니다."); return; }

        // TwoStep 확률 상한 체크(50%p)
        if (e.type == ShopItemType.TwoStepChance && Safe(() => shop.GetTwoStepChance(), 0f) >= 0.5f - 1e-6f)
        { ShowReasonTemp("최대치에 도달했습니다."); return; }

        int need = GetNextCost(e.type);
        int have = GetPrestigePoints();
        if (need <= 0) { ShowReasonTemp("구매 불가"); return; }
        if (have < need){ ShowReasonTemp("포인트가 부족합니다."); return; }

        bool ok = TryBuy(e.type);
        if (!ok){ ShowReasonTemp("구매 실패"); return; }

        HideReasonImmediate();
        RefreshEntry(e);
        RefreshPointsHeader(force:true);
    }

    // ───────────────────────── 갱신 ─────────────────────────
    public void RefreshAll(bool force=false)
    {
        RefreshPointsHeader(force);
        foreach (var e in entries) RefreshEntry(e);
    }

    void RefreshPointsHeader(bool force=false)
    {
        if (!pointsText) return;
        int pts = GetPrestigePoints();
        if (!force && pts == _lastShownPoints) return;
        _lastShownPoints = pts;
        pointsText.text = $"환생 포인트: <b>{pts:N0}</b>";
    }

    void RefreshEntry(EntryUI e)
    {
        if (e == null) return;

        if (e.combinedLabel) e.combinedLabel.text = ComposeName(e.type);

        int lv = GetLevel(e.type);
        int cap = e.levelCap;
        int cost = GetNextCost(e.type);

        bool isMaxByCap = (cap >= 0 && lv >= cap);
        bool isMaxByEffect = (e.type == ShopItemType.TwoStepChance) && (Safe(() => shop.GetTwoStepChance(), 0f) >= 0.5f - 1e-6f);
        bool isMax = isMaxByCap || isMaxByEffect;

        if (e.levelText) e.levelText.text = (cap < 0) ? $"Lv. {Mathf.Max(0, lv)}" : $"Lv. {Mathf.Clamp(lv,0,cap)} / {cap}";
        if (e.costText)
        {
            string value = ComposeValue(e.type);
            e.costText.text = (isMax || cost <= 0) ? value : $"{value}\n{cost:N0}";
        }

        if (e.iconTarget) e.iconTarget.sprite = isMax ? e.maxIconSprite : e.upgradeIconSprite;
        if (e.buyOrUpgradeButton) e.buyOrUpgradeButton.interactable = !isMax;
    }

    // ───────────────────────── 표시 문자열 ─────────────────────────
    string ComposeName(ShopItemType t) => t switch
    {
        ShopItemType.IncomeMultiplier      => "초당 수익 배수",
        ShopItemType.TwoStepChance         => "합성 +2단 확률",
        ShopItemType.StartGoldMultiplier   => "환생 시작 자금",
        ShopItemType.PrestigePointGain     => " 환생 포인트 획득",
        ShopItemType.ManualSpawnMaxPlus    => "수동 소환 최대치",
        ShopItemType.ManualSpawnSpeedPlus  => "수동 소환 쿨다운",
        ShopItemType.AutoMergeSpeedPlus    => "자동 합성 주기",
        ShopItemType.AutoSpawnSpeedPlus    => "자동 소환 주기",
        ShopItemType.FieldMaxPlus          => "필드 슬롯",
        ShopItemType.ClickBonusPlus        => "클릭 보너스(배수)",
        ShopItemType.OfflineRewardPlus     => "오프라인 보상",
        ShopItemType.OfflineMaxTimePlus    => "오프라인 상한",
        _ => "업그레이드"
    };

    string ComposeValue(ShopItemType t)
    {
        if (shop == null) return "-";
        switch (t)
        {
            case ShopItemType.IncomeMultiplier:
                { double mul = Safe(()=>shop.GetIncomeMultiplier(),1.0); double inc=(mul-1.0)*100.0; return $"+{inc:0.#}%"; }
            case ShopItemType.TwoStepChance:
                { float p = Safe(()=>shop.GetTwoStepChance(),0f); return $"{p*100f:0.#}%"; }
            case ShopItemType.StartGoldMultiplier:
                { double mul = Safe(()=>shop.GetStartGoldMultiplier(),1.0); return $"x{mul:0.00}"; }
            case ShopItemType.PrestigePointGain:
                { double mul = Safe(()=>shop.GetPrestigePointGainMul(),1.0); double inc=(mul-1.0)*100.0; return $"+{inc:0.#}%"; }

            case ShopItemType.ManualSpawnMaxPlus:
                { int add = Safe(()=>shop.GetManualSpawnMaxPlus(),0); return $"+{add}개"; }
            case ShopItemType.ManualSpawnSpeedPlus:
                { double m = Safe(()=>shop.GetManualSpawnIntervalMul(),1.0); return $"-{(1.0-m)*100.0:0.#}%"; }
            case ShopItemType.AutoMergeSpeedPlus:
                { double m = Safe(()=>shop.GetAutoMergeIntervalMul(),1.0); return $"-{(1.0-m)*100.0:0.#}%"; }
            case ShopItemType.AutoSpawnSpeedPlus:
                { double m = Safe(()=>shop.GetAutoSpawnIntervalMul(),1.0); return $"-{(1.0-m)*100.0:0.#}%"; }
            case ShopItemType.FieldMaxPlus:
                { int add = Safe(()=>shop.GetFieldMaxPlus(),0); return $"+{add}칸"; }
            case ShopItemType.ClickBonusPlus:
                { double mul = Safe(()=>shop.GetClickBonusMul(),1.0); return $"x{mul:0.00}"; }
            case ShopItemType.OfflineRewardPlus:
                { double mul = Safe(()=>shop.GetOfflineRewardMul(),1.0); return $"x{mul:0.00}"; }
            case ShopItemType.OfflineMaxTimePlus:
                { int sec = Safe(()=>shop.GetOfflineMaxExtraSeconds(),0); return $"+{sec/60}분"; }
        }
        return "-";
    }

    // ───────────────────────── 매니저 연동 ─────────────────────────
    int GetPrestigePoints(){ try{ return prestige? prestige.GetPrestigePoints():0; } catch { return 0; } }

    int GetLevel(ShopItemType t)
    {
        if (shop == null) return 0;
        string f = t switch
        {
            ShopItemType.IncomeMultiplier      => "incomeLv",
            ShopItemType.TwoStepChance         => "twoStepLv",
            ShopItemType.StartGoldMultiplier   => "startGoldLv",
            ShopItemType.PrestigePointGain     => "prestigeGainLv",
            ShopItemType.ManualSpawnMaxPlus    => "plusManualSpawnMaxLv",
            ShopItemType.ManualSpawnSpeedPlus  => "plusManualSpawnSpeedLv",
            ShopItemType.AutoMergeSpeedPlus    => "plusAutoMergeSpeedLv",
            ShopItemType.AutoSpawnSpeedPlus    => "plusAutoSpawnSpeedLv",
            ShopItemType.FieldMaxPlus          => "plusFieldMaxLv",
            ShopItemType.ClickBonusPlus        => "plusClickBonusLv",
            ShopItemType.OfflineRewardPlus     => "plusOfflineRewardLv",
            ShopItemType.OfflineMaxTimePlus    => "plusOfflineMaxTimeLv",
            _ => null
        };
        if (string.IsNullOrEmpty(f)) return 0;
        try {
            var fi = typeof(PrestigeShopManager).GetField(f, BindingFlags.NonPublic|BindingFlags.Instance);
            if (fi!=null && fi.FieldType==typeof(int)) return (int)fi.GetValue(shop);
        } catch {}
        return 0;
    }

    int GetNextCost(ShopItemType t)
    {
        if (shop == null) return int.MaxValue;
        try {
            return t switch
            {
                ShopItemType.IncomeMultiplier      => shop.GetIncomeNextCost(),
                ShopItemType.TwoStepChance         => shop.GetTwoStepNextCost(),
                ShopItemType.StartGoldMultiplier   => shop.GetStartGoldNextCost(),
                ShopItemType.PrestigePointGain     => shop.GetPrestigeGainNextCost(),

                ShopItemType.ManualSpawnMaxPlus    => shop.GetPlusManualSpawnMaxCost(),
                ShopItemType.ManualSpawnSpeedPlus  => shop.GetPlusManualSpeedCost(),
                ShopItemType.AutoMergeSpeedPlus    => shop.GetPlusAutoMergeCost(),
                ShopItemType.AutoSpawnSpeedPlus    => shop.GetPlusAutoSpawnCost(),
                ShopItemType.FieldMaxPlus          => shop.GetPlusFieldMaxCost(),
                ShopItemType.ClickBonusPlus        => shop.GetPlusClickBonusCost(),
                ShopItemType.OfflineRewardPlus     => shop.GetPlusOfflineRewardCost(),
                ShopItemType.OfflineMaxTimePlus    => shop.GetPlusOfflineMaxTimeCost(),
                _ => int.MaxValue
            };
        } catch { return int.MaxValue; }
    }

    bool TryBuy(ShopItemType t)
    {
        if (shop == null) return false;
        try {
            return t switch
            {
                ShopItemType.IncomeMultiplier      => shop.TryBuyIncome(),
                ShopItemType.TwoStepChance         => shop.TryBuyTwoStep(),
                ShopItemType.StartGoldMultiplier   => shop.TryBuyStartGold(),
                ShopItemType.PrestigePointGain     => shop.TryBuyPrestigeGain(),

                ShopItemType.ManualSpawnMaxPlus    => shop.TryBuyPlusManualSpawnMax(),
                ShopItemType.ManualSpawnSpeedPlus  => shop.TryBuyPlusManualSpeed(),
                ShopItemType.AutoMergeSpeedPlus    => shop.TryBuyPlusAutoMerge(),
                ShopItemType.AutoSpawnSpeedPlus    => shop.TryBuyPlusAutoSpawn(),
                ShopItemType.FieldMaxPlus          => shop.TryBuyPlusFieldMax(),
                ShopItemType.ClickBonusPlus        => shop.TryBuyPlusClickBonus(),
                ShopItemType.OfflineRewardPlus     => shop.TryBuyPlusOfflineReward(),
                ShopItemType.OfflineMaxTimePlus    => shop.TryBuyPlusOfflineMaxTime(),
                _ => false
            };
        } catch { return false; }
    }

    // ───────────────────────── helpers ─────────────────────────
    static T Safe<T>(Func<T> f, T fb){ try { return f(); } catch { return fb; } }
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
