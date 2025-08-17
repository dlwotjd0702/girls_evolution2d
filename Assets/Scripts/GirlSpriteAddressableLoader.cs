using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GirlSpriteAddressableLoader : MonoBehaviour
{
    [Header("Addressables Labels")]
    [SerializeField] private string sdLabel = "SD";   // SD 라벨
    [SerializeField] private string ldLabel = "LD";   // LD 라벨

    [Header("Preload Options")]
    [SerializeField] private bool preloadSD = true;   // 시작 시 SD 로드
    [SerializeField] private bool preloadLD = false;  // LD는 필요 시 로드

    [Header("Key Options")]
    [SerializeField] private bool useDigitsKey = true;     // 이름의 숫자를 키로 사용(예: "14")
    [SerializeField] private bool toLowercaseKeys = true;  // 키 소문자 통일

    // 내부 캐시
    private readonly Dictionary<string, Sprite> _sd = new();
    private readonly Dictionary<string, Sprite> _ld = new();

    // 외부 노출(호환)
    public Dictionary<string, Sprite> SpriteDict => _sd;          // SD 사전(기존 호환)
    public IReadOnlyDictionary<string, Sprite> SpriteDictSD => _sd;
    public IReadOnlyDictionary<string, Sprite> SpriteDictLD => _ld;

    public bool IsLoadedSD { get; private set; }
    public bool IsLoadedLD { get; private set; }
    public bool IsReady => IsLoadedSD;

    AsyncOperationHandle<IList<Sprite>> _sdHandle;
    AsyncOperationHandle<IList<Sprite>> _ldHandle;
    bool _sdHandleValid, _ldHandleValid;

    private async void Awake()
    {
        if (preloadSD)
            await LoadLabelIntoDict(sdLabel, _sd);
        if (preloadLD)
            await LoadLabelIntoDict(ldLabel, _ld);
    }

    /// GameSystem에서 호출: SD → LD 순서로 로드(이미 로드된 건 건너뜀)
    public async Task LoadAllGirlSpritesAsync()
    {
        if (!IsLoadedSD) await LoadLabelIntoDict(sdLabel, _sd);
        if (!IsLoadedLD) await LoadLabelIntoDict(ldLabel, _ld);
    }

    public async Task LoadLabelIntoDict(string label, Dictionary<string, Sprite> dict)
    {
        var handle = Addressables.LoadAssetsAsync<Sprite>(label, s =>
        {
            var key = MakeKey(s.name); // 파일명 '1.png' → "1"
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

    public async Task EnsureLDLoadedAsync()
    {
        if (IsLoadedLD) return;
        await LoadLabelIntoDict(ldLabel, _ld);
    }

    private string MakeKey(string name)
    {
        string key = name;
        if (useDigitsKey)
        {
            // BUGFIX: @"\\d+" → @"\d+"
            var m = Regex.Match(name, @"\d+");
            key = m.Success ? m.Value : name;
        }
        if (toLowercaseKeys) key = key.ToLowerInvariant();
        return key;
    }

    /// 데이터 기준 스프라이트 얻기 (preferLD면 LD 먼저, 없으면 SD 폴백)
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

        if (preferLD && _ld.TryGetValue(key, out var sLD)) return sLD;
        if (_sd.TryGetValue(key, out var sSD)) return sSD;

        if (useDigitsKey)
        {
            key = MakeKey(rawKey);
            if (preferLD && _ld.TryGetValue(key, out sLD)) return sLD;
            if (_sd.TryGetValue(key, out sSD)) return sSD;
        }
        return null;
    }

    public void UnloadAll()
    {
        if (_sdHandleValid) { Addressables.Release(_sdHandle); _sdHandleValid = false; }
        if (_ldHandleValid) { Addressables.Release(_ldHandle); _ldHandleValid = false; }
        _sd.Clear(); _ld.Clear();
        IsLoadedSD = IsLoadedLD = false;
    }
}
