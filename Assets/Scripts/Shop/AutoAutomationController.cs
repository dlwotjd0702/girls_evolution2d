using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AutoAutomationController : MonoBehaviour
{
    public enum AutoType { AutoMerge, AutoSpawn }

    [Header("Config")]
    public AutoType type;

    [Header("Refs")]
    public EconomyManager economy;

    [Header("Texts (TMP)")]
    public TextMeshProUGUI combinedLabel; // "오토 합성 • 3.2s" / "오토 소환 • 잠김"
    public TextMeshProUGUI levelText;     // "Lv. x / y"
    public TextMeshProUGUI costText;      // 다음 비용(N0), MAX면 "-"

    [Header("Buy/Upgrade (Icon Only)")]
    public Button buyOrUpgradeButton;
    public Image  buyIconTarget;
    public Sprite buyIconSprite;      // Lv0
    public Sprite upgradeIconSprite;  // 1..Cap-1
    public Sprite maxIconSprite;      // Cap

    [Header("Toggle (Single Button + Icon)")]
    public Button toggleButton;       // 한 개 버튼으로 온/오프
    public Image  toggleIconTarget;   // 온/오프 이미지 교체
    public Sprite toggleOnSprite;
    public Sprite toggleOffSprite;

    [Header("Reason Label)")]
    public TextMeshProUGUI reasonLabel;
    public float reasonShowSeconds = 1.15f;
    Coroutine _reasonRoutine;

    void Awake()
    {
        if (!economy) economy = FindObjectOfType<EconomyManager>();

        if (buyOrUpgradeButton) buyOrUpgradeButton.onClick.AddListener(OnClickBuyOrUpgrade);
        if (toggleButton)       toggleButton.onClick.AddListener(OnClickToggle);

        if (reasonLabel) reasonLabel.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (economy!=null)
        {
            economy.OnGoldChanged    += HandleGoldChanged;
            economy.OnUpgradeChanged += HandleUpgradeChanged;
        }
        Refresh();
    }
    void OnDisable()
    {
        if (economy!=null)
        {
            economy.OnGoldChanged    -= HandleGoldChanged;
            economy.OnUpgradeChanged -= HandleUpgradeChanged;
        }
    }
    void HandleGoldChanged(double _) => Refresh();
    void HandleUpgradeChanged() => Refresh();

    public void Refresh()
    {
        if (!economy) return;

        int  lv  = (type==AutoType.AutoMerge) ? economy.GetAutoMergeUpgradeLevel()
                                              : economy.GetAutoSpawnUpgradeLevel();
        int  cap = (type==AutoType.AutoMerge) ? economy.GetAutoMergeCap()
                                              : economy.GetAutoSpawnCap();
        bool on  = (type==AutoType.AutoMerge) ? economy.IsAutoMergeOn()
                                              : economy.IsAutoSpawnOn();

        // 통합 라벨
        string title = (type==AutoType.AutoMerge) ? "자동 합성" : "자동 소환";
        string value;
        if (lv <= 0) value = "잠김";
        else
        {
            float iv = (type==AutoType.AutoMerge) ? economy.GetAutoMergeInterval()
                                                  : economy.GetAutoSpawnInterval();
            value = (iv >= float.MaxValue*0.5f) ? "-" : $"{iv:0.0}s";
        }
        if (combinedLabel) combinedLabel.text = $"{title}({value})";

        // 레벨/코스트
        if (levelText) levelText.text = FormatLvCap(lv, cap);
        double nextCost = (type==AutoType.AutoMerge) ? economy.GetAutoMergeNextCost()
                                                     : economy.GetAutoSpawnNextCost();
        if (costText) costText.text = double.IsInfinity(nextCost) ? "-" : $"{nextCost:N0}";

        // 아이콘 & 상호작용
        if (buyIconTarget)
        {
            if      (lv <= 0)        buyIconTarget.sprite = buyIconSprite;
            else if (lv >= cap)      buyIconTarget.sprite = maxIconSprite;
            else                     buyIconTarget.sprite = upgradeIconSprite;
        }
        if (buyOrUpgradeButton) buyOrUpgradeButton.interactable = (lv < cap);

        if (toggleButton)      toggleButton.gameObject.SetActive(lv > 0);
        if (toggleIconTarget)  toggleIconTarget.sprite = on ? toggleOnSprite : toggleOffSprite;
    }

    void OnClickBuyOrUpgrade()
    {
        if (!economy) { ShowReason("시스템 미준비"); return; }

        int lv  = (type==AutoType.AutoMerge) ? economy.GetAutoMergeUpgradeLevel()
                                             : economy.GetAutoSpawnUpgradeLevel();
        int cap = (type==AutoType.AutoMerge) ? economy.GetAutoMergeCap()
                                             : economy.GetAutoSpawnCap();
        if (lv >= cap) { ShowReason("최대 레벨입니다."); return; }

        double need = (type==AutoType.AutoMerge) ? economy.GetAutoMergeNextCost()
                                                 : economy.GetAutoSpawnNextCost();
        bool ok = (type==AutoType.AutoMerge) ? economy.TryBuyAutoMergeUpgrade()
                                             : economy.TryBuyAutoSpawnUpgrade();

        if (!ok)
        {
            if (economy.GetGold() < need) ShowReason("골드가 부족합니다.");
            else ShowReason("구매가 불가합니다.");
            return;
        }

        HideReason();
        Refresh();
    }

    void OnClickToggle()
    {
        if (!economy) { ShowReason("시스템 미준비"); return; }

        int lv = (type==AutoType.AutoMerge) ? economy.GetAutoMergeUpgradeLevel()
                                            : economy.GetAutoSpawnUpgradeLevel();
        if (lv <= 0) { ShowReason("먼저 구매로 언락하세요."); return; }

        bool on = (type==AutoType.AutoMerge) ? economy.IsAutoMergeOn()
                                             : economy.IsAutoSpawnOn();

        if (type==AutoType.AutoMerge) economy.SetAutoMergeOn(!on);
        else                          economy.SetAutoSpawnOn(!on);

        HideReason();
        Refresh();
    }

    // "Lv. 현재 / 최대" 포맷
    string FormatLvCap(int lv, int cap)
    {
        if (cap <= 0) return $"Lv. {lv}";
        lv = Mathf.Clamp(lv, 0, cap);
        return $"Lv.{lv}/{cap}";
    }

    // Reason helpers
    void ShowReason(string msg)
    {
        if (!reasonLabel) return;
        HideReason();
        reasonLabel.text = msg;
        reasonLabel.gameObject.SetActive(true);
        _reasonRoutine = StartCoroutine(HideAfter(reasonShowSeconds));
    }
    System.Collections.IEnumerator HideAfter(float sec){ yield return new WaitForSecondsRealtime(sec); HideReason(); }
    void HideReason(){ if(!reasonLabel) return; if(_reasonRoutine!=null){ StopCoroutine(_reasonRoutine); _reasonRoutine=null; } reasonLabel.text=""; reasonLabel.gameObject.SetActive(false); }
}
