public class GirlData
{
    public int id { get; set; }
    public string name { get; set; }
    public int level { get; set; }
    public double incomePerSec { get; set; }
    public int mergeCount { get; set; }
    public string spriteName { get; set; }
    public string unlockDesc { get; set; }
    public int initialDirection { get; set; } = 1; // 1=왼쪽, -1=오른쪽
}