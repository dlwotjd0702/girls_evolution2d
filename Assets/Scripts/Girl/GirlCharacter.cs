using UnityEngine;

public class GirlCharacter : MonoBehaviour
{
    public int level;
    public string displayName;
    public SpriteRenderer spriteRenderer;
    public GirlMergeManager mergeManager;
    public GirlData data;

    // 점프 및 이동 파라미터
    public float jumpPower = 0.38f;         // y축 점프 높이
    public float moveDistance = 0.5f;       // 한 번 점프 시 x/y축 이동량 (절대값)
    public float squatScaleY = 0.76f;       // 쪼그라들 때 Y스케일
    public float squatScaleX = 1.18f;       // 쪼그라들 때 X스케일
    public float jumpDuration = 0.32f;      // 점프 애니메이션 시간
    public float jumpIntervalMin = 7f;      // 최소 점프 간격
    public float jumpIntervalMax = 15f;     // 최대 점프 간격

    // 이동 제한
    public float minX = -2.5f, maxX = 2.5f;
    public float minY = -4.8f,    maxY = 4.8f;

    private Vector3 baseScale;
    private Vector3 basePosition;
    private Vector3 targetPosition;
    private float nextJumpTime = 0f;
    private float jumpElapsed = 0f;
    private bool isJumping = false;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        baseScale = transform.localScale;
        basePosition = transform.position;
        targetPosition = basePosition;
        ScheduleNextJump();
    }

    void Update()
    {
        if (!isJumping)
        {
            if (Time.time >= nextJumpTime)
            {
                // --- 점프 시작 ---
                // 1. 랜덤 x/y 방향 결정
                float dirX = Random.value < 0.5f ? -1f : 1f;
                float dirY = Random.value < 0.5f ? -1f : 1f;
                float moveX = dirX * Random.Range(moveDistance * 0.8f, moveDistance * 1.2f);
                float moveY = dirY * Random.Range(moveDistance * 0.5f, moveDistance * 1.5f);

                // 2. clamp로 화면 이탈 방지
                float newX = Mathf.Clamp(transform.position.x + moveX, minX, maxX);
                float newY = Mathf.Clamp(transform.position.y + moveY, minY, maxY);

                targetPosition = new Vector3(newX, newY, basePosition.z);

                // 3. 방향에 따라 스프라이트 반전(오른쪽 이동시)
                if (dirX > 0)
                    transform.localRotation = Quaternion.Euler(0, 180, 0);
                else
                    transform.localRotation = Quaternion.identity;

                isJumping = true;
                jumpElapsed = 0f;
            }
            else
            {
                transform.localScale = baseScale;
                transform.position = targetPosition; // 항상 마지막 도착지에서 대기
            }
        }
        else
        {
            jumpElapsed += Time.deltaTime;
            float t = jumpElapsed / jumpDuration;
            t = Mathf.Clamp01(t);

            // 애니메이션 곡선
            float curve = Mathf.Sin(t * Mathf.PI);

            // Scale 변화 (squat → 원래)
            float scaleY = Mathf.Lerp(squatScaleY, 1f, curve);
            float scaleX = Mathf.Lerp(squatScaleX, 1f, curve);

            // y축 점프
            float jumpPhase = Mathf.Clamp01((t - 0.18f) / 0.68f);
            float jumpOffset = Mathf.Sin(jumpPhase * Mathf.PI) * jumpPower;

            // x/y축 이동 (Lerp로 부드럽게)
            Vector3 from = basePosition;
            Vector3 to = targetPosition;
            float moveT = Mathf.SmoothStep(0, 1, t);
            Vector3 movePos = Vector3.Lerp(from, to, moveT);

            // 실제 적용
            transform.localScale = new Vector3(baseScale.x * scaleX, baseScale.y * scaleY, baseScale.z);
            transform.position = movePos + Vector3.up * jumpOffset;

            if (jumpElapsed >= jumpDuration)
            {
                isJumping = false;
                basePosition = targetPosition;
                transform.localScale = baseScale;
                transform.position = basePosition;
                ScheduleNextJump();
            }
        }
    }

    void ScheduleNextJump()
    {
        nextJumpTime = Time.time + Random.Range(jumpIntervalMin, jumpIntervalMax);
    }

    public void Init(GirlData data, Sprite sprite)
    {
        this.data = data;
        this.level = data.level;
        this.displayName = data.name;
        if (spriteRenderer != null && sprite != null)
            spriteRenderer.sprite = sprite;
    }

    void OnMouseDown()
    {
        mergeManager?.OnGirlClicked(this);
    }

    public void SetSelected(bool isSelected)
    {
        // 선택효과 미구현
    }
}
