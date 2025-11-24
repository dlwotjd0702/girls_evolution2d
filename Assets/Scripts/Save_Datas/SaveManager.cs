using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.IO;
// using BackEnd; // 실제 프로젝트에 뒤끝 SDK 네임스페이스 추가

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    public static SaveData pendingSaveData = null;

    private const string SAVE_FILE_NAME = "save.json";
    private const string BACKUP_FILE_NAME = "save_backup.json";

    private string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
    private string BackupFilePath => Path.Combine(Application.persistentDataPath, BACKUP_FILE_NAME);

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

        string saveJson = JsonUtility.ToJson(data, true);

        try
        {
            // 백업 파일이 있으면 백업으로 복사
            if (File.Exists(SaveFilePath))
            {
                File.Copy(SaveFilePath, BackupFilePath, true);
            }

            // 새 세이브 파일 저장
            File.WriteAllText(SaveFilePath, saveJson);

            // PlayerPrefs도 병행 저장 (호환성 유지)
            PlayerPrefs.SetString("SaveData", saveJson);
            PlayerPrefs.Save();

            Debug.Log($"[SaveManager] 저장 완료: {SaveFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 저장 실패: {e.Message}");
        }
    }

    // 게임 데이터 불러오기
    public void LoadGame()
    {
        SaveData data = null;
        string saveJson = null;

        try
        {
            // 1순위: 로컬 파일에서 불러오기
            if (File.Exists(SaveFilePath))
            {
                saveJson = File.ReadAllText(SaveFilePath);
                data = JsonUtility.FromJson<SaveData>(saveJson);
                Debug.Log($"[SaveManager] 로컬 파일에서 불러오기: {SaveFilePath}");
            }
            // 2순위: PlayerPrefs에서 불러오기 (기존 호환성)
            else if (PlayerPrefs.HasKey("SaveData"))
            {
                saveJson = PlayerPrefs.GetString("SaveData");
                data = JsonUtility.FromJson<SaveData>(saveJson);
                Debug.Log("[SaveManager] PlayerPrefs에서 불러오기");
            }
            else
            {
                Debug.Log("[SaveManager] 저장 파일이 없습니다. 새 게임 시작.");
                return;
            }

            if (data != null)
            {
                foreach (var s in FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>())
                    s.ApplyLoadedData(data);

                Debug.Log("[SaveManager] 불러오기 완료");
                // 데이터 적용은 비동기이므로, GirlFieldManager에서 완료 알림을 받음
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 불러오기 실패: {e.Message}");
            
            // 백업 파일 시도
            try
            {
                if (File.Exists(BackupFilePath))
                {
                    saveJson = File.ReadAllText(BackupFilePath);
                    data = JsonUtility.FromJson<SaveData>(saveJson);
                    
                    foreach (var s in FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>())
                        s.ApplyLoadedData(data);
                    
                    Debug.Log("[SaveManager] 백업 파일에서 복구 완료");
                }
            }
            catch (System.Exception e2)
            {
                Debug.LogError($"[SaveManager] 백업 복구 실패: {e2.Message}");
            }
        }
    }
    

    // 세이브 파일 삭제 (테스트용)
    public bool DeleteSaveFile()
    {
        try
        {
            bool deleted = false;
            
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                deleted = true;
            }
            
            if (File.Exists(BackupFilePath))
            {
                File.Delete(BackupFilePath);
            }

            // PlayerPrefs도 삭제
            if (PlayerPrefs.HasKey("SaveData"))
            {
                PlayerPrefs.DeleteKey("SaveData");
                PlayerPrefs.Save();
            }

            Debug.Log("[SaveManager] 세이브 파일 삭제 완료");
            return deleted;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 세이브 파일 삭제 실패: {e.Message}");
            return false;
        }
    }

    // 세이브 파일 리셋 (게임 데이터 초기화)
    public void ResetSaveFile()
    {
        DeleteSaveFile();
        Debug.Log("[SaveManager] 세이브 파일 리셋 완료. 다음 저장 시 새 게임으로 시작됩니다.");
    }

    // 세이브 파일 존재 여부 확인
    public bool HasSaveFile()
    {
        return File.Exists(SaveFilePath) || PlayerPrefs.HasKey("SaveData");
    }

    // 세이브 파일 경로 반환 (디버그용)
    public string GetSaveFilePath()
    {
        return SaveFilePath;
    }
}