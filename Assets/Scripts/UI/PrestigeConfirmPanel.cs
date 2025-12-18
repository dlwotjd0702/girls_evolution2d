using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 환생 확인 패널
/// - 환생할 것인지 확인
/// - 환생 포인트 가중 요소 표시
/// </summary>
public class PrestigeConfirmPanel : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI breakdownText; // 환생 포인트 가중 목록 텍스트
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    
    [Header("References")]
    [SerializeField] private PrestigeManager prestigeManager;
    [SerializeField] private GirlFieldManager fieldManager;
    [SerializeField] private EconomyManager economyManager;
    
    void Awake()
    {
        if (prestigeManager == null)
            prestigeManager = PrestigeManager.Instance;
        
        if (fieldManager == null)
            fieldManager = FindObjectOfType<GirlFieldManager>(true);
        
        if (economyManager == null)
            economyManager = FindObjectOfType<EconomyManager>(true);
        
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirm);
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(Hide);
        }
        
        Hide();
    }
    
    public void Show()
    {
        if (panelRoot == null) return;
        
        panelRoot.SetActive(true);
        
        // 환생 포인트 계산 및 표시
        UpdatePrestigePointInfo();
    }
    
    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
    
    void OnConfirm()
    {
        if (prestigeManager != null)
        {
            prestigeManager.DoPrestige();
            prestigeManager.RefreshPrestigeButton(true);
        }
        Hide();
    }
    
    void UpdatePrestigePointInfo()
    {
        if (prestigeManager == null || fieldManager == null) return;
        if (breakdownText == null) return;
        
        var breakdowns = GetPrestigeBreakdowns();
        breakdownText.text = string.Join("\n", breakdowns);
    }
    
    int CalculatePrestigePoints()
    {
        if (prestigeManager == null || fieldManager == null || economyManager == null) return 0;
        
        GetPrestigeComponents(out int level25Stacks, out int lowerLevelPoints, out int upgradePoints);
        int level25Points = level25Stacks * 100;
        int totalBasePoints = level25Points + lowerLevelPoints + upgradePoints;
        double mulPPG = prestigeManager.GetPrestigePointGainMul();
        return Mathf.Max(0, Mathf.FloorToInt((float)(totalBasePoints * mulPPG)));
    }
    
    int CalculateBasePrestigeGain()
    {
        if (prestigeManager == null) return 0;
        return prestigeManager.PreviewPrestigeGain();
    }
    
    List<string> GetPrestigeBreakdowns()
    {
        var breakdowns = new List<string>();
        
        if (prestigeManager == null || fieldManager == null || economyManager == null) return breakdowns;
        
        double gainMul = prestigeManager.GetPrestigePointGainMul();
        GetPrestigeComponents(out int level25Stacks, out int lowerLevelPoints, out int upgradePoints);
        
        if (level25Stacks > 0)
        {
            breakdowns.Add(LocalizationManager.GetText(
                $"25단계 {level25Stacks}레벨 - 100*{level25Stacks} point",
                $"Level 25 x{level25Stacks} - 100*{level25Stacks} point"
            ));
        }
        
        if (lowerLevelPoints > 0)
        {
            breakdowns.Add(LocalizationManager.GetText(
                $"하위단계(합산) - {lowerLevelPoints:N0} point",
                $"Lower Tiers (Sum) - {lowerLevelPoints:N0} point"
            ));
        }
        
        if (upgradePoints > 0)
        {
            breakdowns.Add(LocalizationManager.GetText(
                $"강화수치 - {upgradePoints:N0} point",
                $"Upgrade Points - {upgradePoints:N0} point"
            ));
        }
        
        int totalBasePoints = level25Stacks * 100 + lowerLevelPoints + upgradePoints;
        if (totalBasePoints > 0)
        {
            breakdowns.Add("------------------------");
            breakdowns.Add(LocalizationManager.GetText(
                $"합산 = {totalBasePoints:N0} point",
                $"Total = {totalBasePoints:N0} point"
            ));
        }
        
        breakdowns.Add(LocalizationManager.GetText(
            $"환생 포인트 배율 x{gainMul:F2}",
            $"Prestige Point Multiplier x{gainMul:F2}"
        ));
        breakdowns.Add("------------------------");
        breakdowns.Add(LocalizationManager.GetText(
            $"최종 예상: {CalculatePrestigePoints():N0} point",
            $"Final Expected: {CalculatePrestigePoints():N0} point"
        ));
        
        return breakdowns;
    }

    void GetPrestigeComponents(out int level25Stacks, out int lowerLevelPoints, out int upgradePoints)
    {
        level25Stacks = Mathf.Max(0, fieldManager != null ? fieldManager.Level25UpgradeLevel : 0);
        lowerLevelPoints = CalculateLowerTierPoints();
        upgradePoints = CalculateUpgradePoints();
    }

    int CalculateLowerTierPoints()
    {
        if (fieldManager == null || prestigeManager == null) return 0;
        int topLevel = prestigeManager.topLevel;
        int lowerLevelPoints = 0;
        foreach (var g in fieldManager.girlList)
        {
            if (g == null) continue;
            if (g.Level >= topLevel) continue;
            
            int diff = (topLevel - 1) - g.Level;
            double points = 50.0 / Math.Pow(2.0, Mathf.Max(0, diff));
            lowerLevelPoints += Mathf.RoundToInt((float)points);
        }
        return lowerLevelPoints;
    }

    int CalculateUpgradePoints()
    {
        if (economyManager == null) return 0;
        int upgradePoints = 0;
        upgradePoints += economyManager.GetSpawnMaxUpgradeLevel();
        upgradePoints += economyManager.GetSpawnSpeedUpgradeLevel();
        upgradePoints += economyManager.GetFieldMaxUpgradeLevel();
        upgradePoints += economyManager.GetClickBonusUpgradeLevel();
        upgradePoints += economyManager.GetOfflineRewardUpgradeLevel();
        upgradePoints += economyManager.GetOfflineMaxTimeUpgradeLevel();
        upgradePoints += economyManager.GetAutoMergeUpgradeLevel();
        upgradePoints += economyManager.GetAutoSpawnUpgradeLevel();
        return upgradePoints;
    }
}

