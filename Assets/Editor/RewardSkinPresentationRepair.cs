using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Only the offline popup and codex lock badge; never changes progression or saves.</summary>
public static class RewardSkinPresentationRepair
{
    const string PreviewName = "Offline Reward Preview";
    static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/army Bold SDF.asset");
    static Sprite Art(string name) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/sprite/" + name + ".png");
    static T Ref<T>(Object source, string name) where T : Object => PanelLayoutRepair.Ref<T>(source, name);
    static void SetRef(Object source, string name, Object value)
    {
        var serialized = new SerializedObject(source);
        serialized.FindProperty(name).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    static RectTransform Child(Transform parent, string name)
    {
        var found = parent.Find(name) as RectTransform;
        return found ? found : (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
    }
    static void Put(RectTransform rect, Transform parent, float w, float h, float x, float y)
    {
        rect.SetParent(parent, false); rect.localScale = Vector3.one;
        rect.gameObject.layer = parent.gameObject.layer;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.sizeDelta = new Vector2(w, h); rect.anchoredPosition = new Vector2(x, y);
    }
    static void Stretch(RectTransform rect)
    {
        rect.localScale = Vector3.one; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one * .5f; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    static void Text(TMP_Text text, float size)
    {
        text.font = Font; text.fontSharedMaterial = Font.material; text.color = new Color(1, .95f, .81f);
        text.fontSize = size; text.enableAutoSizing = true; text.fontSizeMin = size - 4; text.fontSizeMax = size;
        text.alignment = TextAlignmentOptions.Center; text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis; text.raycastTarget = false;
    }
    static void Surface(Image image)
    {
        image.sprite = Art("cell_bg_rounded_1024x256"); image.type = Image.Type.Sliced;
        image.preserveAspect = false; image.pixelsPerUnitMultiplier = 1; image.color = Color.white;
    }
    static TMP_Text Label(Transform parent, string name, float size)
    {
        var rect = Child(parent, name); rect.SetParent(parent, false);
        var label = rect.GetComponent<TextMeshProUGUI>() ?? rect.gameObject.AddComponent<TextMeshProUGUI>();
        Text(label, size); return label;
    }

    public static void ConfigureLockBadge(EncyclopediaSlot slot)
    {
        var overlay = Ref<GameObject>(slot, "lockedOverlay");
        if (!overlay) return;
        var rect = (RectTransform)overlay.transform;
        Put(rect, slot.transform, 250, 260, 0, 85);
        var background = overlay.GetComponent<Image>(); Surface(background); background.raycastTarget = false;
        // The original lock texture has large painted margins. Clip its display
        // region so the symbol itself stays readable without altering the asset.
        var symbol = Child(rect, "Lock Symbol"); Put(symbol, rect, 140, 176, 0, 28);
        var previousIcon = symbol.GetComponent<Image>(); if (previousIcon) previousIcon.enabled = false;
        if (!symbol.GetComponent<RectMask2D>()) symbol.gameObject.AddComponent<RectMask2D>();
        var art = Child(symbol, "Artwork"); Put(art, symbol, 420, 420, 0, 0);
        var icon = art.GetComponent<Image>() ?? art.gameObject.AddComponent<Image>();
        icon.sprite = Art("잠금"); icon.color = Color.white; icon.preserveAspect = true; icon.raycastTarget = false;
        var label = Label(rect, "Lock Label", 44); Put(label.rectTransform, rect, 210, 64, 0, -92);
        label.text = LocalizationManager.GetText("미해금", "LOCKED");
        SetRef(slot, "lockedLabel", label);
        overlay.transform.SetAsLastSibling();
    }

    [MenuItem("Tools/Girls Evolution/Repair Offline Reward and Skin Presentation %&r")]
    public static void Apply()
    {
        try { ApplyCore(); }
        catch (Exception error) { Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/reward-skin-repair-error.txt", error.ToString()); throw; }
    }
    static void ApplyCore()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Ingame.unity") throw new InvalidOperationException("Open Ingame first.");
        UIReadabilityRefresh.ClosePreviews();
        var reward = Object.FindObjectOfType<OfflineRewardPanel>(true);
        // The original host is a plain Transform. Reload the narrowly migrated
        // scene so its preserved fileID is resolved as a RectTransform.
        if (!reward.GetComponent<RectTransform>())
        {
            scene = EditorSceneManager.OpenScene(scene.path);
            reward = Object.FindObjectOfType<OfflineRewardPanel>(true);
        }
        Undo.RegisterFullObjectHierarchyUndo(reward.gameObject, "Repair offline reward popup");
        var host = reward.GetComponent<RectTransform>();
        if (!host) throw new InvalidOperationException("Migrate the offline reward host to RectTransform first.");
        Stretch(host);
        var root = (RectTransform)Ref<GameObject>(reward, "panelRoot").transform;
        foreach (var rounded in root.GetComponentsInChildren<RoundedPanelCorners>(true)) rounded.enabled = false;
        var title = root.GetComponentsInChildren<TextMeshProUGUI>(true).Single(t => t.name == "문구");
        var card = Child(root, "Reward Card"); Put(card, root, 960, 1120, 0, 0);
        Surface(card.GetComponent<Image>() ?? card.gameObject.AddComponent<Image>());
        Stretch(root);
        var dimmer = root.GetComponent<Image>(); dimmer.sprite = null; dimmer.type = Image.Type.Simple;
        dimmer.color = new Color(.02f, .01f, .04f, .78f); dimmer.raycastTarget = true;
        Put(title.rectTransform, card, 820, 104, 0, 435); Text(title, 60);
        title.text = LocalizationManager.GetText("오프라인 보상", "OFFLINE REWARD"); SetRef(reward, "titleText", title);
        var duration = Ref<TextMeshProUGUI>(reward, "durationText");
        Put(duration.rectTransform, card, 820, 140, 0, 280); Text(duration, 44);
        var amount = Ref<TextMeshProUGUI>(reward, "rewardText");
        Put(amount.rectTransform, card, 650, 144, 55, 100); Text(amount, 76);
        var coin = Child(card, "Reward Coin"); Put(coin, card, 110, 110, -340, 100);
        var coinImage = coin.GetComponent<Image>() ?? coin.gameObject.AddComponent<Image>();
        coinImage.sprite = Art("골포"); coinImage.color = Color.white; coinImage.preserveAspect = true; coinImage.raycastTarget = false;
        SetRef(reward, "rewardIcon", coinImage);
        foreach (bool ad in new[] { true, false })
        {
            var button = Ref<Button>(reward, ad ? "claimAdButton" : "claimJustButton");
            Put((RectTransform)button.transform, card, 800, 180, 0, ad ? -145 : -365);
            foreach (var old in button.GetComponentsInChildren<TMP_Text>(true)) old.gameObject.SetActive(false);
            Surface(button.image); button.image.raycastTarget = true; button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors; colors.normalColor = Color.white; colors.disabledColor = new Color(.55f, .55f, .6f, 1); button.colors = colors;
            var label = Label(button.transform, "Reward Caption", 48); label.gameObject.SetActive(true);
            Put(label.rectTransform, button.transform, 750, 140, 0, 0);
            label.text = ad ? LocalizationManager.GetText("광고 보고 2배 받기", "WATCH AD · 2× REWARD") : LocalizationManager.GetText("그냥 받기", "CLAIM");
            SetRef(reward, ad ? "claimAdLabel" : "claimJustLabel", label);
        }
        SetRef(reward, "claimButton", null); // Previously aliased to the ad button.
        SetRef(reward, "adButtonIcon", null); // The old atlas plate must not replace the new button frame.
        var prefab = PrefabUtility.LoadPrefabContents("Assets/Prefabs/EncyclopediaSlot.prefab");
        try { ConfigureLockBadge(prefab.GetComponent<EncyclopediaSlot>()); PrefabUtility.SaveAsPrefabAsset(prefab, "Assets/Prefabs/EncyclopediaSlot.prefab"); }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        root.gameObject.SetActive(false);
        EditorUtility.SetDirty(reward); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("[RewardSkinRepair] Offline popup and mobile lock badge repaired. No save changes.");
    }

    [MenuItem("Tools/Girls Evolution/Preview Offline Reward %&y")]
    public static void PreviewReward()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        UIReadabilityRefresh.ClosePreviews();
        var source = Object.FindObjectOfType<OfflineRewardPanel>(true);
        var clone = Object.Instantiate(source.gameObject, source.transform.parent);
        clone.name = PreviewName; clone.hideFlags = HideFlags.DontSave;
        var reward = clone.GetComponent<OfflineRewardPanel>();
        // Display-only fixture: no economy, currency, ad service, or reward callbacks.
        foreach (var key in new[] { "economy", "premiumCurrency", "adServiceBehaviour" }) SetRef(reward, key, null);
        reward.Show(new OfflineRewardPanel.Payload { rawSeconds = 10800, appliedSeconds = 7200, totalReward = 123456789, multiplier = 1 });
        foreach (var key in new[] { "claimAdButton", "claimJustButton" })
        {
            var button = Ref<Button>(reward, key); button.onClick.RemoveAllListeners(); button.gameObject.SetActive(true); button.interactable = true;
        }
        Ref<TMP_Text>(reward, "claimAdLabel").text = LocalizationManager.GetText("광고 보고 2배 받기", "WATCH AD · 2× REWARD");
        Canvas.ForceUpdateCanvases();
    }
    public static void CloseRewardPreview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        // FindObjectsOfType excludes DontSave fixtures, so inspect loaded objects
        // and keep the scene/name/flags guard before destroying anything.
        foreach (var preview in Resources.FindObjectsOfTypeAll<OfflineRewardPanel>().Where(p => p.gameObject.scene.IsValid() && p.name == PreviewName && p.gameObject.hideFlags == HideFlags.DontSave))
            Object.DestroyImmediate(preview.gameObject);
    }
}
