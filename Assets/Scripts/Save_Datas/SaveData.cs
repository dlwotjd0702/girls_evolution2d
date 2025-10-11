using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    // 데이터 버전
    public int dataVersion = 3;

    // ───── 토글 (자동) ─────
    public bool autoMergeOn;   // 오토합성 토글
    public bool autoSpawnOn;   // 오토소환 토글

    // ───── Economy ─────
    public double gold;

    // ───── Tier ─────
    public int currentTierIndex;   // 현재 층(0~3)
    public int unlockedTierMask;   // 언락 비트마스크

    // ───── 도감/발견 ─────
    public int discoveredMask;
    public List<GirlSaveInfo> girls = new List<GirlSaveInfo>();
    public int maxLevelReached = 1;

    // ───── Prestige ─────
    public int prestigePoint;
    public int totalPrestigeCount;

    // ───── 업그레이드(골드 기반) ─────
    public int manualSpawnMaxUpgrade;
    public int manualSpawnSpeedUpgrade;
    public int autoMergeUpgrade;      // 0이면 미구매(오토항목은 0→1이 '구매')
    public int autoSpawnUpgrade;      // 0이면 미구매
    public int maxFieldCountUpgrade;
    public int clickBonusUpgrade;

    // 신규: 오프라인 보상/상한
    public int offlineRewardUpgrade;  // 배율 상승
    public int offlineMaxTimeUpgrade; // 상한(시간) 증가

    // ───── 소환 가격 영구 인상용 누적 카운트 ─────
    // index: level-1 (0..24)
    public int[] summonPurchaseCounts = new int[25];

    // ───── 메타 ─────
    public string savedAt;
    public void SetSaveTime() => savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
}

[Serializable]
public class GirlSaveInfo
{
    public int level;
    public GirlSaveInfo(int level) { this.level = level; }
}