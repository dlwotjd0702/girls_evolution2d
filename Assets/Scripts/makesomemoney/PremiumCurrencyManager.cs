// PremiumCurrencyManager.cs
// - 유료재화(보석) 관리 + Unity IAP(v4 API) 연동
// - 가격표시 GetPriceString, 구매 Purchase(string), Purchase(string, Action), Purchase(string, Action<bool>)
// - 간단 저장: PlayerPrefs

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

// 필요시 경고 숨기기 (선택)
// #pragma warning disable 618

public class PremiumCurrencyManager : MonoBehaviour, IStoreListener
{
    public static PremiumCurrencyManager Instance { get; private set; }

    [Header("Balance")]
    [SerializeField] private long gems = 0;
    public event Action<long> OnGemsChanged;
    public long GetGems() => gems;

    [Header("Products (ID ↔ 지급량)")]
    [SerializeField] private List<ProductEntry> products = new()
    {
        new ProductEntry{ productId="gems_small",  type=ProductType.Consumable, grantAmount=80   },
        new ProductEntry{ productId="gems_medium", type=ProductType.Consumable, grantAmount=500  },
        new ProductEntry{ productId="gems_large",  type=ProductType.Consumable, grantAmount=1200 },
    };

    [Serializable]
    public class ProductEntry
    {
        public string productId;
        public ProductType type = ProductType.Consumable;
        public int grantAmount = 0;
    }

    // IAP (v4)
    private IStoreController store;
    private IExtensionProvider extensions;

    // 가격 캐시
    private readonly Dictionary<string, string> priceCache = new();

    // 단일 구매 콜백 대기(간단화)
    private string pendingProductId = null;
    private Action<bool> pendingOnComplete = null;

    const string PP_GEMS = "GEMS_BALANCE";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // 간단 저장 로드
        gems = (long)PlayerPrefs.GetFloat(PP_GEMS, 0);
        OnGemsChanged?.Invoke(gems);

        // IAP 초기화
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        foreach (var p in products) builder.AddProduct(p.productId, p.type);
        UnityPurchasing.Initialize(this, builder);
    }

    // ───────── 잔액 API ─────────
    public void AddGems(long amount)
    {
        if (amount <= 0) return;
        gems += amount;
        OnGemsChanged?.Invoke(gems);
        PlayerPrefs.SetFloat(PP_GEMS, gems);
        PlayerPrefs.Save();
    }

    public bool TrySpendGems(long amount)
    {
        if (amount <= 0) return true;
        if (gems < amount) return false;
        gems -= amount;
        OnGemsChanged?.Invoke(gems);
        PlayerPrefs.SetFloat(PP_GEMS, gems);
        PlayerPrefs.Save();
        return true;
    }

    // ───────── 표시/구매 ─────────
    public string GetPriceString(string productId)
    {
        if (string.IsNullOrEmpty(productId)) return "";
        if (priceCache.TryGetValue(productId, out var s)) return s;

        var p = store?.products?.WithID(productId);
        if (p != null && p.metadata != null)
        {
            var txt = p.metadata.localizedPriceString;
            priceCache[productId] = txt;
            return txt;
        }
        return "";
    }

    // 기존 1-인자
    public void Purchase(string productId)
    {
        if (string.IsNullOrEmpty(productId) || store == null) return;
        pendingProductId = null;
        pendingOnComplete = null;
        store.InitiatePurchase(productId);
    }

    // 새 오버로드: 완료 시 성공이면 onSuccess() 호출
    public void Purchase(string productId, Action onSuccess)
    {
        Purchase(productId, success => { if (success) onSuccess?.Invoke(); });
    }

    // 새 오버로드: 완료 시 성공여부 반환
    public void Purchase(string productId, Action<bool> onComplete)
    {
        if (string.IsNullOrEmpty(productId) || store == null) { onComplete?.Invoke(false); return; }
        pendingProductId = productId;
        pendingOnComplete = onComplete;
        store.InitiatePurchase(productId);
    }

    public void RestorePurchases()
    {
#if UNITY_IOS || UNITY_STANDALONE_OSX
        var apple = extensions.GetExtension<IAppleExtensions>();
        apple.RestoreTransactions(_ => {});
#endif
    }

    // ───────── IStoreListener ─────────
    public void OnInitialized(IStoreController c, IExtensionProvider e)
    {
        store = c; extensions = e;

        // 가격 캐시
        foreach (var p in products)
        {
            var info = c.products.WithID(p.productId);
            if (info != null && info.metadata != null)
                priceCache[p.productId] = info.metadata.localizedPriceString;
        }
    }

    public void OnInitializeFailed(InitializationFailureReason error) { }
    public void OnInitializeFailed(InitializationFailureReason error, string message) { }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
    {
        var id = e.purchasedProduct.definition.id;
        bool granted = false;

        foreach (var p in products)
        {
            if (p.productId == id && p.grantAmount > 0)
            {
                AddGems(p.grantAmount);
                granted = true;
                break;
            }
        }

        // 대기 콜백 처리
        if (!string.IsNullOrEmpty(pendingProductId))
        {
            bool ok = granted && string.Equals(pendingProductId, id, StringComparison.Ordinal);
            pendingOnComplete?.Invoke(ok);
            pendingProductId = null;
            pendingOnComplete = null;
        }

        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        if (!string.IsNullOrEmpty(pendingProductId))
        {
            pendingOnComplete?.Invoke(false);
            pendingProductId = null;
            pendingOnComplete = null;
        }
    }
}
