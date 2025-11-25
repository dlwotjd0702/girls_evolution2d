// ============================
// GirlMergeManager.cs (FULL, +2단 도약 확률 적용 버전)
// - nextLevel 계산 직후, "계승 등급 + 환생 상점"의 +2단 도약 확률을 합산(최대 50%)
// - MaxLevel/직전레벨 구간에는 미적용
// - 외부 매니저가 없어도 컴파일되도록 리플렉션 기반 안전 조회(1회 캐싱)
// ============================
using System;
using System.Reflection;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GirlMergeManager : MonoBehaviour
{
    [Header("DI")]
    public EconomyManager economy;
    public GirlDataManager DataManager;
    public GirlFieldManager fieldManager;

    private GirlCharacter draggingGirl = null;
    private GirlCharacter highlightedTarget = null;

    // 자동합성 중복/충돌 방지
    private bool _mergeBusy = false;
    private readonly HashSet<GirlCharacter> _mergingSet = new();

    // ── 외부 확률(계승/상점) 리플렉션 캐시 ──
    static bool _legacyCached = false;
    static object _legacyInst;
    static MethodInfo _miLegacyTwoStep; // float GetTwoStepMergeChance()

    static bool _shopCached = false;
    static object _shopInst;
    static MethodInfo _miShopTwoStep;   // float GetTwoStepChance()

    static Type FindTypeByName(string name)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(name);
            if (t != null) return t;
            try
            {
                foreach (var tt in asm.GetTypes())
                    if (tt.Name == name) return tt;
            }
            catch { /* 일부 어셈블리는 GetTypes 실패 가능 */ }
        }
        return null;
    }
    static void EnsureLegacyCache()
    {
        if (_legacyCached) return;
        _legacyCached = true;
        var t = FindTypeByName("LegacyRankManager");
        if (t == null) return;
        var pi = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        _legacyInst = pi?.GetValue(null, null);
        _miLegacyTwoStep = t.GetMethod("GetTwoStepMergeChance", BindingFlags.Public | BindingFlags.Instance);
    }
    static void EnsureShopCache()
    {
        if (_shopCached) return;
        _shopCached = true;
        var t = FindTypeByName("PrestigeShopManager");
        if (t == null) return;
        var pi = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        _shopInst = pi?.GetValue(null, null);
        _miShopTwoStep = t.GetMethod("GetTwoStepChance", BindingFlags.Public | BindingFlags.Instance);
    }
    static float GetLegacyTwoStepChanceSafe()
    {
        try
        {
            EnsureLegacyCache();
            if (_legacyInst != null && _miLegacyTwoStep != null)
            {
                var v = _miLegacyTwoStep.Invoke(_legacyInst, null);
                return Convert.ToSingle(v);
            }
        }
        catch { }
        return 0f;
    }
    static float GetShopTwoStepChanceSafe()
    {
        try
        {
            EnsureShopCache();
            if (_shopInst != null && _miShopTwoStep != null)
            {
                var v = _miShopTwoStep.Invoke(_shopInst, null);
                return Convert.ToSingle(v);
            }
        }
        catch { }
        return 0f;
    }

    public void SetDraggingGirl(GirlCharacter girl) => draggingGirl = girl;

    public void ClearDraggingGirl()
    {
        draggingGirl = null;
        if (highlightedTarget) highlightedTarget.Highlight(false);
        highlightedTarget = null;
    }

    public void UpdateMergeHighlight(GirlCharacter dragging)
    {
        GirlCharacter bestTarget = null;
        float bestDist = 180f;

        foreach (var g in fieldManager.girlList)
        {
            if (!g || g == dragging) continue;
            if (g.Level != dragging.Level) continue;
            float dist = Vector2.Distance(
                ((RectTransform)g.transform).localPosition,
                ((RectTransform)dragging.transform).localPosition
            );
            if (dist < bestDist)
            {
                bestDist = dist;
                bestTarget = g;
            }
        }

        if (highlightedTarget && highlightedTarget != bestTarget)
            highlightedTarget.Highlight(false);

        highlightedTarget = bestTarget;
        if (highlightedTarget != null)
            highlightedTarget.Highlight(true);
    }

    public void TryMergeByDrag(GirlCharacter dragging)
    {
        if (highlightedTarget != null)
            StartCoroutine(MergeRoutine(dragging, highlightedTarget));
    }

    public void AddIncomeGold(GirlCharacter girl, bool isClick = false)
    {
        if (economy == null || girl == null) return;

        double gain = girl.GetIncome();
        if (gain <= 0) return;

        if (isClick)
        {
            gain *= 0.1; // 클릭 기반: 생산 골드의 10%
            gain *= economy.GetClickBonusMultiplier();
        }

        economy.AddGold(gain);
        fieldManager?.ShowGoldPopup(girl, gain, isClick);
    }

    public void TryAutoMerge()
    {
        if (_mergeBusy) return;

        var list = fieldManager.girlList;
        int n = list.Count;

        float bestDist = float.MaxValue;
        GirlCharacter first = null, second = null;

        for (int i = 0; i < n; i++)
        {
            var gi = list[i];
            if (!gi || _mergingSet.Contains(gi)) continue;

            for (int j = i + 1; j < n; j++)
            {
                var gj = list[j];
                if (!gj || _mergingSet.Contains(gj)) continue;

                if (gi.Level != gj.Level) continue;

                // 25 이상은 자동합성 대상 아님(이미 최종)
                if (gi.IsFinal || gj.IsFinal) continue;

                float dist = Vector2.Distance(
                    ((RectTransform)gi.transform).localPosition,
                    ((RectTransform)gj.transform).localPosition
                );
                if (dist < bestDist)
                {
                    bestDist = dist;
                    first = gi;
                    second = gj;
                }
            }
        }

        if (first != null && second != null)
        {
            StartCoroutine(MergeRoutine(first, second));
        }
    }

    IEnumerator MergeRoutine(GirlCharacter a, GirlCharacter b)
    {
        if (!a || !b || a == b) yield break;
        if (_mergingSet.Contains(a) || _mergingSet.Contains(b)) yield break;

        _mergeBusy = true;
        _mergingSet.Add(a);
        _mergingSet.Add(b);

        Vector3 center = (((RectTransform)a.transform).localPosition + ((RectTransform)b.transform).localPosition) * 0.5f;
        int nextLevel = a.Level + 1;

        // ◀ +2단 도약 확률: 계승 등급 + 환생 상점 (합산 cap 50%)
        //    단, 25/직전(=MaxLevel-1) 구간에는 미적용
        if (nextLevel <= TierRules.MaxLevel - 2)
        {
            float pRank = GetLegacyTwoStepChanceSafe();
            float pShop = GetShopTwoStepChanceSafe();
            float p = Mathf.Min(0.50f, pRank + pShop);
            if (UnityEngine.Random.value < p) nextLevel += 1; // 총 +2
        }

        // 합성 중 입력 막기
        a.enabled = false;
        b.enabled = false;

        yield return MergeAnimation(a, b, center);

        // 풀 반환 전 다시 활성화(풀 재사용 안전)
        a.enabled = true;
        b.enabled = true;

        fieldManager.RemoveGirl(a);
        fieldManager.RemoveGirl(b);

        if (nextLevel >= TierRules.MaxLevel) // 24+24 -> 25
        {
            fieldManager.AcquireLevel25();    // 최초만 생성, 이후엔 기존 25의 레벨만 ++
        }
        else
        {
            fieldManager.ManualSpawnGirl(nextLevel, center);
        }

        // 합성 보너스(선택)
        GirlData nextData = DataManager?.GetDataByLevel(Mathf.Min(nextLevel, TierRules.MaxLevel));
        if (nextData != null && economy != null)
            economy.AddGold(nextData.incomePerSec);

        _mergingSet.Remove(a);
        _mergingSet.Remove(b);
        _mergeBusy = false;
    }

    IEnumerator MergeAnimation(GirlCharacter a, GirlCharacter b, Vector3 center)
    {
        float spread = 90f;
        Vector3 aOrigin = ((RectTransform)a.transform).localPosition;
        Vector3 bOrigin = ((RectTransform)b.transform).localPosition;
        Vector3 aSpread = center + (aOrigin - center).normalized * spread;
        Vector3 bSpread = center + (bOrigin - center).normalized * spread;

        float t = 0;
        while (t < 0.25f)
        {
            t += Time.deltaTime * 2.0f;
            ((RectTransform)a.transform).localPosition = Vector3.Lerp(aOrigin, aSpread, t / 0.25f);
            ((RectTransform)b.transform).localPosition = Vector3.Lerp(bOrigin, bSpread, t / 0.25f);
            yield return null;
        }
        t = 0;
        while (t < 0.35f)
        {
            t += Time.deltaTime * 2.0f;
            ((RectTransform)a.transform).localPosition = Vector3.Lerp(aSpread, center, t / 0.35f);
            ((RectTransform)b.transform).localPosition = Vector3.Lerp(bSpread, center, t / 0.35f);
            yield return null;
        }
    }
}
