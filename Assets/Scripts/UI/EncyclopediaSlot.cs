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
    }
    
    public void Setup(int level, string name, Sprite sprite, bool isUnlocked, Action onClick)
    {
        this.level = level;
        this.isUnlocked = isUnlocked;
        this.onClick = onClick;
        
        // 아이콘 설정
        if (iconImage != null)
        {
            iconImage.sprite = sprite;
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
        
        // 버튼 상호작용 및 클릭 리스너 설정
        if (button != null)
        {
            button.interactable = isUnlocked;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }
    }
}

