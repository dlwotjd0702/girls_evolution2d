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

    void Awake()
    {
        if (!economy) economy = FindObjectOfType<EconomyManager>(true);
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
                durationText.SetText($"오프라인: {timeStr}\n(최대 {FormatDuration(payload.appliedSeconds)}만 적용)");
            }
            else
            {
                durationText.SetText($"오프라인: {timeStr}");
            }
        }

        UpdateAdButtonVisual();

        if (panelRoot) panelRoot.SetActive(true);
    }

    string FormatDuration(double seconds)
    {
        var span = TimeSpan.FromSeconds(Math.Max(0, seconds));
        if (span.TotalHours >= 1.0)
            return $"{(int)span.TotalHours}시간 {span.Minutes}분";
        if (span.TotalMinutes >= 1.0)
            return $"{(int)span.TotalMinutes}분 {span.Seconds}초";
        return $"{Mathf.Max(1, (int)Math.Round((float)span.TotalSeconds))}초";
    }

    void UpdateAdButtonVisual()
    {
        if (claimAdButton == null) return;

        bool adsRemoved = PremiumCurrencyManager.Instance != null && PremiumCurrencyManager.Instance.AdsRemoved;
        bool showButton = adService != null && !adsRemoved;
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
        double amount = currentPayload.totalReward * Math.Max(1f, multiplier);
        if (economy != null && amount > 0)
            economy.AddGold(amount);

        hasPayload = false;
        if (panelRoot) panelRoot.SetActive(false);
    }
}

