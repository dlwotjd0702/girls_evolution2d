using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class GirlDataManager
{
    private List<GirlData> dataList = new List<GirlData>();
    private Dictionary<int, GirlData> dataByLevel = new Dictionary<int, GirlData>();
    public bool IsLoaded { get; private set; } = false;

    public async Task LoadAsync(string addressableKey = "girl.csv")
    {
        dataList.Clear();
        dataByLevel.Clear();
        IsLoaded = false;

        var handle = Addressables.LoadAssetAsync<TextAsset>(addressableKey);
        await handle.Task;

        if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
        {
            string csvText = handle.Result.text;
            using (var reader = new StringReader(csvText))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                   {
                       Delimiter = ",",
                       HasHeaderRecord = true,
                       IgnoreBlankLines = true,
                       BadDataFound = null
                   }))
            {
                foreach (var record in csv.GetRecords<GirlData>())
                {
                    dataList.Add(record);
                    dataByLevel[record.level] = record;
                }
            }
            dataList = dataList.OrderBy(d => d.level).ToList();
            IsLoaded = true;
            Debug.Log($"[GirlDataManager] Addressables Load 완료: {dataList.Count}개");
        }
        else
        {
            Debug.LogError("[GirlDataManager] Addressables Load 실패!");
        }
    }

    public GirlData GetDataByLevel(int level)
    {
        dataByLevel.TryGetValue(level, out var data);
        return data;
    }
    public IReadOnlyList<GirlData> All => dataList;
}