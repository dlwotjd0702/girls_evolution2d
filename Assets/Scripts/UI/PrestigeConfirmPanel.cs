using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrestigeConfirmPanel : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI breakdownText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("References")]
    [SerializeField] private PrestigeManager prestigeManager;
    [SerializeField] private GirlFieldManager fieldManager;
    [SerializeField] private EconomyManager economyManager;
    [SerializeField] private MythicCollectionManager mythicCollection;
    float refreshTimer;

    void Awake()
    {
        ResolveReferences();
        if (confirmButton) confirmButton.onClick.AddListener(OnConfirm);
        if (cancelButton) cancelButton.onClick.AddListener(Hide);
        Hide();
    }

    void ResolveReferences()
    {
        if (!prestigeManager) prestigeManager = PrestigeManager.Instance;
        if (!fieldManager) fieldManager = FindObjectOfType<GirlFieldManager>(true);
        if (!economyManager) economyManager = FindObjectOfType<EconomyManager>(true);
        if (!mythicCollection) mythicCollection = FindObjectOfType<MythicCollectionManager>(true);
    }

    void OnDestroy()
    {
        if (confirmButton) confirmButton.onClick.RemoveListener(OnConfirm);
        if (cancelButton) cancelButton.onClick.RemoveListener(Hide);
    }

    public void Show()
    {
        if (!panelRoot) return;
        ResolveReferences();
        panelRoot.SetActive(true);
        refreshTimer = 0f;
        RefreshPreview();
    }

    public void Hide()
    {
        if (panelRoot) panelRoot.SetActive(false);
    }

    void Update()
    {
        if (!panelRoot || !panelRoot.activeInHierarchy) return;
        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer < 0.25f) return;
        refreshTimer = 0f;
        RefreshPreview();
    }

    void OnConfirm()
    {
        if (!prestigeManager || !prestigeManager.IsPrestigeReady())
        {
            RefreshPreview();
            return;
        }
        if (confirmButton) confirmButton.interactable = false;
        prestigeManager.DoPrestige();
        Hide();
    }

    void RefreshPreview()
    {
        bool ready = prestigeManager && prestigeManager.IsPrestigeReady();
        if (confirmButton) confirmButton.interactable = ready;
        if (!breakdownText || !prestigeManager) return;
        var r = prestigeManager.GetPrestigeReward();
        string ko = $"<b>새로운 여정 · 환생 {(long)prestigeManager.GetTotalPrestigeCount() + 1:N0}회차</b>\n"
            + $"신화 닌자  {r.FinalPoints:N0} pt\n필드 닌자  {r.FieldPoints:N0} pt\n소모되는 골드 강화  {r.UpgradePoints:N0} pt\n"
            + $"환생 배율  ×{r.Multiplier:0.##}\n<b><color=#FFE2A0>획득 예정  +{r.TotalPoints:N0} pt</color></b>\n\n"
            + "<color=#A8E3CC>유지</color> 도감 · 보석 · 보석 강화 · 환생 강화 · 계승\n"
            + "<color=#F0B9B4>초기화</color> 필드 · 골드 · 골드 강화 · 소환 가격\n"
            + "<size=85%>신입 닌자 2명 + 소환 충전 가득으로 다시 시작\n유지되는 강화는 포인트로 중복 지급되지 않습니다.</size>";
        string en = $"<b>A new journey · Run {(long)prestigeManager.GetTotalPrestigeCount() + 1:N0}</b>\n"
            + $"Mythic ninja  {r.FinalPoints:N0} pt\nField ninjas  {r.FieldPoints:N0} pt\nConsumed gold upgrades  {r.UpgradePoints:N0} pt\n"
            + $"Prestige multiplier  ×{r.Multiplier:0.##}\n<b><color=#FFE2A0>Expected reward  +{r.TotalPoints:N0} pt</color></b>\n\n"
            + "<color=#A8E3CC>KEEP</color> Collection, gems, gem upgrades, prestige, legacy\n"
            + "<color=#F0B9B4>RESET</color> Field, gold, gold upgrades, summon prices\n"
            + "<size=85%>Restart with 2 novice ninjas and full summon charges.\nKept upgrades do not award repeat prestige points.</size>";
        var collection = mythicCollection;
        if (collection?.NextForm != null)
        {
            ko += $"\n<color=#FFE2A0>다음 신화: {collection.NextForm.DisplayName}</color>\n<size=80%>다음 회차에서 25단계를 달성하면 도감에 등록됩니다.</size>";
            en += $"\n<color=#FFE2A0>Next mythic: {collection.NextForm.DisplayName}</color>\n<size=80%>Reach level 25 next run to collect this form.</size>";
        }
        breakdownText.text = LocalizationManager.GetText(ko, en);
    }
}
