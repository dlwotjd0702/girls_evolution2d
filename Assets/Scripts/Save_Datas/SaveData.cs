using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int dataVersion = 6;
    public int codexDiscoveredMask;
    public List<string> ownedSkinIds;
    public List<EquippedSkinSave> equippedSkins;
    public string activeMythicFormId;
    public List<string> discoveredMythicForms;
    public long incomeBoostUntilUtc;

    public bool autoMergeOn;
    public bool autoSpawnOn;

    // Economy
    public double gold;

    // Premium Currency
    public long gems; // 보석
    public long adsRemoved; // 광고 제거 플래그 (1=true, 0=false)

    // Tier
    public int currentTierIndex;
    public int unlockedTierMask;

    // 발견/필드
    public int discoveredMask;
    public List<GirlSaveInfo> girls = new List<GirlSaveInfo>();
    public int maxLevelReached = 1;
    public int level25UpgradeLevel = 0;

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

    // v3: permanent purchases; indexes are EconomyManager.UpgradeKind values.
    // null distinguishes old saves without currency provenance.
    public int[] permanentUpgradeLevels;

    // 레벨별 소환 누적 구매수
    public int[] summonPurchaseCounts = new int[25];
    public int[] gemSummonPurchaseCounts = new int[25]; // 보석 소환 횟수

    // ── 추가: 계승/상점 (기본 0 → 구세이브 호환) ──
    public int legacyXp;                    // v6+: 환생 1회당 1 계승 진행도(최대 25)
    public int prestigeShopIncomeLv;        // 환생 상점: 수익 배수
    public int prestigeShopTwoStepLv;       // 환생 상점: +2단 확률
    public int prestigeShopStartGoldLv;     // 환생 상점: 시작 자금
    public int prestigeShopPrestigeGainLv;  // 환생 상점: 환생 포인트 획득

    // 환생 상점 Plus (일반 상점 영구 보정)
    public int ppManualSpawnMaxLv;
    public int ppManualSpawnSpeedLv;
    public int ppAutoMergeLv;
    public int ppAutoSpawnLv;
    public int ppMaxFieldCountLv;
    public int ppClickBonusLv;
    public int ppOfflineRewardLv;
    public int ppOfflineMaxTimeLv;

    // 메타
    public string savedAt;
    public long savedAtUtcUnixSeconds;
    public void SetSaveTime()
    {
        var now = DateTimeOffset.UtcNow;
        savedAtUtcUnixSeconds = now.ToUnixTimeSeconds();
        // 기존 버전 UI와 구세이브 호환을 위해 사람이 읽는 필드도 유지한다.
        savedAt = now.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
    }
    
    // 언어 설정 (0=시스템 언어 미설정, 1=한국어, 2=영어)
    // 0이면 최초 실행으로 간주하여 시스템 언어 사용
    public int languageCode = 0;
    
    // 쿠폰 시스템
    public List<string> usedCoupons = new List<string>(); // 사용한 쿠폰 코드 목록
    
    // ── 플레이 디버그 정보 (밸런스 패치용 통계) ──
    public double totalPlayTimeSeconds = 0.0; // 총 플레이타임 (초)
    public string firstPlayTime = ""; // 최초 플레이 시간
    public string lastPlayTime = ""; // 마지막 플레이 시간
    public int level25ReachedCount = 0; // 레벨 25 달성 횟수
    public List<string> level25ReachedTimes = new List<string>(); // 레벨 25 달성 시간 목록
    public long totalClicks = 0; // 총 클릭 횟수
    public long totalMerges = 0; // 총 합성 횟수
    public long totalSpawns = 0; // 총 소환 횟수
    public double totalGoldEarned = 0.0; // 총 획득 골드
    public double totalGoldSpent = 0.0; // 총 소비 골드
    
    // ── 튜토리얼 ──
    public bool tutorialCompleted = false; // 튜토리얼 완료 여부
}

[Serializable]
public class GirlSaveInfo
{
    public int level;
    public GirlSaveInfo(int level){ this.level = level; }
}

[Serializable]
public class EquippedSkinSave
{
    public int level;
    public string skinId;
}
