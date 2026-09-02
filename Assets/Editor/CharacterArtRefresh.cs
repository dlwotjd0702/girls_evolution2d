using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>Applies reviewed LD art only; character IDs, SD art and mythic forms stay unchanged.</summary>
public static class CharacterArtRefresh
{
    static Action restorePreview;
    public static readonly int[] ReviewedLevels = { 2, 6, 7, 10, 11, 12, 13, 14, 15, 16, 17, 19, 20, 23, 24 };
    public static string PathFor(int level) => $"Assets/Art/Characters/LD/{level}.png";

    public static bool HasCleanAlpha(string path)
    {
        if (!File.Exists(path)) return false;
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path))) return false;
            if (texture.width < 500 || texture.height < 500) return false;
            var pixels = texture.GetPixels32();
            int empty = 0, opaque = 0;
            for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
            {
                byte alpha = pixels[y * texture.width + x].a;
                if (alpha == 0) empty++;
                // PNG encoders commonly quantize visually opaque pixels to 250-254.
                // Treat that range as solid while still requiring a fully clear border.
                if (alpha >= 250) opaque++;
                if ((x == 0 || y == 0 || x == texture.width - 1 || y == texture.height - 1) && alpha != 0) return false;
            }
            return empty > pixels.Length * 0.3 && opaque > pixels.Length * 0.05;
        }
        finally { UnityEngine.Object.DestroyImmediate(texture); }
    }

    [MenuItem("Tools/Girls Evolution/Art/Apply Reviewed LD")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        // Validate the complete batch before changing any addressable entry.
        foreach (int level in ReviewedLevels)
            if (!HasCleanAlpha(PathFor(level))) throw new InvalidOperationException("Unreviewed or opaque art: " + PathFor(level));

        var oldEntries = settings.groups.Where(g => g != null).SelectMany(g => g.entries)
            .Where(e => e.labels.Contains("LD")).ToArray();
        foreach (int level in ReviewedLevels)
            if (oldEntries.Count(e => e.address == level.ToString()) != 1)
                throw new InvalidOperationException("Ambiguous LD address: " + level);

        Undo.RecordObject(settings, "Apply reviewed LD sprites");
        foreach (var group in settings.groups.Where(g => g != null)) Undo.RecordObject(group, "Apply reviewed LD sprites");
        foreach (int level in ReviewedLevels)
        {
            string path = PathFor(level);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (!sprite || sprite.name != level.ToString()) throw new InvalidOperationException("Sprite key mismatch: " + path);
            var old = oldEntries.Single(e => e.address == level.ToString());
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (old.guid != guid) settings.RemoveAssetEntry(old.guid);
            var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.address = level.ToString();
            entry.SetLabel("LD", true, true);
            entry.SetLabel("SD", false);
        }
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
        AssetDatabase.SaveAssets();
        ExportManifest();
        Debug.Log($"[CharacterArt] Applied {ReviewedLevels.Length} reviewed LD sprites. SD and 25+ forms unchanged.");
    }

    [Serializable] public class SpriteRecord { public string quality, address, path, spriteName; }
    [Serializable] class Manifest { public SpriteRecord[] sprites; }
    public static void ExportManifest()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var records = new List<SpriteRecord>();
        foreach (var entry in settings.groups.Where(g => g != null).SelectMany(g => g.entries))
        foreach (var quality in new[] { "SD", "LD" })
            if (entry.labels.Contains(quality))
                records.Add(new SpriteRecord { quality = quality, address = entry.address, path = entry.AssetPath,
                    spriteName = AssetDatabase.LoadAssetAtPath<Sprite>(entry.AssetPath)?.name });
        Directory.CreateDirectory("Docs/ArtReview");
        File.WriteAllText("Docs/ArtReview/applied-sprites.json", JsonUtility.ToJson(new Manifest { sprites = records.ToArray() }, true));
    }

    [MenuItem("Tools/Girls Evolution/Art/Preview LD Detail")]
    public static void PreviewDetail()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        restorePreview?.Invoke();
        var panel = UnityEngine.Object.FindObjectOfType<EncyclopediaDetailPanel>(true);
        var state = new SerializedObject(panel);
        var root = (GameObject)state.FindProperty("panelRoot").objectReferenceValue;
        var illustration = (UnityEngine.UI.Image)state.FindProperty("ldIllustrationImage").objectReferenceValue;
        var labels = new[] { "nameText", "levelText", "incomeText" }.Select(key => (TMPro.TMP_Text)state.FindProperty(key).objectReferenceValue).ToArray();
        var texts = labels.Select(label => label.text).ToArray();
        var sprite = illustration.sprite; var color = illustration.color; bool aspect = illustration.preserveAspect;
        bool active = root.activeSelf;
        restorePreview = () => {
            illustration.sprite = sprite; illustration.color = color; illustration.preserveAspect = aspect;
            for (int i = 0; i < labels.Length; i++) labels[i].text = texts[i];
            root.SetActive(active);
        };
        panel.Show("얼음 오브 닌자", 20, 0, AssetDatabase.LoadAssetAtPath<Sprite>(PathFor(20)));
        Selection.activeGameObject = panel.gameObject;
        Canvas.ForceUpdateCanvases();
    }

    [MenuItem("Tools/Girls Evolution/Art/Close LD Detail Preview")]
    public static void CloseDetail()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        restorePreview?.Invoke(); restorePreview = null;
    }
}
