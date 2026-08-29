using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ScenePresentationRefresh
{
    [MenuItem("Tools/Girls Evolution/Apply Scene Polish")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Ingame.unity")
            throw new InvalidOperationException("Open Ingame before applying presentation changes.");
        var canvas = UnityEngine.Object.FindObjectsOfType<Canvas>(true).First(c => c.isRootCanvas);
        var tiers = UnityEngine.Object.FindObjectOfType<TierManager>(true);
        var field = UnityEngine.Object.FindObjectOfType<GirlFieldManager>(true);
        var prestige = UnityEngine.Object.FindObjectOfType<PrestigeManager>(true);
        var fonts = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
        var font = fonts.First(t => t.font != null).font;
        string[] files = { "garden-v2", "mountain-v2", "sky-v2", "astral-v2" };
        var sprites = files.Select(file => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Fields/" + file + ".png")).ToArray();
        if (sprites.Any(sprite => !sprite)) throw new InvalidOperationException("Import the four field sprites first.");

        Undo.RecordObject(tiers, "Refresh world art");
        var settings = new SerializedObject(tiers);
        var backgrounds = settings.FindProperty("tierBackgroundSprites");
        for (int i = 0; i < 4; i++) backgrounds.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        settings.FindProperty("inStartScale").floatValue = 1.08f;
        settings.FindProperty("outEndScale").floatValue = 0.96f;
        settings.FindProperty("riseY").floatValue = 36f;
        settings.FindProperty("nextStartYOffset").floatValue = -24f;
        settings.ApplyModifiedProperties();
        foreach (string name in new[] { "background", "background2" })
        {
            var image = canvas.transform.Find(name).GetComponent<Image>();
            Undo.RecordObject(image, "Refresh field background");
            image.sprite = sprites[0];
            image.raycastTarget = false;
        }

        // Preserve the original illustrated header and panel identity.
        var header = canvas.transform.Find("위쪽상태창");
        if (header)
        {
            var headerImage = header.GetComponentInChildren<Image>(true);
            Undo.RecordObject(headerImage, "Unify header palette");
            headerImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/sprite/topbar_clean.png");
            headerImage.color = Color.white;
            foreach (var label in header.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                Undo.RecordObject(label, "Improve currency contrast");
                label.color = new Color(0.97f, 0.95f, 0.86f);
            }
        }

        if (!UnityEngine.Object.FindObjectOfType<Camera>())
        {
            var cameraGo = new GameObject("UI Background Camera", typeof(Camera));
            Undo.RegisterCreatedObjectUndo(cameraGo, "Add UI background camera");
            var camera = cameraGo.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.12f, 0.11f);
            camera.cullingMask = 0;
            cameraGo.transform.position = new Vector3(0, 0, -10);
        }

        var hud = canvas.GetComponentInChildren<EvolutionProgressHUD>(true);
        if (!hud)
        {
            var panel = MakeRect("Evolution Progress", canvas.transform, new Vector2(0.5f, 1), new Vector2(920, 110), new Vector2(0, -210));
            panel.transform.SetSiblingIndex(canvas.transform.Find("GameObject").GetSiblingIndex());
            var backing = panel.gameObject.AddComponent<Image>();
            backing.color = new Color(0.065f, 0.12f, 0.13f, 0.90f);
            backing.raycastTarget = false;
            hud = panel.gameObject.AddComponent<EvolutionProgressHUD>();
            hud.worldLabel = MakeText("World", panel, font, 25, new Vector2(868, 36), new Vector2(0, 29));
            hud.worldLabel.color = new Color(0.68f, 0.85f, 0.77f);
            hud.goalLabel = MakeText("Next Milestone", panel, font, 30, new Vector2(868, 43), new Vector2(0, -8));
            var track = MakeRect("Progress Track", panel, new Vector2(0.5f, 0.5f), new Vector2(868, 6), new Vector2(0, -43));
            var trackImage = track.gameObject.AddComponent<Image>();
            trackImage.color = new Color(1, 1, 1, 0.12f); trackImage.raycastTarget = false;
            var fill = MakeRect("Fill", track, new Vector2(0.5f, 0.5f), new Vector2(868, 6), Vector2.zero);
            hud.progressFill = fill.gameObject.AddComponent<Image>();
            hud.progressFill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            hud.progressFill.type = Image.Type.Filled;
            hud.progressFill.fillMethod = Image.FillMethod.Horizontal;
            hud.progressFill.color = new Color(0.81f, 0.70f, 0.40f);
            hud.progressFill.raycastTarget = false;
        }
        Undo.RecordObject(hud, "Bind evolution progress");
        hud.field = field; hud.tiers = tiers; hud.prestige = prestige;
        var hudRect = (RectTransform)hud.transform;
        Undo.RecordObject(hudRect, "Keep progress below currency and away from settings");
        hudRect.sizeDelta = new Vector2(860, 110);
        hudRect.anchoredPosition = new Vector2(-50, -285);
        foreach (var child in hud.GetComponentsInChildren<RectTransform>(true))
        {
            if (child == hudRect) continue;
            Undo.RecordObject(child, "Fit progress content");
            child.sizeDelta = new Vector2(808, child.sizeDelta.y);
        }
        hud.Refresh();
        ConfigureProgressionExtras(hud, font);

        var confirm = UnityEngine.Object.FindObjectOfType<PrestigeConfirmPanel>(true);
        var confirmSettings = new SerializedObject(confirm);
        var text = (TextMeshProUGUI)confirmSettings.FindProperty("breakdownText").objectReferenceValue;
        Undo.RecordObject(text, "Improve prestige readability");
        text.enableAutoSizing = true; text.fontSizeMin = 22; text.fontSizeMax = 36;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.color = new Color(0.96f, 0.94f, 0.88f);
        text.margin = new Vector4(24, 18, 24, 18);
        text.richText = true; text.raycastTarget = false;
        Undo.RecordObject(text.rectTransform, "Make room for reset and keep details");
        text.rectTransform.sizeDelta = new Vector2(780, 690);
        text.rectTransform.anchoredPosition = new Vector2(0, 10);
        var panelRoot = (GameObject)confirmSettings.FindProperty("panelRoot").objectReferenceValue;
        Undo.RecordObject(panelRoot.transform, "Fit prestige panel to portrait screen");
        ((RectTransform)panelRoot.transform).sizeDelta = new Vector2(940, 1120);
        var panelImage = panelRoot.GetComponent<Image>();
        if (panelImage)
        {
            Undo.RecordObject(panelImage, "Unify prestige panel palette");
            panelImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/sprite/도감배경.png");
            panelImage.color = Color.white;
        }
        foreach (var label in panelRoot.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (label == text) continue;
            Undo.RecordObject(label, "Unify prestige text palette");
            label.color = new Color(1f, 0.89f, 0.66f);
        }
        foreach (string property in new[] { "confirmButton", "cancelButton" })
        {
            var button = (Button)confirmSettings.FindProperty(property).objectReferenceValue;
            var rect = (RectTransform)button.transform;
            Undo.RecordObject(rect, "Separate actions from prestige details");
            rect.anchoredPosition = new Vector2(property == "confirmButton" ? -205 : 205, -370);
            rect.sizeDelta = new Vector2(370, 116);
        }

        RoundExistingPanels(canvas);
        var game = UnityEngine.Object.FindObjectOfType<GameSystem>(true);
        confirm.Hide();
        if (game.loadingPanel)
        {
            Undo.RecordObject(game.loadingPanel, "Expose game in design view");
            game.loadingPanel.SetActive(false);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ScenePolish] Four backgrounds, world progress HUD, gentle transitions and prestige readability saved.");
    }

    static void RoundExistingPanels(Canvas canvas)
    {
        var panelSprites = new[] { "도감배경", "도감패널", "상점 패널", "소환패널", "topbar_clean" }
            .Select(name => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/sprite/" + name + ".png"))
            .Where(sprite => sprite != null).ToArray();
        int count = 0;
        foreach (var image in canvas.GetComponentsInChildren<Image>(true))
        {
            if (!panelSprites.Contains(image.sprite) && image.name != "Evolution Progress") continue;
            // Do not reshape button hit areas or disturb their existing illustrated frames.
            if (image.GetComponent<Button>()) continue;
            var effect = image.GetComponent<RoundedPanelCorners>();
            if (!effect) effect = Undo.AddComponent<RoundedPanelCorners>(image.gameObject);
            Undo.RecordObject(effect, "Round existing panel corners");
            effect.radius = image.name == "Evolution Progress" ? 18f : 40f;
            image.SetVerticesDirty();
            count++;
        }
        Debug.Log($"[ScenePolish] Rounded {count} panel images; sprites, buttons and hierarchy preserved.");
    }

    static void ConfigureProgressionExtras(EvolutionProgressHUD hud, TMP_FontAsset font)
    {
        var game = UnityEngine.Object.FindObjectOfType<GameSystem>(true);
        var collection = game.GetComponent<MythicCollectionManager>();
        if (!collection) collection = Undo.AddComponent<MythicCollectionManager>(game.gameObject);
        Undo.RecordObject(collection, "Configure reincarnation forms");
        var moon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Mythic/moon.png");
        var phoenix = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Mythic/phoenix.png");
        if (!moon || !phoenix) throw new InvalidOperationException("Import both mythic sprites before applying the scene.");
        collection.forms = new[] {
            new MythicCollectionManager.Form { id = "sakura", koreanName = "벚꽃의 시조", englishName = "Sakura Origin", sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/닌자SD2/25.png") },
            new MythicCollectionManager.Form { id = "moon", koreanName = "월영의 수호자", englishName = "Moon Guardian", sprite = moon },
            new MythicCollectionManager.Form { id = "phoenix", koreanName = "홍련의 봉황", englishName = "Crimson Phoenix", sprite = phoenix } };
        var activity = game.GetComponent<IncomeActivityManager>();
        if (!activity) activity = Undo.AddComponent<IncomeActivityManager>(game.gameObject);

        var panel = (RectTransform)hud.transform;
        hud.enabled = false;
        hud.worldLabel.gameObject.SetActive(false);
        hud.goalLabel.gameObject.SetActive(false);
        panel.Find("Progress Track").gameObject.SetActive(false);
        panel.anchorMin = new Vector2(0, 1); panel.anchorMax = new Vector2(1, 1);
        panel.sizeDelta = new Vector2(-220, 210);
        panel.anchoredPosition = new Vector2(-50, -335);
        panel.GetComponent<Image>().color = new Color(0.10f, 0.055f, 0.14f, 0.96f);

        var view = hud.GetComponentInChildren<IncomeActivityHUD>(true);
        if (!view)
        {
            var content = MakeRect("Income Activity", panel, new Vector2(0.5f, 0.5f), new Vector2(808, 78), new Vector2(0, -29));
            view = content.gameObject.AddComponent<IncomeActivityHUD>();
            view.feverLabel = MakeText("Fever Label", content, font, 23, new Vector2(520, 36), new Vector2(-144, 12));
            var track = MakeRect("Fever Track", content, new Vector2(0.5f, 0.5f), new Vector2(520, 9), new Vector2(-144, -20));
            var image = track.gameObject.AddComponent<Image>(); image.color = new Color(1, 1, 1, 0.16f); image.raycastTarget = false;
            var fill = MakeRect("Fever Fill", track, new Vector2(0.5f, 0.5f), new Vector2(520, 9), Vector2.zero);
            view.feverFill = fill.gameObject.AddComponent<Image>();
            view.feverFill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            view.feverFill.type = Image.Type.Filled; view.feverFill.fillMethod = Image.FillMethod.Horizontal;
            view.feverFill.raycastTarget = false;
            var buttonRect = MakeRect("Optional Income Boost", content, new Vector2(0.5f, 0.5f), new Vector2(250, 74), new Vector2(279, 0));
            var buttonImage = buttonRect.gameObject.AddComponent<Image>();
            buttonImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/sprite/도감배경.png");
            view.boostButton = buttonRect.gameObject.AddComponent<Button>();
            view.boostButton.targetGraphic = buttonImage;
            view.boostLabel = MakeText("Reward Label", buttonRect, font, 20, new Vector2(225, 58), Vector2.zero);
            view.boostLabel.alignment = TextAlignmentOptions.Center;
            view.boostLabel.enableAutoSizing = true; view.boostLabel.fontSizeMin = 14; view.boostLabel.fontSizeMax = 20;
            view.boostLabel.enableWordWrapping = true;
        }
        // Mobile layout: one readable fever panel and a separate thumb-sized reward button.
        var contentRect = (RectTransform)view.transform;
        contentRect.anchorMin = new Vector2(0, 0.5f); contentRect.anchorMax = new Vector2(1, 0.5f);
        contentRect.sizeDelta = new Vector2(-60, 200);
        contentRect.anchoredPosition = Vector2.zero;
        view.feverLabel.rectTransform.anchorMin = new Vector2(0, 0.5f); view.feverLabel.rectTransform.anchorMax = new Vector2(1, 0.5f);
        view.feverLabel.rectTransform.sizeDelta = new Vector2(-16, 62);
        view.feverLabel.rectTransform.anchoredPosition = new Vector2(0, 65);
        view.feverLabel.enableAutoSizing = false;
        view.feverLabel.fontSize = 48;
        view.feverLabel.fontStyle = FontStyles.Bold;
        if (!view.feverHint)
            view.feverHint = MakeText("Fever Instruction", contentRect, font, 36, new Vector2(784, 48), new Vector2(0, 12));
        view.feverHint.rectTransform.anchorMin = new Vector2(0, 0.5f); view.feverHint.rectTransform.anchorMax = new Vector2(1, 0.5f);
        view.feverHint.rectTransform.sizeDelta = new Vector2(-16, 48);
        view.feverHint.rectTransform.anchoredPosition = new Vector2(0, 12);
        view.feverHint.enableAutoSizing = false;
        view.feverHint.fontSize = 36;
        view.feverHint.color = new Color(1f, 0.9f, 0.68f);
        var feverTrack = (RectTransform)view.feverFill.transform.parent;
        feverTrack.anchorMin = new Vector2(0, 0.5f); feverTrack.anchorMax = new Vector2(1, 0.5f);
        feverTrack.sizeDelta = new Vector2(-16, 42);
        feverTrack.anchoredPosition = new Vector2(0, -62);
        feverTrack.GetComponent<Image>().color = new Color(0.34f, 0.27f, 0.36f, 1);
        view.feverFill.rectTransform.anchorMin = new Vector2(0, 0.5f); view.feverFill.rectTransform.anchorMax = new Vector2(1, 0.5f);
        view.feverFill.rectTransform.sizeDelta = new Vector2(0, 42);
        view.feverFill.rectTransform.anchoredPosition = Vector2.zero;
        view.feverFill.fillOrigin = 0;
        var canvas = hud.GetComponentInParent<Canvas>().rootCanvas;
        var rewardRect = (RectTransform)view.boostButton.transform;
        Undo.RecordObject(rewardRect, "Make ad reward usable on small phones");
        rewardRect.SetParent(canvas.transform, false);
        rewardRect.SetSiblingIndex(panel.GetSiblingIndex() + 1);
        rewardRect.anchorMin = new Vector2(0, 0); rewardRect.anchorMax = new Vector2(1, 0);
        rewardRect.pivot = new Vector2(0.5f, 0.5f);
        rewardRect.anchoredPosition = new Vector2(0, 420);
        rewardRect.sizeDelta = new Vector2(-120, 176);
        rewardRect.localScale = Vector3.one;
        var rewardColors = view.boostButton.colors;
        rewardColors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 1f);
        view.boostButton.colors = rewardColors;
        view.boostButton.targetGraphic.color = Color.white;
        view.boostLabel.rectTransform.anchorMin = new Vector2(0, 0.5f); view.boostLabel.rectTransform.anchorMax = new Vector2(1, 0.5f);
        view.boostLabel.rectTransform.sizeDelta = new Vector2(-110, 138);
        view.boostLabel.rectTransform.anchoredPosition = Vector2.zero;
        view.boostLabel.enableAutoSizing = false;
        view.boostLabel.fontSize = 48;
        view.boostLabel.richText = true;
        view.boostLabel.color = new Color(1f, 0.92f, 0.72f);
        view.boostLabel.fontStyle = FontStyles.Bold;
        view.boostLabel.enableWordWrapping = false;
        Undo.RecordObject(view, "Bind online bonus HUD");
        view.activity = activity;
        view.Refresh();
        var field = UnityEngine.Object.FindObjectOfType<GirlFieldManager>(true);
        Undo.RecordObject(field, "Reserve field space for mobile controls");
        field.topHud = panel;
        field.bottomHud = rewardRect;
        ShopPresentationRefresh.Configure(canvas, font, view);

        var encyclopedia = UnityEngine.Object.FindObjectOfType<EncyclopediaPanelController>(true);
        var serialized = new SerializedObject(encyclopedia);
        var container = (Transform)serialized.FindProperty("slotContainer").objectReferenceValue;
        var grid = container.GetComponent<GridLayoutGroup>();
        if (grid && !container.GetComponent<ContentSizeFitter>())
        {
            var rect = (RectTransform)container;
            Undo.RecordObject(rect, "Make room for mythic collection slots");
            int columns = Mathf.Max(1, grid.constraintCount);
            int rows = Mathf.CeilToInt((25 + collection.forms.Length) / (float)columns);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, grid.padding.vertical + rows * (grid.cellSize.y + grid.spacing.y) - grid.spacing.y);
        }
    }

    [MenuItem("Tools/Girls Evolution/Preview Prestige Panel")]
    public static void PreviewPrestige()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var panel = UnityEngine.Object.FindObjectOfType<PrestigeConfirmPanel>(true);
        panel.Show();
        Canvas.ForceUpdateCanvases();
        var settings = new SerializedObject(panel);
        var text = (TextMeshProUGUI)settings.FindProperty("breakdownText").objectReferenceValue;
        text.ForceMeshUpdate();
        Debug.Log($"[ScenePolish] Prestige text overflow={text.isTextOverflowing}, size={text.fontSize}");
    }

    [MenuItem("Tools/Girls Evolution/Close Prestige Preview")]
    public static void ClosePrestigePreview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        UnityEngine.Object.FindObjectOfType<PrestigeConfirmPanel>(true).Hide();
    }

    static RectTransform MakeRect(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = anchor;
        rect.sizeDelta = size; rect.anchoredPosition = position;
        return rect;
    }

    static TextMeshProUGUI MakeText(string name, Transform parent, TMP_FontAsset font, float size, Vector2 bounds, Vector2 position)
    {
        var rect = MakeRect(name, parent, new Vector2(0.5f, 0.5f), bounds, position);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font; label.fontSize = size; label.color = new Color(0.97f, 0.96f, 0.88f);
        label.alignment = TextAlignmentOptions.MidlineLeft; label.raycastTarget = false;
        label.enableWordWrapping = false;
        return label;
    }
}
