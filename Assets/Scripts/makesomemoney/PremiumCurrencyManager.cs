// ============================================================================
// PremiumCurrencyManager.cs  (젬 라벨 단일화)
// - 젬 텍스트: 단일 TMP_Text만 인스펙터에서 연결
// - 나머지 IAP v5 로직은 기존 그대로
// ============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using System.Linq;
using TMPro;

public class PremiumCurrencyManager : MonoBehaviour, ISaveable
{
    public static PremiumCurrencyManager Instance { get; private set; }

    [Serializable]
    public class GemProduct
    {
        public string productId;
        public ProductType type = ProductType.Consumable;
        public int grantAmount = 0;
    }

    [Header("Products")]
    public List<GemProduct> products = new()
    {
        new GemProduct{ productId="gems_small",  type=ProductType.Consumable, grantAmount=80  },
        new GemProduct{ productId="gems_medium", type=ProductType.Consumable, grantAmount=500 },
        new GemProduct{ productId="gems_large",  type=ProductType.Consumable, grantAmount=1200},
        // Non-consumable product to permanently remove ads from the game. Set grantAmount=0 since no currency is awarded.
        new GemProduct{ productId=REMOVE_ADS_ID, type=ProductType.NonConsumable, grantAmount=0 },
    };

    [Header("Balance")]
    [SerializeField] private long gems = 0;
    public event Action<long> OnGemsChanged;
    public event Action       OnCatalogReady;

    // ===== Remove Ads (Non-consumable purchase) =====
    /// <summary>
    /// Identifier for the non-consumable product that permanently removes ads.
    /// </summary>
    public const string REMOVE_ADS_ID = "remove_ads";

    /// <summary>
    /// Persistent flag indicating whether the user has purchased ad removal. When true, reward videos and other ads should be disabled.
    /// </summary>
    [SerializeField] private bool adsRemoved = false;

    /// <summary>
    /// Event fired when the adsRemoved state changes (true when the player has removed ads).
    /// </summary>
    public event Action<bool> OnAdsRemovedChanged;

    // PlayerPrefs key used for storing the ad removal state when no save manager is present.
    const string PP_ADS_REMOVED = "ADS_REMOVED_FLAG";

    [Header("UI Label (Optional)")]
    [SerializeField] private TextMeshProUGUI gemLabel; // 인스펙터에서 하나만 연결

    const string PP_GEMS = "GEMS_BALANCE_V2";

    private StoreController storeController;
    private readonly Dictionary<string,string> priceCache = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // SaveManager가 없으면 PlayerPrefs에서 복구
        if (!HasSaveManager())
        {
            var s = PlayerPrefs.GetString(PP_GEMS, "0");
            if (long.TryParse(s, out var v)) gems = Math.Max(0, v);
            // Restore the ad removal flag from PlayerPrefs
            adsRemoved = PlayerPrefs.GetInt(PP_ADS_REMOVED, 0) != 0;
        }
        NotifyAndPersist(); // 초기 라벨 갱신
        InitializeIAP();
    }

    // ===== Public API =====
    public long  GetGems() => gems;
    public void  SetGems(long amount){ gems = Math.Max(0, amount); NotifyAndPersist(); }
    public void  AddGems(long amount){ if (amount<=0) return; gems += amount; NotifyAndPersist(); }
    public bool  TrySpendGems(long amount){ if (amount<=0) return true; if (gems<amount) return false; gems -= amount; NotifyAndPersist(); return true; }

    public void Purchase(string productId)
    {
        if (string.IsNullOrEmpty(productId)) return;
        if (storeController == null) { Debug.LogWarning("[IAP] Store not initialized."); return; }
        storeController.PurchaseProduct(productId);
    }

    public string GetLocalizedPrice(string productId)
    {
        if (string.IsNullOrEmpty(productId)) return "";
        if (priceCache.TryGetValue(productId, out var s) && !string.IsNullOrEmpty(s)) return s;

        var p = storeController?.GetProductById(productId);
        if (p?.metadata != null)
        {
            s = p.metadata.localizedPriceString;
            priceCache[productId] = s;
            return s;
        }
        return "";
    }

    public void RestorePurchases()
    {
#if UNITY_IOS || UNITY_STANDALONE_OSX
        storeController?.RestoreTransactions((success, error) => {});
#else
        Debug.Log("[IAP] RestorePurchases is only for iOS/macOS.");
#endif
    }

    // ===== IAP v5 초기화/이벤트 (기존과 동일) =====
    async void InitializeIAP()
    {
        storeController = UnityIAPServices.StoreController();

        storeController.OnProductsFetched     += OnProductsFetched;
        storeController.OnProductsFetchFailed += OnProductsFetchFailed;
        storeController.OnPurchasePending     += OnPurchasePending;
        storeController.OnPurchaseConfirmed   += OnPurchaseConfirmed;
        storeController.OnPurchaseFailed      += OnPurchaseFailed;

        try { await storeController.Connect(); }
        catch (Exception ex)
        {
            Debug.LogError("[IAP] Connection failed: " + ex.Message);
            OnCatalogReady?.Invoke();
            return;
        }

        var definitions = new List<ProductDefinition>();
        foreach (var gp in products)
            if (!string.IsNullOrEmpty(gp.productId))
                definitions.Add(new ProductDefinition(gp.productId, gp.type));

        if (definitions.Count > 0) storeController.FetchProducts(definitions);
        else OnCatalogReady?.Invoke();
    }

    void OnProductsFetched(List<Product> fetchedProducts)
    {
        foreach (var prod in fetchedProducts)
            if (prod?.metadata != null)
                priceCache[prod.definition.id] = prod.metadata.localizedPriceString;

        OnCatalogReady?.Invoke();
        storeController.FetchPurchases();
    }

    void OnProductsFetchFailed(ProductFetchFailed failure)
    {
        Debug.LogWarning("[IAP] Products fetch failed: " + failure);
        OnCatalogReady?.Invoke();
    }

    void OnPurchasePending(PendingOrder pendingOrder)
    {
        if (storeController != null && pendingOrder != null)
            storeController.ConfirmPurchase(pendingOrder);
    }

    void OnPurchaseConfirmed(Order order)
    {
        if (order == null) return;
        var items = order.CartOrdered?.Items();
        if (items == null) return;

        foreach (var item in items)
        {
            var productId = item?.Product?.definition?.id;
            if (string.IsNullOrEmpty(productId)) continue;

            // Handle gem grants and special products
            var grant = 0;
            foreach (var gp in products)
            {
                if (gp.productId == productId)
                {
                    grant = gp.grantAmount;
                    break;
                }
            }

            // If the player purchased the remove ads product, set the flag and skip gem awarding
            if (productId == REMOVE_ADS_ID)
            {
                SetAdsRemoved(true);
                Debug.Log("[IAP] Ads have been permanently removed.");
            }
            else if (grant > 0)
            {
                AddGems(grant);
                Debug.Log($"[IAP] +{grant} gems ({productId})");
            }
            else
            {
                Debug.LogWarning("[IAP] Unknown product id: " + productId);
            }
        }
    }

    void OnPurchaseFailed(FailedOrder failedOrder)
    {
        var productId = failedOrder?.CartOrdered?.Items()?.FirstOrDefault()?.Product?.definition?.id;
        var reason = failedOrder?.FailureReason;
        Debug.LogWarning($"[IAP] Purchase failed: {productId} - {reason}");
    }

    // ===== Save / Load =====
    public void CollectSaveData(SaveData d)
    {
        TrySetLong(d, "gems", gems);
        TrySetLong(d, "diamonds", gems);
        TrySetLong(d, "premiumCurrency", gems);

        // Persist the ad removal state as a long (1=true, 0=false)
        TrySetLong(d, "adsRemoved", adsRemoved ? 1L : 0L);
    }
    public void ApplyLoadedData(SaveData d)
    {
        long v = TryGetLong(d, "gems", long.MinValue);
        if (v==long.MinValue) v = TryGetLong(d, "diamonds", long.MinValue);
        if (v==long.MinValue) v = TryGetLong(d, "premiumCurrency", long.MinValue);

        if (v!=long.MinValue) gems = Math.Max(0, v);
        else
        {
            var s = PlayerPrefs.GetString(PP_GEMS, "0");
            if (long.TryParse(s, out var p)) gems = Math.Max(0, p);
        }

        // Load ad removal state from SaveData or PlayerPrefs fallback
        long adsFlag = TryGetLong(d, "adsRemoved", long.MinValue);
        if (adsFlag != long.MinValue)
        {
            adsRemoved = adsFlag != 0;
        }
        else
        {
            adsRemoved = PlayerPrefs.GetInt(PP_ADS_REMOVED, 0) != 0;
        }
        NotifyAndPersist();
    }

    // ===== Helpers =====
    void NotifyAndPersist()
    {
        if (gemLabel) gemLabel.text = $"{gems:N0}";
        OnGemsChanged?.Invoke(gems);

        if (!HasSaveManager())
        {
            PlayerPrefs.SetString(PP_GEMS, gems.ToString());
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Sets the ad removal flag and persists it. Fires <see cref="OnAdsRemovedChanged"/> when the state changes.
    /// </summary>
    /// <param name="value">Whether ads should be considered removed.</param>
    public void SetAdsRemoved(bool value)
    {
        if (adsRemoved == value) return;
        adsRemoved = value;
        OnAdsRemovedChanged?.Invoke(adsRemoved);
        PersistAdsRemoved();
    }

    /// <summary>
    /// Returns true if the player has purchased the remove-ads non-consumable.
    /// </summary>
    public bool AdsRemoved => adsRemoved;

    void PersistAdsRemoved()
    {
        if (!HasSaveManager())
        {
            PlayerPrefs.SetInt(PP_ADS_REMOVED, adsRemoved ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
    bool HasSaveManager()
    {
        try
        {
            var t = typeof(SaveManager);
            var pi = t.GetProperty("Instance", System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);
            return pi?.GetValue(null, null) != null;
        } catch { return false; }
    }
    static void TrySetLong(object obj, string name, long value)
    {
        var f = obj.GetType().GetField(name, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance);
        if (f==null) return;
        if (f.FieldType==typeof(long)) f.SetValue(obj, value);
        else if (f.FieldType==typeof(int)) f.SetValue(obj, (int)Mathf.Clamp(value, int.MinValue, int.MaxValue));
    }
    static long TryGetLong(object obj, string name, long fb)
    {
        var f = obj.GetType().GetField(name, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance);
        if (f==null) return fb;
        try { return Convert.ToInt64(f.GetValue(obj)); } catch { return fb; }
    }
}
