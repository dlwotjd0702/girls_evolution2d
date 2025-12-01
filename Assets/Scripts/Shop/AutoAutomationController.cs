using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AutoAutomationController : MonoBehaviour
{
    public enum AutoType { AutoMerge, AutoSpawn }
    public enum PurchaseCurrency { Gold, Gem } // 인스펙터에서 선택

    [Header("Config")]
    public AutoType type;
    [Tooltip("업그레이드 결제 통화 선택 (Gold/Gem). Gem은 Economy에 TryBuy...WithGems(...) API가 있을 때만 실제 구매 시도.")]
    public PurchaseCurrency purchaseCurrency = PurchaseCurrency.Gold;

    [Header("Refs")]
    public EconomyManager economy;
    [SerializeField] private PremiumCurrencyManager premiumCurrency; // 보석 관리자
    [Tooltip("선택: 보석 부족/골드 부족 시 띄울 패널(광고/상점 유도)")]
    public InsufficientFundsPanel insufficientPanel;

    // 선택: 환생 상점 영구 단축 표시용(PrestigeShopManager 페이사드)
    private object _prestigeShop;            // PrestigeShopManager.Instance
    private MethodInfo _miGetAutoMergeMul;   // double GetAutoMergeIntervalMul()
    private MethodInfo _miGetAutoSpawnMul;   // double GetAutoSpawnIntervalMul()

    [Header("Texts (TMP)")]
    public TextMeshProUGUI combinedLabel; // "자동 합성(3.2s) • -15%"
    public TextMeshProUGUI levelText;     // "Lv. x / y"
    public TextMeshProUGUI costText;      // 다음 비용(N0), MAX면 "-"

    [Header("Buy/Upgrade (Icon Only)")]
    public Button buyOrUpgradeButton;
    public Image  buyIconTarget;
    public Sprite buyIconSprite;      // Lv0
    public Sprite upgradeIconSprite;  // 1..Cap-1
    public Sprite maxIconSprite;      // Cap

    [Header("Toggle (Single Button + Icon)")]
    public Button toggleButton;       // 한 개 버튼으로 온/오프
    public Image  toggleIconTarget;   // 온/오프 이미지 교체
    public Sprite toggleOnSprite;
    public Sprite toggleOffSprite;

    [Header("Reason Label (Fallback)")]
    public TextMeshProUGUI reasonLabel;
    public float reasonShowSeconds = 1.15f;
    Coroutine _reasonRoutine;
    private static string GemAdHint => LocalizationManager.GetText("광고 시청 보상으로 보석 5개를 받을 수 있어요!", "Watch ads to get 5 gems as a reward!");

    [Header("Gem Cost Curve (independent from gold)")]
    [Tooltip("자동 합성 보석 업그레이드 기본값")]
    [SerializeField] private int   autoMergeGemBase  = 20;
    [SerializeField] private float autoMergeGemGrow  = 1.35f;
    [Tooltip("자동 소환 보석 업그레이드 기본값")]
    [SerializeField] private int   autoSpawnGemBase  = 25;
    [SerializeField] private float autoSpawnGemGrow  = 1.35f;

    void Awake()
    {
        if (!economy) economy = FindObjectOfType<EconomyManager>();
        if (premiumCurrency == null) premiumCurrency = FindObjectOfType<PremiumCurrencyManager>(true);

        if (buyOrUpgradeButton) buyOrUpgradeButton.onClick.AddListener(OnClickBuyOrUpgrade);
        if (toggleButton)       toggleButton.onClick.AddListener(OnClickToggle);

        if (reasonLabel) reasonLabel.gameObject.SetActive(false);

        CachePrestigeShop();
    }

    void OnEnable()
    {
        if (economy!=null)
        {
            economy.OnGoldChanged    += HandleGoldChanged;
            economy.OnUpgradeChanged += HandleUpgradeChanged;
        }
        Refresh();
    }
    void OnDisable()
    {
        if (economy!=null)
        {
            economy.OnGoldChanged    -= HandleGoldChanged;
            economy.OnUpgradeChanged -= HandleUpgradeChanged;
        }
    }
    void HandleGoldChanged(double _) => Refresh();
    void HandleUpgradeChanged() => Refresh();

    public void Refresh()
    {
        if (!economy) return;

        int  lv  = (type==AutoType.AutoMerge) ? economy.GetAutoMergeUpgradeLevel()
                                              : economy.GetAutoSpawnUpgradeLevel();
        int  cap = (type==AutoType.AutoMerge) ? economy.GetAutoMergeCap()
                                              : economy.GetAutoSpawnCap();
        bool on  = (type==AutoType.AutoMerge) ? economy.IsAutoMergeOn()
                                              : economy.IsAutoSpawnOn();

        // ── 표시 값(현재 간격 + 환생 영구 단축 %) ──
        string title = (type==AutoType.AutoMerge) ? LocalizationManager.GetText("자동 합성", "Auto Merge") : LocalizationManager.GetText("자동 소환", "Auto Spawn");
        string value;
        if (lv <= 0) value = LocalizationManager.GetText("잠김", "Locked");
        else
        {
            // UI에는 토글 상태와 상관없이 "현재 업그레이드/환생 기준 이론상 간격"을 보여준다.
            float iv = (type==AutoType.AutoMerge) ? economy.GetAutoMergeIntervalForDisplay()
                                                  : economy.GetAutoSpawnIntervalForDisplay();
            value = (iv >= float.MaxValue*0.5f) ? "-" : $"{iv:0.0}s";
        }

        string perm = GetPrestigeAutoReducePercentText();
        if (combinedLabel) combinedLabel.text = string.IsNullOrEmpty(perm)
            ? $"{title}({value})"
            : $"{title}({value}) • {perm}";

        // 레벨/코스트
        if (levelText) levelText.text = FormatLvCap(lv, cap);
        double nextCost = (type==AutoType.AutoMerge) ? economy.GetAutoMergeNextCost()
                                                     : economy.GetAutoSpawnNextCost();
        if (costText)
        {
            if (double.IsInfinity(nextCost))
            {
                costText.text = "-";
            }
            else if (purchaseCurrency == PurchaseCurrency.Gem)
            {
                // 보석 비용: 골드와 독립적인 완만한 곡선 사용 (항상 정수)
                long gemCost = GetGemCostForAuto(type);
                costText.text = $"{gemCost:N0} gems";
            }
            else
            {
                costText.text = $"{nextCost:N0} G";
            }
        }

        // 아이콘 & 상호작용
        if (buyIconTarget)
        {
            if      (lv <= 0)        buyIconTarget.sprite = buyIconSprite;
            else if (lv >= cap)      buyIconTarget.sprite = maxIconSprite;
            else                     buyIconTarget.sprite = upgradeIconSprite;
        }
        if (buyOrUpgradeButton) buyOrUpgradeButton.interactable = (lv < cap);

        // 🔸 온/오프 버튼: 게임오브젝트는 항상 켜두고, 구매 전에는 비활성/아이콘 숨김
        if (toggleButton)     toggleButton.interactable = lv > 0;       // 잠금 시 클릭 불가
        if (toggleIconTarget) toggleIconTarget.enabled  = lv > 0;       // 잠금 시 아이콘 비표시
        if (toggleIconTarget) toggleIconTarget.sprite   = on ? toggleOnSprite : toggleOffSprite;
    }

    void OnClickBuyOrUpgrade()
    {
        if (!economy) { ShowReason(LocalizationManager.GetText("시스템 미준비", "System not ready")); return; }

        int lv  = (type==AutoType.AutoMerge) ? economy.GetAutoMergeUpgradeLevel()
                                             : economy.GetAutoSpawnUpgradeLevel();
        int cap = (type==AutoType.AutoMerge) ? economy.GetAutoMergeCap()
                                             : economy.GetAutoSpawnCap();
        if (lv >= cap) { ShowReason(LocalizationManager.GetText("최대 레벨입니다.", "Max level reached")); return; }

        double needGold = (type==AutoType.AutoMerge) ? economy.GetAutoMergeNextCost()
                                                     : economy.GetAutoSpawnNextCost();

        // ── 통화 분기 ──
        if (purchaseCurrency == PurchaseCurrency.Gold)
        {
            bool ok = (type==AutoType.AutoMerge) ? economy.TryBuyAutoMergeUpgrade()
                                                 : economy.TryBuyAutoSpawnUpgrade();

            if (!ok)
            {
                // 골드 부족 → 패널로 유도(광고/보석 상점)
                if (insufficientPanel) insufficientPanel.ShowForGoldShortage(needGold, economy.GetGold());
                else
                {
                    if (economy.GetGold() < needGold) ShowReason(LocalizationManager.GetText("골드가 부족합니다.", "Not enough gold."));
                    else ShowReason(LocalizationManager.GetText("구매가 불가합니다.", "Cannot purchase."));
                }
                return;
            }
        }
        else // PurchaseCurrency.Gem
        {
            if (premiumCurrency == null)
            {
                ShowReason(LocalizationManager.GetText("보석 시스템 미준비", "Gem system not ready"));
                return;
            }

            // 보석 비용: 골드와 독립적인 완만한 곡선 사용 (항상 정수)
            long gemCost = GetGemCostForAuto(type);

            // 보석 차감 시도
            if (!premiumCurrency.TrySpendGems(gemCost))
            {
                // 보석 부족 시 insufficientPanel 표시
                if (insufficientPanel != null)
                {
                    long have = premiumCurrency.GetGems();
                    insufficientPanel.ShowGemShortage(gemCost, have);
                }
                else
                {
                    ShowReason($"보석이 부족합니다.\n{GemAdHint}");
                }
                return;
            }

            // 보석 차감 성공 시 골드 차감 없이 레벨만 올리기
            // lv와 cap은 이미 메서드 시작 부분에서 선언됨
            if (lv >= cap)
            {
                // 최대 레벨 도달 시 보석 환불
                premiumCurrency.AddGems(gemCost);
                ShowReason(LocalizationManager.GetText("최대 레벨입니다.", "Max level reached."));
                return;
            }

            // 골드 차감 없이 레벨만 증가 (보석 구매 전용 메서드 사용)
            bool ok = (type==AutoType.AutoMerge) ? economy.TryBuyAutoMergeUpgradeWithGems()
                                                : economy.TryBuyAutoSpawnUpgradeWithGems();

            if (!ok)
            {
                // 구매 실패 시 보석 환불
                premiumCurrency.AddGems(gemCost);
                ShowReason(LocalizationManager.GetText("구매가 불가합니다.", "Cannot purchase."));
                return;
            }
        }

        HideReason();
        Refresh();
    }

    void OnClickToggle()
    {
        if (!economy) { ShowReason(LocalizationManager.GetText("시스템 미준비", "System not ready")); return; }

        int lv = (type==AutoType.AutoMerge) ? economy.GetAutoMergeUpgradeLevel()
                                            : economy.GetAutoSpawnUpgradeLevel();
        if (lv <= 0) { ShowReason(LocalizationManager.GetText("먼저 구매로 언락하세요.", "Please unlock by purchasing first.")); return; }

        bool on = (type==AutoType.AutoMerge) ? economy.IsAutoMergeOn()
                                             : economy.IsAutoSpawnOn();

        if (type==AutoType.AutoMerge) economy.SetAutoMergeOn(!on);
        else                          economy.SetAutoSpawnOn(!on);

        HideReason();
        Refresh();
    }

    // "Lv. 현재 / 최대" 포맷
    string FormatLvCap(int lv, int cap)
    {
        if (cap <= 0) return $"Lv. {lv}";
        lv = Mathf.Clamp(lv, 0, cap);
        return $"Lv.{lv}/{cap}";
    }

    // ───────────────────── Prestige 연동(표시 전용) ─────────────────────
    void CachePrestigeShop()
    {
        try
        {
            var t = FindTypeByName("PrestigeShopManager");
            if (t == null) return;
            var pi = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            _prestigeShop = pi?.GetValue(null, null);
            _miGetAutoMergeMul = t.GetMethod("GetAutoMergeIntervalMul", BindingFlags.Public | BindingFlags.Instance);
            _miGetAutoSpawnMul = t.GetMethod("GetAutoSpawnIntervalMul", BindingFlags.Public | BindingFlags.Instance);
        }
        catch { }
    }

    long GetGemCostForAuto(AutoType t)
    {
        if (economy == null) return 0;

        int lv = (t == AutoType.AutoMerge)
            ? economy.GetAutoMergeUpgradeLevel()
            : economy.GetAutoSpawnUpgradeLevel();

        lv = Mathf.Max(0, lv);

        double baseCost = (t == AutoType.AutoMerge) ? autoMergeGemBase : autoSpawnGemBase;
        double grow     = (t == AutoType.AutoMerge) ? autoMergeGemGrow : autoSpawnGemGrow;

        double raw = baseCost * Math.Pow(grow, lv);
        long gems  = (long)Math.Max(1, Math.Round(raw));
        return gems;
    }

    string GetPrestigeAutoReducePercentText()
    {
        try
        {
            if (_prestigeShop == null) return "";
            double mul =
                (type==AutoType.AutoMerge)
                ? Convert.ToDouble(_miGetAutoMergeMul?.Invoke(_prestigeShop, null) ?? 1.0)
                : Convert.ToDouble(_miGetAutoSpawnMul?.Invoke(_prestigeShop, null) ?? 1.0);

            double reducePct = (1.0 - Mathf.Clamp01((float)mul)) * 100.0;
            if (reducePct <= 0.0001) return "";
            return $"-{reducePct:0.#}%";
        }
        catch { return ""; }
    }

    static Type FindTypeByName(string name)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(name);
            if (t != null) return t;
            try
            {
                foreach (var tt in asm.GetTypes())
                    if (tt.Name == name) return tt;
            }
            catch { }
        }
        return null;
    }

    // ───────────────────── Gem/Panel helpers ─────────────────────
    // Reason helpers (패널 없을 땐 라벨 fallback)
    void ShowReason(string msg)
    {
        if (insufficientPanel && !string.IsNullOrEmpty(msg))
        {
            // 골드/보석 부족 상황이 아니면 범용 메시지로
            insufficientPanel.ShowGeneric(LocalizationManager.GetText("안내", "Notice"), msg);
            return;
        }
        if (!reasonLabel) return;
        HideReason();
        reasonLabel.text = msg;
        reasonLabel.gameObject.SetActive(true);
        _reasonRoutine = StartCoroutine(HideAfter(reasonShowSeconds));
    }
    System.Collections.IEnumerator HideAfter(float sec){ yield return new WaitForSecondsRealtime(sec); HideReason(); }
    void HideReason(){ if(!reasonLabel) return; if(_reasonRoutine!=null){ StopCoroutine(_reasonRoutine); _reasonRoutine=null; } reasonLabel.text=""; reasonLabel.gameObject.SetActive(false); }
}
