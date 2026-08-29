using System;
using System.Collections.Generic;

/// <summary>One reward calculation shared by the preview, confirmation and payout.</summary>
public readonly struct PrestigeReward
{
    public int FinalPoints { get; }
    public int FieldPoints { get; }
    public int UpgradePoints { get; }
    public int BasePoints { get; }
    public double Multiplier { get; }
    public int TotalPoints { get; }

    public PrestigeReward(int stacks, IEnumerable<int> levels, int topLevel,
        int consumedUpgradeLevels, double multiplier)
    {
        double n = Math.Max(0, stacks);
        FinalPoints = ClampPoints(n * 2500.0 + 1000.0 * (n - 1.0) * n / 2.0);
        double field = 0;
        if (levels != null)
            foreach (int level in levels)
                if (level > 0 && level < topLevel)
                    field += Math.Ceiling(2500.0 / Math.Pow(2.0, topLevel - level));
        FieldPoints = ClampPoints(field);
        UpgradePoints = ClampPoints(Math.Max(0, consumedUpgradeLevels) * 100.0);
        BasePoints = ClampPoints((double)FinalPoints + FieldPoints + UpgradePoints);
        Multiplier = double.IsNaN(multiplier) ? 1.0 : Math.Max(1.0, multiplier);
        TotalPoints = ClampPoints(BasePoints * Multiplier);
    }

    public static int ClampPoints(double value)
    {
        if (double.IsNaN(value) || value <= 0) return 0;
        if (value >= int.MaxValue) return int.MaxValue;
        return (int)Math.Ceiling(value);
    }
}
