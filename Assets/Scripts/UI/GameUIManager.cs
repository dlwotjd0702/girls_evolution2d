using UnityEngine;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI prestigePointText;
    public GameObject upgradePanel;
    public GameObject prestigeButton;

    [Header("Manager DI")]
    public CurrencyManager currencyManager;
    public PrestigeManager prestigeManager;
    public UpgradeManager upgradeManager;

    // ─────────────────────────────────────────────────────────
    // 구독은 OnEnable / 해제는 OnDisable 로 일원화
    // (씬 리로드/비활성-재활성 시 중복 구독 방지)
    // ─────────────────────────────────────────────────────────
    void OnEnable()
    {
        if (currencyManager != null)
        {
            currencyManager.onGoldChanged -= UpdateGoldUI;
            currencyManager.onGoldChanged += UpdateGoldUI;
            UpdateGoldUI(currencyManager.GetGold());
        }

        if (prestigeManager != null)
        {
            prestigeManager.onPrestigeAvailable -= OnPrestigeAvailable;
            prestigeManager.onPrestigeAvailable += OnPrestigeAvailable;
        }

        prestigeButton?.SetActive(false);
        UpdatePrestigeUI();
        upgradePanel?.SetActive(false);
    }

    void OnDisable()
    {
        if (currencyManager != null)
            currencyManager.onGoldChanged -= UpdateGoldUI;

        if (prestigeManager != null)
            prestigeManager.onPrestigeAvailable -= OnPrestigeAvailable;
    }

    // ───────────────── UI 업데이트 ─────────────────
    void UpdateGoldUI(double gold)
    {
        if (!goldText) return;
        goldText.text = FormatAbbrev(gold);
    }

    void UpdatePrestigeUI()
    {
        if (prestigePointText && prestigeManager != null)
            prestigePointText.text = $"환생석: {prestigeManager.GetPrestigePoint()}";
    }

    void OnPrestigeAvailable()
    {
        if (prestigeButton) prestigeButton.SetActive(true);
    }

    // ───────────────── 버튼 핸들러 ─────────────────
    public void OnClickPrestige()
    {
        prestigeManager?.DoPrestige();
        prestigeButton?.SetActive(false);
        UpdatePrestigeUI();
        // 환생 후 골드도 0으로 바뀌므로 즉시 갱신
        if (currencyManager != null) UpdateGoldUI(currencyManager.GetGold());
    }

    public void OnClickUpgradePanel()
    {
        if (upgradePanel != null)
            upgradePanel.SetActive(!upgradePanel.activeSelf);
    }

    // ───────────────── 헬퍼 ─────────────────
    static string FormatAbbrev(double v)
    {
        double av = System.Math.Abs(v);
        if (av >= 1e12) return $"{v / 1e12:0.#}T";
        if (av >= 1e9)  return $"{v / 1e9:0.#}B";
        if (av >= 1e6)  return $"{v / 1e6:0.#}M";
        if (av >= 1e3)  return $"{v / 1e3:0.#}K";
        return $"{v:N0}";
    }
}
