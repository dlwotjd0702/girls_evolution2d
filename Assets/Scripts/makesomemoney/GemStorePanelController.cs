using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GemStorePanelController : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public string productId;
        public int    grantGems;
        public TextMeshProUGUI titleText;   // "80 Gems"
        public TextMeshProUGUI priceText;   // "₩1,200"
        public Button buyButton;
    }

    public PremiumCurrencyManager premium;
    public List<Entry> entries = new();

    void OnEnable()
    {
        if (!premium) premium = FindObjectOfType<PremiumCurrencyManager>(true);
        if (premium != null) premium.OnCatalogReady += Refresh;
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
        if (e.titleText) e.titleText.text = $"{e.grantGems:N0} Gems";

        if (e.priceText) e.priceText.text = premium.GetLocalizedPrice(e.productId);

        if (e.buyButton)
        {
            e.buyButton.onClick.RemoveAllListeners();
            e.buyButton.onClick.AddListener(() => premium.Purchase(e.productId));
        }
    }
}