using UnityEngine;
using UnityEngine.UI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnParent;

    [Header("UI References")]
    [SerializeField] private Button spawnButton;

    [Header("Spawn Settings")]
    [SerializeField] private bool autoCreateEnemyPrefab = true;
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
        SetupUI();
        CreateEnemyPrefabIfNeeded();
    }

    private void SetupUI()
    {
        if (spawnButton != null)
        {
            spawnButton.onClick.AddListener(SpawnEnemy);
            Debug.Log($"EnemySpawner: Spawn button connected");
        }
        else
        {
            Debug.LogWarning("EnemySpawner: Spawn button not found");
        }
    }

    private void CreateEnemyPrefabIfNeeded()
    {
        if (enemyPrefab == null && autoCreateEnemyPrefab)
        {
            Debug.Log("EnemySpawner: Creating enemy prefab automatically...");

            GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefab.name = "Enemy";

            // Add enemy script
            prefab.AddComponent<Enemy>();

            // Make it red
            Renderer renderer = prefab.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.red;
            }

            enemyPrefab = prefab;

            prefab.SetActive(false);

            Debug.Log("EnemySpawner: Enemy prefab created");
        }
    }

    public void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: Enemy prefab not found");
            return;
        }

        if (pathManager == null)
        {
            Debug.LogError("EnemySpawner: PathManager not found");
            return;
        }

        Waypoint spawnPoint = pathManager.GetStartWaypoint();
        if (spawnPoint == null)
        {
            Debug.LogError("EnemySpawner: No start waypoint found");
            return;
        }

        // Calculate spawn position
        Vector3 spawnPosition = spawnPoint.Position + spawnOffset;

        // Spawn the enemy
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, spawnParent);
        newEnemy.SetActive(true);
        newEnemy.name = $"Enemy_{enemiesSpawned:000}";

        enemiesSpawned++;
        Debug.Log($"EnemySpawner: Spawned enemy {enemiesSpawned}");


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
