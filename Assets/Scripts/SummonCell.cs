using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SummonCell : MonoBehaviour
{
    [Header("UI Refs (in prefab)")]
    [SerializeField] private Image    iconImage;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text costLabel;
    [SerializeField] private Button   summonButton;      // 골드 소환 버튼
    [SerializeField] private Button   gemSummonButton;   // 보석 소환 버튼
    [SerializeField] private TMP_Text gemCostLabel;      // 보석 비용 표시 (선택사항)

    // 선택: 외부에서 주입받아 쓸 수 있게 그대로 둠(지금은 패널에서 직접 표시)
    [NonSerialized] public TMP_Text reasonLabel;

    private Action _onClick;
    private Action _onGemClick;
    private bool _hasGoldAction;
    private bool _hasGemAction;

    void Reset()
    {
        if (!summonButton) summonButton = GetComponentInChildren<Button>();
    }

    void Awake()
    {
        if (summonButton != null)
            summonButton.onClick.AddListener(() => _onClick?.Invoke());
        
        if (gemSummonButton != null)
            gemSummonButton.onClick.AddListener(() => _onGemClick?.Invoke());
    }

    public void Setup(int level, string nameText, double cost, Sprite icon, Action onClick, Action onGemClick = null, long gemCost = 0)
    {
        _onClick = onClick;
        _onGemClick = onGemClick;
        _hasGoldAction = onClick != null;
        _hasGemAction = onGemClick != null;

        if (nameLabel) nameLabel.text = nameText;
        UpdateCost(cost);
        if (iconImage) iconImage.sprite = icon;

        // 보석 비용 표시
        if (gemCostLabel != null)
        {
            if (gemCost > 0 && onGemClick != null)
            {
                gemCostLabel.text = LocalizationManager.GetText($"보석 소환\n{gemCost:N0}개", $"SUMMON\n{gemCost:N0} Gems");
                gemCostLabel.gameObject.SetActive(true);
            }
            else
            {
                gemCostLabel.gameObject.SetActive(false);
            }
        }

        // 요구: 항상 활성 상태 유지 (실제 상호작용 가능 여부는 패널에서 관리)
        if (summonButton) summonButton.interactable = _hasGoldAction;
        if (gemSummonButton) gemSummonButton.interactable = _hasGemAction && gemCost > 0;
    }

    // 비용 라벨 갱신용
    public void UpdateCost(double newCost)
    {
        if (costLabel) costLabel.text = LocalizationManager.GetText("골드 소환\n", "SUMMON\n") + EconomyManager.FormatAbbrev(newCost);
    }
    
    // 보석 비용 및 버튼 상태 갱신
    public void UpdateGemCost(long gemCost, bool canAfford)
    {
        _ = canAfford; // 버튼은 항상 활성 상태를 유지
        if (gemCostLabel != null)
        {
            if (gemCost > 0)
            {
                gemCostLabel.text = LocalizationManager.GetText($"보석 소환\n{gemCost:N0}개", $"SUMMON\n{gemCost:N0} Gems");
                gemCostLabel.gameObject.SetActive(true);
            }
            else
            {
                gemCostLabel.gameObject.SetActive(false);
            }
        }
        
        if (gemSummonButton != null)
        {
            gemSummonButton.interactable = _hasGemAction && gemCost > 0;
        }
    }
    
    // 버튼 상호작용 가능 여부 업데이트 (필드 가득 참 등)
    public void SetButtonsInteractable(bool goldInteractable, bool gemInteractable)
    {
        if (summonButton != null) summonButton.interactable = _hasGoldAction && goldInteractable;
        if (gemSummonButton != null) gemSummonButton.interactable = _hasGemAction && gemInteractable;
    }

    // (패널에서 ReasonLabel 직접 관리하므로 현재 미사용)
    public void SetReason(bool noSpace, bool noGold)
    {
        if (!reasonLabel) return;
        if (noSpace)       reasonLabel.text = "필드가 가득 찼습니다.";
        else if (noGold)   reasonLabel.text = "골드가 부족합니다.";
        else               reasonLabel.text = string.Empty;
    }

}
