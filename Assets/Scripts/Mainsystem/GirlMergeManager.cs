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
    static Func<float> _legacyTwoStepCached;

    static bool _shopCached = false;
    static object _shopInst;
    static MethodInfo _miShopTwoStep;   // float GetTwoStepChance()
    static Func<float> _shopTwoStepCached;

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
    // 리플렉션 → 캐시된 delegate를 통한 경량 호출
    static float GetLegacyTwoStepChanceSafe()
    {
        try
        {
            EnsureLegacyCache();
            if (_legacyInst != null && _miLegacyTwoStep != null)
            {
                // 한 번만 delegate를 만들어 캐시
                if (_legacyTwoStepCached == null)
                {
                    _legacyTwoStepCached = (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), _legacyInst, _miLegacyTwoStep, false);
                }
                return _legacyTwoStepCached != null ? _legacyTwoStepCached() : 0f;
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
                if (_shopTwoStepCached == null)
                {
                    _shopTwoStepCached = (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), _shopInst, _miShopTwoStep, false);
                }
                return _shopTwoStepCached != null ? _shopTwoStepCached() : 0f;
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
        if (dragging == null || fieldManager == null) return;

        // 드래그 중인 위치/레벨만 미리 캐싱
        var draggingRt = dragging.transform as RectTransform;
        if (draggingRt == null) return;

        Vector3 dragPos = draggingRt.localPosition;
        int dragLevel = dragging.Level;

        GirlCharacter bestTarget = null;
        float bestDistSqr = 180f * 180f; // 최대 탐색 반경 제곱

        var list = fieldManager.girlList;
        int count = list.Count;

        for (int i = 0; i < count; i++)
        {
            var g = list[i];
            if (!g || g == dragging) continue;
            if (g.Level != dragLevel) continue; // 같은 레벨만 대상

            var rt = g.transform as RectTransform;
            if (rt == null) continue;

            Vector3 delta = rt.localPosition - dragPos;
            float distSqr = delta.sqrMagnitude;

            // 일정 거리 밖은 early-out (제곱 거리 기준)
            if (distSqr >= bestDistSqr) continue;

            bestDistSqr = distSqr;
            bestTarget = g;
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
            
            // 클릭 효과음 재생
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayClickSFX();
            }
        }

        economy.AddGold(gain);
        fieldManager?.ShowGoldPopup(girl, gain, isClick);
    }

    public void TryAutoMerge()
    {
        if (_mergeBusy || fieldManager == null) return;

        var list = fieldManager.girlList;
        int n = list.Count;
        if (n <= 1) return;

        // 현재 필드에서 발견된 최대 레벨은 자동 합성 대상에서 제외
        int currentMaxLevel = fieldManager.CurrentMaxLevel;

        // N이 너무 크면 완전 탐색(O(N²)) 대신 "처음 찾은 페어"만 사용해 조기 종료
        const int HARD_PAIR_SCAN_LIMIT = 80;

        float bestDistSqr = float.MaxValue;
        GirlCharacter first = null, second = null;

        for (int i = 0; i < n; i++)
        {
            var gi = list[i];
            if (!gi || _mergingSet.Contains(gi) || gi.IsFinal) continue;

            // 현재 발견 최대 레벨 이상은 자동 합성 금지 (직접 합성만 허용)
            if (gi.Level >= currentMaxLevel && currentMaxLevel > 0) continue;

            var ri = gi.transform as RectTransform;
            if (ri == null) continue;

            int level = gi.Level;

            for (int j = i + 1; j < n; j++)
            {
                var gj = list[j];
                if (!gj || _mergingSet.Contains(gj) || gj.IsFinal) continue;
                if (gj.Level != level) continue;
                if (gj.Level >= currentMaxLevel && currentMaxLevel > 0) continue;

                var rj = gj.transform as RectTransform;
                if (rj == null) continue;

                Vector3 delta = ri.localPosition - rj.localPosition;
                float distSqr = delta.sqrMagnitude;

                if (distSqr < bestDistSqr)
                {
                    bestDistSqr = distSqr;
                    first = gi;
                    second = gj;

                    // 캐릭터 수가 많은 경우에는 "가장 가까운 페어"를 찾지 않고
                    // 첫 번째로 발견한 유효 페어에서 바로 종료해 스파이크 방지
                    if (n > HARD_PAIR_SCAN_LIMIT)
                    {
                        StartCoroutine(MergeRoutine(first, second));
                        return;
                    }
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
        
        // 합성 효과음 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMergeSFX();
        }

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
