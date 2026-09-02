using System;
using System.Collections;
using GoogleMobileAds.Api;
using UnityEngine;

public class RewardedAdsManager_AdMob : MonoBehaviour
{
    public static RewardedAdsManager_AdMob Instance { get; private set; }

    [Header("Ad Unit Ids")]
    [Tooltip("주의: App ID가 아닌 Rewarded Ad Unit ID를 입력하세요.\n" +
             "App ID 형식: ca-app-pub-XXXX~XXXX (물결표 ~)\n" +
             "Ad Unit ID 형식: ca-app-pub-XXXX/XXXX (슬래시 /)\n" +
             "테스트용: Android - ca-app-pub-3940256099942544/5224354917\n" +
             "테스트용: iOS - ca-app-pub-3940256099942544/1712485313")]
#if UNITY_ANDROID
    [SerializeField] private string rewardedAdUnitId = "ca-app-pub-3859743300335240/8641404741"; // 미소녀닌자 합성 Android 보상형 광고 단위 ID
#elif UNITY_IOS
    [SerializeField] private string rewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313"; // 테스트용 Rewarded Ad Unit ID
#else
    [SerializeField] private string rewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917"; // 테스트용 (Android 기본값)
#endif

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private RewardedAd _rewardedAd;
    private bool _isLoading;
    private bool _isShowing;
    private Action _onFinished;
    private bool _isInitialized = false;
    private float _lastLoadAttemptTime = 0f;
    private const float MIN_LOAD_INTERVAL = 2f; // 최소 로드 간격 (초)

    public bool IsReady => !_isShowing && _rewardedAd != null && _rewardedAd.CanShowAd();
    public bool IsInitialized => _isInitialized;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (enableDebugLogs)
            Debug.Log($"[RewardedAdsManager] Initializing AdMob SDK... AdUnitId: {rewardedAdUnitId}");
        
        // AdMob SDK 초기화
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
        MobileAds.Initialize(initStatus =>
        {
            _isInitialized = true;
            if (enableDebugLogs)
            {
                Debug.Log($"[RewardedAdsManager] AdMob SDK initialized. Status: {initStatus}");
            }
            // WebView 초기화를 위한 짧은 지연 후 광고 로드
            // "Unable to obtain a JavascriptEngine" 에러 방지를 위해 지연 추가
            StartCoroutine(DelayedLoadAfterInit());
        });
    }

    private IEnumerator DelayedLoadAfterInit()
    {
        // WebView 초기화를 위한 짧은 지연 (0.5초)
        // "Unable to obtain a JavascriptEngine" 에러 방지
        yield return new WaitForSeconds(0.5f);
        Load();
    }

    public void Load()
    {
        if (_isShowing) return;
        // 초기화가 완료되지 않았으면 대기
        if (!_isInitialized)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[RewardedAdsManager] Cannot load ad: SDK not initialized yet.");
            return;
        }

        // 최소 로드 간격 체크 (너무 자주 로드 시도 방지)
        if (Time.time - _lastLoadAttemptTime < MIN_LOAD_INTERVAL)
            return;

        if (_isLoading)
            return;

        if (string.IsNullOrEmpty(rewardedAdUnitId))
        {
            Debug.LogError("[RewardedAdsManager] Cannot load ad: AdUnitId is empty!");
            return;
        }

        _isLoading = true;
        _lastLoadAttemptTime = Time.time;

        if (enableDebugLogs)
            Debug.Log($"[RewardedAdsManager] Loading rewarded ad... AdUnitId: {rewardedAdUnitId}");

        var request = new AdRequest();
        RewardedAd.Load(rewardedAdUnitId, request, (ad, error) =>
        {
            _isLoading = false;
            
            if (error != null)
            {
                _rewardedAd = null;
                int errorCode = error.GetCode();
                string errorMessage = error.GetMessage();
                string errorDomain = error.GetDomain();
                
                Debug.LogError($"[RewardedAdsManager] Failed to load rewarded ad. Code: {errorCode}, Domain: {errorDomain}, Message: {errorMessage}");
                
                // 에러 코드별 상세 정보 (Google Mobile Ads SDK 에러 코드는 정수)
                // 일반적인 에러 코드:
                // 0: Internal error
                // 1: Invalid request
                // 2: Network error
                // 3: No fill
                string errorMsgLower = errorMessage?.ToLower() ?? "";
                
                if (errorCode == 0 || errorMsgLower.Contains("internal"))
                {
                    if (errorMsgLower.Contains("javascriptengine") || errorMsgLower.Contains("javascript engine"))
                    {
                        Debug.LogError("[RewardedAdsManager] Unable to obtain JavascriptEngine. This usually means Android WebView is not properly initialized. " +
                                     "Possible solutions:\n" +
                                     "1. Ensure Android WebView is installed and updated on the device\n" +
                                     "2. Add a delay after MobileAds.Initialize() before loading ads\n" +
                                     "3. Check if WebView-related classes are preserved in link.xml");
                        // WebView 초기화 문제이므로 더 긴 지연 후 재시도
                        Invoke(nameof(Load), 10f);
                        return;
                    }
                    Debug.LogError("[RewardedAdsManager] Internal error. Check AdMob App ID in AndroidManifest.xml");
                }
                else if (errorCode == 1 || errorMsgLower.Contains("invalid") || errorMsgLower.Contains("request"))
                {
                    Debug.LogError("[RewardedAdsManager] Invalid request. Check AdUnitId is correct.");
                }
                else if (errorCode == 2 || errorMsgLower.Contains("network"))
                {
                    Debug.LogError("[RewardedAdsManager] Network error. Check internet connection.");
                }
                else if (errorCode == 3 || errorMsgLower.Contains("no fill") || errorMsgLower.Contains("no ad"))
                {
                    Debug.LogWarning("[RewardedAdsManager] No ad available (no fill). Will retry later.");
                }
                else
                {
                    Debug.LogWarning($"[RewardedAdsManager] Unknown error code: {errorCode}. Message: {errorMessage}");
                }
                
                // 일정 시간 후 재시도
                Invoke(nameof(Load), 5f);
                return;
            }

            if (ad == null)
            {
                _rewardedAd = null;
                Debug.LogError("[RewardedAdsManager] Failed to load rewarded ad: ad is null.");
                Invoke(nameof(Load), 5f);
                return;
            }

            _rewardedAd = ad;
            if (enableDebugLogs)
                Debug.Log("[RewardedAdsManager] Rewarded ad loaded successfully!");

            // 자동 재로딩용 이벤트
            _rewardedAd.OnAdFullScreenContentClosed += () =>
            {
                if (enableDebugLogs)
                    Debug.Log("[RewardedAdsManager] Ad closed. Reloading...");
                _rewardedAd = null;
                FinishShowing();
                Load();
            };
            
            _rewardedAd.OnAdFullScreenContentFailed += (error) =>
            {
                Debug.LogError($"[RewardedAdsManager] Ad failed to show. Error: {error?.GetMessage() ?? "Unknown"}");
                _rewardedAd = null;
                FinishShowing();
                Load();
            };

            _rewardedAd.OnAdPaid += (adValue) =>
            {
                if (enableDebugLogs)
                    Debug.Log($"[RewardedAdsManager] Ad paid: {adValue.Value} {adValue.CurrencyCode}");
            };
        });
    }

    private void FinishShowing()
    {
        _isShowing = false;
        var callback = _onFinished;
        _onFinished = null;
        callback?.Invoke();
    }

    public void Show(Action onReward, Action onFinished = null)
    {
        if (!IsReady)
        {
            Debug.LogWarning("[RewardedAdsManager] Cannot show ad: ad is not ready. Attempting to load...");
            Load();
            onFinished?.Invoke();
            return;
        }

        if (enableDebugLogs)
            Debug.Log("[RewardedAdsManager] Showing rewarded ad...");

        _isShowing = true;
        _onFinished = onFinished;
        bool granted = false;
        try { _rewardedAd.Show(reward =>
        {
            if (granted) return;
            granted = true;
            if (enableDebugLogs)
                Debug.Log($"[RewardedAdsManager] Ad reward granted: {reward.Type} - {reward.Amount}");
            try { onReward?.Invoke(); } catch (Exception e) 
            { 
                Debug.LogError($"[RewardedAdsManager] Error in reward callback: {e.Message}");
            }
        }); }
        catch (Exception error) { FinishShowing(); Debug.LogException(error); }
    }
}
