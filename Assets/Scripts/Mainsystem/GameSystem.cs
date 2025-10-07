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

    [Header("Economy (통합본)")]
    public EconomyManager economy;

    [Header("UI")]
    public GameUIManager gameUIManager; // economy 버전이면 연결

    public bool AssetsReady { get; private set; } = false;
    public event Action AssetsReadyEvent;

    private void Awake()
    {
        Instance = this;

        // DI
        if (fieldManager != null)
        {
            if (fieldManager.dataManager == null)    fieldManager.dataManager = girlDataManager;
            if (fieldManager.spriteLoader == null)   fieldManager.spriteLoader = spriteLoader;
            if (fieldManager.mergeManager == null)   fieldManager.mergeManager = mergeManager;
            if (fieldManager.economy == null)        fieldManager.economy = economy;
        }

        if (mergeManager != null)
        {
            if (mergeManager.DataManager == null) mergeManager.DataManager = girlDataManager;
            if (mergeManager.fieldManager == null) mergeManager.fieldManager = fieldManager;
            if (mergeManager.economy == null) mergeManager.economy = economy;
        }

        if (economy != null)
        {
            // 내부 프라이빗 직주입(실패해도 무시)
            var f = typeof(EconomyManager).GetField("fieldManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var m = typeof(EconomyManager).GetField("mergeManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null && (EconomyManager)f.GetValue(economy) == null) f.SetValue(economy, fieldManager);
            if (m != null && (EconomyManager)m.GetValue(economy) == null) m.SetValue(economy, mergeManager);
        }

        if (gameUIManager != null)
        {
            // economy 버전 GameUIManager를 사용 중이라면 여기서 연결
            var prop = gameUIManager.GetType().GetField("economy");
            if (prop != null && prop.GetValue(gameUIManager) == null) prop.SetValue(gameUIManager, economy);
        }
    }

    private async void Start()
    {
        try { if (girlDataManager != null) await girlDataManager.LoadAsync(); }
        catch (Exception e) { Debug.LogError($"[GameSystem] Data Load 실패: {e}"); }

        try { if (spriteLoader != null) await spriteLoader.LoadAllGirlSpritesAsync(); }
        catch (Exception e) { Debug.LogError($"[GameSystem] Sprite Load 실패: {e}"); }

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
