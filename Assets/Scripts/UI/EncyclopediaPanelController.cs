using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EncyclopediaPanelController : MonoBehaviour
{
    [SerializeField] private GirlDataManager dataManager;
    [SerializeField] private GirlFieldManager fieldManager;
    [SerializeField] private GirlSpriteAddressableLoader spriteLoader;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private EncyclopediaDetailPanel detailPanel;
    public Button ninjaTab, skinTab;
    public TMP_Text collectionSummary, collectionCount;
    public Sprite selectedTabSprite, normalTabSprite;
    readonly Dictionary<int,EncyclopediaSlot> slots = new Dictionary<int,EncyclopediaSlot>();
    readonly Dictionary<string,EncyclopediaSlot> skinSlots = new Dictionary<string,EncyclopediaSlot>();
    readonly List<EncyclopediaSlot> mythicSlots = new List<EncyclopediaSlot>();
    bool showingSkins;
    float timer;
    int revision = -1;
    int mythicCount = -1;
    bool lastKorean;
    void Awake() { Bind(); }
    void Bind()
    {
        var game = GameSystem.Instance;
        if (!game) return;
        if (dataManager==null) dataManager=game.girlDataManager;
        if (!fieldManager) fieldManager=game.fieldManager;
        if (!spriteLoader) spriteLoader=game.spriteLoader;
    }
    void OnEnable()
    {
        if (ninjaTab) ninjaTab.onClick.AddListener(ShowNinjas);
        if (skinTab) skinTab.onClick.AddListener(ShowSkins);
        Refresh();
        if (Application.isPlaying && spriteLoader && !spriteLoader.IsLoadedLD) LoadCatalogArt();
    }
    async void LoadCatalogArt()
    {
        try
        {
            await spriteLoader.EnsureLDLoadedAsync();
            if (this && isActiveAndEnabled) Refresh();
        }
        catch (System.Exception error) { Debug.LogException(error, this); }
    }
    void OnDisable()
    {
        if (detailPanel) detailPanel.Hide();
        if (ninjaTab) ninjaTab.onClick.RemoveListener(ShowNinjas);
        if (skinTab) skinTab.onClick.RemoveListener(ShowSkins);
    }
    void Update()
    {
        timer+=Time.unscaledDeltaTime;
        if (timer<0.5f) return;
        timer=0;
        var collection=CodexCollectionManager.Instance;
        int count=MythicCollectionManager.Instance ? MythicCollectionManager.Instance.forms.Count(f=>MythicCollectionManager.Instance.IsDiscovered(f.id)) : 0;
        if (revision!=(collection ? collection.Revision : -1) || mythicCount!=count || lastKorean!=LocalizationManager.IsKorean || slots.Count<25)
            Refresh();
    }
    public void ShowNinjas() { showingSkins=false; Refresh(); ResetScroll(); }
    public void ShowSkins() { showingSkins=true; Refresh(); ResetScroll(); }
    void ResetScroll()
    {
        var scroll=slotContainer ? slotContainer.GetComponentInParent<ScrollRect>() : null;
        if (scroll) scroll.verticalNormalizedPosition=1;
    }
    EncyclopediaSlot NewSlot()
    {
        var go=Instantiate(slotPrefab,slotContainer);
        return go.GetComponent<EncyclopediaSlot>() ?? go.AddComponent<EncyclopediaSlot>();
    }
    bool Discovered(int level) => CodexCollectionManager.Instance
        ? CodexCollectionManager.Instance.IsLevelDiscovered(level)
        : fieldManager && fieldManager.GetDiscoveredLevels().Contains(level);
    Sprite BaseSprite(int level, bool ld)
    {
        var mythic=MythicCollectionManager.Instance;
        if (level==25 && mythic && mythic.forms.Length>0) return mythic.forms[0].sprite;
        if (ld && spriteLoader && !spriteLoader.IsLoadedLD) return null;
        return spriteLoader ? spriteLoader.GetSpriteForData(dataManager?.GetDataByLevel(level),ld,false) : null;
    }
    public void Refresh()
    {
        Bind();
        lastKorean=LocalizationManager.IsKorean;
        mythicCount=MythicCollectionManager.Instance ? MythicCollectionManager.Instance.forms.Count(f=>MythicCollectionManager.Instance.IsDiscovered(f.id)) : 0;
        var collection=CodexCollectionManager.Instance;
        if (collection)
        {
            revision=collection.Revision;
            if (collectionSummary) collectionSummary.text =
                $"{CodexCollectionManager.EffectName(CodexCollectionManager.Effect.Income)} +{collection.Bonus(CodexCollectionManager.Effect.Income):P0}   ·   {CodexCollectionManager.EffectName(CodexCollectionManager.Effect.ClickIncome)} +{collection.Bonus(CodexCollectionManager.Effect.ClickIncome):P0}\n"+
                $"{CodexCollectionManager.EffectName(CodexCollectionManager.Effect.MergeSpeed)} +{collection.Bonus(CodexCollectionManager.Effect.MergeSpeed):P0}   ·   {CodexCollectionManager.EffectName(CodexCollectionManager.Effect.SpawnSpeed)} +{collection.Bonus(CodexCollectionManager.Effect.SpawnSpeed):P0}";
            if (collectionCount) collectionCount.text=LocalizationManager.GetText(
                $"닌자 {collection.DiscoveredCount}/25  ·  스킨 {collection.OwnedSkinCount}/{collection.skins.Length}  ·  수집 즉시 영구 적용",
                $"NINJAS {collection.DiscoveredCount}/25  ·  SKINS {collection.OwnedSkinCount}/{collection.skins.Length}  ·  Permanent on unlock");
        }
        RefreshTabs(showingSkins);
        if (!slotContainer || !slotPrefab || dataManager==null) return;
        for (int level=1;level<=25;level++)
        {
            var data=dataManager.GetDataByLevel(level);
            if (data==null) continue;
            if (!slots.TryGetValue(level,out var slot) || !slot) slots[level]=slot=NewSlot();
            slot.gameObject.SetActive(!showingSkins);
            if (showingSkins) continue;
            int captured=level; bool unlocked=Discovered(level);
            slot.Setup(level,data.LocalizedName,BaseSprite(level,true),unlocked,()=>OpenNinja(captured));
            slot.SetCaption(unlocked ? LocalizationManager.GetText(
                $"{level} · {data.name}\n{CodexCollectionManager.EffectName(CodexCollectionManager.LevelEffect(level))} +1%",
                $"Lv.{level} · {data.LocalizedName}\n{CodexCollectionManager.EffectName(CodexCollectionManager.LevelEffect(level))} +1%")
                : LocalizationManager.GetText($"{level}단계 · 미해금\n{level}단계 닌자 발견",$"Lv.{level} · LOCKED\nDiscover Lv.{level}"));
        }
        BuildMythics();
        if (!collection) return;
        foreach (var skin in collection.skins)
        {
            if (!skinSlots.TryGetValue(skin.id,out var slot) || !slot) skinSlots[skin.id]=slot=NewSlot();
            slot.gameObject.SetActive(showingSkins);
            if (!showingSkins) continue;
            bool owned=collection.Owns(skin.id);
            slot.Setup(skin.level,skin.DisplayName,skin.ld,owned,()=>OpenSkin(skin));
            slot.SetCaption(SkinCaption(collection,skin,owned));
        }
    }
    public static string SkinCaption(CodexCollectionManager collection,CodexCollectionManager.Skin skin,bool owned)
    {
        string state=owned ? LocalizationManager.GetText(collection.EquippedId(skin.level)==skin.id ? "장착 중" : "수집 완료",collection.EquippedId(skin.level)==skin.id ? "EQUIPPED" : "OWNED") : collection.RequirementText(skin);
        return LocalizationManager.GetText($"{skin.level}단계 · {skin.DisplayName}",$"Lv.{skin.level} · {skin.DisplayName}")+ $"\n{CodexCollectionManager.EffectName(skin.effect)} +{skin.bonus:P0}\n{state}";
    }
    public void RefreshTabs(bool skins)
    {
        showingSkins=skins;
        RefreshTab(ninjaTab,!skins);
        RefreshTab(skinTab,skins);
    }
    void RefreshTab(Button tab,bool selected)
    {
        if (!tab) return;
        if (normalTabSprite) tab.image.sprite=selected ? selectedTabSprite : normalTabSprite;
        tab.image.color=selected ? new Color(1,.76f,.30f,1) : new Color(.65f,.65f,.72f,1);
        var label=tab.GetComponentInChildren<TMP_Text>(true);
        if (label) label.color=selected ? new Color(1,.83f,.36f,1) : Color.white;
    }
    void OpenNinja(int level)
    {
        if (!detailPanel || !Discovered(level)) return;
        var data=dataManager.GetDataByLevel(level);
        bool unlocked=Discovered(level);
        string condition=unlocked ? LocalizationManager.GetText("수집 완료 · 영구 적용","COLLECTED · PERMANENT") : LocalizationManager.GetText($"해금 조건: {level}단계 닌자 발견",$"Unlock: discover level {level}");
        string effect=$"{CodexCollectionManager.EffectName(CodexCollectionManager.LevelEffect(level))} +1%";
        var collection=CodexCollectionManager.Instance;
        bool hasSkin=collection && !string.IsNullOrEmpty(collection.EquippedId(level));
        detailPanel.ShowCollection(data.LocalizedName,level,fieldManager && fieldManager.economy ? fieldManager.economy.GetLevelIncomePerSec(level) : 0,BaseSprite(level,true),
            condition+"\n"+effect,unlocked,hasSkin ? LocalizationManager.GetText("기본 외형 사용","USE DEFAULT") : null,
            hasSkin ? (System.Action)(()=>{ collection.Equip(level,null); OpenNinja(level); Refresh(); }) : null);
    }
    void OpenSkin(CodexCollectionManager.Skin skin)
    {
        var collection=CodexCollectionManager.Instance;
        if (!collection || !detailPanel || skin == null || !collection.Owns(skin.id)) return;
        collection.RefreshProgress();
        bool owned=collection.Owns(skin.id), equipped=collection.EquippedId(skin.level)==skin.id;
        string description=collection.RequirementText(skin)+"\n"+CodexCollectionManager.EffectName(skin.effect)+$" +{skin.bonus:P0}\n"+
            LocalizationManager.GetText("수집 시 영구 적용 · 장착과 무관","Permanent on unlock · independent of equip");
        detailPanel.ShowCollection(skin.DisplayName,skin.level,fieldManager && fieldManager.economy ? fieldManager.economy.GetLevelIncomePerSec(skin.level) : 0,
            skin.ld,description,owned,LocalizationManager.GetText(owned ? equipped ? "기본 외형으로" : "스킨 장착" : "미해금",owned ? equipped ? "USE DEFAULT" : "EQUIP SKIN" : "LOCKED"),
            owned ? (System.Action)(()=>{collection.Equip(skin.level,equipped ? null : skin.id); OpenSkin(skin); Refresh();}) : null);
    }
    void BuildMythics()
    {
        var collection=MythicCollectionManager.Instance;
        if (!collection) return;
        for(int i=0;i<collection.forms.Length;i++)
        {
            if (mythicSlots.Count<=i) mythicSlots.Add(NewSlot());
            var slot=mythicSlots[i]; slot.gameObject.SetActive(!showingSkins);
            if (showingSkins) continue;
            var form=collection.forms[i]; bool unlocked=collection.IsDiscovered(form.id); int index=i;
            slot.Setup(25,form.DisplayName,form.sprite,unlocked,()=>{
                if(detailPanel && collection.IsDiscovered(form.id)) detailPanel.ShowCollection(form.DisplayName,25,fieldManager && fieldManager.economy ? fieldManager.economy.GetLevelIncomePerSec(25) : 0,form.sprite,
                    LocalizationManager.GetText($"해금: {index}회 환생 후 해당 형태로 25단계 발견\n25단계 수집 효과는 최초 1회 적용",$"Unlock: discover Lv.25 after {index} prestiges\nLv.25 collection bonus is granted once"),unlocked);
            });
            slot.SetCaption(LocalizationManager.GetText($"신화 {i+1}\n{form.DisplayName}",$"MYTHIC {i+1}\n{form.DisplayName}"));
        }
    }
}
