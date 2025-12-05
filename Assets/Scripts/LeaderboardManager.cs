// ============================================================================
// LeaderboardManager.cs
// - Google Play Games SDK 기반 리더보드 시스템
// - 점수 제출, 리더보드 표시 기능 제공
// - 여러 리더보드 카테고리 지원 (최고 레벨, 총 골드, 환생 횟수 등)
// ============================================================================

using System;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    // ── 리더보드 ID (Google Play Console에서 설정 필요) ──
    [Header("Leaderboard IDs")]
    [Tooltip("Google Play Console에서 설정한 리더보드 ID를 입력하세요")]
    [SerializeField] private string leaderboardMaxLevel = "CgkI-XXXXX"; // 최고 달성 레벨
    [SerializeField] private string leaderboardTotalGold = "CgkI-YYYYY"; // 총 획득 골드
    [SerializeField] private string leaderboardPrestigeCount = "CgkI-ZZZZZ"; // 환생 횟수
    [SerializeField] private string leaderboardPlayTime = "CgkI-WWWWW"; // 총 플레이타임

    // ── 이벤트 ──
    public event Action<bool, string> OnScoreSubmitted; // 점수 제출 완료 (성공 여부, 에러 메시지)
    public event Action<bool> OnLeaderboardOpened; // 리더보드 UI 열림

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 최고 달성 레벨 점수 제출
    /// </summary>
    public void SubmitMaxLevel(int maxLevel)
    {
        SubmitScore(leaderboardMaxLevel, maxLevel, "최고 레벨");
    }

    /// <summary>
    /// 총 획득 골드 점수 제출 (long으로 변환)
    /// </summary>
    public void SubmitTotalGold(double totalGold)
    {
        // double을 long으로 변환 (리더보드는 정수만 지원)
        long goldScore = (long)Math.Min(totalGold, long.MaxValue);
        SubmitScore(leaderboardTotalGold, goldScore, "총 획득 골드");
    }

    /// <summary>
    /// 환생 횟수 점수 제출
    /// </summary>
    public void SubmitPrestigeCount(int prestigeCount)
    {
        SubmitScore(leaderboardPrestigeCount, prestigeCount, "환생 횟수");
    }

    /// <summary>
    /// 총 플레이타임 점수 제출 (초 단위)
    /// </summary>
    public void SubmitPlayTime(double totalPlayTimeSeconds)
    {
        long playTimeMinutes = (long)(totalPlayTimeSeconds / 60.0);
        SubmitScore(leaderboardPlayTime, playTimeMinutes, "총 플레이타임");
    }

    /// <summary>
    /// 리더보드에 점수 제출
    /// </summary>
    public void SubmitScore(string leaderboardId, long score, string categoryName = "")
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (string.IsNullOrEmpty(leaderboardId) || leaderboardId.Contains("XXXXX"))
        {
            Debug.LogWarning($"[LeaderboardManager] 리더보드 ID가 설정되지 않았습니다: {categoryName}");
            OnScoreSubmitted?.Invoke(false, "리더보드 ID 미설정");
            return;
        }

        if (!Social.localUser.authenticated)
        {
            Debug.LogWarning("[LeaderboardManager] 로그인되지 않아 점수를 제출할 수 없습니다.");
            OnScoreSubmitted?.Invoke(false, "로그인되지 않음");
            return;
        }

        try
        {
            Social.ReportScore(score, leaderboardId, (success) =>
            {
                if (success)
                {
                    Debug.Log($"[LeaderboardManager] {categoryName} 점수 제출 성공: {score}");
                    OnScoreSubmitted?.Invoke(true, null);
                }
                else
                {
                    Debug.LogError($"[LeaderboardManager] {categoryName} 점수 제출 실패");
                    OnScoreSubmitted?.Invoke(false, "점수 제출 실패");
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"[LeaderboardManager] {categoryName} 점수 제출 중 오류: {e.Message}");
            OnScoreSubmitted?.Invoke(false, e.Message);
        }
#else
        Debug.Log($"[LeaderboardManager] 에디터에서는 점수를 제출할 수 없습니다. ({categoryName}: {score})");
        OnScoreSubmitted?.Invoke(false, "에디터 모드");
#endif
    }

    /// <summary>
    /// 최고 레벨 리더보드 UI 열기
    /// </summary>
    public void ShowMaxLevelLeaderboard()
    {
        ShowLeaderboard(leaderboardMaxLevel);
    }

    /// <summary>
    /// 총 골드 리더보드 UI 열기
    /// </summary>
    public void ShowTotalGoldLeaderboard()
    {
        ShowLeaderboard(leaderboardTotalGold);
    }

    /// <summary>
    /// 환생 횟수 리더보드 UI 열기
    /// </summary>
    public void ShowPrestigeCountLeaderboard()
    {
        ShowLeaderboard(leaderboardPrestigeCount);
    }

    /// <summary>
    /// 플레이타임 리더보드 UI 열기
    /// </summary>
    public void ShowPlayTimeLeaderboard()
    {
        ShowLeaderboard(leaderboardPlayTime);
    }

    /// <summary>
    /// 리더보드 UI 표시
    /// </summary>
    public void ShowLeaderboard(string leaderboardId = null)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Social.localUser.authenticated)
        {
            Debug.LogWarning("[LeaderboardManager] 리더보드를 표시하려면 먼저 로그인해야 합니다.");
            
            // 로그인 시도
            CloudSaveManager.Instance?.SignIn((success) =>
            {
                if (success)
                {
                    ShowLeaderboard(leaderboardId);
                }
            });
            return;
        }

        try
        {
            if (string.IsNullOrEmpty(leaderboardId))
            {
                // 리더보드 ID가 없으면 전체 리더보드 표시
                Social.ShowLeaderboardUI();
            }
            else
            {
                // 특정 리더보드 표시
                PlayGamesPlatform.Instance.ShowLeaderboardUI(leaderboardId, (status) =>
                {
                    bool success = status == UIStatus.Valid;
                    OnLeaderboardOpened?.Invoke(success);
                    if (success)
                    {
                        Debug.Log($"[LeaderboardManager] 리더보드 UI 표시 성공: {leaderboardId}");
                    }
                    else
                    {
                        Debug.LogWarning($"[LeaderboardManager] 리더보드 UI 표시 실패: {status}");
                    }
                });
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[LeaderboardManager] 리더보드 UI 표시 중 오류: {e.Message}");
            OnLeaderboardOpened?.Invoke(false);
        }
#else
        Debug.Log("[LeaderboardManager] 에디터에서는 리더보드를 표시할 수 없습니다.");
        OnLeaderboardOpened?.Invoke(false);
#endif
    }

    /// <summary>
    /// 현재 게임 통계를 기반으로 모든 리더보드에 점수 제출
    /// </summary>
    public void SubmitAllScores()
    {
        if (SaveManager.Instance == null || !SaveManager.Instance.HasSaveFile())
        {
            Debug.LogWarning("[LeaderboardManager] 세이브 파일이 없어 점수를 제출할 수 없습니다.");
            return;
        }

        // 세이브 데이터 읽기
        try
        {
            string saveJson = System.IO.File.ReadAllText(SaveManager.Instance.GetSaveFilePath());
            SaveData saveData = JsonUtility.FromJson<SaveData>(saveJson);

            // 최고 레벨 제출
            SubmitMaxLevel(saveData.maxLevelReached);

            // 총 골드 제출 (PlayStatsTracker에서)
            if (PlayStatsTracker.Instance != null)
            {
                SubmitTotalGold(PlayStatsTracker.Instance.GetTotalGoldEarned());
                SubmitPlayTime(PlayStatsTracker.Instance.GetTotalPlayTimeSeconds());
            }

            // 환생 횟수 제출
            SubmitPrestigeCount(saveData.totalPrestigeCount);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LeaderboardManager] 점수 제출 중 오류: {e.Message}");
        }
    }
}
