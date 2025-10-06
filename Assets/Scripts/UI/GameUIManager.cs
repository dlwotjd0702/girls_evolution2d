using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI prestigePointText;
    public GameObject upgradePanel;
    public GameObject prestigeButton;

    [Header("Tier / Next Floor")]
    public TierManager tierManager;
    [Tooltip("다음 층으로 이동하는 버튼(2층 언락 시 노출)")]
    public GameObject nextFloorButton;
    [Tooltip("다음 층 버튼 이미지(스프라이트 교체용)")]
    public Image nextFloorButtonImage;
    [Tooltip("층 테마 버튼 스프라이트: [0]=1층, [1]=2층, [2]=3층, [3]=마지막층")]
    public Sprite[] nextFloorButtonSprites = new Sprite[4];

    [Header("Manager DI")]
    public CurrencyManager currencyManager;
    public PrestigeManager prestigeManager;
    public UpgradeManager upgradeManager;

    void OnEnable()
    {
        if (currencyManager != null)
        {
            currencyManager.onGoldChanged -= UpdateGoldUI;
            currencyManager.onGoldChanged += UpdateGoldUI;
            UpdateGoldUI(currencyManager.GetGold());
        }

        if (prestigeManager != null)
        {
            prestigeManager.onPrestigeAvailable -= OnPrestigeAvailable;
            prestigeManager.onPrestigeAvailable += OnPrestigeAvailable;
        }

        if (tierManager != null)
        {
            tierManager.OnTierUnlocked -= OnTierEvent;
            tierManager.OnTierUnlocked += OnTierEvent;
            tierManager.OnTierChanged  -= OnTierEvent;
            tierManager.OnTierChanged  += OnTierEvent;
        }

        prestigeButton?.SetActive(false);
        UpdatePrestigeUI();
        upgradePanel?.SetActive(false);

        UpdateNextFloorButton(); // 초기 상태 갱신
    }

    void OnDisable()
    {
        if (currencyManager != null)
            currencyManager.onGoldChanged -= UpdateGoldUI;

        if (prestigeManager != null)
            prestigeManager.onPrestigeAvailable -= OnPrestigeAvailable;

        if (tierManager != null)
        {
            tierManager.OnTierUnlocked -= OnTierEvent;
            tierManager.OnTierChanged  -= OnTierEvent;
        }
    }

    // ───────────────── UI 업데이트 ─────────────────
    void UpdateGoldUI(double gold)
    {
        if (!goldText) return;
        goldText.text = FormatAbbrev(gold);
    }

    void UpdatePrestigeUI()
    {
        if (prestigePointText && prestigeManager != null)
            prestigePointText.text = $"환생석: {prestigeManager.GetPrestigePoint()}";
    }

    void OnPrestigeAvailable()
    {
        if (prestigeButton) prestigeButton.SetActive(true);
    }

    // ───────────────── Next Floor 버튼 표시/스프라이트 ─────────────────
    void OnTierEvent(int _) => UpdateNextFloorButton();

    void UpdateNextFloorButton()
    {
        if (!nextFloorButton) return;

        // 규칙: 2층(=tier 1) 언락 시부터 버튼 표시
        bool show = (tierManager != null && tierManager.IsUnlocked(1));
        nextFloorButton.SetActive(show);
        if (!show) return;

        // 목적지 층 계산 후, 그 층의 테마 스프라이트를 버튼에 표시
        if (tierManager != null)
        {
            int targetTier = tierManager.ComputeNextTierByRule(); // 규칙 기반 다음 층
            var sp = GetTierButtonSprite(targetTier);
            if (nextFloorButtonImage && sp) nextFloorButtonImage.sprite = sp;
        }
    }

    Sprite GetTierButtonSprite(int tier)
    {
        if (nextFloorButtonSprites == null || nextFloorButtonSprites.Length == 0) return null;
        tier = Mathf.Clamp(tier, 0, nextFloorButtonSprites.Length - 1);
        return nextFloorButtonSprites[tier];
    }

    public void OnClickNextFloor()
    {
        tierManager?.GoNextTierByRule();
        // 배경 전환은 TierBackgroundControllerAscend가 OnTierChanged로 자동 재생
        // 버튼 스프라이트는 OnTierChanged 이벤트로 다시 갱신됨
    }

    // ───────────────── 버튼 핸들러 ─────────────────
    public void OnClickPrestige()
    {
        prestigeManager?.DoPrestige();
        prestigeButton?.SetActive(false);
        UpdatePrestigeUI();
        if (currencyManager != null) UpdateGoldUI(currencyManager.GetGold());
    }

    public void OnClickUpgradePanel()
    {
        if (upgradePanel != null)
            upgradePanel.SetActive(!upgradePanel.activeSelf);
    }

    // ───────────────── 헬퍼 ─────────────────
    static string FormatAbbrev(double v)
    {
        double av = System.Math.Abs(v);
        if (av >= 1e12) return $"{v / 1e12:0.#}T";
        if (av >= 1e9)  return $"{v / 1e9:0.#}B";
        if (av >= 1e6)  return $"{v / 1e6:0.#}M";
        if (av >= 1e3)  return $"{v / 1e3:0.#}K";
        return $"{v:N0}";
    }
}
