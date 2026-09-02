using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

/// <summary>Repeatable theme migration: preserves objects, button callbacks and progression data.</summary>
public static class UIAndCollectionRefresh
{
    const string Art="Assets/Art/UI/Refresh/";
    static Sprite[] surfaces,main,utility;
    static TMP_FontAsset font;
    static int replacements;
    static readonly Color Ink=new Color(.96f,.94f,.86f);
    static readonly Color DarkInk=new Color(.22f,.13f,.12f);
    static readonly Dictionary<Sprite,Sprite> mapping=new Dictionary<Sprite,Sprite>();

    // Experimental atlas migration retained for reference only; user rejected this theme.
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var scene=EditorSceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/Ingame.unity") throw new InvalidOperationException("Open Ingame first.");
        surfaces=Atlas("surfaces",3,96); main=Atlas("icons-main",4,0); utility=Atlas("icons-settings",4,0);
        var canvas=Object.FindObjectsOfType<Canvas>(true).First(c=>c.isRootCanvas);
        font=canvas.GetComponentsInChildren<TextMeshProUGUI>(true).First(t=>t.font).font;
        BuildMap(); replacements=0;
        // Decorate from original identity before replacing any Sprite references.
        Decorate(canvas.gameObject);
        Remap(canvas.gameObject);
        foreach(string path in new[]{"Assets/Prefabs/SummonCell.prefab","Assets/Prefabs/EncyclopediaSlot.prefab"})
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try { Decorate(root); Remap(root); if(path.Contains("Encyclopedia")) StyleSlot(root); PrefabUtility.SaveAsPrefabAsset(root,path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        try { InstallSkins(); ConfigureHUD(canvas); ConfigureCatalog(canvas); }
        catch(Exception error) { Directory.CreateDirectory("Logs");File.WriteAllText("Logs/ui-refresh-error.txt",error.ToString());throw; }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Docs/UIRefresh");
        File.WriteAllText("Docs/UIRefresh/applied.txt",$"41 new UI atlas sprites; 8 skin sprites; {replacements} serialized Sprite references replaced.\nApplied {DateTime.Now:O}\nNo player saves changed.\n");
        Debug.Log($"[UIRefresh] Applied {replacements} sprite references, responsive codex, 4 skin pairs and permanent collection bonuses.");
        DumpUI();
    }
    static Sprite[] Atlas(string name,int columns,int border)
    {
        string path=Art+name+".png"; AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        var source=new Texture2D(2,2); ImageConversion.LoadImage(source,File.ReadAllBytes(path));int width=source.width,height=source.height;Object.DestroyImmediate(source);
        importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.mipmapEnabled=false; importer.alphaIsTransparency=true; importer.npotScale=TextureImporterNPOTScale.None;
        importer.maxTextureSize=2048; importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit=100;
        var factory=new SpriteDataProviderFactories();factory.Init();
        var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
        var oldRects=provider.GetSpriteRects();
        var rects=new List<SpriteRect>();
        for(int i=0;i<columns*columns;i++)
        {
            int col=i%columns,row=i/columns, x=col*width/columns, right=(col+1)*width/columns;
            int top=height-row*height/columns, bottom=height-(row+1)*height/columns;
            string spriteName=$"{name}-{i:00}";
            rects.Add(new SpriteRect { name=spriteName,spriteID=oldRects.FirstOrDefault(r=>r.name==spriteName)?.spriteID ?? GUID.Generate(),rect=new Rect(x,bottom,right-x,top-bottom),pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center,border=new Vector4(border,border,border,border) });
        }
        provider.SetSpriteRects(rects.ToArray());
        var names=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();names.SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
        provider.Apply(); importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
    }
    static void LoadArt()
    {
        surfaces=AssetDatabase.LoadAllAssetsAtPath(Art+"surfaces.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        main=AssetDatabase.LoadAllAssetsAtPath(Art+"icons-main.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        utility=AssetDatabase.LoadAllAssetsAtPath(Art+"icons-settings.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
    }
    static void Map(string file,Sprite replacement) { foreach(var s in AssetDatabase.LoadAllAssetsAtPath("Assets/Prefabs/sprite/"+file+".png").OfType<Sprite>()) mapping[s]=replacement; }
    static int Index(Sprite s) { int.TryParse(s.name.Substring(s.name.LastIndexOf('_')+1),out int i); return i; }
    static void BuildMap()
    {
        mapping.Clear();
        foreach(string n in new[]{"도감배경","도감패널","상점 패널","소환패널","topbar_clean","로딩화면"}) Map(n,surfaces[0]);
        Map("cell_bg_rounded_1024x256",surfaces[7]);
        foreach(string n in new[]{"골드","보석","환생","환생버튼","소환패널 버튼"}) Map(n,surfaces[3]);
        var product=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/products-tab.png"); if(product) mapping[product]=surfaces[3];
        Map("클릭골드",main[13]); Map("수동소환 쿨타임",main[14]); Map("수동소환맥스",main[0]);
        Map("수익배율",utility[10]); Map("오프라인보상",utility[11]); Map("환생시작자금",utility[10]);
        Map("환포배율",main[7]); Map("필드인구수",main[8]); Map("인구수",main[8]);
        Map("골포",main[5]); Map("환포",main[7]); Map("보석 1",main[6]);
        Map("광고시청",main[10]); Map("닫기",main[11]); Map("옵션버튼",main[4]); Map("소환버튼",main[0]);
        Map("small",main[6]); Map("medium",utility[8]); Map("large",utility[9]); Map("adremove",utility[6]); Map("2단강화",main[1]);
        Map("자동소환ㅁ",main[14]); Map("자동합성ㅁ",main[15]);
        Map("자동소환ㅇ",main[0]); Map("자동합성ㅇ",main[1]); Map("자동소환x",main[14]); Map("자동합성x",main[15]);
        var travel=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/field/2button.png");if(travel)mapping[travel]=main[9];
        foreach(var s in AssetDatabase.LoadAllAssetsAtPath("Assets/Prefabs/sprite/버튼.png").OfType<Sprite>()) mapping[s]=main[new[]{2,0,3}[Mathf.Clamp(Index(s),0,2)]];
        foreach(var s in AssetDatabase.LoadAllAssetsAtPath("Assets/Prefabs/sprite/강화구매맥스.png").OfType<Sprite>()) mapping[s]=surfaces[new[]{2,3,7}[Mathf.Clamp(Index(s),0,2)]];
        foreach(string file in new[]{"optionbuttons","번역아이콘"}) Map(file,surfaces[3]);
    }
    static void Remap(GameObject root)
    {
        foreach(var component in root.GetComponentsInChildren<Component>(true))
        {
            if(!component)continue;
            var so=new SerializedObject(component); var property=so.GetIterator(); bool changed=false;
            while(property.NextVisible(true))
                if(property.propertyType==SerializedPropertyType.ObjectReference && property.objectReferenceValue is Sprite s && mapping.TryGetValue(s,out var replacement))
                { property.objectReferenceValue=replacement; changed=true; replacements++; }
            if(changed) so.ApplyModifiedPropertiesWithoutUndo();
            if(component is Image image && image.sprite && (surfaces.Contains(image.sprite)||main.Contains(image.sprite)||utility.Contains(image.sprite)))
            {
                image.color=Color.white;
                if(image.type!=Image.Type.Filled) image.type=surfaces.Contains(image.sprite)?Image.Type.Sliced:Image.Type.Simple;
                image.preserveAspect=!surfaces.Contains(image.sprite);
                if(surfaces.Contains(image.sprite))image.pixelsPerUnitMultiplier=3;
                var rounded=image.GetComponent<RoundedPanelCorners>()??image.gameObject.AddComponent<RoundedPanelCorners>();rounded.radius=surfaces.Contains(image.sprite)?28:18;
            }
        }
    }
    static void Decorate(GameObject root)
    {
        foreach(var im in root.GetComponentsInChildren<Image>(true))
        {
            if(!im.sprite)continue;
            string file=Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(im.sprite));
            int idx=Index(im.sprite);
            if(file=="버튼") Caption(im,new[]{"상점","소환","도감"}[Mathf.Clamp(idx,0,2)],new[]{"SHOP","SUMMON","CODEX"}[Mathf.Clamp(idx,0,2)],true);
            else if(file=="optionbuttons")
            {
                var ko=new[]{"언어","사운드","초기화","쿠폰","문의","커뮤니티","저장","Google Play"};
                var en=new[]{"LANGUAGE","SOUND","RESET","COUPON","CONTACT","COMMUNITY","SAVE","Google Play"};
                Caption(im,ko[Mathf.Clamp(idx,0,7)],en[Mathf.Clamp(idx,0,7)],false);
            }
            else if(file=="번역아이콘") Caption(im,idx==0?"한국어":"ENGLISH",idx==0?"한국어":"ENGLISH",false);
            else if(file=="강화구매맥스")
            {
                var label=Caption(im,"강화","UPGRADE",false);label.stateImage=im;label.purchaseSprite=surfaces[2];label.upgradeSprite=surfaces[3];label.maxSprite=surfaces[7];
            }
            else if(file=="골드") Caption(im,"골드","GOLD",false);
            else if(file=="보석") Caption(im,"보석","GEMS",false);
            else if(file=="products-tab") Caption(im,"상품","PRODUCTS",false);
            else if(file=="환생"||file=="환생버튼") Caption(im,"환생","REBIRTH",false);
            else if(file.StartsWith("자동소환")&&file!="자동소환ㅁ") Caption(im,"자동 생성","AUTO SPAWN",true);
            else if(file.StartsWith("자동합성")&&file!="자동합성ㅁ") Caption(im,"자동 합성","AUTO MERGE",true);
        }
    }
    static LocalizedTextureLabel Caption(Image im,string ko,string en,bool bottom)
    {
        var label=im.GetComponent<LocalizedTextureLabel>()??im.gameObject.AddComponent<LocalizedTextureLabel>();
        var text=Text("Texture Label",im.transform,ko,42,Vector2.zero,Vector2.zero);
        Stretch(text.rectTransform,new Vector2(0,0),new Vector2(1,bottom?.32f:1),new Vector2(5,2),new Vector2(-5,-2));
        text.enableAutoSizing=true;text.fontSizeMin=30;text.fontSizeMax=42;text.fontStyle=FontStyles.Bold;
        label.label=text;label.korean=ko;label.english=en;text.raycastTarget=false;
        // Give labels an opaque base when an icon extends underneath them.
        if(bottom){var back=text.GetComponent<Image>(); if(!back){var r=Rect("Caption Back",im.transform,Vector2.zero,Vector2.zero);Stretch(r,new Vector2(0,0),new Vector2(1,.30f),Vector2.zero,Vector2.zero);Surface(r,3);r.SetSiblingIndex(text.transform.GetSiblingIndex());text.transform.SetAsLastSibling();}}
        return label;
    }
    static void InstallSkins()
    {
        var game=Object.FindObjectOfType<GameSystem>(true);
        var collection=game.GetComponent<CodexCollectionManager>()??game.gameObject.AddComponent<CodexCollectionManager>();
        // Never truncate an expanded catalog to the original four demonstration skins.
        collection.skins=SkinCatalogInstaller.BuildCatalog(collection.skins);
        EditorUtility.SetDirty(collection);
    }
    static void ConfigureHUD(Canvas canvas)
    {
        var hud=canvas.GetComponentInChildren<IncomeActivityHUD>(true); var panel=(RectTransform)hud.GetComponentInParent<EvolutionProgressHUD>().transform;
        Stretch(panel,new Vector2(0,1),new Vector2(1,1),Vector2.zero,Vector2.zero); panel.sizeDelta=new Vector2(-220,160);panel.anchoredPosition=new Vector2(-50,-315);Surface(panel,0);
        var content=(RectTransform)hud.transform; Stretch(content,Vector2.zero,Vector2.one,new Vector2(255,10),new Vector2(-24,-10));
        var label=hud.feverLabel;Stretch(label.rectTransform,new Vector2(0,.5f),new Vector2(1,1),Vector2.zero,Vector2.zero);label.fontSize=48;label.enableAutoSizing=false;label.text="FEVER";
        if(hud.feverHint){hud.feverHint.text="";hud.feverHint.gameObject.SetActive(false);}
        var track=(RectTransform)hud.feverFill.transform.parent;Stretch(track,new Vector2(0,0),new Vector2(1,0),Vector2.zero,Vector2.zero);track.sizeDelta=new Vector2(0,48);track.anchoredPosition=new Vector2(0,29);Surface(track,7);
        Stretch(hud.feverFill.rectTransform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);hud.feverFill.sprite=surfaces[8];hud.feverFill.type=Image.Type.Filled;hud.feverFill.fillMethod=Image.FillMethod.Horizontal;hud.feverFill.fillAmount=0;
        var field=Object.FindObjectOfType<GirlFieldManager>(true);field.topHud=panel;
        var population=Ref<TextMeshProUGUI>(field,"populationText");
        var oldPopulationParent=population.transform.parent;
        var popRoot=Rect("Population Left",panel,new Vector2(210,130),new Vector2(-300,0));Surface(popRoot,7);
        population.transform.SetParent(popRoot,false);SetRect(population.rectTransform,new Vector2(180,60),new Vector2(0,-27));population.fontSize=42;population.enableAutoSizing=false;population.alignment=TextAlignmentOptions.Center;population.text="0/30";
        population.gameObject.SetActive(true);population.color=Ink;
        if(oldPopulationParent!=popRoot)oldPopulationParent.gameObject.SetActive(false);
        var popIcon=Rect("Population Icon",popRoot,new Vector2(65,65),new Vector2(0,28));Icon(popIcon,main[8]);
        var economy=Object.FindObjectOfType<EconomyManager>(true);var income=Ref<TextMeshProUGUI>(economy,"goldPerSecText");
        var oldParent=income.transform.parent;var header=canvas.transform.Find("위쪽상태창");income.transform.SetParent(header,false);
        SetRect(income.rectTransform,new Vector2(790,60),new Vector2(-45,-48));income.fontSize=42;income.enableAutoSizing=true;income.fontSizeMin=36;income.fontSizeMax=42;income.alignment=TextAlignmentOptions.Center;income.color=Ink;income.text="+0 G/s (×1)";
        if(oldParent.childCount==1 && oldParent.GetChild(0).GetComponent<Image>())oldParent.gameObject.SetActive(false);
        foreach(var text in header.GetComponentsInChildren<TextMeshProUGUI>(true)){text.color=Ink;if(text!=income){text.fontSize=Mathf.Max(40,text.fontSize);}}
    }
    static void ConfigureCatalog(Canvas canvas)
    {
        var catalog=canvas.GetComponentInChildren<EncyclopediaPanelController>(true);var root=(RectTransform)catalog.transform;
        SetRect(root,new Vector2(1020,1740),Vector2.zero); Surface(root,0);
        var title=root.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();if(title){SetRect(title.rectTransform,new Vector2(720,100),new Vector2(0,765));title.text="도감";title.fontSize=62;title.color=Ink;}
        catalog.ninjaTab=Button("Ninja Tab",root,"닌자","NINJAS",new Vector2(410,125),new Vector2(-218,625),4);
        catalog.skinTab=Button("Skin Tab",root,"스킨","SKINS",new Vector2(410,125),new Vector2(218,625),3);
        catalog.selectedTabSprite=surfaces[4];catalog.normalTabSprite=surfaces[3];
        catalog.collectionSummary=Text("Collection Bonuses",root,"수익 +0%  ·  클릭 수익 +0%\n자동 합성 속도 +0%  ·  생성 속도 +0%",40,new Vector2(900,115),new Vector2(0,475));
        catalog.collectionCount=Text("Collection Count",root,"닌자 0/25 · 스킨 0/4 · 수집 즉시 영구 적용",32,new Vector2(920,62),new Vector2(0,381));
        var container=Ref<Transform>(catalog,"slotContainer");var scroll=container.GetComponentInParent<ScrollRect>(true);
        SetRect((RectTransform)scroll.transform,new Vector2(940,1110),new Vector2(0,-235));
        if(scroll.GetComponent<Image>())scroll.GetComponent<Image>().color=new Color(.05f,.07f,.11f,.8f);
        var grid=container.GetComponent<GridLayoutGroup>()??container.gameObject.AddComponent<GridLayoutGroup>();grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=2;grid.cellSize=new Vector2(430,470);grid.spacing=new Vector2(26,26);grid.padding=new RectOffset(20,20,20,20);grid.childAlignment=TextAnchor.UpperCenter;
        var fitter=container.GetComponent<ContentSizeFitter>()??container.gameObject.AddComponent<ContentSizeFitter>();fitter.horizontalFit=ContentSizeFitter.FitMode.Unconstrained;fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var content=(RectTransform)container;content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,7500);content.anchoredPosition=Vector2.zero;
        var close=root.Find("Close") as RectTransform;if(close)SetRect(close,new Vector2(125,125),new Vector2(425,765));
        ConfigureDetail(Ref<EncyclopediaDetailPanel>(catalog,"detailPanel"));
        catalog.gameObject.SetActive(false);
    }
    static void StyleSlot(GameObject go)
    {
        var slot=go.GetComponent<EncyclopediaSlot>();if(!slot)return;
        var bg=Ref<Image>(slot,"backgroundImage");Stretch(bg.rectTransform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);Surface(bg.rectTransform,6);
        var icon=Ref<Image>(slot,"iconImage");SetRect(icon.rectTransform,new Vector2(370,300),new Vector2(0,68));icon.preserveAspect=true;icon.raycastTarget=false;
        var caption=Ref<TextMeshProUGUI>(slot,"levelText");SetRect(caption.rectTransform,new Vector2(400,132),new Vector2(0,-153));caption.fontSize=42;caption.color=DarkInk;caption.alignment=TextAlignmentOptions.Center;caption.enableWordWrapping=true;caption.raycastTarget=false;
        var locked=Ref<GameObject>(slot,"lockedOverlay");if(locked){var r=(RectTransform)locked.transform;SetRect(r,new Vector2(65,65),new Vector2(166,190));Icon(r,main[12]);}
        var button=Ref<Button>(slot,"button");Stretch((RectTransform)button.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);button.image.color=Color.clear;button.transition=Selectable.Transition.None;
    }
    static void ConfigureDetail(EncyclopediaDetailPanel detail)
    {
        var root=(RectTransform)Ref<GameObject>(detail,"panelRoot").transform;SetRect(root,new Vector2(970,1650),Vector2.zero);Surface(root,0);
        var image=Ref<Image>(detail,"ldIllustrationImage");SetRect(image.rectTransform,new Vector2(760,820),new Vector2(0,180));image.preserveAspect=true;image.type=Image.Type.Simple;image.useSpriteMesh=false;
        var name=Ref<TextMeshProUGUI>(detail,"nameText");SetRect(name.rectTransform,new Vector2(740,85),new Vector2(0,735));name.fontSize=58;name.color=Ink;
        var level=Ref<TextMeshProUGUI>(detail,"levelText");SetRect(level.rectTransform,new Vector2(370,68),new Vector2(-210,-265));level.fontSize=42;level.color=Ink;
        var income=Ref<TextMeshProUGUI>(detail,"incomeText");SetRect(income.rectTransform,new Vector2(440,68),new Vector2(175,-265));income.fontSize=42;income.color=Ink;
        detail.collectionText=Text("Collection Requirement",root,"",38,new Vector2(865,240),new Vector2(0,-440));detail.collectionText.enableWordWrapping=true;
        detail.equipButton=Button("Equip Skin",root,"스킨 장착","EQUIP SKIN",new Vector2(740,155),new Vector2(0,-666),2);
        detail.equipLabel=detail.equipButton.GetComponentInChildren<TextMeshProUGUI>();
        var localized=detail.equipButton.GetComponent<LocalizedTextureLabel>();if(localized)Object.DestroyImmediate(localized);
        var close=Button("Close Detail",root,"×","×",new Vector2(120,120),new Vector2(395,735),5);
        SetRef(detail,"closeButton",close);detail.Hide();
    }
    static T Ref<T>(Object target,string property) where T:Object => (T)new SerializedObject(target).FindProperty(property).objectReferenceValue;
    static void SetRef(Object target,string property,Object value){var so=new SerializedObject(target);so.FindProperty(property).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
    {
        var child=parent.Find(name);var rect=child ? child.GetComponent<RectTransform>() : new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent,false);SetRect(rect,size,position);return rect;
    }
    static void SetRect(RectTransform r,Vector2 size,Vector2 position){r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.localScale=Vector3.one;r.sizeDelta=size;r.anchoredPosition=position;}
    static void Stretch(RectTransform r,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax){r.anchorMin=min;r.anchorMax=max;r.pivot=new Vector2(.5f,.5f);r.localScale=Vector3.one;r.offsetMin=offsetMin;r.offsetMax=offsetMax;}
    static Image Surface(RectTransform r,int index){var im=r.GetComponent<Image>()??r.gameObject.AddComponent<Image>();im.sprite=surfaces[index];im.type=Image.Type.Sliced;im.pixelsPerUnitMultiplier=3;im.preserveAspect=false;im.color=Color.white;var rounded=r.GetComponent<RoundedPanelCorners>()??r.gameObject.AddComponent<RoundedPanelCorners>();rounded.radius=28;return im;}
    static Image Icon(RectTransform r,Sprite s){var im=r.GetComponent<Image>()??r.gameObject.AddComponent<Image>();im.sprite=s;im.type=Image.Type.Simple;im.preserveAspect=true;im.color=Color.white;im.raycastTarget=false;return im;}
    static TextMeshProUGUI Text(string name,Transform parent,string value,float size,Vector2 bounds,Vector2 position)
    {var r=Rect(name,parent,bounds,position);var text=r.GetComponent<TextMeshProUGUI>()??r.gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.text=value;text.fontSize=size;text.color=Ink;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return text;}
    static Button Button(string name,Transform parent,string ko,string en,Vector2 bounds,Vector2 position,int surface)
    {var r=Rect(name,parent,bounds,position);var im=Surface(r,surface);var b=r.GetComponent<Button>()??r.gameObject.AddComponent<Button>();b.targetGraphic=im;Caption(im,ko,en,false);return b;}

    [MenuItem("Tools/Girls Evolution/UI and Collection/Dump UI %&d")]
    public static void DumpUI()
    {
        var canvas=Object.FindObjectsOfType<Canvas>(true).First(c=>c.isRootCanvas);
        string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
        var lines=canvas.GetComponentsInChildren<Image>(true).Select(i=>$"{PathOf(i.transform)} | {(i.sprite?i.sprite.name:"none")} | {AssetDatabase.GetAssetPath(i.sprite)} | {i.rectTransform.rect} | scale={i.transform.localScale} lossy={i.transform.lossyScale} | active={i.gameObject.activeInHierarchy} enabled={i.enabled} | type={i.type} fill={i.fillAmount}")
            .Concat(canvas.GetComponentsInChildren<TextMeshProUGUI>(true).Select(t=>$"TEXT {PathOf(t.transform)} | {t.text} | {t.rectTransform.rect} | scale={t.transform.lossyScale} | active={t.gameObject.activeInHierarchy} enabled={t.enabled} | font={t.fontSize} color={t.color}"));
        Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/ui-images.txt",lines);
    }
}
