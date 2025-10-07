using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class EconomyManager : MonoBehaviour, ISaveable
{
    [Header("Refs")]
    [SerializeField] private GirlFieldManager fieldManager; // GameSystem에서 주입
    [SerializeField] private GirlMergeManager mergeManager; // (선택) 자동합성 트리거

    // ───────────────── Currency ─────────────────
    [Header("Gold")]
    [SerializeField] private double gold = 0;
    public event Action<double> onGoldChanged;
    public double GetGold() => gold;
    public void SetGold(double value) { gold = Math.Max(0, value); onGoldChanged?.Invoke(gold); }
    public void AddGold(double amount) { if (amount <= 0) return; gold += amount; onGoldChanged?.Invoke(gold); }
    public bool SpendGold(double amount)
    {
        if (amount <= 0) return true;
        if (gold < amount) return false;
        gold -= amount; onGoldChanged?.Invoke(gold); return true;
    }

    // ───────────────── Click / Idle ─────────────────
    [Header("Click / Idle")]
    [SerializeField] private bool handleGlobalClick = false; // 빈 화면 클릭만 처리(캐릭 클릭은 GirlCharacter에서)
    [SerializeField] private float clickGoldBase = 1f;       // 클릭당 기본 수익
    [SerializeField] private float idleTickInterval = 1f;    // 초당 틱
    private float _idleTimer;

    void Update()
    {
        if (handleGlobalClick && Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            AddClickGold();

        _idleTimer += Time.deltaTime;
        if (_idleTimer >= idleTickInterval)
        {
            _idleTimer -= idleTickInterval;
            TickIdleIncome();
        }
    }

    bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }

    public void AddClickGold()
    {
        double g = clickGoldBase * GetClickIncomeMultiplier();
        if (g > 0) AddGold(g);
    }

    public void TickIdleIncome()
    {
        if (!fieldManager) return;
        double total = 0;
        var list = fieldManager.girlList;
        for (int i = 0; i < list.Count; i++)
        {
            var g = list[i];
            if (!g) continue;
            total += g.GetIncome();
        }
        if (total > 0) AddGold(total /* * GetIdleIncomeMultiplier() */);
    }

    // ───────────────── Upgrades ─────────────────
    [Header("Upgrades")]
    public int clickBonusUpgrade;
    public int maxFieldCountUpgrade;
    public int manualSpawnMaxUpgrade;
    public int manualSpawnSpeedUpgrade;
    public int autoMergeUpgrade;
    public int autoSpawnUpgrade;

    // 기존 시그니처들 유지
    public double GetClickIncomeMultiplier() => 1.0 + clickBonusUpgrade * 1.0;
    public int    GetMaxFieldCount()         => 8 + maxFieldCountUpgrade;
    public int    GetMaxManualSpawnCount()   => 3 + manualSpawnMaxUpgrade;
    public float  GetManualSpawnInterval()   => Mathf.Max(2f, 10f * Mathf.Pow(0.85f, manualSpawnSpeedUpgrade));
    public bool   IsAutoMergeActive()        => autoMergeUpgrade > 0;
    public float  GetAutoMergeSpeed()        => 1f + 0.5f * autoMergeUpgrade;
    public float  GetAutoSpawnInterval()     => Mathf.Max(1f, 8f * Mathf.Pow(0.85f, autoSpawnUpgrade));

    public double GetUpgradeCost(int currentLevel) => Mathf.RoundToInt(25 * Mathf.Pow(1.7f, currentLevel));
    bool TryUpgrade(ref int field, int max = int.MaxValue)
    {
        if (field >= max) return false;
        double cost = GetUpgradeCost(field);
        if (!SpendGold(cost)) return false;
        field++; return true;
    }
    // 필요 시 호출용 샘플
    public bool UpgradeClickBonus()       => TryUpgrade(ref clickBonusUpgrade, 10);
    public bool UpgradeMaxFieldCount()    => TryUpgrade(ref maxFieldCountUpgrade, 8);
    public bool UpgradeManualSpawnMax()   => TryUpgrade(ref manualSpawnMaxUpgrade, 7);
    public bool UpgradeManualSpawnSpeed() => TryUpgrade(ref manualSpawnSpeedUpgrade, 8);
    public bool UpgradeAutoMerge()        => TryUpgrade(ref autoMergeUpgrade, 1);
    public bool UpgradeAutoSpawn()        => TryUpgrade(ref autoSpawnUpgrade, 8);

    // ───────────────── Prestige ─────────────────
    [Header("Prestige")]
    public int prestigePoint = 0;
    public int totalPrestigeCount = 0;
    public int topLevel = TierRules.MaxLevel;

    public Action onPrestigeAvailable;
    bool _prestigeAvailable;

    void LateUpdate()
    {
        if (!fieldManager) return;
        bool found = false;
        var list = fieldManager.girlList;
        for (int i = 0; i < list.Count; i++)
        {
            var g = list[i];
            if (g && g.Level >= topLevel) { found = true; break; }
        }
        if (found != _prestigeAvailable)
        {
            _prestigeAvailable = found;
            if (_prestigeAvailable) onPrestigeAvailable?.Invoke();
        }
    }

    public void DoPrestige()
    {
        if (!fieldManager) return;

        int reward = 0;
        var snap = new List<GirlCharacter>(fieldManager.girlList);
        for (int i = 0; i < snap.Count; i++)
            if (snap[i] && snap[i].Level >= topLevel) reward++;

        prestigePoint += reward;
        totalPrestigeCount++;

        foreach (var g in snap) if (g) fieldManager.RemoveGirl(g);
        fieldManager.girlList.Clear();
        SetGold(0);
        _prestigeAvailable = false;
    }

    public int GetPrestigePoint()      => prestigePoint;
    public int GetTotalPrestigeCount() => totalPrestigeCount;

    // ───────────────── Save/Load ─────────────────
    public void ApplyLoadedData(SaveData data)
    {
        if (data == null) return;

        gold = Math.Max(0, data.gold); onGoldChanged?.Invoke(gold);

        clickBonusUpgrade       = data.clickBonusUpgrade;
        maxFieldCountUpgrade    = data.maxFieldCountUpgrade;
        manualSpawnMaxUpgrade   = data.manualSpawnMaxUpgrade;
        manualSpawnSpeedUpgrade = data.manualSpawnSpeedUpgrade;
        autoMergeUpgrade        = data.autoMergeUpgrade;
        autoSpawnUpgrade        = data.autoSpawnUpgrade;

        prestigePoint      = data.prestigePoint;
        totalPrestigeCount = data.totalPrestigeCount;
    }

    public void CollectSaveData(SaveData data)
    {
        data.gold = gold;

        data.clickBonusUpgrade       = clickBonusUpgrade;
        data.maxFieldCountUpgrade    = maxFieldCountUpgrade;
        data.manualSpawnMaxUpgrade   = manualSpawnMaxUpgrade;
        data.manualSpawnSpeedUpgrade = manualSpawnSpeedUpgrade;
        data.autoMergeUpgrade        = autoMergeUpgrade;
        data.autoSpawnUpgrade        = autoSpawnUpgrade;

        data.prestigePoint      = prestigePoint;
        data.totalPrestigeCount = totalPrestigeCount;
    }
}
