using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SummonPanelController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GirlDataManager dataManager;
    [SerializeField] private GirlFieldManager fieldManager;
    [SerializeField] private GirlSpriteAddressableLoader spriteLoader;
    [SerializeField] private EconomyManager economy;
    [SerializeField] private PremiumCurrencyManager premiumCurrency; // 보석 관리자
    
    [Header("Gem Summon Settings")]
    [Tooltip("보석 소환 비용 증가량 (소환 횟수당 +1)")]
    [SerializeField] private int gemSummonIncrement = 1; // 소환 횟수당 1개씩 증가

    [Header("UI")]
    [SerializeField] private RectTransform   content;       // ScrollView Content
    [SerializeField] private GameObject      cellPrefab;    // SummonCell 프리팹(GameObject)
    [SerializeField] private TextMeshProUGUI reasonLabel;   // 공용 사유 라벨
    [SerializeField] private InsufficientFundsPanel insufficientFundsPanel; // 골드 부족 패널
    public TextMeshProUGUI availabilityLabel;

    [Header("Refresh")]
    [SerializeField] private float interactableRefreshInterval = 0.25f;

    [Header("Reason Popup")]
    [SerializeField] private float reasonShowSeconds = 1.15f;

    private readonly Dictionary<int, SummonCell> cells = new();
    private float _t;
    private Coroutine _reasonRoutine;
    private Action<int> _maxLevelChangedHandler;
    private const string GemAdHint = "광고 시청 보상으로 보석 5개를 받을 수 있어요!";

    void Awake()
    {
        var gs = GameSystem.Instance;
        if (gs != null)
        {
            if (dataManager == null)  dataManager  = gs.girlDataManager;
            if (fieldManager == null) fieldManager = gs.fieldManager;
            if (spriteLoader == null) spriteLoader = gs.spriteLoader;
            if (economy == null)      economy      = gs.economy;
        }
        
        if (premiumCurrency == null)
            premiumCurrency = FindObjectOfType<PremiumCurrencyManager>(true);
        
        if (reasonLabel) reasonLabel.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        TryBuildIfReady();
        if (fieldManager != null)
        {
            _maxLevelChangedHandler = HandleMaxLevelChanged;
            fieldManager.OnMaxLevelChanged -= _maxLevelChangedHandler;
            fieldManager.OnMaxLevelChanged += _maxLevelChangedHandler;
        }
    }

    void OnDisable()
    {
        if (fieldManager != null && _maxLevelChangedHandler != null)
            fieldManager.OnMaxLevelChanged -= _maxLevelChangedHandler;
        HideReasonImmediate();
    }

    void Update()
    {
        if (cells.Count == 0) TryBuildIfReady();

        _t += Time.unscaledDeltaTime;
        if (_t >= interactableRefreshInterval)
        {
            _t = 0f;
            RefreshCostsOnly();
        }
    }

    void HandleMaxLevelChanged(int _){ Rebuild(); }

    void TryBuildIfReady()
    {
        if (dataManager != null && dataManager.IsLoaded && content && cellPrefab)
            Rebuild();
    }

    void Rebuild()
    {
        foreach (Transform child in content) Destroy(child.gameObject);
        cells.Clear();

        int currentMax = fieldManager != null ? Mathf.Max(1, fieldManager.CurrentMaxLevel) : 3;
        int maxSummonable = MaxSummonableLevel(currentMax);

        for (int level = 1; level <= maxSummonable; level++)
        {
            var data = dataManager.GetDataByLevel(level);
            if (data == null) continue;

            var go   = Instantiate(cellPrefab, content);
            var cell = go.GetComponent<SummonCell>();
            if (!cell) { Debug.LogError("[SummonPanel] SummonCell missing."); continue; }

            cell.reasonLabel = reasonLabel;

            Sprite icon    = spriteLoader ? spriteLoader.GetSpriteForData(data, preferLD:false) : null;
            double curCost = CurrentCost(level);

            int lv = level;
            long gemCost = GetGemCostForLevel(lv);
            
            // 이름을 짧고 간결하게: "Lv.X 이름" -> "X. 이름"
            string shortName = LocalizationManager.GetText(
                $"{data.level}단계 \n {data.name}",
                $"Lv.{data.level}\n{data.LocalizedName}");
            
            cell.Setup(
                level: lv,
                nameText: shortName,
                cost: curCost,
                icon: icon,
                onClick: () => OnClickSummon(lv),
                onGemClick: () => OnClickGemSummon(lv),
                gemCost: gemCost
            );

            cells[level] = cell;
        }
        HideReasonImmediate();
        RefreshCostsOnly();
    }

    public static int MaxSummonableLevel(int currentMax) => Mathf.Clamp(currentMax - 2, 1, TierRules.MaxLevel - 2);

    bool ValidateSummon(int level)
    {
        if (!fieldManager || dataManager == null || !dataManager.IsLoaded || dataManager.GetDataByLevel(level) == null
            || level < 1 || level > MaxSummonableLevel(fieldManager.CurrentMaxLevel))
        {
            ShowReasonTemp(LocalizationManager.GetText("아직 소환할 수 없는 단계입니다.", "This stage is not available yet."));
            return false;
        }
        return true;
    }

    double CurrentCost(int level)
    {
        if (economy != null) return economy.GetSummonCostByMinuteRule(level);
        // fallback(이상 케이스)
        double perSec = Math.Pow(2.0, Math.Max(0, level-1));
        return 60.0 * perSec;
    }

    void OnClickSummon(int level)
    {
        if (!ValidateSummon(level)) return;
        if (fieldManager == null) { ShowReasonTemp(LocalizationManager.GetText("필드가 없습니다.", "Field is unavailable.")); return; }

        int fieldCap = (economy != null) ? economy.GetMaxFieldCount() : 8;
        if (fieldManager.girlList.Count >= fieldCap)
        {
            ShowReasonTemp(LocalizationManager.GetText("필드가 가득 찼습니다.", "The field is full."));
            return;
        }

        double cost = CurrentCost(level);
        double currentGold = economy != null ? economy.GetGold() : 0;
        
        if (economy == null)
        {
            ShowReasonTemp(LocalizationManager.GetText("골드 정보를 불러오지 못했습니다.", "Could not load gold data."));
            return;
        }
        
        if (!economy.SpendGold(cost))
        {
            if (insufficientFundsPanel != null)
            {
                insufficientFundsPanel.ShowForGoldShortage(cost, currentGold);
            }
            
            ShowReasonTemp(LocalizationManager.GetText("골드가 부족합니다.", "Not enough gold."));
            return;
        }

        fieldManager.ManualSpawnGirl(level, Vector3.zero);
        economy?.RecordSummonPurchase(level);

        if (cells.TryGetValue(level, out var cell) && cell != null)
            cell.UpdateCost(CurrentCost(level));

        HideReasonImmediate();
        RefreshCostsOnly();
    }

    // ─── Reason helpers ───
    void ShowReasonTemp(string msg)
    {
        if (!reasonLabel) return;
        HideReasonImmediate();
        reasonLabel.text = msg;
        reasonLabel.gameObject.SetActive(true);
        _reasonRoutine = StartCoroutine(HideReasonAfter(reasonShowSeconds));
    }
    IEnumerator HideReasonAfter(float sec){ yield return new WaitForSecondsRealtime(sec); HideReasonImmediate(); }
    void HideReasonImmediate(){ if (!reasonLabel) return; if (_reasonRoutine!=null){ StopCoroutine(_reasonRoutine); _reasonRoutine=null; } reasonLabel.text=""; reasonLabel.gameObject.SetActive(false); }

    void RefreshCostsOnly()
    {
        long currentGems = premiumCurrency != null ? premiumCurrency.GetGems() : 0;
        int count = fieldManager ? fieldManager.girlList.Count : 0;
        int cap = economy ? economy.GetMaxFieldCount() : 8;
        bool hasSpace = fieldManager && count < cap;
        int maxLevel = MaxSummonableLevel(fieldManager ? fieldManager.CurrentMaxLevel : 1);
        if (availabilityLabel)
        {
            string next = maxLevel < TierRules.MaxLevel - 2
                ? LocalizationManager.GetText($"{maxLevel + 3}단계 도달 → {maxLevel + 1}단계 소환 해금", $"Reach Lv.{maxLevel + 3} to summon Lv.{maxLevel + 1}")
                : LocalizationManager.GetText("모든 소환 단계 해금", "All summon stages unlocked");
            availabilityLabel.text = LocalizationManager.GetText($"필드 {count}/{cap} · {(hasSpace ? $"빈자리 {cap - count}칸" : "합성 후 소환 가능")}",
                $"Field {count}/{cap} · {(hasSpace ? $"{cap - count} free" : "Merge to free space")}") + "\n" + next;
        }
        
        foreach (var kv in cells)
        {
            var cell = kv.Value;
            if (!cell) continue;
            
            int level = kv.Key;
            double goldCost = CurrentCost(level);
            long gemCost = GetGemCostForLevel(level);
            
            cell.UpdateCost(goldCost);
            cell.UpdateGemCost(gemCost, currentGems >= gemCost);
            // Keep shortage explanations reachable; only capacity/readiness blocks the action.
            cell.SetButtonsInteractable(hasSpace && economy, hasSpace && premiumCurrency && economy);
        }
    }
    
    long GetGemCostForLevel(int level)
    {
        int purchaseCount = economy != null ? economy.GetGemSummonPurchaseCount(level) : 0;
        return CalculateGemSummonCost(level, purchaseCount, gemSummonIncrement);
    }

    public static long CalculateGemSummonCost(int level, int purchaseCount, int configuredIncrement = 1)
    {
        level = Mathf.Clamp(level, 1, TierRules.MaxLevel);
        int tier = TierRules.TierIndexFromLevel(level);
        int baseCost = tier switch
        {
            0 => 3,  // 1티어 (1-8레벨): 3개
            1 => 8,  // 2티어 (9-16레벨)
            2 => 20, // 3티어 (17-24레벨): 최종 스택 직행 억제
            _ => 20
        };
        int tierIncrement = tier switch { 0 => 1, 1 => 2, _ => 5 };
        long step = Math.Max(1, configuredIncrement) * tierIncrement;
        return Math.Max(1, baseCost + Math.Max(0, purchaseCount) * step);
    }
    
    void OnClickGemSummon(int level)
    {
        if (!ValidateSummon(level) || !economy) return;
        if (fieldManager == null) { ShowReasonTemp(LocalizationManager.GetText("필드가 없습니다.", "Field is unavailable.")); return; }

        int fieldCap = (economy != null) ? economy.GetMaxFieldCount() : 8;
        if (fieldManager.girlList.Count >= fieldCap)
        {
            ShowReasonTemp(LocalizationManager.GetText("필드가 가득 찼습니다.", "The field is full."));
            return;
        }

        long gemCost = GetGemCostForLevel(level);
        if (premiumCurrency == null)
        {
            ShowReasonTemp(LocalizationManager.GetText("보석 정보를 불러오지 못했습니다.", "Could not load gem data."));
            return;
        }

        if (!premiumCurrency.TrySpendGems(gemCost))
        {
            long have = premiumCurrency.GetGems();
            if (insufficientFundsPanel != null)
            {
                insufficientFundsPanel.ShowGemShortage(gemCost, have);
                ShowReasonTemp(LocalizationManager.GetText("보석이 부족합니다.", "Not enough gems."));
            }
            else
            {
                ShowReasonTemp(LocalizationManager.GetText($"보석이 부족합니다.\n{GemAdHint}", "Not enough gems.\nWatch an ad to earn 5 Gems."));
            }
            return;
        }

        fieldManager.ManualSpawnGirl(level, Vector3.zero);
        economy?.RecordGemSummonPurchase(level); // 보석 소환 횟수 기록

        if (cells.TryGetValue(level, out var cell) && cell != null)
        {
            long newGemCost = GetGemCostForLevel(level);
            cell.UpdateGemCost(newGemCost, premiumCurrency.GetGems() >= newGemCost);
        }

        HideReasonImmediate();
        RefreshCostsOnly();
    }
}
