using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnParent;

    [Header("Spawn Settings")]
    [SerializeField] private Vector3 spawnOffset = Vector3.up * 0.5f;

    private PathManager pathManager;
    private int enemiesSpawned = 0;

    public int EnemiesSpawned => enemiesSpawned;

    void Awake()
    {
        pathManager = FindFirstObjectByType<PathManager>();
        if (pathManager == null)
        {
            Debug.LogError("EnemySpawner: PathManager not found");
        }

        if (spawnParent == null)
        {
            GameObject spawnParentObj = new GameObject("Spawn Enemies");
            spawnParent = spawnParentObj.transform;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Safety check: Remove Enemy component if accidentally attached to spawner
        Enemy enemyComponent = GetComponent<Enemy>();
        if (enemyComponent != null)
        {
            Debug.LogWarning("EnemySpawner: Removing Enemy component from spawner GameObject!");
            DestroyImmediate(enemyComponent);
        }

        ValidateEnemyPrefab();
    }



    private void ValidateEnemyPrefab()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: Enemy prefab is not assigned! Please assign an enemy prefab in the inspector.");
        }
        else
        {
            // Validate that the prefab has required components
            if (enemyPrefab.GetComponent<Enemy>() == null && enemyPrefab.GetComponentInChildren<Enemy>() == null)
            {
                Debug.LogWarning("EnemySpawner: Enemy prefab doesn't have an Enemy component!");
            }
        }
    }

    public void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: Enemy prefab not found");
            return;
        }

        SpawnSpecificEnemy(enemyPrefab);
    }

    public GameObject SpawnSpecificEnemy(GameObject specificEnemyPrefab, float healthMultiplier = 1f)
    {
        if (specificEnemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: Specific enemy prefab not found");
            return null;
        }

        if (pathManager == null)
        {
            Debug.LogError("EnemySpawner: PathManager not found");
            return null;
        }

        Waypoint spawnPoint = pathManager.GetStartWaypoint();
        if (spawnPoint == null)
        {
            Debug.LogError("EnemySpawner: No start waypoint found");
            return null;
        }

        // Calculate spawn position
        Vector3 spawnPosition = spawnPoint.Position + spawnOffset;

        // Spawn the enemy
        GameObject newEnemy = Instantiate(specificEnemyPrefab, spawnPosition, Quaternion.identity, spawnParent);
        newEnemy.SetActive(true);
        newEnemy.name = $"Enemy_{enemiesSpawned:000}";

        // Apply health multiplier if needed
        if (healthMultiplier != 1f)
        {
            Enemy enemyComponent = newEnemy.GetComponent<Enemy>();
            if (enemyComponent == null)
            {
                enemyComponent = newEnemy.GetComponentInChildren<Enemy>();
            }
            
            if (enemyComponent != null)
            {
                enemyComponent.ApplyHealthMultiplier(healthMultiplier);
            }
        }

        enemiesSpawned++;
        Debug.Log($"EnemySpawner: Spawned enemy {enemiesSpawned} ({specificEnemyPrefab.name})");

        return newEnemy;
    }

    public void SpawnMultipleEnemies(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnEnemy();

            if (i < count - 1)
            {
                // Coroutine for automatic spacing
            }
        }
    }

    public void ClearAllEnemies()
    {
        if (spawnParent != null)
        {
            for (int i = spawnParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(spawnParent.GetChild(i).gameObject);
            }

            enemiesSpawned = 0;
            Debug.Log($"EnemySpawner: Cleared all enemies");
        }
    }

    // Context menu for easy testing in editor
    [ContextMenu("Spawn Enemy Test")]
    private void SpawnTestEnemyFromMenu()
    {
        SpawnEnemy();
    }

    [ContextMenu("Clear All Enemies")]
    private void ClearAllEnemiesFromMenu()
    {
        ClearAllEnemies();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
