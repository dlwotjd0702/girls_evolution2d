using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TierBackgroundControllerAscend : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private TierManager tierManager; // (선택) 자동 연동
    [SerializeField] private Image imgA;              // 풀스크린 Image 2장
    [SerializeField] private Image imgB;

    [Header("Tier Sprites (0~3)")]
    [SerializeField] private Sprite[] tierSprites = new Sprite[4];

    [Header("Ascend Transition")]
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private Ease ease = Ease.OutCubic;
    [SerializeField] private bool unscaledTime = true;
    [Space(6)]
    [Tooltip("다음층 시작 스케일 (1보다 크면 크게 등장)")]
    [SerializeField] private float inStartScale = 1.12f;
    [Tooltip("현재층 종료 스케일 (1보다 작게)")]
    [SerializeField] private float outEndScale = 0.88f;
    [Tooltip("현재층이 위로 상승하는 정도(px)")]
    [SerializeField] private float riseY = 160f;
    [Tooltip("다음층이 아래에서 살짝 올라오게 할 오프셋(px, 음수 권장)")]
    [SerializeField] private float nextStartYOffset = -40f;

    [Header("Init")]
    [Tooltip("OnEnable 시 현재 층 배경을 즉시 동기화할지")]
    [SerializeField] private bool syncOnEnable = true;

    bool useA = true;     // 현재 화면에 보이는 쪽
    int  lastTier = 0;
    Sequence seq;

    void OnEnable()
    {
        // 필수 링크 가드
        if (!imgA || !imgB)
        {
            Debug.LogWarning("[TierBG] Image 레퍼런스가 비어있습니다.");
            return;
        }

        if (tierManager)
        {
            tierManager.OnTierChanged += PlayTo;
        }

        if (syncOnEnable)
        {
            int cur = (tierManager ? tierManager.CurrentTierIndex : 0);
            SyncImmediate(cur);
        }
    }

    void OnDisable()
    {
        if (tierManager) tierManager.OnTierChanged -= PlayTo;
        if (seq != null && seq.IsActive()) seq.Kill();
    }

    void Start()
    {
        // Start에서도 한 번 더 보수적으로 초기화 (씬 로딩 순서 케이스용)
        if (!imgA || !imgB) return;

        lastTier = tierManager ? tierManager.CurrentTierIndex : 0;
        SyncImmediate(lastTier);
    }

    /// <summary>
    /// 현재 층의 스프라이트 상태로 즉시 동기화(전환 없이).
    /// 씬 진입/스프라이트 교체 직후 호출하면 깔끔함.
    /// </summary>
    public void SyncImmediate(int tier)
    {
        var s = SafeSprite(tier);
        if (!s)
        {
            Debug.LogWarning($"[TierBG] Tier {tier} 스프라이트가 없습니다.");
            return;
        }

        seq?.Kill();

        // A를 화면, B를 백버퍼로 초기화
        useA = true;
        lastTier = tier;

        ResetRT(imgA.rectTransform);
        ResetRT(imgB.rectTransform);

        imgA.sprite = s; imgA.color = Color.white;
        imgB.sprite = s; imgB.color = new Color(1,1,1,0);
    }

    /// <summary>
    /// TierManager.OnTierChanged(int)와 동일 시그니처.
    /// 직접 호출해도 무방.
    /// </summary>
    public void PlayTo(int toTier)
    {
        if (!imgA || !imgB) return;

        var nextSprite = SafeSprite(toTier);
        if (!nextSprite) { Debug.LogWarning($"[TierBG] Tier {toTier} 스프라이트가 없습니다."); return; }
        if (toTier == lastTier) return;

        seq?.Kill();

        var cur = useA ? imgA : imgB;
        var nxt = useA ? imgB : imgA;

        // 초기 상태 세팅
        ResetRT(cur.rectTransform);
        ResetRT(nxt.rectTransform);

        cur.DOKill(); nxt.DOKill();

        nxt.sprite = nextSprite;

        // 현재층: 스케일 1 → outEndScale, alpha 1 → 0, Y 0 → +riseY
        cur.rectTransform.localScale = Vector3.one;
        cur.rectTransform.anchoredPosition = Vector2.zero;
        cur.color = Color.white;

        // 다음층: 스케일 inStartScale → 1, alpha 0 → 1, Y nextStartYOffset → 0
        nxt.rectTransform.localScale = Vector3.one * inStartScale;
        nxt.rectTransform.anchoredPosition = new Vector2(0f, nextStartYOffset);
        nxt.color = new Color(1,1,1,0);

        // 트윈 구성
        seq = DOTween.Sequence().SetAutoKill(true).SetUpdate(unscaledTime);

        // 현재층 아웃
        seq.Join(cur.rectTransform.DOScale(outEndScale, duration).SetEase(ease));
        seq.Join(cur.rectTransform.DOAnchorPosY(riseY, duration).SetEase(ease));
        seq.Join(cur.DOFade(0f, duration).SetEase(ease));

        // 다음층 인
        seq.Join(nxt.rectTransform.DOScale(1f, duration).SetEase(ease));
        seq.Join(nxt.rectTransform.DOAnchorPosY(0f, duration).SetEase(ease));
        seq.Join(nxt.DOFade(1f, duration).SetEase(ease));

        // (선택) 트윈 수명 오브젝트에 링크 — 객체 파괴 시 자동 Kill
        seq.SetLink(gameObject);

        seq.OnComplete(() =>
        {
            // 정리: 최종 상태 고정
            cur.color = new Color(1,1,1,0);
            nxt.color = Color.white;
            nxt.rectTransform.localScale = Vector3.one;
            nxt.rectTransform.anchoredPosition = Vector2.zero;

            useA = !useA;
            lastTier = toTier;
        });
    }

    // ───────────────── utils ─────────────────
    void ResetRT(RectTransform rt)
    {
        if (!rt) return;
        rt.pivot = new Vector2(0.5f, 0.5f); // 중앙 기준 스케일 권장
        rt.anchorMin = Vector2.zero;        // 풀스크린 고정
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    Sprite SafeSprite(int tier)
    {
        if (tierSprites == null || tierSprites.Length == 0) return null;
        tier = Mathf.Clamp(tier, 0, tierSprites.Length - 1);
        return tierSprites[tier];
    }
}
