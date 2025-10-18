// Assets/Scripts/makesomemoney/PremiumCurrencyManager.cs
#pragma warning disable 0618
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing; // v4 API (v5에서도 호환)

public class PremiumCurrencyManager : MonoBehaviour, IStoreListener, ISaveable
{
    public static PremiumCurrencyManager Instance { get; private set; }

    [Header("Balance")]
    [SerializeField] private long gems = 0;
    public long GetGems() => gems;
    public event Action<long> OnGemsChanged;

    [Header("Products")]
    [Tooltip("스토어 상품ID와 지급량 매핑")]
    [SerializeField] private List<GemProduct> products = new()
    {
        new GemProduct{ productId="gems_small",  type=ProductType.Consumable, grantAmount=80  },
        new GemProduct{ productId="gems_medium", type=ProductType.Consumable, grantAmount=500 },
        new GemProduct{ productId="gems_large",  type=ProductType.Consumable, grantAmount=1200},
    };

    [Serializable] public class GemProduct
    {
        public string productId;
        public ProductType type = ProductType.Consumable;
        public int grantAmount = 0;
    }

    // IAP core
    private IStoreController _controller;
    private IExtensionProvider _extensions;

    // 가격 캐시
    private readonly Dictionary<string,string> _price = new();

    // UI용 이벤트 (카탈로그/가격표 준비됨)
    public event Action OnCatalogReady;

    private const string PP_GEMS = "GEMS_BALANCE";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // 세이브/백업 로드
        if (!HasSaveManager()) gems = (long)PlayerPrefs.GetFloat(PP_GEMS, 0);

        InitIAP();
        OnGemsChanged?.Invoke(gems);
    }

    // ─────────── Public API ───────────
    public void AddGems(long amount)
    {
        if (amount <= 0) return;
        gems = Math.Max(0, gems + amount);
        OnGemsChanged?.Invoke(gems);
        Persist();
    }
    public bool TrySpendGems(long amount)
    {
        if (amount <= 0) return true;
        if (gems < amount) return false;
        gems -= amount;
        OnGemsChanged?.Invoke(gems);
        Persist();
        return true;
    }
    public void SetGems(long amount)
    {
        gems = Math.Max(0, amount);
        OnGemsChanged?.Invoke(gems);
        Persist();
    }

    public string GetLocalizedPrice(string productId)
    {
        if (string.IsNullOrEmpty(productId)) return "";
        if (_price.TryGetValue(productId, out var p)) return p;

        if (_controller != null)
        {
            var prod = _controller.products.WithID(productId);
            if (prod != null && prod.metadata != null)
            {
                var s = prod.metadata.localizedPriceString;
                _price[productId] = s;
                return s;
            }
        }
        return "";
    }

    public void Purchase(string productId)
    {
        if (_controller == null) { Debug.LogWarning("[IAP] Not initialized."); return; }
        if (string.IsNullOrEmpty(productId)) { Debug.LogWarning("[IAP] Empty product id."); return; }

        var prod = _controller.products.WithID(productId);
        if (prod == null || !prod.availableToPurchase)
        {
            Debug.LogWarning("[IAP] Product not available: " + productId);
            return;
        }
        _controller.InitiatePurchase(productId);
    }

    public void RestorePurchases()
    {
#if UNITY_IOS || UNITY_STANDALONE_OSX
        var apple = _extensions?.GetExtension<IAppleExtensions>();
        apple?.RestoreTransactions(result => Debug.Log("[IAP] Restore: " + result));
#else
        Debug.Log("[IAP] RestorePurchases is iOS/macOS only.");
#endif
    }

    // ─────────── IAP Init ───────────
    void InitIAP()
    {
        var module  = StandardPurchasingModule.Instance();
        var builder = ConfigurationBuilder.Instance(module);

        foreach (var gp in products)
            if (!string.IsNullOrEmpty(gp.productId))
                builder.AddProduct(gp.productId, gp.type);

        UnityPurchasing.Initialize(this, builder);
    }

    // ─────────── IStoreListener ───────────
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        _controller = controller;
        _extensions = extensions;

        foreach (var gp in products)
        {
            var p = controller.products.WithID(gp.productId);
            if (p != null && p.metadata != null)
                _price[gp.productId] = p.metadata.localizedPriceString;
        }
        Debug.Log("[IAP] Initialized.");
        OnCatalogReady?.Invoke();
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError("[IAP] Initialize failed: " + error);
    }
    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError($"[IAP] Initialize failed: {error} - {message}");
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
    {
        var id = e.purchasedProduct.definition.id;
        int amount = 0;
        foreach (var gp in products) if (gp.productId == id) { amount = gp.grantAmount; break; }

        if (amount > 0)
        {
            AddGems(amount);
            Debug.Log($"[IAP] Purchase OK: {id}, +{amount} gems");
        }
        else
        {
            Debug.LogWarning("[IAP] Unknown product: " + id);
        }
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogWarning($"[IAP] Purchase failed: {product?.definition?.id} - {failureReason}");
    }

    // ─────────── Save hooks ───────────
    void Persist()
    {
        if (!HasSaveManager())
        {
            PlayerPrefs.SetFloat(PP_GEMS, gems);
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
        }
        catch { return false; }
    }

    public void CollectSaveData(SaveData d)
    {
        TrySetLong(d, "gems", gems);
        TrySetLong(d, "diamonds", gems);
        TrySetLong(d, "premiumCurrency", gems);
    }
    public void ApplyLoadedData(SaveData d)
    {
        long v = TryGetLong(d, "gems", long.MinValue);
        if (v == long.MinValue) v = TryGetLong(d, "diamonds", long.MinValue);
        if (v == long.MinValue) v = TryGetLong(d, "premiumCurrency", long.MinValue);

        gems = (v == long.MinValue) ? (long)PlayerPrefs.GetFloat(PP_GEMS, 0) : Math.Max(0, v);
        OnGemsChanged?.Invoke(gems);
    }

    static void TrySetLong(object obj, string name, long value)
    {
        var f = obj.GetType().GetField(name, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance);
        if (f == null) return;
        if (f.FieldType == typeof(long)) f.SetValue(obj, value);
        else if (f.FieldType == typeof(int)) f.SetValue(obj, (int)Mathf.Clamp(value, int.MinValue, int.MaxValue));
    }
    static long TryGetLong(object obj, string name, long fb)
    {
        var f = obj.GetType().GetField(name, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance);
        if (f == null) return fb;
        try { return Convert.ToInt64(f.GetValue(obj)); } catch { return fb; }
    }
}
#pragma warning restore 0618
