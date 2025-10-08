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
    [SerializeField] private Button   summonButton;

    // 선택: 외부에서 주입받아 쓸 수 있게 그대로 둠(지금은 패널에서 직접 표시)
    [NonSerialized] public TMP_Text reasonLabel;

    private Action _onClick;

    void Reset()
    {
        if (!summonButton) summonButton = GetComponentInChildren<Button>();
    }

    void Awake()
    {
        if (summonButton != null)
            summonButton.onClick.AddListener(() => _onClick?.Invoke());
    }

    public void Setup(int level, string nameText, double cost, Sprite icon, Action onClick)
    {
        _onClick = onClick;

        if (nameLabel) nameLabel.text = nameText;
        if (costLabel) costLabel.text = FormatAbbrev(cost);
        if (iconImage) iconImage.sprite = icon;

        // 요구: 항상 활성 상태 유지
        if (summonButton) summonButton.interactable = true;
    }

    // 비용 라벨 갱신용
    public void UpdateCost(double newCost)
    {
        if (costLabel) costLabel.text = FormatAbbrev(newCost);
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
        return $"{v:0}";
    }
}