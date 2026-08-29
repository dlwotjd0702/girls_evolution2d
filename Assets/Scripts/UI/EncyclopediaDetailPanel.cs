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
    public TextMeshProUGUI collectionText;
    public Button equipButton;
    public TextMeshProUGUI equipLabel;
    

    void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);
        }
        
        // Initial visibility belongs to the scene. Hiding here cancels the first Show
        // when this component lives on the previously inactive popup itself.
    }
    
    public void Show(string name, int level, double income, Sprite ldSprite)
    {
        if (collectionText) collectionText.text = "";
        if (equipButton) equipButton.gameObject.SetActive(false);
        if (panelRoot != null) { panelRoot.transform.SetAsLastSibling(); panelRoot.SetActive(true); }
        
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
            string format = LocalizationManager.GetText("수익: {0}", "Income: {0}");
            string formattedIncome = EconomyManager.FormatAbbrev(income, 1, ""); // 단위 없이 숫자만
            incomeText.text = string.Format(format, formattedIncome) + "/s";
        }
    }

    public void ShowCollection(string name, int level, double income, Sprite sprite, string description,
        bool unlocked, string actionLabel = null, System.Action action = null)
    {
        if (!unlocked) { Hide(); return; }
        Show(name,level,income,sprite);
        if (ldIllustrationImage && !unlocked) ldIllustrationImage.color = new Color(0.13f,0.17f,0.24f,1);
        if (incomeText && !unlocked) incomeText.text = LocalizationManager.GetText("미해금", "LOCKED");
        if (collectionText) collectionText.text = description;
        if (equipButton)
        {
            equipButton.gameObject.SetActive(!string.IsNullOrEmpty(actionLabel));
            equipButton.interactable = unlocked && action != null;
            equipButton.onClick.RemoveAllListeners();
            if (action != null) equipButton.onClick.AddListener(() => action());
        }
        if (equipLabel) equipLabel.text = actionLabel ?? "";
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

