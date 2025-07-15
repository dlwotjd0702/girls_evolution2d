using UnityEngine;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI prestigePointText;
    public GameObject upgradePanel;
    public GameObject prestigeButton;
    
    [Header("Manager References")]
    public CurrencyManager currencyManager;
    public PrestigeManager prestigeManager;
    public UpgradeManager upgradeManager;
    
    void Start()
    {
        // 골드 UI 초기화
        if (currencyManager != null)
        {
            currencyManager.onGoldChanged += UpdateGoldUI;
            UpdateGoldUI(currencyManager.GetGold());
        }
        
        // 환생 UI 초기화
        if (prestigeManager != null)
        {
            prestigeManager.onPrestigeAvailable += () => prestigeButton.SetActive(true);
            prestigeButton.SetActive(false);
            UpdatePrestigeUI();
        }
        
        // 업그레이드 패널 초기화
        if (upgradePanel != null)
            upgradePanel.SetActive(false);
    }
    
    void UpdateGoldUI(double gold)
    {
        if (goldText != null)
        {
            if (gold >= 1e12) // 1조 이상
                goldText.text = $"{gold / 1e12:F1}T";
            else if (gold >= 1e9) // 10억 이상
                goldText.text = $"{gold / 1e9:F1}B";
            else if (gold >= 1e6) // 100만 이상
                goldText.text = $"{gold / 1e6:F1}M";
            else if (gold >= 1e3) // 1000 이상
                goldText.text = $"{gold / 1e3:F1}K";
            else
                goldText.text = $"{gold:N0}";
        }
    }
    
    void UpdatePrestigeUI()
    {
        if (prestigePointText != null && prestigeManager != null)
        {
            prestigePointText.text = $"환생석: {prestigeManager.GetPrestigePoint()}";
        }
    }
    
    public void OnClickPrestige()
    {
        if (prestigeManager != null)
        {
            prestigeManager.DoPrestige();
            prestigeButton.SetActive(false);
            UpdatePrestigeUI();
        }
    }
    
    public void OnClickUpgradePanel()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(!upgradePanel.activeSelf);
        }
    }
    
    public void OnClickIncomeUpgrade()
    {
        if (upgradeManager != null && currencyManager != null)
        {
            upgradeManager.UpgradeIncome(currencyManager);
        }
    }
    
    public void OnClickAutoIncomeUpgrade()
    {
        if (upgradeManager != null && currencyManager != null)
        {
            upgradeManager.UpgradeAutoIncome(currencyManager);
        }
    }
    
    public void OnClickClickIncomeUpgrade()
    {
        if (upgradeManager != null && currencyManager != null)
        {
            upgradeManager.UpgradeClickIncome(currencyManager);
        }
    }
    
    public void OnClickSpawnUpgrade()
    {
        if (upgradeManager != null && currencyManager != null)
        {
            upgradeManager.UpgradeSpawn(currencyManager);
        }
    }
    
    public void OnClickMergeBonusUpgrade()
    {
        if (upgradeManager != null && currencyManager != null)
        {
            upgradeManager.UpgradeMergeBonus(currencyManager);
        }
    }
    
    public void OnClickMaxGirlCountUpgrade()
    {
        if (upgradeManager != null && currencyManager != null)
        {
            upgradeManager.UpgradeMaxGirlCount(currencyManager);
        }
    }
} 