using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Edit-mode regression checks against the real managers, without touching player saves.</summary>
public static class ProgressionRegressionChecks
{
    static readonly List<string> passed = new List<string>();
    static Scene preview;

    [MenuItem("Tools/Girls Evolution/Run Progression Checks %&t")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Run these checks outside Play Mode.");
        passed.Clear();
        preview = EditorSceneManager.NewPreviewScene();
        var oldPool = SimpleUIPool.Instance;
        // No save, sound, stats or live singleton notifications during checks.
        var singletonTypes = new[] { typeof(PrestigeManager), typeof(LegacyRankManager),
            typeof(SaveManager), typeof(AudioManager), typeof(PlayStatsTracker), typeof(EconomyManager),
            typeof(MythicCollectionManager), typeof(IncomeActivityManager),typeof(CodexCollectionManager),typeof(GameSystem) };
        var saved = new Dictionary<PropertyInfo, object>();
        foreach (var type in singletonTypes)
        {
            var property = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (property?.GetSetMethod(true) == null) continue;
            saved[property] = property.GetValue(null);
            property.SetValue(null, null);
        }
        try
        {
            RewardChecks();
            SaveCompatibilityChecks();
            EconomyChecks();
            PrestigeChecks();
            ExistingFlowChecks();
            RoundedPanelChecks();
            MythicAndActivityChecks();
            MobilePresentationChecks();
            ShopAndSummonChecks();
            ReviewedArtChecks();
            CollectionChecks();
            ReadabilityChecks();
            PanelRepairChecks();
            InstalledSkinChecks();
            RewardAndFieldPresentationChecks();
            SceneIntegrityChecks();
            CharacterNameChecks();
            string report = $"PASS: {passed.Count} checks\n" + string.Join("\n", passed);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/progression-checks.txt", report);
            Debug.Log("[ProgressionChecks] " + report);
        }
        catch (Exception error)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/progression-checks.txt", "FAIL\n" + error);
            Debug.LogException(error);
            throw;
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
            SimpleUIPool.Instance = oldPool;
            foreach (var pair in saved) pair.Key.SetValue(null, pair.Value);
        }
    }

    /// <summary>CI/batch entry point that also exercises checks bound to the real Ingame scene.</summary>
    public static void RunBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Ingame.unity", OpenSceneMode.Single);
        Run();
    }

    static void PanelRepairChecks()
    {
        var slotPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EncyclopediaSlot.prefab");
        var slotGo=UnityEngine.Object.Instantiate(slotPrefab);SceneManager.MoveGameObjectToScene(slotGo,preview);
        var slot=slotGo.GetComponent<EncyclopediaSlot>();var button=PanelLayoutRepair.Ref<Button>(slot,"button");int opened=0;
        slot.Setup(2,"Locked",null,false,()=>opened++,true);button.onClick.Invoke();
        Check(!button.interactable&&opened==0,"Locked codex entry rejects even direct UnityEvent invocation");
        slot.Setup(2,"Owned",null,true,()=>opened++);button.onClick.Invoke();
        Check(button.interactable&&opened==1,"Unlocked codex entry opens once");
        slot.Setup(3,"Locked again",null,false,()=>opened++);button.onClick.Invoke();
        Check(!button.interactable&&opened==1,"Reused locked slot cannot retain its old click action");
        var activity=Make<IncomeActivityManager>("Frame-rate fever");var hud=Make<IncomeActivityHUD>("Frame-rate fever HUD");hud.activity=activity;hud.feverFill=Make<Image>("Frame-rate fill");
        for(int i=0;i<3;i++)activity.Fever.RecordClick(0);
        for(int frame=1;frame<=5;frame++)
        {
            activity.Fever.Tick(frame/60d,1/60d);typeof(IncomeActivityHUD).GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(hud,null);
            Check(Math.Abs(hud.feverFill.fillAmount-activity.Fever.Fill)<.00001&&hud.feverFill.fillAmount>0,"Fever fill follows each frame without 100ms batching: "+frame);
        }
        UnityEngine.Object.DestroyImmediate(activity.gameObject);
        var scene=EditorSceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/Ingame.unity")return;
        var liveCanvas=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Canvas>(true)).First(c=>c.isRootCanvas);
        var source=liveCanvas.GetComponentInChildren<EncyclopediaPanelController>(true);
        foreach(var shop in liveCanvas.GetComponentsInChildren<ShopPanelController>(true))foreach(var entry in shop.entries)
            Check(AssetDatabase.GetAssetPath(entry.upgradeIconSprite)=="Assets/Prefabs/sprite/강화구매맥스.png"&&AssetDatabase.GetAssetPath(entry.maxIconSprite)=="Assets/Prefabs/sprite/강화구매맥스.png","Shop runtime state keeps original button artwork: "+shop.name+"/"+entry.type);
        foreach(var shop in liveCanvas.GetComponentsInChildren<PrestigeShopPanelController>(true))foreach(var entry in shop.entries)
            Check(AssetDatabase.GetAssetPath(entry.upgradeIconSprite)=="Assets/Prefabs/sprite/강화구매맥스.png"&&AssetDatabase.GetAssetPath(entry.maxIconSprite)=="Assets/Prefabs/sprite/강화구매맥스.png","Rebirth shop state keeps original button artwork: "+entry.type);
        var automations=liveCanvas.GetComponentsInChildren<AutoAutomationController>(true);
        Check(automations.Length==2,"Both main-screen automation controllers are included in shop validation");
        foreach(var automation in automations)
        {
            Check(new[]{automation.buyIconSprite,automation.upgradeIconSprite,automation.maxIconSprite}.All(s=>AssetDatabase.GetAssetPath(s)=="Assets/Prefabs/sprite/강화구매맥스.png"),"Automation purchase states keep original artwork: "+automation.type);
            Check(automation.combinedLabel&&automation.combinedLabel.text.Contains("자동")&&automation.costText&&automation.costText.text!="1","Automation shop label and price are populated: "+automation.type);
        }
        var clone=UnityEngine.Object.Instantiate(source.gameObject);SceneManager.MoveGameObjectToScene(clone,preview);clone.SetActive(true);
        var catalog=clone.GetComponent<EncyclopediaPanelController>();var detail=PanelLayoutRepair.Ref<EncyclopediaDetailPanel>(catalog,"detailPanel");var panel=PanelLayoutRepair.Ref<GameObject>(detail,"panelRoot");
        typeof(EncyclopediaDetailPanel).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(detail,null);detail.ShowCollection("수집 닌자",6,320,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/닌자SD2/1.png"),"수집 완료 · 클릭 수익 +5%",true,"스킨 장착",()=>{});
        Check(detail.IsVisible()&&panel.transform.GetSiblingIndex()==panel.transform.parent.childCount-1,"Detail is visible on first open and renders above codex tabs");
        var art=PanelLayoutRepair.Ref<Image>(detail,"ldIllustrationImage").rectTransform;
        var name=PanelLayoutRepair.Ref<TextMeshProUGUI>(detail,"nameText").rectTransform;
        var level=PanelLayoutRepair.Ref<TextMeshProUGUI>(detail,"levelText").rectTransform;
        var income=PanelLayoutRepair.Ref<TextMeshProUGUI>(detail,"incomeText").rectTransform;
        Rect Bounds(RectTransform r,Transform parent){var corners=new Vector3[4];r.GetWorldCorners(corners);var min=parent.InverseTransformPoint(corners[0]);var max=parent.InverseTransformPoint(corners[2]);return Rect.MinMaxRect(min.x,min.y,max.x,max.y);}
        var regions=new[]{art,name,level,income,detail.collectionText.rectTransform,(RectTransform)detail.equipButton.transform,(RectTransform)PanelLayoutRepair.Ref<Button>(detail,"closeButton").transform};
        for(int a=0;a<regions.Length;a++)for(int b=a+1;b<regions.Length;b++)Check(!Bounds(regions[a],panel.transform).Overlaps(Bounds(regions[b],panel.transform)),"Detail regions do not overlap: "+a+"/"+b);
        foreach(var region in regions){var bounds=Bounds(region,panel.transform);var outer=((RectTransform)panel.transform).rect;Check(outer.Contains(bounds.min)&&outer.Contains(bounds.max),"Detail region stays inside panel: "+region.name);}
        Check(!panel.transform.Find("Close (1)").gameObject.activeSelf,"Obsolete detail close control is hidden");
        detail.ShowCollection("Locked",20,0,null,"Locked",false);Check(!detail.IsVisible(),"Detail rejects a locked entry even if called directly");
        detail.ShowCollection("Owned",6,320,null,"Owned",true);typeof(EncyclopediaPanelController).GetMethod("OnDisable",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(catalog,null);Check(!detail.IsVisible(),"Closing codex also closes its detail overlay");
        foreach(var scroll in liveCanvas.GetComponentsInChildren<ScrollRect>(true).Where(s=>s.transform.IsChildOf(liveCanvas.transform.Find("Shop"))||s.transform.IsChildOf(source.transform)||s.GetComponentInParent<SummonPanelController>(true)))
        {
            Check(scroll.viewport.GetComponent<RectMask2D>()&&scroll.viewport.GetComponent<RectMask2D>().enabled,"Panel has rectangular content clipping: "+scroll.transform.parent.name+"/"+scroll.name);
            Check(scroll.content.anchorMin==new Vector2(0,1)&&scroll.content.anchorMax==Vector2.one&&scroll.content.pivot==new Vector2(.5f,1),"Panel content grows down from the top: "+scroll.transform.parent.name+"/"+scroll.name);
        }
        UnityEngine.Object.DestroyImmediate(clone);
    }
    static T Make<T>(string name) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(go, preview);
        return go.AddComponent<T>();
    }

    static void InstalledSkinChecks()
    {
        var scene=EditorSceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/Ingame.unity")return;
        var source=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<CodexCollectionManager>(true)).Single();
        var prepared=SkinCatalogInstaller.Planned.Where(p=>SkinCatalogInstaller.IsInstallable(p,source.skins)).ToArray();
        Check(prepared.Length>=20&&prepared.All(p=>source.skins.Any(s=>s.id==p.id&&s.level==p.level)),"Every approved or existing skin pair is registered, not only the first four");
        Check(source.skins.Select(s=>s.id).Distinct().Count()==source.skins.Length,"Installed skin IDs are unique");
        var isolated=Make<CodexCollectionManager>("Installed skins save and equip routing");isolated.skins=source.skins;
        var cardGo=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EncyclopediaSlot.prefab"));SceneManager.MoveGameObjectToScene(cardGo,preview);
        var card=cardGo.GetComponent<EncyclopediaSlot>();var caption=PanelLayoutRepair.Ref<TextMeshProUGUI>(card,"levelText");
        foreach(var skin in source.skins)
        {
            card.Setup(skin.level,skin.DisplayName,skin.ld,false,()=>{});card.SetCaption(EncyclopediaPanelController.SkinCaption(isolated,skin,false));caption.ForceMeshUpdate(true,true);
            Check(!caption.isTextTruncated&&!caption.isTextOverflowing,"Locked skin condition fits its card without truncation: "+skin.id);
        }
        UnityEngine.Object.DestroyImmediate(cardGo);
        isolated.ApplyLoadedData(Fresh());isolated.Observe(long.MaxValue,long.MaxValue,long.MaxValue,long.MaxValue);
        Check(isolated.OwnedSkinCount==0,"Installed skins cannot unlock before their base ninja is discovered");
        for(int level=1;level<=24;level++)isolated.RegisterDiscovery(level);
        foreach(var skin in source.skins)
        {
            Check(skin.sd&&skin.ld&&skin.sd!=skin.ld&&skin.level>=1&&skin.level<=24,"Distinct SD and LD sprites registered: "+skin.id);
            Check(CharacterArtRefresh.HasCleanAlpha(AssetDatabase.GetAssetPath(skin.sd)),
                "Installed SD has real alpha and clear borders: "+skin.id);
            Check(CharacterArtRefresh.HasCleanAlpha(AssetDatabase.GetAssetPath(skin.ld)),
                "Installed LD has real alpha and clear borders: "+skin.id);
            Check(isolated.Equip(skin.level,skin.id)&&isolated.EquippedSprite(skin.level,false)==skin.sd&&isolated.EquippedSprite(skin.level,true)==skin.ld,"Unlocked pair routes to field and detail correctly: "+skin.id);
        }
        var saved=Fresh();isolated.CollectSaveData(saved);isolated.ApplyLoadedData(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(saved)));
        Check(isolated.OwnedSkinCount==source.skins.Length,"All installed skin discoveries survive save roundtrip");
        foreach(var group in source.skins.GroupBy(s=>s.level).Where(g=>g.Count()==SkinCatalogInstaller.SkinsPerLevel))
        {
            var beforeBonuses=Enum.GetValues(typeof(CodexCollectionManager.Effect)).Cast<CodexCollectionManager.Effect>().Select(isolated.Bonus).ToArray();
            foreach(var skin in group)
            {
                isolated.Equip(skin.level,skin.id);
                var snapshot=Fresh();isolated.CollectSaveData(snapshot);
                isolated.ApplyLoadedData(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(snapshot)));
                Check(isolated.EquippedId(skin.level)==skin.id&&isolated.EquippedSprite(skin.level,false)==skin.sd&&isolated.EquippedSprite(skin.level,true)==skin.ld,
                    "Every same-level skin slot survives equip/save/load with its own SD and LD: "+skin.id);
            }
            Check(group.All(s=>isolated.Owns(s.id))&&Enum.GetValues(typeof(CodexCollectionManager.Effect)).Cast<CodexCollectionManager.Effect>().Select(isolated.Bonus).SequenceEqual(beforeBonuses),
                "Switching all four skins preserves ownership and collection bonuses: level "+group.Key);
        }
        foreach(var id in new[]{"violet-festival","jade-garden","moon-patrol","rose-oath"})Check(isolated.Owns(id)&&Mathf.Approximately(source.skins.Single(s=>s.id==id).bonus,.05f),"Original earned skin ID and benefit preserved: "+id);
        UnityEngine.Object.DestroyImmediate(isolated.gameObject);
    }
    static void RewardAndFieldPresentationChecks()
    {
        var scene = EditorSceneManager.GetActiveScene(); if (scene.path != "Assets/Scenes/Ingame.unity") return;
        var source = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<CodexCollectionManager>(true)).Single();
        var collection = Make<CodexCollectionManager>("Actual field image routing"); collection.skins = source.skins;
        typeof(CodexCollectionManager).GetProperty("Instance").SetValue(null, collection);
        collection.ApplyLoadedData(Fresh()); collection.Observe(long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue);
        for (int lv=1;lv<=24;lv++) collection.RegisterDiscovery(lv);
        var loader = Make<GirlSpriteAddressableLoader>("Field SD-only loader");
        Set(loader,"preloadSD",false); Set(loader,"preloadLD",false);
        var image = Make<Image>("Field skin image"); var girl = image.gameObject.AddComponent<GirlCharacter>();
        girl.enabled = false;
        foreach (var skin in source.skins)
        {
            var data = new GirlData {level=skin.level, spriteName="variant999", name=skin.DisplayName};
            collection.Equip(skin.level,skin.id);
            girl.RefreshAppearance(loader.GetFieldSprite(data));
            Check(image.sprite==skin.sd && loader.GetSpriteForData(data,true)==skin.ld,
                "Real Image uses paired SD; detail uses LD even with non-level asset name: "+skin.id);
        }
        var first=source.skins.First(); var second=source.skins.First(s=>s.level==first.level&&s.id!=first.id);
        image.sprite=first.ld; girl.SetInputLocked(true); girl.RefreshAppearance(second.sd);
        Check(image.sprite==first.ld,"Equipping during discovery does not interrupt the LD presentation");
        girl.SetInputLocked(false);
        Check(image.sprite==second.sd,"Latest equipped SD is applied when the discovery input lock ends");
        collection.Equip(first.level,null);
        var ldDictionary=(System.Collections.Generic.Dictionary<string,Sprite>)loader.SpriteDictLD;
        ldDictionary[first.level.ToString()]=first.ld;
        Check(loader.GetFieldSprite(new GirlData{level=first.level})==null,"A missing SD never silently falls back to LD on the field");
        loader.SpriteDict[first.level.ToString()]=first.sd;
        Check(loader.GetFieldSprite(new GirlData{level=first.level})==first.sd,"Unequipped field returns to its base SD");
        ldDictionary["25"]=first.ld;
        Check(loader.GetFieldSprite(new GirlData{level=25})==first.ld,"Level 25 retains its existing LD-only route");
        var liveCatalog=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<EncyclopediaPanelController>(true)).Single();
        var catalogClone=UnityEngine.Object.Instantiate(liveCatalog.gameObject);SceneManager.MoveGameObjectToScene(catalogClone,preview);
        var catalog=catalogClone.GetComponent<EncyclopediaPanelController>();
        Set(catalog,"dataManager",new GirlDataManager());Set(catalog,"fieldManager",null);Set(catalog,"spriteLoader",loader);
        catalog.ShowSkins();
        var catalogSlots=(Dictionary<string,EncyclopediaSlot>)typeof(EncyclopediaPanelController).GetField("skinSlots",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(catalog);
        foreach(var skin in source.skins)
            Check(catalogSlots[skin.id].gameObject.activeSelf&&PanelLayoutRepair.Ref<Image>(catalogSlots[skin.id],"iconImage").sprite==skin.ld,
                "Actual codex skin card displays its LD illustration: "+skin.id);
        UnityEngine.Object.DestroyImmediate(catalogClone);
        UnityEngine.Object.DestroyImmediate(girl.gameObject); UnityEngine.Object.DestroyImmediate(loader.gameObject); UnityEngine.Object.DestroyImmediate(collection.gameObject);

        var card=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EncyclopediaSlot.prefab"));SceneManager.MoveGameObjectToScene(card,preview);
        var slot=card.GetComponent<EncyclopediaSlot>();slot.Setup(1,"Locked",first.ld,false,()=>{});
        var overlay=PanelLayoutRepair.Ref<GameObject>(slot,"lockedOverlay");var badge=(RectTransform)overlay.transform;
        Check(overlay.activeSelf&&badge.rect.width>=220&&badge.rect.height>=200,"Locked badge is centered and large enough for small phones");
        var lockText=PanelLayoutRepair.Ref<TMP_Text>(slot,"lockedLabel");
        Check(lockText&&lockText.fontSizeMin>=40&&!string.IsNullOrWhiteSpace(lockText.text),"Locked art has a readable text label as well as an icon");
        slot.Setup(1,"Owned",first.ld,true,()=>{});Check(!overlay.activeSelf,"Owned card hides the complete lock badge");UnityEngine.Object.DestroyImmediate(card);

        var liveReward=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<OfflineRewardPanel>(true)).Single();
        var clone=UnityEngine.Object.Instantiate(liveReward.gameObject);SceneManager.MoveGameObjectToScene(clone,preview);
        var reward=clone.GetComponent<OfflineRewardPanel>();var economy=Make<EconomyManager>("Isolated offline claim");
        Set(reward,"economy",economy);Set(reward,"premiumCurrency",null);Set(reward,"adServiceBehaviour",null);
        typeof(OfflineRewardPanel).GetMethod("BindButtons",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(reward,null);
        var root=PanelLayoutRepair.Ref<GameObject>(reward,"panelRoot");var panel=(RectTransform)root.transform.Find("Reward Card");
        var ad=PanelLayoutRepair.Ref<Button>(reward,"claimAdButton");var claim=PanelLayoutRepair.Ref<Button>(reward,"claimJustButton");
        var text=PanelLayoutRepair.Ref<TextMeshProUGUI>(reward,"rewardText");var duration=PanelLayoutRepair.Ref<TextMeshProUGUI>(reward,"durationText");
        var regions=new[]{PanelLayoutRepair.Ref<TMP_Text>(reward,"titleText").rectTransform,text.rectTransform,duration.rectTransform,(RectTransform)ad.transform,(RectTransform)claim.transform};
        Rect Bounds(RectTransform r){return new Rect(r.anchoredPosition-r.rect.size*.5f,r.rect.size);}
        for(int a=0;a<regions.Length;a++)for(int b=a+1;b<regions.Length;b++)Check(!Bounds(regions[a]).Overlaps(Bounds(regions[b])),"Offline popup regions do not overlap: "+a+"/"+b);
        foreach(var r in regions)Check(panel.rect.Contains(Bounds(r).min)&&panel.rect.Contains(Bounds(r).max),"Offline popup region stays inside card: "+r.name);
        Check(((RectTransform)ad.transform).rect.height>=180&&((RectTransform)claim.transform).rect.height>=180,"Both offline reward actions have 48px+ phone targets");
        Check(root.GetComponent<Image>().raycastTarget&&((RectTransform)root.transform).anchorMin==Vector2.zero&&((RectTransform)root.transform).anchorMax==Vector2.one,"Offline modal blocks clicks through the full screen");
        foreach(double seconds in new[]{60d,10800d,36000000d})
        {
            reward.Show(new OfflineRewardPanel.Payload{rawSeconds=seconds,appliedSeconds=Math.Min(seconds,7200),totalReward=123456789});
            text.ForceMeshUpdate(true,true);duration.ForceMeshUpdate(true,true);
            Check(root.activeSelf&&!text.isTextOverflowing&&!duration.isTextOverflowing,"Offline duration and amount fit: "+seconds);
        }
        Check(!ad.gameObject.activeSelf&&claim.gameObject.activeSelf,"Offline reward remains claimable without an ad service");
        var before=economy.GetGold();claim.onClick.Invoke();claim.onClick.Invoke();
        Check(economy.GetGold()==before+123456789&&!root.activeSelf,"Normal offline reward is granted exactly once and closes the popup");
        UnityEngine.Object.DestroyImmediate(clone);UnityEngine.Object.DestroyImmediate(economy.gameObject);
    }
    static void CharacterNameChecks()
    {
        Check(File.ReadAllText("Assets/Scripts/Girl/GirlCharacter.cs").Contains("IncomeActivityManager.Instance?.RecordCharacterClick()"),"Confirmed character taps feed the fever meter");
        var rows=File.ReadAllLines("Assets/Data/girl.csv").Skip(1).Where(l=>!string.IsNullOrWhiteSpace(l)).Select(l=>l.Split(',')).Where(r=>int.Parse(r[2])<=24).ToArray();
        Check(rows.Length==24&&rows.All(r=>!string.IsNullOrWhiteSpace(r[1]))&&rows.Select(r=>r[1]).Distinct().Count()==24,"Base levels 1-24 have distinct non-empty display names");
        var englishNames=Enumerable.Range(1,25).Select(GirlData.EnglishName).ToArray();
        Check(englishNames.All(n=>!string.IsNullOrWhiteSpace(n))&&englishNames.Distinct().Count()==25,"Base levels 1-25 have distinct non-empty English display names");
        var plan=SkinCatalogInstaller.Planned;
        Check(plan.Length==96&&plan.Length*2==192&&Enumerable.Range(1,24).All(level=>plan.Where(p=>p.level==level).Select(p=>p.slot).OrderBy(s=>s).SequenceEqual(new[]{0,1,2,3})),"Confirmed skin target is four per level, 96 pairs / 192 images excluding base appearances");
        bool rejectsMissing=false;try{SkinCatalogInstaller.ValidatePlan(plan.Skip(1).ToArray());}catch(InvalidOperationException){rejectsMissing=true;}
        Check(rejectsMissing,"A missing planned skin cannot silently reduce the confirmed target");
        var duplicateSlot=plan.Select(p=>new SkinCatalogInstaller.Item{id=p.id,level=p.level,slot=p.slot}).ToArray();duplicateSlot[1].slot=duplicateSlot[0].slot;
        bool rejectsDuplicateSlot=false;try{SkinCatalogInstaller.ValidatePlan(duplicateSlot);}catch(InvalidOperationException){rejectsDuplicateSlot=true;}
        Check(rejectsDuplicateSlot,"Duplicate per-level skin slots are rejected even when IDs are unique");
        var readyItem=plan.First(SkinCatalogInstaller.IsPrepared);
        var reviewItem=new SkinCatalogInstaller.Item{id=readyItem.id,level=readyItem.level,slot=readyItem.slot,qualityStatus="needs-reinspection"};
        Check(!SkinCatalogInstaller.IsInstallable(reviewItem,Array.Empty<CodexCollectionManager.Skin>()),"File presence alone cannot install an unreviewed new skin");
        Check(SkinCatalogInstaller.IsInstallable(reviewItem,new[]{new CodexCollectionManager.Skin{id=reviewItem.id}}),"Quality reinspection does not remove an already installed player skin");
        reviewItem.qualityStatus="approved";
        Check(SkinCatalogInstaller.IsInstallable(reviewItem,Array.Empty<CodexCollectionManager.Skin>()),"A complete approved pair can be installed");
        reviewItem.id="__missing_approved_skin__";
        Check(!SkinCatalogInstaller.IsInstallable(reviewItem,Array.Empty<CodexCollectionManager.Skin>()),"Approval cannot install a missing SD/LD file pair");
        Check(plan.All(p=>!string.IsNullOrWhiteSpace(p.koreanName)&&!string.IsNullOrWhiteSpace(p.englishName))&&plan.Select(p=>p.koreanName).Distinct().Count()==plan.Length&&plan.Select(p=>p.englishName).Distinct().Count()==plan.Length,"Planned skin display names are populated and unique in both languages");
        var original=new CodexCollectionManager.Skin{id="rename-test",level=6,koreanName="Old",englishName="Old",requirement=CodexCollectionManager.Requirement.Clicks,target=125,effect=CodexCollectionManager.Effect.Income,bonus=.05f};
        SkinCatalogInstaller.ApplyDisplayNames(original,new SkinCatalogInstaller.Item{id="rename-test",koreanName="수정 이름",englishName="Corrected name"});
        Check(original.koreanName=="수정 이름"&&original.englishName=="Corrected name"&&original.id=="rename-test"&&original.level==6&&original.target==125&&original.requirement==CodexCollectionManager.Requirement.Clicks&&original.effect==CodexCollectionManager.Effect.Income&&Mathf.Approximately(original.bonus,.05f),"Renaming existing skins preserves identity, stage, unlock requirement and earned benefit");
        var scene=EditorSceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/Ingame.unity")return;
        var collection=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<CodexCollectionManager>(true)).Single();
        foreach(var item in plan.Where(p=>SkinCatalogInstaller.IsInstallable(p,collection.skins)))
        {
            var skin=collection.skins.Single(s=>s.id==item.id);
            Check(skin.koreanName==item.koreanName&&skin.englishName==item.englishName,"Installed skin uses current reviewed display names: "+item.id);
        }
    }
    static void CollectionChecks()
    {
        var c=Make<CodexCollectionManager>("Isolated collection");
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/닌자SD2/1.png");
        c.skins=Enumerable.Range(0,4).Select(i=>new CodexCollectionManager.Skin{id="test"+i,level=i+1,sd=sprite,ld=sprite,requirement=(CodexCollectionManager.Requirement)i,target=10,effect=(CodexCollectionManager.Effect)i,bonus=.02f}).ToArray();
        c.ApplyLoadedData(Fresh());Check(c.DiscoveredCount==0&&c.OwnedSkinCount==0,"Fresh codex starts empty");
        c.Observe(100,100,100,100);Check(c.OwnedSkinCount==0,"Skin progress alone does not skip base discovery");
        for(int level=1;level<=4;level++)c.RegisterDiscovery(level);
        Check(c.OwnedSkinCount==4,"Discovery and progress unlock matching skins");
        foreach(CodexCollectionManager.Effect effect in Enum.GetValues(typeof(CodexCollectionManager.Effect)))Check(Math.Abs(c.Bonus(effect)-.03)<.00001,"Base and owned skin bonuses add once: "+effect);
        c.RegisterDiscovery(1);c.Observe(100,100,100,100);Check(Math.Abs(c.Bonus(CodexCollectionManager.Effect.Income)-.03)<.00001,"Repeated observations never duplicate collection bonuses");
        Check(c.Equip(1,"test0"),"Owned correct-level skin equips");Check(c.EquippedSprite(1,false)==sprite&&c.EquippedSprite(1,true)==sprite,"Equipped skin has separate SD and LD routing");
        Check(!c.Equip(2,"test0")&&!c.Equip(25,"test0")&&!c.Equip(0,"test0"),"Wrong level and mythic skin equips rejected");
        Check(Math.Abs(c.Bonus(CodexCollectionManager.Effect.Income)-.03)<.00001,"Equipping does not add collection rewards again");
        var saved=Fresh();c.CollectSaveData(saved);saved.totalClicks=saved.totalMerges=saved.totalSpawns=100;saved.totalPrestigeCount=100;
        saved=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(saved));c.ApplyLoadedData(saved);
        Check(c.OwnedSkinCount==4&&c.EquippedId(1)=="test0","Owned skins and equipment survive JSON save roundtrip");
        Check(c.Equip(1,null)&&c.EquippedSprite(1,false)==null,"Default appearance can be restored");
        var legacy=Fresh();legacy.discoveredMask=3;c.ApplyLoadedData(legacy);Check(c.DiscoveredCount==2&&c.OwnedSkinCount==0,"Legacy saves migrate discoveries without granting unfinished skins");
        legacy.ownedSkinIds=new List<string>{"future-skin"};legacy.equippedSkins=new List<EquippedSkinSave>{new EquippedSkinSave{level=1,skinId="future-skin"}};c.ApplyLoadedData(legacy);var roundtrip=Fresh();c.CollectSaveData(roundtrip);
        Check(roundtrip.ownedSkinIds.Contains("future-skin")&&c.EquippedSprite(1,false)==null,"Unknown future skin IDs are preserved without invalid sprite routing");
        c.ApplyLoadedData(Fresh());Check(c.DiscoveredCount==0&&c.OwnedSkinCount==0&&c.EquippedId(1)==null,"Fresh reset clears all codex state");
        UnityEngine.Object.DestroyImmediate(c.gameObject);
    }
    static void ReadabilityChecks()
    {
        var scene=EditorSceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/Ingame.unity")return;
        var canvas=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Canvas>(true)).First(c=>c.isRootCanvas);
        var nav=canvas.transform.Find("GameObject");
        foreach(string name in new[]{"소환","자동소환","소환버튼","자동합성","상점","도감","옵션"})
        {
            var r=nav.Find(name) as RectTransform;
            Check(r&&r.rect.width*r.localScale.x>=164&&r.rect.height*r.localScale.y>=164,"Main control has a physical 48px+ phone target: "+name);
        }
        string[] bottomNames={"소환","자동소환","소환버튼","자동합성","상점","fill"};
        var initialSizes=new[]{new Vector2(168,230),new Vector2(204.8f,204.8f),new Vector2(250,250),new Vector2(189.44f,189.44f),new Vector2(176,234),new Vector2(250,250)};
        var initialPositions=new[]{new Vector2(-410,68),new Vector2(-208,68),new Vector2(0,68),new Vector2(208,82),new Vector2(410,68),new Vector2(0,68)};
        for(int n=0;n<bottomNames.Length;n++)
        {
            var r=(RectTransform)nav.Find(bottomNames[n]);
            var visibleSize=Vector2.Scale(r.rect.size,r.localScale);
            Check(Vector2.Distance(visibleSize,initialSizes[n])<.01f&&Vector2.Distance(r.anchoredPosition,initialPositions[n])<.001f&&r.pivot==new Vector2(.5f,0)&&r.anchorMin==new Vector2(.5f,0)&&r.anchorMax==r.anchorMin,"Bottom control keeps its original size and bottom pivot: "+bottomNames[n]);
        }
        foreach(string name in new[]{"자동소환","자동합성"})
        {
            var art=nav.Find(name+"/"+name+" (1)").GetComponent<Image>();
            Check(art.rectTransform.rect.size==new Vector2(1024,1024)&&art.rectTransform.pivot==new Vector2(.5f,0)&&!art.preserveAspect,"Automatic button art is not shrunk inside a replacement rectangle: "+name);
        }
        var scrollBase=nav.Find("소환버튼").GetComponent<Image>();var scrollFill=nav.Find("fill").GetComponent<Image>();
        var baseCorners=new Vector3[4];var fillCorners=new Vector3[4];scrollBase.rectTransform.GetWorldCorners(baseCorners);scrollFill.rectTransform.GetWorldCorners(fillCorners);
        Check(Enumerable.Range(0,4).All(i=>Vector3.Distance(baseCorners[i],fillCorners[i])<.01f)&&!scrollBase.preserveAspect&&!scrollFill.preserveAspect&&scrollBase.sprite==scrollFill.sprite&&scrollFill.type==Image.Type.Filled&&scrollFill.fillMethod==Image.FillMethod.Vertical,"Scroll background and charge fill render the same original geometry");
        foreach(string name in new[]{"소환","상점","도감"})Check(AssetDatabase.GetAssetPath(nav.Find(name).GetComponent<Image>().sprite)=="Assets/Prefabs/sprite/버튼.png","Familiar labelled navigation restored: "+name);
        foreach(string name in new[]{"소환","상점","도감"})
        {
            var localized=nav.Find(name).GetComponent<LocalizedTextureLabel>();
            Check(localized&&localized.enabled&&localized.englishOnly&&localized.label&&localized.backdrop,"Original navigation has a non-destructive English-only caption: "+name);
        }
        foreach(string name in new[]{"자동소환","자동합성"})
        {
            var root=nav.Find(name);
            var localized=root.GetComponent<LocalizedTextureLabel>();
            Check(localized&&localized.englishOnly&&localized.label&&localized.backdrop,"Automatic control keeps original Korean art with an English-only caption: "+name);
            Check(root.GetComponentsInChildren<Image>(true).Any(i=>i.enabled&&i.sprite&&AssetDatabase.GetAssetPath(i.sprite).StartsWith("Assets/Prefabs/sprite/"+name)),"Original automatic-control artwork is preserved: "+name);
            foreach(string state in new[]{"ㅇ","x"})
                Check(HasTransparentOuterBorder("Assets/Prefabs/sprite/"+name+state+".png"),"Automatic control has a genuinely transparent outer border: "+name+state);
        }
        var incomeRow=canvas.transform.Find("위쪽상태창/Image/GameObject (3)");
        var incomeIcon=incomeRow.Find("Image (2)").GetComponent<Image>();
        Check(incomeIcon.gameObject.activeInHierarchy&&incomeIcon.enabled&&incomeIcon.color.a>.9f&&AssetDatabase.GetAssetPath(incomeIcon.sprite)=="Assets/Prefabs/sprite/골포.png","Original income coin icon is visible");
        var economy=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<EconomyManager>(true)).Single();
        var income=(TMPro.TMP_Text)new SerializedObject(economy).FindProperty("goldPerSecText").objectReferenceValue;
        foreach(string sample in new[]{income.text,"+999.9 aG/s (×112.5%)"})
        {
            string savedIncome=income.text;income.text=sample;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)incomeRow);
            var iconCorners=new Vector3[4];var textCorners=new Vector3[4];
            incomeIcon.rectTransform.GetWorldCorners(iconCorners);income.rectTransform.GetWorldCorners(textCorners);
            float left=incomeRow.parent.InverseTransformPoint(iconCorners[0]).x;
            float iconRight=incomeRow.parent.InverseTransformPoint(iconCorners[2]).x;
            float textLeft=incomeRow.parent.InverseTransformPoint(textCorners[0]).x;
            float right=incomeRow.parent.InverseTransformPoint(textCorners[2]).x;
            Check(income.transform.parent==incomeRow&&iconRight<textLeft,"Income icon and live amount share one row without overlap: "+sample);
            Check(Mathf.Abs((left+right)/2)<1,"Income icon and amount remain centered together: "+sample);
            income.text=savedIncome;
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)incomeRow);
        var hud=canvas.GetComponentInChildren<IncomeActivityHUD>(true);
        Check(!hud.feverHint.gameObject.activeSelf,"Fever remains simple with no instruction text");
        Check(hud.feverFill.transform.parent.GetComponent<Image>().color.a>.9f,"Fever empty track has a visible solid background");
        var feverTrack=(RectTransform)hud.feverFill.transform.parent;
        Check(feverTrack.anchorMin==new Vector2(0,0)&&feverTrack.anchorMax==new Vector2(1,0)&&hud.feverFill.rectTransform.anchorMin==Vector2.zero&&hud.feverFill.rectTransform.anchorMax==Vector2.one&&hud.feverFill.rectTransform.offsetMin==Vector2.zero&&hud.feverFill.rectTransform.offsetMax==Vector2.zero,"Fever track and fill use responsive horizontal stretch anchors");
        var pop=hud.transform.parent.Find("Population Left");Check(pop&&pop.gameObject.activeSelf&&((RectTransform)pop).anchorMin.x==0,"Population is visible on the left");
        Check(!canvas.GetComponentsInChildren<Image>(true).Any(i=>i.sprite&&AssetDatabase.GetAssetPath(i.sprite).StartsWith("Assets/Art/UI/Refresh/")),"Rejected experimental UI icons are not shown in the scene");
        var summonController=canvas.GetComponentInChildren<SummonPanelController>(true);
        var summonContent=(RectTransform)new SerializedObject(summonController).FindProperty("content").objectReferenceValue;
        var summonPanel=summonContent.GetComponentInParent<ScrollRect>(true).transform.parent;
        foreach(var root in new[]{canvas.transform.Find("Shop"),summonPanel})
        {
            var localized=root.GetComponent<LocalizedTextureLabel>();
            Check(localized&&localized.englishOnly&&localized.label&&localized.backdrop,"Baked panel title has a non-destructive English-only caption: "+root.name);
        }
        Check(!hud.boostButton.image.preserveAspect,"Wide income reward is not squeezed into a square texture");
        Check(canvas.GetComponentsInChildren<ScrollRect>(true).All(s=>!s.GetComponent<Image>()||s.GetComponent<Image>().color.a==0),"Scroll backing images do not obscure the original panel art");
        var slot=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EncyclopediaSlot.prefab");
        var serializedSlot=new SerializedObject(slot.GetComponent<EncyclopediaSlot>());
        var cardButton=(Button)serializedSlot.FindProperty("button").objectReferenceValue;
        var caption=(TMPro.TMP_Text)serializedSlot.FindProperty("levelText").objectReferenceValue;
        Check(cardButton.image.color.a==0&&cardButton.transition==Selectable.Transition.None,"Transparent card hit area cannot cover portraits and captions");
        string korean="닌자스킨수익클릭합성생성미해금조건장착수집완료"+string.Concat(scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<CodexCollectionManager>(true)).Single().skins.Select(s=>s.koreanName));
        bool dynamicKoreanFont=caption.font.atlasPopulationMode==AtlasPopulationMode.Dynamic&&caption.font.sourceFontFile!=null;
        Check(dynamicKoreanFont||korean.All(c=>char.IsWhiteSpace(c)||caption.font.HasCharacter(c)),"Codex caption font can generate Korean UI glyphs");
        var frame=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/sprite/cell_bg_rounded_1024x256.png");
        Check(frame.border.x>0&&frame.border.y>0,"Original rounded panel uses nine-slicing to preserve its corners");
        var catalog=canvas.GetComponentInChildren<EncyclopediaPanelController>(true);
        Check(!catalog.collectionCount.text.Contains("미리보기"),"Saved scene contains no codex preview captions");
        Check(canvas.transform.Find("설정패널").gameObject.activeSelf&&!canvas.transform.Find("설정패널/Panel").gameObject.activeSelf,"Settings controller stays active while its dialog is closed");
        var tabTest=Make<EncyclopediaPanelController>("Isolated tab selection");
        tabTest.ninjaTab=Make<Button>("Ninja tab");tabTest.ninjaTab.targetGraphic=tabTest.ninjaTab.gameObject.AddComponent<Image>();
        tabTest.skinTab=Make<Button>("Skin tab");tabTest.skinTab.targetGraphic=tabTest.skinTab.gameObject.AddComponent<Image>();
        tabTest.RefreshTabs(false);var selected=tabTest.ninjaTab.image.color;var normal=tabTest.skinTab.image.color;
        Check(selected!=normal,"Active codex tab has distinct visual emphasis");
        tabTest.RefreshTabs(true);Check(tabTest.ninjaTab.image.color==normal&&tabTest.skinTab.image.color==selected,"Tab emphasis follows the selected collection category");
    }

    static bool HasTransparentOuterBorder(string path)
    {
        if(!File.Exists(path))return false;
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
        try
        {
            if(!ImageConversion.LoadImage(texture,File.ReadAllBytes(path)))return false;
            var pixels=texture.GetPixels32();int empty=0,opaque=0;
            for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)
            {
                byte alpha=pixels[y*texture.width+x].a;
                if(alpha==0)empty++;if(alpha>=250)opaque++;
                if((x==0||y==0||x==texture.width-1||y==texture.height-1)&&alpha!=0)return false;
            }
            return empty>pixels.Length*.2f&&opaque>pixels.Length*.05f;
        }
        finally{UnityEngine.Object.DestroyImmediate(texture);}
    }

    static void SceneIntegrityChecks()
    {
        var scene=EditorSceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/Ingame.unity")return;
        var roots=scene.GetRootGameObjects();
        var transforms=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        Check(transforms.All(t=>t.gameObject.GetComponents<Component>().All(c=>c)),"Ingame scene contains no missing MonoBehaviour components");
        Check(roots.SelectMany(r=>r.GetComponentsInChildren<EventSystem>(true)).Count()==1,"Ingame scene contains exactly one EventSystem");
        Check(roots.SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)).Count(c=>c.isRootCanvas)==1,"Ingame scene contains exactly one root Canvas");
        Check(roots.SelectMany(r=>r.GetComponentsInChildren<AudioListener>(true)).Count()==1,"Ingame scene contains exactly one AudioListener");
        Check(roots.SelectMany(r=>r.GetComponentsInChildren<SaveManager>(true)).Count()==1,"Ingame scene contains exactly one SaveManager");
        Check(roots.SelectMany(r=>r.GetComponentsInChildren<CloudSaveManager>(true)).Count()==1,"Ingame scene contains exactly one CloudSaveManager");
        Check(roots.SelectMany(r=>r.GetComponentsInChildren<EconomyManager>(true)).Count()==1,"Ingame scene contains exactly one EconomyManager");
        Check(roots.SelectMany(r=>r.GetComponentsInChildren<PremiumCurrencyManager>(true)).Count()==1,"Ingame scene contains exactly one PremiumCurrencyManager");
        Check(roots.SelectMany(r=>r.GetComponentsInChildren<GirlFieldManager>(true)).Count()==1,"Ingame scene contains exactly one GirlFieldManager");
        Check(roots.SelectMany(r=>r.GetComponentsInChildren<RewardedAdsManager_AdMob>(true)).Count()==1,"Ingame scene contains exactly one rewarded-ad manager");
        Check(roots.SelectMany(r=>r.GetComponentsInChildren<AdMobOfferService>(true)).Count()==1,"Ingame scene contains exactly one AdMob offer service");
        foreach(var automation in roots.SelectMany(r=>r.GetComponentsInChildren<AutoAutomationController>(true)))
            Check(automation.economy&&automation.combinedLabel&&automation.levelText&&automation.costText&&automation.buyOrUpgradeButton&&automation.buyIconTarget&&automation.buyIconSprite&&automation.upgradeIconSprite&&automation.maxIconSprite&&automation.toggleButton&&automation.toggleIconTarget&&automation.toggleOnSprite&&automation.toggleOffSprite&&automation.fillImage&&automation.reasonLabel,
                "Automation controller has every required scene reference: "+automation.type);
        foreach(var button in roots.SelectMany(r=>r.GetComponentsInChildren<Button>(true)).Where(b=>b.gameObject.activeInHierarchy&&b.interactable))
            Check(button.targetGraphic,"Visible interactable button has a target graphic: "+button.name);
    }

    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("Regression: " + name);
        passed.Add(name);
    }

    static void Set(object target, string field, object value) => target.GetType()
        .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

    static SaveData Fresh() => new SaveData { permanentUpgradeLevels = new int[8] };

    static void SaveCompatibilityChecks()
    {
        var older = new SaveData { totalPlayTimeSeconds = 100, maxLevelReached = 5 };
        older.SetSaveTime();
        var newerProgress = new SaveData { totalPlayTimeSeconds = 200, maxLevelReached = 4 };
        newerProgress.SetSaveTime();
        Check(SaveManager.CompareSavePriority(newerProgress, older) > 0,
            "Cloud/local comparison prefers longer tracked play time");

        var legacyOlder = new SaveData { totalPlayTimeSeconds = 0, maxLevelReached = 12, savedAt = "2025-01-01 00:00:00" };
        var legacyNewer = new SaveData { totalPlayTimeSeconds = 0, maxLevelReached = 12, savedAt = "2025-01-02 00:00:00" };
        Check(SaveManager.CompareSavePriority(legacyNewer, legacyOlder) > 0,
            "Legacy saves without play statistics compare by save time");

        var timestamped = new SaveData();
        timestamped.SetSaveTime();
        Check(timestamped.savedAtUtcUnixSeconds > 0 && SaveManager.TryGetSaveTimeUtc(timestamped, out _),
            "New saves carry an unambiguous UTC timestamp");
    }

    static void RewardChecks()
    {
        Check(new PrestigeReward(1, null, 25, 0, 1).TotalPoints == 2500, "First mythic = 2500 points");
        Check(new PrestigeReward(3, null, 25, 0, 1).FinalPoints == 4500, "Mythic stacks grow linearly");
        var reward = new PrestigeReward(1, new[] { 24, 23, 25, 0 }, 25, 10, 1.2);
        Check(reward.FieldPoints == 1875, "Lower tiers counted; final not double counted");
        Check(reward.TotalPoints == 6450, "Upgrade points and multiplier agree");
        Check(new PrestigeReward(0, null, 25, 0, 1).TotalPoints == 0, "Empty field reward");
        Check(new PrestigeReward(int.MaxValue, null, 25, int.MaxValue, 20).TotalPoints == int.MaxValue,
            "Large prestige values saturate instead of overflowing");
        Check(PrestigeReward.ClampPoints((double)int.MaxValue + 100) == int.MaxValue, "Point balance saturates");

        var legacy = Make<LegacyRankManager>("Legacy balance");
        legacy.ApplyLoadedData(new SaveData { dataVersion = 5, legacyXp = 2500, totalPrestigeCount = 3 });
        Check(legacy.LegacyLevel == 3, "Old point-based legacy XP migrates to actual prestige count");
        var migrated = Fresh(); legacy.CollectSaveData(migrated);
        Check(migrated.legacyXp == 3, "Migrated legacy progress saves in v6 units");
        legacy.ApplyLoadedData(Fresh()); legacy.AddXp(1);
        Check(legacy.LegacyLevel == 1, "One prestige grants exactly one legacy level");
        legacy.AddXp(100);
        Check(legacy.LegacyLevel == LegacyRankManager.MaxLegacyLevel, "Legacy progression respects its level cap");

        Check(SummonPanelController.CalculateGemSummonCost(1, 0) == 3, "Early gem summon remains accessible");
        Check(SummonPanelController.CalculateGemSummonCost(9, 3) == 14, "Mid-tier gem summon scales by two");
        Check(SummonPanelController.CalculateGemSummonCost(23, 0) == 20, "Top-tier gem summon starts at 20 gems");
        Check(Enumerable.Range(0, 4).Sum(i => SummonPanelController.CalculateGemSummonCost(23, i)) == 110,
            "Four top-tier summons cost 110 gems before the first final stack");
    }

    static void EconomyChecks()
    {
        var economy = Make<EconomyManager>("Economy check");
        for (int index = 0; index < 8; index++)
        {
            economy.ApplyLoadedData(Fresh());
            var kind = (EconomyManager.UpgradeKind)index;
            Check(economy.TryBuyUpgradeWithGems(kind), $"Gem purchase works with zero gold: {kind}");
            var data = Fresh(); economy.CollectSaveData(data);
            Check(data.gold == 0 && data.permanentUpgradeLevels[index] == 1, $"No gold charged: {kind}");
            Check(economy.GetResettableUpgradeLevels() == 0, $"Permanent purchase has no repeat reward: {kind}");
            economy.ResetGoldUpgradesForPrestige();
            Check(economy.GetPermanentUpgradeLevel(kind) == 1, $"Survives prestige: {kind}");
            var roundTrip = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
            economy.ApplyLoadedData(roundTrip);
            Check(economy.GetPermanentUpgradeLevel(kind) == 1, $"Save round trip: {kind}");
        }

        var mixed = Fresh(); mixed.gold = 1000000;
        economy.ApplyLoadedData(mixed);
        economy.TryBuySpawnMaxUpgrade();
        economy.TryBuyUpgradeWithGems(EconomyManager.UpgradeKind.ManualSpawnMax);
        Check(economy.GetSpawnMaxUpgradeLevel() == 2 && economy.GetResettableUpgradeLevels() == 1,
            "Mixed currency upgrades tracked separately");
        economy.ResetGoldUpgradesForPrestige();
        Check(economy.GetSpawnMaxUpgradeLevel() == 1 && economy.GetResettableUpgradeLevels() == 0,
            "Only consumed gold upgrade is reset");

        economy.ApplyLoadedData(new SaveData { dataVersion = 2, manualSpawnMaxUpgrade = 3, autoMergeUpgrade = 2 });
        economy.ResetGoldUpgradesForPrestige();
        Check(economy.GetSpawnMaxUpgradeLevel() == 3 && economy.GetAutoMergeUpgradeLevel() == 2,
            "Legacy currency-unknown entitlements are protected");

        var capped = Fresh(); capped.manualSpawnMaxUpgrade = economy.GetSpawnMaxUpgradeCap();
        economy.ApplyLoadedData(capped);
        Check(!economy.TryBuyUpgradeWithGems(EconomyManager.UpgradeKind.ManualSpawnMax), "Capped gem purchase rejected");
        var toggles = Fresh(); toggles.autoMergeUpgrade = 2; toggles.autoMergeOn = true;
        toggles.permanentUpgradeLevels[6] = 1;
        economy.ApplyLoadedData(toggles); economy.ResetGoldUpgradesForPrestige();
        Check(economy.GetAutoMergeUpgradeLevel() == 1 && economy.IsAutoMergeOn(), "Permanent automation keeps its toggle");
    }

    static void PrestigeChecks()
    {
        var economy = Make<EconomyManager>("Prestige economy");
        var data = Fresh(); data.manualSpawnMaxUpgrade = 3;
        economy.ApplyLoadedData(data);
        var field = Make<GirlFieldManager>("Prestige field");
        field.economy = economy;
        field.curSpawnCharge = 0;
        var merge = Make<GirlMergeManager>("Merge");
        field.mergeManager = merge; merge.fieldManager = field;
        var pool = Make<SimpleUIPool>("Pool"); SimpleUIPool.Instance = pool;
        var girl = Make<GirlCharacter>("Final girl"); girl.Level = 25;
        field.girlList.Add(girl);
        Set(field, "level25UpgradeLevel", 1);
        var discovered = (HashSet<int>)typeof(GirlFieldManager)
            .GetField("discoveredLevels", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(field);
        discovered.Add(2);
        discovered.Add(25);
        var tiers = Make<TierManager>("Tier");
        tiers.TryUnlockByLevel(25); tiers.ForceSwitchTo(3);
        var prestige = Make<PrestigeManager>("Prestige");
        prestige.girlFieldManager = field; prestige.economy = economy; prestige.tierManager = tiers;
        Set(prestige, "prestigeGainLv", 2);
        int previewPoints = prestige.GetPrestigeReward().TotalPoints;
        Check(previewPoints == 3360, "Preview includes gold upgrades before reset");
        Check(merge.CanAutoMergeLevel(1) && !merge.CanAutoMergeLevel(2), "Auto merge respects lifetime discovery");
        Check(!merge.CanAutoMergeLevel(25), "Final girls cannot auto merge");
        prestige.DoPrestige();
        Check(prestige.GetPrestigePoints() == previewPoints, "Actual payout equals the preview");
        Check(prestige.GetTotalPrestigeCount() == 1 && !prestige.IsPrestigeReady(), "Prestige ends exactly once");
        Check(economy.GetSpawnMaxUpgradeLevel() == 0, "Gold upgrades reset after payout snapshot");
        Check(field.Level25UpgradeLevel == 0 && field.girlList.Count == 0, "Old field and mythic stack cleared");
        Check(field.IsLevelDiscovered(25), "Encyclopedia survives prestige");
        Check(field.curSpawnCharge == economy.GetMaxManualSpawnCount(), "Summon charges refilled");
        Check(tiers.CurrentTierIndex == 0 && !tiers.IsUnlocked(1), "World reset is immediate");
        prestige.DoPrestige();
        Check(prestige.GetPrestigePoints() == previewPoints && prestige.GetTotalPrestigeCount() == 1,
            "Repeated confirm cannot duplicate rewards");

        var capped = Fresh();
        capped.prestigePoint = int.MaxValue;
        capped.prestigeShopTwoStepLv = PrestigeManager.TwoStepCap;
        capped.ppManualSpawnSpeedLv = PrestigeManager.ManualSpawnSpeedCap;
        capped.ppAutoMergeLv = PrestigeManager.AutoMergeSpeedCap;
        prestige.ApplyLoadedData(capped);
        Check(prestige.GetTwoStepChance() == 0.10f && !prestige.TryBuyTwoStep(),
            "Two-step shop stops at the 10 percent share reserved beside legacy");
        Check(Math.Abs(prestige.GetManualSpawnIntervalMul() - 0.50) < 1e-9 && !prestige.TryBuyPlusManualSpeed(),
            "Manual speed cannot consume points after its effect floor");
        Check(Math.Abs(prestige.GetAutoMergeIntervalMul() - 0.50) < 1e-9 && !prestige.TryBuyPlusAutoMerge(),
            "Auto merge cannot consume points after its effect floor");
    }

    static void ExistingFlowChecks()
    {
        Check(TierRules.MaxLevel == 25, "Existing 25-level progression preserved");
        var tiers = Make<TierManager>("World flow");
        tiers.TryUnlockByLevel(8);
        Check(!tiers.IsUnlocked(1), "Level 8 does not unlock the second world");
        tiers.TryUnlockByLevel(9);
        Check(tiers.IsUnlocked(1) && !tiers.IsUnlocked(2), "Level 9 unlocks only world two");
        tiers.TryUnlockByLevel(17);
        Check(tiers.IsUnlocked(2) && !tiers.IsUnlocked(3), "Level 17 unlocks world three");
        tiers.TryUnlockByLevel(25);
        for (int tier = 0; tier < 4; tier++)
        {
            tiers.ForceSwitchTo(tier);
            Check(tiers.ComputeNextTierByRule() == (tier + 1) % 4, "World navigation cycle: " + tier);
        }
        var save = Fresh(); tiers.CollectSaveData(save);
        tiers.ResetTierUnlocks(); tiers.ApplyLoadedData(save);
        Check(tiers.IsUnlocked(3), "Saved world unlocks restored");

        var loader = Make<GirlSpriteAddressableLoader>("Sprite key check");
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        foreach (string label in new[] { "SD", "LD" })
        {
            var entries = settings.groups.Where(group => group != null)
                .SelectMany(group => group.entries).Where(entry => entry.labels.Contains(label)).ToArray();
            Check(entries.Length == 25, label + " still has exactly 25 registered sprites");
            for (int level = 1; level <= 25; level++)
            {
                var matches = entries.Where(entry => entry.address == level.ToString()).ToArray();
                Check(matches.Length == 1 && AssetDatabase.LoadAssetAtPath<Sprite>(matches[0].AssetPath) != null,
                    label + " level key remains unique and resolves: " + level);
                Check(AssetDatabase.LoadAssetAtPath<Sprite>(matches[0].AssetPath).name == level.ToString(),
                    label + " runtime sprite-name key agrees with address: " + level);
            }
        }
        foreach (int level in new[] { 6, 14 })
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Characters/SD/{level}.png");
            loader.SpriteDict[level.ToString()] = sprite;
            Check(loader.GetSpriteForData(new GirlData { level = level, spriteName = level.ToString() }) == sprite,
                "Retouched sprite uses the unchanged saved level: " + level);
            Check(sprite.texture.alphaIsTransparency, "Retouched sprite imported with alpha: " + level);
        }
    }

    static void MobilePresentationChecks()
    {
        var field = Make<GirlFieldManager>("HUD field bounds");
        var root = Make<Image>("Field coordinate space").rectTransform;
        root.sizeDelta = new Vector2(1080, 1920);
        field.girlRoot = root;
        field.topHud = Make<Image>("Fever exclusion").rectTransform;
        field.topHud.SetParent(root, false);
        field.topHud.sizeDelta = new Vector2(860, 210);
        field.topHud.anchoredPosition = new Vector2(-50, 625);
        field.bottomHud = Make<Image>("Reward exclusion").rectTransform;
        field.bottomHud.SetParent(root, false);
        field.bottomHud.sizeDelta = new Vector2(960, 176);
        field.bottomHud.anchoredPosition = new Vector2(0, -540);
        var bounds = field.GetMovementBounds(150, 150);
        Check(Mathf.Abs(bounds.yMax - 350) < 0.01f, "Character top edge clears the fever panel");
        Check(Mathf.Abs(bounds.yMin + 282) < 0.01f, "Character bottom edge clears the ad button");
        Check(bounds.width > 600 && bounds.height > 600, "Reserved HUD still leaves a usable merge area");
        field.topHud = null; field.bottomHud = null;
        Check(field.GetMovementBounds().yMax == 670, "Unconfigured scenes keep their old movement bounds");

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var entries = settings.groups.Where(group => group != null).SelectMany(group => group.entries);
        Check(entries.Single(e => e.address == "8" && e.labels.Contains("SD")).AssetPath == "Assets/Art/Characters/SD/8.png",
            "Level 8 rainbow kimono matches CSV and LD");
        Check(entries.Single(e => e.address == "9" && e.labels.Contains("SD")).AssetPath == "Assets/Art/Characters/SD/9.png",
            "Level 9 black scroll matches CSV and LD");
        foreach (int level in new[] { 8, 9 })
            Check(File.ReadAllBytes($"Assets/Art/Characters/SD/{level}.png").SequenceEqual(File.ReadAllBytes($"Assets/닌자SD2/{level}.png")),
                "SD image content matches reviewed original, not only its address: " + level);

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Ingame.unity") return;
        var hud = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<IncomeActivityHUD>(true)).Single();
        var progress = hud.GetComponentInParent<EvolutionProgressHUD>();
        var button = (RectTransform)hud.boostButton.transform;
        Check(!progress.enabled && !progress.goalLabel.gameObject.activeSelf && !progress.worldLabel.gameObject.activeSelf,
            "Unrequested goal and world strip removed from the mobile HUD");
        Check(hud.feverFill.rectTransform.rect.height >= 42, "Fever bar is thick enough to read at phone scale");
        Check(hud.feverLabel.fontSize >= 48 && (!hud.boostLabel || hud.boostLabel.fontSize >= 36),
            "Primary HUD label remains readable and optional field-boost labels stay legible");
        var scaler = hud.GetComponentInParent<Canvas>().rootCanvas.GetComponent<CanvasScaler>();
        var store = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<GemStorePanelController>(true)).Single();
        var shop = store.transform.parent.gameObject;
        bool shopActive = shop.activeSelf, productsActive = store.gameObject.activeSelf;
        shop.SetActive(true); store.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(store.GetComponent<ScrollRect>().content);
        try
        {
        foreach (var size in new[] { new Vector2(320, 568), new Vector2(360, 800), new Vector2(390, 844), new Vector2(320, 740) })
        {
            float scale = Mathf.Pow(2, Mathf.Lerp(Mathf.Log(size.x / scaler.referenceResolution.x, 2),
                Mathf.Log(size.y / scaler.referenceResolution.y, 2), scaler.matchWidthOrHeight));
            float width = button.rect.width * button.lossyScale.x / scaler.transform.lossyScale.x;
            float height = button.rect.height * button.lossyScale.y / scaler.transform.lossyScale.y;
            Check(width * scale <= size.x - 24, "Ad button fits narrow/tall phone width: " + size);
            Check(height * scale >= 48, "Ad button has at least 48 logical pixels of height: " + size);
            Check(!hud.boostLabel || hud.boostLabel.fontSize * scale >= 14, "Optional reward headline remains readable: " + size);
        }
        }
        finally { store.gameObject.SetActive(productsActive); shop.SetActive(shopActive); }
    }

    static void ShopAndSummonChecks()
    {
        Check(SummonPanelController.MaxSummonableLevel(1) == 1 && SummonPanelController.MaxSummonableLevel(3) == 1,
            "Early summon unlock remains level one");
        Check(SummonPanelController.MaxSummonableLevel(4) == 2 && SummonPanelController.MaxSummonableLevel(25) == 23,
            "Summon unlock keeps the two-level merge lead");
        var cell = Make<SummonCell>("Summon availability");
        var gold = Make<Button>("Gold summon"); var gems = Make<Button>("Gem summon");
        Set(cell, "summonButton", gold); Set(cell, "gemSummonButton", gems);
        cell.Setup(1, "Level 1", 200, null, () => { }, () => { }, 3);
        cell.SetButtonsInteractable(false, false);
        Check(!gold.interactable && !gems.interactable, "Full field disables both summon actions");
        cell.SetButtonsInteractable(true, false);
        Check(gold.interactable && !gems.interactable, "Summon actions respect independent readiness");
        cell.SetButtonsInteractable(true, true);
        Check(gold.interactable && gems.interactable, "Freed field restores summon actions");

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Ingame.unity") return;
        var objects = scene.GetRootGameObjects();
        var store = objects.SelectMany(go => go.GetComponentsInChildren<GemStorePanelController>(true)).Single();
        var hud = objects.SelectMany(go => go.GetComponentsInChildren<IncomeActivityHUD>(true)).Single();
        Check(store.name == "상품", "Cash tab renamed Products");
        Check(!hud.boostButton.transform.IsChildOf(store.transform)
            && hud.boostButton.name == "Field Income Boost",
            "Income boost is a dedicated field action, not a product-shop row");
        Check(hud.boostButton.image.sprite == null && hud.boostButton.image.color.a == 0,
            "Field income boost has no opaque square backing");
        Check(store.entries[0].isAdEntry && store.entries[1].isGoldAdEntry, "Gem and gold ads precede paid products");
        Check(store.entries.Any(e => e.productId == PremiumCurrencyManager.REMOVE_ADS_ID)
            && store.entries.All(e => e.productId != "remove_ad"), "Ad removal product matches the IAP catalog ID");
        var premium = objects.SelectMany(go => go.GetComponentsInChildren<PremiumCurrencyManager>(true)).Single();
        Check(premium.products.Any(p => p.productId == PremiumCurrencyManager.REMOVE_ADS_ID
                && p.type == UnityEngine.Purchasing.ProductType.NonConsumable)
            && premium.products.All(p => p.productId != "remove_ad"),
            "Scene IAP catalog uses the canonical remove-ads non-consumable ID");
        var adActionHeights = store.entries.Where(e => e.isAdEntry || e.isGoldAdEntry)
            .Select(e => $"{e.buyButton.name}:{((RectTransform)e.buyButton.transform).rect.height:0.##}")
            .ToArray();
        Check(store.entries.Where(e => e.isAdEntry || e.isGoldAdEntry)
                .All(e => ((RectTransform)e.buyButton.transform).rect.height >= 200),
            $"Product ad actions have mobile-sized targets [{string.Join(", ", adActionHeights)}]");
        Check(objects.SelectMany(go => go.GetComponentsInChildren<GirlFieldManager>(true)).Single().bottomHud == null,
            "Moved shop button no longer shrinks the playfield");
        var summon = objects.SelectMany(go => go.GetComponentsInChildren<SummonPanelController>(true)).Single();
        Check(summon.availabilityLabel && summon.availabilityLabel.fontSize >= 36, "Summon capacity and next unlock are visible");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SummonCell.prefab");
        Check(prefab.GetComponentsInChildren<Button>(true).All(b => ((RectTransform)b.transform).rect.height >= 200),
            "Both summon currency buttons meet small-phone height");
    }

    static void ReviewedArtChecks()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var entries = settings.groups.Where(g => g != null).SelectMany(g => g.entries).ToArray();
        var loader = Make<GirlSpriteAddressableLoader>("Reviewed art routing");
        var ld = (Dictionary<string, Sprite>)loader.SpriteDictLD;
        foreach (int level in CharacterArtRefresh.ReviewedLevels)
        {
            string key = level.ToString(), path = CharacterArtRefresh.PathFor(level);
            var entry = entries.Single(e => e.address == key && e.labels.Contains("LD"));
            Check(entry.AssetPath == path, "Reviewed LD address connected: " + level);
            Check(CharacterArtRefresh.HasCleanAlpha(path), "Reviewed LD has real alpha and clear borders: " + level);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var sd = AssetDatabase.LoadAssetAtPath<Sprite>(entries.Single(e => e.address == key && e.labels.Contains("SD")).AssetPath);
            ld[key] = sprite; loader.SpriteDict[key] = sd;
            Check(loader.GetSpriteByKey(key, true) == sprite && loader.GetSpriteByKey(key, false) == sd,
                "Detail uses reviewed LD while field retains SD: " + level);
        }
        Check(entries.Single(e => e.address == "25" && e.labels.Contains("LD")).AssetPath == "Assets/닌자2/25.png",
            "Level 25 LD preserved as requested");
    }

    static void RoundedPanelChecks()
    {
        var image = Make<Image>("Rounded panel");
        image.rectTransform.sizeDelta = new Vector2(200, 100);
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/sprite/도감배경.png");
        var original = image.sprite;
        var corners = image.gameObject.AddComponent<RoundedPanelCorners>();
        corners.radius = 20;
        using var mesh = new VertexHelper();
        var vertices = new UIVertex[4];
        Vector2[] positions = { new Vector2(-100, -50), new Vector2(-100, 50), new Vector2(100, 50), new Vector2(100, -50) };
        for (int i = 0; i < 4; i++)
        {
            vertices[i] = UIVertex.simpleVert;
            vertices[i].position = positions[i];
            vertices[i].uv0 = new Vector2((positions[i].x + 100) / 200, (positions[i].y + 50) / 100);
        }
        mesh.AddUIVertexQuad(vertices);
        corners.ModifyMesh(mesh);
        var output = new List<UIVertex>(); mesh.GetUIVertexStream(output);
        Check(output.Count > 6 && output.Count < 600, "Rounded panel produces bounded corner geometry");
        Check(output.All(v => Mathf.Abs(v.position.x) < 99.9f || Mathf.Abs(v.position.y) < 49.9f),
            "Square outer corner vertices removed");
        Check(output.All(v => Mathf.Abs(v.uv0.x - (v.position.x + 100) / 200) < 0.001f
            && Mathf.Abs(v.uv0.y - (v.position.y + 50) / 100) < 0.001f), "Original panel UV mapping preserved");
        Check(image.sprite == original && image.raycastTarget && image.maskable,
            "Rounding preserves original art, input and mask behavior");
        corners.radius = 0;
        mesh.Clear(); mesh.AddUIVertexQuad(vertices); corners.ModifyMesh(mesh);
        Check(mesh.currentVertCount == 4, "Zero-radius option preserves unmodified geometry");
    }

    static void MythicAndActivityChecks()
    {
        var collection = Make<MythicCollectionManager>("Mythic collection");
        collection.forms = new[] {
            new MythicCollectionManager.Form { id = "sakura" },
            new MythicCollectionManager.Form { id = "moon" },
            new MythicCollectionManager.Form { id = "phoenix" } };
        collection.ApplyLoadedData(Fresh());
        Check(collection.CurrentForm.id == "sakura" && collection.CollectedCount == 0, "New collection starts empty at original form");
        Check(collection.RegisterCurrentForm() && !collection.RegisterCurrentForm(), "Final form collected once; stacking does not duplicate it");
        collection.AdvanceAfterPrestige();
        Check(collection.CurrentForm.id == "moon" && !collection.IsDiscovered("moon"), "Prestige reveals the next goal without granting it");
        collection.RegisterCurrentForm(); collection.AdvanceAfterPrestige(); collection.RegisterCurrentForm();
        Check(collection.CollectedCount == 3, "Three runs collect three distinct final forms");
        collection.AdvanceAfterPrestige();
        Check(collection.CurrentForm.id == "sakura", "Form rotation returns safely after the catalog ends");
        var save = Fresh(); collection.CollectSaveData(save);
        collection.ApplyLoadedData(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save)));
        Check(collection.CollectedCount == 3 && collection.CurrentForm.id == "sakura", "Form IDs and discoveries survive save round trip");
        collection.ApplyLoadedData(new SaveData { totalPrestigeCount = 20 });
        Check(collection.CollectedCount == 1 && collection.IsDiscovered("sakura"), "Legacy progress grants only the original earned form");

        var fever = new FeverMeter();
        fever.RecordClick(0);
        Check(fever.Fill > 0 && fever.Charge == 0 && !fever.IsActive, "First tap acknowledges input without granting fever");
        fever.Reset();
        for (int i = 0; i < 80; i++)
        {
            double now = i * 0.05;
            if (i % 5 == 0) fever.RecordClick(now);
            fever.Tick(now, 0.05);
        }
        Check(fever.IsActive && fever.Fill == 1, "Sustained character taps fill and activate fever");
        for (int i = 80; i < 145; i++) fever.Tick(i * 0.05, 0.05);
        Check(!fever.IsActive && fever.Fill == 0, "Fever decays after tapping stops");
        fever.RecordClick(20); fever.RecordClick(20); fever.RecordClick(20); fever.Tick(20, 30);
        Check(!fever.IsActive && fever.Charge <= 0.1, "One burst or resume gap cannot instantly grant fever");
        fever.Reset();
        Check(fever.ClicksPerSecond == 0, "Focus or prestige reset clears click history");

        var activity = Make<IncomeActivityManager>("Income activity");
        var ads = new TestRewardedOffer();
        Check(activity.RequestBoost(ads, false) && activity.RequestPending, "Opted-in ad request starts without granting a reward");
        Check(activity.GetOnlineMultiplier(IncomeActivityManager.UtcNow) == 1, "No bonus before the ad reward callback");
        Check(!activity.RequestBoost(ads, false), "Concurrent ad request rejected");
        ads.Finish();
        Check(!activity.RequestPending && activity.BoostSecondsRemaining == 0, "Skipped or failed ad releases button without reward");
        activity.RequestBoost(ads, false); ads.Reward();
        var boostSave = Fresh(); activity.CollectSaveData(boostSave);
        ads.Reward(); activity.CollectSaveData(save);
        Check(save.incomeBoostUntilUtc == boostSave.incomeBoostUntilUtc, "Duplicate ad callback cannot stack or extend bonus");
        Check(activity.GetOnlineMultiplier(IncomeActivityManager.UtcNow) == 2 && !activity.RequestBoost(ads, false),
            "Earned ad bonus is 2x and cannot be stacked");
        for (int i = 0; i < 80; i++)
        {
            if (i % 5 == 0) activity.Fever.RecordClick(i * 0.05);
            activity.Fever.Tick(i * 0.05, 0.05);
        }
        Check(activity.GetOnlineMultiplier(IncomeActivityManager.UtcNow) == 3, "Ad 2x and fever +50 percent combine to 3x");
        activity.ApplyLoadedData(boostSave);
        Check(!activity.Fever.IsActive && activity.BoostSecondsRemaining > 0, "Only earned timed boost persists, never fever");
        Check(activity.GetOnlineMultiplier(boostSave.incomeBoostUntilUtc + 1) == 1, "Expired ad bonus returns to base income");
        activity.ApplyLoadedData(Fresh());
        Check(activity.RequestBoost(null, true) && activity.BoostSecondsRemaining > 0, "Ad removal receives the same timed benefit without an ad request");
        var economy = Make<EconomyManager>("Unboosted offline estimate");
        economy.SetGoldPerSecEstimate(100, 3);
        Check(economy.GetGoldPerSecEstimate() == 100, "Online bonus display never changes offline reward estimate");
        var incomeLabel=Make<TMPro.TextMeshProUGUI>("Isolated income percentage");
        Set(economy,"goldPerSecText",incomeLabel);
        foreach(double multiplier in new[]{1d,1.5d,2d,1.125d})
        {
            economy.SetGoldPerSecEstimate(100,3,multiplier);
            Check(incomeLabel.text=="+300 G/s (×"+(multiplier*100).ToString("0.##")+"%)","Income percentage is display-only: "+multiplier);
        }
    }

    sealed class TestRewardedOffer : IAdOfferWithCompletion
    {
        public event Action<bool> OnRewardedReadyChanged { add { } remove { } }
        Action reward, finished;
        public bool IsRewardedReady() => true;
        public void LoadRewarded() { }
        public void ShowRewarded(Action onReward) => ShowRewarded(onReward, null);
        public void ShowRewarded(Action onReward, Action onFinished) { reward = onReward; finished = onFinished; }
        public void Reward() => reward?.Invoke();
        public void Finish() => finished?.Invoke();
    }
}
