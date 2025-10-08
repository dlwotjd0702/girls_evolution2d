// ============================
// GirlMergeManager.cs
// - 자동합성 충돌/중복 방지
// - 24+24 -> 25: 첫 생성만 스폰, 그 이후엔 레벨만 증가
// ============================
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

    public void AddIncomeGold(GirlCharacter girl)
    {
        if (economy != null && girl != null)
            economy.AddGold(girl.GetIncome());
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
