using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IncomeActivityHUD : MonoBehaviour
{
    public IncomeActivityManager activity;
    public TextMeshProUGUI feverLabel;
    public TextMeshProUGUI feverHint;
    public Image feverFill;
    public Button boostButton;
    public TextMeshProUGUI boostLabel;
    public IncomeBoostOfferPopup boostPopup;
    public Sprite popupFrameSprite;
    float refreshTimer;
    void Awake() => NormalizeFeverLayout();
    void OnEnable()
    {
        NormalizeFeverLayout();
        if (boostButton) boostButton.onClick.AddListener(RequestBoost);
        Refresh();
    }
    void OnDisable() { if (boostButton) boostButton.onClick.RemoveListener(RequestBoost); }
    void RequestBoost()
    {
        if (!activity) return;
        EnsurePopup();
        boostPopup.Show();
        Refresh();
    }

    void EnsurePopup()
    {
        if (boostPopup) return;
        boostPopup = FindObjectOfType<IncomeBoostOfferPopup>(true);
        if (!boostPopup)
        {
            var canvas = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
            var popup = new GameObject("Income Boost Offer Popup", typeof(RectTransform));
            popup.transform.SetParent(canvas.transform, false);
            boostPopup = popup.AddComponent<IncomeBoostOfferPopup>();
        }
        boostPopup.Initialize(activity, boostLabel ? boostLabel.font : null, popupFrameSprite);
    }
    void Update()
    {
        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer < 0.1f) return;
        refreshTimer = 0;
        RefreshBoost();
    }
    // Render the meter after this frame's gameplay/input updates; never throttle it with ad text.
    void LateUpdate() => RefreshFever();
    public void Refresh()
    {
        RefreshFever();
        RefreshBoost();
    }

    public void NormalizeFeverLayout()
    {
        if (!feverFill) return;
        var fillRect = feverFill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.pivot = new Vector2(0.5f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillRect.localScale = Vector3.one;
        feverFill.type = Image.Type.Filled;
        feverFill.fillMethod = Image.FillMethod.Horizontal;
        feverFill.fillOrigin = 0;
        feverFill.preserveAspect = false;

        var track = fillRect.parent as RectTransform;
        if (!track) return;
        float y = track.anchoredPosition.y;
        float height = track.sizeDelta.y;
        track.anchorMin = new Vector2(0, 0);
        track.anchorMax = new Vector2(1, 0);
        track.pivot = new Vector2(0.5f, 0.5f);
        track.sizeDelta = new Vector2(0, height);
        track.anchoredPosition = new Vector2(0, y);
        track.localScale = Vector3.one;
    }

    public void RefreshFever()
    {
        if (!activity) return;
        if (feverFill)
        {
            feverFill.fillAmount = activity.Fever.Fill;
            feverFill.color = activity.Fever.IsActive ? new Color(1, 0.63f, 0.2f) : new Color(0.91f, 0.74f, 0.36f);
        }
        if (feverLabel) feverLabel.text = activity.Fever.IsActive ? "FEVER!" : "FEVER";
        if (feverHint) { feverHint.text = ""; feverHint.gameObject.SetActive(false); }
    }
    void RefreshBoost()
    {
        if (!activity) return;
        long remaining = activity.BoostSecondsRemaining;
        if (boostButton) boostButton.interactable = Application.isPlaying;
        if (!boostLabel) return;
        bool adFree = PremiumCurrencyManager.Instance && PremiumCurrencyManager.Instance.AdsRemoved;
        boostLabel.text = remaining > 0
            ? $"<b>{activity.adMultiplier:0.#}×</b>\n<size=65%>{remaining / 60:00}:{remaining % 60:00}</size>"
            : activity.RequestPending ? "<b>2×</b>\n<size=60%>…</size>"
            : $"<b>{activity.adMultiplier:0.#}×</b>\n<size=60%>{activity.boostSeconds / 60} MIN</size>";
    }
}
