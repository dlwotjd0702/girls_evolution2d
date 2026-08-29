using System;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class LegacyRankManager : MonoBehaviour, ISaveable
{
    public static LegacyRankManager Instance { get; private set; }

    [SerializeField] private int legacyXp = 0;  // SaveData에 없으면 0 유지(리플렉션 저장)
    public int LegacyXp => Mathf.Max(0, legacyXp);
    public int LegacyLevel => XpToLevel(legacyXp);

    public event Action OnLegacyChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // 환생/광고/과금에서 호출
    public void AddXp(int amount)
    {
        if (amount <= 0) return;
        legacyXp = PrestigeReward.ClampPoints((double)LegacyXp + amount);
        OnLegacyChanged?.Invoke();
    }

    // 누적 XP → 레벨 (L*(L+1)/2)
    public static int XpToLevel(int xp)
    {
        if (xp <= 0) return 0;
        double lv = (Math.Sqrt(1.0 + 8.0 * xp) - 1.0) * 0.5;
        return Mathf.Max(0, (int)Math.Floor(lv));
    }

    // ── 자동 보너스(구간 누적) ──
    public double GetIncomeMultiplier()
    {
        int L = LegacyLevel; double b=0;
        if (L >= 1)  b += 0.05;
        if (L >= 7)  b += 0.05;
        if (L >= 15) b += 0.10;
        if (L >= 25) b += 0.10; // +30%
        return 1.0 + b;
    }
    public float GetTwoStepMergeChance()
    {
        int L = LegacyLevel; float p=0;
        if (L >= 3)  p += 0.02f;
        if (L >= 9)  p += 0.02f;
        if (L >= 15) p += 0.03f;
        if (L >= 18) p += 0.03f; // +10%p cap
        return Mathf.Clamp(p, 0f, 0.10f);
    }
    public double GetStartGoldMultiplier()
    {
        int L = LegacyLevel; double m=0;
        if (L >= 2)  m += 0.10;
        if (L >= 8)  m += 0.10;
        if (L >= 12) m += 0.20; // +40%
        return 1.0 + m;
    }

    // ── ISaveable (SaveData에 필드가 없으면 무시) ──
    public void CollectSaveData(SaveData d) => TrySetInt(d, "legacyXp", legacyXp);
    public void ApplyLoadedData(SaveData d)
    {
        legacyXp = TryGetInt(d, "legacyXp", legacyXp);
        OnLegacyChanged?.Invoke();
    }

    // ── 리플렉션 유틸 ──
    static void TrySetInt(object obj, string name, int value)
    {
        var f = obj.GetType().GetField(name, BindingFlags.Public|BindingFlags.Instance);
        if (f != null && f.FieldType == typeof(int)) f.SetValue(obj, value);
    }
    static int TryGetInt(object obj, string name, int fallback)
    {
        var f = obj.GetType().GetField(name, BindingFlags.Public|BindingFlags.Instance);
        if (f != null && f.FieldType == typeof(int)) return (int)f.GetValue(obj);
        return fallback;
    }
}
