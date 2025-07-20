using UnityEngine;
using System;

public class CurrencyManager : MonoBehaviour, ISaveable
{
    [Header("골드")]
    [SerializeField] private double gold = 0;
    public event Action<double> onGoldChanged;

    public double GetGold() => gold;

    public void AddGold(double amount)
    {
        gold += amount;
        if (gold < 0) gold = 0;
        onGoldChanged?.Invoke(gold);
    }

    public bool SpendGold(double amount)
    {
        if (gold < amount) return false;
        gold -= amount;
        onGoldChanged?.Invoke(gold);
        return true;
    }

    public void SetGold(double value)
    {
        gold = value < 0 ? 0 : value;
        onGoldChanged?.Invoke(gold);
    }

    // --------- ISaveable 구현 ---------
    public void ApplyLoadedData(SaveData data)
    {
        if (data == null) return;
        gold = data.gold;
        onGoldChanged?.Invoke(gold);
    }

    public void CollectSaveData(SaveData data)
    {
        data.gold = gold;
    }
}