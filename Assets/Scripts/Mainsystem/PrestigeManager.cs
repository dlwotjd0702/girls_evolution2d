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
    public int topLevel = TierRules.MaxLevel; // 25
    public int prestigePoints = 0;

    // 25를 만들 수 있게 되었을 때 버튼 ON 시그널
    public event Action onPrestigeAvailable;

    bool _availableRaised = false;

    void Update()
    {
        // 간단: 현재 필드에 25가 존재하면 환생 버튼 노출
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

    bool HasFinalGirl()
    {
        var list = girlFieldManager.girlList;
        for (int i = 0; i < list.Count; i++)
            if (list[i] && list[i].Level >= topLevel) return true;
        return false;
    }

    public int GetPrestigePoint() => prestigePoints;

    public void DoPrestige()
    {
        if (!girlFieldManager || !economy) return;

        // 25단계의 "레벨"에 비례: 25 최초 1점, 이후 25 추가스택마다 +1 점수
        int gain = CalcPrestigeGain();
        if (gain <= 0) gain = 1;
        prestigePoints += gain;

        // 필드 초기화
        var snapshot = new List<GirlCharacter>(girlFieldManager.girlList);
        foreach (var g in snapshot)
        {
            if (!g) continue;
            girlFieldManager.RemoveGirl(g);
        }
        girlFieldManager.girlList.Clear();

        // 골드/스폰 등 리셋
        economy.SetGold(0);

        // 티어 0으로 복귀
        if (tierManager) tierManager.SwitchTo(0);

        // 다음 판에서 다시 조건 달성해야 버튼 뜨도록
        _availableRaised = false;
    }

    int CalcPrestigeGain()
    {
        // 필드에 있는 최종 소녀의 랭크를 합산해도 되고, 최고 랭크만 써도 됨.
        // 여기선 "최종(25) 1개 있으면 1점 + FinalRank"로 간단화.
        int best = 0;
        foreach (var g in girlFieldManager.girlList)
        {
            if (g && g.Level >= topLevel)
                best = Mathf.Max(best, 1 + g.FinalRank);
        }
        return best;
    }
}
