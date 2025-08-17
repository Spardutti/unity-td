using UnityEngine;
using System.Collections;

public class PlayerBase : MonoBehaviour
{
    [Header("Visual Effects")]
    [SerializeField] private bool enableFlashEffect = true;
    [SerializeField] private float flashDuration = 0.5f;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashIntensity = 2f;

    [Header("Audio")]
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private float soundVolume = 0.7f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    private Renderer baseRenderer;
    private AudioSource audioSource;
    private Material baseMaterial;
    private Color originalColor;
    private bool isFlashing = false;

    void Awake()
    {
        // get component references
        baseRenderer = GetComponentInChildren<Renderer>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializeVisuals();
    }

    private void InitializeVisuals()
    {
        if (baseRenderer != null)
        {
            baseMaterial = baseRenderer.material;
            originalColor = baseMaterial.color;

            if (showDebugLog)
            {
                Debug.Log($"PlayerBase: Initialized with color {originalColor}");
            }
        }
        else
        {
            Debug.LogError("PlayerBase: Base renderer not found");
        }
    }

    void OnTriggerEnter(Collider other)
    {

        // Check if an enemy reached the base
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            Debug.Log($"PlayerBase: Confirmed this is an Enemy component!");
            HandleEnemyReachedBase(enemy);
        }
        else
        {
            Debug.Log($"PlayerBase: Object {other.name} does not have Enemy component");
        }
    }

    private void HandleEnemyReachedBase(Enemy enemy)
    {
        if (showDebugLog)
        {
            Debug.Log($"PlayerBase: Enemy reached base, taking {enemy.GoldReward} gold");
        }

        // get damange from enemy
        int enemyDamage = enemy.AttackDamage;

        // deal damage to the palyer using existing player heatl
        if (PlayerHealthManager.instance != null)
        {
            PlayerHealthManager.instance.TakeDamage(enemyDamage);

            if (showDebugLog)
            {
                Debug.Log($"PlayerBase: Player took {enemyDamage} damage");
            }
        }
        else
        {
            Debug.LogError("PlayerBase: PlayerHealthManager not found");
        }

        PlayDamageEffects();

        // Destroy the enemy or pool
        Destroy(enemy.gameObject);
    }

    private void PlayDamageEffects()
    {
        if (enableFlashEffect && !isFlashing)
        {
            StartCoroutine(FlashEffect());
        }

        // play damage sound
        PlayDamageSound();
    }

    private IEnumerator FlashEffect()
    {
        if (baseMaterial == null) yield break;

        isFlashing = true;

        // flash to damage color
        baseMaterial.color = flashColor * flashIntensity;

        if (showDebugLog)
        {
            Debug.Log($"PlayerBase: Flashing to {flashColor * flashIntensity}");
        }

        // wait for flash duration
        yield return new WaitForSeconds(flashDuration);

        // reset to original color
        baseMaterial.color = originalColor;

        isFlashing = false;
    }

    private void PlayDamageSound()
    {
        if (damageSound != null && audioSource != null)
        {
            audioSource.clip = damageSound;
            audioSource.volume = soundVolume;
            audioSource.Play();

            if (showDebugLog)
            {
                Debug.Log($"PlayerBase: Playing damage sound");
            }
        }
    }

    // Public method for external damage (if needed)
    public void TakeDamage(int damage)
    {
        if (PlayerHealthManager.instance != null)
        {
            PlayerHealthManager.instance.TakeDamage(damage);
            PlayDamageEffects();
        }
    }

    // Method for testing in editor
    [ContextMenu("Test Base Damage")]
    private void TestBaseDamage()
    {
        TakeDamage(1); // Test with 1 damage
    }

    void OnDrawGizmos()
    {
        // Draw trigger area in scene view
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(2.2f, 1f, 2.2f));
    }
}


