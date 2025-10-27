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

    public bool IsRewardedReady() => RewardedAdsManager_AdMob.Instance.IsReady;
    public void LoadRewarded()    => RewardedAdsManager_AdMob.Instance.Load();
    public void ShowRewarded(Action onReward)
    {
        RewardedAdsManager_AdMob.Instance.Show(onReward);
        OnRewardedReadyChanged?.Invoke(false);
    }
}