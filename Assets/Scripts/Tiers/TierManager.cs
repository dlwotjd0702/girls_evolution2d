using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TierManager : MonoBehaviour, ISaveable
{
    // ───────────── 상태 ─────────────
    [field: SerializeField, Range(0,3)]
    public int CurrentTierIndex { get; private set; } = 0;

    // 0층은 기본 오픈
    public readonly bool[] Unlocked = new bool[4] { true, false, false, false };

    public event Action<int> OnTierChanged;
    public event Action<int> OnTierUnlocked;

    // ───────────── Ascend 버튼 ─────────────
    [Header("Ascend Button")]
    [SerializeField] private Button ascendButton;     // 상층 이동 버튼
    [SerializeField] private Image  ascendIcon;       // 버튼 아이콘
    [Tooltip("도착할 층(0~3)의 버튼 아이콘 스프라이트. 비어있으면 BG 스프라이트로 대체.")]
    [SerializeField] private Sprite[] targetTierButtonSprites = new Sprite[4];

    [SerializeField] private bool playClickAnim = true;
    [SerializeField] private float clickScale    = 0.92f;
    [SerializeField] private float clickAnimTime = 0.08f;

    // ───────────── 배경 전환 ─────────────
    [Header("Background Transition")]
    [SerializeField] private Image bgA;              // 풀스크린 Image 두 장
    [SerializeField] private Image bgB;
    [Tooltip("층(0~3)별 배경 스프라이트")]
    [SerializeField] private Sprite[] tierBackgroundSprites = new Sprite[4];

    [SerializeField] private float duration = 0.4f;
    [SerializeField] private Ease  ease     = Ease.OutCubic;
    [SerializeField] private bool  unscaledTime = true;

    [Space(4)]
    [Tooltip("다음층 시작 스케일 (1보다 크면 크게 등장)")]
    [SerializeField] private float inStartScale = 1.12f;
    [Tooltip("현재층 종료 스케일 (1보다 작게)")]
    [SerializeField] private float outEndScale  = 0.88f;
    [Tooltip("현재층이 위로 상승하는 정도(px)")]
    [SerializeField] private float riseY = 160f;
    [Tooltip("다음층이 아래에서 약간 올라오게 할 오프셋(px, 음수 권장)")]
    [SerializeField] private float nextStartYOffset = -40f;

    [Header("Init")]
    [SerializeField] private bool syncOnEnable = true;

    // 내부
    bool useA = true;
    int  lastTier = 0;
    Sequence seq;

    // ───────────── Unity ─────────────
    void Reset()
    {
        ascendButton = GetComponentInChildren<Button>(true);
        if (ascendButton) ascendIcon = ascendButton.GetComponentInChildren<Image>(true);
    }

    void Awake()
    {
        if (ascendIcon) ascendIcon.preserveAspect = true;
    }

    void OnEnable()
    {
        if (ascendButton)
        {
            ascendButton.onClick.RemoveAllListeners();
            ascendButton.onClick.AddListener(OnClickAscend);
        }

        if (syncOnEnable)
            SyncImmediate(CurrentTierIndex);

        RefreshAscendUI();
    }

    void OnDisable()
    {
        if (ascendButton) ascendButton.onClick.RemoveAllListeners();
        seq?.Kill();
    }

    // ───────────── Ascend 버튼 로직 ─────────────
    void OnClickAscend()
    {
        if (playClickAnim && ascendButton)
        {
            var t = ascendButton.transform;
            t.DOKill();
            t.DOScale(clickScale, clickAnimTime).SetEase(Ease.OutQuad)
             .OnComplete(() => t.DOScale(1f, clickAnimTime).SetEase(Ease.OutQuad));
        }

        int target = ComputeNextTierByRule();
        SwitchTo(target);
        RefreshAscendUI(); // 다음 목표 아이콘 갱신
    }

    void RefreshAscendUI()
    {
        // 규칙: 2층(=Tier 1) 언락 전까지 버튼 숨김
        bool show = Unlocked[1];
        if (ascendButton) ascendButton.gameObject.SetActive(show);
        if (!show) return;

        int target = ComputeNextTierByRule();
        var sprite = SafeSpriteButton(target) ?? SafeSpriteBG(target);
        if (ascendIcon) ascendIcon.sprite = sprite;
    }

    // ───────────── 배경 전환 ─────────────
    public void SyncImmediate(int tier)
    {
        var s = SafeSpriteBG(tier);
        if (!bgA || !bgB || !s) return;

        seq?.Kill();

        useA = true;
        lastTier = tier;

        ResetRT(bgA.rectTransform);
        ResetRT(bgB.rectTransform);

        bgA.sprite = s; bgA.color = Color.white;
        bgB.sprite = s; bgB.color = new Color(1,1,1,0);
    }

    void PlayBGTo(int toTier)
    {
        if (!bgA || !bgB) return;

        var nextSprite = SafeSpriteBG(toTier);
        if (!nextSprite || toTier == lastTier) return;

        seq?.Kill();

        var cur = useA ? bgA : bgB;
        var nxt = useA ? bgB : bgA;

        ResetRT(cur.rectTransform);
        ResetRT(nxt.rectTransform);

        cur.DOKill(); nxt.DOKill();

        nxt.sprite = nextSprite;

        // 현재층 아웃
        cur.rectTransform.localScale = Vector3.one;
        cur.rectTransform.anchoredPosition = Vector2.zero;
        cur.color = Color.white;

        // 다음층 인
        nxt.rectTransform.localScale = Vector3.one * inStartScale;
        nxt.rectTransform.anchoredPosition = new Vector2(0f, nextStartYOffset);
        nxt.color = new Color(1,1,1,0);

        seq = DOTween.Sequence().SetAutoKill(true).SetUpdate(unscaledTime);

        seq.Join(cur.rectTransform.DOScale(outEndScale, duration).SetEase(ease));
        seq.Join(cur.rectTransform.DOAnchorPosY(riseY, duration).SetEase(ease));
        seq.Join(cur.DOFade(0f, duration).SetEase(ease));

        seq.Join(nxt.rectTransform.DOScale(1f, duration).SetEase(ease));
        seq.Join(nxt.rectTransform.DOAnchorPosY(0f, duration).SetEase(ease));
        seq.Join(nxt.DOFade(1f, duration).SetEase(ease));

        seq.SetLink(gameObject);

        seq.OnComplete(() =>
        {
            cur.color = new Color(1,1,1,0);
            nxt.color = Color.white;
            nxt.rectTransform.localScale = Vector3.one;
            nxt.rectTransform.anchoredPosition = Vector2.zero;

            useA = !useA;
            lastTier = toTier;
        });
    }

    // ───────────── 티어 전환/언락/규칙 ─────────────
    public void SwitchTo(int tierIndex)
    {
        if ((uint)tierIndex > 3u) return;
        if (!Unlocked[tierIndex]) return;
        if (CurrentTierIndex == tierIndex) return;

        int prev = CurrentTierIndex;
        CurrentTierIndex = tierIndex;

        PlayBGTo(tierIndex);
        OnTierChanged?.Invoke(CurrentTierIndex);
    }

    // 레벨 기반 자동 언락(9→1층, 17→2층, 25→3층)
    public bool TryUnlockByLevel(int level)
    {
        int t = TierRules.TierIndexFromLevel(level);
        bool changed = false;
        for (int i = 0; i <= t; i++)
        {
            if (!Unlocked[i])
            {
                Unlocked[i] = true;
                OnTierUnlocked?.Invoke(i);
                changed = true;
            }
        }
        if (changed) RefreshAscendUI();
        return changed;
    }

    public bool IsUnlocked(int tier) => (uint)tier < 4u && Unlocked[tier];

    /// <summary>
    /// 규칙: 0→(1이 열렸으면 1, 아니면 0), 1→(2 열렸으면 2, 아니면 0), 2→(3 열렸으면 3, 아니면 0), 3→0
    /// </summary>
    public int ComputeNextTierByRule()
    {
        switch (CurrentTierIndex)
        {
            case 0: return IsUnlocked(1) ? 1 : 0;
            case 1: return IsUnlocked(2) ? 2 : 0;
            case 2: return IsUnlocked(3) ? 3 : 0;
            case 3: return 0;
            default: return 0;
        }
    }

    public void GoNextTierByRule()
    {
        SwitchTo(ComputeNextTierByRule());
    }

    // ───────────── 저장/복원 ─────────────
    public void CollectSaveData(SaveData data)
    {
        data.currentTierIndex = CurrentTierIndex;
        data.unlockedTierMask =
            (Unlocked[0] ? 1 : 0) |
            (Unlocked[1] ? 2 : 0) |
            (Unlocked[2] ? 4 : 0) |
            (Unlocked[3] ? 8 : 0);
    }

    public void ApplyLoadedData(SaveData data)
    {
        CurrentTierIndex = Mathf.Clamp(data.currentTierIndex, 0, 3);

        for (int i = 0; i < 4; i++)
            Unlocked[i] = (data.unlockedTierMask & (1 << i)) != 0;

        Unlocked[0] = true; // 안전장치

        SyncImmediate(CurrentTierIndex);
        RefreshAscendUI();
        OnTierChanged?.Invoke(CurrentTierIndex);
    }

    // ───────────── utils ─────────────
    void ResetRT(RectTransform rt)
    {
        if (!rt) return;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    Sprite SafeSpriteBG(int tier)
    {
        if (tierBackgroundSprites == null || tierBackgroundSprites.Length == 0) return null;
        tier = Mathf.Clamp(tier, 0, tierBackgroundSprites.Length - 1);
        return tierBackgroundSprites[tier];
    }

    Sprite SafeSpriteButton(int tier)
    {
        if (targetTierButtonSprites == null || targetTierButtonSprites.Length == 0) return null;
        tier = Mathf.Clamp(tier, 0, targetTierButtonSprites.Length - 1);
        return targetTierButtonSprites[tier];
    }
}

// ─────────────────────────────
// 규칙 테이블 (다른 코드에서 참조하므로 유지)
// ─────────────────────────────
public static class TierRules
{
    public const int MaxLevel = 25;

    /// 0층: 1~8, 1층: 9~16, 2층: 17~24, 3층: 25
    public static int TierIndexFromLevel(int level)
    {
        if (level <= 8)  return 0;
        if (level <= 16) return 1;
        if (level <= 24) return 2;
        return 3; // 25
    }
}
