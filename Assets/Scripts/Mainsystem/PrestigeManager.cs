// PrestigeManager.cs (FULL, SimpleUIPool 의존 제거 / SaveData v2 호환 / GameSystem 호환)
using System;
using System.Collections.Generic;
using UnityEngine;

public class PrestigeManager : MonoBehaviour, ISaveable
{
    [Header("Refs (GameSystem에서 주입)")]
    public GirlFieldManager girlFieldManager;
    public EconomyManager   economy;
    public TierManager      tierManager;

    [Header("Config")]
    public int topLevel = TierRules.MaxLevel; // 보통 25

    [Header("Runtime")]
    [SerializeField] private int prestigePoints = 0;       // 환생 포인트(세이브됨)
    [SerializeField] private int totalPrestigeCount = 0;   // 누적 환생 횟수(세이브됨)

    // 이후 확장용: 환생 경험치(지금은 런타임만, v3에서 저장 필드 추가 예정)
    [SerializeField] private int prestigeXP = 0;

    /// <summary>필드에 최종(25+) 등장 시 1회 알림.</summary>
    public event Action onPrestigeAvailable;
    /// <summary>포인트 증감 시 UI 갱신용.</summary>
    public event Action onPrestigePointsChanged;

    private bool _availableRaised = false;

    void Awake()
    {
        // GameSystem이 주입하므로 여기서는 널이면 찾아만 둠(호환 안전)
        if (!girlFieldManager) girlFieldManager = FindObjectOfType<GirlFieldManager>(true);
        if (!economy)          economy          = FindObjectOfType<EconomyManager>(true);
        if (!tierManager)      tierManager      = FindObjectOfType<TierManager>(true);

        if (topLevel <= 0) topLevel = TierRules.MaxLevel;
    }

    void Update()
    {
        // 최종(25+) 존재 여부 감시 → 최초 1회 알림
        bool hasFinal = HasAnyFinalGirl();
        if (hasFinal && !_availableRaised)
        {
            _availableRaised = true;
            onPrestigeAvailable?.Invoke();
        }
        else if (!hasFinal)
        {
            _availableRaised = false;
        }
    }

    // === Public API ===========================================================
    public int  GetPrestigePoints()     => prestigePoints;
    public int  GetTotalPrestigeCount() => totalPrestigeCount;
    public int  GetPrestigeXP()         => prestigeXP; // 지금은 런타임만 사용

    public bool TryConsumePrestigePoints(int amount)
    {
        if (amount <= 0) return false;
        if (prestigePoints < amount) return false;
        prestigePoints -= amount;
        onPrestigePointsChanged?.Invoke();
        return true;
    }

    /// <summary>현재 필드 상태 기준 환생 시 포인트 미리보기.</summary>
    public int PreviewPrestigeGain()
    {
        int gain = CalcPrestigeGainFromField();
        return (gain <= 0) ? 0 : gain;
    }

    /// <summary>
    /// 환생 실행: 포인트/경험치 적립 → 필드/경제/티어 리셋 → 즉시 저장.
    /// SaveData v2 구조를 변경하지 않도록 기존 필드만 저장/복원한다.
    /// </summary>
    public void DoPrestige()
    {
        if (!girlFieldManager || !economy) return;

        int gain = CalcPrestigeGainFromField();
        if (gain <= 0) gain = 1; // 보정(최종이 있는데 0 방지)

        prestigePoints     += gain;
        prestigeXP         += gain;
        totalPrestigeCount += 1;
        onPrestigePointsChanged?.Invoke();

        // 필드 비우기: 풀 미사용(소녀 프리팹은 풀 대상 아님) → Destroy로 정리
        WipeField();

        // 경제 리셋 (기존 코드베이스 메서드)
        economy.SetGold(0);
        economy.ResetGoldUpgradesForPrestige();

        // 티어 0 복귀
        if (tierManager) tierManager.SwitchTo(0);

        // 다시 조건 달성 시에만 알림
        _availableRaised = false;

        // 강종/크래시 대비 즉시 저장
        if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
    }

    // === 내부 계산/도우미 ======================================================
    /// <summary>필드에 최종(25+) 소녀가 하나라도 존재하는지.</summary>
    private bool HasAnyFinalGirl()
    {
        if (!girlFieldManager) return false;
        var list = girlFieldManager.girlList;
        for (int i = 0; i < list.Count; i++)
        {
            var g = list[i];
            if (g && g.Level >= topLevel) return true;
        }
        return false;
    }

    /// <summary>
    /// 임시 포인트 산식(25 랭크 통합 전 단계):
    ///  - 25레벨 이상이 N개면, 최종레벨을 (25 + (N-1))로 해석.
    ///  - 점수 = max(1, 최종레벨 - (topLevel - 1)) → 25→1, 26→2, 27→3 …
    ///  - 이후 "25단계 랭크(첫 생성만 오브젝트, 이후 생성은 랭크+1)" 구현 시
    ///    이 로직을 랭크 기반 계산으로 교체하면 됨(호환 유지).
    /// </summary>
    private int CalcPrestigeGainFromField()
    {
        if (!girlFieldManager) return 0;

        int count25 = 0;
        var list = girlFieldManager.girlList;
        for (int i = 0; i < list.Count; i++)
        {
            var g = list[i];
            if (!g) continue;
            if (g.Level >= topLevel) count25++;
        }

        if (count25 <= 0) return 0;

        int interpretedFinalLevel = topLevel + (count25 - 1);
        int gain = Math.Max(1, interpretedFinalLevel - (topLevel - 1));
        return gain;
    }

    /// <summary>필드의 모든 소녀 GameObject를 Destroy하고 리스트를 비운다.</summary>
    private void WipeField()
    {
        var snapshot = new List<GirlCharacter>(girlFieldManager.girlList);
        for (int i = 0; i < snapshot.Count; i++)
        {
            var g = snapshot[i];
            if (!g) continue;
            Destroy(g.gameObject);
        }
        girlFieldManager.girlList.Clear();
    }

    // === ISaveable ============================================================
    public void CollectSaveData(SaveData data)
    {
        // SaveData v2 호환: 기존 필드만 사용
        data.prestigePoint      = prestigePoints;
        data.totalPrestigeCount = totalPrestigeCount;
        // prestigeXP 등은 v3에서 정식 저장 예정(지금은 런타임만)
    }

    public void ApplyLoadedData(SaveData data)
    {
        prestigePoints     = Mathf.Max(0, data.prestigePoint);
        totalPrestigeCount = Mathf.Max(0, data.totalPrestigeCount);
        // v2에는 prestigeXP 없음 → 0으로 시작
        prestigeXP         = 0;

        onPrestigePointsChanged?.Invoke();
    }
}
