using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class AndroidBuildSizeOptimizer
{
    const string KoreanFontPath = "Assets/Resources/Font/Noto_Sans_KR/static/NotoSansKR-Regular SDF.asset";
    const string UiFontPath = "Assets/Fonts/army Bold SDF.asset";

    [MenuItem("Tools/Girls Evolution/Optimize Fonts For Android Build %#&9")]
    public static void OptimizeFonts()
    {
        OptimizeFont(KoreanFontPath, 2048);
        OptimizeFont(UiFontPath, 1024);
        ConfigureGlobalKoreanFallback();
        OptimizeAndroidTextures("Assets/Art/Characters/Skins", 1024);
        OptimizeAndroidTextures("Assets/Art/UI/Refresh", 2048);

        AssetDatabase.SaveAssets();
        AssetDatabase.ForceReserializeAssets(new[] { KoreanFontPath, UiFontPath },
            ForceReserializeAssetsOptions.ReserializeAssets);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Debug.Log($"[AndroidBuildSizeOptimizer] Dynamic TMP atlases applied. " +
                  $"Korean={FileSizeMb(KoreanFontPath):0.00} MB, UI={FileSizeMb(UiFontPath):0.00} MB");
    }

    static void OptimizeFont(string path, int atlasSize)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (!font)
        {
            Debug.LogError($"[AndroidBuildSizeOptimizer] Font not found: {path}");
            return;
        }

        Undo.RecordObject(font, "Optimize TMP font build size");
        font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        font.isMultiAtlasTexturesEnabled = true;

        // TMP 3.0 exposes atlasWidth/atlasHeight as read-only properties.
        // Update their serialized backing fields so this remains compatible
        // with the TMP version bundled with Unity 2022.3.
        SerializedObject serializedFont = new SerializedObject(font);
        SerializedProperty atlasWidth = serializedFont.FindProperty("m_AtlasWidth");
        SerializedProperty atlasHeight = serializedFont.FindProperty("m_AtlasHeight");
        if (atlasWidth == null || atlasHeight == null)
        {
            Debug.LogError($"[AndroidBuildSizeOptimizer] Atlas size fields not found: {path}");
            return;
        }

        atlasWidth.intValue = atlasSize;
        atlasHeight.intValue = atlasSize;
        serializedFont.ApplyModifiedPropertiesWithoutUndo();
        font.ClearFontAssetData(true);
        EditorUtility.SetDirty(font);
    }

    static void ConfigureGlobalKoreanFallback()
    {
        TMP_FontAsset koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        TMP_Settings settings = TMP_Settings.instance;
        if (!koreanFont || !settings)
        {
            Debug.LogError("[AndroidBuildSizeOptimizer] TMP settings or Korean fallback font was not found.");
            return;
        }

        if (!TMP_Settings.fallbackFontAssets.Contains(koreanFont))
        {
            Undo.RecordObject(settings, "Add global Korean TMP fallback");
            TMP_Settings.fallbackFontAssets.Add(koreanFont);
            EditorUtility.SetDirty(settings);
        }
    }

    static void OptimizeAndroidTextures(string folder, int maxTextureSize)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) continue;

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            android.name = "Android";
            android.overridden = true;
            android.maxTextureSize = maxTextureSize;
            android.format = TextureImporterFormat.ETC2_RGBA8;
            android.textureCompression = TextureImporterCompression.CompressedHQ;
            android.compressionQuality = 80;
            android.crunchedCompression = false;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
        }
    }

    static double FileSizeMb(string assetPath)
    {
        string absolutePath = Path.GetFullPath(assetPath);
        return File.Exists(absolutePath) ? new FileInfo(absolutePath).Length / 1048576d : 0;
    }
}
