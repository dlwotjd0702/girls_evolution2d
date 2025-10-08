using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SummonPanelController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GirlDataManager dataManager;      // *MonoBehaviour 아님 — null 체크는 (dataManager == null) 형태만
    [SerializeField] private GirlFieldManager fieldManager;
    [SerializeField] private GirlSpriteAddressableLoader spriteLoader;
    [SerializeField] private EconomyManager economy;

    [Header("UI")]
    [SerializeField] private RectTransform content;            // ScrollView Content
    [SerializeField] private GameObject cellPrefab;            // 프리팹은 GameObject로
    [SerializeField] private TextMeshProUGUI reasonLabel;      // 외부 캔버스의 공용 사유 라벨 주입

    [Header("Cost Formula")]
    [SerializeField] private double baseCost = 10;
    [SerializeField] private double growth = 1.15;

    [Header("Refresh")]
    [SerializeField] private float interactableRefreshInterval = 0.25f;

    private readonly Dictionary<int, SummonCell> cells = new();
    private float _t;

    // 이벤트 핸들러 저장(람다 해제용)
    private Action<int> _maxLevelChangedHandler;
    private Action<double> _goldChangedHandler;

    void OnEnable()
    {
        TryBuildIfReady();

        if (fieldManager != null)
        {
            _maxLevelChangedHandler = HandleMaxLevelChanged;
            fieldManager.OnMaxLevelChanged -= _maxLevelChangedHandler;
            fieldManager.OnMaxLevelChanged += _maxLevelChangedHandler;
        }
        if (economy != null)
        {
            _goldChangedHandler = _ => RefreshInteractables();
            economy.OnGoldChanged -= _goldChangedHandler;
            economy.OnGoldChanged += _goldChangedHandler;
        }
    }

    void OnDisable()
    {
        if (fieldManager != null && _maxLevelChangedHandler != null)
            fieldManager.OnMaxLevelChanged -= _maxLevelChangedHandler;
        if (economy != null && _goldChangedHandler != null)
            economy.OnGoldChanged -= _goldChangedHandler;
    }

    void Update()
    {
        if (cells.Count == 0) TryBuildIfReady();

        _t += Time.unscaledDeltaTime;
        if (_t >= interactableRefreshInterval)
        {
            _t = 0f;
            RefreshInteractables();
        }
    }

    void HandleMaxLevelChanged(int newMax) => Rebuild();

    void TryBuildIfReady()
    {
        if (dataManager != null && dataManager.IsLoaded && content != null && cellPrefab != null)
            Rebuild();
    }

    void Rebuild()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);
        cells.Clear();

        int currentMax = (fieldManager != null) ? fieldManager.CurrentMaxLevel : 1;
        int maxSummonable = Mathf.Clamp(currentMax - 2, 1, TierRules.MaxLevel - 2);
        if (maxSummonable < 1) return;

        for (int level = 1; level <= maxSummonable; level++)
        {
            var data = dataManager.GetDataByLevel(level);
            if (data == null) continue;

            var go = Instantiate(cellPrefab, content);
            var cell = go.GetComponent<SummonCell>();
            if (cell == null)
            {
                Debug.LogError("[SummonPanel] SummonCell 컴포넌트가 프리팹에 없습니다.");
                continue;
            }

            // 공용 reasonLabel 주입
            cell.reasonLabel = reasonLabel;

            Sprite icon = spriteLoader ? spriteLoader.GetSpriteForData(data, preferLD: false) : null;
            double cost = EvaluateCost(level);

            cell.Setup(
                level: level,
                nameText: $"Lv.{data.level}  {data.name}",
                cost: cost,
                icon: icon,
                onClick: () => OnClickSummon(level, cost)
            );

            cells[level] = cell;
        }
        RefreshInteractables();
    }

    void RefreshInteractables()
    {
        int fieldCount = (fieldManager != null) ? fieldManager.girlList.Count : 0;
        int fieldCap   = (economy != null) ? economy.GetMaxFieldCount() : 8;
        double gold    = (economy != null) ? economy.GetGold() : 0;

        foreach (var kv in cells)
        {
            int level = kv.Key;
            var cell  = kv.Value;
            if (!cell) continue;

            double cost = EvaluateCost(level);
            bool hasSpace = fieldCount < fieldCap;
            bool canAfford = gold >= cost;

            cell.SetInteractable(hasSpace && canAfford);
            cell.SetReason(!hasSpace, !canAfford);
        }
    }

    void OnClickSummon(int level, double cost)
    {
        if (fieldManager == null) return;
        int fieldCap = (economy != null) ? economy.GetMaxFieldCount() : 8;
        if (fieldManager.girlList.Count >= fieldCap) return;

        if (economy != null)
        {
            if (!economy.SpendGold(cost)) return;
        }
        else
        {
            Debug.LogWarning("[SummonPanel] EconomyManager 없음 — 비용 체크 없이 소환(개발용).");
        }

        fieldManager.ManualSpawnGirl(level, Vector3.zero);
        RefreshInteractables();
    }

    double EvaluateCost(int level)
    {
        double exp = level - 1;
        return baseCost * System.Math.Pow(growth, exp);
    }

    public static string FormatAbbrev(double v)
    {
        double av = System.Math.Abs(v);
        if (av >= 1e12) return $"{v / 1e12:0.#}T";
        if (av >= 1e9)  return $"{v / 1e9:0.#}B";
        if (av >= 1e6)  return $"{v / 1e6:0.#}M";
        if (av >= 1e3)  return $"{v / 1e3:0.#}K";
        return $"{v:0}";
    }
}
