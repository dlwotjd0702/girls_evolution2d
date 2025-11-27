using System;
using UnityEngine;

public class AdMobOfferService : MonoBehaviour, IAdOfferService
{
    public event Action<bool> OnRewardedReadyChanged;

    void Start()
    {
        // 광고 초기 로딩 (Instance가 준비된 후에만)
        if (RewardedAdsManager_AdMob.Instance != null)
        {
            RewardedAdsManager_AdMob.Instance.Load();
        }
        else
        {
            // Instance가 아직 준비되지 않았으면 잠시 후 다시 시도
            StartCoroutine(DelayedLoad());
        }
    }
    
    System.Collections.IEnumerator DelayedLoad()
    {
        // 최대 1초 동안 대기
        float elapsed = 0f;
        while (RewardedAdsManager_AdMob.Instance == null && elapsed < 1f)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        
        if (RewardedAdsManager_AdMob.Instance != null)
        {
            RewardedAdsManager_AdMob.Instance.Load();
        }
        else
        {
            Debug.LogWarning("[AdMobOfferService] RewardedAdsManager_AdMob.Instance is still null after delay.");
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
        
        return RewardedAdsManager_AdMob.Instance.IsReady;
    }
    
    public void LoadRewarded()
    {
        if (RewardedAdsManager_AdMob.Instance == null) return;
        RewardedAdsManager_AdMob.Instance.Load();
    }
    
    public void ShowRewarded(Action onReward)
    {
        // Prevent showing ads if ads have been removed. Invoke the reward callback immediately to simulate success.
        if (PremiumCurrencyManager.Instance != null && PremiumCurrencyManager.Instance.AdsRemoved)
        {
            try { onReward?.Invoke(); } catch {}
            return;
        }
        
        if (RewardedAdsManager_AdMob.Instance == null)
        {
            Debug.LogWarning("[AdMobOfferService] RewardedAdsManager_AdMob.Instance is null. Cannot show ad.");
            return;
        }
        
        RewardedAdsManager_AdMob.Instance.Show(onReward);
        OnRewardedReadyChanged?.Invoke(false);
    }
}