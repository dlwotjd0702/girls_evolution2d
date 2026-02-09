// ============================================================================
// AchievementManager.cs
// - Google Play Games SDK 기반 성취도 시스템
// - 성취도 자동 체크 및 달성 처리 (5가지)
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    // ── 성취도 ID (하드코딩, Google Play Console에서 생성한 실제 ID로 교체 필요) ──
    [Header("Achievement IDs")]
    [Tooltip("Google Play Console에서 생성한 성취도 ID를 입력하세요. CgkI-로 시작하는 긴 문자열입니다.")]
    [SerializeField] private string achievementLevel25 = "Cgkl_tDrmYMIEAIQAQ"; // 레벨 25 달성
    [SerializeField] private string achievementClicks1000 = "Cgkl_tDrmYMIEAIQAg"; // 클릭 1,000회
    [SerializeField] private string achievementPlayTime1Hour = "Cgkl_tDrmYMIEAIQAw"; // 1시간 플레이
    [SerializeField] private string achievementUpgrade10 = "Cgkl_tDrmYMIEAIQBA"; // 강화 10레벨
    [SerializeField] private string achievementTier1 = "Cgkl_tDrmYMIEAIQBQ"; // 1계층 발견

    // ── 성취도 추적 ──
    private HashSet<string> unlockedAchievements = new HashSet<string>();
    private int lastCheckedLevel = 0;
    private long lastCheckedClicks = 0;
    private double lastCheckedPlayTime = 0.0;
    private int lastCheckedUpgrade = 0;
    private int lastCheckedTier = -1;

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

    private void Start()
    {
        // 주기적으로 성취도 체크 (5초마다)
        InvokeRepeating(nameof(CheckAchievements), 5f, 5f);
    }

    // ── 성취도 시스템 ──

    /// <summary>
    /// 성취도 달성 체크 (주기적으로 호출)
    /// </summary>
    private void CheckAchievements()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Social.localUser.authenticated) return;
#endif

        CheckLevelAchievement();
        CheckClickAchievement();
        CheckPlayTimeAchievement();
        CheckUpgradeAchievement();
        CheckTierAchievement();
    }

    /// <summary>
    /// 레벨 25 달성 체크
    /// </summary>
    private void CheckLevelAchievement()
    {
        GirlFieldManager fieldManager = FindObjectOfType<GirlFieldManager>(true);
        if (fieldManager == null) return;

        int currentMaxLevel = fieldManager.CurrentMaxLevel;
        if (currentMaxLevel <= lastCheckedLevel) return;

        if (currentMaxLevel >= 25 && !unlockedAchievements.Contains(achievementLevel25))
        {
            UnlockAchievement(achievementLevel25, "레벨 25 달성");
        }

        lastCheckedLevel = currentMaxLevel;
    }

    /// <summary>
    /// 클릭 1,000회 체크
    /// </summary>
    private void CheckClickAchievement()
    {
        if (PlayStatsTracker.Instance == null) return;

        long totalClicks = PlayStatsTracker.Instance.GetTotalClicks();
        if (totalClicks <= lastCheckedClicks) return;

        if (totalClicks >= 1000 && !unlockedAchievements.Contains(achievementClicks1000))
        {
            UnlockAchievement(achievementClicks1000, "클릭 1,000회");
        }

        lastCheckedClicks = totalClicks;
    }

    /// <summary>
    /// 1시간 플레이 체크
    /// </summary>
    private void CheckPlayTimeAchievement()
    {
        if (PlayStatsTracker.Instance == null) return;

        double totalPlayTime = PlayStatsTracker.Instance.GetTotalPlayTimeSeconds();
        double playTimeHours = totalPlayTime / 3600.0;
        if (playTimeHours <= lastCheckedPlayTime) return;

        if (playTimeHours >= 1.0 && !unlockedAchievements.Contains(achievementPlayTime1Hour))
        {
            UnlockAchievement(achievementPlayTime1Hour, "1시간 플레이");
        }

        lastCheckedPlayTime = playTimeHours;
    }

    /// <summary>
    /// 강화 10레벨 체크
    /// </summary>
    private void CheckUpgradeAchievement()
    {
        if (SaveManager.Instance == null) return;

        SaveData saveData = new SaveData();
        var saveables = FindObjectsOfType<MonoBehaviour>(true);
        foreach (var s in saveables)
        {
            if (s is ISaveable saveable)
            {
                try
                {
                    saveable.CollectSaveData(saveData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AchievementManager] 데이터 수집 실패: {e.Message}");
                }
            }
        }

        int maxUpgrade = Mathf.Max(
            saveData.clickBonusUpgrade,
            saveData.manualSpawnMaxUpgrade,
            saveData.manualSpawnSpeedUpgrade,
            saveData.maxFieldCountUpgrade,
            saveData.autoMergeUpgrade,
            saveData.autoSpawnUpgrade,
            saveData.offlineRewardUpgrade,
            saveData.offlineMaxTimeUpgrade
        );

        if (maxUpgrade <= lastCheckedUpgrade) return;

        if (maxUpgrade >= 10 && !unlockedAchievements.Contains(achievementUpgrade10))
        {
            UnlockAchievement(achievementUpgrade10, "강화 10레벨");
        }

        lastCheckedUpgrade = maxUpgrade;
    }

    /// <summary>
    /// 1계층 발견 체크
    /// </summary>
    private void CheckTierAchievement()
    {
        if (SaveManager.Instance == null) return;

        SaveData saveData = new SaveData();
        var saveables = FindObjectsOfType<MonoBehaviour>(true);
        foreach (var s in saveables)
        {
            if (s is ISaveable saveable)
            {
                try
                {
                    saveable.CollectSaveData(saveData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AchievementManager] 데이터 수집 실패: {e.Message}");
                }
            }
        }

        int maxUnlockedTier = 0;
        for (int i = 0; i < 4; i++)
        {
            if ((saveData.unlockedTierMask & (1 << i)) != 0)
            {
                maxUnlockedTier = i;
            }
        }

        if (maxUnlockedTier <= lastCheckedTier) return;

        if (maxUnlockedTier >= 1 && !unlockedAchievements.Contains(achievementTier1))
        {
            UnlockAchievement(achievementTier1, "1계층 발견");
        }

        lastCheckedTier = maxUnlockedTier;
    }

    /// <summary>
    /// 성취도 달성 처리
    /// </summary>
    private void UnlockAchievement(string achievementId, string achievementName)
    {
        if (string.IsNullOrEmpty(achievementId))
        {
            Debug.LogWarning($"[AchievementManager] 성취도 ID가 설정되지 않았습니다: {achievementName}");
            return;
        }

        if (unlockedAchievements.Contains(achievementId)) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Social.localUser.authenticated)
        {
            Debug.LogWarning($"[AchievementManager] 로그인되지 않아 성취도를 달성할 수 없습니다: {achievementName}");
            return;
        }

        try
        {
            Social.ReportProgress(achievementId, 100.0, (success) =>
            {
                if (success)
                {
                    unlockedAchievements.Add(achievementId);
                    Debug.Log($"[AchievementManager] 성취도 달성: {achievementName}");
                }
                else
                {
                    Debug.LogWarning($"[AchievementManager] 성취도 달성 실패: {achievementName}");
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"[AchievementManager] 성취도 달성 중 예외 발생: {e.Message}");
        }
#else
        // 에디터에서는 달성 처리만
        unlockedAchievements.Add(achievementId);
        Debug.Log($"[AchievementManager] 성취도 달성 (에디터): {achievementName}");
#endif
    }

    /// <summary>
    /// 성취도 UI 표시
    /// </summary>
    public void ShowAchievements()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Social.localUser.authenticated)
        {
            Debug.LogWarning("[AchievementManager] 성취도를 표시하려면 먼저 로그인해야 합니다.");
            if (CloudSaveManager.Instance != null)
            {
                CloudSaveManager.Instance.SignIn((success) =>
                {
                    if (success) ShowAchievements();
                });
            }
            return;
        }

        try
        {
            Social.ShowAchievementsUI();
        }
        catch (Exception e)
        {
            Debug.LogError($"[AchievementManager] 성취도 UI 표시 중 예외 발생: {e.Message}");
        }
#else
        Debug.Log("[AchievementManager] 에디터에서는 성취도를 표시할 수 없습니다.");
#endif
    }
}
