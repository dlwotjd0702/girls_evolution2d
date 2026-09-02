using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Idempotent scene/prefab presentation migration; keeps existing purchase callbacks.</summary>
public static class ShopPresentationRefresh
{
    const string ShopButtonFramePath = "Assets/Art/UI/shop-button-frame.png";

    public static Sprite GetShopButtonFrame()
    {
        var importer = AssetImporter.GetAtPath(ShopButtonFramePath) as TextureImporter;
        if (importer)
        {
            var border = new Vector4(190, 190, 190, 190);
            bool needsImport = importer.textureType != TextureImporterType.Sprite ||
                               importer.spriteImportMode != SpriteImportMode.Single ||
                               importer.spriteBorder != border || importer.mipmapEnabled ||
                               !importer.alphaIsTransparency;
            if (needsImport)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = border;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ShopButtonFramePath);
    }

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
        shop.GetComponent<Image>().raycastTarget = true;

        // Text-free version of the familiar purchase/upgrade frame: category names
        // remain live TMP text while the shop keeps one consistent visual language.
        var categoryFrame = GetShopButtonFrame();
        string[] categoryNames = { "골드", "환생", "보석", "상품" };
        for (int categoryIndex = 0; categoryIndex < categoryNames.Length; categoryIndex++)
        {
            var category = shop.GetComponentsInChildren<Button>(true)
                .First(b => b.transform.parent == shop && b.name == categoryNames[categoryIndex]);
            var localized = category.GetComponent<LocalizedTextureLabel>() ?? category.gameObject.AddComponent<LocalizedTextureLabel>();
            if (localized) localized.enabled = false;
            category.image.sprite = categoryFrame;
            category.image.type = Image.Type.Simple;
            category.image.preserveAspect = false;
            category.image.color = Color.white;
            // Fill the category band more completely while retaining a safe inset from
            // the shop frame. The mild overlap avoids visible seams between rounded tabs.
            Place((RectTransform)category.transform, shop, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(220, 136), new Vector2(-300 + 200 * categoryIndex, 608));
            var categoryLabel = category.GetComponentInChildren<TMP_Text>(true);
            if (!categoryLabel)
            {
                categoryLabel = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI))
                    .GetComponent<TextMeshProUGUI>();
                categoryLabel.transform.SetParent(category.transform, false);
                categoryLabel.font = font;
            }
            if (categoryLabel)
            {
                categoryLabel.gameObject.SetActive(true);
                categoryLabel.enabled = true;
                categoryLabel.text = categoryNames[categoryIndex];
                Stretch(categoryLabel.rectTransform, category.transform, new Vector2(-20, -12));
                categoryLabel.transform.SetAsLastSibling();
                Style(categoryLabel, 44, TextAlignmentOptions.Center);
                if (localized)
                {
                    localized.label = categoryLabel;
                    localized.korean = categoryNames[categoryIndex];
                    localized.english = new[] { "GOLD", "REBIRTH", "GEMS", "PRODUCTS" }[categoryIndex];
                    localized.englishOnly = false;
                    localized.backdrop = null;
                    localized.enabled = true;
                    localized.Refresh();
                }
            }
        }

        var closeButton = shop.Find("Close") as RectTransform;
        if (closeButton)
            Place(closeButton, shop, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(112, 112), new Vector2(300, 750));

        Place((RectTransform)scroll.transform, shop, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(820, 1240), new Vector2(0, -100));
        if (scroll.viewport) Stretch(scroll.viewport, scroll.transform, Vector2.zero);

        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(18, 18, 22, 32); layout.spacing = 18;
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
            Style(entry.titleText, 42, TextAlignmentOptions.Center);
            entry.titleText.enableWordWrapping = true;
            var button = entry.buyButton;
            // Keep the button frame comfortably clear of the row's painted border.
            Place((RectTransform)button.transform, row, new Vector2(0.5f, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 200), new Vector2(-78, 0));
            button.image.sprite = categoryFrame;
            button.image.type = Image.Type.Simple;
            button.image.preserveAspect = false;
            button.image.color = Color.white;
            var colors = button.colors; colors.disabledColor = new Color(0.64f, 0.64f, 0.64f, 1); button.colors = colors;
            // Old ad icon occupied the same Image as the button frame. Text now carries readiness.
            entry.buttonIcon = null;
            Stretch(entry.priceText.rectTransform, button.transform, new Vector2(-28, -26));
            Style(entry.priceText, 38, TextAlignmentOptions.Center);
            entry.priceText.enableWordWrapping = true;
        }

        // Gold, prestige, gem and product catalogs use separate controllers, but
        // their row action buttons share this scene name. Normalize every one so
        // the painted button frame cannot sit on top of the card's right border.
        var productButtons = new HashSet<Button>(store.entries
            .Where(entry => entry != null && entry.buyButton != null)
            .Select(entry => entry.buyButton));
        foreach (var actionButton in shop.GetComponentsInChildren<Button>(true)
                     .Where(button => button.name == "SummonButton" && !productButtons.Contains(button)))
        {
            var actionRect = (RectTransform)actionButton.transform;
            Place(actionRect, actionRect.parent, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(250, 180), new Vector2(-185, 0));
        }

        // 보상형 수익 버프는 상품이 아니라 필드 상태/행동이므로 상점 목록에서 분리한다.
        var reward = (RectTransform)hud.boostButton.transform;
        reward.SetParent(canvas.transform, false);
        reward.SetSiblingIndex(Mathf.Min(hud.transform.GetSiblingIndex() + 1, canvas.transform.childCount - 1));
        reward.anchorMin = reward.anchorMax = new Vector2(1, 0.5f);
        reward.pivot = new Vector2(1, 0.5f);
        // 1080 기준 168px은 320px 폭 기기에서도 약 49 논리픽셀로,
        // 단순한 아이콘 외형을 유지하면서 최소 터치 영역을 확보한다.
        reward.sizeDelta = new Vector2(168, 168);
        reward.anchoredPosition = new Vector2(-28, 270);
        reward.localScale = Vector3.one;
        foreach (var group in reward.GetComponents<LayoutGroup>()) group.enabled = false;
        var element = reward.GetComponent<LayoutElement>();
        if (element) UnityEngine.Object.DestroyImmediate(element);
        reward.name = "Field Income Boost";
        reward.GetComponent<Image>().sprite = null;
        reward.GetComponent<Image>().color = Color.clear;

        var boostIcon = reward.Find("Boost Icon") as RectTransform;
        if (!boostIcon)
        {
            boostIcon = new GameObject("Boost Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            boostIcon.SetParent(reward, false);
        }
        Stretch(boostIcon, reward, new Vector2(-12, -12));
        var iconImage = boostIcon.GetComponent<Image>();
        iconImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/income-boost-icon.png");
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        Stretch(hud.boostLabel.rectTransform, reward, new Vector2(-14, -14));
        hud.boostLabel.transform.SetAsLastSibling();
        Style(hud.boostLabel, 48, TextAlignmentOptions.Center);
        hud.boostLabel.color = Color.white;
        hud.boostLabel.enableWordWrapping = false;
        hud.popupFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/sprite/도감배경.png");
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
        panel.availabilityLabel.text = LocalizationManager.GetText(
            "필드 0/8 · 빈자리 8칸\n4단계 도달 → 2단계 소환 해금",
            "Field 0/8 · 8 free\nReach Lv.4 to summon Lv.2");
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
        rect.sizeDelta = new Vector2(0, height);
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
            go.GetComponent<SummonCell>().Setup(1, LocalizationManager.GetText("1단계\n신입 닌자", "Lv.1\nRookie Ninja"), 200, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/닌자SD2/1.png"), () => { }, () => { }, 3);
        }
        if (panel.availabilityLabel)
            panel.availabilityLabel.text = LocalizationManager.GetText(
                "필드 0/8 · 빈자리 8칸\n4단계 도달 → 2단계 소환 해금",
                "Field 0/8 · 8 free\nReach Lv.4 to summon Lv.2");
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
