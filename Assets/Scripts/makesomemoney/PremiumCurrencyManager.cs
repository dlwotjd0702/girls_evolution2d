// PremiumCurrencyManager.cs (IAP v4 기반, 누락 메서드 보강 버전)
// - IStoreListener 사용 (v5로 갈아타기 전 임시 운용)
// - 다른 스크립트 호환용 메서드 추가:
//   * GetPriceString(productId)
//   * Purchase(productId, onComplete)
//   * GrantAdRewardGems() / GrantAdRewardGems(int)
// - PlayerPrefs 백업 저장 + ISaveable 연동

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class PremiumCurrencyManager : MonoBehaviour, IStoreListener, ISaveable
{
    public static PremiumCurrencyManager Instance { get; private set; }

    [Header("Balance")]
    [SerializeField] private long gems = 0;
    public event Action<long> OnGemsChanged;
    public long GetGems() => gems;

    [Header("Products")]
    [Tooltip("스토어 상품ID와 지급량 매핑")]
    public List<GemProduct> products = new List<GemProduct>
    {
        new GemProduct{ productId="gems_small",  type=ProductType.Consumable, grantAmount=80  },
        new GemProduct{ productId="gems_medium", type=ProductType.Consumable, grantAmount=500 },
        new GemProduct{ productId="gems_large",  type=ProductType.Consumable, grantAmount=1200},
    };

    [Serializable]
    public class GemProduct
    {
        public string productId;
        public ProductType type = ProductType.Consumable;
        public int grantAmount = 0;
    }

    [Header("Ads (stub)")]
    public int adRewardGemsDefault = 10; // 광고 보상 기본값

    // IAP
    private IStoreController _store;
    private IExtensionProvider _extensions;

    // 가격 캐시
    private readonly Dictionary<string, string> _localizedPrice = new();

    // 구매 콜백 (호환용 Purchase(productId, onComplete))
    private string _pendingProductId;
    private Action<bool> _pendingPurchaseCallback;

    // PlayerPrefs fallback key
    const string PP_GEMS = "GEMS_BALANCE";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // SaveManager가 없으면 PlayerPrefs에서 읽기
        if (!HasSaveManager())
            gems = (long)PlayerPrefs.GetFloat(PP_GEMS, 0);

        InitIAP();
        OnGemsChanged?.Invoke(gems);
    }

    // ───────────── Public API ─────────────
    public void AddGems(long amount)
    {
        if (amount <= 0) return;
        gems = Math.Max(0, gems + amount);
        OnGemsChanged?.Invoke(gems);
        PersistBalance();
    }

    public bool TrySpendGems(long amount)
    {
        if (amount <= 0) return true;
        if (gems < amount) return false;
        gems -= amount;
        OnGemsChanged?.Invoke(gems);
        PersistBalance();
        return true;
    }

    public void SetGems(long amount)
    {
        gems = Math.Max(0, amount);
        OnGemsChanged?.Invoke(gems);
        PersistBalance();
    }

    // --- 기존 내부 구매 API (유지) ---
    public void BuySmall()  => BuyProductId(FindIdByIndex(0));
    public void BuyMedium() => BuyProductId(FindIdByIndex(1));
    public void BuyLarge()  => BuyProductId(FindIdByIndex(2));

    public void BuyProductId(string productId)
    {
        if (string.IsNullOrEmpty(productId))
        {
            Debug.LogWarning("[IAP] Invalid product id.");
            return;
        }
        if (_store == null)
        {
            Debug.LogWarning("[IAP] Store not initialized yet.");
            return;
        }
        _store.InitiatePurchase(productId);
    }

    public void RestorePurchases()
    {
#if UNITY_IOS || UNITY_STANDALONE_OSX
        if (_extensions == null) { Debug.LogWarning("[IAP] Extensions not ready."); return; }
        var apple = _extensions.GetExtension<IAppleExtensions>();
        apple.RestoreTransactions(result => Debug.Log("[IAP] Restore result: " + result));
#else
        Debug.Log("[IAP] RestorePurchases: iOS/macOS 전용입니다.");
#endif
    }

    public string GetLocalizedPrice(string productId)
    {
        if (string.IsNullOrEmpty(productId)) return "";
        if (_localizedPrice.TryGetValue(productId, out var p)) return p;
        if (_store != null)
        {
            var pinfo = _store.products.WithID(productId);
            if (pinfo != null && pinfo.metadata != null)
            {
                var txt = pinfo.metadata.localizedPriceString;
                _localizedPrice[productId] = txt;
                return txt;
            }
        }
        return "";
    }

    // ───────────── 호환 래퍼 (다른 스크립트가 요구) ─────────────
    public string GetPriceString(string productId) => GetLocalizedPrice(productId);

    public void Purchase(string productId, Action<bool> onComplete = null)
    {
        _pendingProductId = productId;
        _pendingPurchaseCallback = onComplete;
        BuyProductId(productId);
    }

    public void GrantAdRewardGems() => AddGems(adRewardGemsDefault);
    public void GrantAdRewardGems(int amount) => AddGems(amount);

    public bool TryShowRewardedAdForGems(int amount)
    {
        Debug.Log("[PremiumCurrencyManager] TODO: TryShowRewardedAdForGems not implemented yet.");
        return false;
    }

    // ───────────── IAP 초기화 ─────────────
    private void InitIAP()
    {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        foreach (var gp in products)
        {
            if (!string.IsNullOrEmpty(gp.productId))
                builder.AddProduct(gp.productId, gp.type);
        }
        UnityPurchasing.Initialize(this, builder);
    }

    // ───────────── IStoreListener ─────────────
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        _store = controller;
        _extensions = extensions;

        foreach (var gp in products)
        {
            var p = controller.products.WithID(gp.productId);
            if (p != null && p.metadata != null)
                _localizedPrice[gp.productId] = p.metadata.localizedPriceString;
        }
        Debug.Log("[IAP] Initialized.");
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
        int amount = GetGrantAmountById(id);
        if (amount > 0)
        {
            AddGems(amount);
            Debug.Log($"[IAP] Purchase OK: {id}, +{amount} gems");
        }
        else
        {
            Debug.LogWarning("[IAP] Unknown product or zero grant: " + id);
        }

        if (!string.IsNullOrEmpty(_pendingProductId) && id == _pendingProductId)
        {
            _pendingPurchaseCallback?.Invoke(true);
            _pendingProductId = null;
            _pendingPurchaseCallback = null;
        }
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogWarning($"[IAP] Purchase failed: {product?.definition?.id} - {failureReason}");
        if (!string.IsNullOrEmpty(_pendingProductId) &&
            product != null && product.definition.id == _pendingProductId)
        {
            _pendingPurchaseCallback?.Invoke(false);
            _pendingProductId = null;
            _pendingPurchaseCallback = null;
        }
    }

    // ───────────── Helpers ─────────────
    string FindIdByIndex(int idx)
    {
        if (idx < 0 || idx >= products.Count) return null;
        return products[idx].productId;
    }

    int GetGrantAmountById(string productId)
    {
        for (int i = 0; i < products.Count; i++)
        {
            if (products[i].productId == productId) return products[i].grantAmount;
        }
        return 0;
    }

    // ───────────── 저장 연동(호환) ─────────────
    void PersistBalance()
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
            var pi = t.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var inst = pi?.GetValue(null, null);
            return inst != null;
        }
        catch { return false; }
    }

    // ISaveable
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
        gems = (v != long.MinValue) ? Math.Max(0, v) : (long)PlayerPrefs.GetFloat(PP_GEMS, 0);
        OnGemsChanged?.Invoke(gems);
    }

    static void TrySetLong(object obj, string name, long value)
    {
        var f = obj.GetType().GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (f != null && (f.FieldType == typeof(long) || f.FieldType == typeof(int)))
        {
            if (f.FieldType == typeof(int)) f.SetValue(obj, (int)Mathf.Clamp(value, int.MinValue, int.MaxValue));
            else f.SetValue(obj, value);
        }
    }
    static long TryGetLong(object obj, string name, long fallback)
    {
        var f = obj.GetType().GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (f != null && (f.FieldType == typeof(long) || f.FieldType == typeof(int)))
        {
            var v = f.GetValue(obj);
            try { return Convert.ToInt64(v); } catch { }
        }
        return fallback;
    }
}
