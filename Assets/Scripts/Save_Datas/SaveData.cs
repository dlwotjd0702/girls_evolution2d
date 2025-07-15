using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public double gold;
    public int gem;
    public int prestigePoint;
    public int totalPrestigeCount;
    public List<GirlSaveInfo> girls = new List<GirlSaveInfo>();
    // 필요에 따라 필드 확장
    // public List<InventoryItem> inventory = new List<InventoryItem>();
    // public List<AchievementSaveData> achievements = new List<AchievementSaveData>();
    // public Dictionary<string, int> upgrades = new Dictionary<string, int>();
    public int levelUpgrade;
    public int incomeUpgrade;
    public int autoIncomeUpgrade;
    public int clickIncomeUpgrade;
    public int spawnUpgrade;
    public int mergeBonusUpgrade;
    public int maxGirlCountUpgrade;
    
    public string savedAt;

    public void SetSaveTime()
    {
        savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

[Serializable]
public class GirlSaveInfo
{
    public int level;
    public float posX, posY; // 2D 연출, Z축 고정 또는 미포함

    public GirlSaveInfo(int level, float x, float y)
    {
        this.level = level;
        posX = x;
        posY = y;
    }
}