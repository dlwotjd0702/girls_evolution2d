using System;
using UnityEngine;

[DefaultExecutionOrder(-100)] // GameSystem이 가장 먼저 Awake되도록
public class GameSystem : MonoBehaviour
{
    public static GameSystem Instance { get; private set; }

    [Header("Data")]
    public GirlSpriteAddressableLoader spriteLoader;
    public GirlDataManager girlDataManager = new GirlDataManager();

    [Header("GameLoop")]
    public GirlFieldManager fieldManager;
    public GirlMergeManager mergeManager;
    public UpgradeManager upgradeManager;
    public PrestigeManager prestigeManager;

    [Header("Gold")]
    public CurrencyManager currencyManager;
    public IdleGoldManager idleGoldManager;
    public ClickGoldManager clickGoldManager;

    [Header("UI")]
    public GameUIManager gameUIManager;

    public bool AssetsReady { get; private set; } = false;
    public event Action AssetsReadyEvent;

    private void Awake()
    {
        Instance = this;

        // ── DI: 참조 연결 (Awake에서) ──────────────────────────────────────────
        if (fieldManager != null)
        {
            if (fieldManager.dataManager == null)    fieldManager.dataManager = girlDataManager;
            if (fieldManager.spriteLoader == null)   fieldManager.spriteLoader = spriteLoader;
            if (fieldManager.mergeManager == null)   fieldManager.mergeManager = mergeManager;
            if (fieldManager.upgradeManager == null) fieldManager.upgradeManager = upgradeManager;
        }

        if (mergeManager != null)
        {
            if (mergeManager.DataManager == null)     mergeManager.DataManager = girlDataManager;
            if (mergeManager.currencyManager == null) mergeManager.currencyManager = currencyManager;
            if (mergeManager.fieldManager == null)    mergeManager.fieldManager = fieldManager;
        }

        if (prestigeManager != null)
        {
            if (prestigeManager.girlFieldManager == null) prestigeManager.girlFieldManager = fieldManager;
            if (prestigeManager.currencyManager == null)  prestigeManager.currencyManager = currencyManager;

            if (prestigeManager.topLevel <= 0 || prestigeManager.topLevel != TierRules.MaxLevel)
                prestigeManager.topLevel = TierRules.MaxLevel;
        }

        if (upgradeManager != null)
        {
            if (upgradeManager.currencyManager == null) upgradeManager.currencyManager = currencyManager;
        }

        if (idleGoldManager != null)
        {
            if (idleGoldManager.girlFieldManager == null) idleGoldManager.girlFieldManager = fieldManager;
            if (idleGoldManager.currencyManager == null)  idleGoldManager.currencyManager = currencyManager;
            if (idleGoldManager.upgradeManager == null)   idleGoldManager.upgradeManager = upgradeManager;
        }

        if (clickGoldManager != null)
        {
            if (clickGoldManager.girlFieldManager == null) clickGoldManager.girlFieldManager = fieldManager;
            if (clickGoldManager.currencyManager == null)  clickGoldManager.currencyManager = currencyManager;
            if (clickGoldManager.upgradeManager == null)   clickGoldManager.upgradeManager = upgradeManager;
        }

        if (gameUIManager != null)
        {
            if (gameUIManager.currencyManager == null)  gameUIManager.currencyManager = currencyManager;
            if (gameUIManager.prestigeManager == null)  gameUIManager.prestigeManager = prestigeManager;
            if (gameUIManager.upgradeManager == null)   gameUIManager.upgradeManager = upgradeManager;
        }
    }

    private async void Start()
    {
        // ── 리소스/데이터 로드 (Start에서) ──────────────────────────────────────
        try
        {
            if (girlDataManager != null)
                await girlDataManager.LoadAsync(); // CSV/Json 등
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameSystem] GirlDataManager.LoadAsync 실패: {e}");
        }

        try
        {
            if (spriteLoader != null)
                await spriteLoader.LoadAllGirlSpritesAsync(); // SD→LD 순차 로드(이미 로드면 스킵)
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameSystem] spriteLoader.LoadAllGirlSpritesAsync 실패: {e}");
        }

        AssetsReady = true;
        AssetsReadyEvent?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        AssetsReadyEvent = null;
    }
}
