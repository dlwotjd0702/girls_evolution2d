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

        if (nameLabel) nameLabel.text = nameText;
        if (costLabel) costLabel.text = FormatAbbrev(cost);
        if (iconImage) iconImage.sprite = icon;

        // 보석 비용 표시
        if (gemCostLabel != null)
        {
            if (gemCost > 0 && onGemClick != null)
            {
                gemCostLabel.text = $"{gemCost:N0} G";
                gemCostLabel.gameObject.SetActive(true);
            }
            else
            {
                gemCostLabel.gameObject.SetActive(false);
            }
        }

        // 요구: 항상 활성 상태 유지 (실제 상호작용 가능 여부는 패널에서 관리)
        if (summonButton) summonButton.interactable = true;
        if (gemSummonButton) gemSummonButton.interactable = (onGemClick != null && gemCost > 0);
    }

    // 비용 라벨 갱신용
    public void UpdateCost(double newCost)
    {
        if (costLabel) costLabel.text = FormatAbbrev(newCost);
    }
    
    // 보석 비용 및 버튼 상태 갱신
    public void UpdateGemCost(long gemCost, bool canAfford)
    {
        if (gemCostLabel != null)
        {
            if (gemCost > 0)
            {
                gemCostLabel.text = $"{gemCost:N0} Gem";
                gemCostLabel.gameObject.SetActive(true);
            }
            else
            {
                gemCostLabel.gameObject.SetActive(false);
            }
        }
        
        if (gemSummonButton != null)
        {
            gemSummonButton.interactable = (gemCost > 0 && canAfford);
        }
    }
    
    // 버튼 상호작용 가능 여부 업데이트 (필드 가득 참 등)
    public void SetButtonsInteractable(bool goldInteractable, bool gemInteractable)
    {
        if (summonButton != null) summonButton.interactable = goldInteractable;
        if (gemSummonButton != null) gemSummonButton.interactable = gemInteractable;
    }

    // (패널에서 ReasonLabel 직접 관리하므로 현재 미사용)
    public void SetReason(bool noSpace, bool noGold)
    {
        if (!reasonLabel) return;
        if (noSpace)       reasonLabel.text = "필드가 가득 찼습니다.";
        else if (noGold)   reasonLabel.text = "골드가 부족합니다.";
        else               reasonLabel.text = string.Empty;
    }

    public static string FormatAbbrev(double v)
    {
        double av = Math.Abs(v);
        if (av >= 1e12) return $"{v / 1e12:0.#}T";
        if (av >= 1e9)  return $"{v / 1e9:0.#}B";
        if (av >= 1e6)  return $"{v / 1e6:0.#}M";
        if (av >= 1e3)  return $"{v / 1e3:0.#}K";
        return $"{v:0}G";
    }
}