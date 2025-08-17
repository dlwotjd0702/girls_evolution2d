using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class GirlFieldManager : MonoBehaviour
{
    // ── 기존 참조들 ──
    public GirlDataManager dataManager;
    public GirlSpriteAddressableLoader spriteLoader;
    public GirlMergeManager mergeManager;
    public UpgradeManager upgradeManager;
    public Transform girlRoot;
    public List<GirlCharacter> girlList = new List<GirlCharacter>();

    // ── 계층 연동(버튼에서 직접 호출하는 방식) ──
    [Header("Tier")]
    [SerializeField] private TierManager tierManager;

    [Header("Optional parents (for organization)")]
    [SerializeField] private Transform activeParent; // 현재 계층 보이는 부모
    [SerializeField] private Transform hiddenParent; // 숨김용 부모

    // 소환 게이지 관리
    public int curSpawnCharge = 0;
    private float chargeTimer = 0f;
    private float autoSpawnTimer = 0f;

    void Start()
    {
        curSpawnCharge = GetMaxSpawnCharge();
        UpdateSpawnButtonUI();
        StartCoroutine(AutoMergeRoutine());

        // 시작 시 한 번 초기화: 현 계층만 보이게(리스트에 있는 애들만 대상)
        if (tierManager != null) ShowOnlyTier(tierManager.CurrentTierIndex);
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

        // ▶ 스폰된 애는 현 계층인지 확인해 즉시 보이기/숨기기
        ApplyVisibilityFor(girl);
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

   
    public void SwitchTierNextUnlockedCycle()
    {
        if (tierManager == null) return;

        int prev = tierManager.CurrentTierIndex;
        int target = -1;

        // 1) 현재+1 ~ 3 중에서 "언락된" 다음 계층 찾기
        for (int i = prev + 1; i < 4; i++)
        {
            if (tierManager.Unlocked[i]) { target = i; break; }
        }

        // 2) 위에서 못 찾았으면 0층으로 랩
        if (target == -1) target = 0;

        // (안전) 0층이 잠겨있을 일은 없지만, 혹시 몰라 첫 언락층을 다시 탐색
        if (!tierManager.Unlocked[target])
        {
            for (int i = 0; i < 4; i++)
            {
                if (tierManager.Unlocked[i]) { target = i; break; }
            }
        }

        // 변화 없으면 리턴
        if (target == prev) return;

        // 상태 업데이트 + 두 계층만 토글
        tierManager.SwitchTo(target);
        ToggleTwoTiers(prev, target);
    }


    // 이전 계층만 숨기고, 타겟 계층만 보이기
    private void ToggleTwoTiers(int prevTier, int targetTier)
    {
        for (int i = 0; i < girlList.Count; i++)
        {
            var g = girlList[i];
            if (g == null) continue;

            int level = g.Level; // 프로젝트에 맞게 g.level / g.Data.level 이면 수정
            int itemTier = TierRules.TierIndexFromLevel(level);

            if (itemTier == prevTier) HideGirl(g);
            else if (itemTier == targetTier) ShowGirl(g);
            // 그 외의 계층(예: 0->2로 건너뛸 때 1층)은 기존 상태 유지(이미 숨김일 것)
        }
    }

    // 시작 시 1회: 해당 계층만 보이고 나머지는 숨김
    private void ShowOnlyTier(int tierIndex)
    {
        for (int i = 0; i < girlList.Count; i++)
        {
            var g = girlList[i];
            if (g == null) continue;

            int level = g.Level;
            int itemTier = TierRules.TierIndexFromLevel(level);
            if (itemTier == tierIndex) ShowGirl(g);
            else HideGirl(g);
        }
    }

    // 새로 생성된 개체 1개만 즉시 반영
    private void ApplyVisibilityFor(GirlCharacter girl)
    {
        if (tierManager == null || girl == null) return;
        int itemTier = TierRules.TierIndexFromLevel(girl.Level);
        if (itemTier == tierManager.CurrentTierIndex) ShowGirl(girl);
        else HideGirl(girl);
    }

    private void ShowGirl(GirlCharacter g)
    {
        if (g == null) return;
        g.gameObject.SetActive(true);
        if (activeParent != null && g.transform.parent != activeParent)
            g.transform.SetParent(activeParent, true);
        // 입력/콜라이더를 쓰면 여기서 enable
        // if (g.TryGetComponent<Collider2D>(out var col)) col.enabled = true;
    }

    private void HideGirl(GirlCharacter g)
    {
        if (g == null) return;
        g.gameObject.SetActive(false);
        if (hiddenParent != null && g.transform.parent != hiddenParent)
            g.transform.SetParent(hiddenParent, true);
        // if (g.TryGetComponent<Collider2D>(out var col)) col.enabled = false;
    }

    // IdleGold 등에서 전체를 읽어야 하면 공개
    public IReadOnlyList<GirlCharacter> AllGirls => girlList;
}
