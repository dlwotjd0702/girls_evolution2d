using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GirlDataManager
{
    private List<GirlData> dataList = new();
    private readonly Dictionary<int, GirlData> dataByLevel = new();

    public bool IsLoaded { get; private set; }

    public async Task LoadAsync(string addressableKey = "girl.csv", CancellationToken ct = default)
    {
        dataList.Clear();
        dataByLevel.Clear();
        IsLoaded = false;

        AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(addressableKey);
        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
        {
            Debug.LogError($"[GirlDataManager] Addressables Load 실패: {addressableKey}");
            return;
        }

        try
        {
            string text = handle.Result.text ?? string.Empty;
            char delimiter = text.IndexOf('\t') >= 0 ? '\t' : ',';

            using var reader = new DelimitedReader(text, delimiter);
            if (!reader.ReadRow(out var header))
            {
                Debug.LogError("[GirlDataManager] 헤더가 비었습니다.");
                return;
            }

            int idx_id     = FindCol(header, "id");
            int idx_name   = FindCol(header, "name");
            int idx_level  = FindCol(header, "level");
            int idx_income = FindCol(header, "incomepersec", "income", "income_per_sec");
            int idx_merge  = FindCol(header, "mergecount", "merge_count");
            int idx_sprite = FindCol(header, "spritename", "sprite", "sprite_name");
            int idx_unlock = FindCol(header, "unlockdesc", "desc", "unlock_desc");

            if (idx_id < 0 || idx_name < 0 || idx_level < 0 || idx_income < 0 || idx_merge < 0 || idx_sprite < 0 || idx_unlock < 0)
            {
                Debug.LogError("[GirlDataManager] 필수 컬럼 누락(id,name,level,incomePerSec,mergeCount,spriteName,unlockDesc).");
                return;
            }

            int row = 2;
            while (reader.ReadRow(out var cols))
            {
                ct.ThrowIfCancellationRequested();

                // 빈 행 스킵
                bool allEmpty = true;
                for (int i = 0; i < cols.Count; i++)
                    if (!string.IsNullOrWhiteSpace(cols[i])) { allEmpty = false; break; }
                if (allEmpty) { row++; continue; }

                string Get(int i) => (i >= 0 && i < cols.Count) ? cols[i] : string.Empty;

                if (!int.TryParse(Get(idx_id), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                { Warn(row, "id parse 실패"); row++; continue; }

                string name = Get(idx_name)?.Trim() ?? string.Empty;

                if (!int.TryParse(Get(idx_level), NumberStyles.Integer, CultureInfo.InvariantCulture, out int level))
                { Warn(row, "level parse 실패"); row++; continue; }

                if (!double.TryParse(Get(idx_income), NumberStyles.Float, CultureInfo.InvariantCulture, out double income))
                { Warn(row, "incomePerSec parse 실패"); row++; continue; }

                if (!int.TryParse(Get(idx_merge), NumberStyles.Integer, CultureInfo.InvariantCulture, out int mergeCount))
                { Warn(row, "mergeCount parse 실패"); row++; continue; }

                string spriteName = Get(idx_sprite)?.Trim();
                string unlockDesc = Get(idx_unlock) ?? string.Empty;

                if (level < 1 || level > TierRules.MaxLevel) { Warn(row, $"level 범위(1~{TierRules.MaxLevel}) 초과: {level}"); row++; continue; }
                if (income < 0) { Warn(row, $"incomePerSec 음수: {income}"); row++; continue; }
                if (mergeCount <= 0) { Warn(row, $"mergeCount 비정상: {mergeCount}"); row++; continue; }

                if (dataByLevel.ContainsKey(level))
                { Debug.LogError($"[GirlDataManager] 중복 level {level} (row {row}) — 스킵"); row++; continue; }

                var rec = new GirlData
                {
                    id = id,
                    name = name,
                    level = level,
                    incomePerSec = income,
                    mergeCount = mergeCount,
                    spriteName = string.IsNullOrEmpty(spriteName) ? level.ToString(CultureInfo.InvariantCulture) : spriteName,
                    unlockDesc = unlockDesc
                };

                dataList.Add(rec);
                dataByLevel[level] = rec;
                row++;
            }

            dataList.Sort((a, b) => a.level.CompareTo(b.level));
            IsLoaded = true;
            Debug.Log($"[GirlDataManager] Load 완료: {dataList.Count}개 (key={addressableKey}, delim='{(delimiter=='\t'?"\\t":",")}')");
        }
        finally
        {
            Addressables.Release(handle);
        }
    }

    public GirlData GetDataByLevel(int level)
    {
        dataByLevel.TryGetValue(level, out var data);
        return data;
    }

    public IReadOnlyList<GirlData> All => dataList;

    static void Warn(int row, string msg) => Debug.LogWarning($"[GirlDataManager] row {row}: {msg}");

    static int FindCol(IReadOnlyList<string> header, params string[] names)
    {
        for (int i = 0; i < header.Count; i++)
        {
            var h = NormalizeHeader(header[i]);
            for (int j = 0; j < names.Length; j++)
                if (h == NormalizeHeader(names[j])) return i;
        }
        return -1;
    }

    static string NormalizeHeader(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = char.ToLowerInvariant(s[i]);
            if (c != ' ' && c != '_') sb.Append(c);
        }
        return sb.ToString();
    }
}

internal sealed class DelimitedReader : System.IDisposable
{
    private readonly string _text;
    private readonly char _delim;
    private int _pos;
    private readonly int _len;

    public DelimitedReader(string text, char delimiter)
    {
        _text = text ?? string.Empty;
        _delim = delimiter;
        _pos = 0;
        _len = _text.Length;
        if (_len >= 1 && _text[0] == '\uFEFF') _pos = 1; // BOM
    }

    public bool ReadRow(out List<string> cols)
    {
        cols = null;
        if (_pos >= _len) return false;

        cols = new List<string>(8);
        var sb = new StringBuilder(64);
        bool inQuotes = false;

        while (_pos < _len)
        {
            char c = _text[_pos++];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (_pos < _len && _text[_pos] == '"') { sb.Append('"'); _pos++; }
                    else inQuotes = false;
                }
                else sb.Append(c);
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == _delim) { cols.Add(sb.ToString()); sb.Length = 0; }
                else if (c == '\r' || c == '\n')
                {
                    if (c == '\r' && _pos < _len && _text[_pos] == '\n') _pos++;
                    cols.Add(sb.ToString());
                    return true;
                }
                else sb.Append(c);
            }
        }
        cols.Add(sb.ToString());
        return true;
    }

    public void Dispose() { }
}
