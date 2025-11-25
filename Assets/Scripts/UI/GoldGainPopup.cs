using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 골드 획득 팝업 (풀링 대상)
/// </summary>
public class GoldGainPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private float moveDistance = 90f;
    [SerializeField] private float duration = 0.65f;
    [SerializeField] private Color bounceColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color clickColor = new Color(1f, 0.95f, 0.45f);

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Tween moveTween;
    private Tween fadeTween;

    private Action<GoldGainPopup> _onFinished;

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

        rectTransform.anchoredPosition = anchoredPosition;
        canvasGroup.alpha = 1f;

        if (amountText != null)
        {
            amountText.text = $"+{EconomyManager.FormatAbbrev(amount, 2)}";
            amountText.color = isClick ? clickColor : bounceColor;
        }

        Vector2 targetPos = anchoredPosition + new Vector2(0f, moveDistance);
        moveTween = rectTransform.DOAnchorPos(targetPos, duration).SetEase(Ease.OutQuad);
        fadeTween = canvasGroup.DOFade(0f, duration).SetEase(Ease.InQuad).OnComplete(() =>
        {
            _onFinished?.Invoke(this);
        });
    }

    public void Stop()
    {
        moveTween?.Kill();
        fadeTween?.Kill();
    }

}


