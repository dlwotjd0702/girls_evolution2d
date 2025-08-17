// … 파일 상단 using 그대로 …
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class GirlFieldManager : MonoBehaviour, ISaveable
{
    // ── 참조 ──
    public GirlDataManager dataManager;
    public GirlSpriteAddressableLoader spriteLoader;
    public GirlMergeManager mergeManager;
    public UpgradeManager upgradeManager;
    public Transform girlRoot;
    public List<GirlCharacter> girlList = new List<GirlCharacter>();

    // ── 티어 ──
    [Header("Tier")]
    [SerializeField] private TierManager tierManager;

    [Header("Optional parents (for organization)")]
    [SerializeField] private Transform activeParent; // 현재 계층 표시용
    [SerializeField] private Transform hiddenParent; // 숨김용

    // ── 첫 발견 관리 ──
    private readonly HashSet<int> discoveredLevels = new HashSet<int>(); // 1~25
    private bool _isRestoring = false; // 로드 중엔 첫발견 연출 X
    private Coroutine _restoreRoutine;

    // ── 소환 게이지 ──
    public int curSpawnCharge = 0;
    private float chargeTimer = 0f;
    private float autoSpawnTimer = 0f;

    // ── 첫 발견 연출 튜닝 ──
    [Header("Discovery FX")]
    [SerializeField] private float ldCenterScale = 2.0f;     // 일반 레벨 중앙 배율
    [SerializeField] private float ldCenterScaleFinalMul = 2.0f; // 25는 위 배율에 추가 곱(= 최종 4배)
    [SerializeField] private float ldPopOvershoot = 0.12f;
    [SerializeField] private float ldPopInTime = 0.12f;
    [SerializeField] private float ldHoldTime = 0.15f;
    [SerializeField] private float moveDuration = 0.55f;
    [SerializeField, Range(0f,1f)] private float swapToSDFraction = 0.18f;

    // ── 계층 버튼(단일) ──
    [Header("Tier UI")]
    [SerializeField] private Button tierSwitchButton;

    void Start()
    {
        curSpawnCharge = GetMaxSpawnCharge();
        UpdateSpawnButtonUI();
        StartCoroutine(AutoMergeRoutine());

        if (tierManager != null) ShowOnlyTier(tierManager.CurrentTierIndex);

        SetTierButton(false); // 기본 숨김
    }

    void Update()
    {
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

    public void OnClickSpawnButton()
    {
        if (curSpawnCharge > 0 && girlList.Count < GetMaxFieldCount())
        {
            curSpawnCharge--;
            SpawnGirl(1, (Vector3)GetRandomSpawnPos());
            UpdateSpawnButtonUI();
        }
    }

    private void TryAutoSpawn()
    {
        if (curSpawnCharge > 0 && girlList.Count < GetMaxFieldCount())
        {
            curSpawnCharge--;
            SpawnGirl(1, (Vector3)GetRandomSpawnPos());
            UpdateSpawnButtonUI();
        }
    }

    public void SpawnTestGirls()
    {
        for (int level = 1; level <= 25; level++)
            SpawnGirl(level, (Vector3)GetRandomSpawnPos());
    }

    public void ManualSpawnGirl(int level, Vector3 pos)
    {
        SpawnGirl(level, pos);
    }

    // 25 획득(합성 등): 있으면 스택↑, 없으면 생성
    public void AcquireLevel25()
    {
        var exist = GetFinalGirl();
        if (exist != null)
        {
            exist.IncrementFinalRank();
            ConfigureLevel25(exist);
        }
        else
        {
            // 새로 생성(중앙)
            SpawnGirl(TierRules.MaxLevel, Vector3.zero);
        }
        NotifySpawnedLevel(TierRules.MaxLevel);
    }

    private GirlCharacter GetFinalGirl()
    {
        for (int i = 0; i < girlList.Count; i++)
            if (girlList[i] != null && girlList[i].Level >= TierRules.MaxLevel)
                return girlList[i];
        return null;
    }

    private void SpawnGirl(int level, Vector3 pos)
    {
        if (dataManager == null) return;
        GirlData data = dataManager.GetDataByLevel(level);
        if (data == null) return;

        // 25 이미 있으면 스택만 올리고 리턴
        if (level >= TierRules.MaxLevel)
        {
            var exist = GetFinalGirl();
            if (exist != null)
            {
                exist.IncrementFinalRank();
                ConfigureLevel25(exist);
                NotifySpawnedLevel(level);
                return;
            }
        }

        // (안전) (0,0,0)은 랜덤 대체, 단 25는 중앙 유지
        if (pos == Vector3.zero && level < TierRules.MaxLevel)
        {
            Vector2 rnd = GetRandomSpawnPos();
            pos = new Vector3(rnd.x, rnd.y, 0f);
        }

        // 첫 발견 판단
        bool isFirstDiscover = !_isRestoring && !discoveredLevels.Contains(level);

        // 다른 계층이면 중앙 연출 생략
        int itemTier = TierRules.TierIndexFromLevel(level);
        if (!_isRestoring && tierManager != null && itemTier != tierManager.CurrentTierIndex)
            isFirstDiscover = false;

        // 스프라이트
        Sprite sprite = null;
        if (spriteLoader != null)
            sprite = spriteLoader.GetSpriteForData(data, preferLD: isFirstDiscover);

        // 프리팹/위치
        var go = SimpleUIPool.Instance.Get(girlRoot);
        var rect = go.transform as RectTransform;
        if (rect != null) rect.localPosition = pos;
        else go.transform.localPosition = pos;

        var girl = go.GetComponent<GirlCharacter>();
        girl.OnGetFromPool();
        girl.Init(data, sprite);
        girl.mergeManager = mergeManager;

        girlFieldAdd(girl);

        // 25면 중앙 고정/풀 사이즈 설정
        if (level >= TierRules.MaxLevel)
            ConfigureLevel25(girl);

        // 가시성 반영
        ApplyVisibilityFor(girl);

        // 첫 발견 연출
        if (isFirstDiscover && spriteLoader != null)
        {
            discoveredLevels.Add(level);

            Vector3 finalPos = rect != null ? rect.localPosition : go.transform.localPosition;

            if (!spriteLoader.IsLoadedLD)
                StartCoroutine(EnsureLDAndPlayDiscovery(girl, data, finalPos));
            else
                StartCoroutine(PlayDiscoveryOnce(girl, data, finalPos));
        }

        // 9레벨 이상 → 언락/버튼 ON
        NotifySpawnedLevel(level);
    }

    // 25 전용 화면 채우기/고정
    private void ConfigureLevel25(GirlCharacter girl)
    {
        var container = (activeParent as RectTransform) ?? (girlRoot as RectTransform) ?? (transform as RectTransform);
        girl.EnableFinalMode(container, 0.95f);
        // 위치 최종 보정(중앙)
        var rt = (RectTransform)girl.transform;
        rt.localPosition = Vector3.zero;
    }

    // 9레벨 이상 “등장했을 때만” 호출해서 언락 + 버튼 ON
    public void NotifySpawnedLevel(int level)
    {
        if (level >= 9)
        {
            tierManager?.TryUnlockByLevel(level);
            SetTierButton(true);
        }
    }

    private void SetTierButton(bool on)
    {
        if (tierSwitchButton != null)
        {
            tierSwitchButton.gameObject.SetActive(on);
            tierSwitchButton.interactable = on;
        }
    }

    private IEnumerator EnsureLDAndPlayDiscovery(GirlCharacter girl, GirlData data, Vector3 targetPos)
    {
        var task = spriteLoader.EnsureLDLoadedAsync();
        while (!task.IsCompleted) yield return null;
        yield return PlayDiscoveryOnce(girl, data, targetPos);
    }

    // 첫 발견: (일반=2배, 25=4배) 중앙 팝업 → 이동·축소 중 SD 스왑
    private IEnumerator PlayDiscoveryOnce(GirlCharacter girl, GirlData data, Vector3 targetPos)
    {
        if (girl == null || data == null) yield break;

        var rect = (RectTransform)girl.transform;

        var sd = spriteLoader.GetSpriteForData(data, preferLD: false);
        var ld = spriteLoader.GetSpriteForData(data, preferLD: true) ?? sd;

        var img = girl.GetComponentInChildren<Image>();
        if (img != null && ld != null) img.sprite = ld;

        girl.KillAllTweens();
        girl.StopAllCoroutines();

        // 25면 배율 두 배
        float baseScaleMul = ldCenterScale * (girl.Level >= TierRules.MaxLevel ? ldCenterScaleFinalMul : 1f);

        Vector3 originalScale = girl.transform.localScale;
        float centerBase = Mathf.Max(0.0001f, baseScaleMul);

        rect.localPosition = Vector3.zero;
        girl.transform.localScale = originalScale * (centerBase * (1f - ldPopOvershoot));
        if (img != null) img.color = new Color(1, 1, 1, 0);

        Sequence pop = DOTween.Sequence();
        if (img != null) pop.Join(img.DOFade(1f, ldPopInTime));
        pop.Join(girl.transform.DOScale(originalScale * (centerBase * (1f + ldPopOvershoot)), ldPopInTime)
            .SetEase(Ease.OutBack, overshoot: 1.4f));
        pop.Append(girl.transform.DOScale(originalScale * centerBase, 0.08f).SetEase(Ease.OutCubic));
        yield return pop.WaitForCompletion();

        yield return new WaitForSeconds(ldHoldTime);

        // 25는 원위치가 중앙(고정)이라 이동 없이 SD로만 스왑하고 종료
        if (girl.Level >= TierRules.MaxLevel)
        {
            if (img != null && sd != null) img.sprite = sd;
            // 25는 즉시 최종 모드 유지
            ConfigureLevel25(girl);
            girl.OnGetFromPool(); // 루프는 25라서 시작 안 됨
            rect.localPosition = Vector3.zero;
            yield break;
        }

        // 일반 레벨은 자리로 이동
        Sequence seq = DOTween.Sequence();
        seq.Join(rect.DOLocalMove(targetPos, moveDuration).SetEase(Ease.OutCubic));
        seq.Join(girl.transform.DOScale(originalScale, moveDuration).SetEase(Ease.OutCubic));

        float swapAtTime = Mathf.Clamp01(swapToSDFraction) * moveDuration;
        if (img != null && sd != null)
            seq.InsertCallback(swapAtTime, () => { if (img != null) img.sprite = sd; });

        yield return seq.WaitForCompletion();

        girl.OnGetFromPool();
        rect.localPosition = targetPos;
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

    private Vector2 GetRandomSpawnPos()
    {
        float x = Random.Range(-350f, 350f);
        float y = Random.Range(-600f, 600f);
        return new Vector2(x, y);
    }

    private int GetMaxSpawnCharge() =>
        upgradeManager != null ? upgradeManager.GetMaxManualSpawnCount() : 3;
    private float GetSpawnChargeInterval() =>
        upgradeManager != null ? upgradeManager.GetManualSpawnInterval() : 10f;
    private int GetMaxFieldCount() =>
        upgradeManager != null ? upgradeManager.GetMaxFieldCount() : 8;

    private void UpdateSpawnButtonUI() { }

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

        for (int i = prev + 1; i < 4; i++)
        {
            if (tierManager.Unlocked[i]) { target = i; break; }
        }
        if (target == -1) target = 0;

        if (!tierManager.Unlocked[target])
        {
            for (int i = 0; i < 4; i++)
            {
                if (tierManager.Unlocked[i]) { target = i; break; }
            }
        }
        if (target == prev) return;

        tierManager.SwitchTo(target);
        ToggleTwoTiers(prev, target);
    }

    private void ToggleTwoTiers(int prevTier, int targetTier)
    {
        for (int i = 0; i < girlList.Count; i++)
        {
            var g = girlList[i];
            if (g == null) continue;

            int level = g.Level;
            int itemTier = TierRules.TierIndexFromLevel(level);

            if (itemTier == prevTier) HideGirl(g);
            else if (itemTier == targetTier) ShowGirl(g);
        }
    }

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
    }

    private void HideGirl(GirlCharacter g)
    {
        if (g == null) return;
        g.gameObject.SetActive(false);
        if (hiddenParent != null && g.transform.parent != hiddenParent)
            g.transform.SetParent(hiddenParent, true);
    }

    public IReadOnlyList<GirlCharacter> AllGirls => girlList;

    // ───── ISaveable ─────
    public void CollectSaveData(SaveData data)
    {
        int mask = 0;
        foreach (var lv in discoveredLevels)
        {
            int bit = Mathf.Clamp(lv - 1, 0, 31);
            mask |= (1 << bit);
        }
        data.discoveredMask = mask;

        girlList.RemoveAll(g => g == null);
        data.girls.Clear();
        for (int i = 0; i < girlList.Count; i++)
        {
            var g = girlList[i];
            data.girls.Add(new GirlSaveInfo(g.Level));
        }
    }

    public void ApplyLoadedData(SaveData data)
    {
        if (_restoreRoutine != null) StopCoroutine(_restoreRoutine);
        _restoreRoutine = StartCoroutine(RestoreWhenReady(data));
    }

    private IEnumerator RestoreWhenReady(SaveData data)
    {
        if (GameSystem.Instance != null && !GameSystem.Instance.AssetsReady)
        {
            bool ready = false;
            void OnReady() => ready = true;
            GameSystem.Instance.AssetsReadyEvent += OnReady;

            yield return new WaitUntil(() =>
                ready ||
                ((spriteLoader == null || spriteLoader.IsReady) &&
                 (dataManager == null || dataManager.IsLoaded))
            );

            if (GameSystem.Instance != null)
                GameSystem.Instance.AssetsReadyEvent -= OnReady;
        }
        else
        {
            yield return new WaitUntil(() =>
                (spriteLoader == null || spriteLoader.IsReady) &&
                (dataManager == null || dataManager.IsLoaded)
            );
        }

        discoveredLevels.Clear();
        int mask = data.discoveredMask;
        for (int lv = 1; lv <= TierRules.MaxLevel; lv++)
        {
            int bit = lv - 1;
            if ((mask & (1 << bit)) != 0)
                discoveredLevels.Add(lv);
        }

        var snapshot = new List<GirlCharacter>(girlList);
        foreach (var g in snapshot)
        {
            if (g == null) continue;
            RemoveGirl(g);
        }
        girlList.Clear();

        _isRestoring = true;
        if (data.girls != null)
        {
            for (int i = 0; i < data.girls.Count; i++)
            {
                int level = Mathf.Clamp(data.girls[i].level, 1, TierRules.MaxLevel);
                Vector2 rnd = GetRandomSpawnPos();
                Vector3 pos = (level >= TierRules.MaxLevel) ? Vector3.zero : new Vector3(rnd.x, rnd.y, 0f);
                SpawnGirl(level, pos);
            }
        }
        _isRestoring = false;

        if (tierManager != null)
            ShowOnlyTier(tierManager.CurrentTierIndex);

        bool show = false;
        if (tierManager != null)
            show = tierManager.Unlocked[1] || tierManager.Unlocked[2] || tierManager.Unlocked[3];
        if (!show && data.girls != null)
        {
            for (int i = 0; i < data.girls.Count; i++)
                if (data.girls[i].level >= 9) { show = true; break; }
        }
        SetTierButton(show);

        _restoreRoutine = null;
    }
}
