using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GirlSpriteAddressableLoader : MonoBehaviour
{
    [Header("Addressables Labels")]
    [SerializeField] private string sdLabel = "SD";
    [SerializeField] private string ldLabel = "LD";

    [Header("Preload Options")]
    [SerializeField] private bool preloadSD = true;
    [SerializeField] private bool preloadLD = false;

    [Header("Key Options")]
    [SerializeField] private bool useDigitsKey = true;
    [SerializeField] private bool toLowercaseKeys = true;

    [Header("Fallback")]
    [SerializeField] private bool allowCrossQualityFallback = true;

    private readonly Dictionary<string, Sprite> _sd = new();
    private readonly Dictionary<string, Sprite> _ld = new();

    public Dictionary<string, Sprite> SpriteDict => _sd;
    public IReadOnlyDictionary<string, Sprite> SpriteDictSD => _sd;
    public IReadOnlyDictionary<string, Sprite> SpriteDictLD => _ld;

    public bool IsLoadedSD { get; private set; }
    public bool IsLoadedLD { get; private set; }
    public bool IsReady => IsLoadedSD;

    AsyncOperationHandle<IList<Sprite>> _sdHandle;
    AsyncOperationHandle<IList<Sprite>> _ldHandle;
    bool _sdHandleValid, _ldHandleValid;

    // ▼ Single-flight: 중복 호출 흡수
    Task _sdLoadTask;
    Task _ldLoadTask;

    void Awake()
    {
        if (preloadSD) _ = EnsureSDLoadedAsync();
        if (preloadLD) _ = EnsureLDLoadedAsync();
    }

    public async Task LoadAllGirlSpritesAsync()
    {
        await EnsureSDLoadedAsync();
        await EnsureLDLoadedAsync();
    }

    public Task EnsureSDLoadedAsync()
    {
        if (IsLoadedSD) return Task.CompletedTask;
        return _sdLoadTask ??= LoadLabelIntoDict(sdLabel, _sd);
    }

    public Task EnsureLDLoadedAsync()
    {
        if (IsLoadedLD) return Task.CompletedTask;
        return _ldLoadTask ??= LoadLabelIntoDict(ldLabel, _ld);
    }

    async Task LoadLabelIntoDict(string label, Dictionary<string, Sprite> dict)
    {
        var handle = Addressables.LoadAssetsAsync<Sprite>(label, s =>
        {
            var key = MakeKey(s.name);
            if (dict.ContainsKey(key))
                Debug.LogWarning($"[SpriteLoader] key collision '{key}' in label '{label}' (last wins)");
            dict[key] = s;
        }, true);

        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            if (label == sdLabel) { _sdHandle = handle; _sdHandleValid = true; IsLoadedSD = true; }
            if (label == ldLabel) { _ldHandle = handle; _ldHandleValid = true; IsLoadedLD = true; }
            Debug.Log($"[SpriteLoader] Loaded '{label}' (count≈{handle.Result?.Count ?? 0})");
        }
        else
        {
            Debug.LogError($"[SpriteLoader] Load failed: {label}");
        }
    }

    private string MakeKey(string name)
    {
        string key = name;
        if (useDigitsKey)
        {
            var m = Regex.Match(name, @"\d+");
            if (m.Success)
            {
                if (int.TryParse(m.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int num))
                    key = num.ToString(CultureInfo.InvariantCulture);
                else
                    key = m.Value;
            }
        }
        if (toLowercaseKeys) key = key.ToLowerInvariant();
        return key;
    }

    public Sprite GetSpriteForData(GirlData data, bool preferLD = false)
    {
        if (data == null) return null;
        string rawKey = !string.IsNullOrEmpty(data.spriteName) ? data.spriteName : data.level.ToString();
        return GetSpriteByKey(rawKey, preferLD);
    }

    public Sprite GetSpriteByKey(string rawKey, bool preferLD = false)
    {
        if (string.IsNullOrEmpty(rawKey)) return null;
        string key = toLowercaseKeys ? rawKey.ToLowerInvariant() : rawKey;

        if (preferLD)
        {
            if (_ld.TryGetValue(key, out var sLD)) return sLD;
            if (_sd.TryGetValue(key, out var sSD)) return sSD;
        }
        else
        {
            if (_sd.TryGetValue(key, out var sSD)) return sSD;
            if (allowCrossQualityFallback && _ld.TryGetValue(key, out var sLD)) return sLD;
        }

        if (useDigitsKey)
        {
            key = MakeKey(rawKey);
            if (preferLD)
            {
                if (_ld.TryGetValue(key, out var sLD2)) return sLD2;
                if (_sd.TryGetValue(key, out var sSD2)) return sSD2;
            }
            else
            {
                if (_sd.TryGetValue(key, out var sSD2)) return sSD2;
                if (allowCrossQualityFallback && _ld.TryGetValue(key, out var sLD2)) return sLD2;
            }
        }
        return null;
    }

    public void UnloadAll()
    {
        if (_sdHandleValid) { Addressables.Release(_sdHandle); _sdHandleValid = false; }
        if (_ldHandleValid) { Addressables.Release(_ldHandle); _ldHandleValid = false; }
        _sd.Clear(); _ld.Clear();
        IsLoadedSD = IsLoadedLD = false;
        _sdLoadTask = _ldLoadTask = null;
    }
}
