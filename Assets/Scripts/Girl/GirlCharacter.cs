// ============================
// GirlCharacter.cs  (클릭 연타 스케일 드리프트 가드 적용)
// - 라스트 이후 "새 개체 생성 X, 레벨만 증가"를 위해 FinalRank 제거
// - 수익 = 25레벨 기준 income * (1 + (Level-25))
// - Pulse()가 연속 호출돼도 스케일이 무한히 커지지 않도록 베이스 스케일에서 시작하도록 수정
// ============================
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections;

public class GirlCharacter : MonoBehaviour,
    IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    // 외부 연동
    public int Level;
    public string displayName;
    public GirlMergeManager mergeManager;
    public GirlData data;

    // 25 이상은 라스트 모드
    public bool IsFinal => Level >= TierRules.MaxLevel;

    // 스케일 기준
    [SerializeField] private float defaultScale = 3f;

    // UI/이펙트
    private Image imageUI;
    private Vector3 baseScale;       // 방향(Flip) 제외한 기준 스케일의 절대값
    private Color originColor;
    private RectTransform rectT;
    private Tween jumpTween;
    private Tween pulseTween;        // ⬅ Pulse 전용 트윈(연타 가드용)

    // 점프/움직임
    private float jumpPower = 120f;
    private float moveDistance = 200f;
    private float jumpDuration = 0.38f;
    private float jumpIntervalMin = 2.25f;
    private float jumpIntervalMax = 4.5f;
    private float minX = -410f, maxX = 410f, minY = -780f, maxY = 780f;

    // 상태/플래그
    private Vector3 targetPosition;
    private bool isJumping = false, isDragging = false, highlightOn = false, wasDragged = false;
    private Vector3 dragOffset;
    private int currentDirectionX = 1; // 1(왼쪽), -1(오른쪽)
    private IEnumerator autoRoutine;

    // Pulse 파라미터
    [SerializeField] private float pulseUpScaleMul = 1.06f;
    [SerializeField] private float pulseUpTime     = 0.08f;
    [SerializeField] private float pulseDownTime   = 0.09f;

    // ---- 풀 입출(초기화/정리) ----
    public void OnGetFromPool()
    {
        enabled = true;
        KillAllTweens();
        if (imageUI != null) imageUI.color = originColor;
        isJumping = false; isDragging = false; highlightOn = false; wasDragged = false;
        gameObject.SetActive(true);

        // 기본 스케일 3 기준
        transform.localScale = Vector3.one * defaultScale;
        baseScale = new Vector3(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y), Mathf.Abs(transform.localScale.z));

        if (autoRoutine != null) StopCoroutine(autoRoutine);
        if (!IsFinal)
        {
            autoRoutine = JumpBounceLoop();
            StartCoroutine(autoRoutine);
        }
    }

    public void OnReturnToPool()
    {
        KillAllTweens();
        if (imageUI != null) imageUI.color = originColor;
        isJumping = false; isDragging = false; highlightOn = false; wasDragged = false;
        if (autoRoutine != null) StopCoroutine(autoRoutine);
        gameObject.SetActive(false);
    }

    public void KillAllTweens()
    {
        transform.DOKill();
        if (rectT != null) rectT.DOKill();
        if (jumpTween != null && jumpTween.IsActive()) jumpTween.Kill();
        if (pulseTween != null && pulseTween.IsActive()) pulseTween.Kill();
    }

    void Awake()
    {
        imageUI = GetComponent<Image>() ?? GetComponentInChildren<Image>();
        originColor = imageUI ? imageUI.color : Color.white;
        rectT = GetComponent<RectTransform>();
        currentDirectionX = 1;
    }

    public void Init(GirlData data, Sprite sprite)
    {
        this.data = data;
        this.Level = data.level;
        this.displayName = data.name;
        if (imageUI && sprite) imageUI.sprite = sprite;
        Highlight(false);

        // 스케일 3 기준
        transform.localScale = Vector3.one * defaultScale;
        baseScale = new Vector3(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y), Mathf.Abs(transform.localScale.z));

        targetPosition = rectT.localPosition;
    }

    void OnEnable()
    {
        if (autoRoutine != null) StopCoroutine(autoRoutine);
        if (!IsFinal)
        {
            autoRoutine = JumpBounceLoop();
            StartCoroutine(autoRoutine);
        }
    }

    private IEnumerator JumpBounceLoop()
    {
        while (true)
        {
            while (isDragging) yield return null;

            float jumpDelay = UnityEngine.Random.Range(jumpIntervalMin, jumpIntervalMax);
            yield return new WaitForSeconds(jumpDelay);

            while (isDragging) yield return null;

            StartJump();
            while (isJumping || isDragging) yield return null;

            float bounceDelay = UnityEngine.Random.Range(jumpIntervalMin, jumpIntervalMax);
            yield return new WaitForSeconds(bounceDelay);

            while (isDragging) yield return null;

            mergeManager?.AddIncomeGold(this);
            BounceAnim();
        }
    }

    void StartJump()
    {
        if (IsFinal) return;

        isJumping = true;
        float dirX = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        float dirY = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        float moveX = dirX * UnityEngine.Random.Range(moveDistance * 0.8f, moveDistance * 1.2f);
        float moveY = dirY * UnityEngine.Random.Range(moveDistance * 0.5f, moveDistance * 1.5f);

        float newX = Mathf.Clamp(rectT.localPosition.x + moveX, minX, maxX);
        float newY = Mathf.Clamp(rectT.localPosition.y + moveY, minY, maxY);
        targetPosition = new Vector3(newX, newY, rectT.localPosition.z);

        currentDirectionX = (dirX > 0 ? -1 : 1); // 오른쪽=-1, 왼쪽=1
        transform.localScale = GetDirectionalBaseScale(); // 방향 반영한 기준으로 리셋

        if (jumpTween != null && jumpTween.IsActive()) jumpTween.Kill();
        jumpTween = rectT.DOLocalJump(targetPosition, jumpPower, 1, jumpDuration)
                     .SetEase(Ease.OutQuad)
                     .OnComplete(() => { isJumping = false; });
    }

    // ----- 드래그/클릭 -----
    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsFinal) return;
        isDragging = true; wasDragged = false;
        if (jumpTween != null && jumpTween.IsActive()) jumpTween.Kill();

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectT.parent as RectTransform, eventData.position, eventData.pressEventCamera, out localPoint);
        dragOffset = (Vector3)localPoint - rectT.localPosition;
        mergeManager?.SetDraggingGirl(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsFinal) return;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (IsFinal) return;
        if (isDragging)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectT.parent as RectTransform, eventData.position, eventData.pressEventCamera, out localPoint);
            rectT.localPosition = (Vector3)localPoint - dragOffset;
            mergeManager?.UpdateMergeHighlight(this);
            wasDragged = true;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (IsFinal) return;
        isDragging = false;
        mergeManager?.TryMergeByDrag(this);
        mergeManager?.ClearDraggingGirl();
        Highlight(false);
        targetPosition = rectT.localPosition;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!wasDragged)
        {
            mergeManager?.AddIncomeGold(this); // 25 포함 클릭 수익
            Pulse();
        }
    }

    // ----- 연출 -----
    public void Highlight(bool on)
    {
        highlightOn = on;
        if (imageUI) imageUI.color = on ? Color.yellow : originColor;
    }

    void BounceAnim()
    {
        if (IsFinal) return; // 25 자동 바운스는 X (클릭만 Pulse)

        // 바운스 시작 전에 Pulse 트윈만 종료(이동/점프는 유지)
        if (pulseTween != null && pulseTween.IsActive()) pulseTween.Kill();

        // 방향 반영한 기준 스케일에서 시작 → 드리프트 방지
        transform.localScale = GetDirectionalBaseScale();

        Vector3 scaled = transform.localScale * 1.22f;
        transform.DOScale(scaled, 0.11f).SetEase(Ease.OutQuad)
                 .OnComplete(() =>
                 {
                     Vector3 baseDir = GetDirectionalBaseScale();
                     transform.DOScale(baseDir, 0.10f).SetEase(Ease.InQuad);
                 });
    }

    public void Pulse()
    {
        // 이동/점프 트윈은 유지, Pulse 트윈만 관리
        if (pulseTween != null && pulseTween.IsActive()) pulseTween.Kill();

        // ✅ 항상 방향 반영한 "기준 스케일"에서 시작해 드리프트(무한 확대) 차단
        Vector3 baseS = GetDirectionalBaseScale();
        transform.localScale = baseS;

        var up   = baseS * pulseUpScaleMul;
        var down = baseS;

        var seq = DOTween.Sequence();
        seq.Append(transform.DOScale(up,   pulseUpTime).SetEase(Ease.OutCubic));
        seq.Append(transform.DOScale(down, pulseDownTime).SetEase(Ease.InCubic));
        pulseTween = seq;
    }

    // 25 모드 진입: 가운데 고정 + 화면 채움
    public void EnableFinalMode(RectTransform container, float coverage = 0.95f)
    {
        if (!rectT) rectT = GetComponent<RectTransform>();
        if (!imageUI) imageUI = GetComponentInChildren<Image>();
        if (imageUI) imageUI.preserveAspect = true;

        rectT.anchorMin = rectT.anchorMax = new Vector2(0.5f, 0.5f);
        rectT.pivot = new Vector2(0.5f, 0.5f);
        rectT.anchoredPosition = Vector2.zero;

        if (container)
        {
            var w = container.rect.width * coverage;
            var h = container.rect.height * coverage;
            rectT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
            rectT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            transform.localScale = Vector3.one; // 화면 채움
        }

        if (autoRoutine != null) StopCoroutine(autoRoutine);
        isJumping = false; isDragging = false;
    }

    public double GetIncome()
    {
        double baseIncome = (data != null) ? data.incomePerSec : 0;
        int extra = Mathf.Max(0, Level - TierRules.MaxLevel); // 25→0, 26→1 …
        return baseIncome * (1 + extra);
    }

    // 라스트 스택: 레벨만 올림
    public void IncrementFinalLevel()
    {
        Level++;
        Pulse();
    }

    // ─── Helper: 방향(Flip) 반영한 기준 스케일 계산 ───
    private Vector3 GetDirectionalBaseScale()
    {
        // 현재 방향(부호) 기준으로 baseScale(절대값)을 재구성
        float signX = Mathf.Sign(transform.localScale.x);
        if (signX == 0f) signX = currentDirectionX >= 0 ? 1f : -1f;

        return new Vector3(baseScale.x * signX, baseScale.y, baseScale.z);
    }
}
