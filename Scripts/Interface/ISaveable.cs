public interface ISaveable
{
    void ApplyLoadedData(SaveData data);
    void CollectSaveData(SaveData data);
}