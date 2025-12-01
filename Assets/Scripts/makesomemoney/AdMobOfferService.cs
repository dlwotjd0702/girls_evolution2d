using System;
using UnityEngine;

public class AdMobOfferService : MonoBehaviour, IAdOfferService
{
    public event Action<bool> OnRewardedReadyChanged;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private bool _lastReadyState = false;

    void Start()
    {
        // 광고 초기 로딩 (Instance가 준비되고 초기화 완료된 후에만)
        StartCoroutine(WaitAndLoad());
        
        // 준비 상태 모니터링 (UI 업데이트용)
        InvokeRepeating(nameof(CheckReadyState), 1f, 1f);
    }
    
    System.Collections.IEnumerator WaitAndLoad()
    {
        // 최대 5초 동안 대기 (초기화 완료까지)
        float elapsed = 0f;
        while ((RewardedAdsManager_AdMob.Instance == null || !RewardedAdsManager_AdMob.Instance.IsInitialized) && elapsed < 5f)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        
        if (RewardedAdsManager_AdMob.Instance != null && RewardedAdsManager_AdMob.Instance.IsInitialized)
        {
            if (enableDebugLogs)
                Debug.Log("[AdMobOfferService] AdMob initialized. Loading rewarded ad...");
            RewardedAdsManager_AdMob.Instance.Load();
        }
        else
        {
            Debug.LogError("[AdMobOfferService] RewardedAdsManager_AdMob.Instance is null or not initialized after delay. Check if RewardedAdsManager_AdMob is in the scene.");
        }
    }

    void CheckReadyState()
    {
        bool currentReady = IsRewardedReady();
        if (currentReady != _lastReadyState)
        {
            _lastReadyState = currentReady;
            OnRewardedReadyChanged?.Invoke(currentReady);
        }
    }

    public bool IsRewardedReady()
    {
        // If the player has purchased ad removal, treat the ad as not ready to disable the button.
        if (PremiumCurrencyManager.Instance != null && PremiumCurrencyManager.Instance.AdsRemoved)
            return false;
        
        // Check if RewardedAdsManager_AdMob is initialized
        if (RewardedAdsManager_AdMob.Instance == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[AdMobOfferService] RewardedAdsManager_AdMob.Instance is null.");
            return false;
        }

        if (!RewardedAdsManager_AdMob.Instance.IsInitialized)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[AdMobOfferService] RewardedAdsManager_AdMob is not initialized yet.");
            return false;
        }
        
        return RewardedAdsManager_AdMob.Instance.IsReady;
    }
    
    public void LoadRewarded()
    {
        if (RewardedAdsManager_AdMob.Instance == null)
        {
            Debug.LogWarning("[AdMobOfferService] Cannot load: RewardedAdsManager_AdMob.Instance is null.");
            return;
        }

        if (!RewardedAdsManager_AdMob.Instance.IsInitialized)
        {
            Debug.LogWarning("[AdMobOfferService] Cannot load: AdMob SDK not initialized yet.");
            return;
        }

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
}