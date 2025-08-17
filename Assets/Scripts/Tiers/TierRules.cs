public static class TierRules
{
    public const int MaxLevel = 25;

    /// 0층: 1~8, 1층: 9~16, 2층: 17~24, 3층: 25
    public static int TierIndexFromLevel(int level)
    {
        if (level <= 8) return 0;
        if (level <= 16) return 1;
        if (level <= 24) return 2;
        return 3; // 25
    }
}