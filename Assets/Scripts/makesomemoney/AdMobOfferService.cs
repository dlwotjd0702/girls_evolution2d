using System;
using UnityEngine;

public class AdMobOfferService : MonoBehaviour, IAdOfferWithCompletion
{
    public event Action<bool> OnRewardedReadyChanged;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private bool _lastReadyState = false;
    private float _lastRetryTime = -999f;
    private const float RETRY_INTERVAL = 10f;

    void Start()
    {
        // RewardedAdsManager가 SDK 초기화 직후 첫 로드를 담당한다.
        // 서비스는 초기 로드와 경쟁하지 않고 이후 실패 복구만 담당한다.
        _lastRetryTime = Time.unscaledTime;
        // 준비 상태 모니터링 (UI 업데이트용)
        InvokeRepeating(nameof(CheckReadyState), 1f, 1f);
    }

    void CheckReadyState()
    {
        bool currentReady = IsRewardedReady();
        if (currentReady != _lastReadyState)
        {
            _lastReadyState = currentReady;
            OnRewardedReadyChanged?.Invoke(currentReady);
        }

        // 준비되지 않았으면 주기적으로 로드 재시도
        if (!currentReady && Time.unscaledTime - _lastRetryTime >= RETRY_INTERVAL)
        {
            _lastRetryTime = Time.unscaledTime;
            LoadRewarded();
        }
    }

    public bool IsRewardedReady()
    {
        // If the player has purchased ad removal, treat the ad as not ready to disable the button.
        if (PremiumCurrencyManager.Instance != null && PremiumCurrencyManager.Instance.AdsRemoved)
            return false;
        
        // Check if RewardedAdsManager_AdMob is initialized
        if (RewardedAdsManager_AdMob.Instance == null)
            return false;

        if (!RewardedAdsManager_AdMob.Instance.IsInitialized)
            return false;
        
        return RewardedAdsManager_AdMob.Instance.IsReady;
    }
    
    public void LoadRewarded()
    {
        if (RewardedAdsManager_AdMob.Instance == null)
            return;

        if (!RewardedAdsManager_AdMob.Instance.IsInitialized)
            return;

        RewardedAdsManager_AdMob.Instance.Load();
    }
    
    public void ShowRewarded(Action onReward)
    {
        // Prevent showing ads if ads have been removed. Invoke the reward callback immediately to simulate success.
        if (PremiumCurrencyManager.Instance != null && PremiumCurrencyManager.Instance.AdsRemoved)
        {
            if (enableDebugLogs)
                Debug.Log("[AdMobOfferService] Ads removed - granting reward without ad.");
            try { onReward?.Invoke(); } catch {}
            return;
        }
        
        if (RewardedAdsManager_AdMob.Instance == null)
        {
            Debug.LogError("[AdMobOfferService] Cannot show ad: RewardedAdsManager_AdMob.Instance is null. Check if RewardedAdsManager_AdMob is in the scene.");
            return;
        }

        if (!RewardedAdsManager_AdMob.Instance.IsInitialized)
        {
            Debug.LogError("[AdMobOfferService] Cannot show ad: AdMob SDK not initialized yet.");
            return;
        }
        
        RewardedAdsManager_AdMob.Instance.Show(onReward);
        OnRewardedReadyChanged?.Invoke(false);
    }

    public void ShowRewarded(Action onReward, Action onFinished)
    {
        if (PremiumCurrencyManager.Instance != null && PremiumCurrencyManager.Instance.AdsRemoved)
        {
            try { onReward?.Invoke(); } finally { onFinished?.Invoke(); }
            return;
        }
        var ads = RewardedAdsManager_AdMob.Instance;
        if (ads == null || !ads.IsInitialized || !ads.IsReady) { onFinished?.Invoke(); return; }
        ads.Show(onReward, onFinished);
        OnRewardedReadyChanged?.Invoke(false);
    }
}
