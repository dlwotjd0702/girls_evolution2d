using System.Collections.Generic;
using UnityEngine;

public class GirlFieldManager : MonoBehaviour, ISaveable
{
    public List<GirlCharacter> girlList = new List<GirlCharacter>();
    public GameObject girlPrefab;
    public Transform girlRoot; // 미소녀 오브젝트 부모
    public GirlEvolutionDB evolutionDB;
    public GirlMergeManager mergeManager;

    public void ApplyLoadedData(SaveData data)
    {
        foreach (var girl in girlList)
            Destroy(girl.gameObject);
        girlList.Clear();

        if (data == null || data.girls == null) return;

        foreach (var info in data.girls)
        {
            Vector2 pos = new Vector2(info.posX, info.posY);
            SpawnGirl(info.level, pos);
        }
        Debug.Log($"[GirlFieldManager] 미소녀 {girlList.Count}명 복원");
    }

    public void CollectSaveData(SaveData data)
    {
        data.girls.Clear();
        foreach (var girl in girlList)
        {
            Vector2 pos = girl.transform.position;
            data.girls.Add(new GirlSaveInfo(girl.level, pos.x, pos.y));
        }
        Debug.Log($"[GirlFieldManager] 미소녀 {girlList.Count}명 저장");
    }

    public void SpawnGirl(int level, Vector2 pos)
    {
        GirlEvolutionData evoData = evolutionDB.GetDataByLevel(level);
        if (evoData == null) return;

        var go = Instantiate(girlPrefab, pos, Quaternion.identity, girlRoot);
        var girl = go.GetComponent<GirlCharacter>();
        girl.Init(evoData, null); // sprite 등은 필요시 추가
        
        // mergeManager 참조 설정
        if (mergeManager != null)
            girl.mergeManager = mergeManager;
            
        girlList.Add(girl);
    }
    
    // GirlManager 역할도 수행
    public void RemoveGirl(GirlCharacter girl)
    {
        girlList.Remove(girl);
    }
}