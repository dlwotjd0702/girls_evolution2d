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

    [Header("UI")]
    [SerializeField] private RectTransform   content;       // ScrollView Content
    [SerializeField] private GameObject      cellPrefab;    // SummonCell 프리팹(GameObject)
    [SerializeField] private TextMeshProUGUI reasonLabel;   // 공용 사유 라벨

    [Header("Refresh")]
    [SerializeField] private float interactableRefreshInterval = 0.25f;

    [Header("Reason Popup")]
    [SerializeField] private float reasonShowSeconds = 1.15f;

    private readonly Dictionary<int, SummonCell> cells = new();
    private float _t;
    private Coroutine _reasonRoutine;
    private Action<int> _maxLevelChangedHandler;

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
        int maxSummonable = Mathf.Clamp(currentMax - 2, 1, TierRules.MaxLevel - 2);

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
            cell.Setup(
                level: lv,
                nameText: $"Lv.{data.level}  {data.name}",
                cost: curCost,
                icon: icon,
                onClick: () => OnClickSummon(lv)
            );

            cells[level] = cell;
        }
        HideReasonImmediate();
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
        if (fieldManager == null) { ShowReasonTemp("필드가 없습니다."); return; }

        int fieldCap = (economy != null) ? economy.GetMaxFieldCount() : 8;
        if (fieldManager.girlList.Count >= fieldCap)
        {
            ShowReasonTemp("필드가 가득 찼습니다.");
            return;
        }

        double cost = CurrentCost(level);
        if (economy == null || !economy.SpendGold(cost))
        {
            ShowReasonTemp("골드가 부족합니다.");
            return;
        }

        fieldManager.ManualSpawnGirl(level, Vector3.zero);
        economy?.RecordSummonPurchase(level);

        if (cells.TryGetValue(level, out var cell) && cell != null)
            cell.UpdateCost(CurrentCost(level));

        HideReasonImmediate();
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
        foreach (var kv in cells)
        {
            var cell = kv.Value;
            if (!cell) continue;
            cell.UpdateCost(CurrentCost(kv.Key));
        }
    }
}
