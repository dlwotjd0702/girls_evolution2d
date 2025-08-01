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

    void Start()
    {
        // 골드 UI 초기화 및 콜백 연결
        if (currencyManager != null)
        {
            currencyManager.onGoldChanged += UpdateGoldUI;
            UpdateGoldUI(currencyManager.GetGold());
        }
        // 환생 UI 및 콜백 연결
        if (prestigeManager != null)
        {
            prestigeManager.onPrestigeAvailable += () => prestigeButton?.SetActive(true);
            prestigeButton?.SetActive(false);
            UpdatePrestigeUI();
        }
        // 업그레이드 패널 감춤
        upgradePanel?.SetActive(false);
    }

    void UpdateGoldUI(double gold)
    {
        if (!goldText) return;
        if (gold >= 1e12) goldText.text = $"{gold / 1e12:F1}T";
        else if (gold >= 1e9) goldText.text = $"{gold / 1e9:F1}B";
        else if (gold >= 1e6) goldText.text = $"{gold / 1e6:F1}M";
        else if (gold >= 1e3) goldText.text = $"{gold / 1e3:F1}K";
        else goldText.text = $"{gold:N0}";
    }

    void UpdatePrestigeUI()
    {
        if (prestigePointText && prestigeManager != null)
            prestigePointText.text = $"환생석: {prestigeManager.GetPrestigePoint()}";
    }

    // ----- UI 버튼용 명령 전달 -----

    public void OnClickPrestige()
    {
        prestigeManager?.DoPrestige();
        prestigeButton?.SetActive(false);
        UpdatePrestigeUI();
    }

    public void OnClickUpgradePanel()
    {
        if (upgradePanel != null)
            upgradePanel.SetActive(!upgradePanel.activeSelf);
    }



}
