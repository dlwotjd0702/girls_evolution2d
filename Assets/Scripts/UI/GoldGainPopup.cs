using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 골드 획득 팝업 (풀링 대상)
/// </summary>
public class GoldGainPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private float moveDistance = 120f; // 위로 이동할 거리
    [SerializeField] private float horizontalRange = 60f; // 좌우로 튀는 범위
    [SerializeField] private float duration = 0.8f; // 포물선 애니메이션 시간
    [SerializeField] private Color bounceColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color clickColor = new Color(1f, 0.95f, 0.45f);

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Tween moveTween;
    private Tween fadeTween;

    private Action<GoldGainPopup> _onFinished;
    
    // 포물선 애니메이션 변수
    private Vector2 parabolaStart;
    private Vector2 parabolaEnd;
    private float parabolaProgress;
    private float parabolaDuration;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetReleaseCallback(Action<GoldGainPopup> callback)
    {
        _onFinished = callback;
    }

    public void Play(Vector2 anchoredPosition, double amount, bool isClick)
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        moveTween?.Kill();
        fadeTween?.Kill();

        // 시작 위치: 객체 하단 (원래 위치에서 아래로 조금)
        Vector2 startPos = anchoredPosition + new Vector2(0f, -30f);
        rectTransform.anchoredPosition = startPos;
        canvasGroup.alpha = 1f;

        if (amountText != null)
        {
            amountText.text = $"+{EconomyManager.FormatAbbrev(amount, 2)}";
            amountText.color = isClick ? clickColor : bounceColor;
        }

        // 포물선 경로 생성: 좌우로 랜덤하게 튀도록
        float randomX = Random.Range(-horizontalRange, horizontalRange);
        // 끝 위치: 시작 위치와 비슷한 높이 (약간 위로) - 포물선이 올라갔다 내려오도록
        Vector2 endPos = startPos + new Vector2(randomX, moveDistance * 0.2f); // 끝은 시작보다 약간만 위로
        
        // 포물선 애니메이션 변수 설정
        parabolaStart = startPos;
        parabolaEnd = endPos;
        parabolaProgress = 0f;
        parabolaDuration = duration;
        
        // 커스텀 포물선 애니메이션
        moveTween = DOTween.To(
            () => parabolaProgress,
            x => {
                parabolaProgress = x;
                UpdateParabolaPosition();
            },
            1f,
            duration
        ).SetEase(Ease.OutQuad);
        
        // 페이드 아웃 (포물선이 내려온 후에 시작)
        fadeTween = canvasGroup.DOFade(0f, duration * 0.4f)
            .SetDelay(duration * 0.6f) // 포물선이 올라갔다 내려온 후에 페이드 시작
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                _onFinished?.Invoke(this);
            });
    }

    private void UpdateParabolaPosition()
    {
        if (rectTransform == null) return;
        
        float t = parabolaProgress; // 0~1
        float peakHeight = moveDistance; // 최고점 높이 (올라갈 최대 높이)
        float endHeight = (parabolaEnd.y - parabolaStart.y); // 끝 위치의 높이 차이
        
        // 수평 이동: 선형
        float x = Mathf.Lerp(parabolaStart.x, parabolaEnd.x, t);
        
        // 포물선 공식: 올라갔다가 내려오는 포물선
        // 조건: t=0일 때 y=0, t=1일 때 y=endHeight, t=0.5일 때 y=peakHeight (최고점)
        // 이차함수: y = a*t^2 + b*t
        // t=0: 0 = 0 + 0 = 0 ✓
        // t=1: endHeight = a + b  ... (1)
        // t=0.5: peakHeight = a*0.25 + b*0.5  ... (2)
        // (2)*2: 2*peakHeight = a*0.5 + b
        // (1) - (2)*2: endHeight - 2*peakHeight = a - a*0.5 = a*0.5
        // a = 2*(endHeight - 2*peakHeight)
        // b = endHeight - a = endHeight - 2*(endHeight - 2*peakHeight) = 4*peakHeight - endHeight
        
        float a = 2f * (endHeight - 2f * peakHeight);
        float b = 4f * peakHeight - endHeight;
        float yOffset = a * t * t + b * t;
        
        float y = parabolaStart.y + yOffset;
        
        rectTransform.anchoredPosition = new Vector2(x, y);
    }

    public void Stop()
    {
        moveTween?.Kill();
        fadeTween?.Kill();
        parabolaProgress = 0f;
    }

}


