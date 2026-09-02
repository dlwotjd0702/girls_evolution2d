using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public static class RequestedGameplayUiRepair
{
    static readonly string[] CharacterSprites =
    {
        "Assets/닌자2/1.png",
        "Assets/닌자SD2/1.png",
        "Assets/Art/Characters/Skins/lv01-a-LD.png",
        "Assets/Art/Characters/Skins/lv01-a-SD.png",
        "Assets/Art/Characters/Skins/lv01-b-LD.png",
        "Assets/Art/Characters/Skins/lv01-b-SD.png",
        "Assets/Art/Characters/Skins/lv01-c-LD.png",
        "Assets/Art/Characters/Skins/lv01-c-SD.png",
        "Assets/Art/Characters/Skins/lv01-d-LD.png",
        "Assets/Art/Characters/Skins/lv01-d-SD.png",
        "Assets/Art/Characters/LD/2.png"
    };

    [MenuItem("Tools/Girls Evolution/Apply Requested Gameplay Fixes %&m")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ImportAsFullRect("Assets/Art/UI/income-boost-icon.png");
        foreach (string path in CharacterSprites) ImportAsFullRect(path);

        var canvas = Object.FindObjectOfType<Canvas>(true);
        var hud = Object.FindObjectOfType<IncomeActivityHUD>(true);
        var activity = Object.FindObjectOfType<IncomeActivityManager>(true);
        if (!canvas || !hud || !activity)
        {
            Debug.LogError("[RequestedGameplayUiRepair] Ingame 씬의 Canvas/IncomeActivityHUD/IncomeActivityManager를 찾지 못했습니다.");
            return;
        }

        Undo.RecordObject(activity, "10 minute double income boost");
        activity.boostSeconds = 600;
        activity.adMultiplier = 2f;
        ShopPresentationRefresh.Configure(canvas, hud.boostLabel.font, hud);
        RepairCodexDetail();

        EditorUtility.SetDirty(activity);
        EditorUtility.SetDirty(hud);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[RequestedGameplayUiRepair] 저장/상점/10분 수익 2배 UI와 캐릭터 Sprite FullRect 보정을 적용했습니다.");
    }

    static void RepairCodexDetail()
    {
        var detail = Object.FindObjectOfType<EncyclopediaDetailPanel>(true);
        if (!detail) return;
        var so = new SerializedObject(detail);
        var image = so.FindProperty("ldIllustrationImage").objectReferenceValue as Image;
        if (!image) return;
        Undo.RecordObject(image.rectTransform, "Safe LD illustration framing");
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot = new Vector2(.5f, .5f);
        image.rectTransform.sizeDelta = new Vector2(760, 820);
        image.rectTransform.anchoredPosition = new Vector2(0, 180);
        image.rectTransform.localScale = Vector3.one;
        image.preserveAspect = true;
        image.type = Image.Type.Simple;
        image.useSpriteMesh = false;
        EditorUtility.SetDirty(image);
        EditorUtility.SetDirty(detail);
    }

    static void ImportAsFullRect(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!importer) return;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        bool changed = importer.textureType != TextureImporterType.Sprite
            || importer.spriteImportMode != SpriteImportMode.Single
            || settings.spriteMeshType != SpriteMeshType.FullRect
            || !importer.alphaIsTransparency
            || importer.maxTextureSize < 1024;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048;
        if (changed) importer.SaveAndReimport();
    }
}
