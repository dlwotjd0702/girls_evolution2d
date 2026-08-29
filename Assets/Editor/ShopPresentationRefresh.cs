using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Idempotent scene/prefab presentation migration; keeps existing purchase callbacks.</summary>
public static class ShopPresentationRefresh
{
    public static void Configure(Canvas canvas, TMP_FontAsset font, IncomeActivityHUD hud)
    {
        var store = UnityEngine.Object.FindObjectOfType<GemStorePanelController>(true);
        var shop = store.transform.parent;
        var scroll = store.GetComponent<ScrollRect>();
        var content = scroll.content;
        Undo.RegisterFullObjectHierarchyUndo(shop.gameObject, "Mobile product shop");
        store.name = "상품";
        var tab = shop.GetComponentsInChildren<Button>(true).First(b => b.transform.parent == shop && (b.name == "현질" || b.name == "상품"));
        tab.name = "상품";
        tab.image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/products-tab.png");
        shop.GetComponent<Image>().raycastTarget = true;

        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(8, 8, 16, 24); layout.spacing = 22;
        var fit = content.GetComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1); content.sizeDelta = Vector2.zero;
        content.anchoredPosition = Vector2.zero;
        // Unused footer spacer from the original catalog.
        foreach (Transform child in content) if (child.name == "GameObject") child.gameObject.SetActive(false);

        var ordered = store.entries.OrderBy(e => e.isAdEntry ? 0 : e.isGoldAdEntry ? 1 : e.isGoldGemEntry ? 4 : (e.productId ?? "").StartsWith("remove_ad") ? 2 : 3).ToList();
        store.entries = ordered;
        int index = 0;
        foreach (var entry in ordered)
        {
            if (entry.productId == "remove_ad") entry.productId = PremiumCurrencyManager.REMOVE_ADS_ID;
            var row = (RectTransform)entry.buyButton.transform.parent;
            row.SetSiblingIndex(index++);
            Row(row, 244);
            var background = row.GetComponentsInChildren<Image>(true).FirstOrDefault(i => i.name.StartsWith("cellbg"));
            if (background) Stretch(background.rectTransform, row, Vector2.zero);
            var icon = row.Find("Image");
            if (icon) icon.gameObject.SetActive(false);
            Place(entry.titleText.rectTransform, row, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-28, 158), new Vector2(16, 0));
            Style(entry.titleText, 42, TextAlignmentOptions.MidlineLeft);
            entry.titleText.enableWordWrapping = true;
            var button = entry.buyButton;
            Place((RectTransform)button.transform, row, new Vector2(0.5f, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 200), new Vector2(-6, 0));
            button.image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/sprite/도감배경.png");
            button.image.type = Image.Type.Simple;
            button.image.preserveAspect = false;
            var colors = button.colors; colors.disabledColor = new Color(0.64f, 0.64f, 0.64f, 1); button.colors = colors;
            // Old ad icon occupied the same Image as the button frame. Text now carries readiness.
            entry.buttonIcon = null;
            Stretch(entry.priceText.rectTransform, button.transform, new Vector2(-28, -26));
            Style(entry.priceText, 38, TextAlignmentOptions.Center);
            entry.priceText.enableWordWrapping = true;
        }

        var reward = (RectTransform)hud.boostButton.transform;
        reward.SetParent(content, false); reward.SetSiblingIndex(2);
        Row(reward, 220);
        Stretch(hud.boostLabel.rectTransform, reward, new Vector2(-70, -40));
        Style(hud.boostLabel, 48, TextAlignmentOptions.Center);
        hud.boostLabel.enableWordWrapping = false;
        // The shop reward no longer reserves space on the playfield.
        var field = UnityEngine.Object.FindObjectOfType<GirlFieldManager>(true);
        field.bottomHud = null;
        store.Refresh(); hud.Refresh();
        scroll.verticalNormalizedPosition = 1;
        ConfigureSummon(font);
        EditorUtility.SetDirty(store); EditorUtility.SetDirty(field); EditorUtility.SetDirty(hud);
    }

    static void ConfigureSummon(TMP_FontAsset font)
    {
        var panel = UnityEngine.Object.FindObjectOfType<SummonPanelController>(true);
        var settings = new SerializedObject(panel);
        var content = (RectTransform)settings.FindProperty("content").objectReferenceValue;
        var scroll = content.GetComponentInParent<ScrollRect>(true);
        var outer = scroll.transform.parent;
        Undo.RegisterFullObjectHierarchyUndo(outer.gameObject, "Readable summon catalog");
        var status = outer.Find("Summon Availability") as RectTransform;
        if (!status) status = new GameObject("Summon Availability", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
        Place(status, outer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(650, 130), new Vector2(0, 440));
        panel.availabilityLabel = status.GetComponent<TextMeshProUGUI>();
        panel.availabilityLabel.font = font;
        Style(panel.availabilityLabel, 36, TextAlignmentOptions.Center);
        panel.availabilityLabel.text = "필드 0/8 · 빈자리 8칸\n4단계 도달 → 2단계 소환 해금";
        var viewport = scroll.viewport;
        viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(0, 95); viewport.offsetMax = new Vector2(0, -210);
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.childControlHeight = layout.childControlWidth = true;
        layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
        layout.spacing = 28;
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
        content.sizeDelta = new Vector2(0, content.sizeDelta.y);
        var prefab = (GameObject)settings.FindProperty("cellPrefab").objectReferenceValue;
        var path = AssetDatabase.GetAssetPath(prefab);
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var cell = root.GetComponent<SummonCell>();
            var data = new SerializedObject(cell);
            T Get<T>(string key) where T : UnityEngine.Object => (T)data.FindProperty(key).objectReferenceValue;
            Row((RectTransform)root.transform, 380);
            var background = root.GetComponentsInChildren<Image>(true).First(i => i.name == "Image (1)");
            Stretch(background.rectTransform, root.transform, Vector2.zero);
            var icon = Get<Image>("iconImage");
            Place(icon.rectTransform, root.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(135, 135), new Vector2(82, -77));
            icon.preserveAspect = true; icon.raycastTarget = false;
            var name = Get<TMP_Text>("nameLabel");
            Place(name.rectTransform, root.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(-190, 135), new Vector2(75, -77));
            Style(name, 42, TextAlignmentOptions.MidlineLeft); name.enableWordWrapping = true;
            foreach (var gem in new[] { false, true })
            {
                var button = Get<Button>(gem ? "gemSummonButton" : "summonButton");
                Place((RectTransform)button.transform, root.transform, new Vector2(gem ? 0.5f : 0, 0), new Vector2(gem ? 1 : 0.5f, 0), new Vector2(-28, 200), new Vector2(gem ? -7 : 7, 112));
                button.image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/sprite/도감배경.png");
                button.image.type = Image.Type.Simple;
                var label = Get<TMP_Text>(gem ? "gemCostLabel" : "costLabel");
                Stretch(label.rectTransform, button.transform, new Vector2(-30, -30));
                Style(label, 42, TextAlignmentOptions.Center);
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        EditorUtility.SetDirty(panel);
    }

    static void Row(RectTransform rect, float height)
    {
        // The old row's HorizontalLayoutGroup would override the new button/text anchors.
        foreach (var layout in rect.GetComponents<LayoutGroup>()) layout.enabled = false;
        rect.localScale = Vector3.one;
        rect.sizeDelta = new Vector2(630, height);
        var element = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
        element.minHeight = element.preferredHeight = height;
        element.flexibleHeight = 0;
    }

    static void Place(RectTransform rect, Transform parent, Vector2 min, Vector2 max, Vector2 size, Vector2 position)
    {
        rect.SetParent(parent, false); rect.localScale = Vector3.one;
        rect.anchorMin = min; rect.anchorMax = max; rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size; rect.anchoredPosition = position;
    }
    static void Stretch(RectTransform rect, Transform parent, Vector2 inset)
    {
        Place(rect, parent, Vector2.zero, Vector2.one, inset, Vector2.zero);
    }
    static void Style(TMP_Text text, float size, TextAlignmentOptions alignment)
    {
        text.fontSize = size; text.enableAutoSizing = false;
        text.alignment = alignment; text.raycastTarget = false;
        text.color = new Color(1, 0.93f, 0.75f); text.fontStyle = FontStyles.Bold;
    }

    [MenuItem("Tools/Girls Evolution/Preview Products")]
    public static void PreviewProducts()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var store = UnityEngine.Object.FindObjectOfType<GemStorePanelController>(true);
        store.transform.parent.gameObject.SetActive(true);
        foreach (var scroll in store.transform.parent.GetComponentsInChildren<ScrollRect>(true)) scroll.gameObject.SetActive(scroll.gameObject == store.gameObject);
        store.Refresh(); Canvas.ForceUpdateCanvases();
    }

    [MenuItem("Tools/Girls Evolution/Preview Summon %&n")]
    public static void PreviewSummon()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        ClosePreviews();
        var panel = UnityEngine.Object.FindObjectOfType<SummonPanelController>(true);
        var settings = new SerializedObject(panel);
        var content = (RectTransform)settings.FindProperty("content").objectReferenceValue;
        content.GetComponentInParent<ScrollRect>(true).transform.parent.gameObject.SetActive(true);
        if (!content.Find("Preview Summon Cell"))
        {
            var prefab = (GameObject)settings.FindProperty("cellPrefab").objectReferenceValue;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, content);
            go.name = "Preview Summon Cell"; go.hideFlags = HideFlags.DontSave;
            go.GetComponent<SummonCell>().Setup(1, "1단계\n초보 닌자", 200, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/닌자SD2/1.png"), () => { }, () => { }, 3);
        }
        Canvas.ForceUpdateCanvases();
    }

    [MenuItem("Tools/Girls Evolution/Close Shop Previews")]
    public static void ClosePreviews()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        UnityEngine.Object.FindObjectOfType<GemStorePanelController>(true).transform.parent.gameObject.SetActive(false);
        var panel = UnityEngine.Object.FindObjectOfType<SummonPanelController>(true);
        var settings = new SerializedObject(panel);
        var content = (RectTransform)settings.FindProperty("content").objectReferenceValue;
        var preview = content.Find("Preview Summon Cell");
        if (preview) UnityEngine.Object.DestroyImmediate(preview.gameObject);
        content.GetComponentInParent<ScrollRect>(true).transform.parent.gameObject.SetActive(false);
    }
}
