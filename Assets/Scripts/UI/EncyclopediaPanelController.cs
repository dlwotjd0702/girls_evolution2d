using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// LD 도감 패널 컨트롤러
/// - 25개 슬롯 그리드 레이아웃
/// - 언락된 슬롯만 클릭 가능
/// - 미해방 슬롯은 비활성화 표시
/// </summary>
public class EncyclopediaPanelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GirlDataManager dataManager;
    [SerializeField] private GirlFieldManager fieldManager;
    [SerializeField] private GirlSpriteAddressableLoader spriteLoader;
    
    [Header("UI")]
    [SerializeField] private Transform slotContainer;  // GridLayoutGroup이 있는 부모
    [SerializeField] private GameObject slotPrefab;      // 도감 슬롯 프리팹
    [SerializeField] private EncyclopediaDetailPanel detailPanel;  // 팝업 패널
    
    private readonly Dictionary<int, EncyclopediaSlot> slots = new Dictionary<int, EncyclopediaSlot>();
    private readonly HashSet<int> discoveredLevels = new HashSet<int>();
    
    void Awake()
    {
        var gs = GameSystem.Instance;
        if (gs != null)
        {
            if (dataManager == null) dataManager = gs.girlDataManager;
            if (fieldManager == null) fieldManager = gs.fieldManager;
            if (spriteLoader == null) spriteLoader = gs.spriteLoader;
        }
    }
    
    void OnEnable()
    {
        RefreshDiscoveredLevels();
        BuildSlots();
    }
    
    void RefreshDiscoveredLevels()
    {
        discoveredLevels.Clear();
        
        // GirlFieldManager의 discoveredLevels 가져오기
        if (fieldManager != null)
        {
            var discovered = fieldManager.GetDiscoveredLevels();
            if (discovered != null)
            {
                foreach (var level in discovered)
                    discoveredLevels.Add(level);
            }
        }
    }
    
    void BuildSlots()
    {
        if (slotContainer == null || slotPrefab == null || dataManager == null) return;

        // 최초 1회 생성 후 재사용: 이미 생성된 슬롯이 있으면 파괴하지 않고 재활용
        for (int level = 1; level <= TierRules.MaxLevel; level++)
        {
            var data = dataManager.GetDataByLevel(level);
            if (data == null) continue;

            EncyclopediaSlot slot;
            if (!slots.TryGetValue(level, out slot) || slot == null)
            {
                var go = Instantiate(slotPrefab, slotContainer);
                slot = go.GetComponent<EncyclopediaSlot>();
                if (slot == null)
                    slot = go.AddComponent<EncyclopediaSlot>();
                slots[level] = slot;
            }

            bool isUnlocked = discoveredLevels.Contains(level);
            Sprite iconSprite = null;

            if (isUnlocked && spriteLoader != null)
            {
                // 언락된 경우 SD 스프라이트 사용 (슬롯은 작으므로)
                iconSprite = spriteLoader.GetSpriteForData(data, preferLD: false);
            }

            int capturedLevel = level;

            slot.Setup(
                level: capturedLevel,
                name: data.name,
                sprite: iconSprite,
                isUnlocked: isUnlocked,
                onClick: () => OnSlotClicked(capturedLevel)
            );
        }
    }
    
    void OnSlotClicked(int level)
    {
        // 클릭 시 최신 발견 상태 확인 (패널이 열려있는 동안 발견된 경우 대비)
        RefreshDiscoveredLevels();
        
        if (!discoveredLevels.Contains(level))
        {
            // 개발용 로그는 필요 시만 사용
            // Debug.Log($"[Encyclopedia] 레벨 {level}은 아직 해방되지 않았습니다.");
            return;
        }
        
        if (detailPanel == null)
        {
            Debug.LogError("[Encyclopedia] DetailPanel이 설정되지 않았습니다.");
            return;
        }
        
        var data = dataManager?.GetDataByLevel(level);
        if (data == null)
        {
            Debug.LogError($"[Encyclopedia] 레벨 {level}의 데이터를 찾을 수 없습니다.");
            return;
        }
        
        Sprite ldSprite = null;
        if (spriteLoader != null)
        {
            ldSprite = spriteLoader.GetSpriteForData(data, preferLD: true);
        }
        
        double income = 0;
        if (fieldManager != null)
        {
            // EconomyManager를 통해 수익 계산
            var economy = fieldManager.economy;
            if (economy != null)
            {
                income = economy.GetLevelIncomePerSec(level);
            }
        }
        
        detailPanel.Show(data.name, level, income, ldSprite);
    }
    
    public void Refresh()
    {
        RefreshDiscoveredLevels();
        BuildSlots();
    }
}

