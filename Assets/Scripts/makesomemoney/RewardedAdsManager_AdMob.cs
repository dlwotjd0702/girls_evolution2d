using System;
using UnityEngine;
using GoogleMobileAds.Api;

public class RewardedAdsManager_AdMob : MonoBehaviour
{
    public static RewardedAdsManager_AdMob Instance { get; private set; }

    [Header("Ad Unit IDs (테스트 기본값)")]
    public string androidRewardedUnitId = "ca-app-pub-3940256099942544/5224354917";
    public string iosRewardedUnitId     = "ca-app-pub-3940256099942544/1712485313";

    public bool IsReady => rewardedAd != null && rewardedAd.CanShowAd();

    public event Action<bool> OnReadyChanged;

    private RewardedAd rewardedAd;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        MobileAds.Initialize(_ => LoadRewarded());
    }

    string GetUnitId()
    {
#if UNITY_ANDROID
        return androidRewardedUnitId;
#elif UNITY_IOS
        return iosRewardedUnitId;
#else
        return "";
#endif
    }

    public void LoadRewarded()
    {
        var adUnitId = GetUnitId();
        if (string.IsNullOrEmpty(adUnitId)) return;

        var request = new AdRequest();
        RewardedAd.Load(adUnitId, request, (ad, error) =>
        {
            rewardedAd = error == null ? ad : null;
            OnReadyChanged?.Invoke(IsReady);
        });
    }

    public void ShowRewarded(Action onReward)
    {
        if (!IsReady) return;

        rewardedAd.OnAdFullScreenContentClosed += () =>
        {
            rewardedAd = null;
            OnReadyChanged?.Invoke(false);
            LoadRewarded();
        };

        rewardedAd.Show(r =>
        {
            if (r.Amount > 0) onReward?.Invoke();
            else onReward?.Invoke(); // 테스트 유닛은 보상정보가 고정, 보상 자체는 게임 로직이 정함
        });
    }
}