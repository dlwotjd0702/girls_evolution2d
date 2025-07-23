using System.Collections.Generic;
using UnityEngine;

public class GirlFieldManager : MonoBehaviour
{
    public GirlDataManager dataManager;
    public GirlSpriteAddressableLoader spriteLoader;
    public GirlMergeManager mergeManager;
    public UpgradeManager upgradeManager;

    public GameObject girlPrefab;
    public Transform girlRoot;
    public List<GirlCharacter> girlList = new List<GirlCharacter>();

    public bool autoSpawnEnabled = false;
    public float spawnTimer = 0f;

    void Update()
    {
        if (!autoSpawnEnabled || dataManager == null) return;
        float interval = upgradeManager != null ? upgradeManager.GetSpawnInterval() : 2f;
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= interval)
        {
            spawnTimer = 0f;
            int spawnCount = upgradeManager != null ? upgradeManager.GetSpawnCount() : 1;
            for (int i = 0; i < spawnCount; i++)
            {
                Vector2 randPos = new Vector2(Random.Range(-4, 4), Random.Range(-2, 2));
                SpawnGirl(1, randPos);
            }
        }
    }

    public void SpawnTestGirls()
    {
        for (int level = 1; level <= 25; level++)
        {
            SpawnGirl(level, Vector2.zero);
        }
    }

    public void ManualSpawnGirl(int level, Vector2 pos)
    {
        SpawnGirl(level, pos);
    }

    private void SpawnGirl(int level, Vector2 pos)
    {
        if (dataManager == null) return;
        GirlData data = dataManager.GetDataByLevel(level);
        if (data == null) return;

        Sprite sprite = null;
        if (spriteLoader != null && !string.IsNullOrEmpty(data.spriteName))
            spriteLoader.SpriteDict.TryGetValue(data.spriteName, out sprite);

        var go = Instantiate(girlPrefab, pos, Quaternion.identity, girlRoot);
        var girl = go.GetComponent<GirlCharacter>();
        girl.Init(data, sprite); // Collider 자동화 포함!
        if (mergeManager != null)
            girl.mergeManager = mergeManager;
        girlList.Add(girl);
    }

    public void RemoveGirl(GirlCharacter girl)
    {
        girlList.Remove(girl);
        Destroy(girl.gameObject);
    }
}
