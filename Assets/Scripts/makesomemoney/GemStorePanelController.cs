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
        public string productId;          // "gems_small", "remove_ads" 등
        public int    grantGems;          // 젬 상품 전용(광고 제거는 0)

        [Header("UI Refs")]
        public TextMeshProUGUI titleText; // "80 Gems" 또는 "광고 제거"
        public TextMeshProUGUI priceText; // "₩1,200" (콘솔 가격 자동 반영)
        public Button buyButton;

        [Header("Optional Title Override")]
        public bool   useTitleOverride;   // true면 customTitle 사용
        public string customTitle = "광고 제거";
    }

    public PremiumCurrencyManager premium;
    public List<Entry> entries = new();

    void OnEnable()
    {
        if (!premium) premium = FindObjectOfType<PremiumCurrencyManager>(true);
        if (premium != null) premium.OnCatalogReady += Refresh; // 가격 가져온 뒤 갱신
        Refresh();
    }

    void OnDisable()
    {
        if (premium != null) premium.OnCatalogReady -= Refresh;
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
            if (e.useTitleOverride || isRemoveAds)
                e.titleText.text = string.IsNullOrWhiteSpace(e.customTitle) ? "광고 제거" : e.customTitle;
            else
                e.titleText.text = $"{e.grantGems:N0} Gems";
        }

        // ── 가격(콘솔 메타데이터에서) ─────────────────────────
        if (e.priceText)
            e.priceText.text = premium.GetLocalizedPrice(e.productId); // 콘솔 가격 자동 반영

        // ── 구매 버튼 ─────────────────────────────────────────
        if (e.buyButton)
        {
            e.buyButton.onClick.RemoveAllListeners();
            e.buyButton.onClick.AddListener(() => premium.Purchase(e.productId));
        }
    }
}
