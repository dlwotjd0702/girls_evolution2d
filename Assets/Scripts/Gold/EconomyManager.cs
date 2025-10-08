using System;
using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    [Header("Gold")]
    [SerializeField] private double gold = 0;
    public event Action<double> OnGoldChanged;

    [Header("Idle Income")]
    [SerializeField] private bool idleEnabled = true;
    [SerializeField] private double idlePerSecondBase = 0;
    [SerializeField] private double idleMultiplier = 1.0;
    private float idleTimer = 0f;

    [Header("Click Income")]
    [SerializeField] private double clickBase = 1;
    [SerializeField] private double clickMultiplier = 1.0;

    [Header("Spawn/Field Config")]
    [SerializeField] private int manualSpawnMax = 3;
    [SerializeField] private float manualSpawnInterval = 10f;
    [SerializeField] private int fieldMaxCount = 8;

    [Header("Automation")]
    public int autoSpawnUpgrade = 0; // 0 = 비활성 (프로젝트에 없던 케이스 대비해 명시)
    [SerializeField] private float autoSpawnIntervalBase = 6f;
    [SerializeField] private float autoSpawnIntervalPerLevel = -0.6f; // 레벨당 감소
    [SerializeField] private bool autoMergeEnabled = false;

    public bool HasAutoSpawn => autoSpawnUpgrade > 0;

    public void AddGold(double amount)
    {
        gold += amount;
        OnGoldChanged?.Invoke(gold);
    }

    public bool SpendGold(double cost)
    {
        if (gold < cost) return false;
        gold -= cost;
        OnGoldChanged?.Invoke(gold);
        return true;
    }

    public double GetGold() => gold;
    public void SetGold(double v) { gold = Math.Max(0, v); OnGoldChanged?.Invoke(gold); }

    void Update()
    {
        if (!idleEnabled) return;
        idleTimer += Time.unscaledDeltaTime;
        if (idleTimer >= 1f)
        {
            idleTimer -= 1f;
            var amt = idlePerSecondBase * idleMultiplier;
            if (amt > 0) AddGold(amt);
        }
    }

    public void AddClickGold(double baseAmount = -1)
    {
        var amt = (baseAmount >= 0 ? baseAmount : clickBase) * clickMultiplier;
        if (amt > 0) AddGold(amt);
    }

    // ── API: GirlFieldManager가 사용 ──
    public int   GetMaxManualSpawnCount()  => manualSpawnMax;
    public float GetManualSpawnInterval()  => manualSpawnInterval;
    public int   GetMaxFieldCount()        => fieldMaxCount;

    public bool IsAutoMergeActive() => autoMergeEnabled;

    public float GetAutoSpawnInterval()
    {
        if (autoSpawnUpgrade <= 0) return float.MaxValue;
        var t = autoSpawnIntervalBase + autoSpawnIntervalPerLevel * (autoSpawnUpgrade - 1);
        return Mathf.Clamp(t, 1.0f, 999f);
    }

    // 선택: 외부에서 수치 조정용 세터
    public void SetIdle(double basePerSec, double mult = 1.0) { idlePerSecondBase = basePerSec; idleMultiplier = mult; }
    public void SetClick(double baseClick, double mult = 1.0) { clickBase = baseClick; clickMultiplier = mult; }
    public void SetSpawnConfig(int maxManual, float interval, int maxField) { manualSpawnMax = maxManual; manualSpawnInterval = interval; fieldMaxCount = maxField; }
    public void EnableAutoMerge(bool on) => autoMergeEnabled = on;
}
