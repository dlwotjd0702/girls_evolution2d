using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 설정 패널 기본 클래스
/// 추후 볼륨 조절, 수동 저장, 우편 시스템(재화 지급), 공지사항 등을 구현할 예정
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;
    
    [Header("Settings Buttons")]
    [SerializeField] private Button languageButton;      // 언어 설정 버튼
    [SerializeField] private Button volumeButton;        // 볼륨 조절 버튼
    [SerializeField] private Button manualSaveButton;    // 수동 저장 버튼
    [SerializeField] private Button sendEmailButton;     // 메일 보내기 버튼
    [SerializeField] private Button discordButton;       // 디스코드 링크 버튼
    
    [Header("Sub Panels")]
    [SerializeField] private LanguageSettingsPanel languagePanel;
    [SerializeField] private VolumeSettingsPanel volumePanel;
    
    [Header("Email Settings")]
    [Tooltip("이메일 보내기 기본 주소")]
    [SerializeField] private string defaultEmailAddress = "your-email@example.com";
    [Tooltip("이메일 제목")]
    [SerializeField] private string emailSubject = "Girls Evolution 2D - 문의";
    [Tooltip("이메일 본문")]
    [SerializeField] private string emailBody = "게임에 대한 문의사항을 작성해주세요.";
    
    [Header("Discord Settings")]
    [Tooltip("디스코드 초대 코드 또는 URL")]
    [SerializeField] private string discordInviteCode = "your-invite-code";
    
    [Header("Save Feedback")]
    [SerializeField] private TextMeshProUGUI saveFeedbackText;
    [SerializeField] private float saveFeedbackDuration = 2f;
    private Coroutine saveFeedbackRoutine;
    
    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);
        }
        
        // 배경 클릭으로 닫기
        if (panelRoot != null)
        {
            var bgButton = panelRoot.GetComponent<Button>();
            if (bgButton == null) bgButton = panelRoot.AddComponent<Button>();
            bgButton.onClick.RemoveAllListeners();
            bgButton.onClick.AddListener(Hide);
        }
        
        // 언어 설정 버튼
        if (languageButton != null)
        {
            languageButton.onClick.RemoveAllListeners();
            languageButton.onClick.AddListener(OnClickLanguage);
        }
        
        // 볼륨 조절 버튼
        if (volumeButton != null)
        {
            volumeButton.onClick.RemoveAllListeners();
            volumeButton.onClick.AddListener(OnClickVolume);
        }
        
        // 수동 저장 버튼
        if (manualSaveButton != null)
        {
            manualSaveButton.onClick.RemoveAllListeners();
            manualSaveButton.onClick.AddListener(OnClickManualSave);
        }
        
        // 메일 보내기 버튼
        if (sendEmailButton != null)
        {
            sendEmailButton.onClick.RemoveAllListeners();
            sendEmailButton.onClick.AddListener(OnClickSendEmail);
        }
        
        // 디스코드 버튼
        if (discordButton != null)
        {
            discordButton.onClick.RemoveAllListeners();
            discordButton.onClick.AddListener(OnClickDiscord);
        }
        
        // 저장 피드백 텍스트 초기화
        if (saveFeedbackText != null)
        {
            saveFeedbackText.gameObject.SetActive(false);
        }
    }
    
    public void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
    }
    
    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        
        // 서브 패널도 닫기
        if (languagePanel != null) languagePanel.Hide();
        if (volumePanel != null) volumePanel.Hide();
    }
    
    void OnClickLanguage()
    {
        if (languagePanel != null)
        {
            languagePanel.Show();
        }
    }
    
    void OnClickVolume()
    {
        if (volumePanel != null)
        {
            volumePanel.Show();
        }
    }
    
    void OnClickManualSave()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
            ShowSaveFeedback(LocalizationManager.GetText("저장되었습니다", "Saved"));
        }
        else
        {
            ShowSaveFeedback(LocalizationManager.GetText("저장 실패", "Save Failed"));
        }
    }
    
    void OnClickSendEmail()
    {
        EmailSender.SendEmail(defaultEmailAddress, emailSubject, emailBody);
    }
    
    void OnClickDiscord()
    {
        DiscordLinkOpener.OpenDiscordInvite(discordInviteCode);
    }
    
    void ShowSaveFeedback(string message)
    {
        if (saveFeedbackText == null) return;
        
        if (saveFeedbackRoutine != null)
        {
            StopCoroutine(saveFeedbackRoutine);
        }
        
        saveFeedbackText.text = message;
        saveFeedbackText.gameObject.SetActive(true);
        saveFeedbackRoutine = StartCoroutine(HideSaveFeedbackAfter(saveFeedbackDuration));
    }
    
    System.Collections.IEnumerator HideSaveFeedbackAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (saveFeedbackText != null)
        {
            saveFeedbackText.gameObject.SetActive(false);
        }
        saveFeedbackRoutine = null;
    }
}

