using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

/// <summary>Restore familiar illustrated controls without resetting gameplay or save data.</summary>
public static class UIReadabilityRefresh
{
    [Serializable] public class RefEntry { public string asset,property,guid;public long objectId,fileId; }
    [Serializable] public class ColourEntry {public string asset;public long objectId;public float r,g,b,a;}
    [Serializable] public class RefFile { public RefEntry[] entries;public ColourEntry[] colours; }
    static RefEntry[] original;
    static ColourEntry[] originalColours;
    static readonly Color Light=new Color(1,.95f,.81f),Dark=new Color(.10f,.055f,.15f),Gold=new Color(1,.76f,.21f);
    static Sprite Old(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/sprite/"+name+".png");
    static bool Experimental(Sprite s)=>s&&AssetDatabase.GetAssetPath(s).StartsWith("Assets/Art/UI/Refresh/");
    static int restored;

    [MenuItem("Tools/Girls Evolution/UI and Collection/Restore Readability")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode.");
        var scene=EditorSceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/Ingame.unity")throw new InvalidOperationException("Open Ingame.");
        ClosePreviews();
        var snapshot=JsonUtility.FromJson<RefFile>(File.ReadAllText("Docs/UIRefresh/original-refs.json"));original=snapshot.entries;originalColours=snapshot.colours;
        var frameImporter=(TextureImporter)AssetImporter.GetAtPath("Assets/Prefabs/sprite/cell_bg_rounded_1024x256.png");
        if(frameImporter.spriteBorder!=new Vector4(48,48,48,48)){frameImporter.spriteBorder=new Vector4(48,48,48,48);frameImporter.SaveAndReimport();}
        var canvas=Object.FindObjectsOfType<Canvas>(true).First(c=>c.isRootCanvas);restored=0;
        Restore(canvas.gameObject,scene.path);
        foreach(string path in new[]{"Assets/Prefabs/EncyclopediaSlot.prefab","Assets/Prefabs/SummonCell.prefab"})
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try {Restore(root,path);var slot=root.GetComponent<EncyclopediaSlot>();if(slot){var bg=Ref<Image>(slot,"backgroundImage");bg.sprite=Old("cell_bg_rounded_1024x256");bg.type=Image.Type.Sliced;bg.color=Color.white;bg.preserveAspect=false;var caption=Ref<TextMeshProUGUI>(slot,"levelText");caption.color=Light;caption.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/army Bold SDF.asset");caption.fontSharedMaterial=caption.font.material;var button=Ref<Button>(slot,"button");button.image.color=Color.clear;button.transition=Selectable.Transition.None;Ref<Image>(slot,"iconImage").preserveAspect=true;}PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        Main(canvas);Catalog(canvas);Shop(canvas);Settings(canvas);
        foreach(var scroll in canvas.GetComponentsInChildren<ScrollRect>(true))
        {if(scroll.GetComponent<Image>())scroll.GetComponent<Image>().color=Color.clear;var mask=scroll.viewport?scroll.viewport.GetComponent<Mask>():null;if(mask)mask.showMaskGraphic=false;}
        Canvas.ForceUpdateCanvases();
        foreach(var g in canvas.GetComponentsInChildren<Graphic>(true)){g.SetAllDirty();if(g is MaskableGraphic m)m.RecalculateClipping();}
        foreach(var t in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))t.ForceMeshUpdate(true,true);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        UIAndCollectionRefresh.DumpUI();
        Directory.CreateDirectory("Docs/UIRefresh");File.WriteAllText("Docs/UIRefresh/readability-applied.txt",$"Restored {restored} original sprite references. Familiar navigation, female ninja summon, readable counters, simple fever. {DateTime.Now:O}");
        Debug.Log($"[UIReadability] {restored} original sprite references restored; gameplay and saves preserved.");
    }

    static void Restore(GameObject root,string asset)
    {
        var rows=original.Where(r=>r.asset==asset).GroupBy(r=>r.objectId).ToDictionary(g=>g.Key,g=>g.ToArray());
        foreach(var c in root.GetComponentsInChildren<Component>(true))
        {
            if(!c)continue;
            long id=(long)GlobalObjectId.GetGlobalObjectIdSlow(c).targetObjectId;
            if(asset.EndsWith(".prefab"))
            {var source=PrefabUtility.GetCorrespondingObjectFromSource(c);if(source)AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string _,out id);}
            var so=new SerializedObject(c);bool changed=false;
            if(rows.TryGetValue(id,out var entries))foreach(var entry in entries)
            {
                var p=so.FindProperty(entry.property);
                if(p==null||p.propertyType!=SerializedPropertyType.ObjectReference||!(p.objectReferenceValue is Sprite current)||!Experimental(current))continue;
                var replacement=AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(entry.guid)).OfType<Sprite>().FirstOrDefault(s=>{AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s,out string _,out long sid);return sid==entry.fileId;});
                if(replacement){p.objectReferenceValue=replacement;changed=true;restored++;}
            }
            if(changed)so.ApplyModifiedPropertiesWithoutUndo();
            if(c is Image im)
            {
                if(Experimental(im.sprite)){im.sprite=Old("cell_bg_rounded_1024x256");im.type=Image.Type.Simple;}
                else if(im.sprite&&AssetDatabase.GetAssetPath(im.sprite).StartsWith("Assets/Prefabs/")&&im.type!=Image.Type.Filled)im.type=Image.Type.Simple;
                im.pixelsPerUnitMultiplier=1;im.preserveAspect=im.GetComponent<Button>()&&im.type!=Image.Type.Sliced;
                var colour=originalColours.FirstOrDefault(x=>x.asset==asset&&x.objectId==id);
                if(colour!=null)im.color=new Color(colour.r,colour.g,colour.b,colour.a);
                // Never clip painted icon silhouettes with a second geometric mask.
                var rounded=im.GetComponent<RoundedPanelCorners>();if(rounded)rounded.enabled=!im.GetComponent<Button>()&&im.rectTransform.rect.width>600;
            }
            if(c is LocalizedTextureLabel label)
            {
                // Remove captions introduced over already-labelled original art.
                if(label.name=="Ninja Tab"||label.name=="Skin Tab"||label.name=="Close Detail"||label.name=="Equip Skin")continue;
                label.enabled=false;if(label.label)label.label.gameObject.SetActive(false);
                var back=label.transform.Find("Caption Back");if(back)back.gameObject.SetActive(false);
            }
        }
    }
    static void Main(Canvas canvas)
    {
        var nav=canvas.transform.Find("GameObject");
        RestoreBottomNavigation(nav);
        var codex=nav.Find("도감") as RectTransform;Place(codex,new Vector2(1,1),new Vector2(164,200),new Vector2(-100,-110));
        var options=nav.Find("옵션") as RectTransform;Place(options,new Vector2(1,1),new Vector2(164,164),new Vector2(-100,-300));
        var tier=canvas.transform.Find("Tier") as RectTransform;Place(tier,new Vector2(1,1),new Vector2(164,164),new Vector2(-100,-492));
        var header=canvas.transform.Find("위쪽상태창");var headerImage=header.Find("Image").GetComponent<Image>();
        Place((RectTransform)header,new Vector2(.5f,1),new Vector2(1080,210),new Vector2(0,-105));Stretch(headerImage.rectTransform);
        headerImage.sprite=Old("topbar_clean");headerImage.type=Image.Type.Simple;headerImage.preserveAspect=false;
        var economy=Object.FindObjectOfType<EconomyManager>(true);
        var counters=new[]{Ref<TextMeshProUGUI>(Object.FindObjectOfType<PrestigeManager>(true),"prestigePointLabel"),Ref<TextMeshProUGUI>(economy,"goldText"),Ref<TextMeshProUGUI>(Object.FindObjectOfType<PremiumCurrencyManager>(true),"gemLabel")};
        for(int n=0;n<counters.Length;n++)
        {
            var t=counters[n];var row=(RectTransform)t.transform.parent;row.SetParent(headerImage.transform,false);
            foreach(var layout in row.GetComponents<LayoutGroup>())layout.enabled=false;
            Place(row,new Vector2(.5f,.5f),new Vector2(260,94),new Vector2(-375+n*280,30));
            var icon=row.GetComponentInChildren<Image>();Place(icon.rectTransform,new Vector2(.5f,.5f),new Vector2(76,76),new Vector2(-90,0));icon.preserveAspect=true;
            Place(t.rectTransform,new Vector2(.5f,.5f),new Vector2(177,85),new Vector2(40,0));t.color=Light;t.fontSize=46;t.enableAutoSizing=true;t.fontSizeMin=38;t.fontSizeMax=46;t.alignment=TextAlignmentOptions.Left;t.overflowMode=TextOverflowModes.Ellipsis;
        }
        var income=Ref<TextMeshProUGUI>(economy,"goldPerSecText");Place(income.rectTransform,new Vector2(.5f,.5f),new Vector2(820,65),new Vector2(-100,-58));income.color=Light;income.fontSize=46;income.fontSizeMin=42;income.fontSizeMax=46;

        RemoveAutomaticCaptions(nav);
        RestoreIncomeIcon(canvas);

        var hud=canvas.GetComponentInChildren<IncomeActivityHUD>(true);var panel=(RectTransform)hud.transform.parent;
        Plate(panel);panel.sizeDelta=new Vector2(-240,174);panel.anchoredPosition=new Vector2(-70,-315);
        var pop=panel.Find("Population Left") as RectTransform;Plate(pop);Place(pop,new Vector2(0,.5f),new Vector2(205,150),new Vector2(110,0));
        var popIcon=pop.Find("Population Icon").GetComponent<Image>();popIcon.sprite=Old("인구수");Place(popIcon.rectTransform,new Vector2(.5f,.5f),new Vector2(66,66),new Vector2(-53,0));
        var field=Object.FindObjectOfType<GirlFieldManager>(true);var population=Ref<TextMeshProUGUI>(field,"populationText");
        Place(population.rectTransform,new Vector2(.5f,.5f),new Vector2(116,90),new Vector2(34,0));population.fontSize=44;population.color=Light;population.overflowMode=TextOverflowModes.Overflow;population.gameObject.SetActive(true);
        var content=(RectTransform)hud.transform;Stretch(content);content.offsetMin=new Vector2(228,12);content.offsetMax=new Vector2(-20,-12);
        Place(hud.feverLabel.rectTransform,new Vector2(.5f,1),new Vector2(550,65),new Vector2(0,-36));hud.feverLabel.alignment=TextAlignmentOptions.Left;hud.feverLabel.color=Gold;hud.feverLabel.fontSize=50;
        var track=(RectTransform)hud.feverFill.transform.parent;Stretch(track);track.anchorMax=new Vector2(1,0);track.sizeDelta=new Vector2(0,55);track.anchoredPosition=new Vector2(0,34);Plate(track,new Color(.35f,.25f,.42f));
        Stretch(hud.feverFill.rectTransform);hud.feverFill.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");hud.feverFill.type=Image.Type.Filled;hud.feverFill.color=Gold;hud.feverFill.fillAmount=0;
        var round=hud.feverFill.GetComponent<RoundedPanelCorners>();if(round)round.enabled=false;
        if(hud.feverHint)hud.feverHint.gameObject.SetActive(false);
        // Set every design-time renderer dirty after moving from its old layout group.
        population.fontSharedMaterial=population.font.material;population.ForceMeshUpdate(true,true);
    }
    [MenuItem("Tools/Girls Evolution/UI and Collection/Restore Original Bottom Navigation %&u")]
    public static void FixBottomNavigation()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode.");
        var scene=EditorSceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/Ingame.unity")throw new InvalidOperationException("Open Ingame.");
        var canvas=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Canvas>(true)).First(c=>c.isRootCanvas);
        var nav=canvas.transform.Find("GameObject");
        Undo.RegisterFullObjectHierarchyUndo(nav.gameObject,"Restore initial bottom navigation geometry");
        RestoreBottomNavigation(nav);
        RemoveAutomaticCaptions(nav);
        Canvas.ForceUpdateCanvases();
        foreach(var graphic in nav.GetComponentsInChildren<Graphic>(true))graphic.SetAllDirty();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        UIAndCollectionRefresh.DumpUI();
        Debug.Log("[UIReadability] Original bottom pivots, positions, scales and scroll fill geometry restored. Other UI and gameplay unchanged.");
    }
    static void RestoreBottomNavigation(Transform nav)
    {
        // Values from the initial Ingame scene. Sprite padding and original pivots are intentional.
        OriginalRect(nav.Find("소환"),new Vector2(168,230),new Vector2(-410,68.00006f),Vector3.one);
        OriginalRect(nav.Find("상점"),new Vector2(176,234),new Vector2(410,68.00006f),Vector3.one);
        OriginalRect(nav.Find("소환버튼"),new Vector2(500,500),new Vector2(0,68.00006f),new Vector3(.5f,.5f,1));
        OriginalRect(nav.Find("fill"),new Vector2(500,500),new Vector2(0,68.00006f),new Vector3(.5f,.5f,1));
        OriginalRect(nav.Find("자동소환"),new Vector2(1024,1024),new Vector2(-208,68.00006f),new Vector3(.2f,.2f,1));
        OriginalRect(nav.Find("자동합성"),new Vector2(1024,1024),new Vector2(208,82.00006f),new Vector3(.185f,.185f,1));
        OriginalRect(nav.Find("자동소환/자동소환 (1)"),new Vector2(1024,1024),new Vector2(.000030517578f,.00012207031f),Vector3.one);
        OriginalRect(nav.Find("자동합성/자동합성 (1)"),new Vector2(1024,1024),new Vector2(.00010681152f,.000091552734f),Vector3.one);
        foreach(string name in new[]{"소환","상점","소환버튼","fill","자동소환","자동합성"})
        {
            var root=nav.Find(name);
            foreach(var image in root.GetComponentsInChildren<Image>(true))image.preserveAspect=false;
            foreach(var rounded in root.GetComponentsInChildren<RoundedPanelCorners>(true))rounded.enabled=false;
        }
        var background=nav.Find("소환버튼").GetComponent<Image>();
        var fill=nav.Find("fill").GetComponent<Image>();
        background.sprite=fill.sprite=Old("소환버튼");
        background.type=Image.Type.Simple;background.color=new Color(.2924528f,.2924528f,.2924528f,1);
        fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Vertical;fill.fillOrigin=0;fill.color=Color.white;
    }
    static void OriginalRect(Transform target,Vector2 size,Vector2 position,Vector3 scale)
    {
        var rect=(RectTransform)target;
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,0);
        rect.localScale=scale;rect.sizeDelta=size;rect.anchoredPosition=position;
    }
    [MenuItem("Tools/Girls Evolution/UI and Collection/Fix Main Icons")]
    public static void FixMainIcons()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode.");
        var scene=EditorSceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/Ingame.unity")throw new InvalidOperationException("Open Ingame.");
        var canvas=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Canvas>(true)).First(c=>c.isRootCanvas);
        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject,"Restore original automatic controls and income icon");
        RemoveAutomaticCaptions(canvas.transform.Find("GameObject"));
        RestoreIncomeIcon(canvas);
        Canvas.ForceUpdateCanvases();
        foreach(var g in canvas.GetComponentsInChildren<Graphic>(true))g.SetAllDirty();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        UIAndCollectionRefresh.DumpUI();
        Debug.Log("[UIReadability] Extra automatic-control captions removed; original income coin icon restored. Other panels unchanged.");
    }
    static void RemoveAutomaticCaptions(Transform nav)
    {
        foreach(string name in new[]{"자동소환","자동합성"})
        {
            var root=nav.Find(name);
            foreach(var caption in root.GetComponentsInChildren<LocalizedTextureLabel>(true))
            {
                if(caption.label&&caption.label.name=="Texture Label"&&caption.label.transform.IsChildOf(root))
                    Undo.DestroyObjectImmediate(caption.label.gameObject);
                Undo.DestroyObjectImmediate(caption);
            }
            // These objects were added by the UI experiment, not part of the original button art.
            foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Caption Back"||t.name=="Readable Caption").ToArray())
                if(t)Undo.DestroyObjectImmediate(t.gameObject);
        }
    }
    [MenuItem("Tools/Girls Evolution/Fix Centered Income")]
    public static void FixCenteredIncome()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode.");
        var scene=EditorSceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/Ingame.unity")throw new InvalidOperationException("Open Ingame.");
        var canvas=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Canvas>(true)).First(c=>c.isRootCanvas);
        Undo.RegisterFullObjectHierarchyUndo(canvas.transform.Find("위쪽상태창").gameObject,"Center income with percentage multiplier");
        RestoreIncomeIcon(canvas);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        UIAndCollectionRefresh.DumpUI();
        Debug.Log("[UIReadability] Income and coin centered; multiplier shown as percentage. Other UI unchanged.");
    }
    static void RestoreIncomeIcon(Canvas canvas)
    {
        var header=canvas.transform.Find("위쪽상태창/Image");
        var row=(RectTransform)header.Find("GameObject (3)");
        var icon=row.Find("Image (2)").GetComponent<Image>();
        var economy=Object.FindObjectOfType<EconomyManager>(true);
        var income=Ref<TextMeshProUGUI>(economy,"goldPerSecText");
        row.gameObject.SetActive(true);
        foreach(var layout in row.GetComponents<LayoutGroup>())layout.enabled=false;
        foreach(var fitter in row.GetComponents<ContentSizeFitter>())fitter.enabled=false;
        Place(row,new Vector2(.5f,.5f),new Vector2(700,76),new Vector2(0,-58));
        icon.gameObject.SetActive(true);icon.enabled=true;icon.sprite=Old("골포");icon.color=Color.white;icon.preserveAspect=true;icon.raycastTarget=false;
        Place(icon.rectTransform,new Vector2(.5f,.5f),new Vector2(76,76),Vector2.zero);
        icon.transform.SetAsFirstSibling();
        var iconSize=icon.GetComponent<LayoutElement>()??icon.gameObject.AddComponent<LayoutElement>();
        iconSize.minWidth=iconSize.preferredWidth=76;iconSize.minHeight=iconSize.preferredHeight=76;iconSize.flexibleWidth=0;
        income.transform.SetParent(row,false);
        Place(income.rectTransform,new Vector2(.5f,.5f),new Vector2(616,65),Vector2.zero);
        income.alignment=TextAlignmentOptions.Left;
        income.enableWordWrapping=false;income.enableAutoSizing=true;income.fontSizeMin=42;income.fontSizeMax=46;
        income.overflowMode=TextOverflowModes.Ellipsis;
        // Convert only the editor placeholder; runtime income and save data are untouched.
        income.text=System.Text.RegularExpressions.Regex.Replace(income.text,@"\(×([0-9]+(?:\.[0-9]+)?)\)",match=>
            "(×"+(double.Parse(match.Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture)*100).ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+"%)");
        var textSize=income.GetComponent<LayoutElement>()??income.gameObject.AddComponent<LayoutElement>();
        textSize.minWidth=0;textSize.preferredWidth=-1;textSize.flexibleWidth=0;
        var centered=row.GetComponent<HorizontalLayoutGroup>()??row.gameObject.AddComponent<HorizontalLayoutGroup>();
        centered.enabled=true;centered.padding=new RectOffset();centered.spacing=8;centered.childAlignment=TextAnchor.MiddleCenter;
        centered.childControlWidth=true;centered.childControlHeight=false;
        centered.childForceExpandWidth=false;centered.childForceExpandHeight=false;
        centered.childScaleWidth=false;centered.childScaleHeight=false;
        income.ForceMeshUpdate(true,true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(row);Canvas.ForceUpdateCanvases();
    }
    static void Catalog(Canvas canvas)
    {
        var catalog=canvas.GetComponentInChildren<EncyclopediaPanelController>(true);
        catalog.GetComponent<Image>().sprite=Old("cell_bg_rounded_1024x256");catalog.GetComponent<Image>().type=Image.Type.Sliced;
        catalog.selectedTabSprite=catalog.normalTabSprite=Old("cell_bg_rounded_1024x256");
        foreach(var b in new[]{catalog.ninjaTab,catalog.skinTab}){b.image.sprite=catalog.normalTabSprite;b.image.type=Image.Type.Sliced;b.image.preserveAspect=false;var t=b.GetComponentInChildren<TextMeshProUGUI>(true);t.color=Light;t.fontSize=48;t.enableAutoSizing=false;}
        catalog.RefreshTabs(false);
        ResetCatalogCount(catalog);
        var detail=Ref<EncyclopediaDetailPanel>(catalog,"detailPanel");var root=Ref<GameObject>(detail,"panelRoot");root.GetComponent<Image>().sprite=Old("cell_bg_rounded_1024x256");root.GetComponent<Image>().type=Image.Type.Sliced;
        var equipCaption=detail.equipButton.GetComponent<LocalizedTextureLabel>();if(equipCaption)equipCaption.enabled=false;
        foreach(var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))t.color=Light;
        catalog.gameObject.SetActive(false);
    }
    static void Shop(Canvas canvas)
    {
        var shop=canvas.transform.Find("Shop");
        var hud=canvas.GetComponentInChildren<IncomeActivityHUD>(true);hud.boostButton.image.sprite=Old("도감배경");hud.boostButton.image.type=Image.Type.Simple;hud.boostButton.image.preserveAspect=false;hud.boostLabel.color=Light;
        var store=canvas.GetComponentInChildren<GemStorePanelController>(true);
        // The original painted sign has wide transparent margins; size its art, not its empty pixels.
        Place((RectTransform)shop,new Vector2(.5f,.5f),new Vector2(1420,2220),Vector2.zero);shop.GetComponent<Image>().preserveAspect=false;
        string[] tabs={"골드","환생","보석","상품"};
        var tabFrame=ShopPresentationRefresh.GetShopButtonFrame();
        for(int n=0;n<tabs.Length;n++)
        {
            var b=shop.GetComponentsInChildren<Button>(true).First(x=>x.transform.parent==shop&&x.name==tabs[n]);
            var localized=b.GetComponent<LocalizedTextureLabel>();if(localized)localized.enabled=false;
            b.image.sprite=tabFrame;b.image.type=Image.Type.Simple;b.image.preserveAspect=false;b.image.color=Color.white;
            Place((RectTransform)b.transform,new Vector2(.5f,.5f),new Vector2(220,136),new Vector2(-300+200*n,608));
            var label=b.GetComponentInChildren<TextMeshProUGUI>(true);
            if(!label){label=new GameObject("Label",typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();label.transform.SetParent(b.transform,false);label.font=canvas.GetComponentInChildren<TextMeshProUGUI>(true).font;}
            if(label){label.gameObject.SetActive(true);label.enabled=true;Stretch(label.rectTransform);label.rectTransform.offsetMin=new Vector2(20,12);label.rectTransform.offsetMax=new Vector2(-20,-12);label.transform.SetAsLastSibling();label.text=tabs[n];label.color=Light;label.fontSize=44;label.enableAutoSizing=true;label.fontSizeMin=38;label.fontSizeMax=44;label.alignment=TextAlignmentOptions.Center;}
        }
        foreach(var scroll in shop.GetComponentsInChildren<ScrollRect>(true).Where(s=>s.transform.parent==shop))
        {
            Place((RectTransform)scroll.transform,new Vector2(.5f,.5f),new Vector2(820,1240),new Vector2(0,-100));
            if(scroll.GetComponent<Image>())scroll.GetComponent<Image>().color=Color.clear;
            Stretch(scroll.viewport);var mask=scroll.viewport.GetComponent<Mask>();if(mask)mask.showMaskGraphic=false;
        }
        var close=shop.Find("Close") as RectTransform;if(close)Place(close,new Vector2(.5f,.5f),new Vector2(112,112),new Vector2(300,750));
        foreach(var e in store.entries){e.buyButton.image.sprite=tabFrame;e.buyButton.image.type=Image.Type.Simple;e.buyButton.image.preserveAspect=false;e.buyButton.image.color=Color.white;e.priceText.color=Light;e.titleText.color=Light;e.priceText.fontSize=44;e.titleText.fontSize=46;}
        foreach(var bg in store.GetComponentsInChildren<Image>(true).Where(i=>i.name.StartsWith("cellbg")))bg.preserveAspect=false;
    }
    static void Settings(Canvas canvas)
    {
        var panel=canvas.transform.Find("설정패널/Panel");
        Place((RectTransform)panel,new Vector2(.5f,.5f),new Vector2(1080,1780),Vector2.zero);
        var title=panel.Find("title").GetComponent<TextMeshProUGUI>();
        Place(title.rectTransform,new Vector2(.5f,.5f),new Vector2(680,90),new Vector2(0,620));title.color=Light;title.fontSize=60;
        string[] names={"lang","vol","reset","cop","mail","discord","save","playstore"};
        for(int n=0;n<names.Length;n++)
        {
            var r=(RectTransform)panel.Find(names[n]);Place(r,new Vector2(.5f,.5f),new Vector2(390,210),new Vector2(n%2==0?-215:215,360-(n/2)*250));
            var caption=r.GetComponent<LocalizedTextureLabel>();if(!caption)continue;
            caption.enabled=true;caption.label.gameObject.SetActive(true);caption.label.color=Light;caption.label.fontSize=44;caption.label.enableAutoSizing=false;
            Place(caption.label.rectTransform,new Vector2(.5f,0),new Vector2(400,60),new Vector2(0,-10));caption.Refresh();
        }
        var close=panel.Find("Close") as RectTransform;if(close)Place(close,new Vector2(.5f,.5f),new Vector2(164,164),new Vector2(410,650));
    }
    [MenuItem("Tools/Girls Evolution/UI and Collection/Preview Products %&j")]
    public static void PreviewProducts(){ClosePreviews();ShopPresentationRefresh.PreviewProducts();}
    [MenuItem("Tools/Girls Evolution/UI and Collection/Preview Codex %&k")]
    public static void PreviewCodex()
    {
        ClosePreviews();var catalog=Object.FindObjectOfType<EncyclopediaPanelController>(true);catalog.gameObject.SetActive(true);catalog.RefreshTabs(true);
        var container=Ref<Transform>(catalog,"slotContainer");var prefab=Ref<GameObject>(catalog,"slotPrefab");
        var collection=Object.FindObjectOfType<CodexCollectionManager>(true);var skins=collection.skins;int index=0;
        foreach(var skin in skins)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,container);go.name="Readability Preview";go.hideFlags=HideFlags.DontSave;
            bool unlocked=index++%3!=2;
            var slot=go.GetComponent<EncyclopediaSlot>();slot.Setup(skin.level,skin.DisplayName,skin.ld,unlocked,()=>{});
            slot.SetCaption(EncyclopediaPanelController.SkinCaption(collection,skin,unlocked));
        }
        catalog.collectionCount.text="스킨 미리보기 · 실제 저장 데이터는 변경하지 않음";Canvas.ForceUpdateCanvases();
    }
    [MenuItem("Tools/Girls Evolution/UI and Collection/Preview Settings %&o")]
    public static void PreviewSettings(){ClosePreviews();var settings=Object.FindObjectsOfType<Canvas>(true).First(c=>c.isRootCanvas).transform.Find("설정패널");settings.gameObject.SetActive(true);settings.Find("Panel").gameObject.SetActive(true);Canvas.ForceUpdateCanvases();}
    [MenuItem("Tools/Girls Evolution/UI and Collection/Close Previews %&l")]
    public static void ClosePreviews()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        RewardSkinPresentationRepair.CloseRewardPreview();
        ShopPresentationRefresh.ClosePreviews();
        var catalog=Object.FindObjectOfType<EncyclopediaPanelController>(true);var content=Ref<Transform>(catalog,"slotContainer");
        var detail=Ref<EncyclopediaDetailPanel>(catalog,"detailPanel");
        detail.Hide();
        foreach(var key in new[]{"nameText","levelText","incomeText"})Ref<TextMeshProUGUI>(detail,key).text="";
        detail.collectionText.text="";
        foreach(Transform t in content.Cast<Transform>().ToArray())if(t.name=="Readability Preview"&&t.gameObject.hideFlags==HideFlags.DontSave)Object.DestroyImmediate(t.gameObject);
        catalog.gameObject.SetActive(false);
        catalog.RefreshTabs(false);
        ResetCatalogCount(catalog);
        var settings=Object.FindObjectsOfType<Canvas>(true).First(c=>c.isRootCanvas).transform.Find("설정패널");settings.Find("Panel").gameObject.SetActive(false);settings.gameObject.SetActive(true);
    }
    static void ResetCatalogCount(EncyclopediaPanelController catalog)
    {catalog.collectionCount.text=$"닌자 0/25  ·  스킨 0/{Object.FindObjectOfType<CodexCollectionManager>(true).skins.Length}  ·  수집 즉시 영구 적용";}
    static void Plate(RectTransform r,Color? color=null)
    {var i=r.GetComponent<Image>()??r.gameObject.AddComponent<Image>();i.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");i.type=Image.Type.Sliced;i.pixelsPerUnitMultiplier=.25f;i.color=color??Dark;i.preserveAspect=false;i.raycastTarget=false;var rounded=i.GetComponent<RoundedPanelCorners>();if(rounded)rounded.enabled=false;}
    static T Ref<T>(Object o,string name)where T:Object=>(T)new SerializedObject(o).FindProperty(name).objectReferenceValue;
    static void Place(RectTransform r,Vector2 anchor,Vector2 size,Vector2 pos){r.anchorMin=r.anchorMax=anchor;r.pivot=new Vector2(.5f,.5f);r.localScale=Vector3.one;r.sizeDelta=size;r.anchoredPosition=pos;}
    static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,.5f);r.localScale=Vector3.one;r.offsetMin=r.offsetMax=Vector2.zero;}
}
