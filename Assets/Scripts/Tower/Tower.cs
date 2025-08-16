using UnityEngine;
using System.Collections.Generic;

public class Tower : MonoBehaviour
{

    [Header("Tower Stats")]
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackDamage = 25f;
    [SerializeField] private float attackSpeed = 1f; // per second
    [SerializeField] private int towerCost = 50;


    [Header("Visual Settings")]
    [SerializeField] private Color towerColor = Color.blue;
    [SerializeField] private Vector3 towerSize = new Vector3(0.8f, 1f, 0.8f);

    [Header("Targeting")]
    [SerializeField] private TargetingMode targetingMode = TargetingMode.Closest;

    [Header("Range Visualization")]
    [SerializeField] private bool showRangeIndicator = true;
    [SerializeField] private bool showRangeWhenSelected = true;
    [SerializeField] private Color rangeColor = new Color(1f, 1f, 1f, 0.3f);

    [Header("Projectile Settings")]
    [SerializeField] private bool useProjectiles = true;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectilesSpawnPoint;
    [SerializeField] private bool debugProjectiles = false;

    private float lastAttackTime;
    private Enemy currentTarget;
    private GridManager gridManager;
    private Vector2Int gridPosition;
    private Renderer towerRenderer;
    private List<Enemy> enemiesInRange = new List<Enemy>();

    public float AttackRange => attackRange;
    public float AttackDamage => attackDamage;
    public float AttackSpeed => attackSpeed;
    public int TowerCost => towerCost;
    public Vector2Int GridPosition => gridPosition;
    public Enemy CurrentTarget => currentTarget;
    public bool HasTarget => currentTarget != null;


    void Awake()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        towerRenderer = GetComponent<Renderer>();

        if (towerRenderer == null)
        {
            towerRenderer = GetComponent<Renderer>();
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupVisuals();
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
                if (distance <= attackRange)
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
            TargetingMode.Furthest => Vector3.Distance(transform.position, enemy.transform.position),
            TargetingMode.LowestHealth => enemy.CurrentHealth,
            TargetingMode.HighestHealth => enemy.CurrentHealth,
            TargetingMode.MostProgress => enemy.GetPathProgress(),
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
        currentTarget.TakeDamage(attackDamage);

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

    public void UpgradeTower(float damageIncrease, float rangeIncrease, float speedIncrease)
    {
        attackDamage += damageIncrease;
        attackRange += rangeIncrease;
        attackSpeed += speedIncrease;

        Debug.Log($"Tower {name} upgraded to tower with attack damage {attackDamage}, attack range {attackRange}, attack speed {attackSpeed}");
    }

    // Method to get tower info for UI
    public string GetTowerInfo()
    {
        return $"Damage: {attackDamage:F1}, Range: {attackRange:F1}, Speed: {attackSpeed:F1}";
    }

    void OnDrawGizmos()
    {
        if (showRangeIndicator)
        {
            DrawRangeGizmo();
        }
    }

    void OawGizmosSelected()
    {
        if (showRangeWhenSelected)
        {
            DrawRangeGizmo();
        }
    }

    private void DrawRangeGizmo()
    {
        Gizmos.color = rangeColor;
        Gizmos.DrawSphere(transform.position, attackRange);

        Gizmos.color = new Color(rangeColor.r, rangeColor.g, rangeColor.b, 1f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
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
