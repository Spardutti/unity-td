using UnityEngine;

public class PlayerHealthManager : MonoBehaviour
{
    [Header("Player Health Settings")]
    [SerializeField] private int startingHealth = 10;
    [SerializeField] private int maxHealth = 10;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    private int currentHealth;

    // Events for UI updates
    public static event System.Action<int> OnHealthChanged;
    public static event System.Action<int, int> OnHealthLost;
    public static event System.Action OnPlayerDied;

    // Singleton pattern
    public static PlayerHealthManager instance { get; private set; }

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float HealthPercentage => (float)currentHealth / maxHealth;
    public bool IsAlive => currentHealth > 0;

    void Awake()
    {
        // Singleton setup
        if (instance == null)
        {
            instance = this;
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
        InitializeHealth();
    }

    private void InitializeHealth()
    {
        currentHealth = startingHealth;

        if (showDebugLog)
        {
            Debug.Log($"PlayerHealthManager: Initialized with {currentHealth} health.");
        }

        OnHealthChanged?.Invoke(currentHealth);
    }

    public void TakeDamage(int damage)
    {
        if (!IsAlive)
        {
            Debug.LogWarning($"PlayerHealthManager: Trying to take damage, but player is dead");
            return;
        }

        if (damage < 0)
        {
            Debug.LogWarning($"PlayerHealthManager: Trying to take damage, but damage cannot be negative");
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        if (showDebugLog)
        {
            Debug.Log($"PlayerHealthManager: Taken {damage} damage, now at {currentHealth}/{maxHealth}");
        }

        OnHealthChanged?.Invoke(currentHealth);
        OnHealthLost?.Invoke(damage, currentHealth);

        if (!IsAlive)
        {
            HandlePlayerDeath();
        }

    }

    private void HandlePlayerDeath()
    {
        if (showDebugLog)
        {
            Debug.Log($"PlayerHealthManager: Player died!");
        }

        OnPlayerDied?.Invoke();
    }


    public void RestoreHealth(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"PlayerHealthManager: Restoring {amount} health, but amount cannot be negative");
            return;
        }

        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);

        if (showDebugLog)
        {
            Debug.Log($"PlayerHealthManager: Restored {amount} health, now at {currentHealth}/{maxHealth}");
        }

        OnHealthChanged?.Invoke(currentHealth);

    }

    public void SetHealth(int newHealth)
    {

        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);

        if (showDebugLog)
        {
            Debug.Log($"PlayerHealthManager: Set health to {currentHealth}/{maxHealth}");
        }

        if (!IsAlive)
        {
            HandlePlayerDeath();
        }
    }

    // Context menu methods for testing in editor
    [ContextMenu("Take 1 Damage")]
    private void TakeTestDamage()
    {
        TakeDamage(1);
    }

    [ContextMenu("Restore 1 Health")]
    private void RestoreTestHealth()
    {
        RestoreHealth(1);
    }

    [ContextMenu("Kill Player")]
    private void KillPlayer()
    {
        SetHealth(0);
    }

    [ContextMenu("Full Heal")]
    private void FullHeal()
    {
        SetHealth(maxHealth);
    }
}
