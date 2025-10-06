using System;
using UnityEngine;

public class TierManager : MonoBehaviour, ISaveable
{
    // 0=1층, 1=2층, 2=3층, 3=마지막층
    [field: SerializeField, Range(0,3)]
    public int CurrentTierIndex { get; private set; } = 0;

    // 0층은 기본 오픈
    public readonly bool[] Unlocked = new bool[4] { true, false, false, false };

    public event Action<int> OnTierChanged;
    public event Action<int> OnTierUnlocked;

    // ─────────────────────────
    // 계층 전환 / 언락
    // ─────────────────────────
    public void SwitchTo(int tierIndex)
    {
        if ((uint)tierIndex > 3u) return;
        if (!Unlocked[tierIndex]) return;
        if (CurrentTierIndex == tierIndex) return;

        CurrentTierIndex = tierIndex;
        OnTierChanged?.Invoke(CurrentTierIndex);
    }

    // 레벨 기반 자동 언락(9→1층, 17→2층, 25→3층)
    public bool TryUnlockByLevel(int level)
    {
        int t = TierRules.TierIndexFromLevel(level);
        bool changed = false;
        for (int i = 0; i <= t; i++)
        {
            if (!Unlocked[i])
            {
                Unlocked[i] = true;
                OnTierUnlocked?.Invoke(i);
                changed = true;
            }
        }
        return changed;
    }

    // ─────────────────────────
    // 규칙 기반 "상층" 계산/이동
    // 0(1층)→ 1(2층) 언락 시 1, 아니면 0
    // 1(2층)→ 2(3층) 언락 시 2, 아니면 0
    // 2(3층)→ 3(마지막층) 언락 시 3, 아니면 0
    // 3(마지막층)→ 0(1층)
    // ─────────────────────────
    public bool IsUnlocked(int tier) => (uint)tier < 4u && Unlocked[tier];

    public int ComputeNextTierByRule()
    {
        switch (CurrentTierIndex)
        {
            case 0: return IsUnlocked(1) ? 1 : 0;
            case 1: return IsUnlocked(2) ? 2 : 0;
            case 2: return IsUnlocked(3) ? 3 : 0;
            case 3: return 0;
            default: return 0;
        }
    }

    public void GoNextTierByRule()
    {
        int next = ComputeNextTierByRule();
        SwitchTo(next);
    }

    // ─────────────────────────
    // 저장/복원
    // ─────────────────────────
    public void CollectSaveData(SaveData data)
    {
        data.currentTierIndex = CurrentTierIndex;
        data.unlockedTierMask =
            (Unlocked[0] ? 1 : 0) |
            (Unlocked[1] ? 2 : 0) |
            (Unlocked[2] ? 4 : 0) |
            (Unlocked[3] ? 8 : 0);
    }

    public void ApplyLoadedData(SaveData data)
    {
        CurrentTierIndex = Mathf.Clamp(data.currentTierIndex, 0, 3);

        for (int i = 0; i < 4; i++)
            Unlocked[i] = (data.unlockedTierMask & (1 << i)) != 0;

        Unlocked[0] = true; // 안전장치
        OnTierChanged?.Invoke(CurrentTierIndex);
    }
}
