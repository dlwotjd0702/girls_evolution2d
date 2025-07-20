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

    // 업그레이드 단계 (UpgradeManager와 완벽 일치)
    public int incomeUpgrade;
    public int autoIncomeUpgrade;
    public int clickIncomeUpgrade;
    public int spawnUpgrade;
    public int spawnCountUpgrade; // 추가: 한번에 생성 수 (UpgradeManager 변화 반영)

    // 기타 확장 필드
    public string savedAt;

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