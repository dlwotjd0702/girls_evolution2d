using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using UnityEngine;

public class GirlEvolutionDB : MonoBehaviour
{
    public string tsvFileName = "girls.tsv"; // 파일명만 입력

    public List<GirlEvolutionData> dataList = new List<GirlEvolutionData>();
    
    public Dictionary<int, GirlEvolutionData> dataByLevel = new Dictionary<int, GirlEvolutionData>();

    void Awake()
    {
        string path = Path.Combine(Application.streamingAssetsPath, tsvFileName);
        LoadFromTSVWithCsvHelper(path);
    }
    
    

    public void LoadFromTSVWithCsvHelper(string path)
    {
        dataList.Clear();
        dataByLevel.Clear();

        using (var reader = new StreamReader(path))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
               {
                   Delimiter = "\t",
                   HasHeaderRecord = true,
                   IgnoreBlankLines = true,
                   BadDataFound = null
               }))
        {
            foreach (var record in csv.GetRecords<GirlEvolutionData>())
            {
                dataList.Add(record);
                dataByLevel[record.level] = record;
            }
        }
        Debug.Log($"[GirlEvolutionDB] TSV Loaded: {dataList.Count}개");
    }

    public GirlEvolutionData GetDataByLevel(int level)
    {
        dataByLevel.TryGetValue(level, out var data);
        return data;
    }
}