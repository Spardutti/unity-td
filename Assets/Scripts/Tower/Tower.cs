using UnityEngine;
using System.Collections.Generic;

public class Tower : MonoBehaviour
{

    [Header("Tower Configuration")]
    [SerializeField] private TowerData towerData;

    [Header("Visual Settings")]
    [SerializeField] private Color towerColor = Color.blue;
    [SerializeField] private Vector3 towerSize = new Vector3(0.8f, 1f, 0.8f);

    [Header("Targeting")]
    [SerializeField] private TargetingMode targetingMode = TargetingMode.Closest;

    [Header("Range Visualization")]
    [SerializeField] private bool showRangeIndicator = true;
    [SerializeField] private bool showRangeWhenSelected = true;

    [Header("Projectile Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectilesSpawnPoint;
    [SerializeField] private bool debugProjectiles = false;

    [Header("Audio")]
    [SerializeField] private AudioClip fireSound;
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
        SetupVisuals();
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

            // Update any audio clips
            if (towerData.fireSound != null)
            {
                fireSound = towerData.fireSound;
            }

            Debug.Log($"Tower initialized with {towerData.GetLevelDisplayName()}");
        }
    }

    private void SetupVisuals()
    {
        if (towerRenderer != null)
        {
            towerRenderer.material.color = towerColor;
        }

        transform.localScale = towerSize;
    }

    private void SnapToGrid()
    {
        gridPosition = GridUtility.GetGridPosition(transform, gridManager);
        GridUtility.SnapToGrid(transform, gridManager, towerSize.y * 0.5f);
    }

    private void RegisterWithGrid()
    {
        if (gridManager != null)
        {
            bool success = gridManager.TryOccupyCell(gridPosition);
            if (!success)
            {
                Debug.LogWarning($"Tower {name} could not occupy grid position ({gridPosition.x}, {gridPosition.y})");
            }
            else
            {
                Debug.Log($"Tower {name} registered with grid position ({gridPosition.x}, {gridPosition.y})");
            }
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
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"Tower {name} has no projectile prefab assigned");
            return;
        }
        if (projectilesSpawnPoint == null)
        {
            Debug.LogWarning($"Tower {name} has no projectiles spawn point assigned");
            return;
        }

        if (currentTarget == null) return;

        // Instant damage
        currentTarget.TakeDamage(AttackDamage);

        PlayFireSound();

        // Visual projectile
        Vector3 spawnPosition = projectilesSpawnPoint.position;
        Vector3 targetPosition = currentTarget.transform.position;

        GameObject newProjectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
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
        Debug.Log($"Tower {name}: PlayFireSound called");
        Debug.Log($"Tower {name}: playFireSound = {playFireSound}");
        Debug.Log($"Tower {name}: fireSound = {(fireSound != null ? fireSound.name : "NULL")}");
        Debug.Log($"Tower {name}: audioSource = {(audioSource != null ? "EXISTS" : "NULL")}");

        if (!playFireSound)
        {
            Debug.LogWarning($"Tower {name}: Fire sound is disabled in settings");
            return;
        }

        if (fireSound == null)
        {
            Debug.LogWarning($"Tower {name}: No fire sound clip assigned");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogWarning($"Tower {name}: No AudioSource component found");
            return;
        }

        Debug.Log($"Tower {name}: All checks passed, playing sound");
        audioSource.clip = fireSound;
        audioSource.volume = soundVolume;
        audioSource.Play();
        Debug.Log($"Tower {name}: Sound played successfully");
    }

    private System.Collections.IEnumerator AttackFlash()
    {
        if (towerRenderer != null)
        {
            Color originalColor = towerRenderer.material.color;
            towerRenderer.material.color = Color.white;

            yield return new WaitForSeconds(0.1f);

            towerRenderer.material.color = originalColor;
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

        Debug.Log($"Tower {name} upgraded to {towerData.GetLevelDisplayName()}");
        return true;
    }

    private void UpdateTowerVisuals()
    {
        if (towerData == null) return;

        // Update scale if specified
        if (towerData.levelScales != null && towerData.upgradeLevel <= towerData.levelScales.Length)
        {
            float scale = towerData.levelScales[towerData.upgradeLevel - 1];
            transform.localScale = towerSize * scale;
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
