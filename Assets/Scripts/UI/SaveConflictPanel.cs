// ============================================================================
// SaveConflictPanel.cs
// - 로컬 저장과 클라우드 저장 충돌 시 선택 패널
// - 플레이 시간을 비교하여 사용자가 선택할 수 있게 함
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveConflictPanel : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI localPlayTimeText;
    [SerializeField] private TextMeshProUGUI cloudPlayTimeText;
    
    [Header("Buttons")]
    [SerializeField] private Button useLocalButton;
    [SerializeField] private Button useCloudButton;
    
    private System.Action<bool> onChoiceMade; // true = 클라우드 사용, false = 로컬 사용

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        
        if (useLocalButton != null)
        {
            useLocalButton.onClick.RemoveAllListeners();
            useLocalButton.onClick.AddListener(() => OnChoiceMade(false));
        }
        
        if (useCloudButton != null)
        {
            useCloudButton.onClick.RemoveAllListeners();
            useCloudButton.onClick.AddListener(() => OnChoiceMade(true));
        }
        
        // 배경 클릭 방지 (선택 강제)
        if (panelRoot != null)
        {
            var bgButton = panelRoot.GetComponent<Button>();
            if (bgButton != null)
            {
                bgButton.enabled = false; // 배경 클릭으로 닫기 비활성화
            }
        }
    }

    /// <summary>
    /// 충돌 패널 표시
    /// </summary>
    public void Show(double localPlayTimeHours, double cloudPlayTimeHours, System.Action<bool> onChoice)
    {
        onChoiceMade = onChoice;
        
        if (panelRoot != null) panelRoot.SetActive(true);
        
        // 텍스트 업데이트
        if (titleText != null)
        {
            titleText.text = LocalizationManager.GetText("저장 데이터 충돌", "Save Data Conflict");
        }
        
        if (messageText != null)
        {
            messageText.text = LocalizationManager.GetText(
                "로컬 저장과 클라우드 저장이 다릅니다.\n어느 것을 사용하시겠습니까?",
                "Local save and cloud save are different.\nWhich one would you like to use?"
            );
        }
        
        // 플레이 시간 표시
        if (localPlayTimeText != null)
        {
            string localTimeStr = FormatPlayTime(localPlayTimeHours);
            localPlayTimeText.text = LocalizationManager.GetText(
                $"로컬 저장: {localTimeStr}",
                $"Local Save: {localTimeStr}"
            );
        }
        
        if (cloudPlayTimeText != null)
        {
            string cloudTimeStr = FormatPlayTime(cloudPlayTimeHours);
            cloudPlayTimeText.text = LocalizationManager.GetText(
                $"클라우드 저장: {cloudTimeStr}",
                $"Cloud Save: {cloudTimeStr}"
            );
        }
        
        // 버튼 텍스트 업데이트
        if (useLocalButton != null)
        {
            var btnText = useLocalButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = LocalizationManager.GetText("로컬 저장 사용", "Use Local Save");
            }
        }
        
        if (useCloudButton != null)
        {
            var btnText = useCloudButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = LocalizationManager.GetText("클라우드 저장 사용", "Use Cloud Save");
            }
        }
    }

    /// <summary>
    /// 선택 완료 처리
    /// </summary>
    private void OnChoiceMade(bool useCloud)
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        
        if (onChoiceMade != null)
        {
            onChoiceMade(useCloud);
            onChoiceMade = null;
        }
    }

    /// <summary>
    /// 플레이 시간 포맷팅
    /// </summary>
    private string FormatPlayTime(double hours)
    {
        if (hours < 1.0)
        {
            int minutes = (int)(hours * 60);
            return $"{minutes}분";
        }
        else if (hours < 24.0)
        {
            int h = (int)hours;
            int m = (int)((hours - h) * 60);
            return $"{h}시간 {m}분";
        }
        else
        {
            int days = (int)(hours / 24.0);
            int h = (int)(hours % 24.0);
            return $"{days}일 {h}시간";
        }
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }
}
