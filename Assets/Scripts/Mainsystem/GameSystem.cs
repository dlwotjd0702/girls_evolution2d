using System;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class GameSystem : MonoBehaviour
{
    public static GameSystem Instance { get; private set; }

    [Header("Data")]
    public GirlSpriteAddressableLoader spriteLoader;
    public GirlDataManager girlDataManager = new GirlDataManager();

    [Header("GameLoop")]
    public GirlFieldManager fieldManager;
    public GirlMergeManager mergeManager;
    public PrestigeManager prestigeManager;

    [Header("Economy")]
    public EconomyManager economy;

    [Header("UI")]
    
    public SummonPanelController summonPanel; // 선택

    public bool AssetsReady { get; private set; } = false;
    public event Action AssetsReadyEvent;

    private void Awake()
    {
        Instance = this;

        // GirlField
        if (fieldManager != null)
        {
            if (fieldManager.dataManager == null)  fieldManager.dataManager = girlDataManager;
            if (fieldManager.spriteLoader == null) fieldManager.spriteLoader = spriteLoader;
            if (fieldManager.mergeManager == null) fieldManager.mergeManager = mergeManager;
            if (fieldManager.economy == null)      fieldManager.economy = economy;
        }

        // Merge
        if (mergeManager != null)
        {
            if (mergeManager.DataManager == null)  mergeManager.DataManager = girlDataManager;
            if (mergeManager.fieldManager == null) mergeManager.fieldManager = fieldManager;
            if (mergeManager.economy == null)      mergeManager.economy = economy;
        }

        // Prestige
        if (prestigeManager != null)
        {
            if (prestigeManager.girlFieldManager == null) prestigeManager.girlFieldManager = fieldManager;
            if (prestigeManager.economy == null)          prestigeManager.economy = economy;
            if (prestigeManager.tierManager == null)       prestigeManager.tierManager = FindObjectOfType<TierManager>(true);
            if (prestigeManager.topLevel <= 0)             prestigeManager.topLevel = TierRules.MaxLevel;
        }

        
    }

    private async void Start()
    {
        try
        {
            if (girlDataManager != null) await girlDataManager.LoadAsync(); // CSV/TSV 자동 판별
        }
        catch (Exception e) { Debug.LogError($"[GameSystem] GirlDataManager.LoadAsync 실패: {e}"); }

        try
        {
            if (spriteLoader != null) await spriteLoader.LoadAllGirlSpritesAsync(); // SD→LD 순차 로드
        }
        catch (Exception e) { Debug.LogError($"[GameSystem] spriteLoader.LoadAllGirlSpritesAsync 실패: {e}"); }

        AssetsReady = true;
        AssetsReadyEvent?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        AssetsReadyEvent = null;
        spriteLoader?.UnloadAll();
    }
}
