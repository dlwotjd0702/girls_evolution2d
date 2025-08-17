using System.Collections.Generic;
using UnityEngine;

public class PrestigeManager : MonoBehaviour, ISaveable
{
    public GirlFieldManager girlFieldManager; // DI: GameSystem에서 할당
    public CurrencyManager currencyManager;   // DI: GameSystem에서 할당

    [Header("환생 데이터")]
    public int prestigePoint = 0;
    public int totalPrestigeCount = 0;
    public int topLevel = 25; // Start에서 TierRules.MaxLevel로 동기화

    public System.Action onPrestigeAvailable;

    private bool isPrestigeAvailable = false;

    private void Start()
    {
        if (topLevel <= 0 || topLevel != TierRules.MaxLevel)
            topLevel = TierRules.MaxLevel;
    }

    void Update()
    {
        if (girlFieldManager == null) return;

        bool found = false;
        var list = girlFieldManager.girlList;
        for (int i = 0; i < list.Count; i++)
        {
            var g = list[i];
            if (g != null && g.Level == topLevel)
            {
                found = true;
                break;
            }
        }

        if (found != isPrestigeAvailable)
        {
            isPrestigeAvailable = found;
            if (isPrestigeAvailable)
                onPrestigeAvailable?.Invoke();
        }
    }

    public void DoPrestige()
    {
        if (girlFieldManager == null || currencyManager == null) return;

        int reward = 0;
        var list = girlFieldManager.girlList;
        for (int i = 0; i < list.Count; i++)
        {
            var g = list[i];
            if (g != null && g.Level == topLevel)
                reward++;
        }

        prestigePoint += reward;
        totalPrestigeCount++;

        // 풀 반환으로 비우기
        var snapshot = new List<GirlCharacter>(girlFieldManager.girlList);
        foreach (var g in snapshot)
        {
            if (g == null) continue;
            girlFieldManager.RemoveGirl(g);
        }
        girlFieldManager.girlList.Clear();

        currencyManager.SetGold(0);

        Debug.Log($"[Prestige] 환생 완료! 환생석+{reward} (총 {prestigePoint}) / 누적 환생:{totalPrestigeCount}");
        isPrestigeAvailable = false;
    }

    public int GetPrestigePoint() => prestigePoint;
    public int GetTotalPrestigeCount() => totalPrestigeCount;

    // --------- ISaveable 구현 ---------
    public void ApplyLoadedData(SaveData data)
    {
        if (data == null) return;
        prestigePoint = data.prestigePoint;
        totalPrestigeCount = data.totalPrestigeCount;
        Debug.Log($"[PrestigeManager] 환생데이터 복원 완료: {prestigePoint}, {totalPrestigeCount}");
    }

    public void CollectSaveData(SaveData data)
    {
        data.prestigePoint = prestigePoint;
        data.totalPrestigeCount = totalPrestigeCount;
        Debug.Log($"[PrestigeManager] 환생데이터 저장: {prestigePoint}, {totalPrestigeCount}");
    }
}
