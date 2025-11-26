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

    [Header("Texts")]
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI rewardText;

    [Header("Buttons")]
    public Button watchAdButton;
    public Image  watchAdIcon;
    public Sprite adReadySprite;
    public Sprite adNotReadySprite;
    private const string DefaultGemRewardText = "광고 보상 보석 5개";

    // Keep a reference to the delegate used for the ad ready changed event so
    // we can properly unsubscribe on disable. Lambdas create new delegates
    // each time, so removing a similar lambda would not detach the original handler.
    private Action<bool> adReadyChangedHandler;

    // Keep a reference to the handler subscribed to the ad removal event so it can be unsubscribed.
    private Action<bool> adsRemovedHandler;

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
        // Subscribe once to the OnRewardedReadyChanged event if available
        if (adService != null)
        {
            // Unsubscribe previous handler if one exists
            if (adReadyChangedHandler != null)
            {
                try { adService.OnRewardedReadyChanged -= adReadyChangedHandler; } catch {}
            }
            adReadyChangedHandler = (ready) => UpdateAdButtonVisual();
            adService.OnRewardedReadyChanged += adReadyChangedHandler;
        }

        // Subscribe to the PremiumCurrencyManager's ads removed event so the UI updates when ads are removed
        var pcm = PremiumCurrencyManager.Instance;
        if (pcm != null)
        {
            if (adsRemovedHandler != null)
            {
                try { pcm.OnAdsRemovedChanged -= adsRemovedHandler; } catch {}
            }
            adsRemovedHandler = _ => UpdateAdButtonVisual();
            pcm.OnAdsRemovedChanged += adsRemovedHandler;
        }
    }
    void OnDisable()
    {
        // Properly unsubscribe the stored handler to avoid memory leaks
        if (adService != null && adReadyChangedHandler != null)
        {
            try { adService.OnRewardedReadyChanged -= adReadyChangedHandler; } catch {}
            adReadyChangedHandler = null;
        }

        // Unsubscribe ads removed handler
        var pcm = PremiumCurrencyManager.Instance;
        if (pcm != null && adsRemovedHandler != null)
        {
            try { pcm.OnAdsRemovedChanged -= adsRemovedHandler; } catch {}
            adsRemovedHandler = null;
        }
    }

    // ───────── Public API (오토/상점에서 호출) ─────────
    public void ShowForGoldShortage(double need, double have)
    {
        if (!panelRoot) return;

        // 광고가 준비되지 않았다면 패널 자체를 띄우지 않음
        if (!IsAdReadyForPanel())
        {
            Debug.Log("[InsufficientFundsPanel] 광고 미준비 상태이므로 골드 부족 패널을 표시하지 않습니다.");
            return;
        }

        messageText?.SetText("골드가 부족합니다");

        double perSec = economy ? Math.Max(0.0, economy.GetGoldPerSecEstimate()) : 0.0;
        double reward = perSec * 60.0 * 10.0; // 5분치
        rewardText?.SetText($"+{reward:N0} G");

        UpdateAdButtonVisual();
        panelRoot.SetActive(true);
    }

    public void Hide(){ if (panelRoot) panelRoot.SetActive(false); }

    // ───────── Internals ─────────
    /// <summary>
    /// 부족 패널을 표시해도 되는 광고 준비 상태인지 확인.
    /// - AdsRemoved 상태거나 adService가 없으면 false
    /// - adService.IsRewardedReady()가 true일 때만 true
    /// </summary>
    bool IsAdReadyForPanel()
    {
        // 광고 제거 상품을 구매한 경우 부족 패널을 띄워도 의미가 없으므로 false
        bool adsRemoved = PremiumCurrencyManager.Instance != null && PremiumCurrencyManager.Instance.AdsRemoved;
        if (adsRemoved) return false;

        if (adService == null) return false;

        try
        {
            return adService.IsRewardedReady();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[InsufficientFundsPanel] IsAdReadyForPanel() 체크 실패: {e.Message}");
            return false;
        }
    }

    void UpdateAdButtonVisual()
    {
        if (watchAdButton == null) return;
        bool adsRemoved = PremiumCurrencyManager.Instance != null && PremiumCurrencyManager.Instance.AdsRemoved;
        bool showButton = adService != null && !adsRemoved;
        watchAdButton.gameObject.SetActive(showButton);
        if (!showButton)
        {
            if (watchAdIcon) watchAdIcon.sprite = adNotReadySprite;
            return;
        }

        // 안전하게 IsRewardedReady 호출 (초기화 전일 수 있음)
        bool ready = false;
        try
        {
            if (adService != null)
                ready = adService.IsRewardedReady();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[InsufficientFundsPanel] IsRewardedReady() 호출 실패: {e.Message}");
            ready = false;
        }
        
        watchAdButton.interactable = ready;
        if (watchAdIcon)
            watchAdIcon.sprite = ready ? adReadySprite : adNotReadySprite;
    }

    void OnClickWatchAd()
    {
        if (PremiumCurrencyManager.Instance != null && PremiumCurrencyManager.Instance.AdsRemoved) return;
        if (adService == null)
        {
            watchAdButton?.gameObject.SetActive(false);
            return;
        }

        if (!adService.IsRewardedReady())
        {
            adService.LoadRewarded();
            return;
        }

        adService.ShowRewarded(() =>
        {
            double perSec = economy ? Math.Max(0.0, economy.GetGoldPerSecEstimate()) : 0.0;
            double reward = perSec * 60.0 * 5.0;
            if (economy != null && reward > 0) economy.AddGold(reward);
            Hide();
        });
    }

    public void ShowGeneric(string title, string msg)
    {
        if (!panelRoot) return;
        string text = !string.IsNullOrEmpty(msg) ? msg : (title ?? "안내");
        messageText?.SetText(text);
        rewardText?.SetText(string.Empty);
        UpdateAdButtonVisual();
        panelRoot.SetActive(true);
    }

    public void ShowGemShortage(long need, long have)
    {
        if (!panelRoot) return;

        // 광고 준비 상태일 때만 보석 부족 패널 표시
        if (!IsAdReadyForPanel())
        {
            Debug.Log("[InsufficientFundsPanel] 광고 미준비 상태이므로 보석 부족 패널을 표시하지 않습니다.");
            return;
        }

        messageText?.SetText("보석이 부족합니다");
        rewardText?.SetText(DefaultGemRewardText);
        UpdateAdButtonVisual();
        panelRoot.SetActive(true);
    }
}
