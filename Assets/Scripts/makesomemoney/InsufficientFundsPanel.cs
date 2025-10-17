// InsufficientFundsPanel.cs
// - 부족 재화 안내 + 광고 보상 / 보석 상점 이동
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InsufficientFundsPanel : MonoBehaviour
{
    [Header("Root")]
    public GameObject root;

    [Header("Texts")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI hintText;

    [Header("Buttons")]
    public Button closeButton;
    public Button watchAdButton;
    public Button openGemStoreButton;

    [Header("Config")]
    public int adGoldReward = 500;
    public int adGemReward  = 5;

    public PremiumCurrencyManager premium;
    public EconomyManager economy;

    void Awake()
    {
        if (!premium) premium = PremiumCurrencyManager.Instance ?? FindObjectOfType<PremiumCurrencyManager>(true);
        if (!economy) economy = FindObjectOfType<EconomyManager>(true);

        if (closeButton)         closeButton.onClick.AddListener(Hide);
        if (watchAdButton)       watchAdButton.onClick.AddListener(OnClickWatchAd);
        if (openGemStoreButton)  openGemStoreButton.onClick.AddListener(OnClickOpenGemStore);

        if (root) root.SetActive(false);
    }

    public void ShowForGoldShortage(double need, double have)
    {
        ShowGeneric("골드가 부족합니다", $"필요: {need:N0} / 보유: {have:N0}", "광고를 보면 즉시 보상을 받을 수 있어요!");
    }

    public void ShowForGemShortage(long need, long have)
    {
        ShowGeneric("보석이 부족합니다", $"필요: {need:N0} / 보유: {have:N0}", "상점에서 보석 팩을 구매하거나, 광고 보상으로 획득해 보세요.");
    }

    public void ShowGeneric(string title, string desc, string hint = "")
    {
        if (titleText) titleText.text = title;
        if (descText)  descText.text  = desc;
        if (hintText)
        {
            hintText.text = hint ?? "";
            hintText.gameObject.SetActive(!string.IsNullOrEmpty(hint));
        }
        if (root) root.SetActive(true);
    }

    public void Hide(){ if (root) root.SetActive(false); }

    void OnClickWatchAd()
    {
        // TODO(ads): SDK 붙이면 보상 콜백에서 지급
        if (economy != null && adGoldReward > 0) economy.AddGold(adGoldReward);
        if (premium != null && adGemReward  > 0) premium.GrantAdRewardGems(adGemReward);
        Hide();
    }

    void OnClickOpenGemStore()
    {
        var shop = FindObjectOfType<GemStorePanelController>(true);
        if (shop != null) shop.gameObject.SetActive(true);
        Hide();
    }
}
