using UnityEngine;

public class GirlAutoSpawner : MonoBehaviour
{
    public GirlSpawner spawner;

    float spawnInterval = 2f;
    float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0;
            // 1레벨 미소녀를 랜덤 위치에 생성
            Vector3 randPos = new Vector3(Random.Range(-4, 4), Random.Range(-2, 2), 0);
            spawner.SpawnGirl(1, randPos);
        }
    }
}