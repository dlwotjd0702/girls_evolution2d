using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class GirlFieldManager : MonoBehaviour
{
    public GirlDataManager dataManager;
    public GirlSpriteAddressableLoader spriteLoader;
    public GirlMergeManager mergeManager;
    public UpgradeManager upgradeManager;
    public Transform girlRoot;
    public List<GirlCharacter> girlList = new List<GirlCharacter>();

    // 소환 게이지 관리
    public int curSpawnCharge = 0;
    private float chargeTimer = 0f;
    private float autoSpawnTimer = 0f;

    void Start()
    {
        curSpawnCharge = GetMaxSpawnCharge();
        UpdateSpawnButtonUI();
        StartCoroutine(AutoMergeRoutine());
    }

    void Update()
    {
        // 1. 게이지 자동충전
        if (curSpawnCharge < GetMaxSpawnCharge())
        {
            chargeTimer += Time.deltaTime;
            if (chargeTimer >= GetSpawnChargeInterval())
            {
                chargeTimer -= GetSpawnChargeInterval();
                curSpawnCharge++;
                UpdateSpawnButtonUI();
            }
        }
        // 2. 자동 소환 (게이지 있으면 자동으로 소환)
        if (upgradeManager != null && upgradeManager.autoSpawnUpgrade > 0)
        {
            autoSpawnTimer += Time.deltaTime;
            if (autoSpawnTimer >= upgradeManager.GetAutoSpawnInterval())
            {
                autoSpawnTimer -= upgradeManager.GetAutoSpawnInterval();
                TryAutoSpawn();
            }
        }
    }

    // 수동 소환 버튼에서 호출
    public void OnClickSpawnButton()
    {
        if (curSpawnCharge > 0 && girlList.Count < GetMaxFieldCount())
        {
            curSpawnCharge--;
            SpawnGirl(1, GetRandomSpawnPos());
            UpdateSpawnButtonUI();
        }
    }

    // 자동 소환 처리
    private void TryAutoSpawn()
    {
        if (curSpawnCharge > 0 && girlList.Count < GetMaxFieldCount())
        {
            curSpawnCharge--;
            SpawnGirl(1, GetRandomSpawnPos());
            UpdateSpawnButtonUI();
        }
    }

    // --- 소환/필드 설정 ---
    public void SpawnTestGirls()
    {
        for (int level = 1; level <= 25; level++)
            SpawnGirl(level, Vector2.zero);
    }

    public void ManualSpawnGirl(int level, Vector3 pos)
    {
        SpawnGirl(level, pos);
    }

    private void SpawnGirl(int level, Vector3 pos)
    {
        if (dataManager == null) return;
        GirlData data = dataManager.GetDataByLevel(level);
        if (data == null) return;

        Sprite sprite = null;
        if (spriteLoader != null && !string.IsNullOrEmpty(data.spriteName))
            spriteLoader.SpriteDict.TryGetValue(data.spriteName, out sprite);

        var go = SimpleUIPool.Instance.Get(girlRoot);
        var rect = go.transform as RectTransform;
        if (rect != null) rect.localPosition = pos;
        else go.transform.localPosition = pos;

        var girl = go.GetComponent<GirlCharacter>();
        girl.OnGetFromPool(); // (상태리셋)
        girl.Init(data, sprite);
        girl.mergeManager = mergeManager;
        girlFieldAdd(girl);
    }

    private void girlFieldAdd(GirlCharacter girl)
    {
        if (!girlList.Contains(girl))
            girlList.Add(girl);
    }

    public void RemoveGirl(GirlCharacter girl)
    {
        girlList.Remove(girl);
        girl.KillAllTweens();
        SimpleUIPool.Instance.Return(girl.gameObject);
    }

    // 랜덤 소환 위치
    private Vector2 GetRandomSpawnPos()
    {
        float x = Random.Range(-350f, 350f);
        float y = Random.Range(-600f, 600f);
        return new Vector2(x, y);
    }

    // 업그레이드 연동
    private int GetMaxSpawnCharge() =>
        upgradeManager != null ? upgradeManager.GetMaxManualSpawnCount() : 3;
    private float GetSpawnChargeInterval() =>
        upgradeManager != null ? upgradeManager.GetManualSpawnInterval() : 10f;
    private int GetMaxFieldCount() =>
        upgradeManager != null ? upgradeManager.GetMaxFieldCount() : 8;

    // UI 연동 함수(버튼, 텍스트 등)
    private void UpdateSpawnButtonUI()
    {
        // 실제 UI 연동 필요시 구현
    }

    // 자동합성 루프 (업그레이드 해금 시)
    private IEnumerator AutoMergeRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            if (upgradeManager != null && upgradeManager.IsAutoMergeActive())
                mergeManager.TryAutoMerge();
        }
    }
}
