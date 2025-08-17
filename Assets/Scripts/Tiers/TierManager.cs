using System;
using UnityEngine;

public class TierManager : MonoBehaviour, ISaveable
{
    [field: SerializeField, Range(0,3)]
    public int CurrentTierIndex { get; private set; } = 0;

    public readonly bool[] Unlocked = new bool[4] { true, false, false, false };

    public event Action<int> OnTierChanged;
    public event Action<int> OnTierUnlocked;

    public void SwitchTo(int tierIndex)
    {
        if ((uint)tierIndex > 3u) return;
        if (!Unlocked[tierIndex]) return;
        if (CurrentTierIndex == tierIndex) return;

        CurrentTierIndex = tierIndex;
        OnTierChanged?.Invoke(CurrentTierIndex);
    }

    public bool TryUnlockByLevel(int level)
    {
        var t = TierRules.TierIndexFromLevel(level); // ← 분리된 유틸 사용
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

        Unlocked[0] = true;
        OnTierChanged?.Invoke(CurrentTierIndex);
    }
}