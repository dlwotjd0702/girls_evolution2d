using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SummonCell : MonoBehaviour
{
    [Header("UI Refs (in prefab)")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text costLabel;
    [SerializeField] private Button summonButton;

    // 패널(상위 캔버스)에 있는 공용 Reason Label을 "외부에서 주입"
    // SummonPanelController가 할당해줌
    public TMP_Text reasonLabel; // <- 컴파일 에러 해결 포인트 ①

    private Action _onClick;
    private int _level;
    private double _cost;

    void Reset()
    {
        if (!summonButton) summonButton = GetComponentInChildren<Button>();
    }

    void Awake()
    {
        if (summonButton != null)
            summonButton.onClick.AddListener(() => _onClick?.Invoke());
    }

    /// <summary>
    /// 셀 데이터 바인딩
    /// </summary>
    public void Setup(int level, string nameText, double cost, Sprite icon, Action onClick) // ← 에러 해결 포인트 ② (icon 파라미터 포함)
    {
        _level = level;
        _cost = cost;
        _onClick = onClick;

        if (nameLabel) nameLabel.text = nameText;
        if (costLabel) costLabel.text = FormatAbbrev(cost);
        if (iconImage) iconImage.sprite = icon;
    }

    /// <summary>
    /// 버튼 활성/비활성
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (summonButton) summonButton.interactable = interactable;

        // 비활성 사유는 SetReason에서 처리 (외부 공용 라벨)
        if (!interactable)
        {
            // 필요시 여기서도 시각적 처리 가능(회색처리 등)
        }
    }

    /// <summary>
    /// 비활성 사유 표시 (공용 reasonLabel 사용)
    /// </summary>
    public void SetReason(bool noSpace, bool noGold) // ← 에러 해결 포인트 ③ (2개 bool 받는 오버로드)
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
