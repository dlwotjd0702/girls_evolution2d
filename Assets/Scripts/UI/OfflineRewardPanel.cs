using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OfflineRewardPanel : MonoBehaviour
{
    [Serializable]
    public struct Payload
    {
        public double rawSeconds;
        public double appliedSeconds;
        public double perSecondIncome;
        public double multiplier;
        public double totalReward;
    }

    [Header("Refs")]
    [SerializeField] private EconomyManager economy;
    [SerializeField] private PremiumCurrencyManager premiumCurrency;
    [SerializeField] private MonoBehaviour adServiceBehaviour;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI rewardText; // 보상 금액 표시
    [SerializeField] private TextMeshProUGUI durationText; // 오프라인 시간 표시 (선택)
    [SerializeField] private Button claimButton; // 일반 받기 버튼
    [SerializeField] private Button claimAdButton; // 광고 보고 2배 받기 버튼
    [SerializeField] private Image adButtonIcon; // 광고 버튼 아이콘 (광고 시청/광고 준비중 스프라이트)
    [SerializeField] private Sprite adReadySprite; // 광고 시청 스프라이트
    [SerializeField] private Sprite adNotReadySprite; // 광고 준비중 스프라이트

    [Header("Settings")]
    [Tooltip("광고로 추가 수령되는 배수")]
    [SerializeField] private float adRewardMultiplier = 2f;

    private IAdOfferService adService;
    private Action<bool> adReadyHandler;
    private Payload currentPayload;
    private bool hasPayload = false;
    private bool isCouponReward = false; // 쿠폰 보상인지 여부
    private long couponGemReward = 0; // 쿠폰 보석 보상

    void Awake()
    {
        if (!economy) economy = FindObjectOfType<EconomyManager>(true);
        if (!premiumCurrency) premiumCurrency = FindObjectOfType<PremiumCurrencyManager>(true);
        BindButtons();
        BindAdService();
        if (panelRoot) panelRoot.SetActive(false);
    }

    void OnEnable()
    {
        BindAdService();
        UpdateAdButtonVisual();
    }

    void OnDisable()
    {
        if (adService != null && adReadyHandler != null)
        {
            try { adService.OnRewardedReadyChanged -= adReadyHandler; } catch {}
        }
        adReadyHandler = null;
    }

    void BindButtons()
    {
        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(() => GrantReward(1f));
        }
        if (claimAdButton != null)
        {
            claimAdButton.onClick.RemoveAllListeners();
            claimAdButton.onClick.AddListener(OnClickClaimAd);
        }
    }

    void BindAdService()
    {
        adService = adServiceBehaviour as IAdOfferService;
        if (adService != null)
        {
            if (adReadyHandler != null)
            {
                try { adService.OnRewardedReadyChanged -= adReadyHandler; } catch {}
            }
            adReadyHandler = _ => UpdateAdButtonVisual();
            adService.OnRewardedReadyChanged += adReadyHandler;
        }
    }

    public void Show(Payload payload)
    {
        if (payload.totalReward <= 0) return;
        currentPayload = payload;
        hasPayload = true;
        isCouponReward = false;
        couponGemReward = 0;

        // 보상 금액 표시
        if (rewardText != null)
        {
            rewardText.SetText($"+{EconomyManager.FormatAbbrev(payload.totalReward)}");
        }

        // 오프라인 시간 표시 (선택)
        if (durationText != null)
        {
            string timeStr = FormatDuration(payload.rawSeconds);
            if (payload.appliedSeconds + 0.5 < payload.rawSeconds)
            {
                durationText.SetText(LocalizationManager.GetText(
                    $"오프라인: {timeStr}\n(최대 {FormatDuration(payload.appliedSeconds)}만 적용)",
                    $"Offline: {timeStr}\n(Max {FormatDuration(payload.appliedSeconds)} applied)"
                ));
            }
            else
            {
                durationText.SetText(LocalizationManager.GetText(
                    $"오프라인: {timeStr}",
                    $"Offline: {timeStr}"
                ));
            }
        }

        UpdateAdButtonVisual();

        if (panelRoot) panelRoot.SetActive(true);
    }
    
    /// <summary>
    /// 쿠폰 보상을 표시합니다
    /// </summary>
    public void ShowCouponReward(Payload payload, CouponReward couponReward)
    {
        // 골드 또는 보석 중 하나만 있으면 됨
        bool hasGold = payload.totalReward > 0;
        bool hasGem = !couponReward.isGoldReward && couponReward.rewardAmount > 0;
        
        if (!hasGold && !hasGem) return;
        
        currentPayload = payload;
        hasPayload = true;
        isCouponReward = true;
        couponGemReward = hasGem ? (long)couponReward.rewardAmount : 0;

        // 보상 금액 표시 (골드 또는 보석 중 하나)
        if (rewardText != null)
        {
            string rewardStr = "";
            if (hasGold)
            {
                rewardStr = $"+{EconomyManager.FormatAbbrev(payload.totalReward)}";
            }
            else if (hasGem)
            {
                rewardStr = $"+{couponReward.rewardAmount:N0} Gem";
            }
            rewardText.SetText(rewardStr);
        }

        // 쿠폰 보상임을 표시
        if (durationText != null)
        {
            durationText.SetText(LocalizationManager.GetText(
                "쿠폰 보상",
                "Coupon Reward"
            ));
        }
        
        // 광고 버튼 업데이트 (보석만 있어도 광고 2배 받기 가능)
        UpdateAdButtonVisual();

        // 광고 버튼 업데이트 (골드 보상이 있을 때만 광고 2배 받기 가능)
        UpdateAdButtonVisual();

        if (panelRoot) panelRoot.SetActive(true);
    }

    string FormatDuration(double seconds)
    {
        var span = TimeSpan.FromSeconds(Math.Max(0, seconds));
        if (span.TotalHours >= 1.0)
            return LocalizationManager.GetText(
                $"{(int)span.TotalHours}시간 {span.Minutes}분",
                $"{(int)span.TotalHours}h {span.Minutes}m"
            );
        if (span.TotalMinutes >= 1.0)
            return LocalizationManager.GetText(
                $"{(int)span.TotalMinutes}분 {span.Seconds}초",
                $"{(int)span.TotalMinutes}m {span.Seconds}s"
            );
        return LocalizationManager.GetText(
            $"{Mathf.Max(1, (int)Math.Round((float)span.TotalSeconds))}초",
            $"{Mathf.Max(1, (int)Math.Round((float)span.TotalSeconds))}s"
        );
    }

    void UpdateAdButtonVisual()
    {
        if (claimAdButton == null) return;

        bool adsRemoved = PremiumCurrencyManager.Instance != null && PremiumCurrencyManager.Instance.AdsRemoved;
        // 골드 또는 보석 보상이 있을 때 광고 버튼 표시
        bool hasGoldReward = currentPayload.totalReward > 0;
        bool hasGemReward = isCouponReward && couponGemReward > 0;
        bool hasAnyReward = hasGoldReward || hasGemReward;
        bool showButton = adService != null && !adsRemoved && hasAnyReward;
        claimAdButton.gameObject.SetActive(showButton);

        if (!showButton)
        {
            if (adButtonIcon) adButtonIcon.sprite = adNotReadySprite;
            return;
        }

        bool ready = false;
        try
        {
            ready = adService.IsRewardedReady();
        }
        catch (Exception e)
        {
            Debug.LogError($"[OfflineRewardPanel] Error checking ad readiness: {e.Message}");
            ready = false;
        }

        claimAdButton.interactable = ready;
        if (adButtonIcon)
        {
            adButtonIcon.sprite = ready ? adReadySprite : adNotReadySprite;
        }
    }

    void OnClickClaimAd()
    {
        if (!hasPayload) return;
        bool adsRemoved = PremiumCurrencyManager.Instance != null && PremiumCurrencyManager.Instance.AdsRemoved;
        if (adsRemoved || adService == null)
        {
            GrantReward(adRewardMultiplier);
            return;
        }

        if (!adService.IsRewardedReady())
        {
            adService.LoadRewarded();
            return;
        }

        adService.ShowRewarded(() =>
        {
            GrantReward(adRewardMultiplier);
        });
    }

    void GrantReward(float multiplier)
    {
        if (!hasPayload) return;
        
        // 골드 보상 지급 (광고 2배 받기 적용)
        double goldAmount = currentPayload.totalReward * Math.Max(1f, multiplier);
        if (economy != null && goldAmount > 0)
        {
            economy.AddGold(goldAmount);
        }
        
        // 쿠폰 보상인 경우 보석도 지급 (광고 2배 받기 적용)
        if (isCouponReward && couponGemReward > 0 && premiumCurrency != null)
        {
            long gemAmount = (long)(couponGemReward * Math.Max(1f, multiplier));
            premiumCurrency.AddGems(gemAmount);
        }

        hasPayload = false;
        isCouponReward = false;
        couponGemReward = 0;
        if (panelRoot) panelRoot.SetActive(false);
    }
}

