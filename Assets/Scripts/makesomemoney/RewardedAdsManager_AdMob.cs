using System;
using GoogleMobileAds.Api;
using UnityEngine;

public class RewardedAdsManager_AdMob : MonoBehaviour
{
    public static RewardedAdsManager_AdMob Instance { get; private set; }

    [Header("Ad Unit Ids")]
#if UNITY_ANDROID
    [SerializeField] private string rewardedAdUnitId = "ca-app-pub-3940256099942544~3347511713"; // 테스트ID
#elif UNITY_IOS
    [SerializeField] private string rewardedAdUnitId = "ca-app-pub-3940256099942544~3347511713"; // 테스트ID
#else
    [SerializeField] private string rewardedAdUnitId = "ca-app-pub-3940256099942544~3347511713";
#endif

    private RewardedAd _rewardedAd;
    private bool _isLoading;
    public bool IsReady => _rewardedAd != null && _rewardedAd.CanShowAd();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        MobileAds.Initialize(_ => { Load(); });
    }

    public void Load()
    {
        if (_isLoading || string.IsNullOrEmpty(rewardedAdUnitId)) return;
        _isLoading = true;

        var request = new AdRequest(); // 최신 SDK: Builder 없이 기본 생성자
        RewardedAd.Load(rewardedAdUnitId, request, (ad, error) =>
        {
            _isLoading = false;
            if (error != null || ad == null)
            {
                _rewardedAd = null;
                return;
            }
            _rewardedAd = ad;
            // 자동 재로딩용 이벤트
            _rewardedAd.OnAdFullScreenContentClosed += () => { _rewardedAd = null; Load(); };
            _rewardedAd.OnAdFullScreenContentFailed += _ => { _rewardedAd = null; Load(); };
        });
    }

    public void Show(Action onReward)
    {
        if (!IsReady) return;
        _rewardedAd.Show(reward =>
        {
            try { onReward?.Invoke(); } catch {}
        });
    }
}