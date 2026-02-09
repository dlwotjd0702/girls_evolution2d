using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 언어 설정 패널
/// 한국어/영어 선택 버튼 제공
/// </summary>
public class LanguageSettingsPanel : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI titleText;
    
    [Header("Language Buttons")]
    [SerializeField] private Button koreanButton;
    [SerializeField] private Button englishButton;
    
    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);
        }
        
        // 한국어 버튼
        if (koreanButton != null)
        {
            koreanButton.onClick.RemoveAllListeners();
            koreanButton.onClick.AddListener(() => SetLanguage(true));
        }
        
        // 영어 버튼
        if (englishButton != null)
        {
            englishButton.onClick.RemoveAllListeners();
            englishButton.onClick.AddListener(() => SetLanguage(false));
        }
        
        // 초기 텍스트 설정
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
    
    void SetLanguage(bool isKorean)
    {
        LocalizationManager.SetLanguage(isKorean);
        
        // 언어 변경 후 모든 텍스트 갱신을 위해 씬의 UI 요소들을 새로고침
        RefreshAllUI();
    }
    
    void UpdateTexts()
    {
        if (titleText != null)
        {
            titleText.text = LocalizationManager.GetText("언어 설정", "Language Settings");
        }
        
        // 버튼 텍스트도 업데이트 (TextMeshProUGUI 컴포넌트가 있다면)
        var koreanButtonText = koreanButton?.GetComponentInChildren<TextMeshProUGUI>();
        if (koreanButtonText != null)
        {
            koreanButtonText.text = LocalizationManager.GetText("한국어", "Korean");
        }
        
        var englishButtonText = englishButton?.GetComponentInChildren<TextMeshProUGUI>();
        if (englishButtonText != null)
        {
            englishButtonText.text = LocalizationManager.GetText("영어", "English");
        }
    }
    
    /// <summary>
    /// 언어 변경 후 모든 UI 요소를 새로고침합니다.
    /// </summary>
    void RefreshAllUI()
    {
        // 씬의 모든 UI 컨트롤러를 찾아서 Refresh 메서드 호출
        var controllers = FindObjectsOfType<MonoBehaviour>();
        foreach (var controller in controllers)
        {
            // Refresh, RefreshAll, UpdateTexts 등의 메서드가 있으면 호출
            var refreshMethod = controller.GetType().GetMethod("Refresh", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (refreshMethod != null && refreshMethod.GetParameters().Length == 0)
            {
                try
                {
                    refreshMethod.Invoke(controller, null);
                }
                catch { }
            }
            
            var refreshAllMethod = controller.GetType().GetMethod("RefreshAll", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (refreshAllMethod != null && refreshAllMethod.GetParameters().Length == 0)
            {
                try
                {
                    refreshAllMethod.Invoke(controller, null);
                }
                catch { }
            }
        }
    }
}

