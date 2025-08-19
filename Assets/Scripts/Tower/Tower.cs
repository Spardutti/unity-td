using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class Tower : MonoBehaviour
{

    [Header("Tower Configuration")]
    [SerializeField] private TowerData towerData;

    [Header("Component References")]
    [SerializeField] private Transform projectilesSpawnPoint;

    [Header("Targeting")]
    [SerializeField] private TargetingMode targetingMode = TargetingMode.Closest;

    [Header("Range Visualization")]
    [SerializeField] private bool showRangeIndicator = true;
    [SerializeField] private bool showRangeWhenSelected = true;

    [Header("Debug Settings")]
    [SerializeField] private bool debugProjectiles = false;
    [SerializeField] private bool playFireSound = true;
    [SerializeField] private float soundVolume = 1f;

    private float lastAttackTime;
    private Enemy currentTarget;
    private GridManager gridManager;
    private Vector2Int gridPosition;
    private Renderer towerRenderer;
    private List<Enemy> enemiesInRange = new List<Enemy>();

    private AudioSource audioSource;

    public float AttackRange => towerData?.range ?? 3f;
    public float AttackDamage => towerData?.damage ?? 25f;
    public float AttackSpeed => towerData?.fireRate ?? 1f;
    public int TowerCost => towerData?.cost ?? 50;
    public TowerData TowerData => towerData;
    public AttackType AttackType => towerData?.attackType ?? AttackType.Single;
    public float SplashRadius => towerData?.splashRadius ?? 0f;

    // Data-driven properties
    public GameObject ProjectilePrefab => towerData != null ? towerData.projectilePrefab : null;
    public GameObject ExplosionEffectPrefab => towerData != null ? towerData.ExplosionEffectPrefab : null;
    public AudioClip FireSound => towerData != null ? towerData.fireSound : null;
    public AudioClip ExplosionSound => towerData != null ? towerData.ExplosionSound : null;
    public GameObject MuzzleFlashEffect => towerData != null ? towerData.muzzleFlashEffect : null;

    void Awake()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        towerRenderer = GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();

        if (towerRenderer == null)
        {
            towerRenderer = GetComponent<Renderer>();
        }

        if (audioSource == null)
        {
            Debug.LogWarning($"Tower {name} has no audio source");
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializeTowerData();
        SnapToGrid();
        RegisterWithGrid();
    }

    // Update is called once per frame
    void Update()
    {
        FindEnemiesInRange();
        SelectTarget();

        if (CanAttack())
        {
            AttackTarget();
        }
    }

    private void InitializeTowerData()
    {
        if (towerData != null)
        {
            // Set targeting mode from data
            targetingMode = towerData.defaultTargetingMode;
        }
    }

    private void SnapToGrid()
    {
        gridPosition = GridUtility.GetGridPosition(transform, gridManager);
        GridUtility.SnapToGrid(transform, gridManager, transform.localScale.y * 0.5f);
    }

    private void RegisterWithGrid()
    {
        if (gridManager != null)
        {
            bool success = gridManager.TryOccupyCell(gridPosition);

        }
    }

    private void FindEnemiesInRange()
    {
        enemiesInRange.Clear();
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        foreach (Enemy enemy in allEnemies)
        {
            if (enemy != null && enemy.IsAlive)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance <= AttackRange)
                {
                    enemiesInRange.Add(enemy);
                }
            }
        }
    }

    private void SelectTarget()
    {
        // clear target if its dead or out of range
        if (currentTarget != null && (!currentTarget.IsAlive || !enemiesInRange.Contains(currentTarget)))
        {
            currentTarget = null;
        }

        // Find new target if we dont have one
        if (currentTarget == null && enemiesInRange.Count > 0)
        {
            currentTarget = GetBestTarget();
        }
    }

    private Enemy GetBestTarget()
    {
        if (enemiesInRange.Count == 0) return null;

        Enemy bestTarget = null;
        float bestValue = float.MaxValue;

        foreach (Enemy enemy in enemiesInRange)
        {
            float value = GetTargetValue(enemy);
            if (value < bestValue)
            {
                bestValue = value;
                bestTarget = enemy;
            }
        }
        return bestTarget;
    }

    private float GetTargetValue(Enemy enemy)
    {
        return targetingMode switch
        {
            TargetingMode.Closest => Vector3.Distance(transform.position, enemy.transform.position),
            TargetingMode.Furthest => -Vector3.Distance(transform.position, enemy.transform.position),
            TargetingMode.LowestHealth => enemy.CurrentHealth,
            TargetingMode.HighestHealth => -enemy.CurrentHealth,
            TargetingMode.MostProgress => -enemy.GetPathProgress(),
            TargetingMode.LeastProgress => enemy.GetPathProgress(),
            _ => Vector3.Distance(transform.position, enemy.transform.position)
        };
    }

    private bool CanAttack()
    {
        return currentTarget != null && currentTarget.IsAlive && Time.time >= lastAttackTime + (1f / AttackSpeed);
    }

    private void AttackTarget()
    {
        if (currentTarget == null) return;

        // Face the target (only rotate on Y axis to prevent tilting)
        Vector3 direction = currentTarget.transform.position - transform.position;
        direction.y = 0; // Keep only horizontal direction
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
        }


        FireProjectile();

        lastAttackTime = Time.time;


        // Visual Feedback
        StartCoroutine(AttackFlash());
    }

    private void FireProjectile()
    {
        if (ProjectilePrefab == null)
        {
            Debug.LogWarning($"Tower {name} has no projectile prefab assigned WONT ATTACK");
            return;
        }
        if (projectilesSpawnPoint == null)
        {
            Debug.LogWarning($"Tower {name} has no projectiles spawn point assigned");
            return;
        }

        if (currentTarget == null) return;

        Debug.Log("AttackType: " + AttackType);
        if (AttackType == AttackType.Area)
        {
            ApplyAreaDamage(currentTarget.transform.position);
        }
        else
        {

            // Instant damage
            currentTarget.TakeDamage(AttackDamage);
        }

        PlayFireSound();

        // Visual projectile
        Vector3 spawnPosition = projectilesSpawnPoint.position;
        Vector3 targetPosition = currentTarget.transform.position;

        GameObject newProjectile = Instantiate(ProjectilePrefab, spawnPosition, Quaternion.identity);
        Projectile projectileScript = newProjectile.GetComponent<Projectile>();

        if (projectileScript != null)
        {
            projectileScript.FireToPosition(targetPosition);

            if (debugProjectiles)
            {
                Debug.Log($"Tower {name} fired projectile at target ({currentTarget.name})");
            }
        }
        else
        {
            Debug.LogWarning($"Tower {name} could not find projectile script");
            Destroy(newProjectile);
        }
    }

    private void PlayFireSound()
    {


        if (!playFireSound)
        {
            Debug.LogWarning($"Tower {name}: Fire sound is disabled in settings");
            return;
        }

        if (FireSound == null)
        {
            Debug.LogWarning($"Tower {name}: No fire sound clip assigned");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogWarning($"Tower {name}: No AudioSource component found");
            return;
        }

        audioSource.clip = FireSound;
        audioSource.volume = soundVolume;
        audioSource.Play();
    }

    private System.Collections.IEnumerator AttackFlash()
    {
        if (towerRenderer == null)
        {
            Debug.LogWarning($"Tower {name}: No tower renderer found");
            yield break;
        }
        {
            Color originalColor = towerRenderer.material.color;
            towerRenderer.material.color = Color.white;

            yield return new WaitForSeconds(0.1f);

            towerRenderer.material.color = originalColor;
        }
    }

    private void ApplyAreaDamage(Vector3 explosionCenter)
    {
        Debug.Log("Area damage");
        if (SplashRadius <= 0f)
        {
            Debug.LogWarning($"Tower {name}: No splash radius specified for area damage");
            return;
        }

        // Find all enemies within splash radius
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        List<Enemy> affectedEnemies = new List<Enemy>();

        foreach (Enemy enemy in allEnemies)
        {
            if (enemy != null && enemy.IsAlive)
            {
                float distance = Vector3.Distance(explosionCenter, enemy.transform.position);
                if (distance <= SplashRadius)
                {
                    affectedEnemies.Add(enemy);

                    // Damage falloff based on distance
                    float damageMultiplier = 1f - (distance / SplashRadius) * 0.5f; // 50% minimum damage at edge
                    float actualDamage = AttackDamage * damageMultiplier;

                    enemy.TakeDamage(actualDamage);
                }
            }
        }

        ShowAreaDamageEffect(explosionCenter);
    }

    private void ShowAreaDamageEffect(Vector3 center)
    {
        if (ExplosionEffectPrefab == null)
        {
            Debug.LogWarning($"Tower {name}: No explosion effect prefab assigned");
            return;

        }

        // instantiate explosion effect
        GameObject explosion = Instantiate(ExplosionEffectPrefab, center, Quaternion.identity);

        // Scale effect based on splash radius
        explosion.transform.localScale = Vector3.one * (SplashRadius / 2f);

        // Auto destroy after particle system duration
        ParticleSystem particles = explosion.GetComponent<ParticleSystem>();
        if (particles != null)
        {
            float duration = particles.main.duration + particles.main.startLifetime.constantMax;
            Destroy(explosion, duration);
        }
        else
        {
            Destroy(explosion, 2f);
        }

        PlayExplosionSound(explosion);
    }

    private void PlayExplosionSound(GameObject explosionObject)
    {
        if (ExplosionSound == null) return;

        AudioSource explosionAudio = explosionObject.GetComponent<AudioSource>();
        if (explosionAudio != null)
        {
            explosionAudio.clip = ExplosionSound;
            explosionAudio.Play();
        }

    }

    public bool CanUpgrade()
    {
        return towerData != null && towerData.CanBeUpgraded;
    }

    public bool UpgradeTower()
    {
        if (!CanUpgrade()) return false;

        TowerData newData = towerData.nextUpgrade;
        if (newData == null) return false;

        towerData = newData;

        // Update visuals if needed
        UpdateTowerVisuals();

        return true;
    }

    private void UpdateTowerVisuals()
    {
        if (towerData == null) return;

        // Update scale if specified
        if (towerData.levelScales != null && towerData.upgradeLevel <= towerData.levelScales.Length)
        {
            float scale = towerData.levelScales[towerData.upgradeLevel - 1];
            transform.localScale = transform.localScale * scale;
        }

        // Update material if specified
        if (towerData.levelMaterials != null && towerData.upgradeLevel <= towerData.levelMaterials.Length)
        {
            Material newMaterial = towerData.levelMaterials[towerData.upgradeLevel - 1];
            if (towerRenderer != null && newMaterial != null)
            {
                towerRenderer.material = newMaterial;
            }
        }
    }

    // Method to get tower info for UI
    public string GetTowerInfo()
    {
        return $"Damage: {AttackDamage:F1}, Range: {AttackRange:F1}, Speed: {AttackSpeed:F1}";
    }

    void OnDestroy()
    {
        if (gridManager != null)
        {
            gridManager.FreeCell(gridPosition);
        }
    }
}

public enum TargetingMode
{
    Closest,
    Furthest,
    LowestHealth,
    HighestHealth,
    MostProgress,
    LeastProgress
}
