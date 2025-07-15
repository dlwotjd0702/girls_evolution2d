using UnityEngine;
using GirlsEvolution2D.MainSystem;

public class GirlMergeManager : MonoBehaviour
{
    public CurrencyManager currencyManager;
    public GirlEvolutionDB evolutionDB;
    public GirlSpawner spawner;
    public UpgradeManager upgradeManager;
    private GirlCharacter firstSelected = null;

    public void OnGirlClicked(GirlCharacter girl)
    {
        if (firstSelected == null)
        {
            firstSelected = girl;
            girl.SetSelected(true);
        }
        else
        {
            if (firstSelected != girl && firstSelected.level == girl.level)
            {
                Vector3 newPos = (firstSelected.transform.position + girl.transform.position) * 0.5f;
                int nextLevel = girl.level + 1;
                Destroy(firstSelected.gameObject);
                Destroy(girl.gameObject);

                GirlEvolutionData nextData = evolutionDB.GetDataByLevel(nextLevel);
                spawner.SpawnGirl(nextLevel, newPos);

                // 합성 성공 시 보상
                if (nextData != null)
                {
                    double bonus = nextData.incomePerSec;
                    
                    // 업그레이드 효과 적용
                    if (upgradeManager != null)
                    {
                        bonus *= upgradeManager.GetMergeBonusUpgrade() + 1; // 기본 1배 + 업그레이드 배수
                    }
                    
                    currencyManager.AddGold(bonus);
                    
                    // GPGS 업적 업데이트
                    if (GPGSManager.Instance != null && GPGSManager.Instance.IsAuthenticated)
                    {
                        // 진화 업적 진행도 업데이트
                        GPGSManager.Instance.IncrementAchievement("CgkI8JqQ8-4YEAIQAg", 1, 100);
                    }
                }
            }
            else
            {
                firstSelected.SetSelected(false);
            }
            firstSelected = null;
        }
    }
}