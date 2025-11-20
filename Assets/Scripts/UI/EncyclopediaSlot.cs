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
    [SerializeField] private Button button;
    
    private int level;
    private bool isUnlocked;
    private Action onClick;
    
    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }
    }
    
    public void Setup(int level, string name, Sprite sprite, bool isUnlocked, 
        Sprite lockedSprite, Color lockedColor, Action onClick)
    {
        this.level = level;
        this.isUnlocked = isUnlocked;
        this.onClick = onClick;
        
        // 아이콘 설정
        if (iconImage != null)
        {
            iconImage.sprite = sprite ?? lockedSprite;
            iconImage.color = isUnlocked ? Color.white : lockedColor;
        }
        
        // 레벨 텍스트
        if (levelText != null)
        {
            levelText.text = level.ToString();
            levelText.color = isUnlocked ? Color.white : lockedColor;
        }
        
        // 잠금 오버레이
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!isUnlocked);
        }
        
        // 버튼 상호작용
        if (button != null)
        {
            button.interactable = isUnlocked;
        }
        
        // 배경 이미지 (잠금 스프라이트)
        if (backgroundImage != null && !isUnlocked && lockedSprite != null)
        {
            backgroundImage.sprite = lockedSprite;
            backgroundImage.color = lockedColor;
        }
    }
}

