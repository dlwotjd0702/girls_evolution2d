using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour, ISaveable
{
    public double gold { get; private set; }
    public Action<double> onGoldChanged;

    public void AddGold(double amount)
    {
        gold += amount;
        onGoldChanged?.Invoke(gold);
    }

    public bool SpendGold(double amount)
    {
        if (gold >= amount)
        {
            gold -= amount;
            onGoldChanged?.Invoke(gold);
            return true;
        }
        return false;
    }

    public double GetGold() => gold;

    public void SetGold(double value)
    {
        gold = value;
        onGoldChanged?.Invoke(gold);
    }

    // --------- ISaveable 구현 ---------
    public void ApplyLoadedData(SaveData data)
    {
        if (data == null) return;
        gold = data.gold;
        onGoldChanged?.Invoke(gold);
        Debug.Log($"[CurrencyManager] 골드 복원 완료: {gold}");
    }

    public void CollectSaveData(SaveData data)
    {
        data.gold = gold;
        Debug.Log($"[CurrencyManager] 골드 저장: {gold}");
    }
}