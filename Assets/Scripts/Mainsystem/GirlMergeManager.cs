using GirlsEvolution2D.MainSystem;
using UnityEngine;

public class GirlMergeManager : MonoBehaviour
{
    [Header("DI (GameSystem에서 주입)")]
    public CurrencyManager currencyManager;
    public GirlDataManager DataManager;
    public GirlFieldManager fieldManager;
    private GirlCharacter firstSelected = null;

    public void OnGirlClicked(GirlCharacter girl)
    {
        if (firstSelected == null)
        {
            firstSelected = girl;
            girl.SetSelected(true);
            return;
        }

        if (firstSelected != girl && firstSelected.level == girl.level)
        {
            Vector3 newPos = (firstSelected.transform.position + girl.transform.position) * 0.5f;
            int nextLevel = girl.level + 1;

            // 삭제
            fieldManager.RemoveGirl(firstSelected);
            fieldManager.RemoveGirl(girl);

            // 새 캐릭터 생성
            fieldManager.ManualSpawnGirl(nextLevel, newPos);

            // 합성 보상: 강화 보정 없이 해당 레벨 수익만 지급
            GirlData nextData = DataManager?.GetDataByLevel(nextLevel);
            if (nextData != null && currencyManager != null)
            {
                currencyManager.AddGold(nextData.incomePerSec);
            }

            // 외부 싱글턴 (GPGS 등) 호출 (필요시)
            if (GPGSManager.Instance != null && GPGSManager.Instance.IsAuthenticated)
                GPGSManager.Instance.IncrementAchievement("CgkI8JqQ8-4YEAIQAg", 1, 100);
        }
        else
        {
            firstSelected.SetSelected(false);
        }
        firstSelected = null;
    }
}