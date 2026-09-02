using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Adds only language-dependent captions over baked Korean lettering. Original
/// sprites, transforms, callbacks and the Korean presentation remain untouched.
/// </summary>
public static class GlobalPresentationPolish
{
    const string BackdropName = "Global English Backdrop";
    const string LabelName = "Global English Label";
    static readonly Color Light = new Color(1f, .96f, .82f, 1f);
    static readonly Color Purple = new Color(.11f, .055f, .16f, .97f);

    [MenuItem("Tools/Girls Evolution/Global UI/Apply Minimal Localization")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Ingame.unity") throw new InvalidOperationException("Open Ingame first.");
        var canvas = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Canvas>(true)).First(c => c.isRootCanvas);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/army Bold SDF.asset") ?? canvas.GetComponentInChildren<TextMeshProUGUI>(true).font;
        var nav = canvas.transform.Find("GameObject");

        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Minimal global UI localization");
        ConfigureBaked(nav.Find("소환"), "소환", "SUMMON", font, new Color(.38f, .10f, .045f, .97f), false);
        ConfigureBaked(nav.Find("상점"), "상점", "SHOP", font, new Color(.40f, .20f, .045f, .97f), false);
        ConfigureBaked(nav.Find("도감"), "도감", "CODEX", font, new Color(.10f, .29f, .10f, .97f), false);
        ConfigureBaked(nav.Find("자동소환"), "자동 소환", "AUTO SPAWN", font, Purple, true);
        ConfigureBaked(nav.Find("자동합성"), "자동 합성", "AUTO MERGE", font, Purple, true);
        var shop = canvas.transform.Find("Shop");
        ConfigureShopLabels(shop, font);
        // Keep the English title inside the existing wooden title plate.  The old
        // coordinates placed a second header above it and made the panel look redesigned.
        ConfigurePanelTitle(shop, "상점", "SHOP", font, new Vector2(.38f, .775f), new Vector2(.62f, .875f));

        var summon = canvas.GetComponentInChildren<SummonPanelController>(true);
        var summonContent = new SerializedObject(summon).FindProperty("content").objectReferenceValue as RectTransform;
        var summonRoot = summonContent.GetComponentInParent<ScrollRect>(true).transform.parent;
        ConfigurePanelTitle(summonRoot, "소환", "SUMMON", font, new Vector2(.29f, .775f), new Vector2(.71f, .91f));

        var codex = canvas.GetComponentInChildren<EncyclopediaPanelController>(true);
        var codexTitle = codex.transform.Find("Text (TMP)")?.GetComponent<TMP_Text>();
        if (codexTitle)
        {
            var localizedTitle = codexTitle.GetComponent<LocalizedTextureLabel>() ?? codexTitle.gameObject.AddComponent<LocalizedTextureLabel>();
            localizedTitle.label = codexTitle;
            localizedTitle.korean = "도감";
            localizedTitle.english = "CODEX";
            localizedTitle.englishOnly = false;
            localizedTitle.backdrop = null;
            localizedTitle.enabled = true;
            localizedTitle.Refresh();
        }

        var hud = canvas.GetComponentInChildren<IncomeActivityHUD>(true);
        hud.NormalizeFeverLayout();

        Canvas.ForceUpdateCanvases();
        foreach (var graphic in canvas.GetComponentsInChildren<Graphic>(true)) graphic.SetAllDirty();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[GlobalUI] Original Korean art preserved; English-only overlays and responsive fever layout installed.");
    }

    [MenuItem("Tools/Girls Evolution/Global UI/Preview English")]
    public static void PreviewEnglish() => Preview(false);

    [MenuItem("Tools/Girls Evolution/Global UI/Preview Korean")]
    public static void PreviewKorean() => Preview(true);

    static void Preview(bool korean)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        LocalizationManager.SetLanguage(korean);
        foreach (var label in Object.FindObjectsOfType<LocalizedTextureLabel>(true)) label.Refresh();
        Canvas.ForceUpdateCanvases();
        SceneView.RepaintAll();
        EditorApplication.QueuePlayerLoopUpdate();
        Debug.Log("[GlobalUI] Preview: " + (korean ? "Korean original art" : "English localized overlays"));
    }

    public static void ConfigureShopLabels(Transform shop, TMP_FontAsset font)
    {
        if (!shop) return;
        string[] names = { "골드", "환생", "보석", "상품" };
        string[] english = { "GOLD", "REBIRTH", "GEMS", "PRODUCTS" };
        for (int i = 0; i < names.Length; i++)
        {
            var button = shop.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.transform.parent == shop && b.name == names[i]);
            if (!button) continue;
            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (!text) continue;
            text.gameObject.SetActive(true);
            text.font = font;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = 26;
            text.fontSizeMax = 42;
            text.raycastTarget = false;
            var localized = button.GetComponent<LocalizedTextureLabel>() ?? button.gameObject.AddComponent<LocalizedTextureLabel>();
            localized.label = text;
            localized.korean = names[i];
            localized.english = english[i];
            localized.englishOnly = false;
            localized.backdrop = null;
            localized.enabled = true;
            localized.Refresh();
        }

        var states = AssetDatabase.LoadAllAssetsAtPath("Assets/Prefabs/sprite/강화구매맥스.png").OfType<Sprite>().OrderBy(s => s.name).ToArray();
        foreach (var localized in shop.GetComponentsInChildren<LocalizedTextureLabel>(true).Where(l => l.stateImage && l.label))
        {
            if (states.Length >= 3)
            {
                localized.purchaseSprite = states[0];
                localized.upgradeSprite = states[1];
                localized.maxSprite = states[2];
            }
            var backdrop = EnsureImage(localized.transform, BackdropName, Purple);
            SetAnchors(backdrop.rectTransform, new Vector2(.07f, .14f), new Vector2(.93f, .86f));
            SetAnchors(localized.label.rectTransform, new Vector2(.07f, .14f), new Vector2(.93f, .86f));
            localized.label.font = font;
            localized.label.color = new Color(1f, .78f, .18f, 1f);
            localized.label.alignment = TextAlignmentOptions.Center;
            localized.label.enableAutoSizing = true;
            localized.label.fontSizeMin = 28;
            localized.label.fontSizeMax = 54;
            localized.label.raycastTarget = false;
            backdrop.transform.SetAsLastSibling();
            localized.label.transform.SetAsLastSibling();
            localized.englishOnly = true;
            localized.backdrop = backdrop;
            localized.enabled = true;
            localized.Refresh();
        }
    }

    static void ConfigureBaked(Transform root, string korean, string english, TMP_FontAsset font, Color backdropColor, bool automatic)
    {
        if (!root) throw new InvalidOperationException("Missing UI object: " + korean);
        var backdrop = EnsureImage(root, BackdropName, backdropColor);
        var labelTransform = root.Find(LabelName);
        var label = labelTransform ? labelTransform.GetComponent<TextMeshProUGUI>() : null;
        if (!label)
        {
            var go = new GameObject(LabelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(root, false);
            label = go.GetComponent<TextMeshProUGUI>();
        }

        Vector2 min = automatic ? new Vector2(.14f, .075f) : new Vector2(.08f, .055f);
        Vector2 max = automatic ? new Vector2(.86f, .285f) : new Vector2(.92f, .34f);
        SetAnchors(backdrop.rectTransform, min, max);
        SetAnchors(label.rectTransform, min, max);
        label.font = font;
        label.color = Light;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.enableAutoSizing = true;
        label.fontStyle = FontStyles.Bold;
        label.fontSizeMin = automatic ? 78 : 24;
        label.fontSizeMax = automatic ? 128 : 44;
        label.raycastTarget = false;
        var shadow = label.GetComponent<Shadow>() ?? label.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, .9f);
        shadow.effectDistance = automatic ? new Vector2(5, -5) : new Vector2(2, -2);
        backdrop.transform.SetAsLastSibling();
        label.transform.SetAsLastSibling();

        var localized = root.GetComponent<LocalizedTextureLabel>() ?? root.gameObject.AddComponent<LocalizedTextureLabel>();
        localized.label = label;
        localized.korean = korean;
        localized.english = english;
        localized.stateImage = null;
        localized.englishOnly = true;
        localized.backdrop = backdrop;
        localized.enabled = true;
        localized.Refresh();
    }

    static void ConfigurePanelTitle(Transform root, string korean, string english, TMP_FontAsset font, Vector2 min, Vector2 max)
    {
        if (!root) return;
        var backdrop = EnsureImage(root, BackdropName, new Color(.075f, .068f, .065f, .985f));
        var labelTransform = root.Find(LabelName);
        var label = labelTransform ? labelTransform.GetComponent<TextMeshProUGUI>() : null;
        if (!label)
        {
            var go = new GameObject(LabelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(root, false);
            label = go.GetComponent<TextMeshProUGUI>();
        }
        SetAnchors(backdrop.rectTransform, min, max);
        SetAnchors(label.rectTransform, min, max);
        label.font = font;
        label.color = Light;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.enableAutoSizing = true;
        label.fontStyle = FontStyles.Bold;
        label.fontSizeMin = 42;
        label.fontSizeMax = 86;
        label.raycastTarget = false;
        backdrop.transform.SetAsLastSibling();
        label.transform.SetAsLastSibling();
        var localized = root.GetComponent<LocalizedTextureLabel>() ?? root.gameObject.AddComponent<LocalizedTextureLabel>();
        localized.label = label;
        localized.korean = korean;
        localized.english = english;
        localized.stateImage = null;
        localized.englishOnly = true;
        localized.backdrop = backdrop;
        localized.enabled = true;
        localized.Refresh();
    }

    static Image EnsureImage(Transform parent, string name, Color color)
    {
        var child = parent.Find(name);
        Image image = child ? child.GetComponent<Image>() : null;
        if (!image)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            image = go.GetComponent<Image>();
        }
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = new Vector2(.5f, .5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
