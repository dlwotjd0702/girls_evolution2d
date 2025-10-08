using System;
using System.Collections.Generic;
using UnityEngine;

public class PrestigeManager : MonoBehaviour
{
    [Header("Refs")]
    public GirlFieldManager girlFieldManager;
    public EconomyManager economy;
    public TierManager tierManager;

    [Header("Config")]
    public int topLevel = TierRules.MaxLevel; // 보통 25
    public int prestigePoints = 0;

    /// <summary>최종 레벨(25 이상)이 필드에 등장했을 때 1회 호출.</summary>
    public event Action onPrestigeAvailable;

    bool _availableRaised = false;

    void Awake()
    {
        // DI 보강(선택)
        var gs = GameSystem.Instance;
        if (!girlFieldManager && gs) girlFieldManager = gs.fieldManager;
        if (!economy && gs)          economy = gs.economy;
        if (!tierManager)            tierManager = FindObjectOfType<TierManager>(true);
    }

    void Update()
    {
        if (!girlFieldManager) return;

        bool hasFinal = HasFinalGirl();
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

    /// <summary>필드에 최종(25 이상) 소녀가 하나라도 있는지 여부.</summary>
    bool HasFinalGirl()
    {
        var list = girlFieldManager.girlList;
        for (int i = 0; i < list.Count; i++)
            if (list[i] && list[i].Level >= topLevel) return true;
        return false;
    }

    public int GetPrestigePoint() => prestigePoints;

    /// <summary>
    /// 환생 수행. 포인트는 "최종 소녀의 현재 레벨"에 비례.
    /// 25 → 1점, 26 → 2점, 27 → 3점 … = (finalLevel - 24)
    /// </summary>
    public void DoPrestige()
    {
        if (!girlFieldManager || !economy) return;

        int gain = CalcPrestigeGain();
        if (gain <= 0) gain = 1; // 안전장치
        prestigePoints += gain;

        // 필드 초기화
        var snapshot = new List<GirlCharacter>(girlFieldManager.girlList);
        foreach (var g in snapshot)
        {
            if (!g) continue;
            girlFieldManager.RemoveGirl(g);
        }
        girlFieldManager.girlList.Clear();

        // 경제 리셋
        economy.SetGold(0);

        // 티어 0으로 복귀
        if (tierManager) tierManager.SwitchTo(0);

        // 다음 판에서 다시 조건 달성해야 버튼 뜨도록
        _availableRaised = false;
    }

    /// <summary>
    /// 최종 소녀의 레벨로 환생 포인트 계산.
    /// 여러 개라면 "가장 높은 레벨" 기준. (일반적으로 하나만 존재)
    /// </summary>
    int CalcPrestigeGain()
    {
        int bestFinalLevel = 0;
        foreach (var g in girlFieldManager.girlList)
        {
            if (g && g.Level >= topLevel)
                bestFinalLevel = Mathf.Max(bestFinalLevel, g.Level);
        }

        if (bestFinalLevel <= 0) return 0;

        // 25 -> 1, 26 -> 2, 27 -> 3 ...
        return Mathf.Max(1, bestFinalLevel - (topLevel - 1));
    }
}
