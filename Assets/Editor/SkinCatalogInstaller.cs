using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Preserves installed skins; new pairs require explicit visual approval. Never modifies player saves.</summary>
public static class SkinCatalogInstaller
{
    public const string PlanPath = "Docs/UIRefresh/skins-96-plan.json";
    public const int LevelCount = 24, SkinsPerLevel = 4, ExpectedPairs = LevelCount * SkinsPerLevel;
    const string Folder = "Assets/Art/Characters/Skins/";
    [Serializable] public class Plan { public Item[] skins; }
    [Serializable] public class Item { public string id, koreanName, englishName, qualityStatus; public int level, slot; }
    public static Item[] Planned
    {
        get
        {
            var items=JsonUtility.FromJson<Plan>(File.ReadAllText(PlanPath)).skins;
            ValidatePlan(items);
            return items;
        }
    }
    public static void ValidatePlan(Item[] items)
    {
        if(items==null||items.Length!=ExpectedPairs||items.Any(p=>p==null||p.level<1||p.level>LevelCount||p.slot<0||p.slot>=SkinsPerLevel||string.IsNullOrWhiteSpace(p.id))||items.Select(p=>p.id).Distinct().Count()!=ExpectedPairs)
            throw new InvalidOperationException("Expected 96 unique skins for levels 1-24 (192 SD/LD images).");
        for(int level=1;level<=LevelCount;level++)
            if(!items.Where(p=>p.level==level).Select(p=>p.slot).OrderBy(s=>s).SequenceEqual(Enumerable.Range(0,SkinsPerLevel)))
                throw new InvalidOperationException("Every level requires four distinct skin slots: "+level);
    }
    public static string PathFor(string id, string kind) => Folder + id + "-" + kind + ".png";
    public static bool IsPrepared(Item item) => File.Exists(PathFor(item.id,"SD")) && File.Exists(PathFor(item.id,"LD"));
    public static bool IsInstallable(Item item, CodexCollectionManager.Skin[] existing) => IsPrepared(item) &&
        (item.qualityStatus=="approved" || (existing!=null&&existing.Any(s=>s!=null&&s.id==item.id)));

    [MenuItem("Tools/Girls Evolution/UI and Collection/Refresh Reviewed Names %&h")]
    public static void RefreshReviewedNames()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var scene=EditorSceneManager.GetActiveScene();
        if (scene.path!="Assets/Scenes/Ingame.unity") throw new InvalidOperationException("Open Ingame first.");
        var collection=Object.FindObjectOfType<CodexCollectionManager>(true);
        var plan=Planned.ToDictionary(p=>p.id);
        Undo.RecordObject(collection,"Refresh reviewed skin names");
        foreach(var skin in collection.skins)
            if(skin!=null&&plan.TryGetValue(skin.id,out var item)) ApplyDisplayNames(skin,item);
        EditorUtility.SetDirty(collection);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SkinCatalog] Refreshed display names only; no image import or player-save changes.");
    }

    [MenuItem("Tools/Girls Evolution/UI and Collection/Install Prepared Skins %&g")]
    public static void InstallPrepared()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Ingame.unity") throw new InvalidOperationException("Open Ingame first.");
        UIReadabilityRefresh.ClosePreviews();
        var collection = Object.FindObjectOfType<CodexCollectionManager>(true);
        var catalog = BuildCatalog(collection.skins);
        Undo.RecordObject(collection,"Install prepared skin pairs");
        collection.skins = catalog;
        EditorUtility.SetDirty(collection);
        UIReadabilityRefresh.ClosePreviews();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        var plan=Planned;
        var pending = plan.Where(p => !catalog.Any(s=>s.id==p.id)).Select(p => p.id).ToArray();
        int awaitingReview=plan.Count(p=>IsPrepared(p)&&p.qualityStatus!="approved");
        File.WriteAllText("Docs/UIRefresh/skin-install-status.txt", $"{DateTime.Now:O}\nConfirmed target: {ExpectedPairs} skins / {ExpectedPairs*2} SD/LD images, excluding base appearances.\nRegistered pairs: {catalog.Length}\nUnregistered pairs: {pending.Length}\nExisting file pairs awaiting visual approval/reinspection: {awaitingReview}\nRegistration is not final visual approval.\n" + string.Join("\n",pending) + "\n");
        Debug.Log($"[SkinCatalog] Registered {catalog.Length}/{ExpectedPairs} pairs; {pending.Length} remain. Player saves unchanged.");
    }

    public static CodexCollectionManager.Skin[] BuildCatalog(CodexCollectionManager.Skin[] existing)
    {
        var plan = Planned;
        existing=existing??Array.Empty<CodexCollectionManager.Skin>();
        var ready = plan.Where(p=>IsInstallable(p,existing)).OrderBy(p=>p.level).ThenBy(p=>p.slot).ToArray();
        var result = new List<CodexCollectionManager.Skin>();
        foreach (var item in ready)
        {
            var previous = existing.FirstOrDefault(s=>s!=null&&s.id==item.id);
            // Display names may be corrected without changing earned IDs, conditions or bonuses.
            var skin = previous ?? Create(item);
            ApplyDisplayNames(skin,item);
            skin.sd = Import(PathFor(item.id,"SD"),512);
            skin.ld = Import(PathFor(item.id,"LD"),1024);
            result.Add(skin);
        }
        foreach (var skin in existing.Where(s=>s!=null&&!result.Any(r=>r.id==s.id))) result.Add(skin);
        return result.OrderBy(s=>s.level).ToArray();
    }
    public static void ApplyDisplayNames(CodexCollectionManager.Skin skin, Item item)
    {
        if (skin.id != item.id || string.IsNullOrWhiteSpace(item.koreanName) || string.IsNullOrWhiteSpace(item.englishName))
            throw new InvalidOperationException("Skin name update requires matching IDs and both display names.");
        skin.koreanName=item.koreanName;
        skin.englishName=item.englishName;
    }
    static CodexCollectionManager.Skin Create(Item item)
    {
        int effect = ((item.level-1)*2+item.slot)%4;
        long levelSquared = item.level*item.level;
        long[] targets = {100+30*levelSquared,30+5*levelSquared,100+15*levelSquared,Math.Max(1,item.level/6)+item.slot};
        return new CodexCollectionManager.Skin { id=item.id, level=item.level, koreanName=item.koreanName,
            englishName=item.englishName, requirement=(CodexCollectionManager.Requirement)effect,
            target=targets[effect], effect=(CodexCollectionManager.Effect)effect, bonus=.02f };
    }
    static Sprite Import(string path,int maxSize)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=100;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
        settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
        importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
        importer.maxTextureSize=maxSize;importer.npotScale=TextureImporterNPOTScale.None;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path) ?? throw new InvalidOperationException("Sprite import failed: "+path);
    }
}
