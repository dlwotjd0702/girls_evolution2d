// ============================================================================
// TutorialManager.cs
// - 게임 튜토리얼 시스템
// - 첫 실행 시 단계별 가이드 제공
// - 진행 상태 저장 및 스킵 기능
// ============================================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialManager : MonoBehaviour, ISaveable
{
    public static TutorialManager Instance { get; private set; }

    [Header("Tutorial Panel")]
    [SerializeField] private GameObject tutorialPanelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button closeButton;

    [Header("Tutorial Steps")]
    [SerializeField] private TutorialStep[] tutorialSteps;

    [Header("Highlight Settings")]
    [SerializeField] private GameObject highlightOverlay; // 하이라이트 오버레이 (선택사항)
    [SerializeField] private RectTransform highlightTarget; // 하이라이트할 UI 요소 (동적 설정)

    private int currentStepIndex = 0;
    private bool isTutorialActive = false;

    [Serializable]
    public class TutorialStep
    {
        public string stepName;
        [TextArea(3, 5)]
        public string title;
        [TextArea(5, 10)]
        public string message;
        public string targetObjectName; // 하이라이트할 GameObject 이름 (선택사항)
        public bool waitForAction; // 사용자 액션 대기 여부
        public string actionToWait; // 대기할 액션 이름 (예: "Summon", "Merge", "ShopOpen")
    }

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
        // 세이브 데이터 로드 후 튜토리얼 체크
        StartCoroutine(CheckTutorialAfterLoad());
    }

    private IEnumerator CheckTutorialAfterLoad()
    {
        // SaveManager가 데이터를 로드할 때까지 대기
        yield return new WaitForSeconds(1f);

        if (SaveManager.Instance != null)
        {
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
                        Debug.LogError($"[TutorialManager] 데이터 수집 실패: {e.Message}");
                    }
                }
            }

            // 튜토리얼이 완료되지 않았으면 시작
            if (!saveData.tutorialCompleted)
            {
                StartTutorial();
            }
        }
    }

    /// <summary>
    /// 튜토리얼 시작
    /// </summary>
    public void StartTutorial()
    {
        if (tutorialPanelRoot == null)
        {
            Debug.LogWarning("[TutorialManager] 튜토리얼 패널이 설정되지 않았습니다.");
            return;
        }

        currentStepIndex = 0;
        isTutorialActive = true;
        ShowCurrentStep();
    }

    /// <summary>
    /// 현재 단계 표시
    /// </summary>
    private void ShowCurrentStep()
    {
        if (currentStepIndex < 0 || currentStepIndex >= tutorialSteps.Length)
        {
            CompleteTutorial();
            return;
        }

        TutorialStep step = tutorialSteps[currentStepIndex];
        
        if (titleText != null)
        {
            titleText.text = LocalizationManager.GetText(step.title, step.title);
        }

        if (messageText != null)
        {
            messageText.text = LocalizationManager.GetText(step.message, step.message);
        }

        // 하이라이트 설정
        if (!string.IsNullOrEmpty(step.targetObjectName))
        {
            HighlightTarget(step.targetObjectName);
        }
        else
        {
            ClearHighlight();
        }

        // 버튼 표시
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(!step.waitForAction);
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(step.waitForAction);
        }

        // 패널 표시
        if (tutorialPanelRoot != null)
        {
            tutorialPanelRoot.SetActive(true);
        }

        // 액션 대기
        if (step.waitForAction)
        {
            StartCoroutine(WaitForAction(step.actionToWait));
        }
    }

    /// <summary>
    /// 다음 단계로 이동
    /// </summary>
    public void NextStep()
    {
        if (!isTutorialActive) return;

        currentStepIndex++;
        ShowCurrentStep();
    }

    /// <summary>
    /// 특정 액션 대기
    /// </summary>
    private IEnumerator WaitForAction(string actionName)
    {
        bool actionCompleted = false;

        switch (actionName)
        {
            case "Summon":
                // 소환 버튼 클릭 대기
                yield return StartCoroutine(WaitForSummon());
                actionCompleted = true;
                break;

            case "Merge":
                // 합성 대기 (드래그 앤 드롭)
                yield return StartCoroutine(WaitForMerge());
                actionCompleted = true;
                break;

            case "ShopOpen":
                // 상점 열기 대기
                yield return StartCoroutine(WaitForShopOpen());
                actionCompleted = true;
                break;

            case "TierSwitch":
                // 티어 이동 대기
                yield return StartCoroutine(WaitForTierSwitch());
                actionCompleted = true;
                break;

            case "AutoSpawn":
                // 자동소환 활성화 대기
                yield return StartCoroutine(WaitForAutoSpawn());
                actionCompleted = true;
                break;

            case "AutoMerge":
                // 자동합성 활성화 대기
                yield return StartCoroutine(WaitForAutoMerge());
                actionCompleted = true;
                break;

            default:
                // 기본: 2초 대기
                yield return new WaitForSeconds(2f);
                actionCompleted = true;
                break;
        }

        if (actionCompleted)
        {
            NextStep();
        }
    }

    private IEnumerator WaitForSummon()
    {
        GirlFieldManager fieldManager = FindObjectOfType<GirlFieldManager>(true);
        if (fieldManager == null) yield break;

        int initialCount = fieldManager.girlList.Count;
        
        while (fieldManager.girlList.Count == initialCount)
        {
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator WaitForMerge()
    {
        GirlMergeManager mergeManager = FindObjectOfType<GirlMergeManager>(true);
        if (mergeManager == null) yield break;

        // 합성 이벤트 구독 (간단한 방법: 필드의 캐릭터 수가 줄어드는지 확인)
        GirlFieldManager fieldManager = FindObjectOfType<GirlFieldManager>(true);
        if (fieldManager == null) yield break;

        int initialCount = fieldManager.girlList.Count;
        float waitTime = 0f;
        
        // 합성 완료 대기 (최대 30초)
        while (waitTime < 30f)
        {
            if (fieldManager.girlList.Count < initialCount)
            {
                yield return new WaitForSeconds(0.5f); // 합성 애니메이션 대기
                break;
            }
            yield return new WaitForSeconds(0.1f);
            waitTime += 0.1f;
        }
    }

    private IEnumerator WaitForShopOpen()
    {
        ShopPanelController shopPanel = FindObjectOfType<ShopPanelController>(true);
        if (shopPanel == null) yield break;

        // 상점 패널이 활성화될 때까지 대기
        while (!shopPanel.gameObject.activeInHierarchy)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // 패널이 열린 후 잠시 대기
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator WaitForTierSwitch()
    {
        TierManager tierManager = FindObjectOfType<TierManager>(true);
        if (tierManager == null) yield break;

        int initialTier = tierManager.CurrentTierIndex;
        
        while (tierManager.CurrentTierIndex == initialTier)
        {
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator WaitForAutoSpawn()
    {
        if (SaveManager.Instance == null) yield break;

        SaveData saveData = new SaveData();
        var saveables = FindObjectsOfType<MonoBehaviour>(true);
        foreach (var s in saveables)
        {
            if (s is ISaveable saveable)
            {
                try { saveable.CollectSaveData(saveData); }
                catch { }
            }
        }

        while (!saveData.autoSpawnOn)
        {
            yield return new WaitForSeconds(0.5f);
            
            saveData = new SaveData();
            foreach (var s in saveables)
            {
                if (s is ISaveable saveable)
                {
                    try { saveable.CollectSaveData(saveData); }
                    catch { }
                }
            }
        }
    }

    private IEnumerator WaitForAutoMerge()
    {
        if (SaveManager.Instance == null) yield break;

        SaveData saveData = new SaveData();
        var saveables = FindObjectsOfType<MonoBehaviour>(true);
        foreach (var s in saveables)
        {
            if (s is ISaveable saveable)
            {
                try { saveable.CollectSaveData(saveData); }
                catch { }
            }
        }

        while (!saveData.autoMergeOn)
        {
            yield return new WaitForSeconds(0.5f);
            
            saveData = new SaveData();
            foreach (var s in saveables)
            {
                if (s is ISaveable saveable)
                {
                    try { saveable.CollectSaveData(saveData); }
                    catch { }
                }
            }
        }
    }

    /// <summary>
    /// 타겟 하이라이트
    /// </summary>
    private void HighlightTarget(string targetName)
    {
        if (highlightOverlay == null) return;

        GameObject target = GameObject.Find(targetName);
        if (target != null)
        {
            highlightTarget = target.GetComponent<RectTransform>();
            if (highlightTarget != null)
            {
                highlightOverlay.SetActive(true);
                // 하이라이트 위치 조정 (구현 필요)
            }
        }
    }

    /// <summary>
    /// 하이라이트 제거
    /// </summary>
    private void ClearHighlight()
    {
        if (highlightOverlay != null)
        {
            highlightOverlay.SetActive(false);
        }
        highlightTarget = null;
    }

    /// <summary>
    /// 튜토리얼 스킵
    /// </summary>
    public void SkipTutorial()
    {
        CompleteTutorial();
    }

    /// <summary>
    /// 튜토리얼 완료
    /// </summary>
    private void CompleteTutorial()
    {
        isTutorialActive = false;
        
        if (tutorialPanelRoot != null)
        {
            tutorialPanelRoot.SetActive(false);
        }

        ClearHighlight();

        // 저장
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }

        Debug.Log("[TutorialManager] 튜토리얼 완료");
    }

    private void OnEnable()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(NextStep);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(SkipTutorial);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(NextStep);
        }
    }

    // ── ISaveable 구현 ──

    public void CollectSaveData(SaveData data)
    {
        data.tutorialCompleted = !isTutorialActive && currentStepIndex >= tutorialSteps.Length;
    }

    public void ApplyLoadedData(SaveData data)
    {
        if (data.tutorialCompleted)
        {
            isTutorialActive = false;
            if (tutorialPanelRoot != null)
            {
                tutorialPanelRoot.SetActive(false);
            }
        }
    }
}
