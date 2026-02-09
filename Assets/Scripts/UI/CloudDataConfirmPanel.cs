// ============================================================================
// CloudDataConfirmPanel.cs
// - 클라우드 데이터 적용 확인 패널
// - 클라우드 데이터로 덮어씌울 것인지 확인
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CloudDataConfirmPanel : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    
    private System.Action onConfirm;
    
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
    
    public void Show(System.Action onConfirmCallback)
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        onConfirm = onConfirmCallback;
        UpdateTexts();
    }
    
    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        onConfirm = null;
    }
    
    void OnConfirm()
    {
        onConfirm?.Invoke();
        Hide();
    }
    
    void UpdateTexts()
    {
        if (titleText != null)
        {
            titleText.text = LocalizationManager.GetText("클라우드 데이터 적용", "Apply Cloud Data");
        }
        
        if (messageText != null)
        {
            messageText.text = LocalizationManager.GetText(
                "클라우드 데이터로 덮어 씌우시겠습니까?\n현재 로컬 데이터는 덮어씌워집니다.",
                "Do you want to overwrite local data with cloud data?\nCurrent local data will be overwritten."
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
