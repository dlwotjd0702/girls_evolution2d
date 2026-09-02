using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>필드의 수익 버프 아이콘에서 여는 보상형 광고 확인창.</summary>
public class IncomeBoostOfferPopup : MonoBehaviour
{
    IncomeActivityManager activity;
    TMP_FontAsset font;
    Sprite frameSprite;
    TextMeshProUGUI title;
    TextMeshProUGUI message;
    Button confirmButton;
    TextMeshProUGUI confirmLabel;

    public void Initialize(IncomeActivityManager target, TMP_FontAsset popupFont, Sprite popupFrameSprite)
    {
        activity = target;
        font = popupFont;
        frameSprite = popupFrameSprite;
        if (!title) Build();
        gameObject.SetActive(false);
    }

    public void Show()
    {
        if (!activity) return;
        if (!title) Build();
        RefreshCopy();
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (!gameObject.activeSelf || !activity) return;
        RefreshCopy();
    }

    void Build()
    {
        var rect = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        var blocker = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        blocker.color = new Color(0.02f, 0.01f, 0.04f, 0.78f);
        blocker.raycastTarget = true;

        // Keep generous vertical breathing room: this popup is shown over the narrow
        // portrait field and the localized two-line message must not touch the buttons.
        var card = Rect("Card", transform, new Vector2(0.5f, 0.5f), new Vector2(880, 720), Vector2.zero);
        var cardImage = card.gameObject.AddComponent<Image>();
        cardImage.sprite = frameSprite;
        cardImage.type = frameSprite ? Image.Type.Sliced : Image.Type.Simple;
        cardImage.color = frameSprite ? Color.white : new Color(0.12f, 0.075f, 0.2f, 0.98f);

        title = Label("Title", card, 54, new Vector2(0, 225), new Vector2(720, 92));
        title.enableAutoSizing = true;
        title.fontSizeMin = 42;
        title.fontSizeMax = 54;

        message = Label("Message", card, 38, new Vector2(0, 58), new Vector2(720, 220));
        message.enableAutoSizing = true;
        message.fontSizeMin = 28;
        message.fontSizeMax = 38;
        message.lineSpacing = 8;

        // Keep both actions fully inside the framed panel at portrait resolutions.
        confirmButton = Button("Confirm", card, new Vector2(-165, -154));
        confirmLabel = confirmButton.GetComponentInChildren<TextMeshProUGUI>();
        confirmButton.onClick.AddListener(Confirm);

        var cancel = Button("Cancel", card, new Vector2(165, -154));
        cancel.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.GetText("취소", "CANCEL");
        cancel.onClick.AddListener(() => gameObject.SetActive(false));
    }

    void RefreshCopy()
    {
        bool active = activity.BoostSecondsRemaining > 0;
        bool adFree = PremiumCurrencyManager.Instance && PremiumCurrencyManager.Instance.AdsRemoved;
        title.text = active
            ? LocalizationManager.GetText("수익 2배 적용 중", "DOUBLE INCOME ACTIVE")
            : LocalizationManager.GetText("10분 수익 2배", "10 MIN DOUBLE INCOME");
        message.text = active
            ? LocalizationManager.GetText($"남은 시간 {activity.BoostSecondsRemaining / 60:00}:{activity.BoostSecondsRemaining % 60:00}", $"Remaining {activity.BoostSecondsRemaining / 60:00}:{activity.BoostSecondsRemaining % 60:00}")
            : LocalizationManager.GetText(
                adFree ? "광고 제거 혜택으로 바로 적용할까요?" : "광고를 보고 10분 동안\n모든 필드 수익을 2배로 받을까요?",
                adFree ? "Apply it now with your ad-free benefit?" : "Watch an ad to double all field income\nfor 10 minutes?");
        confirmLabel.text = active
            ? LocalizationManager.GetText("확인", "OK")
            : adFree ? LocalizationManager.GetText("바로 적용", "APPLY") : LocalizationManager.GetText("광고 보기", "WATCH AD");
        confirmButton.interactable = active || activity.CanRequestBoost;

        if (!active && !adFree && !activity.CanRequestBoost && !activity.RequestPending)
        {
            message.text += LocalizationManager.GetText("\n<size=75%>광고를 준비 중입니다.</size>", "\n<size=75%>Preparing ad…</size>");
            activity.FindAdService()?.LoadRewarded();
        }
    }

    void Confirm()
    {
        if (!activity) return;
        if (activity.BoostSecondsRemaining > 0)
        {
            gameObject.SetActive(false);
            return;
        }
        if (activity.RequestBoost(activity.FindAdService(), PremiumCurrencyManager.Instance && PremiumCurrencyManager.Instance.AdsRemoved))
            gameObject.SetActive(false);
    }

    RectTransform Rect(string objectName, Transform parent, Vector2 anchor, Vector2 size, Vector2 position)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    TextMeshProUGUI Label(string objectName, Transform parent, float size, Vector2 position, Vector2 dimensions)
    {
        var rect = Rect(objectName, parent, new Vector2(0.5f, 0.5f), dimensions, position);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = size;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1, 0.94f, 0.79f);
        label.enableWordWrapping = true;
        label.raycastTarget = false;
        return label;
    }

    Button Button(string objectName, Transform parent, Vector2 position)
    {
        var rect = Rect(objectName, parent, new Vector2(0.5f, 0.5f), new Vector2(330, 120), position);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = frameSprite;
        image.type = frameSprite ? Image.Type.Sliced : Image.Type.Simple;
        image.color = frameSprite ? Color.white : new Color(0.3f, 0.18f, 0.4f, 1);
        var button = rect.gameObject.AddComponent<Button>();
        var label = Label("Label", rect, 38, Vector2.zero, Vector2.zero);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(16, 10);
        label.rectTransform.offsetMax = new Vector2(-16, -10);
        label.enableAutoSizing = true;
        label.fontSizeMin = 26;
        label.fontSizeMax = 38;
        return button;
    }
}
