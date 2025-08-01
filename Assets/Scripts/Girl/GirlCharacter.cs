using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections;

public class GirlCharacter : MonoBehaviour,
    IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    // 외부 연동
    public int level;
    public string displayName;
    public GirlMergeManager mergeManager;
    public GirlData data;

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
        KillAllTweens();
        if (imageUI != null)
            imageUI.color = originColor;
        isJumping = false; isDragging = false; highlightOn = false; wasDragged = false;
        gameObject.SetActive(true);
        // 점프 루프 시작(풀에서 나올 때만)
        if (autoRoutine != null) StopCoroutine(autoRoutine);
        autoRoutine = JumpBounceLoop();
        StartCoroutine(autoRoutine);
    }

    public void OnReturnToPool()
    {
        KillAllTweens();
        if (imageUI != null)
            imageUI.color = originColor;
        isJumping = false; isDragging = false; highlightOn = false; wasDragged = false;
        if (autoRoutine != null) StopCoroutine(autoRoutine);
        gameObject.SetActive(false);
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
        this.level = data.level;
        this.displayName = data.name;
        if (imageUI && sprite) imageUI.sprite = sprite;
        Highlight(false);
        baseScale = transform.localScale;
        targetPosition = rectT.localPosition;
        // (방향 초기화는 필요X, 그대로 유지)
    }

    void Start()
    {
        if (autoRoutine == null)
        {
            autoRoutine = JumpBounceLoop();
            StartCoroutine(autoRoutine);
        }
    }

    // ----- 점프/골드 루프 -----
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
        isJumping = true;
        // 랜덤 방향
        float dirX = Random.value < 0.5f ? -1f : 1f;
        float dirY = Random.value < 0.5f ? -1f : 1f;
        float moveX = dirX * Random.Range(moveDistance * 0.8f, moveDistance * 1.2f);
        float moveY = dirY * Random.Range(moveDistance * 0.5f, moveDistance * 1.5f);

        float newX = Mathf.Clamp(rectT.localPosition.x + moveX, minX, maxX);
        float newY = Mathf.Clamp(rectT.localPosition.y + moveY, minY, maxY);
        targetPosition = new Vector3(newX, newY, rectT.localPosition.z);

        // **좌우반전(로컬 스케일 X만, Y/기타는 그대로)**
        currentDirectionX = (dirX > 0 ? -1 : 1); // 오른쪽=-1, 왼쪽=1
        Vector3 scale = baseScale;
        scale.x *= currentDirectionX;
        transform.localScale = scale;

        // DOTween 점프
        if (jumpTween != null && jumpTween.IsActive()) jumpTween.Kill();
        jumpTween = rectT.DOLocalJump(
            targetPosition,
            jumpPower,
            1,
            jumpDuration
        ).SetEase(Ease.OutQuad)
         .OnComplete(() =>
         {
             isJumping = false;
         });
    }

    // ----- 드래그/클릭 -----
    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true; wasDragged = false;
        if (jumpTween != null && jumpTween.IsActive()) jumpTween.Kill();

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectT.parent as RectTransform, eventData.position, eventData.pressEventCamera, out localPoint);
        dragOffset = (Vector3)localPoint - rectT.localPosition;
        mergeManager?.SetDraggingGirl(this);
    }
    public void OnBeginDrag(PointerEventData eventData) { isDragging = true; }
    public void OnDrag(PointerEventData eventData)
    {
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
            mergeManager?.AddIncomeGold(this);
            BounceAnim();
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
        transform.DOKill();
        // 바운스 때도 “현재 방향” 유지
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
}
