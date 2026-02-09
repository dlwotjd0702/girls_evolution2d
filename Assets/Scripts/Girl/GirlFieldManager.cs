// ============================
// GirlFieldManager.cs (FULL, 환생 상점/계승 등급 보너스 + 상점 Plus 효과 반영 버전)
// - ComputeIdleGoldPerSec(): "계승 등급 + 환생 상점" 수익 배수 곱
// - 수동 소환 최대/쿨타임, 자동 소환/합성 주기, 필드 최대칸에 "PrestigeShop Plus" 반영
// - 외부 매니저(PrestigeShopManager/LegacyRankManager)가 없어도 리플렉션 기반 안전 동작
// ============================
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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

    [Header("Tier")]
    [SerializeField] private TierManager tierManager;
    [SerializeField] private Transform activeParent;
    [SerializeField] private Transform hiddenParent;

    [Header("Visibility")]
    [Tooltip("가시성 전환 시 부모를 바꿔 계층 정리할지 여부. On이면 월드 좌표/스케일 안전 복원.")]
    [SerializeField] private bool reparentForVisibility = false;

    private readonly HashSet<int> discoveredLevels = new HashSet<int>();
    private bool _isRestoring = false;
    private Coroutine _restoreRoutine;

    // ── 수동 소환 차지 ──
    public int  curSpawnCharge = 0;
    private float chargeTimer = 0f;

    // ── 자동 타이머 ──
    private float autoSpawnTimer = 0f;
    private float autoMergeTimer = 0f;

    // 타이머 진행률을 외부에서 가져올 수 있도록 (0.0 ~ 1.0)
    public float GetAutoSpawnTimerProgress()
    {
        if (economy == null || !economy.IsAutoSpawnOn()) return 0f;
        float interval = economy.GetAutoSpawnInterval();
        if (interval >= float.MaxValue) return 0f;
        interval = ApplyAutoSpawnMul(interval);
        if (interval >= float.MaxValue) return 0f;
        return Mathf.Clamp01(autoSpawnTimer / interval);
    }
    
    public float GetAutoMergeTimerProgress()
    {
        if (economy == null || !economy.IsAutoMergeOn() || mergeManager == null) return 0f;
        float interval = economy.GetAutoMergeInterval();
        if (interval >= float.MaxValue) return 0f;
        interval = ApplyAutoMergeMul(interval);
        if (interval >= float.MaxValue) return 0f;
        return Mathf.Clamp01(autoMergeTimer / interval);
    }

    // ── Idle 수익 지급 ──
    [Header("Idle Income")]
    [SerializeField] private float idleTickSeconds = 1.0f; // 1초 단위 지급
    private float  idleTimer = 0f;
    private double lastComputedPerSec = 0.0;

    [Header("Discovery FX")]
    [SerializeField] private float ldCenterScale = 2.0f;
    [SerializeField] private float ldCenterScaleFinalMul = 2.0f;
    [SerializeField] private float ldPopOvershoot = 0.12f;
    [SerializeField] private float ldPopInTime = 0.12f;
    [SerializeField] private float ldHoldTime = 0.15f;
    [SerializeField] private float moveDuration = 0.55f;
    [SerializeField, Range(0f,1f)] private float swapToSDFraction = 0.18f;
    [SerializeField] private GameObject discoverySpotlightPanel;
    [SerializeField] private Image discoverySpotlightImage;
    [SerializeField] private float spotlightFadeIn = 0.18f;
    [SerializeField] private float spotlightFadeOut = 0.15f; // SD 스왑 시 빠른 fade out
    [SerializeField, Range(0f,1f)] private float spotlightMaxAlpha = 0.9f;
    [SerializeField] private RectTransform discoveryPresentationRoot;
    private Tween spotlightFadeTween;

    [Header("Tier Ascend FX")]
    [SerializeField] private float tierAscendScaleMul = 1.32f;
    [SerializeField] private float tierAscendDuration = 0.35f;
    [SerializeField] private Vector2 tierAscendMoveOffset = new Vector2(0f, 240f);

    private int _lastTierShown = 0;
    public int CurrentMaxLevel { get; private set; } = 1;
    public event Action<int> OnMaxLevelChanged;

    [Header("Spawn Button UI")]
    [SerializeField] private Button          spawnButton;
    [SerializeField] private Image           chargeFillImage;  // Image Type = Filled
    [SerializeField] private TextMeshProUGUI chargeCountText;  // "cur/max"
    [SerializeField] private TextMeshProUGUI spawnReasonLabel;
    [SerializeField] private float spawnReasonShowSeconds = 1.5f;
    private Coroutine spawnReasonRoutine;

    [Header("Population UI")]
    [SerializeField] private TextMeshProUGUI populationText;
    [SerializeField] private string populationFormat = "{0}/{1}";
    
    [Header("Level 25 UI")]
    [SerializeField] private TextMeshProUGUI level25Text; // 25단계 레벨 표시 텍스트
    [SerializeField] private int level25UpgradeLevel = 0; // 25단계 강화 레벨
    public int Level25UpgradeLevel => level25UpgradeLevel;

    [Header("Gold Popup")]
    [SerializeField] private GoldGainPopupPool goldPopupPool;
    [SerializeField] private Vector2 goldPopupOffset = new Vector2(0f, 0f); // 팝업은 하단에서 시작하므로 오프셋 0

    [Header("Offline Reward")]
    [SerializeField] private OfflineRewardPanel offlineRewardPanel;
    [SerializeField] private float offlineRewardMinSeconds = 30f;

    // ── 외부 배수/Plus 리플렉션 캐시 ──
    static bool _legacyCached = false;
    static object _legacyInst;
    static MethodInfo _miLegacyIncome; // double GetIncomeMultiplier()

    static bool _shopCached = false;
    static object _shopInst;
    static MethodInfo _miShopIncome;                 // double GetIncomeMultiplier()
    static MethodInfo _miShopPlusSpawnMax;           // int    GetManualSpawnMaxPlus()
    static MethodInfo _miShopMulManualInterval;      // double GetManualSpawnIntervalMul()
    static MethodInfo _miShopMulAutoSpawn;           // double GetAutoSpawnIntervalMul()
    static MethodInfo _miShopMulAutoMerge;           // double GetAutoMergeIntervalMul()
    static MethodInfo _miShopPlusFieldMax;           // int    GetFieldMaxPlus()

    void OnEnable()
    {
        if (tierManager != null)
        {
            _lastTierShown = tierManager.CurrentTierIndex;
            tierManager.OnTierChanged  -= OnTierChangedExternal;
            tierManager.OnTierChanged  += OnTierChangedExternal;
        }

        if (spawnButton != null)
        {
            spawnButton.onClick.RemoveAllListeners();
            spawnButton.onClick.AddListener(OnClickSpawnButton);
        }
    }

    void OnDisable()
    {
        if (tierManager != null)
            tierManager.OnTierChanged -= OnTierChangedExternal;

        if (spawnButton != null)
            spawnButton.onClick.RemoveListener(OnClickSpawnButton);
    }

    void Start()
    {
        curSpawnCharge = GetMaxSpawnCharge();
        chargeTimer = 0f;
        UpdateSpawnButtonUI();
        UpdatePopulationUI();

        if (tierManager != null)
        {
            _lastTierShown = tierManager.CurrentTierIndex;
            ShowOnlyTier(_lastTierShown);
        }

        RecomputeMaxLevelAndNotify();

        if (discoverySpotlightPanel != null)
        {
            discoverySpotlightPanel.SetActive(false);
            if (discoverySpotlightImage == null)
                discoverySpotlightImage = discoverySpotlightPanel.GetComponent<Image>();
            if (discoverySpotlightImage != null)
            {
                var c = discoverySpotlightImage.color;
                c.a = 0f;
                discoverySpotlightImage.color = c;
            }
        }
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // ── 수동 차지 충전 ──
        int   maxCharge = GetMaxSpawnCharge();
        float interval  = GetSpawnChargeInterval();
        if (curSpawnCharge < maxCharge)
        {
            chargeTimer += dt;
            if (chargeTimer >= interval)
            {
                while (chargeTimer >= interval && curSpawnCharge < maxCharge)
                {
                    chargeTimer -= interval;   // 잔여 진행도 유지
                    curSpawnCharge++;
                }
            }
        }

        // ── 자동 소환 ──
        float autoS = (economy != null) ? economy.GetAutoSpawnInterval() : float.MaxValue;
        autoS = ApplyAutoSpawnMul(autoS);
        if (autoS < float.MaxValue)
        {
            autoSpawnTimer += dt;
            if (autoSpawnTimer >= autoS)
            {
                autoSpawnTimer -= autoS;
                TryAutoSpawn();
            }
        }

        // ── 자동 합성 ──
        float autoM = (economy != null) ? economy.GetAutoMergeInterval() : float.MaxValue;
        autoM = ApplyAutoMergeMul(autoM);
        if (autoM < float.MaxValue && mergeManager != null)
        {
            autoMergeTimer += dt;
            if (autoMergeTimer >= autoM)
            {
                autoMergeTimer -= autoM;
                mergeManager.TryAutoMerge();
            }
        }

        // ── Idle 수익 계산(2^ 레벨 규칙 기반) + 1초 단위 지급 ──
        lastComputedPerSec = ComputeIdleGoldPerSec();
        idleTimer += dt;
        if (idleTimer >= idleTickSeconds)
        {
            double payout = lastComputedPerSec * idleTickSeconds;
            if (payout > 0 && economy != null) economy.AddGold(payout);
            idleTimer -= idleTickSeconds;
        }

        // 골드 수익 즉시 갱신
        if (economy != null)
        {
            economy.SetGoldPerSecEstimate(lastComputedPerSec);
        }

        // UI
        UpdateSpawnButtonUI();
    }

    // 현재 필드에 존재하는 모든 캐릭터의 초당 수익 합산 (2^ 성장)
    private double ComputeIdleGoldPerSec()
    {
        if (economy == null) return 0;
        double sum = 0;
        bool finalCounted = false;
        int finalStack = Mathf.Max(1, level25UpgradeLevel);
        for (int i = 0; i < girlList.Count; i++)
        {
            var g = girlList[i];
            if (!g) continue;
            if (g.Level >= TierRules.MaxLevel)
            {
                if (!finalCounted)
                {
                    double baseFinal = economy.GetLevelIncomePerSec(TierRules.MaxLevel);
                    sum += baseFinal * finalStack;
                    finalCounted = true;
                }
                continue;
            }
            sum += economy.GetLevelIncomePerSec(g.Level);
        }

        // ◀ 계승(자동) + 상점(구매) 배수 곱 — 외부 매니저가 없어도 안전
        double mulRank = GetLegacyIncomeMulSafe();
        double mulShop = GetShopIncomeMulSafe();
        return sum * mulRank * mulShop;
    }

    // ── 외부 배수/Plus 안전 조회(1회 캐싱) ──
    static Type FindTypeByName(string name)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(name);
            if (t != null) return t;
            try
            {
                foreach (var tt in asm.GetTypes())
                    if (tt.Name == name) return tt;
            }
            catch { /* 일부 어셈블리는 GetTypes 실패 가능 */ }
        }
        return null;
    }

    static void EnsureLegacyCache()
    {
        if (_legacyCached) return;
        _legacyCached = true;
        var t = FindTypeByName("LegacyRankManager");
        if (t == null) return;
        var pi = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        _legacyInst = pi?.GetValue(null, null);
        _miLegacyIncome = t.GetMethod("GetIncomeMultiplier", BindingFlags.Public | BindingFlags.Instance);
    }
    static void EnsureShopCache()
    {
        if (_shopCached) return;
        _shopCached = true;
        var t = FindTypeByName("PrestigeShopManager");
        if (t == null) return;
        var pi = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        _shopInst = pi?.GetValue(null, null);

        // 배수
        _miShopIncome            = t.GetMethod("GetIncomeMultiplier",            BindingFlags.Public | BindingFlags.Instance);
        // Plus
        _miShopPlusSpawnMax      = t.GetMethod("GetManualSpawnMaxPlus",          BindingFlags.Public | BindingFlags.Instance);
        _miShopMulManualInterval = t.GetMethod("GetManualSpawnIntervalMul",      BindingFlags.Public | BindingFlags.Instance);
        _miShopMulAutoSpawn      = t.GetMethod("GetAutoSpawnIntervalMul",        BindingFlags.Public | BindingFlags.Instance);
        _miShopMulAutoMerge      = t.GetMethod("GetAutoMergeIntervalMul",        BindingFlags.Public | BindingFlags.Instance);
        _miShopPlusFieldMax      = t.GetMethod("GetFieldMaxPlus",                BindingFlags.Public | BindingFlags.Instance);
    }

    // ── 계승/상점 수익 배수 ──
    static double GetLegacyIncomeMulSafe()
    {
        try
        {
            EnsureLegacyCache();
            if (_legacyInst != null && _miLegacyIncome != null)
            {
                var v = _miLegacyIncome.Invoke(_legacyInst, null);
                return Convert.ToDouble(v);
            }
        }
        catch { }
        return 1.0;
    }
    static double GetShopIncomeMulSafe()
    {
        try
        {
            if (PrestigeManager.Instance != null)
            {
                return PrestigeManager.Instance.GetIncomeMultiplier();
            }
            EnsureShopCache();
            if (_shopInst != null && _miShopIncome != null)
            {
                var v = _miShopIncome.Invoke(_shopInst, null);
                return Convert.ToDouble(v);
            }
        }
        catch { }
        return 1.0;
    }

    // ── 상점 Plus: 안전 조회 ──
    static int GetShopManualSpawnMaxPlusSafe()
    {
        try
        {
            if (PrestigeManager.Instance != null)
            {
                return PrestigeManager.Instance.GetManualSpawnMaxPlus();
            }
            EnsureShopCache();
            if (_shopInst != null && _miShopPlusSpawnMax != null)
                return Convert.ToInt32(_miShopPlusSpawnMax.Invoke(_shopInst, null));
        } catch {}
        return 0;
    }
    static double GetShopManualSpawnIntervalMulSafe()
    {
        try
        {
            if (PrestigeManager.Instance != null)
            {
                return PrestigeManager.Instance.GetManualSpawnIntervalMul();
            }
            EnsureShopCache();
            if (_shopInst != null && _miShopMulManualInterval != null)
                return Convert.ToDouble(_miShopMulManualInterval.Invoke(_shopInst, null));
        } catch {}
        return 1.0;
    }
    static double GetShopAutoSpawnIntervalMulSafe()
    {
        try
        {
            if (PrestigeManager.Instance != null)
            {
                return PrestigeManager.Instance.GetAutoSpawnIntervalMul();
            }
            EnsureShopCache();
            if (_shopInst != null && _miShopMulAutoSpawn != null)
                return Convert.ToDouble(_miShopMulAutoSpawn.Invoke(_shopInst, null));
        } catch {}
        return 1.0;
    }
    static double GetShopAutoMergeIntervalMulSafe()
    {
        try
        {
            if (PrestigeManager.Instance != null)
            {
                return PrestigeManager.Instance.GetAutoMergeIntervalMul();
            }
            EnsureShopCache();
            if (_shopInst != null && _miShopMulAutoMerge != null)
                return Convert.ToDouble(_miShopMulAutoMerge.Invoke(_shopInst, null));
        } catch {}
        return 1.0;
    }
    static int GetShopFieldMaxPlusSafe()
    {
        try
        {
            if (PrestigeManager.Instance != null)
            {
                return PrestigeManager.Instance.GetFieldMaxPlus();
            }
            EnsureShopCache();
            if (_shopInst != null && _miShopPlusFieldMax != null)
                return Convert.ToInt32(_miShopPlusFieldMax.Invoke(_shopInst, null));
        } catch {}
        return 0;
    }

    public void OnClickSpawnButton()
    {
        if (curSpawnCharge > 0 && girlList.Count < GetMaxFieldCount())
        {
            curSpawnCharge = Mathf.Max(0, curSpawnCharge - 1);
            SpawnGirl(1, (Vector3)GetRandomSpawnPos());
            UpdateSpawnButtonUI();
            HideSpawnReasonImmediate();
        }
        else if (girlList.Count >= GetMaxFieldCount())
        {
            ShowSpawnReasonTemp("필드가 가득 찼습니다.");
        }
        else if (curSpawnCharge <= 0)
        {
            HideSpawnReasonImmediate();
        }
    }

    private void TryAutoSpawn()
    {
        if (curSpawnCharge > 0 && girlList.Count < GetMaxFieldCount())
        {
            curSpawnCharge = Mathf.Max(0, curSpawnCharge - 1);
            SpawnGirl(1, (Vector3)GetRandomSpawnPos());
            UpdateSpawnButtonUI();
        }
    }

    public void SpawnTestGirls()
    {
        for (int level = 1; level <= TierRules.MaxLevel; level++)
            SpawnGirl(level, (Vector3)GetRandomSpawnPos());
    }
    public void ManualSpawnGirl(int level, Vector3 pos) => SpawnGirl(level, pos);

    /// <summary>
    /// 테스트용: 특정 레벨을 즉시 한 번 소환
    /// </summary>
    public void SpawnLevelForTest(int level)
    {
        level = Mathf.Clamp(level, 1, TierRules.MaxLevel);
        SpawnGirl(level, Vector3.zero);
    }

    public void AcquireLevel25()
    {
        level25UpgradeLevel = Mathf.Max(1, level25UpgradeLevel + 1);
        var exist = GetFinalGirl();
        if (exist != null) 
        { 
            // 기존 레벨 25 캐릭터가 있으면 level25UpgradeLevel만 증가
            // 실제 캐릭터의 Level은 25로 유지 (CurrentMaxLevel도 25로 유지)
            ConfigureLevel25(exist);
            Debug.Log($"[GirlFieldManager] AcquireLevel25: 기존 레벨 25 캐릭터 유지, level25UpgradeLevel 증가 → {level25UpgradeLevel}");
        }
        else               
        { 
            SpawnGirl(TierRules.MaxLevel, Vector3.zero);
            Debug.Log($"[GirlFieldManager] AcquireLevel25: 새 레벨 25 캐릭터 생성, level25UpgradeLevel={level25UpgradeLevel}");
        }
        UpdateLevel25Text();
        NotifySpawnedLevel(TierRules.MaxLevel);
        
        // 통계 기록: 레벨 25 달성 (최초 생성 시에만)
        if (PlayStatsTracker.Instance != null && exist == null)
        {
            PlayStatsTracker.Instance.RecordLevel25Reached();
        }
    }

    private GirlCharacter GetFinalGirl()
    {
        for (int i = 0; i < girlList.Count; i++)
        {
            var g = girlList[i];
            if (g != null && g.Level >= TierRules.MaxLevel) return g;
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
        bool tierMismatch = tierManager != null && itemTier != tierManager.CurrentTierIndex;
        bool shouldShowAscendFx = !_isRestoring && tierMismatch;
        bool allowLDDiscovery = !_isRestoring && isFirstDiscover && level < TierRules.MaxLevel && spriteLoader != null;

        Sprite sprite = null;
        if (spriteLoader != null)
        {
            // 복원 중일 때는 SD 스프라이트 사용 (발견 애니메이션 없음)
            sprite = spriteLoader.GetSpriteForData(data, preferLD: _isRestoring ? false : isFirstDiscover);
        }

        var go = SimpleUIPool.Instance.Get(girlRoot);
        var rect = go.transform as RectTransform;

        // 풀에서 나올 때 초기화(부모 스케일 애니 효과 차단)
        go.transform.localScale = Vector3.one;
        go.transform.localRotation = Quaternion.identity;

        if (rect != null) rect.localPosition = pos; else go.transform.localPosition = pos;

        var girl = go.GetComponent<GirlCharacter>();
        if (girl == null)
        {
            Debug.LogError("[GirlFieldManager] SpawnGirl: GirlCharacter 컴포넌트를 찾을 수 없습니다.");
            SimpleUIPool.Instance.Return(go);
            return;
        }
        
        girl.enabled = true;

        girl.OnGetFromPool();
        girl.Init(data, sprite);
        girl.mergeManager = mergeManager;

        girlFieldAdd(girl);

        if (level >= TierRules.MaxLevel) ConfigureLevel25(girl);

        if (shouldShowAscendFx)
            ShowGirl(girl);
        else
            ApplyVisibilityFor(girl);

        Vector3 finalPos = rect != null ? rect.localPosition : go.transform.localPosition;

        if (isFirstDiscover)
            discoveredLevels.Add(level);

        if (allowLDDiscovery)
        {
            if (!spriteLoader.IsLoadedLD) StartCoroutine(EnsureLDAndPlayDiscovery(girl, data, finalPos, shouldShowAscendFx));
            else                           StartCoroutine(PlayDiscoveryOnce(girl, data, finalPos, shouldShowAscendFx));
        }
        else if (shouldShowAscendFx)
        {
            StartCoroutine(PlayTierAscendOnly(girl, finalPos, shouldShowAscendFx));
        }

        NotifySpawnedLevel(level);
        
        // 통계 기록: 소환
        if (PlayStatsTracker.Instance != null && !_isRestoring)
        {
            PlayStatsTracker.Instance.RecordSpawn();
        }
    }

    private void ConfigureLevel25(GirlCharacter girl)
    {
        var container = (activeParent as RectTransform) ?? (girlRoot as RectTransform) ?? (transform as RectTransform);
        girl.EnableFinalMode(container, 0.95f);
        var rt = (RectTransform)girl.transform;
        rt.localPosition = new Vector3(0f, 50f, 0f); // 25단계 위치를 조금 높게 설정
        
        // 25단계 레벨 표시 텍스트 업데이트
        UpdateLevel25Text();
    }
    
    private void UpdateLevel25Text()
    {
        if (level25Text == null) return;
        bool show = level25UpgradeLevel > 0 && tierManager != null && tierManager.CurrentTierIndex == 3;
        if (show)
        {
            level25Text.text = $"Lv {level25UpgradeLevel}";
            level25Text.gameObject.SetActive(true);
        }
        else
        {
            level25Text.gameObject.SetActive(false);
        }
    }

    public void NotifySpawnedLevel(int level)
    {
        UpdateMaxLevel(level);
        if (level >= 9 && tierManager != null)
            tierManager.TryUnlockByLevel(level);
    }

    public void ShowGoldPopup(GirlCharacter girl, double amount, bool isClick)
    {
        if (goldPopupPool == null || girl == null) return;
        var rt = girl.transform as RectTransform;
        if (rt == null) return;
        Vector2 anchored = rt.anchoredPosition + goldPopupOffset;
        goldPopupPool.Show(anchored, amount, isClick);
    }

    private void UpdateMaxLevel(int achievedLevel)
    {
        if (achievedLevel > CurrentMaxLevel)
        {
            CurrentMaxLevel = achievedLevel;
            OnMaxLevelChanged?.Invoke(CurrentMaxLevel);
            
            // 업적 체크는 AchievementManager에서 자동으로 처리됨
        }
    }

    public void RecomputeMaxLevelAndNotify()
    {
        int maxLv = 1;
        for (int i = 0; i < girlList.Count; i++)
        {
            if (girlList[i] == null) continue;
            if (girlList[i].Level > maxLv) maxLv = girlList[i].Level;
        }
        if (maxLv != CurrentMaxLevel)
        {
            CurrentMaxLevel = maxLv;
            OnMaxLevelChanged?.Invoke(CurrentMaxLevel);
        }
    }

    private IEnumerator EnsureLDAndPlayDiscovery(GirlCharacter girl, GirlData data, Vector3 targetPos, bool resyncVisibilityAfterFx)
    {
        var task = spriteLoader.EnsureLDLoadedAsync();
        while (!task.IsCompleted) yield return null;
        yield return PlayDiscoveryOnce(girl, data, targetPos, resyncVisibilityAfterFx);
    }

    private IEnumerator PlayDiscoveryOnce(GirlCharacter girl, GirlData data, Vector3 targetPos, bool resyncVisibilityAfterFx)
    {
        if (girl == null || data == null) yield break;

        // LD 연출 동안에는 캐릭터 클릭/드래그 입력을 막는다
        girl.SetInputLocked(true);

        var rect = (RectTransform)girl.transform;
        var sd = spriteLoader.GetSpriteForData(data, preferLD: false);
        var ld = spriteLoader.GetSpriteForData(data, preferLD: true) ?? sd;

        var img = girl.GetComponentInChildren<Image>();
        if (img != null && ld != null) img.sprite = ld;

        MoveGirlToDiscoveryCenter(rect, out var originalParent, out var originalSiblingIndex);

        SetDiscoverySpotlight(true);
        
        // 새로운 단계 발견 효과음 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDiscoverySFX();
        }

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
            RestoreGirlParent(rect, originalParent, originalSiblingIndex);
            girl.OnGetFromPool();
            ConfigureLevel25(girl);
            rect.localPosition = Vector3.zero;
            SetDiscoverySpotlight(false);
            if (resyncVisibilityAfterFx)
                ApplyVisibilityFor(girl);
            girl.SetInputLocked(false);
            yield break;
        }

        if (resyncVisibilityAfterFx)
        {
            if (img != null && sd != null) img.sprite = sd;
            FadeOutSpotlight();
            yield return PlayTierAscendFx(girl, rect, img);
            RestoreGirlParent(rect, originalParent, originalSiblingIndex);
            girl.OnGetFromPool();
            if (girl.Level >= TierRules.MaxLevel)
                ConfigureLevel25(girl);
            rect.localPosition = targetPos;
            ApplyVisibilityFor(girl);
            SetDiscoverySpotlight(false);
            girl.SetInputLocked(false);
            yield break;
        }

        Sequence seq = DOTween.Sequence();
        seq.Join(rect.DOLocalMove(targetPos, moveDuration).SetEase(Ease.OutCubic));
        seq.Join(girl.transform.DOScale(originalScale, moveDuration).SetEase(Ease.OutCubic));

        float swapAtTime = Mathf.Clamp01(swapToSDFraction) * moveDuration;
        if (img != null && sd != null)
        {
            seq.InsertCallback(swapAtTime, () => 
            { 
                if (img != null) img.sprite = sd;
                // SD로 스왑될 때 스포트라이트 빠르게 fade out
                FadeOutSpotlight();
            });
        }

        yield return seq.WaitForCompletion();

        RestoreGirlParent(rect, originalParent, originalSiblingIndex);
        girl.OnGetFromPool();
        if (girl.Level >= TierRules.MaxLevel)
            ConfigureLevel25(girl);
        rect.localPosition = targetPos;
        if (resyncVisibilityAfterFx)
            ApplyVisibilityFor(girl);
        SetDiscoverySpotlight(false);
        girl.SetInputLocked(false);
    }

    private IEnumerator PlayTierAscendOnly(GirlCharacter girl, Vector3 targetPos, bool resyncVisibilityAfterFx)
    {
        if (girl == null) yield break;
        var rect = (RectTransform)girl.transform;
        var img = girl.GetComponentInChildren<Image>();
        yield return PlayTierAscendFx(girl, rect, img);
        girl.OnGetFromPool();
        if (girl.Level >= TierRules.MaxLevel)
            ConfigureLevel25(girl);
        rect.localPosition = targetPos;
        if (resyncVisibilityAfterFx)
            ApplyVisibilityFor(girl);
    }

    private IEnumerator PlayTierAscendFx(GirlCharacter girl, RectTransform rect, Image img)
    {
        if (girl == null || rect == null) yield break;

        Vector3 startScale = girl.transform.localScale;
        Vector2 startPos = rect.anchoredPosition;

        rect.DOKill();
        girl.transform.DOKill();
        img?.DOKill();

        Sequence seq = DOTween.Sequence();
        seq.Join(rect.DOAnchorPos(startPos + tierAscendMoveOffset, tierAscendDuration).SetEase(Ease.OutCubic));
        seq.Join(girl.transform.DOScale(startScale * tierAscendScaleMul, tierAscendDuration).SetEase(Ease.OutCubic));
        if (img != null)
        {
            seq.Join(img.DOFade(0f, tierAscendDuration).SetEase(Ease.InCubic));
        }
        yield return seq.WaitForCompletion();

        rect.anchoredPosition = startPos;
    }

    private void girlFieldAdd(GirlCharacter girl)
    {
        if (!girlList.Contains(girl))
            girlList.Add(girl);

        UpdatePopulationUI();
    }

    public void RemoveGirl(GirlCharacter girl)
    {
        if (girl == null) return;
        bool wasFinal = girl.IsFinal;
        girlList.Remove(girl);
        girl.KillAllTweens();
        SimpleUIPool.Instance.Return(girl.gameObject);
        UpdatePopulationUI();
        
        // 25단계가 제거되면 레벨 텍스트 업데이트
        if (wasFinal)
        {
            UpdateLevel25Text();
        }
    }

    public void ResetLevel25Progress()
    {
        level25UpgradeLevel = 0;
        UpdateLevel25Text();
    }

    private int EstimateLevel25UpgradeLevel(SaveData data)
    {
        if (data == null || data.girls == null) return 0;
        int maxStack = 0;
        foreach (var info in data.girls)
        {
            if (info == null) continue;
            if (info.level >= TierRules.MaxLevel)
            {
                int stack = Mathf.Max(1, info.level - (TierRules.MaxLevel - 1));
                if (stack > maxStack) maxStack = stack;
            }
        }
        return maxStack;
    }

    private Vector2 GetRandomSpawnPos()
    {
        // 맵 경계 축소: 기존의 약 70%로 축소
        float x = URandom.Range(-320f, 320f);  // -350~350 -> -245~245
        float y = URandom.Range(-500f, 500f);  // -600~600 -> -420~420
        return new Vector2(x, y);
    }

    // ── Economy 값에 PrestigeShop Plus 적용 ──
    private int GetMaxSpawnCharge()
    {
        int baseVal = economy != null ? economy.GetMaxManualSpawnCount() : 3;
        return Mathf.Max(1, baseVal);
    }
    private float GetSpawnChargeInterval()
    {
        float baseVal = economy != null ? economy.GetManualSpawnInterval() : 10f;
        return Mathf.Max(0.05f, baseVal);
    }
    private int GetMaxFieldCount()
    {
        int baseVal = economy != null ? economy.GetMaxFieldCount() : 8;
        return Mathf.Max(1, baseVal);
    }

    // 자동 주기 보정
    private float ApplyAutoSpawnMul(float baseInterval)
    {
        return baseInterval;
    }
    private float ApplyAutoMergeMul(float baseInterval)
    {
        return baseInterval;
    }

    private void UpdateSpawnButtonUI()
    {
        int max = GetMaxSpawnCharge();
        curSpawnCharge = Mathf.Clamp(curSpawnCharge, 0, max);

        if (chargeCountText != null) chargeCountText.text = $"{curSpawnCharge}/{max}";

        float interval = Mathf.Max(0.0001f, GetSpawnChargeInterval());
        float fill = (curSpawnCharge >= max) ? 1f : Mathf.Clamp01(chargeTimer / interval);
        if (chargeFillImage != null) chargeFillImage.fillAmount = fill;

        if (spawnButton != null)
            spawnButton.interactable = (curSpawnCharge > 0);

        UpdatePopulationUI();
    }

    private void UpdatePopulationUI()
    {
        if (populationText == null) return;
        int max = GetMaxFieldCount();
        int current = Mathf.Min(girlList.Count, max);
        string format = string.IsNullOrEmpty(populationFormat) ? "{0}/{1}" : populationFormat;
        populationText.text = string.Format(format, current, max);
    }

    private void ShowSpawnReasonTemp(string msg)
    {
        if (!spawnReasonLabel) return;
        HideSpawnReasonImmediate();
        spawnReasonLabel.text = msg;
        spawnReasonLabel.gameObject.SetActive(true);
        if (spawnReasonShowSeconds > 0f)
            spawnReasonRoutine = StartCoroutine(HideSpawnReasonAfter(spawnReasonShowSeconds));
    }

    private IEnumerator HideSpawnReasonAfter(float sec)
    {
        yield return new WaitForSecondsRealtime(sec);
        HideSpawnReasonImmediate();
    }

    private void HideSpawnReasonImmediate()
    {
        if (!spawnReasonLabel) return;
        if (spawnReasonRoutine != null)
        {
            StopCoroutine(spawnReasonRoutine);
            spawnReasonRoutine = null;
        }
        spawnReasonLabel.text = "";
        spawnReasonLabel.gameObject.SetActive(false);
    }

    private void TryShowOfflineReward()
    {
        if (SaveManager.Instance == null || economy == null) return;

        double offlineSeconds;
        if (!SaveManager.Instance.TryConsumeOfflineSeconds(out offlineSeconds, offlineRewardMinSeconds))
            return;

        double perSec = ComputeIdleGoldPerSec();
        if (perSec <= 0) return;

        double cappedSeconds = Math.Min(offlineSeconds, economy.GetOfflineMaxSeconds());
        if (cappedSeconds <= 0) return;

        double multiplier = economy.GetOfflineRewardMultiplier();
        double reward = perSec * cappedSeconds * multiplier;
        if (reward <= 0) return;

        if (!offlineRewardPanel)
            offlineRewardPanel = FindObjectOfType<OfflineRewardPanel>(true);

        if (offlineRewardPanel != null)
        {
            var payload = new OfflineRewardPanel.Payload
            {
                rawSeconds = offlineSeconds,
                appliedSeconds = cappedSeconds,
                perSecondIncome = perSec,
                multiplier = multiplier,
                totalReward = reward
            };
            offlineRewardPanel.Show(payload);
        }
        else
        {
            economy.AddGold(reward);
            Debug.Log("[GirlFieldManager] OfflineRewardPanel이 없어 보상을 즉시 지급했습니다.");
        }
    }

    private void ToggleTwoTiers(int prevTier, int targetTier)
    {
        for (int i = 0; i < girlList.Count; i++)
        {
            var g = girlList[i];
            if (g == null) continue;

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
            if (g == null) continue;

            int itemTier = TierRules.TierIndexFromLevel(g.Level);
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
        if (!g) return;
        g.gameObject.SetActive(true);

        if (reparentForVisibility && activeParent && g.transform.parent != activeParent)
        {
            var t = g.transform;
            var worldPos = t.position;
            var worldRot = t.rotation;
            t.SetParent(activeParent, false);
            t.position = worldPos;
            t.rotation = worldRot;
            t.localScale = Vector3.one;
        }

        if (g.IsFinal)
        {
            ConfigureLevel25(g);
        }
    }

    private void HideGirl(GirlCharacter g)
    {
        if (!g) return;
        g.gameObject.SetActive(false);

        if (reparentForVisibility && hiddenParent && g.transform.parent != hiddenParent)
        {
            var t = g.transform;
            var worldPos = t.position;
            var worldRot = t.rotation;
            t.SetParent(hiddenParent, false);
            t.position = worldPos;
            t.rotation = worldRot;
            t.localScale = Vector3.one;
        }
    }

    private void OnTierChangedExternal(int newTier)
    {
        ToggleTwoTiers(_lastTierShown, newTier);
        _lastTierShown = newTier;
        UpdateLevel25Text();
    }

    // ───── 도감 접근 ─────
    public HashSet<int> GetDiscoveredLevels()
    {
        return new HashSet<int>(discoveredLevels);
    }
    
    public bool IsLevelDiscovered(int level)
    {
        return discoveredLevels.Contains(level);
    }

    // ───── ISaveable ─────
    public void CollectSaveData(SaveData data)
    {
        // 도감 해금 정보 저장 (32비트 마스크, 레벨 1~32까지 지원)
        int mask = 0;
        foreach (var lv in discoveredLevels)
        {
            int bit = Mathf.Clamp(lv - 1, 0, 31);
            mask |= (1 << bit);
        }
        data.discoveredMask = mask;

        // 최대 도달 레벨 저장
        int maxLv = 1;
        foreach (var lv in discoveredLevels)
        {
            if (lv > maxLv) maxLv = lv;
        }
        data.maxLevelReached = maxLv;
        data.level25UpgradeLevel = level25UpgradeLevel;

        // 필드의 유닛들 저장
        girlList.RemoveAll(g => g == null);
        data.girls.Clear();
        for (int i = 0; i < girlList.Count; i++)
        {
            var g = girlList[i];
            if (g != null)
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
        level25UpgradeLevel = Mathf.Max(0, data.level25UpgradeLevel);
        if (level25UpgradeLevel == 0)
            level25UpgradeLevel = EstimateLevel25UpgradeLevel(data);

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
            if (g == null) continue;
            RemoveGirl(g);
        }
        girlList.Clear();

        // 복원
        _isRestoring = true;
        if (data.girls != null && data.girls.Count > 0)
        {
            Debug.Log($"[GirlFieldManager] 복원 시작: {data.girls.Count}개 유닛");
            for (int i = 0; i < data.girls.Count; i++)
            {
                int level = Mathf.Clamp(data.girls[i].level, 1, TierRules.MaxLevel);
                Vector2 rnd = GetRandomSpawnPos();
                Vector3 pos = (level >= TierRules.MaxLevel) ? Vector3.zero : new Vector3(rnd.x, rnd.y, 0f);
                
                // 스프라이트가 로드되었는지 확인
                if (spriteLoader != null && !spriteLoader.IsReady)
                {
                    Debug.LogWarning($"[GirlFieldManager] 스프라이트 로더가 준비되지 않았습니다. 레벨 {level} 복원 건너뜀.");
                    continue;
                }
                
                SpawnGirl(level, pos);
            }
            Debug.Log($"[GirlFieldManager] 복원 완료: {girlList.Count}개 유닛 복원됨");
        }
        _isRestoring = false;

        if (tierManager != null)
        {
            _lastTierShown = tierManager.CurrentTierIndex;
            ShowOnlyTier(_lastTierShown);
        }

        RecomputeMaxLevelAndNotify();
        UpdateLevel25Text();
        
        // 광고 로딩 완료 대기 후 로딩 패널 숨기고 오프라인 보상 표시
        StartCoroutine(WaitForAdLoadingAndShowOfflineReward());
    }

    /// <summary>
    /// 광고 로딩 완료 대기 후 로딩 패널 숨기고 오프라인 보상 표시
    /// </summary>
    private IEnumerator WaitForAdLoadingAndShowOfflineReward()
    {
        // 광고 서비스 찾기
        IAdOfferService adService = null;
        var adServiceBehaviours = FindObjectsOfType<MonoBehaviour>();
        foreach (var behaviour in adServiceBehaviours)
        {
            if (behaviour is IAdOfferService service)
            {
                adService = service;
                break;
            }
        }

        // 광고 제거 유저인 경우 바로 진행
        bool adsRemoved = PremiumCurrencyManager.Instance != null && PremiumCurrencyManager.Instance.AdsRemoved;
        if (adsRemoved || adService == null)
        {
            // 광고 서비스가 없거나 광고가 제거된 경우 바로 진행
            if (GameSystem.Instance != null && GameSystem.Instance.loadingPanel != null)
            {
                GameSystem.Instance.loadingPanel.SetActive(false);
            }
            TryShowOfflineReward();
            yield break;
        }

        // 광고 로딩 완료 대기 (최대 5초)
        float elapsed = 0f;
        float timeout = 5f;
        bool adReady = false;

        while (elapsed < timeout && !adReady)
        {
            try
            {
                adReady = adService.IsRewardedReady();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GirlFieldManager] 광고 준비 상태 확인 중 오류: {e.Message}");
            }

            if (!adReady)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
        }

        // 로딩 패널 숨기기
        if (GameSystem.Instance != null && GameSystem.Instance.loadingPanel != null)
        {
            GameSystem.Instance.loadingPanel.SetActive(false);
        }

        // 오프라인 보상 표시
        TryShowOfflineReward();
    }

    private void SetDiscoverySpotlight(bool enabled)
    {
        if (!discoverySpotlightPanel) return;

        if (discoverySpotlightImage == null)
            discoverySpotlightImage = discoverySpotlightPanel.GetComponent<Image>();

        if (enabled)
        {
            if (!discoverySpotlightPanel.activeSelf)
                discoverySpotlightPanel.SetActive(true);

            if (discoverySpotlightImage != null)
            {
                spotlightFadeTween?.Kill();
                var color = discoverySpotlightImage.color;
                color.a = 0f;
                discoverySpotlightImage.color = color;
                spotlightFadeTween = discoverySpotlightImage
                    .DOFade(spotlightMaxAlpha, spotlightFadeIn)
                    .SetEase(Ease.OutQuad);
            }
        }
        else
        {
            FadeOutSpotlight();
        }
    }

    private void FadeOutSpotlight()
    {
        if (!discoverySpotlightPanel || !discoverySpotlightPanel.activeSelf) return;

        if (discoverySpotlightImage == null)
            discoverySpotlightImage = discoverySpotlightPanel.GetComponent<Image>();

        if (discoverySpotlightImage != null)
        {
            spotlightFadeTween?.Kill();
            spotlightFadeTween = discoverySpotlightImage
                .DOFade(0f, spotlightFadeOut)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    discoverySpotlightPanel.SetActive(false);
                });
        }
        else
        {
            discoverySpotlightPanel.SetActive(false);
        }
    }

    private void MoveGirlToDiscoveryCenter(RectTransform rect, out Transform originalParent, out int originalSiblingIndex)
    {
        originalParent = null;
        originalSiblingIndex = 0;
        if (rect == null) return;

        originalParent = rect.parent;
        if (originalParent != null)
            originalSiblingIndex = rect.GetSiblingIndex();

        RectTransform container = discoveryPresentationRoot
                                  ?? activeParent as RectTransform
                                  ?? girlRoot as RectTransform
                                  ?? transform as RectTransform;
        if (container != null)
        {
            rect.SetParent(container, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }
        else
        {
            rect.localPosition = Vector3.zero;
        }
    }

    private void RestoreGirlParent(RectTransform rect, Transform originalParent, int originalSiblingIndex)
    {
        if (rect == null || originalParent == null) return;

        rect.SetParent(originalParent, false);
        int maxIndex = Mathf.Max(0, rect.parent.childCount - 1);
        rect.SetSiblingIndex(Mathf.Clamp(originalSiblingIndex, 0, maxIndex));
    }
}
