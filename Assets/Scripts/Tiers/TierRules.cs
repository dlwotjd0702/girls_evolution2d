using UnityEngine;

public static class TierRules
{
    public const int LevelsPerTier = 8;   // 1~8, 9~16, 17~24
    public const int MaxLevel      = 25;  // 25: 최종/환생층

    public static int TierIndexFromLevel(int level)
        => (level >= MaxLevel) ? 3 : Mathf.Clamp((level - 1) / LevelsPerTier, 0, 2);

    public static int TierBaseLevel(int tierIndex)
        => (tierIndex == 3) ? MaxLevel : tierIndex * LevelsPerTier + 1;

    public static int TierTopLevel(int tierIndex)
        => (tierIndex == 3) ? MaxLevel : TierBaseLevel(tierIndex) + LevelsPerTier - 1;

    public static bool IsInTier(int level, int tierIndex)
        => TierIndexFromLevel(level) == tierIndex;
}