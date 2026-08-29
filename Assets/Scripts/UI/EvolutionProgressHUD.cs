using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Current-world identity and the next concrete evolution milestone.</summary>
public class EvolutionProgressHUD : MonoBehaviour
{
    public GirlFieldManager field;
    public TierManager tiers;
    public PrestigeManager prestige;
    public MythicCollectionManager mythicCollection;
    public TextMeshProUGUI worldLabel;
    public TextMeshProUGUI goalLabel;
    public Image progressFill;
    float refreshTimer;

    void OnEnable() => Refresh();

    void Update()
    {
        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer < 0.4f) return;
        refreshTimer = 0;
        Refresh();
    }

    public void Refresh()
    {
        if (!field || !tiers || !prestige) return;
        int tier = Mathf.Clamp(tiers.CurrentTierIndex, 0, 3);
        string[] ko = { "대나무 정원", "안개 수련장", "하늘 성역", "별빛 환생전" };
        string[] en = { "BAMBOO GARDEN", "MIST SANCTUARY", "SKY SANCTUARY", "ASTRAL SANCTUARY" };
        var collection = mythicCollection ? mythicCollection : MythicCollectionManager.Instance;
        if (worldLabel) worldLabel.text = LocalizationManager.GetText(
            $"{tier + 1:00}  {ko[tier]}   ·   환생 {prestige.GetTotalPrestigeCount():N0}회",
            $"{tier + 1:00}  {en[tier]}   ·   PRESTIGE {prestige.GetTotalPrestigeCount():N0}");
        if (worldLabel && collection?.CurrentForm != null)
            worldLabel.text += LocalizationManager.GetText($" · 신화 {collection.CollectedCount}/{collection.forms.Length}", $" · MYTHIC {collection.CollectedCount}/{collection.forms.Length}");

        int reached = Mathf.Clamp(field.CurrentMaxLevel, 1, TierRules.MaxLevel);
        int target = reached < 9 ? 9 : reached < 17 ? 17 : 25;
        int start = reached < 9 ? 1 : reached < 17 ? 9 : 17;
        bool ready = prestige.IsPrestigeReady();
        if (progressFill) progressFill.fillAmount = ready ? 1f : Mathf.InverseLerp(start, target, reached);
        if (!goalLabel) return;
        goalLabel.text = ready
            ? LocalizationManager.GetText($"환생 가능  +{prestige.GetPrestigeReward().TotalPoints:N0} pt · 4층에서 환생",
                $"Prestige ready  +{prestige.GetPrestigeReward().TotalPoints:N0} pt · Go to world 4")
            : LocalizationManager.GetText($"다음 목표  Lv.{target} · 현재 최고 Lv.{reached}",
                $"NEXT  Lv.{target} · CURRENT BEST  Lv.{reached}");
        if (collection?.CurrentForm != null && !ready)
            goalLabel.text += $" · {collection.CurrentForm.DisplayName}";
    }
}
