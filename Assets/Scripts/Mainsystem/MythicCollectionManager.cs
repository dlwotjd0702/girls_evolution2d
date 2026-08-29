using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Reincarnation changes the final form, never the saved character level.</summary>
public class MythicCollectionManager : MonoBehaviour, ISaveable
{
    [Serializable]
    public class Form
    {
        public string id;
        public string koreanName;
        public string englishName;
        public Sprite sprite;
        public string DisplayName => LocalizationManager.GetText(koreanName, englishName);
    }

    public static MythicCollectionManager Instance { get; private set; }
    public Form[] forms = Array.Empty<Form>();
    readonly HashSet<string> discovered = new HashSet<string>();
    string currentFormId = "sakura";
    public event Action Changed;
    public int CurrentIndex => Math.Max(0, Array.FindIndex(forms, form => form.id == currentFormId));
    public Form CurrentForm => forms.Length > 0 ? forms[CurrentIndex] : null;
    public Form NextForm => forms.Length > 0 ? forms[(CurrentIndex + 1) % forms.Length] : null;
    public int CollectedCount => forms.Count(form => discovered.Contains(form.id));
    public bool IsDiscovered(string id) => discovered.Contains(id);

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    public bool RegisterCurrentForm()
    {
        if (CurrentForm == null || !discovered.Add(CurrentForm.id)) return false;
        Changed?.Invoke();
        return true;
    }

    public void AdvanceAfterPrestige()
    {
        if (NextForm == null) return;
        currentFormId = NextForm.id;
        Changed?.Invoke();
    }

    public void CollectSaveData(SaveData data)
    {
        data.activeMythicFormId = CurrentForm != null ? CurrentForm.id : currentFormId;
        data.discoveredMythicForms = discovered.OrderBy(id => id).ToList();
    }

    public void ApplyLoadedData(SaveData data)
    {
        discovered.Clear();
        if (data.discoveredMythicForms != null)
        {
            foreach (string id in data.discoveredMythicForms)
                if (!string.IsNullOrEmpty(id)) discovered.Add(id);
        }
        else if (data.totalPrestigeCount > 0 || data.level25UpgradeLevel > 0
            || (data.discoveredMask & (1 << 24)) != 0)
            discovered.Add("sakura"); // Old saves earned the original form, not new forms retroactively.

        currentFormId = string.IsNullOrEmpty(data.activeMythicFormId) ? "sakura" : data.activeMythicFormId;
        // Keep unknown collection IDs in saves so temporarily removed content is not lost.
        if (forms.Length > 0 && !forms.Any(form => form.id == currentFormId)) currentFormId = forms[0].id;
        Changed?.Invoke();
    }
}
