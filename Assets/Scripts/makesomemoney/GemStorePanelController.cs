// GemStorePanelController.cs
// - 보석 상점 탭 전용 컨트롤러
// - PremiumCurrencyManager.GetPriceString / Purchase 사용
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GemStorePanelController : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public string productId;                 // ex) "gems_small"
        public int    grantAmountPreview = 0;    // UI 미리보기(실제 지급은 PremiumCurrencyManager 설정 사용)
        public TextMeshProUGUI titleText;        // "보석 80개"
        public TextMeshProUGUI priceText;        // 현지화 가격
        public Button buyButton;
    }

    public PremiumCurrencyManager premium;
    public Entry[] entries;

    void Awake()
    {
        if (!premium) premium = PremiumCurrencyManager.Instance ?? FindObjectOfType<PremiumCurrencyManager>(true);

        if (entries != null)
        {
            foreach (var e in entries)
            {
                var entry = e;
                if (entry.buyButton != null)
                {
                    entry.buyButton.onClick.RemoveAllListeners();
                    entry.buyButton.onClick.AddListener(() => OnClickBuy(entry.productId));
                }
            }
        }
    }

    void OnEnable()
    {
        RefreshAll();
    }

    public void RefreshAll()
    {
        if (premium == null || entries == null) return;

        foreach (var e in entries)
        {
            if (e.titleText != null)
            {
                var amountText = (e.grantAmountPreview > 0) ? e.grantAmountPreview.ToString("N0") : "?";
                e.titleText.text = $"보석 {amountText}개";
            }
            if (e.priceText != null)
            {
                var price = premium.GetPriceString(e.productId);
                e.priceText.text = string.IsNullOrEmpty(price) ? "..." : price;
            }
        }
    }

    void OnClickBuy(string productId)
    {
        if (premium == null) return;
        premium.Purchase(productId, ok =>
        {
            // 필요시 성공/실패 UX 추가
            if (!ok) Debug.LogWarning($"[GemStore] Purchase failed: {productId}");
        });
    }
}
