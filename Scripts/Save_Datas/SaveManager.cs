using UnityEngine;
using System.Linq;
using System.Collections.Generic;
// using BackEnd; // 실제 프로젝트에 뒤끝 SDK 네임스페이스 추가

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    public static SaveData pendingSaveData = null;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        LoadGame();
    }

    void OnApplicationPause(bool pause)
    {
        if (pause) SaveGame();
    }

    void OnApplicationQuit()
    {
        SaveGame();
    }

    // 게임 데이터 저장
    public void SaveGame()
    {
        SaveData data = new SaveData();
        data.SetSaveTime();
        foreach (var s in FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>())
            s.CollectSaveData(data);

        // **뒤끝 연동 예시**
        string saveJson = JsonUtility.ToJson(data);
        // Backend.GameData.Insert("UserSaveTable", new Param { { "SaveData", saveJson } }, callback);

        // 임시: 오프라인 저장도 병행 (테스트/로컬백업)
        PlayerPrefs.SetString("SaveData", saveJson);
        PlayerPrefs.Save();

        Debug.Log("[SaveManager] 저장 완료: " + saveJson);
    }

    // 게임 데이터 불러오기
    public void LoadGame()
    {
        // **뒤끝 연동 예시**
        // Backend.GameData.Get("UserSaveTable", new Where(), callback);
        // (콜백에서 saveJson 추출 후 아래와 같이 파싱)

        // 임시: 오프라인 PlayerPrefs에서 불러오기
        if (!PlayerPrefs.HasKey("SaveData")) return;
        string saveJson = PlayerPrefs.GetString("SaveData");
        SaveData data = JsonUtility.FromJson<SaveData>(saveJson);

        foreach (var s in FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>())
            s.ApplyLoadedData(data);

        Debug.Log("[SaveManager] 불러오기 완료: " + saveJson);
    }
}