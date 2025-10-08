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
    [SerializeField] private TextMeshProUGUI reasonLabel;   // 공용 사유 라벨(캔버스)

    [Header("Cost Base")]
    [SerializeField] private double baseCost = 10; // 레벨1 기준 기본가

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
        // 가능하면 자동 DI
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
            RefreshCostsOnly(); // 버튼은 항상 활성 → 비용 라벨만 갱신
        }
    }

    void HandleMaxLevelChanged(int newMax) => Rebuild();

    void TryBuildIfReady()
    {
        if (dataManager != null && dataManager.IsLoaded && content && cellPrefab)
            Rebuild();
    }

    void Rebuild()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);
        cells.Clear();

        // ❗ fieldManager가 null이어도 최소 Lv1 셀은 뜨게 한다.
        int currentMax =
            fieldManager != null ? Mathf.Max(1, fieldManager.CurrentMaxLevel)
                                 : 3; // fallback(최소 1레벨 보여주기 위함)

        // “최대레벨-2까지” 보여주되, 최소 1은 보이게
        int maxSummonable = Mathf.Clamp(currentMax - 2, 1, TierRules.MaxLevel - 2);

        for (int level = 1; level <= maxSummonable; level++)
        {
            var data = dataManager.GetDataByLevel(level);
            if (data == null) continue;

            var go   = Instantiate(cellPrefab, content);
            var cell = go.GetComponent<SummonCell>();
            if (!cell)
            {
                Debug.LogError("[SummonPanel] SummonCell 컴포넌트가 프리팹에 없습니다.");
                continue;
            }

            // (선택) 공용 라벨 주입
            cell.reasonLabel = reasonLabel;

            Sprite icon    = spriteLoader ? spriteLoader.GetSpriteForData(data, preferLD: false) : null;
            double curCost = CurrentCost(level);

            int lv = level; // 캡처 안전
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
        // EconomyManager에 동적 가격(구매 누적) 로직이 있는 경우 사용
        if (economy != null && economy.TryGetComponent(out EconomyManager e))
        {
            // EconomyManager에 GetSummonCost(level, baseCost) 구현돼 있으면 사용
            var mi = typeof(EconomyManager).GetMethod("GetSummonCost");
            if (mi != null) return (double)mi.Invoke(e, new object[] { level, baseCost });
        }
        // 없으면 레벨 성장만(기본 동작)
        return baseCost * Math.Pow(1.15, Math.Max(0, level - 1));
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
        double gold = (economy != null) ? economy.GetGold() : double.MaxValue;

        if (gold < cost || (economy != null && !economy.SpendGold(cost)))
        {
            ShowReasonTemp("골드가 부족합니다.");
            return;
        }

        // 성공: 실제 소환
        fieldManager.ManualSpawnGirl(level, Vector3.zero);

        // EconomyManager에 구매 카운트가 있다면 증가 → 다음 가격 상승
        var miRecord = typeof(EconomyManager).GetMethod("RecordSummonPurchase");
        if (economy != null && miRecord != null) miRecord.Invoke(economy, new object[] { level });

        // 해당 셀 비용 라벨 즉시 갱신
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

    IEnumerator HideReasonAfter(float sec)
    {
        yield return new WaitForSecondsRealtime(sec);
        HideReasonImmediate();
    }

    void HideReasonImmediate()
    {
        if (!reasonLabel) return;
        if (_reasonRoutine != null) { StopCoroutine(_reasonRoutine); _reasonRoutine = null; }
        reasonLabel.text = string.Empty;
        reasonLabel.gameObject.SetActive(false);
    }

    // 버튼은 항상 활성 → 주기적으로 가격 라벨만 업데이트
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
