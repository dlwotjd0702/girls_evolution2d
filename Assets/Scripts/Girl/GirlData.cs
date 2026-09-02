public class GirlData
{
    static readonly string[] EnglishNames =
    {
        "",
        "Rookie Ninja", "Little Ninja", "Scroll Ninja", "Ponytail Ninja", "Green Scarf Ninja",
        "Blade Ninja", "Twin-Tail Ninja", "Silk Ninja", "Black Scroll Ninja", "Venom Ninja",
        "Smoke Ninja", "Energetic Ninja", "Crimson Ninja", "Dual-Blade Ninja", "Longsword Ninja",
        "Twin-Dagger Ninja", "Blue Marine Ninja", "Master Ninja", "Jewel Ninja", "Orb Ninja",
        "Red Lotus Ninja", "Glow Armor Ninja", "Platinum Ninja", "Black-Purple Ninja", "Mythic Angel Ninja"
    };

    public int id { get; set; }
    public string name { get; set; }
    public int level { get; set; }
    public double incomePerSec { get; set; }
    public int mergeCount { get; set; }
    public string spriteName { get; set; }
    public string unlockDesc { get; set; }
    public int initialDirection { get; set; } = 1; // 1=왼쪽, -1=오른쪽

    public string LocalizedName => LocalizationManager.GetText(name, EnglishName(level));

    public static string EnglishName(int characterLevel)
    {
        return characterLevel > 0 && characterLevel < EnglishNames.Length
            ? EnglishNames[characterLevel]
            : $"Ninja Lv.{characterLevel}";
    }
}
