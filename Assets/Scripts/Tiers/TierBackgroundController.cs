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

    // (선택) 전환 중 입력 막고 싶으면 오버레이 CanvasGroup 연결
    [Header("Optional")]
    [SerializeField] private CanvasGroup inputBlocker; // blocksRaycasts=true인 CG

    bool useA = true;     // 현재 화면에 보이는 쪽
    int  lastTier = 0;
    Sequence seq;

    void OnEnable()
    {
        if (tierManager) tierManager.OnTierChanged += PlayTo;
    }
    void OnDisable()
    {
        if (tierManager) tierManager.OnTierChanged -= PlayTo;
        seq?.Kill();
    }

    void Start()
    {
        lastTier = tierManager ? tierManager.CurrentTierIndex : 0;
        var s = SafeSprite(lastTier);

        imgA.sprite = s; imgA.color = Color.white;
        imgB.sprite = s; imgB.color = new Color(1,1,1,0);

        ResetRT(imgA.rectTransform);
        ResetRT(imgB.rectTransform);
    }

    // ─────────────────────────────────────────────
    // 버튼에서 GirlFieldManager.SwitchTierTo(..) 호출 뒤
    // background.PlayTo(targetTier) 호출해도 되고,
    // tierManager를 연결해두면 OnTierChanged로 자동 실행됨.
    // ─────────────────────────────────────────────
    public void PlayTo(int toTier)
    {
        var nextSprite = SafeSprite(toTier);
        if (!nextSprite) return;
        if (toTier == lastTier) return;

        seq?.Kill();
        BlockInput(true);

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
        seq = DOTween.Sequence().SetUpdate(unscaledTime);

        // 현재층 아웃
        seq.Join(cur.rectTransform.DOScale(outEndScale, duration).SetEase(ease));
        seq.Join(cur.rectTransform.DOAnchorPosY(riseY, duration).SetEase(ease));
        seq.Join(cur.DOFade(0f, duration).SetEase(ease));

        // 다음층 인
        seq.Join(nxt.rectTransform.DOScale(1f, duration).SetEase(ease));
        seq.Join(nxt.rectTransform.DOAnchorPosY(0f, duration).SetEase(ease));
        seq.Join(nxt.DOFade(1f, duration).SetEase(ease));

        seq.OnComplete(() =>
        {
            // 정리: 최종 상태 고정
            cur.color = new Color(1,1,1,0);
            nxt.color = Color.white;
            nxt.rectTransform.localScale = Vector3.one;
            nxt.rectTransform.anchoredPosition = Vector2.zero;

            useA = !useA;
            lastTier = toTier;
            BlockInput(false);
        });
    }

    // ───────────────── utils ─────────────────
    void ResetRT(RectTransform rt)
    {
        if (!rt) return;
        rt.pivot = new Vector2(0.5f, 0.5f); // 중앙 기준 스케일 권장
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

    void BlockInput(bool on)
    {
        if (!inputBlocker) return;
        inputBlocker.blocksRaycasts = on;
        inputBlocker.interactable   = on;
        inputBlocker.alpha          = on ? 0f : 0f; // 보이진 않게
    }
}
