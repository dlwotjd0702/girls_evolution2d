using System;
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
        public string customTitle = "광고 제거";
        
        [Header("Ad Entry")]
        public bool isAdEntry = false;    // true면 광고 시청 Entry
    }

    public PremiumCurrencyManager premium;
    public List<Entry> entries = new();

    [Header("Insufficient Funds Panel")]
    [SerializeField] private InsufficientFundsPanel insufficientPanel;

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
                e.titleText.text = string.IsNullOrWhiteSpace(e.customTitle) ? "광고 제거" : e.customTitle;
            else
                e.titleText.text = $"{e.grantGems:N0} Gems";
        }

        // ── 가격 ─────────────────────────────────────────────
        if (e.priceText)
        {
            if (e.isAdEntry)
                e.priceText.text = "광고 시청";
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
        if (insufficientPanel != null)
        {
            insufficientPanel.ShowGeneric("구매 실패", "보석 구매에 실패했습니다. 다시 시도해주세요.");
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
            if (insufficientPanel != null)
            {
                insufficientPanel.ShowGeneric("광고 미지원", "광고 서비스가 준비되지 않았습니다.");
            }
            return;
        }

        if (!adService.IsRewardedReady())
        {
            if (insufficientPanel != null)
            {
                insufficientPanel.ShowGeneric("광고 준비 중", "광고를 불러오는 중입니다. 잠시 후 다시 시도해주세요.");
            }
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
}
