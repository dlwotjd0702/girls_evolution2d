using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 도감 상세 정보 팝업 패널
/// - 상단: LD 일러스트 크게 표시
/// - 하단: 이름, 레벨, income 정보 표시
/// </summary>
public class EncyclopediaDetailPanel : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image ldIllustrationImage;  // 상단 큰 일러스트
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI incomeText;
    [SerializeField] private Button closeButton;
    

    void Awake()
    {
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
        
        Hide();
    }
    
    public void Show(string name, int level, double income, Sprite ldSprite)
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        
        // LD 일러스트
        if (ldIllustrationImage != null)
        {
            ldIllustrationImage.sprite = ldSprite;
            ldIllustrationImage.color = ldSprite != null ? Color.white : new Color(1, 1, 1, 0);
            if (ldIllustrationImage.preserveAspect == false)
                ldIllustrationImage.preserveAspect = true;
        }
        
        // 이름
        if (nameText != null)
        {
            nameText.text = name ?? LocalizationManager.GetText("알 수 없음", "Unknown");
        }
        
        // 레벨
        if (levelText != null)
        {
            levelText.text = LocalizationManager.GetText($"{level} 단계", $"Level {level}");
        }
        
        // 수익
        if (incomeText != null)
        {
            string format = LocalizationManager.GetText("수익: {0:N0}", "Income: {0:N0}");
            incomeText.text = string.Format(format, income)+"G/s";
        }
    }
    
    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }
    
    public bool IsVisible()
    {
        return panelRoot != null && panelRoot.activeSelf;
    }
}

