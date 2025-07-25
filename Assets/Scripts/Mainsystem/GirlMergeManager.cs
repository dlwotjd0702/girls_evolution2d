using GirlsEvolution2D.MainSystem;
using UnityEngine;
using System.Collections;

public class GirlMergeManager : MonoBehaviour
{
    [Header("DI (GameSystem에서 주입)")]
    public CurrencyManager currencyManager;
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
        float bestDist = 0.7f;
        foreach (var g in fieldManager.girlList)
        {
            if (g == dragging) continue;
            if (g.level != dragging.level) continue;
            float dist = Vector2.Distance(g.transform.position, dragging.transform.position);
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
        if (currencyManager != null && girl.data != null)
            currencyManager.AddGold(girl.data.incomePerSec);
    }

    IEnumerator MergeRoutine(GirlCharacter a, GirlCharacter b)
    {
        Vector3 center = (a.transform.position + b.transform.position) * 0.5f;
        int nextLevel = a.level + 1;

        a.enabled = false;
        b.enabled = false;

        yield return StartCoroutine(MergeAnimation(a, b, center));

        fieldManager.RemoveGirl(a);
        fieldManager.RemoveGirl(b);
        fieldManager.ManualSpawnGirl(nextLevel, center);

        GirlData nextData = DataManager?.GetDataByLevel(nextLevel);
        if (nextData != null && currencyManager != null)
            currencyManager.AddGold(nextData.incomePerSec);

       // if (GPGSManager.Instance != null && GPGSManager.Instance.IsAuthenticated)
       //     GPGSManager.Instance.IncrementAchievement("CgkI8JqQ8-4YEAIQAg", 1, 100);
    }

    IEnumerator MergeAnimation(GirlCharacter a, GirlCharacter b, Vector3 center)
    {
        float spread = 0.45f;
        Vector3 aOrigin = a.transform.position;
        Vector3 bOrigin = b.transform.position;
        Vector3 aSpread = center + (aOrigin - center).normalized * spread;
        Vector3 bSpread = center + (bOrigin - center).normalized * spread;

        float t = 0;
        while (t < 0.25f)
        {
            t += Time.deltaTime * 2.0f;
            a.transform.position = Vector3.Lerp(aOrigin, aSpread, t / 0.25f);
            b.transform.position = Vector3.Lerp(bOrigin, bSpread, t / 0.25f);
            yield return null;
        }
        t = 0;
        while (t < 0.35f)
        {
            t += Time.deltaTime * 2.0f;
            a.transform.position = Vector3.Lerp(aSpread, center, t / 0.35f);
            b.transform.position = Vector3.Lerp(bSpread, center, t / 0.35f);
            yield return null;
        }
    }
}
