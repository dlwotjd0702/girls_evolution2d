using System;
using UnityEngine;

public class AdMobOfferService : MonoBehaviour, IAdOfferService
{
    public event Action<bool> OnRewardedReadyChanged;

    void Start()
    {
        // 광고 초기 로딩
        RewardedAdsManager_AdMob.Instance.Load();
        // 필요 시 이벤트를 연결해 광고 준비 상태 변경 시 알림
    }

    public bool IsRewardedReady()
    {
        // If the player has purchased ad removal, treat the ad as not ready to disable the button.
        if (PremiumCurrencyManager.Instance != null && PremiumCurrencyManager.Instance.AdsRemoved)
            return false;
        return RewardedAdsManager_AdMob.Instance.IsReady;
    }
    public void LoadRewarded()    => RewardedAdsManager_AdMob.Instance.Load();
    public void ShowRewarded(Action onReward)
    {
        // Prevent showing ads if ads have been removed. Invoke the reward callback immediately to simulate success.
        if (PremiumCurrencyManager.Instance != null && PremiumCurrencyManager.Instance.AdsRemoved)
        {
            try { onReward?.Invoke(); } catch {}
            return;
        }
        RewardedAdsManager_AdMob.Instance.Show(onReward);
        OnRewardedReadyChanged?.Invoke(false);
    }
}