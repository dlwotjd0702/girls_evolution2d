using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 도감 슬롯 개별 UI 컴포넌트
/// </summary>
public class EncyclopediaSlot : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private TMP_Text lockedLabel;
    [SerializeField] private Button button;
    
    private int level;
    private bool isUnlocked;
    private Action onClick;
    public void SetCaption(string caption)
    {
        if (!levelText) return;
        levelText.text = caption;
        levelText.enableAutoSizing = true;
        levelText.fontSizeMin = 34;
        levelText.fontSizeMax = 42;
    }
    
    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
    }
    
    public void Setup(int level, string name, Sprite sprite, bool isUnlocked, Action onClick, bool inspectLocked = false)
    {
        this.level = level;
        this.isUnlocked = isUnlocked;
        this.onClick = onClick;
        
        // 아이콘 설정
        if (iconImage != null)
        {
            iconImage.sprite = sprite;
            iconImage.preserveAspect = true;
            iconImage.color = !sprite ? Color.clear : isUnlocked ? Color.white : new Color(0.18f,0.22f,0.30f,1);
        }
        
        // 레벨 텍스트
        if (levelText != null)
        {
            levelText.text = level.ToString();
        }
        
        // 잠금 오버레이 (슬롯 자체에 잠금 이미지 포함되어 있으므로 SetActive만 관리)
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!isUnlocked);
        }
        if (lockedLabel) lockedLabel.text = LocalizationManager.GetText("미해금", "LOCKED");
        
        // 버튼 상호작용 및 클릭 리스너 설정
        if (button != null)
        {
            button.interactable = isUnlocked;
            button.onClick.RemoveAllListeners();
            // Guard the callback too: a stale/programmatic UnityEvent must not open locked art.
            button.onClick.AddListener(() => { if (this.isUnlocked) this.onClick?.Invoke(); });
        }
    }
}

