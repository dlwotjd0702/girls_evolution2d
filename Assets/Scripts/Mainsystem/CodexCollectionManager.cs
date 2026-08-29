using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Permanent discoveries and earned cosmetics. Bonuses are owned, never equipped.</summary>
public class CodexCollectionManager : MonoBehaviour, ISaveable
{
    public enum Effect { Income, ClickIncome, MergeSpeed, SpawnSpeed }
    public enum Requirement { Clicks, Merges, Spawns, Prestiges }
    [Serializable] public class Skin
    {
        public string id, koreanName, englishName;
        [Range(1,24)] public int level;
        public Sprite sd, ld;
        public Requirement requirement;
        public long target;
        public Effect effect;
        [Range(0,0.25f)] public float bonus = 0.05f;
        public string DisplayName => LocalizationManager.GetText(koreanName, englishName);
    }
    public static CodexCollectionManager Instance { get; private set; }
    public Skin[] skins = Array.Empty<Skin>();
    readonly HashSet<string> owned = new HashSet<string>();
    readonly Dictionary<int,string> equipped = new Dictionary<int,string>();
    readonly double[] bonuses = new double[4];
    readonly long[] progress = new long[4];
    int discoveredMask;
    float refreshTimer;
    public event Action Changed;
    public int Revision { get; private set; }
    public int DiscoveredCount => Enumerable.Range(1,25).Count(IsLevelDiscovered);
    public int OwnedSkinCount => skins.Count(s => s != null && owned.Contains(s.id));
    public double Multiplier(Effect effect) => 1 + bonuses[(int)effect];
    public double Bonus(Effect effect) => bonuses[(int)effect];
    public bool IsLevelDiscovered(int level) => level >= 1 && level <= 25 && (discoveredMask & (1 << (level - 1))) != 0;
    public bool Owns(string id) => !string.IsNullOrEmpty(id) && owned.Contains(id);
    public string EquippedId(int level) => equipped.TryGetValue(level, out var id) ? id : null;
    public long Progress(Skin skin) => skin == null ? 0 : progress[(int)skin.requirement];
    public static Effect LevelEffect(int level) => (Effect)((Mathf.Clamp(level,1,25) - 1) % 4);
    public const double LevelBonus = 0.01;
    public static string EffectName(Effect effect) => LocalizationManager.GetText(
        new[] { "수익", "클릭 수익", "자동 합성 속도", "생성 속도" }[(int)effect],
        new[] { "Income", "Tap income", "Auto merge speed", "Spawn speed" }[(int)effect]);

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }
    void Update()
    {
        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer < 0.5f) return;
        refreshTimer = 0;
        RefreshProgress();
    }
    public void RefreshProgress()
    {
        var stats = PlayStatsTracker.Instance;
        if (stats) Observe(stats.GetTotalClicks(), stats.GetTotalMerges(), stats.GetTotalSpawns(),
            PrestigeManager.Instance ? PrestigeManager.Instance.GetTotalPrestigeCount() : progress[3]);
    }
    public void Observe(long clicks, long merges, long spawns, long prestiges)
    {
        progress[0] = Math.Max(0,clicks); progress[1] = Math.Max(0,merges);
        progress[2] = Math.Max(0,spawns); progress[3] = Math.Max(0,prestiges);
        EvaluateUnlocks();
    }
    public void RegisterDiscovery(int level)
    {
        if (level < 1 || level > 25 || IsLevelDiscovered(level)) return;
        discoveredMask |= 1 << (level - 1);
        EvaluateUnlocks(false);
        Recalculate();
    }
    public bool CanUnlock(Skin skin) => skin != null && IsLevelDiscovered(skin.level)
        && Progress(skin) >= Math.Max(1,skin.target);
    void EvaluateUnlocks(bool notify = true)
    {
        bool changed = false;
        foreach (var skin in skins)
            if (CanUnlock(skin) && !string.IsNullOrEmpty(skin.id)) changed |= owned.Add(skin.id);
        if (changed && notify) Recalculate();
    }
    void Recalculate()
    {
        Array.Clear(bonuses,0,bonuses.Length);
        for (int level=1; level<=25; level++)
            if (IsLevelDiscovered(level)) bonuses[(int)LevelEffect(level)] += LevelBonus;
        var counted = new HashSet<string>();
        foreach (var skin in skins)
            if (skin != null && Owns(skin.id) && counted.Add(skin.id))
                bonuses[(int)skin.effect] += Math.Max(0,Math.Min(0.25,skin.bonus));
        Revision++;
        Changed?.Invoke();
    }
    public Sprite EquippedSprite(int level, bool preferLD)
    {
        if (level >= 25 || !equipped.TryGetValue(level, out var id) || !Owns(id)) return null;
        var skin = skins.FirstOrDefault(s => s != null && s.id == id && s.level == level);
        return skin == null ? null : preferLD ? skin.ld : skin.sd;
    }
    public bool Equip(int level, string id)
    {
        if (level < 1 || level >= 25) return false;
        if (string.IsNullOrEmpty(id)) equipped.Remove(level);
        else
        {
            var skin = skins.FirstOrDefault(s => s != null && s.id == id && s.level == level);
            if (skin == null || !Owns(id) || !skin.sd || !skin.ld) return false;
            equipped[level] = id;
        }
        Revision++; Changed?.Invoke();
        var field = GameSystem.Instance ? GameSystem.Instance.fieldManager : null;
        if (field && field.spriteLoader)
            foreach (var girl in field.girlList)
                if (girl && girl.Level == level) girl.RefreshAppearance(field.spriteLoader.GetFieldSprite(girl.data));
        if (Application.isPlaying) SaveManager.Instance?.SaveGame();
        return true;
    }
    public string RequirementText(Skin skin)
    {
        string metric = LocalizationManager.GetText(new[] { "클릭", "합성", "생성", "환생" }[(int)skin.requirement],
            new[] { "Taps", "Merges", "Spawns", "Prestiges" }[(int)skin.requirement]);
        string discovered = IsLevelDiscovered(skin.level) ? "✓" : "○";
        return LocalizationManager.GetText($"{discovered} {skin.level}단계 발견 · {metric} {Math.Min(Progress(skin),skin.target):N0}/{skin.target:N0}",
            $"{discovered} Discover Lv.{skin.level} · {metric} {Math.Min(Progress(skin),skin.target):N0}/{skin.target:N0}");
    }
    public void CollectSaveData(SaveData data)
    {
        data.codexDiscoveredMask = discoveredMask;
        data.ownedSkinIds = owned.OrderBy(id => id).ToList();
        data.equippedSkins = equipped.OrderBy(p => p.Key).Select(p => new EquippedSkinSave { level=p.Key, skinId=p.Value }).ToList();
    }
    public void ApplyLoadedData(SaveData data)
    {
        // Read the snapshot directly: loading order of field/stat components is not guaranteed.
        discoveredMask = (data.codexDiscoveredMask | data.discoveredMask) & 0x1ffffff;
        if (data.girls != null) foreach (var girl in data.girls)
            if (girl != null && girl.level >= 1 && girl.level <= 25) discoveredMask |= 1 << (girl.level-1);
        owned.Clear(); equipped.Clear();
        if (data.ownedSkinIds != null) foreach (string id in data.ownedSkinIds)
            if (!string.IsNullOrEmpty(id)) owned.Add(id);
        if (data.equippedSkins != null) foreach (var item in data.equippedSkins)
            if (item != null && item.level >= 1 && item.level < 25 && Owns(item.skinId)) equipped[item.level] = item.skinId;
        progress[0]=Math.Max(0,data.totalClicks); progress[1]=Math.Max(0,data.totalMerges);
        progress[2]=Math.Max(0,data.totalSpawns); progress[3]=Math.Max(0,data.totalPrestigeCount);
        EvaluateUnlocks(false); Recalculate();
    }
}
