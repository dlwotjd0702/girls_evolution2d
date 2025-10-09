// ============================
// GirlFieldManager.cs
// - 수동 소환 버튼/차지 UI
// - 오토소환/오토합성: Unlock→토글 노출→강화(쿨타임↓)
// - 25 최초 1회만 Spawn, 이후엔 기존 25의 Level++
// ============================
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using URandom = UnityEngine.Random;

public class GirlFieldManager : MonoBehaviour, ISaveable
{
    // ── 참조 ──
    public GirlDataManager dataManager;
    public GirlSpriteAddressableLoader spriteLoader;
    public GirlMergeManager mergeManager;
    public EconomyManager economy;
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
    private bool _isRestoring = false;
    private Coroutine _restoreRoutine;

    // ── 수동 소환 게이지 ──
    public int curSpawnCharge = 0;
    private float chargeTimer = 0f;

    // ── 오토 소환 ──
    private float autoSpawnTimer = 0f;
    [Header("Auto Spawn Settings")]
    [SerializeField] private int  autoSpawnLevel = 0;         // 0=미구매, 1+=강화
    [SerializeField] private bool autoSpawnOn    = false;
    [SerializeField] private float autoSpawnBaseInterval = 6.0f;
    [SerializeField] private float autoSpawnPerLevelMul  = 0.90f;
    [SerializeField] private float autoSpawnMinInterval  = 0.8f;
    [Header("Auto Spawn Costs")]
    [SerializeField] private double autoSpawnUnlockCost      = 500;
    [SerializeField] private double autoSpawnUpgradeBaseCost = 600;
    [SerializeField] private double autoSpawnUpgradeGrowth   = 2.2;

    // ── 오토 합성 ──
    private float autoMergeTimer = 0f;
    [Header("Auto Merge Settings")]
    [SerializeField] private int  autoMergeLevel = 0;         // 0=미구매, 1+=강화
    [SerializeField] private bool autoMergeOn    = false;
    [SerializeField] private float autoMergeBaseInterval = 4.0f;
    [SerializeField] private float autoMergePerLevelMul  = 0.90f;
    [SerializeField] private float autoMergeMinInterval  = 0.5f;
    [Header("Auto Merge Costs")]
    [SerializeField] private double autoMergeUnlockCost      = 200;
    [SerializeField] private double autoMergeUpgradeBaseCost = 250;
    [SerializeField] private double autoMergeUpgradeGrowth   = 2.0;

    // ── 소환 연출 튜닝 ──
    [Header("Discovery FX")]
    [SerializeField] private float ldCenterScale = 2.0f;
    [SerializeField] private float ldCenterScaleFinalMul = 2.0f; // 25는 2배 더 → 총 4배
    [SerializeField] private float ldPopOvershoot = 0.12f;
    [SerializeField] private float ldPopInTime = 0.12f;
    [SerializeField] private float ldHoldTime = 0.15f;
    [SerializeField] private float moveDuration = 0.55f;
    [SerializeField, Range(0f,1f)] private float swapToSDFraction = 0.18f;

    // ── 내부 ──
    private int _lastTierShown = 0;

    // ── SummonPanel 의존: 현재까지 달성한 최고 레벨 ──
    public int CurrentMaxLevel { get; private set; } = 1;
    public event Action<int> OnMaxLevelChanged;

    // ── 수동 소환 버튼/차지 UI 바인딩 ── (남은초 텍스트 없음)
    [Header("Spawn Button UI")]
    [SerializeField] private Button          spawnButton;      // (선택) 인스펙터 OnClick=OnClickSpawnButton
    [SerializeField] private Image           chargeFillImage;  // Image Type=Filled (Radial 권장)
    [SerializeField] private TextMeshProUGUI chargeCountText;  // "cur/max"

    // ─────────────────────────────────────────────────────────────────────

    void OnEnable()
    {
        if (tierManager != null)
        {
            _lastTierShown = tierManager.CurrentTierIndex;
            tierManager.OnTierChanged  -= OnTierChangedExternal;
            tierManager.OnTierChanged  += OnTierChangedExternal;
        }
    }

    void OnDisable()
    {
        if (tierManager != null)
            tierManager.OnTierChanged -= OnTierChangedExternal;
    }

    void Start()
    {
        curSpawnCharge = GetMaxSpawnCharge();
        UpdateSpawnButtonUI();

        if (tierManager != null)
        {
            _lastTierShown = tierManager.CurrentTierIndex;
            ShowOnlyTier(_lastTierShown);
        }

        RecomputeMaxLevelAndNotify();
    }

    void Update()
    {
        // ── 수동 차지 충전 ──
        if (curSpawnCharge < GetMaxSpawnCharge())
        {
            chargeTimer += Time.unscaledDeltaTime;
            if (chargeTimer >= GetSpawnChargeInterval())
            {
                chargeTimer -= GetSpawnChargeInterval();
                curSpawnCharge++;
                UpdateSpawnButtonUI();
            }
        }

        // ── 오토 소환 ──
        float autoS = GetAutoSpawnInterval();
        if (autoS < float.MaxValue && autoSpawnOn)
        {
            autoSpawnTimer += Time.unscaledDeltaTime;
            if (autoSpawnTimer >= autoS)
            {
                autoSpawnTimer -= autoS;
                TryAutoSpawn();
            }
        }

        // ── 오토 합성 ──
        float autoM = GetAutoMergeInterval();
        if (autoM < float.MaxValue && autoMergeOn && mergeManager != null)
        {
            autoMergeTimer += Time.unscaledDeltaTime;
            if (autoMergeTimer >= autoM)
            {
                autoMergeTimer -= autoM;
                mergeManager.TryAutoMerge();
            }
        }
    }

    // ───────────── UI/수동 소환 ─────────────
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

    // ───────────── 오토소환 API (상점에서 호출) ─────────────
    public int  GetAutoSpawnLevel() => autoSpawnLevel;
    public bool IsAutoSpawnOn()     => autoSpawnLevel > 0 && autoSpawnOn;

    public float GetAutoSpawnInterval()
    {
        if (autoSpawnLevel <= 0 || !autoSpawnOn) return float.MaxValue;
        int lv = Mathf.Max(1, autoSpawnLevel);
        float t = autoSpawnBaseInterval * Mathf.Pow(autoSpawnPerLevelMul, lv - 1);
        return Mathf.Clamp(t, autoSpawnMinInterval, 999f);
    }

    public double GetAutoSpawnNextCost()
    {
        if (autoSpawnLevel <= 0) return autoSpawnUnlockCost;
        int nextLv = autoSpawnLevel + 1;
        return autoSpawnUpgradeBaseCost * Math.Pow(autoSpawnUpgradeGrowth, nextLv - 2);
    }

    public bool TryBuyAutoSpawn(EconomyManager eco)
    {
        if (!eco) return false;
        double cost = GetAutoSpawnNextCost();
        if (!eco.SpendGold(cost)) return false;

        if (autoSpawnLevel <= 0) { autoSpawnLevel = 1; autoSpawnOn = true; }
        else autoSpawnLevel++;
        return true;
    }
    public void SetAutoSpawnOn(bool on) { if (autoSpawnLevel <= 0) return; autoSpawnOn = on; }

    // ───────────── 오토합성 API (상점에서 호출) ─────────────
    public int  GetAutoMergeLevel() => autoMergeLevel;
    public bool IsAutoMergeOn()     => autoMergeLevel > 0 && autoMergeOn;

    public float GetAutoMergeInterval()
    {
        if (autoMergeLevel <= 0 || !autoMergeOn) return float.MaxValue;
        int lv = Mathf.Max(1, autoMergeLevel);
        float t = autoMergeBaseInterval * Mathf.Pow(autoMergePerLevelMul, lv - 1);
        return Mathf.Clamp(t, autoMergeMinInterval, 999f);
    }

    public double GetAutoMergeNextCost()
    {
        if (autoMergeLevel <= 0) return autoMergeUnlockCost;
        int nextLv = autoMergeLevel + 1;
        return autoMergeUpgradeBaseCost * Math.Pow(autoMergeUpgradeGrowth, nextLv - 2);
    }

    public bool TryBuyAutoMerge(EconomyManager eco)
    {
        if (!eco) return false;
        double cost = GetAutoMergeNextCost();
        if (!eco.SpendGold(cost)) return false;

        if (autoMergeLevel <= 0) { autoMergeLevel = 1; autoMergeOn = true; }
        else autoMergeLevel++;
        return true;
    }
    public void SetAutoMergeOn(bool on) { if (autoMergeLevel <= 0) return; autoMergeOn = on; }

    // ───────────── 생성/발견/티어 ─────────────
    public void SpawnTestGirls()
    {
        for (int level = 1; level <= TierRules.MaxLevel; level++)
            SpawnGirl(level, (Vector3)GetRandomSpawnPos());
    }

    public void ManualSpawnGirl(int level, Vector3 pos) => SpawnGirl(level, pos);

    // 25 획득: 최초 1회만 생성, 그 이후엔 기존 25의 Level 증가
    public void AcquireLevel25()
    {
        var exist = GetFinalGirl();
        if (exist != null)
        {
            exist.IncrementFinalLevel();   // Level++
            ConfigureLevel25(exist);      // 중앙 고정/화면 채움 유지
        }
        else
        {
            SpawnGirl(TierRules.MaxLevel, Vector3.zero); // 첫 생성만
        }
        NotifySpawnedLevel(TierRules.MaxLevel);
    }

    private GirlCharacter GetFinalGirl()
    {
        for (int i = 0; i < girlList.Count; i++)
        {
            var g = girlList[i];
            if (g != null && g.Level >= TierRules.MaxLevel)
                return g;
        }
        return null;
    }

    private void SpawnGirl(int level, Vector3 pos)
    {
        if (dataManager == null) return;
        GirlData data = dataManager.GetDataByLevel(Mathf.Clamp(level, 1, TierRules.MaxLevel));
        if (data == null) return;

        if (level < TierRules.MaxLevel && pos == Vector3.zero)
        {
            Vector2 rnd = GetRandomSpawnPos();
            pos = new Vector3(rnd.x, rnd.y, 0f);
        }

        bool isFirstDiscover = !_isRestoring && !discoveredLevels.Contains(level);
        int itemTier = TierRules.TierIndexFromLevel(level);
        if (!_isRestoring && tierManager != null && itemTier != tierManager.CurrentTierIndex)
            isFirstDiscover = false;

        Sprite sprite = null;
        if (spriteLoader != null)
            sprite = spriteLoader.GetSpriteForData(data, preferLD: isFirstDiscover);

        var go = SimpleUIPool.Instance.Get(girlRoot);
        var rect = go.transform as RectTransform;
        if (rect != null) rect.localPosition = pos;
        else go.transform.localPosition = pos;

        var girl = go.GetComponent<GirlCharacter>();
        girl.enabled = true;

        girl.OnGetFromPool();
        girl.Init(data, sprite);
        girl.mergeManager = mergeManager;

        girlFieldAdd(girl);

        if (level >= TierRules.MaxLevel)
            ConfigureLevel25(girl);

        ApplyVisibilityFor(girl);

        if (isFirstDiscover && spriteLoader != null)
        {
            discoveredLevels.Add(level);
            Vector3 finalPos = rect != null ? rect.localPosition : go.transform.localPosition;

            if (!spriteLoader.IsLoadedLD)
                StartCoroutine(EnsureLDAndPlayDiscovery(girl, data, finalPos));
            else
                StartCoroutine(PlayDiscoveryOnce(girl, data, finalPos));
        }

        NotifySpawnedLevel(level);
    }

    private void ConfigureLevel25(GirlCharacter girl)
    {
        var container = (activeParent as RectTransform) ?? (girlRoot as RectTransform) ?? (transform as RectTransform);
        girl.EnableFinalMode(container, 0.95f);
        var rt = (RectTransform)girl.transform;
        rt.localPosition = Vector3.zero;
    }

    public void NotifySpawnedLevel(int level)
    {
        UpdateMaxLevel(level);

        if (level >= 9)
            tierManager?.TryUnlockByLevel(level);
    }

    private void UpdateMaxLevel(int achievedLevel)
    {
        if (achievedLevel > CurrentMaxLevel)
        {
            CurrentMaxLevel = achievedLevel;
            OnMaxLevelChanged?.Invoke(CurrentMaxLevel);
        }
    }

    private void RecomputeMaxLevelAndNotify()
    {
        int maxLv = 1;
        for (int i = 0; i < girlList.Count; i++)
        {
            if (!girlList[i]) continue;
            if (girlList[i].Level > maxLv) maxLv = girlList[i].Level;
        }
        if (maxLv != CurrentMaxLevel)
        {
            CurrentMaxLevel = maxLv;
            OnMaxLevelChanged?.Invoke(CurrentMaxLevel);
        }
    }

    private IEnumerator EnsureLDAndPlayDiscovery(GirlCharacter girl, GirlData data, Vector3 targetPos)
    {
        var task = spriteLoader.EnsureLDLoadedAsync();
        while (!task.IsCompleted) yield return null;
        yield return PlayDiscoveryOnce(girl, data, targetPos);
    }

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

        float baseScaleMul = ldCenterScale * (girl.Level >= TierRules.MaxLevel ? ldCenterScaleFinalMul : 1f);

        Vector3 originalScale = girl.transform.localScale;
        float centerBase = Mathf.Max(0.0001f, baseScaleMul);

        rect.localPosition = Vector3.zero;
        girl.transform.localScale = originalScale * (centerBase * (1f - ldPopOvershoot));
        if (img != null) img.color = new Color(1, 1, 1, 0);

        Sequence pop = DOTween.Sequence();
        if (img != null) pop.Join(img.DOFade(1f, ldPopInTime));
        pop.Join(girl.transform.DOScale(originalScale * (centerBase * (1f + ldPopOvershoot)), ldPopInTime).SetEase(Ease.OutBack, overshoot: 1.4f));
        pop.Append(girl.transform.DOScale(originalScale * centerBase, 0.08f).SetEase(Ease.OutCubic));
        yield return pop.WaitForCompletion();

        yield return new WaitForSeconds(ldHoldTime);

        if (girl.Level >= TierRules.MaxLevel)
        {
            if (img != null && sd != null) img.sprite = sd;
            ConfigureLevel25(girl);
            girl.OnGetFromPool();
            rect.localPosition = Vector3.zero;
            yield break;
        }

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
        float x = URandom.Range(-350f, 350f);
        float y = URandom.Range(-600f, 600f);
        return new Vector2(x, y);
    }

    private int GetMaxSpawnCharge()        => economy != null ? economy.GetMaxManualSpawnCount() : 3;
    private float GetSpawnChargeInterval() => economy != null ? economy.GetManualSpawnInterval() : 10f;
    private int GetMaxFieldCount()         => economy != null ? economy.GetMaxFieldCount() : 8;

    // ▼ 수동 소환 버튼/차지 UI 갱신
    private void UpdateSpawnButtonUI()
    {
        int max = GetMaxSpawnCharge();
        curSpawnCharge = Mathf.Clamp(curSpawnCharge, 0, max);

        if (chargeCountText)
            chargeCountText.text = $"{curSpawnCharge}/{max}";

        float interval = Mathf.Max(0.0001f, GetSpawnChargeInterval());
        float fill = (curSpawnCharge >= max) ? 1f : Mathf.Clamp01(chargeTimer / interval);
        if (chargeFillImage)
            chargeFillImage.fillAmount = fill;

        if (spawnButton)
            spawnButton.interactable = (curSpawnCharge > 0) && (girlList.Count < GetMaxFieldCount());
    }

    // ── 티어 토글/표시 ──
    private void ToggleTwoTiers(int prevTier, int targetTier)
    {
        for (int i = 0; i < girlList.Count; i++)
        {
            var g = girlList[i];
            if (!g) continue;

            int itemTier = TierRules.TierIndexFromLevel(g.Level);

            if (itemTier == prevTier) HideGirl(g);
            else if (itemTier == targetTier) ShowGirl(g);
        }
    }

    private void ShowOnlyTier(int tierIndex)
    {
        for (int i = 0; i < girlList.Count; i++)
        {
            var g = girlList[i];
            if (!g) continue;

            int itemTier = TierRules.TierIndexFromLevel(g.Level);
            if (itemTier == tierIndex) ShowGirl(g);
            else HideGirl(g);
        }
    }

    private void ApplyVisibilityFor(GirlCharacter girl)
    {
        if (!tierManager || !girl) return;
        int itemTier = TierRules.TierIndexFromLevel(girl.Level);
        if (itemTier == tierManager.CurrentTierIndex) ShowGirl(girl);
        else HideGirl(girl);
    }

    private void ShowGirl(GirlCharacter g)
    {
        if (!g) return;
        g.gameObject.SetActive(true);
        if (activeParent && g.transform.parent != activeParent)
            g.transform.SetParent(activeParent, true);
    }

    private void HideGirl(GirlCharacter g)
    {
        if (!g) return;
        g.gameObject.SetActive(false);
        if (hiddenParent && g.transform.parent != hiddenParent)
            g.transform.SetParent(hiddenParent, true);
    }

    private void OnTierChangedExternal(int newTier)
    {
        ToggleTwoTiers(_lastTierShown, newTier);
        _lastTierShown = newTier;
    }

    // ───── ISaveable ─────
    public void CollectSaveData(SaveData data)
    {
        // 발견 마스크
        int mask = 0;
        foreach (var lv in discoveredLevels)
        {
            int bit = Mathf.Clamp(lv - 1, 0, 31);
            mask |= (1 << bit);
        }
        data.discoveredMask = mask;

        // 필드 캐릭터 레벨 목록
        girlList.RemoveAll(g => g == null);
        data.girls.Clear();
        for (int i = 0; i < girlList.Count; i++)
        {
            var g = girlList[i];
            data.girls.Add(new GirlSaveInfo(g.Level));
        }

        // 자동화 상태 저장
        data.autoSpawnUpgrade = Mathf.Max(0, autoSpawnLevel);
        data.autoSpawnOn      = autoSpawnOn;
        data.autoMergeUpgrade = Mathf.Max(0, autoMergeLevel);
        data.autoMergeOn      = autoMergeOn;
    }

    public void ApplyLoadedData(SaveData data)
    {
        if (_restoreRoutine != null) StopCoroutine(_restoreRoutine);
        _restoreRoutine = StartCoroutine(RestoreWhenReady(data));
    }

    private IEnumerator RestoreWhenReady(SaveData data)
    {
        // 에셋 준비 대기
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

        // 첫 발견 복원
        var discovered = discoveredLevels;
        discovered.Clear();
        int mask = data.discoveredMask;
        for (int lv = 1; lv <= TierRules.MaxLevel; lv++)
        {
            int bit = lv - 1;
            if ((mask & (1 << bit)) != 0)
                discovered.Add(lv);
        }

        // 기존 필드 정리
        var snapshot = new List<GirlCharacter>(girlList);
        foreach (var g in snapshot)
        {
            if (!g) continue;
            RemoveGirl(g);
        }
        girlList.Clear();

        // 필드 복원
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

        // 자동화 상태 복원
        autoSpawnLevel = Mathf.Max(0, data.autoSpawnUpgrade);
        autoSpawnOn    = data.autoSpawnOn && autoSpawnLevel > 0;

        autoMergeLevel = Mathf.Max(0, data.autoMergeUpgrade);
        autoMergeOn    = data.autoMergeOn && autoMergeLevel > 0;

        if (tierManager != null)
        {
            _lastTierShown = tierManager.CurrentTierIndex;
            ShowOnlyTier(_lastTierShown);
        }

        RecomputeMaxLevelAndNotify();
    }
}
