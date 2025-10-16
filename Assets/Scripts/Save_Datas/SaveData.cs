using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int dataVersion = 2;

    public bool autoMergeOn;
    public bool autoSpawnOn;

    // Economy
    public double gold;

    // Tier
    public int currentTierIndex;
    public int unlockedTierMask;

    // 발견/필드
    public int discoveredMask;
    public List<GirlSaveInfo> girls = new List<GirlSaveInfo>();
    public int maxLevelReached = 1;

    // Prestige (구호환)
    public int prestigePoint;
    public int totalPrestigeCount;

    // 업그레이드(현행/구호환)
    public int manualSpawnMaxUpgrade;
    public int manualSpawnSpeedUpgrade;
    public int autoMergeUpgrade;
    public int autoSpawnUpgrade;
    public int maxFieldCountUpgrade;
    public int clickBonusUpgrade;

    // 오프라인/메타
    public int offlineRewardUpgrade;
    public int offlineMaxTimeUpgrade;

    // 레벨별 소환 누적 구매수
    public int[] summonPurchaseCounts = new int[25];

    // ── 추가: 계승/상점 (기본 0 → 구세이브 호환) ──
    public int legacyXp;                    // 계승 경험치
    public int prestigeShopIncomeLv;        // 환생 상점: 수익 배수
    public int prestigeShopTwoStepLv;       // 환생 상점: +2단 확률
    public int prestigeShopStartGoldLv;     // 환생 상점: 시작 자금

    // 메타
    public string savedAt;
    public void SetSaveTime(){ savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); }
}

[Serializable]
public class GirlSaveInfo
{
    public int level;
    public GirlSaveInfo(int level){ this.level = level; }
}