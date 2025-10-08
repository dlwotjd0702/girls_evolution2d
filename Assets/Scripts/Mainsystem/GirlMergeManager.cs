using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GirlMergeManager : MonoBehaviour
{
    [Header("DI (GameSystem에서 주입)")]
    public EconomyManager economy;
    public GirlDataManager DataManager;
    public GirlFieldManager fieldManager;

    private GirlCharacter draggingGirl = null;
    private GirlCharacter highlightedTarget = null;

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
            if (g == dragging) continue;
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
        var list = fieldManager.girlList;
        int n = list.Count;
        float bestDist = float.MaxValue;
        GirlCharacter first = null, second = null;

        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                if (list[i].Level != list[j].Level) continue;
                float dist = Vector2.Distance(
                    ((RectTransform)list[i].transform).localPosition,
                    ((RectTransform)list[j].transform).localPosition
                );
                if (dist < bestDist)
                {
                    bestDist = dist;
                    first = list[i];
                    second = list[j];
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
        Vector3 center = (((RectTransform)a.transform).localPosition + ((RectTransform)b.transform).localPosition) * 0.5f;
        int nextLevel = a.Level + 1;

        a.enabled = false;
        b.enabled = false;

        yield return StartCoroutine(MergeAnimation(a, b, center));

        a.enabled = true;
        b.enabled = true;

        fieldManager.RemoveGirl(a);
        fieldManager.RemoveGirl(b);

        if (nextLevel >= TierRules.MaxLevel) // 25 스택 처리
        {
            fieldManager.AcquireLevel25();
        }
        else
        {
            fieldManager.ManualSpawnGirl(nextLevel, center);
        }

        GirlData nextData = DataManager?.GetDataByLevel(nextLevel);
        if (nextData != null && economy != null)
            economy.AddGold(nextData.incomePerSec);
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
