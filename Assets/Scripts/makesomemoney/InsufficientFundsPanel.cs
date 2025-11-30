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
    private static string DefaultGemRewardText => LocalizationManager.GetText("광고 보상 보석 5개", "Ad Reward: 5 Gems");

    // Keep a reference to the delegate used for the ad ready changed event so
    // we can properly unsubscribe on disable. Lambdas create new delegates
    // each time, so removing a similar lambda would not detach the original handler.
    private Action<bool> adReadyChangedHandler;

    // Keep a reference to the handler subscribed to the ad removal event so it can be unsubscribed.
    private Action<bool> adsRemovedHandler;

    [Header("Cooldown")]
    [Tooltip("광고 제거 상품을 구매한 유저에게 부족 패널을 다시 보여주기까지의 쿨타임 (초 단위). 기본 30분.")]
    [SerializeField] private float cooldownSeconds = 1800f;
    private float lastShownTime = -99999f;

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

        // 패널을 띄워도 되는지(광고 준비/쿨타임)를 먼저 검사
        if (!CanShowPanel())
            return;

        messageText?.SetText(LocalizationManager.GetText("골드가 부족합니다", "Not enough gold"));

        double perSec = economy ? Math.Max(0.0, economy.GetGoldPerSecEstimate()) : 0.0;
        double reward = perSec * 60.0 * 10.0; // 10분치
        // 최소 300G 보장
        if (reward < 300.0) reward = 300.0;
        rewardText?.SetText($"+{EconomyManager.FormatAbbrev(reward)}");

        UpdateAdButtonVisual();
        panelRoot.SetActive(true);
        lastShownTime = Time.unscaledTime;
    }

    public void Hide(){ if (panelRoot) panelRoot.SetActive(false); }

    // ───────── Internals ─────────
    bool HasAdsRemoved()
    {
        var pcm = PremiumCurrencyManager.Instance;
        return pcm != null && pcm.AdsRemoved;
    }

    /// <summary>
    /// 광고 제거 미구매 유저에 대해, 광고 준비 상태를 확인.
    /// </summary>
    bool IsAdReadyForNonRemoved()
    {
        if (adService == null) return false;

        try
        {
            return adService.IsRewardedReady();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[InsufficientFundsPanel] IsRewardedReady() 체크 실패: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 현재 상황에서 부족 패널을 띄워도 되는지 여부를 판단.
    /// - 광고 제거 미구매: 광고가 준비되어 있을 때만 true
    /// - 광고 제거 구매: 내부 쿨타임이 지난 경우에만 true
    /// </summary>
    bool CanShowPanel()
    {
        if (HasAdsRemoved())
        {
            // 광고 제거 유저는 지정된 쿨타임이 지난 경우에만 표시
            if (Time.unscaledTime - lastShownTime < cooldownSeconds)
                return false;
            return true;
        }

        // 광고 제거 미구매 유저는 광고 준비 여부에 따라 표시
        return IsAdReadyForNonRemoved();
    }

    void UpdateAdButtonVisual()
    {
        if (watchAdButton == null) return;

        bool adsRemoved = HasAdsRemoved();

        if (adsRemoved)
        {
            // 광고 제거 상품을 구매한 경우: "그냥 받기" 버튼으로 항상 활성화
            watchAdButton.gameObject.SetActive(true);
            watchAdButton.interactable = true;
            if (watchAdIcon)
                watchAdIcon.sprite = adReadySprite; // 여기에 "그냥 받기" 스프라이트를 설정해둘 예정
            return;
        }

        bool showButton = adService != null;
        watchAdButton.gameObject.SetActive(showButton);
        if (!showButton)
        {
            if (watchAdIcon) watchAdIcon.sprite = adNotReadySprite;
            return;
        }

        // 안전하게 IsRewardedReady 호출 (초기화 전일 수 있음)
        bool ready = IsAdReadyForNonRemoved();

        watchAdButton.interactable = ready;
        if (watchAdIcon)
            watchAdIcon.sprite = ready ? adReadySprite : adNotReadySprite;
    }

    void OnClickWatchAd()
    {
        // 광고 제거 유저: 바로 보상 지급 + 패널 닫기
        if (HasAdsRemoved())
        {
            double perSec = economy ? Math.Max(0.0, economy.GetGoldPerSecEstimate()) : 0.0;
            double reward = perSec * 60.0 * 5.0; // 5분치
            if (reward < 300.0) reward = 300.0;  // 최소 300G 보장
            if (economy != null && reward > 0) economy.AddGold(reward);
            Hide();
            return;
        }

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
            double reward = perSec * 60.0 * 5.0; // 5분치
            if (reward < 300.0) reward = 300.0;  // 최소 300G 보장
            if (economy != null && reward > 0) economy.AddGold(reward);
            Hide();
        });
    }

    public void ShowGeneric(string title, string msg)
    {
        if (!panelRoot) return;
        string text = !string.IsNullOrEmpty(msg) ? msg : (title ?? LocalizationManager.GetText("안내", "Notice"));
        messageText?.SetText(text);
        rewardText?.SetText(string.Empty);
        UpdateAdButtonVisual();
        panelRoot.SetActive(true);
    }

    public void ShowGemShortage(long need, long have)
    {
        if (!panelRoot) return;

        // 패널을 띄워도 되는지(광고 준비/쿨타임)를 먼저 검사
        if (!CanShowPanel())
            return;

        messageText?.SetText(LocalizationManager.GetText("보석이 부족합니다", "Not enough gems"));
        rewardText?.SetText(DefaultGemRewardText);
        UpdateAdButtonVisual();
        panelRoot.SetActive(true);
        lastShownTime = Time.unscaledTime;
    }
}
