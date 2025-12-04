using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GemStorePanelController : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        [Header("Catalog")]
        public string productId;          // "gems_small", "remove_ads", "ad_reward" 등
        public int    grantGems;          // 젬 상품 전용(광고 제거는 0, 광고 보상은 5)

        [Header("UI Refs")]
        public TextMeshProUGUI titleText; // "80 Gems" 또는 "광고 제거"
        public TextMeshProUGUI priceText; // "₩1,200" (콘솔 가격 자동 반영) 또는 "광고 시청"
        public Button buyButton;
        public Image  buttonIcon;         // 광고 버튼 아이콘 (광고 Entry 전용)

        [Header("Optional Title Override")]
        public bool   useTitleOverride;   // true면 customTitle 사용
        public string customTitle = "광고 제거"; // This will be localized when used
        
        [Header("Ad Entry")]
        public bool isAdEntry = false;    // true면 광고 시청 Entry
    }

    public PremiumCurrencyManager premium;
    public List<Entry> entries = new();

    [Header("Reason Label")]
    [SerializeField] private TextMeshProUGUI reasonLabel;
    [SerializeField] private float reasonShowSeconds = 1.5f;
    private Coroutine _reasonRoutine;

    [Header("Ad Service")]
    [SerializeField] private MonoBehaviour adServiceBehaviour;
    private IAdOfferService adService;

    [Header("Ad Button Sprites")]
    [SerializeField] private Sprite adReadySprite;      // 광고 준비됨
    [SerializeField] private Sprite adNotReadySprite;    // 광고 준비 중

    [Header("Ad Reward")]
    [SerializeField] private int adGemReward = 5;        // 광고 시청 시 지급할 젬 수

    private Action<bool> adReadyHandler;
    private Action<bool> adsRemovedHandler;

    void Awake()
    {
        if (reasonLabel != null) reasonLabel.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (!premium) premium = FindObjectOfType<PremiumCurrencyManager>(true);
        if (premium != null)
        {
            premium.OnCatalogReady += Refresh; // 가격 가져온 뒤 갱신
            premium.OnPurchaseFailed += OnPurchaseFailed;
        }
        BindAdService(); // Refresh 전에 바인딩
        Refresh();
    }

    void OnDisable()
    {
        if (premium != null)
        {
            premium.OnCatalogReady -= Refresh;
            premium.OnPurchaseFailed -= OnPurchaseFailed;
        }
        UnbindAdService();
        HideReasonImmediate();
    }

    void Refresh()
    {
        if (!premium) return;
        foreach (var e in entries) RefreshEntry(e);
    }

    void RefreshEntry(Entry e)
    {
        if (e == null) return;

        // ── 타이틀 ─────────────────────────────────────────────
        bool isRemoveAds = (e.productId == PremiumCurrencyManager.REMOVE_ADS_ID);
        if (e.titleText)
        {
            if (e.isAdEntry)
                e.titleText.text = $"+{adGemReward:N0} Gems";
            else if (e.useTitleOverride || isRemoveAds)
                e.titleText.text = string.IsNullOrWhiteSpace(e.customTitle) ? LocalizationManager.GetText("광고 제거", "Remove Ads") : e.customTitle;
            else
                e.titleText.text = $"{e.grantGems:N0} Gems";
        }

        // ── 가격 ─────────────────────────────────────────────
        if (e.priceText)
        {
            if (e.isAdEntry)
                e.priceText.text = LocalizationManager.GetText("광고 시청", "Watch Ad");
            else
                e.priceText.text = premium.GetLocalizedPrice(e.productId); // 콘솔 가격 자동 반영
        }

        // ── 구매 버튼 ─────────────────────────────────────────
        if (e.buyButton)
        {
            e.buyButton.onClick.RemoveAllListeners();
            if (e.isAdEntry)
            {
                e.buyButton.onClick.AddListener(() => OnClickWatchAd(e));
            }
            else
            {
                e.buyButton.onClick.AddListener(() => premium.Purchase(e.productId));
            }
        }

        // ── 광고 버튼 아이콘 업데이트 ─────────────────────────
        if (e.isAdEntry && e.buttonIcon != null)
        {
            bool ready = false;
            bool adsRemoved = PremiumCurrencyManager.Instance != null && PremiumCurrencyManager.Instance.AdsRemoved;
            
            if (adsRemoved || adService == null)
            {
                ready = false;
            }
            else
            {
                try
                {
                    ready = adService.IsRewardedReady();
                }
                catch
                {
                    ready = false;
                }
            }
            
            e.buttonIcon.sprite = ready ? adReadySprite : adNotReadySprite;
            if (e.buyButton) e.buyButton.interactable = ready && !adsRemoved;
        }
    }


    void OnPurchaseFailed(string productId, string reason)
    {
        // 모든 구매 버튼 비활성화
        foreach (var e in entries)
        {
            if (e.buyButton != null)
            {
                e.buyButton.interactable = false;
            }
        }
        
        // Reason label로 실패 메시지 표시
        string failMessage = LocalizationManager.GetText(
            "구매에 실패했습니다.\n잠시 후 다시 시도해주세요.", 
            "Purchase failed.\nPlease try again later."
        );
        ShowReasonTemp(failMessage);
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
            adReadyHandler = _ => Refresh();
            adService.OnRewardedReadyChanged += adReadyHandler;
        }

        var pcm = PremiumCurrencyManager.Instance;
        if (pcm != null)
        {
            if (adsRemovedHandler != null)
            {
                try { pcm.OnAdsRemovedChanged -= adsRemovedHandler; } catch {}
            }
            adsRemovedHandler = _ => Refresh();
            pcm.OnAdsRemovedChanged += adsRemovedHandler;
        }
    }

    void UnbindAdService()
    {
        if (adService != null && adReadyHandler != null)
        {
            try { adService.OnRewardedReadyChanged -= adReadyHandler; } catch {}
        }
        adReadyHandler = null;

        var pcm = PremiumCurrencyManager.Instance;
        if (pcm != null && adsRemovedHandler != null)
        {
            try { pcm.OnAdsRemovedChanged -= adsRemovedHandler; } catch {}
        }
        adsRemovedHandler = null;
    }

    void OnClickWatchAd(Entry e)
    {
        var pcm = premium ?? PremiumCurrencyManager.Instance;
        if (pcm != null && pcm.AdsRemoved)
        {
            GrantAdGems();
            return;
        }

        if (adService == null)
        {
            ShowReasonTemp(LocalizationManager.GetText(
                "광고 서비스가 준비되지 않았습니다.", 
                "Ad service is not ready."
            ));
            return;
        }

        if (!adService.IsRewardedReady())
        {
            ShowReasonTemp(LocalizationManager.GetText(
                "광고를 불러오는 중입니다.\n잠시 후 다시 시도해주세요.", 
                "Loading ad.\nPlease try again in a moment."
            ));
            adService.LoadRewarded();
            return;
        }

        adService.ShowRewarded(() =>
        {
            GrantAdGems();
        });
    }

    void GrantAdGems()
    {
        if (adGemReward <= 0) return;
        var pcm = premium ?? PremiumCurrencyManager.Instance ?? FindObjectOfType<PremiumCurrencyManager>(true);
        if (pcm != null)
        {
            pcm.AddGems(adGemReward);
            Refresh();
        }
    }

    // Reason helpers
    void ShowReasonTemp(string msg)
    {
        if (reasonLabel == null) return;
        HideReasonImmediate();
        reasonLabel.text = msg;
        reasonLabel.gameObject.SetActive(true);
        _reasonRoutine = StartCoroutine(HideReasonAfter(reasonShowSeconds));
    }
    
    System.Collections.IEnumerator HideReasonAfter(float sec)
    {
        yield return new WaitForSecondsRealtime(sec);
        HideReasonImmediate();
    }
    
    void HideReasonImmediate()
    {
        if (reasonLabel == null) return;
        if (_reasonRoutine != null)
        {
            StopCoroutine(_reasonRoutine);
            _reasonRoutine = null;
        }
        reasonLabel.text = "";
        reasonLabel.gameObject.SetActive(false);
        
        // Reason이 사라질 때 버튼들 다시 활성화
        foreach (var e in entries)
        {
            if (e.buyButton != null)
            {
                // 광고 Entry는 광고 준비 상태에 따라 활성화
                if (e.isAdEntry)
                {
                    bool ready = false;
                    bool adsRemoved = PremiumCurrencyManager.Instance != null && PremiumCurrencyManager.Instance.AdsRemoved;
                    if (!adsRemoved && adService != null)
                    {
                        try { ready = adService.IsRewardedReady(); } catch { }
                    }
                    e.buyButton.interactable = ready && !adsRemoved;
                }
                else
                {
                    e.buyButton.interactable = true;
                }
            }
        }
    }
}
