// ============================================================================
// NewGameConfirmPanel.cs
// - 새로 시작하기 확인 패널
// - 모든 게임 데이터가 삭제된다는 경고 표시
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

public class NewGameConfirmPanel : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    
    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirm);
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(Hide);
        }
        
        UpdateTexts();
    }
    
    void OnEnable()
    {
        UpdateTexts();
    }
    
    public void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        UpdateTexts();
    }
    
    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }
    
    void OnConfirm()
    {
        StartCoroutine(ResetAndReloadScene());
    }
    
    private IEnumerator ResetAndReloadScene()
    {
        Hide();
        
        // 1. DOTween 애니메이션 모두 정리 (에러 방지)
        DOTween.KillAll();
        
        // 2. 짧은 대기 (UI 업데이트 완료)
        yield return new WaitForSeconds(0.1f);
        
        // 3. 로컬 및 클라우드 저장 파일 삭제 (ResetSaveFile이 모두 처리)
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ResetSaveFile();
        }
        
        // 4. 클라우드 저장 완료 대기 (비동기 저장이므로)
        yield return new WaitForSeconds(0.3f);
        
        // 6. 씬 리로드
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
    
    void UpdateTexts()
    {
        if (titleText != null)
        {
            titleText.text = LocalizationManager.GetText("새로 시작하기", "New Game");
        }
        
        if (messageText != null)
        {
            messageText.text = LocalizationManager.GetText(
                "모든 게임 데이터가 삭제됩니다.\n정말 새로 시작하시겠습니까?",
                "All game data will be deleted.\nAre you sure you want to start a new game?"
            );
        }
        
        if (confirmButton != null)
        {
            var btnText = confirmButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = LocalizationManager.GetText("확인", "Confirm");
            }
        }
        
        if (cancelButton != null)
        {
            var btnText = cancelButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = LocalizationManager.GetText("취소", "Cancel");
            }
        }
    }
}
