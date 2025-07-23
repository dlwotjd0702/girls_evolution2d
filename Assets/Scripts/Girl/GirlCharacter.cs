using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GirlCharacter : MonoBehaviour
{
    public int level;
    public string displayName;
    public GirlMergeManager mergeManager;
    public GirlData data;

    private SpriteRenderer spriteRenderer;
    private PolygonCollider2D polyCollider;

    // 점프/이동 파라미터
    public float jumpPower = 0.38f;
    public float moveDistance = 0.5f;
    public float squatScaleY = 0.76f, squatScaleX = 1.18f;
    public float jumpDuration = 0.32f;
    public float jumpIntervalMin = 7f, jumpIntervalMax = 15f;
    public float minX = -2.5f, maxX = 2.5f, minY = -4.8f, maxY = 4.8f;

    private Vector3 baseScale;
    private Vector3 targetPosition;
    private float nextJumpTime = 0f, jumpElapsed = 0f;
    private bool isJumping = false, isDragging = false, highlightOn = false, wasDragged = false;
    private Vector3 dragOffset;
    private Color originColor;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        polyCollider = GetComponent<PolygonCollider2D>();
        originColor = spriteRenderer.color;
    }

    void Start()
    {
        targetPosition = transform.position;
        ScheduleNextJump();
        // baseScale = transform.localScale; // 이제 Init에서 처리
    }

    void Update()
    {
        if (isDragging) return;

        if (!isJumping)
        {
            if (Time.time >= nextJumpTime)
            {
                float dirX = Random.value < 0.5f ? -1f : 1f;
                float dirY = Random.value < 0.5f ? -1f : 1f;
                float moveX = dirX * Random.Range(moveDistance * 0.8f, moveDistance * 1.2f);
                float moveY = dirY * Random.Range(moveDistance * 0.5f, moveDistance * 1.5f);

                float newX = Mathf.Clamp(transform.position.x + moveX, minX, maxX);
                float newY = Mathf.Clamp(transform.position.y + moveY, minY, maxY);
                targetPosition = new Vector3(newX, newY, transform.position.z);

                transform.localRotation = dirX > 0 ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
                isJumping = true; jumpElapsed = 0f;
            }
            else
            {
                transform.localScale = baseScale;
                transform.position = targetPosition;
            }
        }
        else
        {
            jumpElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(jumpElapsed / jumpDuration);

            float curve = Mathf.Sin(t * Mathf.PI);
            float scaleY = Mathf.Lerp(squatScaleY, 1f, curve);
            float scaleX = Mathf.Lerp(squatScaleX, 1f, curve);
            float jumpPhase = Mathf.Clamp01((t - 0.18f) / 0.68f);
            float jumpOffset = Mathf.Sin(jumpPhase * Mathf.PI) * jumpPower;

            Vector3 from = transform.position;
            Vector3 to = targetPosition;
            float moveT = Mathf.SmoothStep(0, 1, t);
            Vector3 movePos = Vector3.Lerp(from, to, moveT);

            transform.localScale = new Vector3(baseScale.x * scaleX, baseScale.y * scaleY, baseScale.z);
            transform.position = movePos + Vector3.up * jumpOffset;

            if (jumpElapsed >= jumpDuration)
            {
                isJumping = false;
                transform.localScale = baseScale;
                transform.position = targetPosition;
                ScheduleNextJump();
            }
        }
    }

    void ScheduleNextJump() => nextJumpTime = Time.time + Random.Range(jumpIntervalMin, jumpIntervalMax);

    public void Init(GirlData data, Sprite sprite)
    {
        this.data = data;
        this.level = data.level;
        this.displayName = data.name;
        if (spriteRenderer && sprite) spriteRenderer.sprite = sprite;
        if (polyCollider && sprite) UpdateColliderToSprite(polyCollider, sprite);

        // ★ 반드시 현재 스케일을 baseScale로 저장!
        baseScale = transform.localScale;
    }
    void UpdateColliderToSprite(PolygonCollider2D col, Sprite sprite)
    {
        col.pathCount = sprite.GetPhysicsShapeCount();
        var path = new List<Vector2>();
        for (int i = 0; i < col.pathCount; i++)
        {
            path.Clear();
            sprite.GetPhysicsShape(i, path);
            col.SetPath(i, path.ToArray());
        }
    }

    // --------- 드래그 & 클릭 ---------
    void OnMouseDown()
    {
        isDragging = true; wasDragged = false;
        dragOffset = transform.position - ScreenToWorld(Input.mousePosition);
        mergeManager?.SetDraggingGirl(this);
    }
    void OnMouseDrag()
    {
        if (isDragging)
        {
            Vector3 worldPos = ScreenToWorld(Input.mousePosition);
            worldPos.z = 0;
            transform.position = worldPos + dragOffset;
            mergeManager?.UpdateMergeHighlight(this);
            wasDragged = true;
        }
    }
    void OnMouseUp()
    {
        if (isDragging)
        {
            isDragging = false;
            mergeManager?.TryMergeByDrag(this);
            mergeManager?.ClearDraggingGirl();
            Highlight(false);
            targetPosition = transform.position;

            if (!wasDragged)
            {
                // 클릭시 골드+애니
                mergeManager?.AddIncomeGold(this);
                StopAllCoroutines();
                StartCoroutine(BounceAnim());
            }
        }
    }

    // --------- 연출 ---------
    public void Highlight(bool on)
    {
        highlightOn = on;
        spriteRenderer.color = on ? Color.yellow : originColor;
    }

    IEnumerator BounceAnim()
    {
        float duration = 0.16f;
        float peak = 1.25f;
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            float s = Mathf.Lerp(1, peak, p < 0.5f ? p * 2 : 2 - p * 2);
            transform.localScale = baseScale * s;
            yield return null;
        }
        transform.localScale = baseScale;
    }

    Vector3 ScreenToWorld(Vector3 screenPos)
    {
        var v = Camera.main.ScreenToWorldPoint(screenPos);
        v.z = 0;
        return v;
    }
}
