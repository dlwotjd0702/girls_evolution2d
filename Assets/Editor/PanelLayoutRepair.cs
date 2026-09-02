using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

/// <summary>Repair panel geometry only, preserving original artwork, serialized references and purchase callbacks.</summary>
public static class PanelLayoutRepair
{
    static readonly Color Ink=new Color(1,.95f,.81f);
    static TMP_FontAsset Font=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/army Bold SDF.asset");
    static Sprite Art(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/sprite/"+name+".png");
    public static T Ref<T>(Object o,string key) where T:Object=>(T)new SerializedObject(o).FindProperty(key).objectReferenceValue;
    static Canvas CanvasRoot()=>Object.FindObjectsOfType<Canvas>(true).First(c=>c.isRootCanvas);
    static AutoAutomationController[] ShopAutomations(Transform root)=>Object.FindObjectsOfType<AutoAutomationController>(true).Where(c=>c.buyOrUpgradeButton&&c.buyOrUpgradeButton.transform.IsChildOf(root)).ToArray();

    [MenuItem("Tools/Girls Evolution/Repair Panel Layouts %&i")]
    public static void Apply()
    {
        try { ApplyCore(); }
        catch(Exception error){System.IO.Directory.CreateDirectory("Logs");System.IO.File.WriteAllText("Logs/panel-repair-error.txt",error.ToString());throw;}
    }
    static void ApplyCore()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
        var scene=EditorSceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/Ingame.unity")throw new InvalidOperationException("Open Ingame first.");
        UIReadabilityRefresh.ClosePreviews();
        var canvas=CanvasRoot();
        System.IO.Directory.CreateDirectory("Logs");System.IO.File.WriteAllText("Logs/panel-repair-roots.txt",canvas.name+"\n"+string.Join("\n",canvas.transform.Cast<Transform>().Select(t=>t.name)));
        var summon=canvas.GetComponentInChildren<SummonPanelController>(true);
        var summonRoot=Ref<RectTransform>(summon,"content").GetComponentInParent<ScrollRect>(true).transform.parent;
        foreach(var root in new[]{canvas.transform.Find("Shop"),summonRoot,canvas.transform.Find("도감")})
        {
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject,"Repair panel layout");
            foreach(var mask in root.GetComponentsInChildren<RoundedPanelCorners>(true))mask.enabled=false;
        }
        RepairShop(canvas);RepairSummon(canvas);RepairCodex(canvas);
        Canvas.ForceUpdateCanvases();
        foreach(var graphic in canvas.GetComponentsInChildren<Graphic>(true))graphic.SetAllDirty();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        UIAndCollectionRefresh.DumpUI();
        Debug.Log("[PanelRepair] Shop, summon and codex repaired; navigation and saves unchanged.");
    }
    static void RepairShop(Canvas canvas)
    {
        var root=canvas.transform.Find("Shop");
        // Restore controller state sprites as well as the currently visible Image.
        // Otherwise RefreshAll replaces the correct button with an abandoned atlas tile.
        var states=AssetDatabase.LoadAllAssetsAtPath("Assets/Prefabs/sprite/강화구매맥스.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        foreach(var controller in root.GetComponentsInChildren<ShopPanelController>(true))foreach(var entry in controller.entries){entry.upgradeIconSprite=states[1];entry.maxIconSprite=states[2];}
        foreach(var controller in root.GetComponentsInChildren<PrestigeShopPanelController>(true))foreach(var entry in controller.entries){entry.upgradeIconSprite=states[1];entry.maxIconSprite=states[2];}
        foreach(var controller in ShopAutomations(root)){controller.buyIconSprite=states[0];controller.upgradeIconSprite=states[1];controller.maxIconSprite=states[2];}
        // The original painted panel includes transparent margins; preserve its art sizing.
        Put((RectTransform)root,root.parent,1420,2220,0,0);
        root.GetComponent<Image>().preserveAspect=false;
        foreach(var scroll in root.GetComponentsInChildren<ScrollRect>(true).Where(s=>s.transform.parent==root))
        {
            Put((RectTransform)scroll.transform,root,840,1240,0,-100);Scroll(scroll);
            List(scroll.content,18);
            foreach(Transform row in scroll.content)
            {
                var name=row.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t=>t.name=="NameLabel");
                var cost=row.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t=>t.name=="CostRow");
                var level=row.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t=>t.name=="Level");
                var buy=row.GetComponentsInChildren<Button>(true).FirstOrDefault(b=>b.name=="SummonButton");
                if(!name||!cost||!buy)continue;
                Row((RectTransform)row,280);
                foreach(var layout in row.GetComponentsInChildren<LayoutGroup>(true))layout.enabled=false;
                foreach(var fit in row.GetComponentsInChildren<ContentSizeFitter>(true))fit.enabled=false;
                foreach(var bg in row.GetComponentsInChildren<Image>(true).Where(i=>i.name.StartsWith("cellbg"))) {Stretch(bg.rectTransform,row);Surface(bg);bg.transform.SetAsFirstSibling();}
                var icon=row.Find("Image")?.GetComponent<Image>();
                if(icon){Put(icon.rectTransform,row,148,148,-325,34);icon.preserveAspect=true;icon.raycastTarget=false;}
                if(level){Put(level.rectTransform,row,190,60,-325,-86);Text(level,34);}
                Put(name.rectTransform,row,320,100,-65,65);Text(name,42,TextAlignmentOptions.MidlineLeft);
                Put(cost.rectTransform,row,340,132,-55,-56);Text(cost,40,TextAlignmentOptions.MidlineLeft);
                Put((RectTransform)buy.transform,row,250,180,272,0);buy.image.preserveAspect=true;
                // Korean keeps the original artwork; English receives a small matching overlay.
            }
        }
        var store=root.GetComponentInChildren<GemStorePanelController>(true);
        foreach(var entry in store.entries)
        {
            var row=(RectTransform)entry.buyButton.transform.parent;Row(row,244);
            foreach(var bg in row.GetComponentsInChildren<Image>(true).Where(i=>i.name.StartsWith("cellbg"))){Stretch(bg.rectTransform,row);Surface(bg);}
            Put(entry.titleText.rectTransform,row,390,184,-195,0);Text(entry.titleText,44,TextAlignmentOptions.MidlineLeft);
            Put((RectTransform)entry.buyButton.transform,row,360,200,205,0);ButtonFrame(entry.buyButton);
            Stretch(entry.priceText.rectTransform,entry.buyButton.transform,24,20);Text(entry.priceText,40);
        }
        var hud=canvas.GetComponentInChildren<IncomeActivityHUD>(true);ButtonFrame(hud.boostButton);
        Stretch(hud.boostLabel.rectTransform,hud.boostButton.transform,24,20);Text(hud.boostLabel,48);hud.boostLabel.fontSizeMin=48;
        foreach(var controller in root.GetComponentsInChildren<ShopPanelController>(true))controller.RefreshAll();
        foreach(var controller in root.GetComponentsInChildren<PrestigeShopPanelController>(true))controller.RefreshAll();
        foreach(var controller in ShopAutomations(root))controller.Refresh();
        GlobalPresentationPolish.ConfigureShopLabels(root,Font);
    }
    static void RepairSummon(Canvas canvas)
    {
        var controller=canvas.GetComponentInChildren<SummonPanelController>(true);
        var content=Ref<RectTransform>(controller,"content");var scroll=content.GetComponentInParent<ScrollRect>(true);var root=scroll.transform.parent;
        Put((RectTransform)root,root.parent,1250,2050,0,0);root.GetComponent<Image>().preserveAspect=false;
        Put((RectTransform)scroll.transform,root,840,1120,0,-225);Scroll(scroll);List(content,24);
        Put(controller.availabilityLabel.rectTransform,root,830,120,0,425);Text(controller.availabilityLabel,40);
        var close=root.Find("Close") as RectTransform;if(close)Put(close,root,164,164,425,770);
        var reason=Ref<TextMeshProUGUI>(controller,"reasonLabel");if(reason){Put(reason.rectTransform,root,820,120,0,-835);Text(reason,40);}
        EditPrefab("Assets/Prefabs/SummonCell.prefab",go=>
        {
            var cell=go.GetComponent<SummonCell>();var row=(RectTransform)go.transform;
            foreach(var layout in go.GetComponentsInChildren<LayoutGroup>(true))layout.enabled=false;
            foreach(var fit in go.GetComponentsInChildren<ContentSizeFitter>(true))fit.enabled=false;
            Row(row,430);
            var bg=go.GetComponentsInChildren<Image>(true).First(i=>i.name=="Image (1)");Stretch(bg.rectTransform,row);Surface(bg);bg.transform.SetAsFirstSibling();
            var icon=Ref<Image>(cell,"iconImage");Put(icon.rectTransform,row,190,190,-285,100);icon.preserveAspect=true;icon.raycastTarget=false;
            var name=Ref<TMP_Text>(cell,"nameLabel");Put(name.rectTransform,row,480,180,80,100);Text(name,46,TextAlignmentOptions.MidlineLeft);
            foreach(bool gem in new[]{false,true})
            {
                var button=Ref<Button>(cell,gem?"gemSummonButton":"summonButton");Put((RectTransform)button.transform,row,370,200,gem?202:-202,-107);ButtonFrame(button);
                var label=Ref<TMP_Text>(cell,gem?"gemCostLabel":"costLabel");Stretch(label.rectTransform,button.transform,18,18);Text(label,44);
            }
        });
    }
    static void RepairCodex(Canvas canvas)
    {
        var catalog=canvas.GetComponentInChildren<EncyclopediaPanelController>(true);var root=(RectTransform)catalog.transform;
        Put(root,root.parent,1020,1760,0,0);Surface(root.GetComponent<Image>());
        var title=root.Find("Text (TMP)").GetComponent<TMP_Text>();Put(title.rectTransform,root,700,100,-45,765);Text(title,60);
        Put((RectTransform)root.Find("Close"),root,164,164,410,765);
        Put((RectTransform)catalog.ninjaTab.transform,root,430,156,-230,600);Put((RectTransform)catalog.skinTab.transform,root,430,156,230,600);
        foreach(var tab in new[]{catalog.ninjaTab,catalog.skinTab}){Surface(tab.image);Text(tab.GetComponentInChildren<TMP_Text>(true),48);}
        Put(catalog.collectionSummary.rectTransform,root,910,125,0,432);Text(catalog.collectionSummary,38);
        Put(catalog.collectionCount.rectTransform,root,910,80,0,325);Text(catalog.collectionCount,34);
        var content=(RectTransform)Ref<Transform>(catalog,"slotContainer");var scroll=content.GetComponentInParent<ScrollRect>(true);
        Put((RectTransform)scroll.transform,root,940,1080,0,-280);Scroll(scroll);
        foreach(var l in content.GetComponents<LayoutGroup>())l.enabled=false;
        var grid=content.GetComponent<GridLayoutGroup>()??content.gameObject.AddComponent<GridLayoutGroup>();grid.enabled=true;
        grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=2;grid.cellSize=new Vector2(440,560);grid.spacing=new Vector2(20,24);grid.padding=new RectOffset(20,20,12,20);grid.childAlignment=TextAnchor.UpperCenter;
        Content(content);
        EditPrefab("Assets/Prefabs/EncyclopediaSlot.prefab",go=>
        {
            var slot=go.GetComponent<EncyclopediaSlot>();var row=(RectTransform)go.transform;row.sizeDelta=new Vector2(440,560);
            var bg=Ref<Image>(slot,"backgroundImage");Stretch(bg.rectTransform,row);Surface(bg);
            var icon=Ref<Image>(slot,"iconImage");Put(icon.rectTransform,row,380,330,0,85);icon.preserveAspect=true;icon.raycastTarget=false;
            var caption=Ref<TMP_Text>(slot,"levelText");Put(caption.rectTransform,row,406,180,0,-177);Text(caption,40);
            RewardSkinPresentationRepair.ConfigureLockBadge(slot);
            var button=Ref<Button>(slot,"button");Stretch((RectTransform)button.transform,row);button.image.color=Color.clear;button.transition=Selectable.Transition.None;
        });
        var detail=Ref<EncyclopediaDetailPanel>(catalog,"detailPanel");var panel=(RectTransform)Ref<GameObject>(detail,"panelRoot").transform;
        Put(panel,root,980,1760,0,0);Surface(panel.GetComponent<Image>());panel.GetComponent<Image>().raycastTarget=true;
        var illustration=Ref<Image>(detail,"ldIllustrationImage");Put(illustration.rectTransform,panel,870,970,0,190);illustration.preserveAspect=true;illustration.raycastTarget=false;
        var name=Ref<TextMeshProUGUI>(detail,"nameText");Put(name.rectTransform,panel,700,100,-55,765);Text(name,56);
        var level=Ref<TextMeshProUGUI>(detail,"levelText");Put(level.rectTransform,panel,350,75,-235,-352);Text(level,42);
        var income=Ref<TextMeshProUGUI>(detail,"incomeText");Put(income.rectTransform,panel,480,75,180,-352);Text(income,42);
        Put(detail.collectionText.rectTransform,panel,880,190,0,-500);Text(detail.collectionText,40);
        Put((RectTransform)detail.equipButton.transform,panel,820,164,0,-725);ButtonFrame(detail.equipButton);Stretch(detail.equipLabel.rectTransform,detail.equipButton.transform,24,16);Text(detail.equipLabel,46);
        var closeButton=Ref<Button>(detail,"closeButton");Put((RectTransform)closeButton.transform,panel,164,164,400,765);closeButton.image.sprite=Art("닫기");closeButton.image.type=Image.Type.Simple;closeButton.image.preserveAspect=true;closeButton.image.color=Color.white;
        foreach(var label in closeButton.GetComponentsInChildren<TMP_Text>(true))label.gameObject.SetActive(false);
        var legacyClose=panel.Find("Close (1)");if(legacyClose)legacyClose.gameObject.SetActive(false);
        foreach(var old in panel.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.transform.parent==panel&&t!=name&&t!=level&&t!=income&&t!=detail.collectionText))old.gameObject.SetActive(false);
        panel.SetAsLastSibling();detail.Hide();catalog.gameObject.SetActive(false);
    }
    static void EditPrefab(string path,Action<GameObject> edit)
    {
        var go=PrefabUtility.LoadPrefabContents(path);
        try {foreach(var rounded in go.GetComponentsInChildren<RoundedPanelCorners>(true))rounded.enabled=false;edit(go);PrefabUtility.SaveAsPrefabAsset(go,path);}
        finally{PrefabUtility.UnloadPrefabContents(go);}
    }
    static void Scroll(ScrollRect scroll)
    {
        scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
        scroll.horizontalScrollbar=null;scroll.verticalScrollbar=null;
        foreach(var bar in scroll.GetComponentsInChildren<Scrollbar>(true))bar.gameObject.SetActive(false);
        if(scroll.GetComponent<Image>())scroll.GetComponent<Image>().color=Color.clear;
        Stretch(scroll.viewport,scroll.transform);
        var mask=scroll.viewport.GetComponent<Mask>();if(mask)mask.enabled=false;
        var rectMask=scroll.viewport.GetComponent<RectMask2D>()??scroll.viewport.gameObject.AddComponent<RectMask2D>();rectMask.enabled=true;
        var image=scroll.viewport.GetComponent<Image>();if(image){image.color=Color.clear;image.raycastTarget=true;}
        scroll.verticalNormalizedPosition=1;
    }
    static void List(RectTransform content,float spacing)
    {
        var layout=content.GetComponent<VerticalLayoutGroup>()??content.gameObject.AddComponent<VerticalLayoutGroup>();layout.enabled=true;
        layout.childControlWidth=layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
        layout.childScaleWidth=layout.childScaleHeight=false;layout.padding=new RectOffset(8,8,12,24);layout.spacing=spacing;Content(content);
    }
    static void Content(RectTransform content)
    {
        content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.localScale=Vector3.one;content.sizeDelta=new Vector2(0,0);content.anchoredPosition=Vector2.zero;
        var fit=content.GetComponent<ContentSizeFitter>()??content.gameObject.AddComponent<ContentSizeFitter>();fit.enabled=true;fit.horizontalFit=ContentSizeFitter.FitMode.Unconstrained;fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
    }
    static void Row(RectTransform row,float height)
    {
        foreach(var layout in row.GetComponents<LayoutGroup>())layout.enabled=false;
        row.localScale=Vector3.one;row.sizeDelta=new Vector2(824,height);
        var element=row.GetComponent<LayoutElement>()??row.gameObject.AddComponent<LayoutElement>();element.minHeight=element.preferredHeight=height;element.flexibleHeight=0;
    }
    static void Put(RectTransform rect,Transform parent,float width,float height,float x,float y)
    {rect.SetParent(parent,false);rect.localScale=Vector3.one;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(width,height);rect.anchoredPosition=new Vector2(x,y);}
    static void Stretch(RectTransform rect,Transform parent,float horizontal=0,float vertical=0)
    {rect.SetParent(parent,false);rect.localScale=Vector3.one;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,.5f);rect.offsetMin=new Vector2(horizontal,vertical);rect.offsetMax=new Vector2(-horizontal,-vertical);}
    static void Surface(Image image){image.sprite=Art("cell_bg_rounded_1024x256");image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=1;image.preserveAspect=false;image.color=Color.white;}
    static void ButtonFrame(Button button){Surface(button.image);button.transition=Selectable.Transition.ColorTint;var c=button.colors;c.normalColor=Color.white;c.disabledColor=new Color(.6f,.6f,.6f,1);button.colors=c;}
    static void Text(TMP_Text text,float size,TextAlignmentOptions alignment=TextAlignmentOptions.Center)
    {text.font=Font;text.fontSharedMaterial=Font.material;text.color=Ink;text.fontSize=size;text.enableAutoSizing=true;text.fontSizeMin=size-4;text.fontSizeMax=size;text.alignment=alignment;text.enableWordWrapping=true;text.overflowMode=TextOverflowModes.Ellipsis;text.raycastTarget=false;}

    [MenuItem("Tools/Girls Evolution/Preview Gold Panel")]
    public static void PreviewGold()=>PreviewShop("골드");
    [MenuItem("Tools/Girls Evolution/Preview Rebirth Panel")]
    public static void PreviewRebirth()=>PreviewShop("환생");
    [MenuItem("Tools/Girls Evolution/Preview Gem Panel")]
    public static void PreviewGem()=>PreviewShop("보석");
    static void PreviewShop(string name)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        UIReadabilityRefresh.ClosePreviews();var root=CanvasRoot().transform.Find("Shop");root.gameObject.SetActive(true);
        foreach(var scroll in root.GetComponentsInChildren<ScrollRect>(true).Where(s=>s.transform.parent==root)){scroll.gameObject.SetActive(scroll.name==name);if(scroll.name==name){LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);scroll.verticalNormalizedPosition=1;}}
        foreach(var shop in root.GetComponentsInChildren<ShopPanelController>())shop.RefreshAll();
        foreach(var shop in root.GetComponentsInChildren<PrestigeShopPanelController>())shop.RefreshAll();
        foreach(var automation in ShopAutomations(root))automation.Refresh();
        Canvas.ForceUpdateCanvases();
    }
    [MenuItem("Tools/Girls Evolution/Preview Codex Detail")]
    public static void PreviewDetail()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        UIReadabilityRefresh.PreviewCodex();var catalog=CanvasRoot().GetComponentInChildren<EncyclopediaPanelController>(true);
        var skin=Object.FindObjectOfType<CodexCollectionManager>(true).skins.First();
        Ref<EncyclopediaDetailPanel>(catalog,"detailPanel").ShowCollection(skin.DisplayName,skin.level,320,skin.ld,"수집 완료 · 클릭 수익 +5%\n수집 시 영구 적용 · 장착과 무관",true,"스킨 장착",()=>{});
        Canvas.ForceUpdateCanvases();
    }
}
