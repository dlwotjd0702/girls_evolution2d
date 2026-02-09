using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 볼륨 설정 패널
/// 전체 볼륨 조절 (모든 AudioSource에 적용)
/// </summary>
public class VolumeSettingsPanel : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI titleText;
    
    [Header("Volume Control")]
    [SerializeField] private TextMeshProUGUI volumeLabel;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TextMeshProUGUI volumeValueText;
    
    [Header("Settings")]
    [SerializeField] private float defaultVolume = 0.7f;
    
    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);
        }
        
        // 볼륨 슬라이더
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", defaultVolume);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        
        UpdateTexts();
        ApplyVolume();
    }
    
    void OnEnable()
    {
        UpdateTexts();
        ApplyVolume();
    }
    
    public void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        UpdateTexts();
        ApplyVolume();
    }
    
    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }
    
    void OnVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
        
        // AudioManager에 볼륨 업데이트 알림
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.UpdateMasterVolume(value);
        }
        
        // AudioManager의 AudioSource에 적용
        // (AudioManager가 모든 오디오를 관리하므로 별도로 FindObjectsOfType 불필요)
        
        UpdateVolumeText(volumeValueText, value);
    }
    
    void ApplyVolume()
    {
        float volume = PlayerPrefs.GetFloat("MasterVolume", defaultVolume);
        
        if (volumeSlider != null) volumeSlider.value = volume;
        
        OnVolumeChanged(volume);
    }
    
    void UpdateVolumeText(TextMeshProUGUI text, float value)
    {
        if (text != null)
        {
            text.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }
    
    void UpdateTexts()
    {
        if (titleText != null)
        {
            titleText.text = LocalizationManager.GetText("볼륨 설정", "Volume Settings");
        }
        
        if (volumeLabel != null)
        {
            volumeLabel.text = LocalizationManager.GetText("볼륨", "Volume");
        }
        
        if (volumeSlider != null)
        {
            UpdateVolumeText(volumeValueText, volumeSlider.value);
        }
    }
}


