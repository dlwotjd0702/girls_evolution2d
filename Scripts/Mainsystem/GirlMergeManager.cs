using UnityEngine;

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