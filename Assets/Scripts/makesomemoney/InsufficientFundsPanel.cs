using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InsufficientFundsPanel : MonoBehaviour
{
    [Header("Refs")]
    public EconomyManager economy;
    public PremiumCurrencyManager premium;
    public RewardedAdsManager_AdMob ads;

    [Header("Root")]
    public GameObject root;

    [Header("Common UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI messageText;

    [Header("Ad Section (Gold Only)")]
    public GameObject adSectionRoot;
    public Button     watchAdButton;
    public Image      watchAdIcon;
    public Sprite     adReadySprite;
    public Sprite     adNotReadySprite;
    public TextMeshProUGUI rewardText; // “+12.3K G(5분)”

    [Header("Gem Section")]
    public GameObject gemSectionRoot;
    public Button     openGemStoreButton;

    [Header("Close")]
    public Button closeButton;

    const int REWARD_SECONDS = 300; // 5분

    void Awake()
    {
        if (root) root.SetActive(false);
        if (closeButton) closeButton.onClick.AddListener(() => { if (root) root.SetActive(false); });

        if (watchAdButton) watchAdButton.onClick.AddListener(OnClickWatchAd);
        if (ads) ads.OnReadyChanged += _ => RefreshAdState();
    }

    void OnEnable() { RefreshAdState(); }

    // ───────────────────── Public API ─────────────────────
    public void ShowForGoldShortage(double need, double have)
    {
        if (!root) return;
        root.SetActive(true);

        if (titleText)   titleText.text = "골드가 부족합니다";
        if (messageText) messageText.text = $"필요: {need:N0} / 보유: {have:N0}";

        // 광고 섹션 활성, 보석 섹션 비활성
        if (adSectionRoot)  adSectionRoot.SetActive(true);
        if (gemSectionRoot) gemSectionRoot.SetActive(false);

        UpdateRewardText();
        RefreshAdState();
    }

    public void ShowForGemShortage(long need, long have)
    {
        if (!root) return;
        root.SetActive(true);

        if (titleText)   titleText.text = "보석이 부족합니다";
        if (messageText) messageText.text = $"필요: {need:N0} / 보유: {have:N0}";

        // 광고 섹션 비활성(보석에는 광고 오퍼 없음), 보석 상점 버튼만
        if (adSectionRoot)  adSectionRoot.SetActive(false);
        if (gemSectionRoot) gemSectionRoot.SetActive(true);
    }

    public void ShowGeneric(string title, string msg)
    {
        if (!root) return;
        root.SetActive(true);

        if (titleText)   titleText.text = title;
        if (messageText) messageText.text = msg;

        if (adSectionRoot)  adSectionRoot.SetActive(false);
        if (gemSectionRoot) gemSectionRoot.SetActive(false);
    }

    // ───────────────────── Internals ─────────────────────
    void UpdateRewardText()
    {
        if (!economy || rewardText == null) return;
        var perSec = economy.GetGoldPerSecEstimate();
        var reward = perSec * REWARD_SECONDS;
        rewardText.text = $"+{FormatCompact(reward)} G (5분)";
    }

    void RefreshAdState()
    {
        if (!adSectionRoot || !watchAdButton || !watchAdIcon || ads == null) return;

        bool ready = ads.IsReady;
        watchAdButton.interactable = ready;
        watchAdIcon.sprite = ready ? adReadySprite : adNotReadySprite;
    }

    void OnClickWatchAd()
    {
        if (ads == null || economy == null) return;

        var perSec = economy.GetGoldPerSecEstimate();
        var reward = perSec * REWARD_SECONDS;

        ads.ShowRewarded(() =>
        {
            if (reward > 0) economy.AddGold(reward);
            if (root) root.SetActive(false);
        });
    }

    string FormatCompact(double v)
    {
        double a = Mathf.Abs((float)v);
        if (a < 1_000d) return v.ToString("0");
        if (a < 1_000_000d) return (v/1_000d).ToString("0.0") + "K";
        if (a < 1_000_000_000d) return (v/1_000_000d).ToString("0.0") + "M";
        if (a < 1_000_000_000_000d) return (v/1_000_000_000d).ToString("0.0") + "B";
        return (v/1_000_000_000_000d).ToString("0.0") + "T";
    }
}
