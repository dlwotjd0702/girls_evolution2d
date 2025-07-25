/*using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System;
using System.Collections;

namespace GirlsEvolution2D.MainSystem
{
    public class GPGSManager : MonoBehaviour
    {
        public static GPGSManager Instance { get; private set; }
        
        [Header("GPGS 설정")]
        [SerializeField] private bool enableDebugLog = true;
        
        public bool IsAuthenticated { get; private set; }
        public bool IsInitialized { get; private set; }
        
        public event Action<bool> OnAuthenticationChanged;
        public event Action OnSignInSuccess;
        public event Action<string> OnSignInFailed;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGPGS();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeGPGS()
        {
            try
            {
                // GPGS 초기화 - 최신 버전 호환
                #if UNITY_ANDROID
                    PlayGamesPlatform.Activate();
                    IsInitialized = true;
                    
                    if (enableDebugLog)
                        Debug.Log("GPGS 초기화 완료");
                #else
                    IsInitialized = false;
                    if (enableDebugLog)
                        Debug.Log("GPGS는 Android 플랫폼에서만 지원됩니다.");
                #endif
            }
            catch (Exception e)
            {
                Debug.LogError($"GPGS 초기화 실패: {e.Message}");
                IsInitialized = false;
            }
        }
        
        public void SignIn()
        {
            #if UNITY_ANDROID
                if (!IsInitialized)
                {
                    Debug.LogWarning("GPGS가 초기화되지 않았습니다.");
                    return;
                }
                
                PlayGamesPlatform.Instance.Authenticate((bool success) =>
                {
                    IsAuthenticated = success;
                    
                    if (success)
                    {
                        if (enableDebugLog)
                            Debug.Log("GPGS 로그인 성공");
                        OnSignInSuccess?.Invoke();
                    }
                    else
                    {
                        if (enableDebugLog)
                            Debug.Log("GPGS 로그인 실패");
                        OnSignInFailed?.Invoke("로그인에 실패했습니다.");
                    }
                    
                    OnAuthenticationChanged?.Invoke(success);
                });
            #else
                Debug.LogWarning("GPGS는 Android 플랫폼에서만 지원됩니다.");
            #endif
        }
        
        // 비동기 로그인 메서드 추가
        public IEnumerator SignInAsync()
        {
            #if UNITY_ANDROID
                if (!IsInitialized)
                {
                    Debug.LogWarning("GPGS가 초기화되지 않았습니다.");
                    yield break;
                }
                
                bool signInCompleted = false;
                bool signInResult = false;
                
                PlayGamesPlatform.Instance.Authenticate((bool success) =>
                {
                    signInResult = success;
                    signInCompleted = true;
                });
                
                while (!signInCompleted)
                {
                    yield return null;
                }
                
                IsAuthenticated = signInResult;
                
                if (signInResult)
                {
                    if (enableDebugLog)
                        Debug.Log("GPGS 로그인 성공");
                    OnSignInSuccess?.Invoke();
                }
                else
                {
                    if (enableDebugLog)
                        Debug.Log("GPGS 로그인 실패");
                    OnSignInFailed?.Invoke("로그인에 실패했습니다.");
                }
                
                OnAuthenticationChanged?.Invoke(signInResult);
            #else
                Debug.LogWarning("GPGS는 Android 플랫폼에서만 지원됩니다.");
                yield break;
            #endif
        }
        
        public void SignOut()
        {
            #if UNITY_ANDROID
                PlayGamesPlatform.Instance.SignOut();
                IsAuthenticated = false;
                OnAuthenticationChanged?.Invoke(false);
                
                if (enableDebugLog)
                    Debug.Log("GPGS 로그아웃 완료");
            #else
                Debug.LogWarning("GPGS는 Android 플랫폼에서만 지원됩니다.");
            #endif
        }
        
        // 업적 관련 메서드들
        public void UnlockAchievement(string achievementId)
        {
            #if UNITY_ANDROID
                if (!IsAuthenticated) return;
                
                Social.ReportProgress(achievementId, 100.0f, (bool success) =>
                {
                    if (enableDebugLog)
                        Debug.Log($"업적 해금 {(success ? "성공" : "실패")}: {achievementId}");
                });
            #else
                Debug.LogWarning("GPGS는 Android 플랫폼에서만 지원됩니다.");
            #endif
        }
        
        public void IncrementAchievement(string achievementId, int steps, int totalSteps)
        {
            #if UNITY_ANDROID
                if (!IsAuthenticated) return;
                
                PlayGamesPlatform.Instance.IncrementAchievement(achievementId, steps, totalSteps, (bool success) =>
                {
                    if (enableDebugLog)
                        Debug.Log($"업적 진행 {(success ? "성공" : "실패")}: {achievementId}");
                });
            #else
                Debug.LogWarning("GPGS는 Android 플랫폼에서만 지원됩니다.");
            #endif
        }
        
        // 업적 진행도 설정
        public void SetAchievementProgress(string achievementId, double progress)
        {
            #if UNITY_ANDROID
                if (!IsAuthenticated) return;
                
                Social.ReportProgress(achievementId, (float)progress, (bool success) =>
                {
                    if (enableDebugLog)
                        Debug.Log($"업적 진행도 설정 {(success ? "성공" : "실패")}: {achievementId} - {progress}%");
                });
            #else
                Debug.LogWarning("GPGS는 Android 플랫폼에서만 지원됩니다.");
            #endif
        }
        
        // 리더보드 관련 메서드들
        public void SubmitScore(string leaderboardId, long score)
        {
            #if UNITY_ANDROID
                if (!IsAuthenticated) return;
                
                Social.ReportScore(score, leaderboardId, (bool success) =>
                {
                    if (enableDebugLog)
                        Debug.Log($"점수 제출 {(success ? "성공" : "실패")}: {score}");
                });
            #else
                Debug.LogWarning("GPGS는 Android 플랫폼에서만 지원됩니다.");
            #endif
        }
        
        public void ShowLeaderboard(string leaderboardId = "")
        {
            #if UNITY_ANDROID
                if (!IsAuthenticated) return;
                
                if (string.IsNullOrEmpty(leaderboardId))
                    Social.ShowLeaderboardUI();
                else
                    PlayGamesPlatform.Instance.ShowLeaderboardUI(leaderboardId);
            #else
                Debug.LogWarning("GPGS는 Android 플랫폼에서만 지원됩니다.");
            #endif
        }
        
        public void ShowAchievements()
        {
            #if UNITY_ANDROID
                if (!IsAuthenticated) return;
                
                Social.ShowAchievementsUI();
            #else
                Debug.LogWarning("GPGS는 Android 플랫폼에서만 지원됩니다.");
            #endif
        }
        
        // 사용자 정보 가져오기
        public string GetUserDisplayName()
        {
            #if UNITY_ANDROID
                if (!IsAuthenticated) return "";
                return PlayGamesPlatform.Instance.GetUserDisplayName();
            #else
                return "";
            #endif
        }
        
        public string GetUserId()
        {
            #if UNITY_ANDROID
                if (!IsAuthenticated) return "";
                return PlayGamesPlatform.Instance.GetUserId();
            #else
                return "";
            #endif
        }
        
        public string GetUserEmail()
        {
            #if UNITY_ANDROID
                if (!IsAuthenticated) return "";
                return PlayGamesPlatform.Instance.GetUserEmail();
            #else
                return "";
            #endif
        }
        
        // GPGS 상태 확인
        public bool IsGPGSAvailable()
        {
            #if UNITY_ANDROID
                return IsInitialized && PlayGamesPlatform.Instance != null;
            #else
                return false;
            #endif
        }
        
        // 에러 처리 개선
        private void OnApplicationPause(bool pauseStatus)
        {
            #if UNITY_ANDROID
                if (!pauseStatus && IsAuthenticated)
                {
                    // 앱이 포그라운드로 돌아올 때 GPGS 상태 확인
                    if (enableDebugLog)
                        Debug.Log("앱 포그라운드 복귀 - GPGS 상태 확인");
                }
            #endif
        }
    }
} */