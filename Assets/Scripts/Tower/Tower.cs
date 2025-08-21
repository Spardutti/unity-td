using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class Tower : MonoBehaviour
{

    [Header("Tower Configuration")]
    [SerializeField] private TowerData towerData;

    [Header("XP Progression")]
    [SerializeField] private int currentXp = 0;
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private TowerUpgradeChoice[] appliedUpgrades = new TowerUpgradeChoice[10];

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

    [Header("Turret Rotation")]
    [SerializeField] private Transform turretTransform;

    private float lastAttackTime;
    private Enemy currentTarget;

    private bool isAiming = false;
    private Coroutine aimingCoroutine;
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

    public int CurrentXP => currentXp;
    public int CurrentLevel => currentLevel;
    public bool IsReadyToLevelUp => CanLevelUp();
    public int XPToNextLevel => GetXPToNextLevel();

    // Data-driven properties
    public GameObject ProjectilePrefab => towerData != null ? towerData.projectilePrefab : null;
    public GameObject ExplosionEffectPrefab => towerData != null ? towerData.ExplosionEffectPrefab : null;
    public AudioClip FireSound => towerData != null ? towerData.fireSound : null;
    public AudioClip ExplosionSound => towerData != null ? towerData.ExplosionSound : null;
    public GameObject MuzzleFlashEffect => towerData != null ? towerData.muzzleFlashEffect : null;
    public float TurretRotationSpeed => towerData != null ? towerData.turretRotationSpeed : 180f;
    public float AimingThreshold => towerData != null ? towerData.aimingThreshold : 5f;

    void Awake()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        towerRenderer = GetComponentInChildren<Renderer>();
        audioSource = GetComponent<AudioSource>();

        if (towerRenderer == null)
        {
            towerRenderer = GetComponent<Renderer>();
        }

        if (audioSource == null)
        {
            Debug.LogWarning($"Tower {name} has no audio source");
        }

        // Check for collider - required for hover and click detection
        Collider towerCollider = GetComponent<Collider>();
        if (towerCollider == null)
        {
            Debug.LogError($"Tower {name} is missing a Collider component! Hover detection and clicking will not work.");
        }

        turretTransform = transform.Find("Turret");
        if (turretTransform == null)
        {
            Debug.LogWarning($"Tower {name} has no 'Turret' child - turret rotation disabled");
        }

        if (turretTransform != null)
        {
            Transform turretSpawnPoint = turretTransform.Find("ProjectileSpawnPoint");
            if (turretSpawnPoint != null)
            {
                projectilesSpawnPoint = turretSpawnPoint;
            }
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
        float modifiedRange = GetModifiedRange();

        foreach (Enemy enemy in allEnemies)
        {
            if (enemy != null && enemy.IsAlive)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance <= modifiedRange)
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
            if (aimingCoroutine != null)
            {
                StopCoroutine(aimingCoroutine);
                aimingCoroutine = null;
                isAiming = false;
            }
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

        // Start aiming if not already aiming
        if (!isAiming && turretTransform != null)
        {
            if (aimingCoroutine != null)
            {
                StopCoroutine(aimingCoroutine);
            }
            aimingCoroutine = StartCoroutine(PreAimAndFire());
        }
        // fallback for towers without turret
        else if (turretTransform == null)
        {
            {
                FireProjectile();
                lastAttackTime = Time.time;
                StartCoroutine(AttackFlash());
            }
        }
    }

    private System.Collections.IEnumerator PreAimAndFire()
    {
        isAiming = true;
        while (currentTarget != null && currentTarget.IsAlive && enemiesInRange.Contains(currentTarget))
        {
            // Calculate target direction
            Vector3 direction = currentTarget.transform.position - turretTransform.position;
            direction.y = 0; // Keep only horizontal direction

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                float targetY = targetRotation.eulerAngles.y;

                // smoothly rotate turret toward target
                float currentY = turretTransform.eulerAngles.y;
                float newY = Mathf.MoveTowardsAngle(currentY, targetY, TurretRotationSpeed * Time.deltaTime);
                turretTransform.rotation = Quaternion.Euler(0, newY, 0);

                // check if aimed close enough to fire
                float angleDifference = Mathf.Abs(Mathf.DeltaAngle(currentY, targetY));

                if (angleDifference < AimingThreshold && Time.time > lastAttackTime + (1f / AttackSpeed))
                {
                    FireProjectile();
                    lastAttackTime = Time.time;
                    StartCoroutine(AttackFlash());
                }
            }
            yield return null;
        }

        isAiming = false;
        aimingCoroutine = null;
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
            currentTarget.TakeDamage(GetModifiedDamage(), this);
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
                    float actualDamage = GetModifiedDamage() * damageMultiplier;

                    enemy.TakeDamage(actualDamage, this);
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

    public void GainExperience(int xpAmount)
    {
        if (xpAmount <= 0 || currentLevel >= towerData.MaxXPLevel) return;

        currentXp += xpAmount;

        if (debugProjectiles)
        {
            Debug.Log($"Tower {name} gained {xpAmount} xp");
        }

        // Check for multiple level ups
        while (CanLevelUp())
        {
            // dont auto level up just mark ready
            break;
        }
    }

    public bool CanLevelUp()
    {
        if (currentLevel >= towerData.MaxXPLevel) return false;
        int requiredXP = towerData.GetXpRequiredForLevel(currentLevel + 1);
        return currentXp >= requiredXP;
    }

    public int GetXPToNextLevel()
    {
        if (currentLevel >= towerData.MaxXPLevel) return 0;
        int requiredXP = towerData.GetXpRequiredForLevel(currentLevel + 1);
        return Mathf.Max(0, requiredXP - currentXp);
    }

    public void ApplyUpgrade(TowerUpgradeChoice upgrade)
    {
        if (upgrade == null || !CanLevelUp()) return;

        // Store the upgrade
        appliedUpgrades[currentLevel - 1] = upgrade;
        currentLevel++;

        // Apply visual effect if available
        if (upgrade.UpgradeParticleEffect != null)
        {
            Instantiate(upgrade.UpgradeParticleEffect, transform.position, Quaternion.identity);
        }

        if (debugProjectiles)
        {
            Debug.Log($"Tower {name} upgraded to level {currentLevel} applied upgrade {upgrade.upgradeName}");
        }
    }

    public float GetModifiedDamage()
    {
        float damage = AttackDamage;
        for (int i = 0; i < currentLevel - 1; i++)
        {
            if (appliedUpgrades[i] != null)
            {
                damage *= appliedUpgrades[i].DamageMultiplier;
                damage += appliedUpgrades[i].DamageBonus;
            }
        }
        return damage;
    }

    public float GetModifiedRange()
    {
        float range = AttackRange;
        for (int i = 0; i < currentLevel - 1; i++)
        {
            if (appliedUpgrades[i] != null)
            {
                range *= appliedUpgrades[i].RangeMultiplier;
                range += appliedUpgrades[i].RangeBonus;
            }
        }
        return range;
    }

    public float GetModifiedFireRate()
    {
        float fireRate = AttackSpeed;
        for (int i = 0; i < currentLevel - 1; i++)
        {
            if (appliedUpgrades[i] != null)
            {
                fireRate *= appliedUpgrades[i].FireRateMultiplier;
                fireRate += appliedUpgrades[i].FireRateBonus;
            }
        }
        return fireRate;
    }


    // Method to get tower info for UI
    public string GetTowerInfo()
    {
        return $"Damage: {AttackDamage:F1}, Range: {AttackRange:F1}, Speed: {AttackSpeed:F1}";
    }

    void OnDestroy()
    {
        if (aimingCoroutine != null)
        {
            StopCoroutine(aimingCoroutine);
        }
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
