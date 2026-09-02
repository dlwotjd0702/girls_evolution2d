// ============================================================================
// CloudSaveManager.cs
// - Google Play Games SDK 기반 클라우드 저장 시스템
// - 로그인, 클라우드 저장/로드, 충돌 해결 기능 제공
// - 로컬 저장과 병행하여 안정성 확보
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
#endif

public class CloudSaveManager : MonoBehaviour
{
    public enum LoginResult
    {
        LoginRequired,
        InProgress,
        Success,
        Canceled,
        InternalError,
        Unavailable
    }

    public static CloudSaveManager Instance { get; private set; }

    // ── 이벤트 ──
    public event Action<bool> OnLoginStatusChanged; // 로그인 상태 변경
    public event Action<bool, string> OnCloudSaveComplete; // 클라우드 저장 완료 (성공 여부, 에러 메시지)
    public event Action<bool, SaveData> OnCloudLoadComplete; // 클라우드 로드 완료 (성공 여부, 로드된 데이터)
    public event Action<SaveData, SaveData> OnConflictDetected; // 충돌 감지 (로컬, 클라우드)

    // ── 상태 ──
    public bool IsAuthenticated { get; private set; } = false;
    public bool IsSaving { get; private set; } = false;
    public bool IsLoading { get; private set; } = false;
    public bool IsLoginInProgress { get; private set; } = false;
    public LoginResult LastLoginResult { get; private set; } = LoginResult.LoginRequired;
    public string LastLoginStatusCode { get; private set; } = string.Empty;

    private const string CLOUD_SAVE_FILENAME = "girls_evolution_save";
    private const string FIRST_RUN_KEY = "CloudSaveManager_FirstRun";
    private readonly List<Action<SaveData>> pendingLoadCallbacks = new List<Action<SaveData>>();

    [Header("Login Retry")]
    [SerializeField] private bool enableAutoRetryLogin = true;
    [SerializeField] private float retryLoginInterval = 30f;
#if UNITY_ANDROID && !UNITY_EDITOR
    private ISavedGameClient savedGameClient;
#endif

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
        InitializeGooglePlayGames();
        if (enableAutoRetryLogin)
        {
            StartCoroutine(AutoRetryLoginLoop());
        }
    }

    /// <summary>
    /// Google Play Games 초기화 및 로그인 시도 (최초 실행 시에만)
    /// </summary>
    private void InitializeGooglePlayGames()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // 최초 실행 여부 확인
        bool isFirstRun = !PlayerPrefs.HasKey(FIRST_RUN_KEY);
        
        if (!isFirstRun)
        {
            // 최초 실행이 아니면 SDK만 활성화하고 로그인 시도하지 않음
            try
            {
                PlayGamesPlatform.Activate();
                Debug.Log("[CloudSaveManager] SDK 활성화 완료 (최초 실행이 아니므로 자동 로그인 시도 안 함)");
                
                // 이미 로그인되어 있을 수 있으므로 확인
                if (PlayGamesPlatform.Instance != null && PlayGamesPlatform.Instance.IsAuthenticated())
                {
                    IsAuthenticated = true;
                    LastLoginResult = LoginResult.Success;
                    LastLoginStatusCode = SignInStatus.Success.ToString();
                    savedGameClient = PlayGamesPlatform.Instance.SavedGame;
                    Debug.Log("[CloudSaveManager] 이미 로그인되어 있습니다.");
                    OnLoginStatusChanged?.Invoke(true);
                }
                else
                {
                    IsAuthenticated = false;
                    LastLoginResult = LoginResult.LoginRequired;
                    LastLoginStatusCode = string.Empty;
                    OnLoginStatusChanged?.Invoke(false);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveManager] SDK 활성화 실패: {e.Message}");
                IsAuthenticated = false;
                IsLoginInProgress = false;
                LastLoginResult = LoginResult.Unavailable;
                LastLoginStatusCode = e.GetType().Name;
                OnLoginStatusChanged?.Invoke(false);
            }
            return;
        }
        
        // 최초 실행 시에만 로그인 시도
        try
        {
            PlayGamesPlatform.Activate();

            IsLoginInProgress = true;
            LastLoginResult = LoginResult.InProgress;
            LastLoginStatusCode = string.Empty;
            PlayGamesPlatform.Instance.Authenticate((status) =>
            {
                bool success = ApplyLoginResult(status);
                
                // 최초 실행 플래그 저장 (로그인 시도 완료 후 저장)
                PlayerPrefs.SetInt(FIRST_RUN_KEY, 1);
                PlayerPrefs.Save();
                
                // 더 자세한 로깅
                string statusMessage = status switch
                {
                    SignInStatus.Success => "성공",
                    SignInStatus.Canceled => "취소됨 (사용자가 로그인을 취소했거나 Google Play Games가 설정되지 않음)",
                    SignInStatus.InternalError => "내부 오류 (네트워크 오류 또는 Google Play Games 서비스 문제)",
                    _ => "알 수 없는 상태"
                };
                
                Debug.Log($"[CloudSaveManager] 최초 실행 로그인 결과: {success}, 상태: {status} ({statusMessage})");
                OnLoginStatusChanged?.Invoke(success);
                
                if (success)
                {
                    savedGameClient = PlayGamesPlatform.Instance.SavedGame;
                    Debug.Log("[CloudSaveManager] Google Play Games 로그인 성공 (최초 실행)");
                }
                else
                {
                    Debug.LogWarning($"[CloudSaveManager] Google Play Games 로그인 실패 (최초 실행): {status}");
                    if (status == SignInStatus.Canceled)
                    {
                        Debug.LogWarning("[CloudSaveManager] 자동 로그인이 취소되었습니다. 사용자가 설정에서 수동으로 로그인할 수 있습니다.");
                    }
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"[CloudSaveManager] 초기화 실패: {e.Message}");
            IsAuthenticated = false;
            IsLoginInProgress = false;
            LastLoginResult = LoginResult.Unavailable;
            LastLoginStatusCode = e.GetType().Name;
            OnLoginStatusChanged?.Invoke(false);
        }
#else
        Debug.Log("[CloudSaveManager] 에디터 또는 비 Android 플랫폼에서는 클라우드 저장이 비활성화됩니다.");
        IsAuthenticated = false;
        IsLoginInProgress = false;
        LastLoginResult = LoginResult.Unavailable;
        LastLoginStatusCode = "UnsupportedPlatform";
        OnLoginStatusChanged?.Invoke(false);
#endif
    }

    /// <summary>
    /// 수동 로그인 시도
    /// </summary>
    public void SignIn(Action<bool> callback = null)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (IsLoginInProgress)
        {
            Debug.Log("[CloudSaveManager] 로그인 진행 중입니다.");
            return;
        }
        if (IsAuthenticated)
        {
            Debug.Log("[CloudSaveManager] 이미 로그인되어 있습니다.");
            callback?.Invoke(true);
            return;
        }

        IsLoginInProgress = true;
        LastLoginResult = LoginResult.InProgress;
        LastLoginStatusCode = string.Empty;
        PlayGamesPlatform.Instance.Authenticate((status) =>
        {
            bool success = ApplyLoginResult(status);
            
            // 더 자세한 로깅
            string statusMessage = status switch
            {
                SignInStatus.Success => "성공",
                SignInStatus.Canceled => "취소됨 (사용자가 로그인을 취소했거나 Google Play Games가 설정되지 않음)",
                SignInStatus.InternalError => "내부 오류 (네트워크 오류 또는 Google Play Games 서비스 문제)",
                _ => "알 수 없는 상태"
            };
            
            Debug.Log($"[CloudSaveManager] 수동 로그인 결과: {success}, 상태: {status} ({statusMessage})");
            
            if (!success)
            {
                // 실패 원인에 따른 상세 로그
                if (status == SignInStatus.Canceled)
                {
                    Debug.LogWarning("[CloudSaveManager] 로그인이 취소되었습니다. 가능한 원인:");
                    Debug.LogWarning("  1. 사용자가 로그인 다이얼로그에서 취소");
                    Debug.LogWarning("  2. 기기에 Google 계정이 로그인되어 있지 않음");
                    Debug.LogWarning("  3. Google Play Games 앱이 설치되어 있지 않음");
                    Debug.LogWarning("  4. Google Play Games 서비스가 비활성화됨");
                }
                else if (status == SignInStatus.InternalError)
                {
                    Debug.LogError("[CloudSaveManager] 로그인 중 내부 오류가 발생했습니다. 가능한 원인:");
                    Debug.LogError("  1. 네트워크 연결 문제");
                    Debug.LogError("  2. Google Play Games 서비스 일시적 오류");
                    Debug.LogError("  3. 앱 ID 설정 오류");
                }
            }
            
            OnLoginStatusChanged?.Invoke(success);
            callback?.Invoke(success);
            
            if (success)
            {
                savedGameClient = PlayGamesPlatform.Instance.SavedGame;
                Debug.Log("[CloudSaveManager] Google Play Games 로그인 성공 및 SavedGameClient 초기화 완료");
            }
        });
#else
        Debug.Log("[CloudSaveManager] 에디터에서는 로그인할 수 없습니다.");
        IsAuthenticated = false;
        IsLoginInProgress = false;
        LastLoginResult = LoginResult.Unavailable;
        LastLoginStatusCode = "UnsupportedPlatform";
        callback?.Invoke(false);
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private bool ApplyLoginResult(SignInStatus status)
    {
        bool success = status == SignInStatus.Success;
        IsAuthenticated = success;
        IsLoginInProgress = false;
        LastLoginStatusCode = status.ToString();

        switch (status)
        {
            case SignInStatus.Success:
                LastLoginResult = LoginResult.Success;
                savedGameClient = PlayGamesPlatform.Instance.SavedGame;
                break;
            case SignInStatus.Canceled:
                LastLoginResult = LoginResult.Canceled;
                break;
            case SignInStatus.InternalError:
                LastLoginResult = LoginResult.InternalError;
                break;
            default:
                LastLoginResult = LoginResult.InternalError;
                break;
        }

        return success;
    }
#endif

    /// <summary>
    /// 플랫폼 로그인 상태를 다시 확인하고 필요 시 내부 상태를 동기화
    /// </summary>
    public bool RefreshAuthenticationState(bool notifyIfChanged = true)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        bool previous = IsAuthenticated;
        bool platformAuth = false;
        try
        {
            if (PlayGamesPlatform.Instance != null)
            {
                platformAuth = PlayGamesPlatform.Instance.IsAuthenticated();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CloudSaveManager] 로그인 상태 확인 실패: {e.Message}");
        }

        if (platformAuth != previous)
        {
            IsAuthenticated = platformAuth;
            if (platformAuth && PlayGamesPlatform.Instance != null)
            {
                LastLoginResult = LoginResult.Success;
                LastLoginStatusCode = SignInStatus.Success.ToString();
                savedGameClient = PlayGamesPlatform.Instance.SavedGame;
            }
            else if (!platformAuth)
            {
                LastLoginResult = LoginResult.LoginRequired;
                LastLoginStatusCode = string.Empty;
            }
            if (notifyIfChanged)
            {
                OnLoginStatusChanged?.Invoke(IsAuthenticated);
            }
        }
        return IsAuthenticated;
#else
        if (IsAuthenticated)
        {
            IsAuthenticated = false;
            LastLoginResult = LoginResult.Unavailable;
            LastLoginStatusCode = "UnsupportedPlatform";
            if (notifyIfChanged)
            {
                OnLoginStatusChanged?.Invoke(false);
            }
        }
        return false;
#endif
    }

    private System.Collections.IEnumerator AutoRetryLoginLoop()
    {
        while (true)
        {
            if (!IsAuthenticated && !IsLoginInProgress
#if UNITY_ANDROID && !UNITY_EDITOR
                && true
#else
                && false
#endif
            )
            {
                SignIn();
            }
            yield return new WaitForSeconds(retryLoginInterval);
        }
    }

    /// <summary>
    /// 로그아웃 (Google Play Games SDK에서는 SignOut 메서드가 제거됨)
    /// 상태만 초기화하고 실제 로그아웃은 사용자가 시스템 설정에서 처리해야 함
    /// </summary>
    public void SignOut()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Google Play Games SDK v2.1.0 이상에서는 SignOut 메서드가 제거됨
        // 사용자는 시스템 설정에서 Google 계정 로그아웃을 해야 함
        IsAuthenticated = false;
        IsLoginInProgress = false;
        LastLoginResult = LoginResult.LoginRequired;
        LastLoginStatusCode = string.Empty;
        savedGameClient = null;
        Debug.Log("[CloudSaveManager] 로그아웃 상태로 초기화 완료 (실제 로그아웃은 시스템 설정에서 처리 필요)");
        OnLoginStatusChanged?.Invoke(false);
#else
        Debug.Log("[CloudSaveManager] 에디터에서는 로그아웃할 수 없습니다.");
#endif
    }

    /// <summary>
    /// 클라우드에 저장 (오프라인 환경에서도 안전하게 처리)
    /// </summary>
    public void SaveToCloud(SaveData saveData, bool forceOverwrite = false)
    {
        if (!IsAuthenticated)
        {
            Debug.LogWarning("[CloudSaveManager] 로그인되지 않아 클라우드 저장을 건너뜁니다.");
            OnCloudSaveComplete?.Invoke(false, "로그인되지 않음");
            return;
        }

        if (IsSaving)
        {
            Debug.LogWarning("[CloudSaveManager] 이미 저장 중입니다.");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            IsSaving = true;
            string saveJson = JsonUtility.ToJson(saveData, true);
            byte[] saveBytes = Encoding.UTF8.GetBytes(saveJson);

            SavedGameMetadataUpdate.Builder updateBuilder = new SavedGameMetadataUpdate.Builder()
                .WithUpdatedDescription($"Saved at {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                .WithUpdatedPlayedTime(TimeSpan.FromSeconds(saveData.totalPlayTimeSeconds));

            SavedGameMetadataUpdate metadataUpdate = updateBuilder.Build();

            // 오프라인 환경을 고려하여 ReadCacheOrNetwork 사용 (캐시 우선)
            savedGameClient.OpenWithAutomaticConflictResolution(
                CLOUD_SAVE_FILENAME,
                DataSource.ReadCacheOrNetwork, // 오프라인에서도 캐시 사용 가능
                // Automatic open cannot use UseManual: the Android SDK treats that
                // combination as an unhandled strategy when a real conflict occurs.
                // CommitUpdate below always writes saveBytes, so choosing a valid
                // snapshot here still makes the caller's data the final revision.
                forceOverwrite ? ConflictResolutionStrategy.UseUnmerged : ConflictResolutionStrategy.UseMostRecentlySaved,
                (status, game) =>
                {
                    try
                    {
                        if (status == SavedGameRequestStatus.Success)
                        {
                            savedGameClient.CommitUpdate(
                                game,
                                metadataUpdate,
                                saveBytes,
                                (commitStatus, committedGame) =>
                                {
                                    IsSaving = false;
                                    if (commitStatus == SavedGameRequestStatus.Success)
                                    {
                                        Debug.Log("[CloudSaveManager] 클라우드 저장 성공");
                                        OnCloudSaveComplete?.Invoke(true, null);
                                    }
                                    else
                                    {
                                        // 네트워크 오류 등으로 실패해도 예외 발생하지 않음
                                        Debug.LogWarning($"[CloudSaveManager] 클라우드 저장 실패 (오프라인 가능): {commitStatus}");
                                        OnCloudSaveComplete?.Invoke(false, commitStatus.ToString());
                                    }
                                });
                        }
                        else
                        {
                            IsSaving = false;
                            // 네트워크 오류 등으로 실패해도 예외 발생하지 않음
                            Debug.LogWarning($"[CloudSaveManager] 클라우드 파일 열기 실패 (오프라인 가능): {status}");
                            OnCloudSaveComplete?.Invoke(false, status.ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        IsSaving = false;
                        Debug.LogError($"[CloudSaveManager] 클라우드 저장 중 예외 발생: {e.Message}");
                        OnCloudSaveComplete?.Invoke(false, $"예외: {e.Message}");
                    }
                });
        }
        catch (Exception e)
        {
            IsSaving = false;
            Debug.LogError($"[CloudSaveManager] 클라우드 저장 초기화 중 예외 발생: {e.Message}");
            OnCloudSaveComplete?.Invoke(false, $"예외: {e.Message}");
        }
#else
        IsSaving = false;
        Debug.Log("[CloudSaveManager] 에디터에서는 클라우드 저장을 건너뜁니다.");
        OnCloudSaveComplete?.Invoke(false, "에디터 모드");
#endif
    }

    /// <summary>
    /// 클라우드에서 로드
    /// </summary>
    public void LoadFromCloud(Action<SaveData> onComplete = null)
    {
        if (onComplete != null)
        {
            pendingLoadCallbacks.Add(onComplete);
        }

        if (!IsAuthenticated)
        {
            Debug.LogWarning("[CloudSaveManager] 로그인되지 않아 클라우드 로드를 건너뜁니다.");
            CompleteCloudLoad(false, null);
            return;
        }

        if (IsLoading)
        {
            Debug.Log("[CloudSaveManager] 이미 로드 중이므로 완료 콜백을 기존 요청에 합칩니다.");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // savedGameClient가 초기화되지 않았으면 대기
        if (savedGameClient == null)
        {
            Debug.LogWarning("[CloudSaveManager] savedGameClient가 초기화되지 않았습니다. 잠시 후 다시 시도하세요.");
            CompleteCloudLoad(false, null);
            return;
        }

        try
        {
            IsLoading = true;

            savedGameClient.OpenWithAutomaticConflictResolution(
                CLOUD_SAVE_FILENAME,
                DataSource.ReadCacheOrNetwork, // 오프라인에서도 캐시 사용 가능
                ConflictResolutionStrategy.UseLongestPlaytime,
                (status, game) =>
                {
                    try
                    {
                        if (status == SavedGameRequestStatus.Success)
                        {
                            savedGameClient.ReadBinaryData(
                                game,
                                (readStatus, data) =>
                                {
                                    if (readStatus == SavedGameRequestStatus.Success && data != null)
                                    {
                                        try
                                        {
                                            string saveJson = Encoding.UTF8.GetString(data);
                                            SaveData saveData = JsonUtility.FromJson<SaveData>(saveJson);
                                            Debug.Log("[CloudSaveManager] 클라우드 로드 성공");
                                            CompleteCloudLoad(true, saveData);
                                        }
                                        catch (Exception e)
                                        {
                                            Debug.LogError($"[CloudSaveManager] 클라우드 데이터 파싱 실패: {e.Message}");
                                            CompleteCloudLoad(false, null);
                                        }
                                    }
                                    else
                                    {
                                        // 네트워크 오류 등으로 실패해도 예외 발생하지 않음
                                        Debug.LogWarning($"[CloudSaveManager] 클라우드 저장 파일이 없거나 오프라인: {readStatus}");
                                        CompleteCloudLoad(false, null);
                                    }
                                });
                        }
                        else
                        {
                            // 네트워크 오류 등으로 실패해도 예외 발생하지 않음
                            Debug.LogWarning($"[CloudSaveManager] 클라우드 파일 열기 실패 (오프라인 가능): {status}");
                            CompleteCloudLoad(false, null);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[CloudSaveManager] 클라우드 로드 중 예외 발생: {e.Message}");
                        CompleteCloudLoad(false, null);
                    }
                });
        }
        catch (Exception e)
        {
            Debug.LogError($"[CloudSaveManager] 클라우드 로드 초기화 중 예외 발생: {e.Message}");
            CompleteCloudLoad(false, null);
        }
#else
        Debug.Log("[CloudSaveManager] 에디터에서는 클라우드 로드를 건너뜁니다.");
        CompleteCloudLoad(false, null);
#endif
    }

    private void CompleteCloudLoad(bool success, SaveData data)
    {
        IsLoading = false;
        OnCloudLoadComplete?.Invoke(success, data);

        if (pendingLoadCallbacks.Count == 0) return;
        var callbacks = pendingLoadCallbacks.ToArray();
        pendingLoadCallbacks.Clear();
        foreach (var callback in callbacks)
        {
            try { callback?.Invoke(success ? data : null); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    /// <summary>
    /// 충돌 해결을 위한 수동 비교 및 선택
    /// </summary>
    public void ResolveConflict(SaveData localData, SaveData cloudData, bool useCloud)
    {
        if (useCloud)
        {
            // 클라우드 데이터 사용
            SaveManager.Instance?.LoadGame(); // 일단 로컬 로드
            // 이후 클라우드 데이터를 적용하도록 처리 필요
            Debug.Log("[CloudSaveManager] 클라우드 데이터를 사용합니다.");
        }
        else
        {
            // 로컬 데이터 사용 (강제 업로드)
            SaveToCloud(localData, forceOverwrite: true);
            Debug.Log("[CloudSaveManager] 로컬 데이터를 클라우드에 강제 업로드합니다.");
        }
    }

    /// <summary>
    /// 로컬과 클라우드 동기화 (자동 충돌 해결)
    /// </summary>
    public void SyncWithCloud()
    {
        if (!IsAuthenticated)
        {
            Debug.LogWarning("[CloudSaveManager] 로그인되지 않아 동기화를 건너뜁니다.");
            return;
        }

        LoadFromCloud((cloudData) =>
        {
            if (cloudData == null)
            {
                // 클라우드에 저장 파일이 없으면 로컬을 업로드
                if (SaveManager.Instance != null && System.IO.File.Exists(SaveManager.Instance.GetSaveFilePath()))
                {
                    try
                    {
                        string localJson = System.IO.File.ReadAllText(SaveManager.Instance.GetSaveFilePath());
                        SaveData localSaveData = JsonUtility.FromJson<SaveData>(localJson);
                        if (localSaveData != null)
                        {
                            SaveToCloud(localSaveData);
                            Debug.Log("[CloudSaveManager] 클라우드에 저장 파일이 없어 로컬 데이터를 업로드합니다.");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[CloudSaveManager] 로컬 데이터 업로드 실패: {e.Message}");
                    }
                }
                return;
            }

            // 클라우드 데이터가 있으면 최신 세이브 시간 비교
            SaveData localData = null;
            if (SaveManager.Instance != null && System.IO.File.Exists(SaveManager.Instance.GetSaveFilePath()))
            {
                try
                {
                    string localJson = System.IO.File.ReadAllText(SaveManager.Instance.GetSaveFilePath());
                    localData = JsonUtility.FromJson<SaveData>(localJson);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CloudSaveManager] 로컬 데이터 읽기 실패: {e.Message}");
                }
            }

            if (localData != null)
            {
                bool localTimeValid = SaveManager.TryGetSaveTimeUtc(localData, out DateTime localTime);
                bool cloudTimeValid = SaveManager.TryGetSaveTimeUtc(cloudData, out DateTime cloudTime);

                if (cloudTimeValid && localTimeValid)
                {
                    if (cloudTime > localTime)
                    {
                        // 클라우드가 더 최신 - 클라우드 데이터 사용 권장
                        Debug.Log("[CloudSaveManager] 클라우드 데이터가 더 최신입니다. 클라우드 데이터 사용을 권장합니다.");
                        OnConflictDetected?.Invoke(localData, cloudData);
                    }
                    else if (localTime > cloudTime)
                    {
                        // 로컬이 더 최신 - 로컬 데이터 업로드
                        Debug.Log("[CloudSaveManager] 로컬 데이터가 더 최신입니다. 클라우드에 업로드합니다.");
                        SaveToCloud(localData);
                    }
                    else
                    {
                        // 시간이 같으면 충돌 없음
                        Debug.Log("[CloudSaveManager] 로컬과 클라우드 데이터가 동일한 시간입니다.");
                    }
                }
                else if (cloudTimeValid)
                {
                    // 로컬 시간이 유효하지 않으면 클라우드 사용
                    Debug.Log("[CloudSaveManager] 로컬 데이터 시간이 유효하지 않아 클라우드 데이터 사용을 권장합니다.");
                    OnConflictDetected?.Invoke(localData, cloudData);
                }
                else if (localTimeValid)
                {
                    // 클라우드 시간이 유효하지 않으면 로컬 업로드
                    Debug.Log("[CloudSaveManager] 클라우드 데이터 시간이 유효하지 않아 로컬 데이터를 업로드합니다.");
                    SaveToCloud(localData);
                }
            }
            else
            {
                // 로컬 데이터가 없으면 클라우드 데이터를 로드할 수 있음 (UI에서 처리)
                Debug.Log("[CloudSaveManager] 로컬 데이터가 없습니다. 클라우드 데이터 사용 가능.");
            }
        });
    }
}
