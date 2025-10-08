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
    public EconomyManager economy;
    public PrestigeManager prestigeManager;

    void OnEnable()
    {
        if (economy != null)
        {
            economy.OnGoldChanged -= UpdateGoldUI;
            economy.OnGoldChanged += UpdateGoldUI;
            UpdateGoldUI(economy.GetGold());
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
        if (economy != null) economy.OnGoldChanged -= UpdateGoldUI;
        if (prestigeManager != null) prestigeManager.onPrestigeAvailable -= OnPrestigeAvailable;
    }

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

    public void OnClickPrestige()
    {
        prestigeManager?.DoPrestige();
        prestigeButton?.SetActive(false);
        UpdatePrestigeUI();
        if (economy != null) UpdateGoldUI(economy.GetGold());
    }

    public void OnClickUpgradePanel()
    {
        if (upgradePanel != null)
            upgradePanel.SetActive(!upgradePanel.activeSelf);
    }

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
