using NUnit.Framework.Internal.Commands;
using UnityEngine;
using UnityEngine.Events;

public class EconomyManager : MonoBehaviour
{
    [Header("Starting Resources")]
    [SerializeField] private int startingGOld = 100;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    private int currentGold;

    // Events for UI updates
    public static event System.Action<int> OnGoldChanged;
    public static event System.Action<int, int> OnGoldSpent;
    public static event System.Action<int, int> OnGoldEarned;

    // Singleton pattern
    public static EconomyManager Instance { get; private set; }

    public int CurrentGold => currentGold;
    public bool HasEnoughGold(int cost) => currentGold >= cost;

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializeEconomy();
    }

    private void InitializeEconomy()
    {
        currentGold = startingGOld;

        if (showDebugLog)
        {
            Debug.Log($"EconomyManager: Initialized with {currentGold} gold.");
        }

        OnGoldChanged?.Invoke(currentGold);
    }

    public bool TryToSpendGold(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"EconomyManager: Trying to spend {amount} gold, but amount cannot be negative.");
            return false;
        }
        if (!HasEnoughGold(amount))
        {
            if (showDebugLog)
            {
                Debug.LogWarning($"EconomyManager: Trying to spend {amount} gold, but only has {currentGold} gold.");
            }
            return false;

        }

        currentGold -= amount;
        if (showDebugLog)
        {
            Debug.Log($"EconomyManager: Spent {amount} gold.");
        }

        OnGoldChanged?.Invoke(currentGold);
        OnGoldSpent?.Invoke(amount, currentGold);


        return true;

    }

    public void AddGold(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"EconomyManager: Adding {amount} gold, but amount cannot be negative.");
            return;
        }

        currentGold += amount;
        if (showDebugLog)
        {
            Debug.Log($"EconomyManager: Added {amount} gold.");
        }

        OnGoldChanged?.Invoke(currentGold);
        OnGoldEarned?.Invoke(amount, currentGold);
    }

    // Set gold to a specific amount for testing purposes
    public void SetGold(int amount)
    {
        currentGold = Mathf.Max(0, amount);
        OnGoldChanged?.Invoke(currentGold);
    }

    [ContextMenu("Add 50 Gold")]
    private void AddTestGold()
    {
        AddGold(50);
    }

    [ContextMenu("Spend 10 Gold")]
    private void SpendTestGold()
    {
        TryToSpendGold(10);
    }


}
