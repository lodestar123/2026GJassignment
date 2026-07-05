using System;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    public const int StartingGold = 20;
    public const int GoldPulseReward = 10;

    public int Gold { get; private set; }

    public event Action<int> OnGoldChanged;
    public event Action<int> OnGoldSpent;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Gold = StartingGold;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        Gold += amount;
        OnGoldChanged?.Invoke(Gold);
    }

    public bool TrySpendGold(int amount)
    {
        if (amount <= 0 || Gold < amount)
            return false;

        Gold -= amount;
        OnGoldChanged?.Invoke(Gold);
        OnGoldSpent?.Invoke(amount);
        return true;
    }
}
