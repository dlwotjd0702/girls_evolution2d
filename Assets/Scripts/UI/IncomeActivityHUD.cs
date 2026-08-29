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
    float refreshTimer;

    void OnEnable()
    {
        if (boostButton) boostButton.onClick.AddListener(RequestBoost);
        Refresh();
    }
    void OnDisable() { if (boostButton) boostButton.onClick.RemoveListener(RequestBoost); }
    void RequestBoost() { if (activity) activity.RequestBoost(); Refresh(); }
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
        if (boostButton) boostButton.interactable = Application.isPlaying && activity.CanRequestBoost;
        if (!boostLabel) return;
        bool adFree = PremiumCurrencyManager.Instance && PremiumCurrencyManager.Instance.AdsRemoved;
        boostLabel.text = remaining > 0
            ? LocalizationManager.GetText($"수익 {activity.adMultiplier:0.#}배 적용 중\n<size=80%>남은 시간 {remaining / 60:00}:{remaining % 60:00}</size>", $"INCOME ×{activity.adMultiplier:0.#} ACTIVE\n<size=80%>{remaining / 60:00}:{remaining % 60:00} remaining</size>")
            : activity.RequestPending ? LocalizationManager.GetText("광고 진행 중", "AD IN PROGRESS")
            : LocalizationManager.GetText($"수익 {activity.adMultiplier:0.#}배 · {activity.boostSeconds / 60}분\n<size=80%>{(adFree ? "광고 없이 받기" : "광고 보고 받기")}</size>", $"INCOME ×{activity.adMultiplier:0.#} · {activity.boostSeconds / 60} MIN\n<size=80%>{(adFree ? "CLAIM WITHOUT AD" : "WATCH AD TO CLAIM")}</size>");
    }
}
