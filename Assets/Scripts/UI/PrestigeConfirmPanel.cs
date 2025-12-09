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
        
        // 배경 클릭으로 닫기
        if (panelRoot != null)
        {
            var bgButton = panelRoot.GetComponent<Button>();
            if (bgButton == null) bgButton = panelRoot.AddComponent<Button>();
            bgButton.onClick.RemoveAllListeners();
            bgButton.onClick.AddListener(Hide);
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
        if (prestigeManager == null) return 0;
        
        // PrestigeManager의 PreviewPrestigeGain()을 사용하여 실제 지급 포인트와 일치시킴
        int baseGain = prestigeManager.PreviewPrestigeGain();
        double mulPPG = prestigeManager.GetPrestigePointGainMul();
        return Mathf.Max(0, Mathf.CeilToInt((float)(baseGain * mulPPG)));
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
        
        // 레벨 25 포인트: 1000 포인트 × level25Stacks (PrestigeManager와 일치)
        if (level25Stacks > 0)
        {
            int level25Points = level25Stacks * 1000;
            breakdowns.Add(LocalizationManager.GetText(
                $"25단계 {level25Stacks}레벨 - 1000*{level25Stacks} = {level25Points:N0} point",
                $"Level 25 x{level25Stacks} - 1000*{level25Stacks} = {level25Points:N0} point"
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
        
        int totalBasePoints = level25Stacks * 1000 + lowerLevelPoints + upgradePoints;
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
            // 레벨 25 이상은 이미 계산했으므로 제외 (PrestigeManager와 일치)
            if (g.Level >= topLevel) continue;
            
            // 하위 단계 포인트 계산: 25단계 기준 1000 포인트에서 단계 내려갈 때마다 1/2
            // 24단계: 500, 23단계: 250, 22단계: 125, ... (PrestigeManager와 일치)
            int diff = topLevel - g.Level; // 25→24: 1, 25→23: 2, ...
            double points = 1000.0 / Math.Pow(2.0, diff);
            lowerLevelPoints += Mathf.CeilToInt((float)points); // 소수점 올림 처리 (PrestigeManager와 일치)
        }
        return lowerLevelPoints;
    }

    int CalculateUpgradePoints()
    {
        if (economyManager == null) return 0;
        // 골드/보석 강화 레벨에 대한 포인트: 100 포인트/레벨 (PrestigeManager와 일치)
        const int POINTS_PER_UPGRADE_LEVEL = 100;
        
        int upgradePoints = 0;
        upgradePoints += economyManager.GetSpawnMaxUpgradeLevel() * POINTS_PER_UPGRADE_LEVEL;
        upgradePoints += economyManager.GetSpawnSpeedUpgradeLevel() * POINTS_PER_UPGRADE_LEVEL;
        upgradePoints += economyManager.GetFieldMaxUpgradeLevel() * POINTS_PER_UPGRADE_LEVEL;
        upgradePoints += economyManager.GetClickBonusUpgradeLevel() * POINTS_PER_UPGRADE_LEVEL;
        upgradePoints += economyManager.GetAutoMergeUpgradeLevel() * POINTS_PER_UPGRADE_LEVEL;
        upgradePoints += economyManager.GetAutoSpawnUpgradeLevel() * POINTS_PER_UPGRADE_LEVEL;
        upgradePoints += economyManager.GetOfflineRewardUpgradeLevel() * POINTS_PER_UPGRADE_LEVEL;
        upgradePoints += economyManager.GetOfflineMaxTimeUpgradeLevel() * POINTS_PER_UPGRADE_LEVEL;
        return upgradePoints;
    }
}

