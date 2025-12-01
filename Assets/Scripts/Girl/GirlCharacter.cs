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
    [SerializeField] private float finalModeScale = 9f;

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
    private float jumpIntervalMin = 4.5f; // 2.25f * 2
    private float jumpIntervalMax = 9.0f; // 4.5f * 2
    private float minX = -450f, maxX = 450f, minY = -670f, maxY = 670f;

    // 상태/플래그 (인스펙터에서 직접 관찰/디버그 가능하도록 SerializeField)
    [SerializeField] private Vector3 targetPosition;
    [SerializeField] private bool isJumping = false;
    [SerializeField] private bool isDragging = false;
    [SerializeField] private bool highlightOn = false;
    [SerializeField] private bool inputLocked = false; // LD 연출 등으로 입력 막을 때 사용
    private Vector3 dragOffset;
    private int currentDirectionX = 1; // 1(왼쪽), -1(오른쪽)
    private IEnumerator autoRoutine;
    private Tween idleGrooveTween;  // Idle 그루브 애니메이션

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
        isJumping = false;
        isDragging = false;
        highlightOn = false;
        inputLocked = false;
        gameObject.SetActive(true);

        // 기본 스케일 3 기준
        float targetScale = Mathf.Max(0.01f, defaultScale);
        var initialScale = Vector3.one * targetScale;
        transform.localScale = initialScale;
        baseScale = new Vector3(Mathf.Abs(initialScale.x), Mathf.Abs(initialScale.y), Mathf.Abs(initialScale.z));
        ResetScaleToBase();
        
        // 초기 방향 설정
        currentDirectionX = 1; // 기본 왼쪽 방향
        ApplyDirectionToOrientation();

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
        isJumping = false;
        isDragging = false;
        highlightOn = false;
        inputLocked = false;
        if (autoRoutine != null) StopCoroutine(autoRoutine);
        transform.localScale = Vector3.one;
        baseScale = Vector3.one;
        gameObject.SetActive(false);
    }

    public void KillAllTweens()
    {
        transform.DOKill();
        if (rectT != null) rectT.DOKill();
        if (jumpTween != null && jumpTween.IsActive()) jumpTween.Kill();
        if (pulseTween != null && pulseTween.IsActive()) pulseTween.Kill();
        if (idleGrooveTween != null && idleGrooveTween.IsActive()) idleGrooveTween.Kill();
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
        ResetScaleToBase();

        // 데이터 기반 초기 방향 (1=왼쪽, -1=오른쪽)
        currentDirectionX = (data != null && data.initialDirection < 0) ? -1 : 1;
        ApplyDirectionToOrientation();

        targetPosition = rectT.localPosition;
        
        // 25단계는 움직임 루프 시작하지 않음
        if (IsFinal && autoRoutine != null)
        {
            StopCoroutine(autoRoutine);
            autoRoutine = null;
        }
    }

    void OnEnable()
    {
        if (autoRoutine != null) StopCoroutine(autoRoutine);
        // 25단계는 움직임 루프 시작하지 않음
        if (IsFinal) return;
        if (!IsFinal)
        {
            autoRoutine = JumpBounceLoop();
            StartCoroutine(autoRoutine);
        }
    }

    private IEnumerator JumpBounceLoop()
    {
        // 초기 Idle 그루브 시작
        StartIdleGroove();

        // 코루틴 내부에서 박스/할당을 줄이기 위해 WaitForSeconds는 한 번만 생성해서 재사용
        WaitForSeconds cachedDelay = null;

        while (true)
        {
            while (isDragging) yield return null;

            float jumpDelay = UnityEngine.Random.Range(jumpIntervalMin, jumpIntervalMax);
            cachedDelay ??= new WaitForSeconds(jumpDelay);
            yield return cachedDelay;
            cachedDelay = null;

            while (isDragging) yield return null;

            StartJump();
            while (isJumping || isDragging) yield return null;

            float bounceDelay = UnityEngine.Random.Range(jumpIntervalMin, jumpIntervalMax);
            cachedDelay ??= new WaitForSeconds(bounceDelay);
            yield return cachedDelay;
            cachedDelay = null;

            while (isDragging) yield return null;

            mergeManager?.AddIncomeGold(this);
            BounceAnim();
        }
    }
    
    // Idle 상태 미세한 그루브 애니메이션
    private void StartIdleGroove()
    {
        if (isJumping || isDragging) return;
        
        if (idleGrooveTween != null && idleGrooveTween.IsActive()) return;
        
        Vector3 baseS = GetOrientedBaseScale();
        float grooveAmount = 0.03f; // 3% 미세한 변화
        
        // 각 단계의 속도는 이전과 같게 유지 (2.5초 기준)
        float step1Duration = 1.25f; // 2.5 * 0.5
        float step2Duration = 1.25f; // 2.5 * 0.5
        float step3Duration = 0.75f; // 2.5 * 0.3
        
        // 전체 그루브 시간을 더 길게 (루프 사이 대기 시간 추가)
        float pauseBetweenLoops = 3.0f + UnityEngine.Random.Range(-0.5f, 0.5f); // 루프 사이 대기 시간
        
        // 미세한 스케일 변화와 약간의 회전
        Sequence groove = DOTween.Sequence();
        
        // 스케일: 약간 커졌다 작아졌다 (속도는 이전과 동일)
        groove.Append(transform.DOScale(baseS * (1f + grooveAmount), step1Duration).SetEase(Ease.InOutSine));
        groove.Append(transform.DOScale(baseS * (1f - grooveAmount * 0.7f), step2Duration).SetEase(Ease.InOutSine));
        groove.Append(transform.DOScale(baseS, step3Duration).SetEase(Ease.InOutSine));
        
        // 루프 사이 대기 시간 추가 (전체 그루브 시간을 더 길게)
        groove.AppendInterval(pauseBetweenLoops);
        
        // 무한 반복
        groove.SetLoops(-1, LoopType.Restart);
        groove.SetUpdate(true); // TimeScale 무시
        
        idleGrooveTween = groove;
    }

    void StartJump()
    {
        if (IsFinal) return;

        isJumping = true;

        // Idle 그루브 애니메이션 중지
        if (idleGrooveTween != null && idleGrooveTween.IsActive()) idleGrooveTween.Kill();

        // --- 가장자리 보정: 가장자리에 가까울수록 "안쪽"으로 뛸 확률을 높임 ---
        float curX = rectT.localPosition.x;
        float curY = rectT.localPosition.y;

        // 0(왼쪽/아래) ~ 1(오른쪽/위) 범위로 정규화
        float nx = Mathf.InverseLerp(minX, maxX, curX);
        float ny = Mathf.InverseLerp(minY, maxY, curY);

        // 중앙(0.5)일 때는 50:50, 왼쪽/아래로 갈수록 오른쪽/위로 뛸 확률을 0.8까지 올리고
        // 오른쪽/위로 갈수록 0.2까지 낮춤 (약한 편향)
        float probRight = Mathf.Lerp(0.8f, 0.2f, nx); // 왼쪽에 있을수록 오른쪽으로 많이 튐
        float probUp    = Mathf.Lerp(0.8f, 0.2f, ny); // 아래에 있을수록 위로 많이 튐

        float dirX = (UnityEngine.Random.value < probRight) ?  1f : -1f;
        float dirY = (UnityEngine.Random.value < probUp)    ?  1f : -1f;

        float moveX = dirX * UnityEngine.Random.Range(moveDistance * 0.8f, moveDistance * 1.2f);
        float moveY = dirY * UnityEngine.Random.Range(moveDistance * 0.5f, moveDistance * 1.5f);

        float newX = Mathf.Clamp(rectT.localPosition.x + moveX, minX, maxX);
        float newY = Mathf.Clamp(rectT.localPosition.y + moveY, minY, maxY);
        targetPosition = new Vector3(newX, newY, rectT.localPosition.z);

        // 이동 방향에 따라 스프라이트 Flip 적용
        int newDirection = (dirX > 0) ? -1 : 1; // 오른쪽=-1, 왼쪽=1
        if (newDirection != currentDirectionX)
        {
            currentDirectionX = newDirection;
            ResetScaleToBase();
            ApplyDirectionToOrientation(); // 방향 즉시 반영
        }

        if (jumpTween != null && jumpTween.IsActive()) jumpTween.Kill();
        
        // 점프 애니메이션 개선: 더 자연스러운 곡선과 타이밍
        jumpTween = rectT.DOLocalJump(targetPosition, jumpPower, 1, jumpDuration)
                     .SetEase(Ease.OutCubic)  // OutQuad -> OutCubic으로 변경하여 더 부드럽게
                     .OnComplete(() => 
                     { 
                         isJumping = false;
                         // 점프 완료 후 Idle 그루브 재시작
                         StartIdleGroove();
                     });
    }

    // ----- 입력 잠금 제어 -----
    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;
    }

    public bool IsInputLocked => inputLocked;

    // ----- 드래그/클릭 -----
    public void OnPointerDown(PointerEventData eventData)
    {
        // LD 연출 등으로 입력이 잠긴 경우 무시
        if (inputLocked) return;

        // 25단계는 드래그 불가, 클릭만 가능
        if (IsFinal)
        {
            return;
        }

        if (!rectT) rectT = GetComponent<RectTransform>();
        // 혹시 비활성화된 상태에서 이벤트가 들어온 경우를 방어
        if (!enabled) enabled = true;

        // 입력이 들어온 시점에 트윈/점프 상태 초기화
        KillAllTweens();
        isJumping  = false;
        isDragging = false;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectT.parent as RectTransform, eventData.position, eventData.pressEventCamera, out localPoint);
        dragOffset = (Vector3)localPoint - rectT.localPosition;
        mergeManager?.SetDraggingGirl(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (inputLocked) return;

        if (IsFinal)
        {
            return;
        }
        // 드래그가 시작되면 점프/애니메이션 상태는 모두 리셋하고
        // 순수 드래그 상태로 전환
        KillAllTweens();
        isJumping = false;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (inputLocked) return;

        if (IsFinal)
        {
            return;
        }
        if (!rectT) rectT = GetComponent<RectTransform>();

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectT.parent as RectTransform, eventData.position, eventData.pressEventCamera, out localPoint);
        rectT.localPosition = (Vector3)localPoint - dragOffset;
        mergeManager?.UpdateMergeHighlight(this);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (inputLocked) return;

        if (IsFinal)
        {
            return;
        }
        isDragging = false;
        if (!rectT) rectT = GetComponent<RectTransform>();
        mergeManager?.TryMergeByDrag(this);
        mergeManager?.ClearDraggingGirl();
        Highlight(false);
        targetPosition = rectT.localPosition;
        
        // 드래그 종료 후 Idle 그루브 재시작
        if (!isJumping)
        {
            StartIdleGroove();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inputLocked) return; // LD 연출 중에는 클릭 수익/연출도 정지

        // 드래그 여부와 상관없이 항상 클릭 수익 및 연출 처리 (25단계 포함)
        mergeManager?.AddIncomeGold(this, true);
        Pulse();
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
        
        // Idle 그루브도 일시 중지
        if (idleGrooveTween != null && idleGrooveTween.IsActive()) idleGrooveTween.Kill();

        // 방향 반영한 기준 스케일에서 시작 → 드리프트 방지
        ResetScaleToBase();

        // 더 자연스러운 바운스: 약간 더 부드러운 곡선과 타이밍
        Vector3 baseDir = GetOrientedBaseScale();
        Vector3 scaled = baseDir * 1.18f; // 1.22f -> 1.18f로 약간 줄여서 더 자연스럽게
        
        Sequence bounce = DOTween.Sequence();
        bounce.Append(transform.DOScale(scaled, 0.13f).SetEase(Ease.OutBack, 1.2f)) // OutBack으로 더 탄성있게
              .Append(transform.DOScale(baseDir * 0.98f, 0.08f).SetEase(Ease.InQuad)) // 약간 작아졌다가
              .Append(transform.DOScale(baseDir, 0.10f).SetEase(Ease.OutQuad)) // 원래대로
              .OnComplete(() =>
              {
                  // 바운스 완료 후 Idle 그루브 재시작
                  StartIdleGroove();
              });
    }

    public void Pulse()
    {
        // 이동/점프 트윈은 유지, Pulse 트윈만 관리
        if (pulseTween != null && pulseTween.IsActive()) pulseTween.Kill();

        // ✅ 항상 방향 반영한 "기준 스케일"에서 시작해 드리프트(무한 확대) 차단
        Vector3 baseS = GetOrientedBaseScale();
        transform.localScale = baseS;

        var up   = baseS * pulseUpScaleMul;
        var down = baseS;

        var seq = DOTween.Sequence();
        seq.Append(transform.DOScale(up,   pulseUpTime).SetEase(Ease.OutCubic));
        seq.Append(transform.DOScale(down, pulseDownTime).SetEase(Ease.InCubic));
        pulseTween = seq;
    }

    // 25 모드 진입: 가운데 고정 + 스케일 유지
    public void EnableFinalMode(RectTransform container, float coverage = 0.95f)
    {
        if (!rectT) rectT = GetComponent<RectTransform>();
        if (!imageUI) imageUI = GetComponentInChildren<Image>();
        if (imageUI) imageUI.preserveAspect = true;

        rectT.anchorMin = rectT.anchorMax = new Vector2(0.5f, 0.5f);
        rectT.pivot = new Vector2(0.5f, 0.5f);
        rectT.anchoredPosition = Vector2.zero;

        // 모든 스케일 트윈 종료 후 25단계 전용 스케일로 복원
        KillAllTweens();
        float targetScale = Mathf.Max(0.01f, finalModeScale);
        transform.localScale = Vector3.one * targetScale;
        baseScale = new Vector3(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y), Mathf.Abs(transform.localScale.z));
        ResetScaleToBase();
        ApplyDirectionToOrientation();

        if (autoRoutine != null) StopCoroutine(autoRoutine);
        isJumping = false; isDragging = false;
        
        // 25단계는 클릭 가능하도록 enabled 유지
        enabled = true;
        StartIdleGroove();
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
    private Vector3 GetBaseScale()
    {
        if (baseScale == Vector3.zero) return Vector3.one;
        return baseScale;
    }

    // currentDirectionX를 반영한 "실제 표시용" 기준 스케일
    private Vector3 GetOrientedBaseScale()
    {
        Vector3 s = GetBaseScale();      // 항상 양수
        if (currentDirectionX < 0)
        {
            s.x *= -1f;                  // 오른쪽을 볼 때만 X를 음수로
        }
        return s;
    }

    private void ResetScaleToBase()
    {
        // 무조건 현재 바라보는 방향을 유지한 채로 기준 스케일로 복원
        transform.localScale = GetOrientedBaseScale();
    }

    private void ApplyDirectionToOrientation()
    {
        if (!rectT) rectT = GetComponent<RectTransform>();
        if (!rectT) return;

        // 1) 회전은 항상 0도로 고정 (Y 180도 회전 사용 금지: UI 레이캐스트 꼬임 방지)
        var euler = rectT.localEulerAngles;
        euler.y = 0f;
        rectT.localEulerAngles = euler;

        // 2) 현재 방향에 맞는 스케일을 적용 (한 번 전환되면 유지)
        ResetScaleToBase();
    }
}
