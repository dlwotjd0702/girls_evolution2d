using UnityEngine;

public class GameSystem : MonoBehaviour
{
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

    private async void Awake()
    {
        // GirlDataManager 비동기 로드
        if (girlDataManager != null)
            await girlDataManager.LoadAsync();
        if (spriteLoader != null)
            await spriteLoader.LoadAllGirlSpritesAsync();

        // GirlFieldManager DI
        if (fieldManager != null)
        {
            if (fieldManager.dataManager == null)
                fieldManager.dataManager = girlDataManager;
            if (fieldManager.spriteLoader == null)
                fieldManager.spriteLoader = spriteLoader;
            if (fieldManager.mergeManager == null)
                fieldManager.mergeManager = mergeManager;
            if (fieldManager.upgradeManager == null)
                fieldManager.upgradeManager = upgradeManager;
        }
        // GirlMergeManager DI
        if (mergeManager != null)
        {
            if (mergeManager.DataManager == null)
                mergeManager.DataManager = girlDataManager;
            if (mergeManager.currencyManager == null)
                mergeManager.currencyManager = currencyManager;
            if (mergeManager.fieldManager == null)
                mergeManager.fieldManager = fieldManager;
        }
        // PrestigeManager DI
        if (prestigeManager != null)
        {
            if (prestigeManager.girlFieldManager == null)
                prestigeManager.girlFieldManager = fieldManager;
            if (prestigeManager.currencyManager == null)
                prestigeManager.currencyManager = currencyManager;
        }
        // UpgradeManager DI
        if (upgradeManager != null)
        {
            if (upgradeManager.currencyManager == null)
                upgradeManager.currencyManager = currencyManager;
        }
        // IdleGoldManager DI
        if (idleGoldManager != null)
        {
            if (idleGoldManager.girlFieldManager == null)
                idleGoldManager.girlFieldManager = fieldManager;
            if (idleGoldManager.currencyManager == null)
                idleGoldManager.currencyManager = currencyManager;
            if (idleGoldManager.upgradeManager == null)
                idleGoldManager.upgradeManager = upgradeManager;
        }
        // ClickGoldManager DI
        if (clickGoldManager != null)
        {
            if (clickGoldManager.girlFieldManager == null)
                clickGoldManager.girlFieldManager = fieldManager;
            if (clickGoldManager.currencyManager == null)
                clickGoldManager.currencyManager = currencyManager;
            if (clickGoldManager.upgradeManager == null)
                clickGoldManager.upgradeManager = upgradeManager;
        }
        // GameUIManager DI
        if (gameUIManager != null)
        {
            if (gameUIManager.currencyManager == null)
                gameUIManager.currencyManager = currencyManager;
            if (gameUIManager.prestigeManager == null)
                gameUIManager.prestigeManager = prestigeManager;
            if (gameUIManager.upgradeManager == null)
                gameUIManager.upgradeManager = upgradeManager;
        }
    }
}
