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
    public event Action<string, string> OnPurchaseFailed; // productId, reason

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

    [Header("UI Label (Optional)")]
    [SerializeField] private TextMeshProUGUI gemLabel; // 인스펙터에서 하나만 연결

    private StoreController storeController;
    private readonly Dictionary<string,string> priceCache = new();
    private bool isApplyingLoad = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // SaveManager가 있으면 ApplyLoadedData에서 복구하므로 여기서는 복구하지 않음
        NotifyAndPersist(); // SaveManager의 ApplyLoadedData 이전에도 UI를 초기화
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
        
        // 캐시에서 먼저 확인
        if (priceCache.TryGetValue(productId, out var s) && !string.IsNullOrEmpty(s)) 
            return s;

        // 캐시에 없으면 storeController에서 직접 가져오기
        var p = storeController?.GetProductById(productId);
        if (p?.metadata != null)
        {
            s = p.metadata.localizedPriceString;
            // 가격이 비어있지 않을 때만 캐시에 저장하고 반환
            if (!string.IsNullOrEmpty(s))
            {
                priceCache[productId] = s;
                return s;
            }
        }
        
        // 가격을 찾을 수 없으면 빈 문자열 반환
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
        storeController.OnPurchasesFetched    += OnPurchasesFetched;
        storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
        storeController.OnPurchasePending     += OnPurchasePending;
        storeController.OnPurchaseConfirmed   += OnPurchaseConfirmed;
        storeController.OnPurchaseFailed      += HandlePurchaseFailed;
        storeController.OnStoreDisconnected   += OnStoreDisconnected;

        try { await storeController.Connect(); }
        catch (Exception ex)
        {
            Debug.LogError("[IAP] Connection failed: " + ex.Message);
            OnCatalogReady?.Invoke();
            return;
        }

        var definitions = new List<ProductDefinition>();
        var seenProductIds = new HashSet<string>();
        foreach (var gp in products)
        {
            // Older scene data used the singular ID. Normalize it at runtime so an
            // already-open scene or an upgraded install cannot silently omit the
            // non-consumable product from the fetched catalog.
            if (gp != null && gp.productId == "remove_ad")
                gp.productId = REMOVE_ADS_ID;

            if (gp != null && !string.IsNullOrEmpty(gp.productId) && seenProductIds.Add(gp.productId))
                definitions.Add(new ProductDefinition(gp.productId, gp.type));
        }

        if (definitions.Count > 0) storeController.FetchProducts(definitions);
        else OnCatalogReady?.Invoke();
    }

    void OnProductsFetched(List<Product> fetchedProducts)
    {
        foreach (var prod in fetchedProducts)
        {
            if (prod?.metadata != null && !string.IsNullOrEmpty(prod.definition.id))
            {
                string price = prod.metadata.localizedPriceString;
                // 가격이 비어있지 않을 때만 캐시에 저장
                if (!string.IsNullOrEmpty(price))
                {
                    priceCache[prod.definition.id] = price;
                }
            }
        }

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
        if (storeController == null || pendingOrder == null) return;

        // Unity IAP 5 requires the entitlement to be granted and persisted before
        // acknowledging the order. Confirming first can lose a paid entitlement if
        // the app closes before OnPurchaseConfirmed is delivered.
        GrantEntitlements(pendingOrder);
        storeController.ConfirmPurchase(pendingOrder);
    }

    void OnPurchaseConfirmed(Order order)
    {
        if (order is FailedOrder failedOrder)
        {
            Debug.LogWarning($"[IAP] Purchase confirmation failed: {failedOrder.FailureReason} - {failedOrder.Details}");
            return;
        }

        if (order is ConfirmedOrder confirmedOrder)
        {
            var ids = confirmedOrder.CartOrdered?.Items()?
                .Select(item => item?.Product?.definition?.id)
                .Where(id => !string.IsNullOrEmpty(id));
            Debug.Log("[IAP] Purchase confirmed: " + (ids != null ? string.Join(", ", ids) : "unknown"));
        }
    }

    void GrantEntitlements(Order order)
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

    void OnPurchasesFetched(Orders orders)
    {
        if (orders?.ConfirmedOrders == null) return;

        // Confirmed consumables must never be granted again during restoration.
        // Only restore durable non-consumable entitlements from the store receipt.
        foreach (var order in orders.ConfirmedOrders)
        {
            var items = order?.CartOrdered?.Items();
            if (items == null) continue;

            foreach (var item in items)
            {
                if (item?.Product?.definition?.id == REMOVE_ADS_ID)
                    SetAdsRemoved(true);
            }
        }
    }

    void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
    {
        Debug.LogWarning("[IAP] Existing purchases fetch failed: " + failure?.Message);
    }

    void OnStoreDisconnected(StoreConnectionFailureDescription failure)
    {
        Debug.LogWarning("[IAP] Store disconnected: " + failure?.Message);
    }

    void HandlePurchaseFailed(FailedOrder failedOrder)
    {
        var productId = failedOrder?.CartOrdered?.Items()?.FirstOrDefault()?.Product?.definition?.id;
        var reason = failedOrder?.FailureReason;
        Debug.LogWarning($"[IAP] Purchase failed: {productId} - {reason}");
        OnPurchaseFailed?.Invoke(productId ?? "", reason?.ToString() ?? "");
    }

    // ===== Save / Load =====
    public void CollectSaveData(SaveData d)
    {
        // SaveData에 직접 필드로 저장 (리플렉션 대신)
        d.gems = gems;
        d.adsRemoved = adsRemoved ? 1L : 0L;
    }
    
    public void ApplyLoadedData(SaveData d)
    {
        isApplyingLoad = true;
        // SaveData에서 직접 필드로 로드 (SaveData에 필드가 있으면 항상 사용)
        // gems는 0일 수도 있으므로, SaveData 필드를 우선 사용
        gems = Math.Max(0, d.gems);
        
        // Load ad removal state from SaveData
        adsRemoved = (d.adsRemoved > 0);

        NotifyAndPersist();
        isApplyingLoad = false;
    }

    // ===== Helpers =====
    void NotifyAndPersist()
    {
        // 0이어도 항상 단위가 보이도록 "0 Gem" 형태로 표시
        if (gemLabel) gemLabel.text = $"{gems:N0} Gem";
        OnGemsChanged?.Invoke(gems);
        if (!isApplyingLoad)
        {
            SaveManager.Instance?.SaveGame();
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
        // SaveManager를 통해서만 영구 저장하므로 별도 처리가 필요 없음
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
