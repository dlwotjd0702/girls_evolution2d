using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    // 화폐류
    public double gold;
    public int gem;
    public int prestigePoint;
    public int totalPrestigeCount;

    // 필드의 미소녀 상태
    public List<GirlSaveInfo> girls = new List<GirlSaveInfo>();

    // 기타 확장 필드
    public string savedAt;
    public int manualSpawnMaxUpgrade;
    public int manualSpawnSpeedUpgrade;
    public int autoMergeUpgrade;
    public int autoSpawnUpgrade;
    public int maxFieldCountUpgrade;
    public int clickBonusUpgrade;

    // 저장 시각 기록용
    public void SetSaveTime()
    {
        savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

[Serializable]
public class GirlSaveInfo
{
    public int level;
    public float posX, posY;

    public GirlSaveInfo(int level, float x, float y)
    {
        this.level = level;
        posX = x;
        posY = y;
    }
}