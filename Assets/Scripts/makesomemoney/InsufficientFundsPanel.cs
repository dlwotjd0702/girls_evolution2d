// Assets/Scripts/makesomemoney/InsufficientFundsPanel.cs
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InsufficientFundsPanel : MonoBehaviour
{
    [Header("Refs")]
    public EconomyManager economy;
    [Tooltip("보상형 광고 서비스(AdMob 구현 바인딩)")]
    public MonoBehaviour adServiceBehaviour; // IAdOfferService 구현체 컴포넌트 할당
    private IAdOfferService adService;

    [Header("Root")]
    public GameObject panelRoot;

    [Header("Header")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI messageText;

    [Header("Gold Shortage UI")]
    public GameObject goldBlock;
    public TextMeshProUGUI goldNeedHaveText;
    public TextMeshProUGUI goldRewardText;
    public Button watchAdButton;
    public Image  watchAdIcon;
    public Sprite adReadySprite;
    public Sprite adNotReadySprite;

    [Header("Gem Shortage UI")]
    public GameObject gemBlock;
    public TextMeshProUGUI gemNeedHaveText;
    public Button openGemShopButton;

    void Awake()
    {
        if (!economy) economy = FindObjectOfType<EconomyManager>(true);
        if (panelRoot) panelRoot.SetActive(false);
        BindAdService();
    }
    void OnEnable(){ BindAdService(); }

    void BindAdService()
    {
        adService = adServiceBehaviour as IAdOfferService;
        if (watchAdButton != null)
        {
            watchAdButton.onClick.RemoveAllListeners();
            watchAdButton.onClick.AddListener(OnClickWatchAd);
        }
        UpdateAdButtonVisual();
        if (adService != null) adService.OnRewardedReadyChanged += _ => UpdateAdButtonVisual();
    }
    void OnDisable()
    {
        if (adService != null) adService.OnRewardedReadyChanged -= _ => UpdateAdButtonVisual();
    }

    // ───────── Public API (오토/상점에서 호출) ─────────
    public void ShowForGoldShortage(double need, double have)
    {
        if (!panelRoot) return;
        titleText?.SetText("골드가 부족합니다");
        messageText?.SetText("아래 보상을 통해 부족분을 채워보세요.");

        if (goldBlock) goldBlock.SetActive(true);
        if (gemBlock)  gemBlock.SetActive(false);

        goldNeedHaveText?.SetText($"필요: {need:N0} / 보유: {have:N0}");

        double perSec = economy ? Math.Max(0.0, economy.GetGoldPerSecEstimate()) : 0.0;
        double reward = perSec * 60.0 * 5.0; // 5분치
        goldRewardText?.SetText($"+{reward:N0} G (광고)");

        UpdateAdButtonVisual();
        panelRoot.SetActive(true);
    }

    public void ShowForGemShortage(long need, long have)
    {
        if (!panelRoot) return;
        titleText?.SetText("보석이 부족합니다");
        messageText?.SetText("상점에서 보석을 구매하거나, 다른 경로를 이용해주세요.");

        if (goldBlock) goldBlock.SetActive(false);
        if (gemBlock)  gemBlock.SetActive(true);

        gemNeedHaveText?.SetText($"필요: {need:N0} / 보유: {have:N0}");
        panelRoot.SetActive(true);
    }

    public void ShowGeneric(string title, string msg)
    {
        if (!panelRoot) return;
        titleText?.SetText(title ?? "안내");
        messageText?.SetText(msg ?? "");
        if (goldBlock) goldBlock.SetActive(false);
        if (gemBlock)  gemBlock.SetActive(false);
        UpdateAdButtonVisual();
        panelRoot.SetActive(true);
    }

    public void Hide(){ if (panelRoot) panelRoot.SetActive(false); }

    // ───────── Internals ─────────
    void UpdateAdButtonVisual()
    {
        bool ready = adService != null && adService.IsRewardedReady();
        if (watchAdButton) watchAdButton.interactable = ready;
        if (watchAdIcon)
            watchAdIcon.sprite = ready ? adReadySprite : adNotReadySprite;
    }

    void OnClickWatchAd()
    {
        if (adService == null || !adService.IsRewardedReady()) return;

        // 광고 성공 시 5분치 골드 지급
        adService.ShowRewarded(() =>
        {
            double perSec = economy ? Math.Max(0.0, economy.GetGoldPerSecEstimate()) : 0.0;
            double reward = perSec * 60.0 * 5.0;
            if (economy != null && reward > 0) economy.AddGold(reward);
            Hide();
        });
    }
}
