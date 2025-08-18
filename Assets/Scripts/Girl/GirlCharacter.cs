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

    // 최종(25) 전용 랭크/모드
    public int FinalRank { get; private set; } = 0; // 25 추가 획득 시 ++
    public bool IsFinal => Level >= TierRules.MaxLevel; // 25

    // UI/이펙트
    private Image imageUI;
    private Vector3 baseScale;
    private Color originColor;
    private RectTransform rectT;
    private Tween jumpTween;

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

    // ---- 풀 입출(초기화/정리) ----
    public void OnGetFromPool()
    {
        // ✅ 풀에서 나올 때 항상 활성화 보장
        enabled = true;

        KillAllTweens();
        if (imageUI != null)
            imageUI.color = originColor;
        isJumping = false; isDragging = false; highlightOn = false; wasDragged = false;
        gameObject.SetActive(true);

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
        if (imageUI != null)
            imageUI.color = originColor;
        isJumping = false; isDragging = false; highlightOn = false; wasDragged = false;
        if (autoRoutine != null) StopCoroutine(autoRoutine);
        gameObject.SetActive(false);
        // 비활성화로 두고 싶다면 여기서 enabled=false; 를 하되,
        // OnGetFromPool에서 다시 true로 되돌리므로 안전.
    }

    public void KillAllTweens()
    {
        transform.DOKill();
        if (rectT != null) rectT.DOKill();
        if (jumpTween != null && jumpTween.IsActive()) jumpTween.Kill();
    }

    void Awake()
    {
        imageUI = GetComponent<Image>();
        if (imageUI == null)
            imageUI = GetComponentInChildren<Image>();
        originColor = imageUI ? imageUI.color : Color.white;
        rectT = GetComponent<RectTransform>();
        baseScale = transform.localScale;
        currentDirectionX = 1;
    }

    public void Init(GirlData data, Sprite sprite)
    {
        this.data = data;
        this.Level = data.level;
        this.displayName = data.name;
        if (imageUI && sprite) imageUI.sprite = sprite;
        Highlight(false);
        baseScale = transform.localScale;
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

    // ----- 점프/골드 루프 (25는 미사용) -----
    private IEnumerator JumpBounceLoop()
    {
        while (true)
        {
            while (isDragging) yield return null;

            float jumpDelay = Random.Range(jumpIntervalMin, jumpIntervalMax);
            yield return new WaitForSeconds(jumpDelay);

            while (isDragging) yield return null;

            StartJump();
            while (isJumping || isDragging) yield return null;

            float bounceDelay = Random.Range(jumpIntervalMin, jumpIntervalMax);
            yield return new WaitForSeconds(bounceDelay);

            while (isDragging) yield return null;

            if (mergeManager != null) mergeManager.AddIncomeGold(this);
            BounceAnim();
        }
    }

    void StartJump()
    {
        if (IsFinal) return; // 25는 점프 금지

        isJumping = true;
        float dirX = Random.value < 0.5f ? -1f : 1f;
        float dirY = Random.value < 0.5f ? -1f : 1f;
        float moveX = dirX * Random.Range(moveDistance * 0.8f, moveDistance * 1.2f);
        float moveY = dirY * Random.Range(moveDistance * 0.5f, moveDistance * 1.5f);

        float newX = Mathf.Clamp(rectT.localPosition.x + moveX, minX, maxX);
        float newY = Mathf.Clamp(rectT.localPosition.y + moveY, minY, maxY);
        targetPosition = new Vector3(newX, newY, rectT.localPosition.z);

        currentDirectionX = (dirX > 0 ? -1 : 1); // 오른쪽=-1, 왼쪽=1
        Vector3 scale = baseScale;
        scale.x *= currentDirectionX;
        transform.localScale = scale;

        if (jumpTween != null && jumpTween.IsActive()) jumpTween.Kill();
        jumpTween = rectT.DOLocalJump(
            targetPosition,
            jumpPower,
            1,
            jumpDuration
        ).SetEase(Ease.OutQuad)
         .OnComplete(() => { isJumping = false; });
    }

    // ----- 드래그/클릭 -----
    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsFinal) return; // 25는 드래그 금지
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
        transform.DOKill();
        Vector3 scaled = baseScale * 1.22f;
        scaled.x *= currentDirectionX;
        transform.DOScale(scaled, 0.11f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                Vector3 baseDir = baseScale;
                baseDir.x *= currentDirectionX;
                transform.DOScale(baseDir, 0.10f).SetEase(Ease.InQuad);
            });
    }

    // 25 클릭용 펄스
    public void Pulse()
    {
        transform.DOKill();
        var s0 = transform.localScale;
        var s1 = s0 * 1.06f;
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(s1, 0.08f).SetEase(Ease.OutCubic));
        seq.Append(transform.DOScale(s0, 0.09f).SetEase(Ease.InCubic));
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
            transform.localScale = Vector3.one;
        }

        if (autoRoutine != null) StopCoroutine(autoRoutine);
        isJumping = false; isDragging = false;
    }

    public double GetIncome()
    {
        double baseIncome = (data != null) ? data.incomePerSec : 0;
        if (IsFinal) return baseIncome * (1 + FinalRank);
        return baseIncome;
    }

    public void IncrementFinalRank()
    {
        FinalRank++;
        Pulse();
    }
}
