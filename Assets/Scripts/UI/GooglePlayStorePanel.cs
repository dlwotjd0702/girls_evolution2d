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

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private float feedbackDuration = 2f;
    private Coroutine feedbackRoutine;

    private SaveData cloudData = null;
    private SaveData localData = null;
    private bool isLoadingCloud = false;

    private void Awake()
    {
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

        // CloudSaveManager 이벤트 구독
        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnLoginStatusChanged += OnLoginStatusChanged;
        }
    }

    private void OnDestroy()
    {
        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnLoginStatusChanged -= OnLoginStatusChanged;
        }
    }

    private void OnEnable()
    {
        RefreshUI();
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
            UpdateLoginStatus(false, "클라우드 저장을 사용할 수 없습니다.");
            return;
        }

        bool isAuthenticated = CloudSaveManager.Instance.IsAuthenticated;
        UpdateLoginStatus(isAuthenticated, isAuthenticated ? "로그인됨" : "로그인 필요");

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
    }

    /// <summary>
    /// 로그인 상태 업데이트
    /// </summary>
    private void UpdateLoginStatus(bool isAuthenticated, string statusText)
    {
        // 로그인 버튼 표시/숨김
        if (loginButton != null)
        {
            loginButton.gameObject.SetActive(!isAuthenticated);
        }

        // 저장 버튼 표시/숨김 (로그인되어 있을 때만 표시)
        if (saveToCloudButton != null)
        {
            saveToCloudButton.gameObject.SetActive(isAuthenticated);
        }

        // 로그인 상태 텍스트
        if (loginStatusText != null)
        {
            loginStatusText.text = LocalizationManager.GetText(statusText, statusText);
        }
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
                $"플레이타임: {playTimeHours:F2}시간\n" +
                $"최대 단계: {currentData.maxLevelReached}\n" +
                $"저장 시간: {currentData.savedAt}",
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
                $"플레이타임: {playTimeHours:F2}시간\n" +
                $"최대 단계: {cloudData.maxLevelReached}\n" +
                $"저장 시간: {cloudData.savedAt}",
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
        if (CloudSaveManager.Instance == null)
        {
            ShowFeedback(LocalizationManager.GetText("클라우드 저장을 사용할 수 없습니다.", "Cloud save is not available."));
            return;
        }

        if (CloudSaveManager.Instance.IsAuthenticated)
        {
            ShowFeedback(LocalizationManager.GetText("이미 로그인되어 있습니다.", "Already logged in."));
            return;
        }

        ShowFeedback(LocalizationManager.GetText("로그인 시도 중...", "Attempting to sign in..."));

        CloudSaveManager.Instance.SignIn((success) =>
        {
            if (success)
            {
                ShowFeedback(LocalizationManager.GetText("로그인 성공!", "Login successful!"));
                RefreshUI();
            }
            else
            {
                ShowFeedback(LocalizationManager.GetText(
                    "로그인 실패. Google Play Games가 설치되어 있고 Google 계정이 로그인되어 있는지 확인해주세요.",
                    "Login failed. Please make sure Google Play Games is installed and you are signed in with a Google account."
                ));
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

        isLoadingCloud = true;
        ShowFeedback(LocalizationManager.GetText("클라우드 데이터 불러오는 중...", "Loading cloud data..."));

        CloudSaveManager.Instance.LoadFromCloud((data) =>
        {
            isLoadingCloud = false;

            if (data != null)
            {
                cloudData = data;
                RefreshCloudData();
                RefreshCurrentData(); // 현재 데이터도 다시 수집
                ShowFeedback(LocalizationManager.GetText("클라우드 데이터를 불러왔습니다.", "Cloud data loaded."));
                
                // 클라우드 데이터를 게임에 적용할지 물어볼 수도 있지만, 일단은 불러오기만 함
                // 필요시 loadCloudDataButton을 "불러오기 및 적용" 버튼으로 변경 가능
            }
            else
            {
                cloudData = null;
                ShowFeedback(LocalizationManager.GetText("클라우드에 저장된 데이터가 없습니다.", "No cloud data found."));
            }
        });
    }

    /// <summary>
    /// 클라우드에 저장하기 버튼 클릭
    /// </summary>
    private void OnClickSaveToCloud()
    {
        if (CloudSaveManager.Instance == null || !CloudSaveManager.Instance.IsAuthenticated)
        {
            ShowFeedback(LocalizationManager.GetText("로그인이 필요합니다.", "Login required."));
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

        CloudSaveManager.Instance.SaveToCloud(localData);

        // 저장 완료 이벤트 구독
        System.Action<bool, string> onSaveComplete = null;
        onSaveComplete = (success, error) =>
        {
            CloudSaveManager.Instance.OnCloudSaveComplete -= onSaveComplete;

            if (success)
            {
                ShowFeedback(LocalizationManager.GetText("클라우드에 저장되었습니다.", "Saved to cloud."));
                // 저장 후 클라우드 데이터 다시 로드
                StartCoroutine(ReloadCloudDataAfterSave());
            }
            else
            {
                ShowFeedback(LocalizationManager.GetText("클라우드 저장 실패", "Cloud save failed"));
            }
        };

        CloudSaveManager.Instance.OnCloudSaveComplete += onSaveComplete;
    }

    /// <summary>
    /// 저장 후 클라우드 데이터 다시 로드
    /// </summary>
    private IEnumerator ReloadCloudDataAfterSave()
    {
        yield return new WaitForSeconds(0.5f);
        OnClickLoadCloudData();
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
    }

    /// <summary>
    /// 피드백 메시지 표시
    /// </summary>
    private void ShowFeedback(string message)
    {
        if (feedbackText == null) return;

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

