using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using System.Linq;

// Updated for Unity IAP v5.0
// The previous implementation relied on IStoreListener and synchronous initialization.
// This class now uses the StoreController from UnityIAPServices and asynchronous
// initialization to comply with the Google Play Billing Library v7+ and Apple policies.
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
    };

    [Header("Balance")]
    [SerializeField] private long gems = 0;
    public event Action<long> OnGemsChanged;
    public event Action       OnCatalogReady;

    const string PP_GEMS = "GEMS_BALANCE_V2";

    // Store controller for IAP v5
    private StoreController storeController;

    // 가격 캐시
    private readonly Dictionary<string,string> priceCache = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // SaveManager가 없다면 PlayerPrefs에서 복구
        if (!HasSaveManager())
        {
            var s = PlayerPrefs.GetString(PP_GEMS, "0");
            if (long.TryParse(s, out var v)) gems = Math.Max(0, v);
        }
        OnGemsChanged?.Invoke(gems);
        // Begin asynchronous initialization of the IAP services
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
        if (storeController == null)
        {
            Debug.LogWarning("[IAP] Store not initialized.");
            return;
        }
        storeController.PurchaseProduct(productId);
    }

    public string GetLocalizedPrice(string productId)
    {
        if (string.IsNullOrEmpty(productId)) return "";
        // Return cached price if available
        if (priceCache.TryGetValue(productId, out var s) && !string.IsNullOrEmpty(s))
            return s;

        // Attempt to fetch from the store controller directly
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
        // In IAP v5, restore transactions is handled via StoreController
        storeController?.RestoreTransactions((success, error) => { /* handle restore callback if needed */ });
#else
        Debug.Log("[IAP] RestorePurchases is only for iOS/macOS.");
#endif
    }

    // ===== IAP 초기화 (IAP v5) =====
    // Connects to the store, fetches products, and sets up event handlers.
    async void InitializeIAP()
    {
        // Obtain a store controller instance
        storeController = UnityIAPServices.StoreController();

        // Register event handlers before making API calls
        storeController.OnProductsFetched += OnProductsFetched;
        storeController.OnProductsFetchFailed += OnProductsFetchFailed;
        storeController.OnPurchasePending += OnPurchasePending;
        storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
        storeController.OnPurchaseFailed += OnPurchaseFailed;

        // Connect to the store
        try
        {
            await storeController.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogError("[IAP] Connection failed: " + ex.Message);
            OnCatalogReady?.Invoke();
            return;
        }

        // Build list of product definitions from our gem products
        var definitions = new List<ProductDefinition>();
        foreach (var gp in products)
        {
            if (!string.IsNullOrEmpty(gp.productId))
            {
                definitions.Add(new ProductDefinition(gp.productId, gp.type));
            }
        }

        // Fetch product metadata from the store
        if (definitions.Count > 0)
        {
            storeController.FetchProducts(definitions);
        }
        else
        {
            // Nothing to fetch; still notify that the catalog is ready so UI can react
            OnCatalogReady?.Invoke();
        }
    }

    // ===== IAP v5 event handlers =====
    void OnProductsFetched(List<Product> fetchedProducts)
    {
        // Populate localized price cache
        foreach (var prod in fetchedProducts)
        {
            if (prod?.metadata != null)
            {
                priceCache[prod.definition.id] = prod.metadata.localizedPriceString;
            }
        }
        // Notify that catalog is ready for UI refresh
        OnCatalogReady?.Invoke();

        // Optionally fetch existing purchases (not strictly necessary for consumables)
        storeController.FetchPurchases();
    }

    void OnProductsFetchFailed(ProductFetchFailed failure)
    {
        Debug.LogWarning("[IAP] Products fetch failed: " + failure);
        OnCatalogReady?.Invoke();
    }

    void OnPurchasePending(PendingOrder pendingOrder)
    {
        // Immediately confirm the pending order to finalize the transaction
        if (storeController != null && pendingOrder != null)
        {
            storeController.ConfirmPurchase(pendingOrder);
        }
    }

    void OnPurchaseConfirmed(Order order)
    {
        if (order == null) return;
        // Iterate through items in the confirmed order's cart and grant rewards
        var items = order.CartOrdered?.Items();
        if (items != null)
        {
            foreach (var item in items)
            {
                var productId = item?.Product?.definition?.id;
                if (string.IsNullOrEmpty(productId)) continue;
                var grant = 0;
                foreach (var gp in products)
                {
                    if (gp.productId == productId)
                    {
                        grant = gp.grantAmount;
                        break;
                    }
                }
                if (grant > 0)
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
    }

    void OnPurchaseFailed(FailedOrder failedOrder)
    {
        var productId = failedOrder?.CartOrdered?.Items()?.FirstOrDefault()?.Product?.definition?.id;
        var reason = failedOrder?.FailureReason;
        Debug.LogWarning($"[IAP] Purchase failed: {productId} - {reason}");
    }

    // ===== Save / Load (ISaveable 호환) =====
    public void CollectSaveData(SaveData d)
    {
        TrySetLong(d, "gems", gems);
        TrySetLong(d, "diamonds", gems);
        TrySetLong(d, "premiumCurrency", gems);
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
        OnGemsChanged?.Invoke(gems);
    }

    // ===== Helpers =====
    void NotifyAndPersist()
    {
        OnGemsChanged?.Invoke(gems);
        if (!HasSaveManager())
        {
            PlayerPrefs.SetString(PP_GEMS, gems.ToString());
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
