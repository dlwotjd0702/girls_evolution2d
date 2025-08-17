using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int dataVersion = 1;

    // 경제
    public double gold;

    // 티어
    public int currentTierIndex;
    public int unlockedTierMask;

    // 도감: 1~25 레벨 첫 발견 여부 (bit = level-1)
    public int discoveredMask;

    // 필드의 미소녀 상태 (위치 저장 X → 레벨만 저장)
    public List<GirlSaveInfo> girls = new List<GirlSaveInfo>();

    // Prestige 호환
    public int prestigePoint;
    public int totalPrestigeCount;

    // 업그레이드 상태(필요 시 각 매니저에서 채워 넣기)
    public int manualSpawnMaxUpgrade;
    public int manualSpawnSpeedUpgrade;
    public int autoMergeUpgrade;
    public int autoSpawnUpgrade;
    public int maxFieldCountUpgrade;
    public int clickBonusUpgrade;

    // 메타
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

    public GirlSaveInfo(int level)
    {
        this.level = level;
    }
}