using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    // 데이터 버전(역호환 유지)
    public int dataVersion = 2;

    // ───── 자동화 토글 상태 ─────
    public bool autoMergeOn;   // 오토합성 토글
    public bool autoSpawnOn;   // 오토소환 토글

    // ───── Economy ─────
    public double gold;

    // ───── Tier ─────
    public int currentTierIndex;   // 현재 층(0~3)
    public int unlockedTierMask;   // 언락된 층 비트마스크 (bit = tier)

    // ───── 도감/발견 ─────
    // 레벨 첫 발견 여부(1~25) bit = level-1
    public int discoveredMask;

    // 필드의 소녀 상태: 위치는 저장하지 않고 레벨만 저장
    public List<GirlSaveInfo> girls = new List<GirlSaveInfo>();

    // 현재 플레이에서 도달한 최대 레벨 (소환 패널 계산 등에서 사용)
    public int maxLevelReached = 1;

    // ───── Prestige ───── (기존 호환)
    public int prestigePoint;
    public int totalPrestigeCount;

    // ───── 업그레이드 상태 (기존 호환) ─────
    public int manualSpawnMaxUpgrade;
    public int manualSpawnSpeedUpgrade;
    public int autoMergeUpgrade;  // 오토합성 강화 레벨
    public int autoSpawnUpgrade;  // 오토소환 강화 레벨
    public int maxFieldCountUpgrade;
    public int clickBonusUpgrade;

    // ▼ 레벨별 소환 구매 누적 (소환 패널의 ‘영구 가격 상승’ 유지)
    // index = level-1, value = 해당 레벨에서의 누적 구매 횟수
    public int[] summonBuyCounts;

    // ───── 메타 ─────
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