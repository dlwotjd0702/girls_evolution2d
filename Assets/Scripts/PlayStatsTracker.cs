// ============================================================================
// PlayStatsTracker.cs
// - 플레이 통계 수집 및 저장 (밸런스 패치용)
// - ISaveable 인터페이스 구현하여 SaveManager와 자동 연동
// - 싱글톤 패턴으로 전역 접근 가능
// ============================================================================

using System;
using UnityEngine;

public class PlayStatsTracker : MonoBehaviour, ISaveable
{
    public static PlayStatsTracker Instance { get; private set; }

    // ── 런타임 통계 (SaveData와 동기화) ──
    private double totalPlayTimeSeconds = 0.0;
    private string firstPlayTime = "";
    private string lastPlayTime = "";
    private int level25ReachedCount = 0;
    private System.Collections.Generic.List<string> level25ReachedTimes = new System.Collections.Generic.List<string>();
    private long totalClicks = 0;
    private long totalMerges = 0;
    private long totalSpawns = 0;
    private double totalGoldEarned = 0.0;
    private double totalGoldSpent = 0.0;

    // ── 플레이타임 추적 ──
    private float sessionStartTime = 0f;
    private bool isTracking = false;

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
        StartTracking();
    }

    private void Update()
    {
        if (isTracking)
        {
            totalPlayTimeSeconds += Time.deltaTime;
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            StopTracking();
        }
        else
        {
            StartTracking();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            StartTracking();
        }
        else
        {
            StopTracking();
        }
    }

    private void StartTracking()
    {
        if (!isTracking)
        {
            isTracking = true;
            sessionStartTime = Time.realtimeSinceStartup;
        }
    }

    private void StopTracking()
    {
        if (isTracking)
        {
            isTracking = false;
            // 세션 시간을 누적 (Update에서 이미 누적되지만, 정확도를 위해)
            float sessionDuration = Time.realtimeSinceStartup - sessionStartTime;
            totalPlayTimeSeconds += sessionDuration;
        }
    }

    // ── 통계 업데이트 메서드 ──

    /// <summary>
    /// 클릭 횟수 증가
    /// </summary>
    public void RecordClick()
    {
        totalClicks++;
    }

    /// <summary>
    /// 합성 횟수 증가
    /// </summary>
    public void RecordMerge()
    {
        totalMerges++;
    }

    /// <summary>
    /// 소환 횟수 증가
    /// </summary>
    public void RecordSpawn()
    {
        totalSpawns++;
    }

    /// <summary>
    /// 골드 획득 기록
    /// </summary>
    public void RecordGoldEarned(double amount)
    {
        if (amount > 0)
        {
            totalGoldEarned += amount;
        }
    }

    /// <summary>
    /// 골드 소비 기록
    /// </summary>
    public void RecordGoldSpent(double amount)
    {
        if (amount > 0)
        {
            totalGoldSpent += amount;
        }
    }

    /// <summary>
    /// 레벨 25 달성 기록
    /// </summary>
    public void RecordLevel25Reached()
    {
        level25ReachedCount++;
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        level25ReachedTimes.Add(timestamp);
    }

    /// <summary>
    /// 최초 플레이 시간 설정 (한 번만)
    /// </summary>
    private void EnsureFirstPlayTime()
    {
        if (string.IsNullOrEmpty(firstPlayTime))
        {
            firstPlayTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    /// <summary>
    /// 마지막 플레이 시간 업데이트
    /// </summary>
    private void UpdateLastPlayTime()
    {
        lastPlayTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    // ── ISaveable 구현 ──

    public void ApplyLoadedData(SaveData data)
    {
        if (data == null) return;

        // 통계 데이터 복원
        totalPlayTimeSeconds = data.totalPlayTimeSeconds;
        firstPlayTime = data.firstPlayTime ?? "";
        lastPlayTime = data.lastPlayTime ?? "";
        level25ReachedCount = data.level25ReachedCount;
        
        if (data.level25ReachedTimes != null)
        {
            level25ReachedTimes = new System.Collections.Generic.List<string>(data.level25ReachedTimes);
        }
        else
        {
            level25ReachedTimes = new System.Collections.Generic.List<string>();
        }
        
        totalClicks = data.totalClicks;
        totalMerges = data.totalMerges;
        totalSpawns = data.totalSpawns;
        totalGoldEarned = data.totalGoldEarned;
        totalGoldSpent = data.totalGoldSpent;

        // 최초 플레이 시간이 없으면 현재 시간으로 설정
        EnsureFirstPlayTime();
    }

    public void CollectSaveData(SaveData data)
    {
        if (data == null) return;

        // 현재 세션 시간 누적
        if (isTracking)
        {
            float sessionDuration = Time.realtimeSinceStartup - sessionStartTime;
            totalPlayTimeSeconds += sessionDuration;
            sessionStartTime = Time.realtimeSinceStartup; // 리셋
        }

        // 최초 플레이 시간 설정
        EnsureFirstPlayTime();
        UpdateLastPlayTime();

        // 통계 데이터 저장
        data.totalPlayTimeSeconds = totalPlayTimeSeconds;
        data.firstPlayTime = firstPlayTime;
        data.lastPlayTime = lastPlayTime;
        data.level25ReachedCount = level25ReachedCount;
        
        if (data.level25ReachedTimes == null)
        {
            data.level25ReachedTimes = new System.Collections.Generic.List<string>();
        }
        data.level25ReachedTimes.Clear();
        data.level25ReachedTimes.AddRange(level25ReachedTimes);
        
        data.totalClicks = totalClicks;
        data.totalMerges = totalMerges;
        data.totalSpawns = totalSpawns;
        data.totalGoldEarned = totalGoldEarned;
        data.totalGoldSpent = totalGoldSpent;
    }

    // ── 디버그용 Getter (선택사항) ──

    public double GetTotalPlayTimeSeconds() => totalPlayTimeSeconds;
    public string GetFirstPlayTime() => firstPlayTime;
    public string GetLastPlayTime() => lastPlayTime;
    public int GetLevel25ReachedCount() => level25ReachedCount;
    public long GetTotalClicks() => totalClicks;
    public long GetTotalMerges() => totalMerges;
    public long GetTotalSpawns() => totalSpawns;
    public double GetTotalGoldEarned() => totalGoldEarned;
    public double GetTotalGoldSpent() => totalGoldSpent;
}

