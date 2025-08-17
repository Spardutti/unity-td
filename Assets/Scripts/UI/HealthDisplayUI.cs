using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthDisplayUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image healthIcon;

    [Header("Display Settings")]
    [SerializeField] private string healthFormat = "HP: {0}/{1}";
    [SerializeField] private bool showOnlyCurrentHealth = false;

    [Header("Animation Settings")]
    [SerializeField] private bool animateOnDamage = true;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private float damageFlashScale = 1.2f;

    [Header("Health Warning")]
    [SerializeField] private bool enableLowHealthWarning = true;
    [SerializeField] private int lowHealthThreshold = 3;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private Color normalHealthColor = Color.white;

    private Color originalTextColor;
    private Vector3 originalScale;
    private Coroutine currentAnimationCoroutine;

    void Awake()
    {
        // auto find components if not assigned
        if (healthText == null)
        {
            healthText = GetComponentInChildren<TextMeshProUGUI>();
        }
        if (healthIcon == null)
        {
            healthIcon = GetComponentInChildren<Image>();
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Store original values for animations
        if (healthText != null)
        {
            originalTextColor = healthText.color;
            originalScale = healthText.transform.localScale;
        }

        // Subscribe to PlayerHealthManager events
        PlayerHealthManager.OnHealthChanged += UpdateHealthDisplay;
        PlayerHealthManager.OnHealthLost += OnHealthLost;

        // Initialize display with current health
        if (PlayerHealthManager.instance != null)
        {
            UpdateHealthDisplay(PlayerHealthManager.instance.CurrentHealth);
        }
        else
        {
            UpdateHealthDisplay(0);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from PlayerHealthManager events
        PlayerHealthManager.OnHealthChanged -= UpdateHealthDisplay;
        PlayerHealthManager.OnHealthLost -= OnHealthLost;

    }

    private void UpdateHealthDisplay(int currentHealth)
    {
        if (healthText == null) return;

        // update text based on format preference
        if (showOnlyCurrentHealth)
        {
            healthText.text = currentHealth.ToString();
        }
        else
        {
            int manxHealth = PlayerHealthManager.instance?.MaxHealth ?? 10;
            healthText.text = string.Format(healthFormat, currentHealth, manxHealth);
        }

        // Update color based on health level
        UpdateHealthColor(currentHealth);
    }

    private void UpdateHealthColor(int currentHealth)
    {
        if (!enableLowHealthWarning || healthText == null) return;

        if (currentHealth <= lowHealthThreshold && currentHealth > 0)
        {
            healthText.color = lowHealthColor;
        }
        else
        {
            healthText.color = normalHealthColor;
        }
    }

    private void OnHealthLost(int damage, int currentHealth)
    {
        if (animateOnDamage)
        {
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);

                if (healthText != null)
                {
                    healthText.transform.localScale = originalScale;
                }
            }
            currentAnimationCoroutine = StartCoroutine(PlayDamageAnimation());
        }
    }

    private System.Collections.IEnumerator PlayDamageAnimation()
    {
        if (healthText == null) yield break;

        // Store original values
        Color originalColor = healthText.color;
        Vector3 originalScale = healthText.transform.localScale;

        // flash and scale up
        healthText.color = damageFlashColor;
        healthText.transform.localScale = originalScale * damageFlashScale;

        // wait for animation duration
        yield return new WaitForSeconds(animationDuration);

        // reset to original values
        healthText.color = originalColor;
        healthText.transform.localScale = originalScale;

        // update color based on current health
        if (PlayerHealthManager.instance != null)
        {
            UpdateHealthColor(PlayerHealthManager.instance.CurrentHealth);
        }
    }

    // Method for testing damage animation
    [ContextMenu("Test Damage Animation")]
    private void TestDamageAnimation()
    {
        OnHealthLost(1, 5);
    }
}
