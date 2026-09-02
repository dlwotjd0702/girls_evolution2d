using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text;
using System.Threading;
// using BackEnd; // 실제 프로젝트에 뒤끝 SDK 네임스페이스 추가

[DefaultExecutionOrder(-200)] // GameSystem(-100)보다 먼저 실행
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    public static SaveData pendingSaveData = null;

    private const string SAVE_FILE_NAME = "save.json";
    private const string BACKUP_FILE_NAME = "save_backup.json";

    private string SaveDirectory
    {
        get
        {
#if UNITY_EDITOR
            string editorDir = Path.Combine(Application.dataPath, "..", "EditorSaves");
            if (!Directory.Exists(editorDir)) Directory.CreateDirectory(editorDir);
            return Path.GetFullPath(editorDir);
#else
            if (!Directory.Exists(Application.persistentDataPath))
                Directory.CreateDirectory(Application.persistentDataPath);
            return Application.persistentDataPath;
#endif
        }
    }

    private string SaveFilePath => Path.Combine(SaveDirectory, SAVE_FILE_NAME);
    private string BackupFilePath => Path.Combine(SaveDirectory, BACKUP_FILE_NAME);
    
    [Header("Auto Save Settings")]
    [Tooltip("자동 저장 간격 (초)")]
    [SerializeField] private float autoSaveInterval = 60f; // 1분
    
    [Header("Cloud Save Settings")]
    [Tooltip("클라우드 저장 활성화")]
    [SerializeField] private bool enableCloudSave = true;
    [Tooltip("로컬 저장 후 클라우드에도 자동 저장")]
    [SerializeField] private bool autoCloudSave = true;
    [Tooltip("게임 시작 시 클라우드와 동기화")]
    [SerializeField] private bool syncOnStart = true;

    // Public 접근자
    public bool EnableCloudSave => enableCloudSave;
    
    [Header("Debug / Maintenance")]
    [Tooltip("시작 시 세이브 데이터를 삭제하고 새 게임으로 시작합니다.")]
    [SerializeField] private bool resetSaveOnStart = false;
    
    
    private float autoSaveTimer = 0f;
    private double pendingOfflineSeconds = 0f;
    private bool saveInProgress;
    private bool cloudReconciliationComplete;
    private const int LOCAL_SAVE_RETRY_COUNT = 4;
    private const int CLOUD_LOAD_RETRY_COUNT = 3;
    private static readonly string[] SaveTimeFormats = { "yyyy-MM-dd HH:mm:ss", "yyyy/MM/dd HH:mm:ss" };

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        cloudReconciliationComplete = !enableCloudSave || !syncOnStart;

        if (resetSaveOnStart)
        {
            Debug.LogWarning("[SaveManager] resetSaveOnStart가 활성화되어 세이브 데이터를 삭제합니다.");
            ResetSaveFile();
        }
        
        // 로컬 저장 우선 로드, 없으면 클라우드에서 복구
        StartCoroutine(LoadGameWithCloudRecovery());
        
        autoSaveTimer = 0f;
    }
    
    void Update()
    {
        // 1분 간격 자동 저장
        autoSaveTimer += Time.unscaledDeltaTime;
        if (autoSaveTimer >= autoSaveInterval)
        {
            autoSaveTimer = 0f;
            SaveGame();
        }
    }

    void OnApplicationPause(bool pause)
    {
        if (pause) SaveGame();
    }

    void OnApplicationQuit()               
    {
        SaveGame();
    }

    // 게임 데이터 저장
    public void SaveGame()
    {
        if (saveInProgress) return;
        saveInProgress = true;

        SaveData data = new SaveData();
        data.SetSaveTime();
        
        // 모든 ISaveable 구현체 찾기 (비활성화된 오브젝트도 포함)
        var saveables = FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveable>().ToList();
        
        foreach (var s in saveables)
        {
            try
            {
            s.CollectSaveData(data);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] {s.GetType().Name} 데이터 수집 실패: {e.Message}");
            }
        }

        string saveJson = JsonUtility.ToJson(data, true);

        try
        {
            WriteLocalSaveAtomically(saveJson);

            // PlayerPrefs도 병행 저장 (호환성 유지)
            PlayerPrefs.SetString("SaveData", saveJson);
            PlayerPrefs.Save();

            TryAutoSaveToCloud(data);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 저장 실패: {e.Message}");
        }
        finally
        {
            saveInProgress = false;
        }
    }

    /// <summary>
    /// 클라우드에서 로드 (수동 동기화)
    /// </summary>
    public void LoadFromCloud()
    {
        if (!enableCloudSave || CloudSaveManager.Instance == null)
        {
            Debug.LogWarning("[SaveManager] 클라우드 저장이 비활성화되어 있습니다.");
            return;
        }

        CloudSaveManager.Instance.LoadFromCloud((cloudData) =>
        {
            if (cloudData != null)
            {
                // 클라우드 데이터 적용
                var saveables = FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveable>().ToList();
                foreach (var s in saveables)
                {
                    try
                    {
                        s.ApplyLoadedData(cloudData);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[SaveManager] 클라우드 데이터 적용 실패 ({s.GetType().Name}): {e.Message}");
                    }
                }
                
                // 로컬에도 저장
                SaveGame();
                Debug.Log("[SaveManager] 클라우드 데이터 로드 및 적용 완료");
            }
            else
            {
                Debug.LogWarning("[SaveManager] 클라우드에서 로드할 데이터가 없습니다.");
            }
        });
    }

    /// <summary>
    /// 클라우드에 수동 저장
    /// </summary>
    public void SaveToCloud()
    {
        if (!enableCloudSave || CloudSaveManager.Instance == null)
        {
            Debug.LogWarning("[SaveManager] 클라우드 저장이 비활성화되어 있습니다.");
            return;
        }

        SaveData data = new SaveData();
        data.SetSaveTime();
        
        var saveables = FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveable>().ToList();
        foreach (var s in saveables)
        {
            try
            {
                s.CollectSaveData(data);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] {s.GetType().Name} 데이터 수집 실패: {e.Message}");
            }
        }

        CloudSaveManager.Instance.SaveToCloud(data);
    }

    /// <summary>
    /// 로컬 저장 우선 로드, 없으면 클라우드에서 복구
    /// 클라우드 저장의 플레이 시간이 더 길면 선택 패널 표시
    /// </summary>
    private System.Collections.IEnumerator LoadGameWithCloudRecovery(bool applyLocalData = true)
    {
        // 1. 로컬 저장 시도
        SaveData localData = null;
        bool localLoadSuccess = applyLocalData
            ? LoadGame(out localData)
            : TryReadLocalSnapshot(out localData);
        
        // 2. 클라우드 저장 확인 (로컬 저장이 있든 없든 확인)
        if (enableCloudSave && syncOnStart)
        {
            // CloudSaveManager 초기화 대기
            float waitTime = 0f;
            while (CloudSaveManager.Instance == null && waitTime < 5f)
            {
                yield return new WaitForSeconds(0.5f);
                waitTime += 0.5f;
            }
            
            if (CloudSaveManager.Instance == null)
            {
                // CloudSaveManager가 없으면 로컬 저장만 사용
                cloudReconciliationComplete = true;
                if (!localLoadSuccess)
                {
                    Debug.LogWarning("[SaveManager] CloudSaveManager를 찾을 수 없습니다. 새 게임을 시작합니다.");
                    InitializeLocalizationManagerForNewGame();
                }
                yield break;
            }
            
            // 로그인 대기
            waitTime = 0f;
            while (!CloudSaveManager.Instance.IsAuthenticated && waitTime < 10f)
            {
                yield return new WaitForSeconds(0.5f);
                waitTime += 0.5f;
            }

            if (!CloudSaveManager.Instance.IsAuthenticated)
            {
                // 자동 로그인/네트워크 복구가 늦게 끝나더라도 기존 클라우드 저장을
                // 이번 실행에서 다시 확인한다. 재시도 때는 로컬 데이터를 재적용하지 않는다.
                CloudSaveManager.Instance.OnLoginStatusChanged -= OnLateCloudLoginStatusChanged;
                CloudSaveManager.Instance.OnLoginStatusChanged += OnLateCloudLoginStatusChanged;
                if (!localLoadSuccess) InitializeLocalizationManagerForNewGame();
                yield break;
            }

            CloudSaveManager.Instance.OnLoginStatusChanged -= OnLateCloudLoginStatusChanged;
            var cloudManager = CloudSaveManager.Instance;
            
            // 클라우드에서 로드 시도. 일시적인 네트워크 오류를 '빈 클라우드'로
            // 오인해 새 로컬 게임을 업로드하지 않도록 성공 여부도 별도로 추적한다.
            bool cloudLoadComplete = false;
            bool cloudLoadSucceeded = false;
            SaveData cloudData = null;

            for (int attempt = 0; attempt < CLOUD_LOAD_RETRY_COUNT && !cloudLoadSucceeded; attempt++)
            {
                cloudLoadComplete = false;
                System.Action<bool, SaveData> loadStatus = null;
                loadStatus = (success, _) =>
                {
                    cloudLoadSucceeded = success;
                    cloudManager.OnCloudLoadComplete -= loadStatus;
                };
                cloudManager.OnCloudLoadComplete += loadStatus;
                cloudManager.LoadFromCloud((cloudSaveData) =>
                {
                    cloudData = cloudSaveData;
                    cloudLoadComplete = true;
                });

                waitTime = 0f;
                while (!cloudLoadComplete && waitTime < 10f)
                {
                    yield return new WaitForSeconds(0.5f);
                    waitTime += 0.5f;
                }
                cloudManager.OnCloudLoadComplete -= loadStatus;

                if (!cloudLoadSucceeded && attempt + 1 < CLOUD_LOAD_RETRY_COUNT)
                    yield return new WaitForSeconds(1f + attempt);
            }

            if (!cloudLoadSucceeded)
            {
                Debug.LogWarning("[SaveManager] 클라우드 읽기에 실패해 자동 업로드를 잠급니다. 다음 실행에서 다시 동기화합니다.");
                if (!localLoadSuccess) InitializeLocalizationManagerForNewGame();
                yield break;
            }

            cloudReconciliationComplete = true;

            if (localLoadSuccess && cloudData == null)
            {
                Debug.Log("[SaveManager] 클라우드 저장이 없어 확인된 로컬 데이터를 업로드합니다.");
                TryAutoSaveToCloud(localData);
                yield break;
            }

            if (!localLoadSuccess && cloudData == null)
            {
                InitializeLocalizationManagerForNewGame();
                yield break;
            }
            
            // 로컬 저장이 없고 클라우드 저장이 있으면 클라우드에서 복구
            if (!localLoadSuccess && cloudData != null)
            {
                Debug.Log("[SaveManager] 로컬 저장이 없어 클라우드에서 복구합니다.");
                ApplySaveData(cloudData);
                SaveToLocal(cloudData);
                yield break;
            }
            
            // 둘 다 있으면 플레이 시간 비교
            if (localLoadSuccess && cloudData != null)
            {
                double localPlayTime = localData != null && localData.totalPlayTimeSeconds > 0 ? 
                    localData.totalPlayTimeSeconds / 3600.0 : 0.0;
                double cloudPlayTime = cloudData != null && cloudData.totalPlayTimeSeconds > 0 ? 
                    cloudData.totalPlayTimeSeconds / 3600.0 : 0.0;
                
                // 현행 저장은 플레이 시간을 우선하고, 구세이브처럼 플레이 시간이
                // 없는 경우에는 UTC 저장 시각과 진행도를 차례로 비교한다.
                if (CompareSavePriority(cloudData, localData) > 0)
                {
                    Debug.Log($"[SaveManager] 클라우드 저장의 플레이 시간이 더 깁니다. (로컬: {localPlayTime:F2}시간, 클라우드: {cloudPlayTime:F2}시간)");
                    
                    // 선택 패널 표시
                    SaveConflictPanel conflictPanel = FindObjectOfType<SaveConflictPanel>(true);
                    if (conflictPanel != null)
                    {
                        bool choiceMade = false;
                        bool useCloud = false;
                        
                        conflictPanel.Show(localPlayTime, cloudPlayTime, (useCloudSave) =>
                        {
                            useCloud = useCloudSave;
                            choiceMade = true;
                            
                            if (useCloud)
                            {
                                Debug.Log("[SaveManager] 사용자가 클라우드 저장을 선택했습니다.");
                                ApplySaveData(cloudData);
                                SaveToLocal(cloudData);
                            }
                            else
                            {
                                Debug.Log("[SaveManager] 사용자가 로컬 저장을 선택했습니다.");
                                // 로컬 저장을 클라우드에 업로드
                                SaveData currentData = new SaveData();
                                currentData.SetSaveTime();
                                var saveables = FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveable>().ToList();
                                foreach (var s in saveables)
                                {
                                    try
                                    {
                                        s.CollectSaveData(currentData);
                                    }
                                    catch (System.Exception e)
                                    {
                                        Debug.LogError($"[SaveManager] {s.GetType().Name} 데이터 수집 실패: {e.Message}");
                                    }
                                }
                                CloudSaveManager.Instance?.SaveToCloud(currentData, forceOverwrite: true);
                            }
                        });
                        
                        // 선택 완료 대기
                        while (!choiceMade)
                        {
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                    else
                    {
                        // 패널이 없으면 자동으로 클라우드 사용
                        Debug.LogWarning("[SaveManager] SaveConflictPanel을 찾을 수 없습니다. 클라우드 저장을 자동으로 사용합니다.");
                        ApplySaveData(cloudData);
                        SaveToLocal(cloudData);
                    }
                }
                else
                {
                    // 로컬이 더 길거나 같으면 로컬 사용 (이미 로드됨)
                    Debug.Log($"[SaveManager] 로컬 저장의 플레이 시간이 더 깁니다. (로컬: {localPlayTime:F2}시간, 클라우드: {cloudPlayTime:F2}시간)");
                    if (CompareSavePriority(localData, cloudData) > 0)
                        TryAutoSaveToCloud(localData);
                }
            }
        }
        else if (!localLoadSuccess)
        {
            // 클라우드 저장이 비활성화되어 있고 로컬 저장도 없으면 새 게임 시작
            Debug.Log("[SaveManager] 로컬 저장이 없고 클라우드 저장이 비활성화되어 있습니다. 새 게임을 시작합니다.");
            InitializeLocalizationManagerForNewGame();
        }
    }

    /// <summary>
    /// 저장 데이터를 게임에 적용
    /// </summary>
    private void ApplySaveData(SaveData data)
    {
        if (data == null) return;
        
        CaptureOfflineDuration(data);
        
        var saveables = FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveable>().ToList();
        foreach (var s in saveables)
        {
            try
            {
                s.ApplyLoadedData(data);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] {s.GetType().Name} 데이터 적용 실패: {e.Message}");
            }
        }
        
        Debug.Log("[SaveManager] 저장 데이터 적용 완료");
    }

    /// <summary>
    /// 저장 데이터를 로컬에 저장
    /// </summary>
    private void SaveToLocal(SaveData data)
    {
        if (data == null) return;
        
        string saveJson = JsonUtility.ToJson(data, true);
        try
        {
            WriteLocalSaveAtomically(saveJson);
            PlayerPrefs.SetString("SaveData", saveJson);
            PlayerPrefs.Save();
            Debug.Log("[SaveManager] 로컬에 저장 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 로컬 저장 실패: {e.Message}");
        }
    }

    private void TryAutoSaveToCloud(SaveData data)
    {
        var cloud = CloudSaveManager.Instance;
        if (!autoCloudSave || !enableCloudSave || !cloudReconciliationComplete || data == null
            || cloud == null || !cloud.IsAuthenticated || cloud.IsSaving)
            return;

        cloud.SaveToCloud(data);
    }

    private void OnLateCloudLoginStatusChanged(bool authenticated)
    {
        if (!authenticated || CloudSaveManager.Instance == null) return;
        CloudSaveManager.Instance.OnLoginStatusChanged -= OnLateCloudLoginStatusChanged;
        StartCoroutine(LoadGameWithCloudRecovery(false));
    }

    private bool TryReadLocalSnapshot(out SaveData data)
    {
        data = null;
        try
        {
            string json = null;
            if (File.Exists(SaveFilePath)) json = File.ReadAllText(SaveFilePath);
            else if (PlayerPrefs.HasKey("SaveData")) json = PlayerPrefs.GetString("SaveData");
            else if (File.Exists(BackupFilePath)) json = File.ReadAllText(BackupFilePath);
            if (string.IsNullOrWhiteSpace(json)) return false;
            data = JsonUtility.FromJson<SaveData>(json);
            return data != null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] 로컬 스냅샷 비교용 읽기 실패: {e.Message}");
            return false;
        }
    }

    public static int CompareSavePriority(SaveData left, SaveData right)
    {
        if (ReferenceEquals(left, right)) return 0;
        if (left == null) return -1;
        if (right == null) return 1;

        bool leftHasPlayTime = left.totalPlayTimeSeconds > 0.5;
        bool rightHasPlayTime = right.totalPlayTimeSeconds > 0.5;
        if (leftHasPlayTime && rightHasPlayTime)
        {
            int playTimeOrder = left.totalPlayTimeSeconds.CompareTo(right.totalPlayTimeSeconds);
            if (playTimeOrder != 0) return playTimeOrder;
        }

        bool leftHasTime = TryGetSaveTimeUtc(left, out var leftTime);
        bool rightHasTime = TryGetSaveTimeUtc(right, out var rightTime);
        if (leftHasTime && rightHasTime)
        {
            int timeOrder = leftTime.CompareTo(rightTime);
            if (timeOrder != 0) return timeOrder;
        }
        else if (leftHasTime != rightHasTime)
        {
            return leftHasTime ? 1 : -1;
        }

        int levelOrder = left.maxLevelReached.CompareTo(right.maxLevelReached);
        if (levelOrder != 0) return levelOrder;
        return left.gold.CompareTo(right.gold);
    }

    public static bool TryGetSaveTimeUtc(SaveData data, out DateTime utc)
    {
        utc = DateTime.MinValue;
        if (data == null) return false;
        if (data.savedAtUtcUnixSeconds > 0)
        {
            try
            {
                utc = DateTimeOffset.FromUnixTimeSeconds(data.savedAtUtcUnixSeconds).UtcDateTime;
                return true;
            }
            catch (ArgumentOutOfRangeException) { }
        }

        if (string.IsNullOrWhiteSpace(data.savedAt)) return false;
        if (!DateTime.TryParseExact(data.savedAt, SaveTimeFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out var localTime)) return false;
        utc = localTime.ToUniversalTime();
        return true;
    }

    /// <summary>
    /// 완성된 임시 파일을 같은 볼륨에서 교체해 저장 파일이 반쯤 쓰인 상태로 노출되지 않게 한다.
    /// OneDrive/백신/에디터가 잠깐 파일을 잡은 경우에는 짧게 재시도한다.
    /// </summary>
    private void WriteLocalSaveAtomically(string json)
    {
        Directory.CreateDirectory(SaveDirectory);
        string tempPath = Path.Combine(SaveDirectory, SAVE_FILE_NAME + ".tmp");
        Exception lastError = null;

        for (int attempt = 0; attempt < LOCAL_SAVE_RETRY_COUNT; attempt++)
        {
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Flush(true);
                }

                if (File.Exists(SaveFilePath))
                {
                    File.Replace(tempPath, SaveFilePath, BackupFilePath, true);
                }
                else
                {
                    File.Move(tempPath, SaveFilePath);
                }
                return;
            }
            catch (IOException error)
            {
                lastError = error;
                if (attempt + 1 < LOCAL_SAVE_RETRY_COUNT)
                    Thread.Sleep(40 * (attempt + 1));
            }
            catch (UnauthorizedAccessException error)
            {
                lastError = error;
                if (attempt + 1 < LOCAL_SAVE_RETRY_COUNT)
                    Thread.Sleep(40 * (attempt + 1));
            }
        }

        try { if (File.Exists(tempPath)) File.Delete(tempPath); }
        catch (Exception) { }
        throw new IOException($"로컬 저장 파일을 {LOCAL_SAVE_RETRY_COUNT}회 시도했지만 교체하지 못했습니다. 경로: {SaveFilePath}", lastError);
    }

    // 게임 데이터 불러오기 (로컬 저장만 시도)
    public bool LoadGame()
    {
        SaveData data;
        return LoadGame(out data);
    }

    /// <summary>
    /// 게임 데이터 불러오기 (로컬 저장만 시도, 데이터 반환)
    /// </summary>
    public bool LoadGame(out SaveData loadedData)
    {
        loadedData = null;
        string saveJson = null;

        try
        {
            // 1순위: 로컬 파일에서 불러오기
            if (File.Exists(SaveFilePath))
            {
                saveJson = File.ReadAllText(SaveFilePath);
                loadedData = JsonUtility.FromJson<SaveData>(saveJson);
            }
            // 2순위: PlayerPrefs에서 불러오기 (기존 호환성)
            else if (PlayerPrefs.HasKey("SaveData"))
            {
                saveJson = PlayerPrefs.GetString("SaveData");
                loadedData = JsonUtility.FromJson<SaveData>(saveJson);
            }
            else
            {
                // 로컬 저장이 없음 (클라우드 복구는 LoadGameWithCloudRecovery에서 처리)
                return false;
            }

            if (loadedData != null)
            {
                CaptureOfflineDuration(loadedData);
                
                // 모든 ISaveable 구현체 찾기 (비활성화된 오브젝트도 포함)
                var saveables = FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveable>().ToList();
                
                foreach (var s in saveables)
                {
                    try
                    {
                        s.ApplyLoadedData(loadedData);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[SaveManager] {s.GetType().Name} 데이터 적용 실패: {e.Message}");
                    }
                }
                // 데이터 적용은 비동기이므로, GirlFieldManager에서 완료 알림을 받음
                
                Debug.Log("[SaveManager] 로컬 저장 파일에서 로드 완료");
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 불러오기 실패: {e.Message}");
            
            // 백업 파일 시도
            try
            {
                if (File.Exists(BackupFilePath))
                {
                    saveJson = File.ReadAllText(BackupFilePath);
                    loadedData = JsonUtility.FromJson<SaveData>(saveJson);
                    CaptureOfflineDuration(loadedData);

                    var saveables = FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveable>().ToList();
                    foreach (var s in saveables)
                    {
                        try
                        {
                            s.ApplyLoadedData(loadedData);
                        }
                        catch (System.Exception e2)
                        {
                            Debug.LogError($"[SaveManager] {s.GetType().Name} 백업 복구 실패: {e2.Message}");
                        }
                    }
                    
                    Debug.Log("[SaveManager] 백업 파일에서 복구 완료");
                    return true;
                }
            }
            catch (System.Exception e2)
            {
                Debug.LogError($"[SaveManager] 백업 복구 실패: {e2.Message}");
            }
        }
        
        return false; // 로컬 저장 로드 실패
    }
    

    // 세이브 파일 삭제 (테스트용)
    public bool DeleteSaveFile()
    {
        try
        {
            bool deleted = false;
            
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                deleted = true;
            }
            
            if (File.Exists(BackupFilePath))
            {
                File.Delete(BackupFilePath);
            }

            // PlayerPrefs도 삭제
            if (PlayerPrefs.HasKey("SaveData"))
            {
                PlayerPrefs.DeleteKey("SaveData");
                PlayerPrefs.Save();
            }

            Debug.Log("[SaveManager] 세이브 파일 삭제 완료");
            return deleted;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 세이브 파일 삭제 실패: {e.Message}");
            return false;
        }
    }

    // 세이브 파일 리셋 (게임 데이터 초기화)
    public void ResetSaveFile()
    {
        DeleteSaveFile();
        
        // 클라우드 저장도 초기화 (빈 데이터로 덮어쓰기)
        if (enableCloudSave && CloudSaveManager.Instance != null && CloudSaveManager.Instance.IsAuthenticated)
        {
            SaveData emptyData = new SaveData();
            CloudSaveManager.Instance.SaveToCloud(emptyData, forceOverwrite: true);
            Debug.Log("[SaveManager] 클라우드 저장도 초기화 완료.");
        }
        
        Debug.Log("[SaveManager] 세이브 파일 리셋 완료. 다음 저장 시 새 게임으로 시작됩니다.");
    }

    // 세이브 파일 존재 여부 확인
    public bool HasSaveFile()
    {
        return File.Exists(SaveFilePath) || PlayerPrefs.HasKey("SaveData");
    }

    // 세이브 파일 경로 반환 (디버그용)
    public string GetSaveFilePath()
    {
        return SaveFilePath;
    }

    void CaptureOfflineDuration(SaveData data)
    {
        pendingOfflineSeconds = 0f;
        if (data == null) return;
        if (TryGetSaveTimeUtc(data, out var savedUtc))
        {
            pendingOfflineSeconds = Math.Max(0.0, (DateTime.UtcNow - savedUtc).TotalSeconds);
            return;
        }
        string stamp = data.savedAt;
        if (string.IsNullOrEmpty(stamp)) return;

        if (DateTime.TryParseExact(stamp, SaveTimeFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out var savedTime))
        {
            pendingOfflineSeconds = Math.Max(0.0, (DateTime.Now - savedTime).TotalSeconds);
        }
    }

    public bool TryConsumeOfflineSeconds(out double seconds, double minSeconds = 30.0)
    {
        seconds = 0.0;
        if (pendingOfflineSeconds < minSeconds) return false;
        seconds = pendingOfflineSeconds;
        pendingOfflineSeconds = 0.0;
        return true;
    }
    
    /// <summary>
    /// 새 게임 시작 시 LocalizationManager를 시스템 언어로 초기화합니다.
    /// </summary>
    void InitializeLocalizationManagerForNewGame()
    {
        var localizationManager = FindObjectOfType<LocalizationManager>(true);
        if (localizationManager != null)
        {
            // 시스템 언어로 초기화
            bool isKorean = Application.systemLanguage == SystemLanguage.Korean;
            LocalizationManager.SetLanguage(isKorean);
        }
    }
}
