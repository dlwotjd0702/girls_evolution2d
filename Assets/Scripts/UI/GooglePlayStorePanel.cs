// ============================================================================
// GooglePlayStorePanel.cs
// - Google Play Games 로그인 및 클라우드 저장 관리 패널
// - 로컬 저장과 클라우드 저장 데이터 비교 및 선택 기능
// ============================================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GooglePlayStorePanel : MonoBehaviour
{
    [Header("Panel Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Title")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Login Section")]
    [SerializeField] private Button loginButton;
    [SerializeField] private TextMeshProUGUI loginStatusText;
    

    [Header("Cloud Data Section")]
    [SerializeField] private GameObject cloudDataSection; // 로그인 성공 시 표시
    [SerializeField] private Button loadCloudDataButton;
    [SerializeField] private TextMeshProUGUI cloudDataText; // 클라우드 데이터 통합 텍스트

    [Header("Current Data Section")]
    [SerializeField] private TextMeshProUGUI currentDataText; // 현재 데이터 통합 텍스트
    [SerializeField] private Button saveToCloudButton;

    [Header("Confirm Panel")]
    [SerializeField] private CloudDataConfirmPanel confirmPanel; // 클라우드 데이터 적용 확인 패널

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private float feedbackDuration = 2f;
    private Coroutine feedbackRoutine;

    private SaveData cloudData = null;
    private SaveData localData = null;
    private bool isLoadingCloud = false;

    private void Awake()
    {
        ConfigureResponsiveLayout();

        if (panelRoot != null) panelRoot.SetActive(false);

        // 버튼 이벤트 연결
        if (loginButton != null)
        {
            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(OnClickLogin);
        }

        if (loadCloudDataButton != null)
        {
            loadCloudDataButton.onClick.RemoveAllListeners();
            loadCloudDataButton.onClick.AddListener(OnClickLoadCloudData);
        }

        if (saveToCloudButton != null)
        {
            saveToCloudButton.onClick.RemoveAllListeners();
            saveToCloudButton.onClick.AddListener(OnClickSaveToCloud);
        }

        // CloudSaveManager 이벤트 구독 (Start에서도 구독하므로 여기서는 중복 방지)
        // Start()에서 구독하도록 변경
    }

    private void OnDestroy()
    {
        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnLoginStatusChanged -= OnLoginStatusChanged;
            CloudSaveManager.Instance.OnCloudLoadComplete -= OnCloudLoadCompleteHandler;
        }
    }

    private void Start()
    {
        // CloudSaveManager 이벤트 구독
        SubscribeToCloudSaveManager();
    }

    private void OnEnable()
    {
        ConfigureResponsiveLayout();

        // 패널이 활성화될 때마다 최신 로그인 상태 확인 및 UI 갱신
        // CloudSaveManager가 아직 초기화되지 않았을 수 있으므로 이벤트 구독도 확인
        SubscribeToCloudSaveManager();
        RefreshUI();
    }

    /// <summary>
    /// CloudSaveManager 이벤트 구독 (중복 방지)
    /// </summary>
    private void SubscribeToCloudSaveManager()
    {
        if (CloudSaveManager.Instance != null)
        {
            // 중복 구독 방지
            CloudSaveManager.Instance.OnLoginStatusChanged -= OnLoginStatusChanged;
            CloudSaveManager.Instance.OnCloudLoadComplete -= OnCloudLoadCompleteHandler;
            
            CloudSaveManager.Instance.OnLoginStatusChanged += OnLoginStatusChanged;
            CloudSaveManager.Instance.OnCloudLoadComplete += OnCloudLoadCompleteHandler;
            
            // 이미 로그인되어 있으면 클라우드 데이터 미리 로드 (적용은 하지 않음)
            if (CloudSaveManager.Instance.IsAuthenticated)
            {
                PreloadCloudData();
            }
        }
        else
        {
            // CloudSaveManager가 아직 생성되지 않았을 수 있으므로 잠시 후 다시 시도
            StartCoroutine(WaitForCloudSaveManagerAndPreload());
        }
    }

    /// <summary>
    /// CloudSaveManager가 준비될 때까지 대기 후 클라우드 데이터 미리 로드
    /// </summary>
    private IEnumerator WaitForCloudSaveManagerAndPreload()
    {
        float elapsed = 0f;
        float timeout = 5f;
        
        while (CloudSaveManager.Instance == null && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        if (CloudSaveManager.Instance != null)
        {
            SubscribeToCloudSaveManager();
        }
    }

    /// <summary>
    /// 클라우드 데이터 미리 로드 (적용은 하지 않음)
    /// </summary>
    private void PreloadCloudData()
    {
        if (CloudSaveManager.Instance == null || !CloudSaveManager.Instance.IsAuthenticated)
        {
            return;
        }

        if (isLoadingCloud || cloudData != null)
        {
            // 이미 로드 중이거나 이미 로드된 경우 스킵
            return;
        }

        // 조용히 로드 (피드백 메시지 없음)
        isLoadingCloud = true;
        CloudSaveManager.Instance.LoadFromCloud((data) =>
        {
            isLoadingCloud = false;
            if (data != null)
            {
                cloudData = data;
                Debug.Log("[GooglePlayStorePanel] 클라우드 데이터를 미리 로드했습니다. (적용하지 않음)");
            }
        });
    }

    /// <summary>
    /// 클라우드 로드 완료 이벤트 핸들러 (외부에서 호출된 경우)
    /// </summary>
    private void OnCloudLoadCompleteHandler(bool success, SaveData data)
    {
        // 외부에서 로드된 경우 cloudData 업데이트
        if (success && data != null)
        {
            cloudData = data;
        }
    }

    public void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        RefreshUI();
        
        // 타이틀 텍스트 설정
        if (titleText != null)
        {
            titleText.text = LocalizationManager.GetText("구글 클라우드 저장", "Google Cloud Save");
        }
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    /// <summary>
    /// UI 새로고침
    /// </summary>
    private void RefreshUI()
    {
        if (CloudSaveManager.Instance == null)
        {
            UpdateLoginStatus(false);
            return;
        }

        CloudSaveManager.Instance.RefreshAuthenticationState(false);
        bool isAuthenticated = CloudSaveManager.Instance.IsAuthenticated;
        UpdateLoginStatus(isAuthenticated);

        // 로그인되어 있으면 클라우드 데이터 섹션 표시
        if (cloudDataSection != null)
        {
            cloudDataSection.SetActive(isAuthenticated);
        }

        // 현재 로컬 데이터 표시
        RefreshCurrentData();

        // 클라우드 데이터가 로드되어 있으면 표시
        if (cloudData != null)
        {
            RefreshCloudData();
        }
        // 클라우드 데이터가 없고 로그인되어 있으면 로드 시도 (이미 로드 중이 아닐 때만)
        else if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.IsAuthenticated && !isLoadingCloud)
        {
            LoadCloudDataInternal();
        }
    }

    /// <summary>
    /// 로그인 상태 업데이트
    /// </summary>
    private void UpdateLoginStatus(bool isAuthenticated)
    {
        CloudSaveManager cloudManager = CloudSaveManager.Instance;
        bool isLoginInProgress = cloudManager != null && cloudManager.IsLoginInProgress;

        // 로그인 버튼 표시/숨김
        if (loginButton != null)
        {
            loginButton.gameObject.SetActive(!isAuthenticated);
            loginButton.interactable = !isLoginInProgress;
        }
        if (loginStatusText != null)
        {
            // 연결 성공 여부와 실패 원인을 같은 위치에 계속 표시한다.
            loginStatusText.gameObject.SetActive(true);
        }

        // 저장 버튼 표시/숨김 (로그인되어 있을 때만 표시)
        if (saveToCloudButton != null)
        {
            saveToCloudButton.gameObject.SetActive(isAuthenticated);
        }

        // 로그인 상태 텍스트
        if (loginStatusText != null)
        {
            loginStatusText.text = GetLoginStatusText(cloudManager, isAuthenticated);
        }
    }

    private string GetLoginStatusText(CloudSaveManager cloudManager, bool isAuthenticated)
    {
        if (isAuthenticated)
        {
            return LocalizationManager.GetText(
                "Google Play Games 연결됨",
                "Google Play Games connected");
        }

        if (cloudManager == null)
        {
            return LocalizationManager.GetText(
                "클라우드 저장을 사용할 수 없습니다.",
                "Cloud save is unavailable.");
        }

        switch (cloudManager.LastLoginResult)
        {
            case CloudSaveManager.LoginResult.InProgress:
                return LocalizationManager.GetText(
                    "Google Play Games 로그인 중…",
                    "Signing in to Google Play Games…");
            case CloudSaveManager.LoginResult.Canceled:
                return LocalizationManager.GetText(
                    "로그인 취소 또는 Play Games 프로필 미설정\n프로필과 테스터 계정을 확인해주세요.",
                    "Sign-in canceled or Play Games profile not set\nCheck the profile and tester account.");
            case CloudSaveManager.LoginResult.InternalError:
                return LocalizationManager.GetText(
                    "Play Games 연결 오류\n네트워크·앱 서명·게임 서비스 설정을 확인해주세요.",
                    "Play Games connection error\nCheck network, app signing, and game-service settings.");
            case CloudSaveManager.LoginResult.Unavailable:
                return LocalizationManager.GetText(
                    "이 기기에서 Play Games를 사용할 수 없습니다.",
                    "Play Games is unavailable on this device.");
            default:
                return LocalizationManager.GetText(
                    "Google Play Games 로그인이 필요합니다.",
                    "Google Play Games sign-in required.");
        }
    }

    private void ConfigureResponsiveLayout()
    {
        if (loginStatusText != null)
        {
            RectTransform statusRect = loginStatusText.rectTransform;
            statusRect.sizeDelta = new Vector2(760f, 150f);
            ConfigureResponsiveText(loginStatusText, 26f, 50f);
        }

        if (feedbackText != null)
        {
            RectTransform feedbackRect = feedbackText.rectTransform;
            if (panelRoot != null && feedbackRect.parent != panelRoot.transform)
            {
                feedbackRect.SetParent(panelRoot.transform, false);
            }

            feedbackRect.anchorMin = new Vector2(0.5f, 0.5f);
            feedbackRect.anchorMax = new Vector2(0.5f, 0.5f);
            feedbackRect.pivot = new Vector2(0.5f, 0.5f);
            feedbackRect.anchoredPosition = new Vector2(0f, -420f);
            feedbackRect.sizeDelta = new Vector2(820f, 150f);
            ConfigureResponsiveText(feedbackText, 24f, 40f);
        }
    }

    private static void ConfigureResponsiveText(TextMeshProUGUI text, float minSize, float maxSize)
    {
        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.Center;
        text.margin = new Vector4(24f, 10f, 24f, 10f);
    }

    /// <summary>
    /// 현재 로컬 데이터 새로고침
    /// </summary>
    private void RefreshCurrentData()
    {
        if (SaveManager.Instance == null) return;

        // 현재 게임 상태 수집
        SaveData currentData = new SaveData();
        currentData.SetSaveTime();

        var saveables = FindObjectsOfType<MonoBehaviour>(true);
        foreach (var s in saveables)
        {
            if (s is ISaveable saveable)
            {
                try
                {
                    saveable.CollectSaveData(currentData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GooglePlayStorePanel] 데이터 수집 실패: {e.Message}");
                }
            }
        }

        localData = currentData;

        // 현재 데이터 통합 텍스트 표시 (줄바꿈으로)
        if (currentDataText != null)
        {
            double playTimeHours = currentData.totalPlayTimeSeconds / 3600.0;
            string dataText = LocalizationManager.GetText(
                "로컬 데이터\n" +
                $"플레이타임: {playTimeHours:F2}시간\n" +
                $"최대 단계: {currentData.maxLevelReached}\n" +
                $"저장 시간: {currentData.savedAt}",
                "Local Data\n" +
                $"Play Time: {playTimeHours:F2}h\n" +
                $"Max Level: {currentData.maxLevelReached}\n" +
                $"Saved At: {currentData.savedAt}"
            );
            currentDataText.text = dataText;
        }
    }

    /// <summary>
    /// 클라우드 데이터 새로고침
    /// </summary>
    private void RefreshCloudData()
    {
        if (cloudData == null) return;

        // 클라우드 데이터 통합 텍스트 표시 (줄바꿈으로)
        if (cloudDataText != null)
        {
            double playTimeHours = cloudData.totalPlayTimeSeconds / 3600.0;
            string dataText = LocalizationManager.GetText(
                "클라우드 데이터\n" +
                $"플레이타임: {playTimeHours:F2}시간\n" +
                $"최대 단계: {cloudData.maxLevelReached}\n" +
                $"저장 시간: {cloudData.savedAt}",
                "Cloud Data\n" +
                $"Play Time: {playTimeHours:F2}h\n" +
                $"Max Level: {cloudData.maxLevelReached}\n" +
                $"Saved At: {cloudData.savedAt}"
            );
            cloudDataText.text = dataText;
        }
    }

    /// <summary>
    /// 로그인 버튼 클릭
    /// </summary>
    private void OnClickLogin()
    {
        CloudSaveManager cloudManager = CloudSaveManager.Instance;
        if (cloudManager == null)
        {
            ShowFeedback(LocalizationManager.GetText("클라우드 저장을 사용할 수 없습니다.", "Cloud save is not available."));
            RefreshUI();
            return;
        }

        if (cloudManager.IsAuthenticated)
        {
            ShowFeedback(LocalizationManager.GetText("이미 로그인되어 있습니다.", "Already logged in."));
            return;
        }

        if (cloudManager.IsLoginInProgress)
        {
            ShowFeedback(LocalizationManager.GetText("로그인 진행 중…", "Sign-in in progress…"));
            RefreshUI();
            return;
        }

        ShowFeedback(LocalizationManager.GetText("로그인 시도 중…", "Attempting to sign in…"));

        cloudManager.SignIn((success) =>
        {
            if (success)
            {
                ShowFeedback(LocalizationManager.GetText("로그인 성공!", "Login successful!"));
                RefreshUI();
                // 로그인 성공 시 savedGameClient 초기화 대기 후 클라우드 데이터 자동 불러오기
                StartCoroutine(WaitForSavedGameClientAndLoad());
            }
            else
            {
                // 상세 원인은 위의 고정 상태 영역에 표시하고, 일시 알림은 짧게 유지한다.
                RefreshUI();
                ShowFeedback(LocalizationManager.GetText(
                    "로그인 실패 — 위 안내를 확인해주세요.",
                    "Sign-in failed — see the status above."));
            }
        });
    }

    /// <summary>
    /// 클라우드 데이터 불러오기 버튼 클릭
    /// </summary>
    private void OnClickLoadCloudData()
    {
        if (CloudSaveManager.Instance == null || !CloudSaveManager.Instance.IsAuthenticated)
        {
            ShowFeedback(LocalizationManager.GetText("로그인이 필요합니다.", "Login required."));
            return;
        }

        if (isLoadingCloud)
        {
            ShowFeedback(LocalizationManager.GetText("이미 로드 중입니다...", "Already loading..."));
            return;
        }

        // 클라우드 데이터가 이미 로드되어 있으면 확인 패널 표시
        if (cloudData != null)
        {
            ShowConfirmPanel();
            return;
        }

        // 클라우드 데이터가 없으면 먼저 불러오기
        LoadCloudDataInternal();
        RefreshCloudData();
    }

    /// <summary>
    /// 확인 패널 표시
    /// </summary>
    private void ShowConfirmPanel()
    {
        if (confirmPanel == null)
        {
            // confirmPanel이 없으면 직접 찾기
            confirmPanel = FindObjectOfType<CloudDataConfirmPanel>(true);
        }

        if (confirmPanel == null)
        {
            Debug.LogWarning("[GooglePlayStorePanel] CloudDataConfirmPanel을 찾을 수 없습니다. 직접 적용합니다.");
            ApplyCloudDataToGame();
            return;
        }

        confirmPanel.Show(() =>
        {
            // 확인 버튼 클릭 시 클라우드 데이터 적용
            ApplyCloudDataToGame();
        });
    }

    /// <summary>
    /// savedGameClient 초기화 대기 후 클라우드 데이터 자동 불러오기
    /// </summary>
    private IEnumerator WaitForSavedGameClientAndLoad()
    {
        if (CloudSaveManager.Instance == null)
        {
            yield break;
        }

        // 로그인 상태 확인 및 savedGameClient 초기화 대기 (최대 3초)
        float elapsed = 0f;
        float timeout = 3f;
        bool canLoad = false;
        
        while (elapsed < timeout && !canLoad)
        {
            if (CloudSaveManager.Instance.IsAuthenticated)
            {
                // 로그인되어 있고, 로드 중이 아니고, 클라우드 데이터가 없으면 로드 시도
                if (!isLoadingCloud && cloudData == null)
                {
                    // CloudSaveManager의 LoadFromCloud가 savedGameClient null 체크를 하므로 바로 시도
                    LoadCloudDataInternal();
                    yield break;
                }
            }
            
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        // 타임아웃 후에도 로그인되어 있고 데이터가 없으면 시도
        if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.IsAuthenticated && !isLoadingCloud && cloudData == null)
        {
            Debug.LogWarning("[GooglePlayStorePanel] WaitForSavedGameClientAndLoad 타임아웃, 클라우드 데이터 로드 시도");
            LoadCloudDataInternal();
        }
    }

    /// <summary>
    /// 클라우드 데이터 자동 불러오기 (로그인 성공 시)
    /// </summary>
    private void LoadCloudDataAutomatically()
    {
        if (CloudSaveManager.Instance == null || !CloudSaveManager.Instance.IsAuthenticated)
        {
            return;
        }

        if (isLoadingCloud || cloudData != null)
        {
            return;
        }

        // 비동기로 로드하므로 RefreshCloudData()는 콜백에서 호출됨
        LoadCloudDataInternal();
    }

    /// <summary>
    /// 클라우드 데이터 불러오기 내부 로직
    /// </summary>
    private void LoadCloudDataInternal()
    {
        if (CloudSaveManager.Instance == null || !CloudSaveManager.Instance.IsAuthenticated)
        {
            Debug.LogWarning("[GooglePlayStorePanel] LoadCloudDataInternal: 로그인되지 않았습니다.");
            return;
        }

        if (isLoadingCloud)
        {
            Debug.LogWarning("[GooglePlayStorePanel] LoadCloudDataInternal: 이미 로드 중입니다.");
            return;
        }

        isLoadingCloud = true;
        
        // 패널이 활성화되어 있을 때만 피드백 메시지 표시
        if (panelRoot != null && panelRoot.activeSelf)
        {
            ShowFeedback(LocalizationManager.GetText("클라우드 데이터 불러오는 중...", "Loading cloud data..."));
        }

        CloudSaveManager.Instance.LoadFromCloud((data) =>
        {
            isLoadingCloud = false;

            if (data != null)
            {
                cloudData = data;
                RefreshCloudData();
                RefreshCurrentData(); // 현재 데이터도 다시 수집
                
                // 패널이 활성화되어 있을 때만 피드백 메시지 표시
                if (panelRoot != null && panelRoot.activeSelf)
                {
                    ShowFeedback(LocalizationManager.GetText("클라우드 데이터를 불러왔습니다.", "Cloud data loaded."));
                }
                else
                {
                    Debug.Log("[GooglePlayStorePanel] 클라우드 데이터를 미리 로드했습니다. (패널 비활성화 상태)");
                }
            }
            else
            {
                cloudData = null;
                
                // 패널이 활성화되어 있을 때만 피드백 메시지 표시
                if (panelRoot != null && panelRoot.activeSelf)
                {
                    ShowFeedback(LocalizationManager.GetText("클라우드에 저장된 데이터가 없습니다.", "No cloud data found."));
                }
            }
        });
    }

    /// <summary>
    /// 클라우드에 저장하기 버튼 클릭
    /// </summary>
    private void OnClickSaveToCloud()
    {
        var cloudManager = CloudSaveManager.Instance;
        if (cloudManager == null || !cloudManager.IsAuthenticated)
        {
            ShowFeedback(LocalizationManager.GetText("로그인이 필요합니다.", "Login required."));
            return;
        }

        if (cloudManager.IsSaving)
        {
            ShowFeedback(LocalizationManager.GetText("이미 클라우드에 저장 중입니다.", "A cloud save is already in progress."));
            return;
        }

        if (SaveManager.Instance == null)
        {
            ShowFeedback(LocalizationManager.GetText("저장 실패", "Save failed"));
            return;
        }

        // 현재 게임 상태 수집
        RefreshCurrentData();

        if (localData == null)
        {
            ShowFeedback(LocalizationManager.GetText("데이터 수집 실패", "Failed to collect data"));
            return;
        }

        ShowFeedback(LocalizationManager.GetText("클라우드에 저장 중...", "Saving to cloud..."));

        // 로그인 해제 같은 동기 실패 경로도 놓치지 않도록 요청 전에 구독한다.
        System.Action<bool, string> onSaveComplete = null;
        onSaveComplete = (success, error) =>
        {
            cloudManager.OnCloudSaveComplete -= onSaveComplete;

            if (success)
            {
                ShowFeedback(LocalizationManager.GetText("클라우드에 저장되었습니다.", "Saved to cloud."));
                // 저장 후 클라우드 데이터 다시 로드
                LoadCloudDataInternal();
            }
            else
            {
                ShowFeedback(LocalizationManager.GetText("클라우드 저장 실패", "Cloud save failed"));
            }
        };

        cloudManager.OnCloudSaveComplete += onSaveComplete;
        cloudManager.SaveToCloud(localData);
    }


    /// <summary>
    /// 클라우드 데이터 불러오기 버튼 클릭 시 클라우드 데이터를 게임에 적용하는 기능 추가
    /// </summary>
    private void ApplyCloudDataToGame()
    {
        if (cloudData == null)
        {
            ShowFeedback(LocalizationManager.GetText("클라우드 데이터가 없습니다.", "No cloud data."));
            return;
        }

        if (SaveManager.Instance == null)
        {
            ShowFeedback(LocalizationManager.GetText("저장 실패", "Save failed"));
            return;
        }

        ShowFeedback(LocalizationManager.GetText("클라우드 데이터를 적용 중...", "Applying cloud data..."));

        // 모든 ISaveable에 클라우드 데이터 적용
        var saveables = FindObjectsOfType<MonoBehaviour>(true);
        foreach (var s in saveables)
        {
            if (s is ISaveable saveable)
            {
                try
                {
                    saveable.ApplyLoadedData(cloudData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GooglePlayStorePanel] 데이터 적용 실패: {e.Message}");
                }
            }
        }

        // 로컬에도 저장
        SaveManager.Instance.SaveGame();

        ShowFeedback(LocalizationManager.GetText("클라우드 데이터를 적용했습니다.", "Cloud data applied."));
        RefreshCurrentData();
    }

    /// <summary>
    /// 로그인 상태 변경 이벤트 핸들러
    /// </summary>
    private void OnLoginStatusChanged(bool isAuthenticated)
    {
        RefreshUI();
        
        // 로그인 성공 시 클라우드 데이터 미리 로드 (패널이 활성화되어 있을 때만)
        if (isAuthenticated && cloudData == null && !isLoadingCloud)
        {
            // 패널이 활성화되어 있으면 자동 로드, 비활성화되어 있으면 조용히 미리 로드
            if (panelRoot != null && panelRoot.activeSelf)
            {
                // 패널이 열려있으면 피드백 메시지와 함께 로드
                StartCoroutine(WaitForSavedGameClientAndLoad());
            }
            else
            {
                // 패널이 닫혀있으면 조용히 미리 로드
                PreloadCloudData();
            }
        }
    }

    /// <summary>
    /// 피드백 메시지 표시
    /// </summary>
    private void ShowFeedback(string message)
    {
        if (feedbackText == null) return;

        ConfigureResponsiveLayout();

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        feedbackText.text = message;
        feedbackText.gameObject.SetActive(true);
        feedbackRoutine = StartCoroutine(HideFeedbackAfter(feedbackDuration));
    }

    private IEnumerator HideFeedbackAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
        }
        feedbackRoutine = null;
    }
}

