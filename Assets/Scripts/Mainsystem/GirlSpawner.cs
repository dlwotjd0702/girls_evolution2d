using UnityEngine;

public class GirlSpawner : MonoBehaviour
{
    public GirlEvolutionDB evolutionDB;
    public GameObject girlPrefab;
    public Transform spawnRoot;

    public Sprite[] girlSprites; // 인덱스 = 레벨-1 (가장 간단한 경우)

    // 예시: 1레벨 미소녀 자동 생성
    public void SpawnGirl(int level, Vector3 position)
    {
        GirlEvolutionData data = evolutionDB.GetDataByLevel(level);
        if (data == null) return;

        var go = Instantiate(girlPrefab, position, Quaternion.identity, spawnRoot);
        var girl = go.GetComponent<GirlCharacter>();
        var sprite = girlSprites[Mathf.Clamp(level - 1, 0, girlSprites.Length - 1)];
        girl.Init(data, sprite);
    }
}