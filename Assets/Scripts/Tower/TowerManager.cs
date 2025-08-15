using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class TowerManager : MonoBehaviour
{
    [Header("Tower Settings")]
    [SerializeField] private GameObject towerPrefab;
    [SerializeField] private Transform towerParent;
    [SerializeField] private bool autoCreateTowerPrefab = true;

    [Header("Placement Settings")]
    [SerializeField] private LayerMask groundLayerMask = 1; // Default layer
    [SerializeField] private Material previewMaterial;
    [SerializeField] private bool showPlacementPreview = true;

    [Header("Input")]
    [SerializeField] private InputActionReference placeTowerAction;
    [SerializeField] private InputActionReference cancelAction;

    // Private variables
    private GridManager gridManager;
    private Camera playerCamera;
    private List<Tower> activeTowers = new List<Tower>();
    private GameObject previewTower;
    private bool isPlacementMode = false;
    private int towersBuilt = 0;

    // Public properties
    public int TowersBuilt => towersBuilt;
    public int ActiveTowerCount => activeTowers.Count;
    public bool IsPlacementMode => isPlacementMode;

    void Awake()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        playerCamera = Camera.main;

        if (playerCamera == null)
        {
            playerCamera = FindFirstObjectByType<Camera>();
        }

        if (towerParent == null)
        {
            GameObject parentObj = new GameObject("Towers");
            towerParent = parentObj.transform;
        }
    }

    void Start()
    {
        if (autoCreateTowerPrefab && towerPrefab == null)
        {
            CreateDefaultTowerPrefab();
        }

        SetupInput();

        Debug.Log("TowerManager: Ready for tower placement");
    }

    void OnEnable()
    {
        placeTowerAction?.action.Enable();
        cancelAction?.action.Enable();
    }

    void OnDisable()
    {
        placeTowerAction?.action.Disable();
        cancelAction?.action.Disable();
    }

    void Update()
    {
        HandleInput();

        if (isPlacementMode)
        {
            UpdatePlacementPreview();
        }
    }

    private void CreateDefaultTowerPrefab()
    {
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        prefab.name = "Tower";

        // Add Tower script
        prefab.AddComponent<Tower>();

        // Make it blue
        Renderer renderer = prefab.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.blue;
        }

        // Remove collider (we'll handle placement differently)
        Collider collider = prefab.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }

        towerPrefab = prefab;
        prefab.SetActive(false);

        Debug.Log("TowerManager: Created default tower prefab");
    }

    private void SetupInput()
    {
        // Setup input actions if available
        if (placeTowerAction != null)
        {
            placeTowerAction.action.performed += OnPlaceTower;
        }

        if (cancelAction != null)
        {
            cancelAction.action.performed += OnCancel;
        }
    }

    private void HandleInput()
    {
        // Fallback input handling using direct keyboard/mouse input
        if (placeTowerAction == null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame && isPlacementMode)
            {
                // Only place tower if already in placement mode
                TryPlaceTower();
            }
        }

        if (cancelAction == null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CancelPlacement();
            }
        }

        // Toggle placement mode with T key
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            TogglePlacementMode();
        }
    }

    private void OnPlaceTower(InputAction.CallbackContext context)
    {
        if (isPlacementMode)
        {
            TryPlaceTower();
        }
        // Don't automatically enter placement mode on click
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        CancelPlacement();
    }

    public void StartPlacementMode()
    {
        if (towerPrefab == null)
        {
            Debug.LogWarning("TowerManager: No tower prefab available");
            return;
        }

        isPlacementMode = true;
        CreatePlacementPreview();

        Debug.Log("TowerManager: Placement mode started - Click to place tower, ESC to cancel");
    }

    public void TogglePlacementMode()
    {
        if (isPlacementMode)
        {
            CancelPlacement();
        }
        else
        {
            StartPlacementMode();
        }
    }

    private void CreatePlacementPreview()
    {
        if (!showPlacementPreview || towerPrefab == null) return;

        if (previewTower != null)
        {
            DestroyImmediate(previewTower);
        }

        previewTower = Instantiate(towerPrefab);
        previewTower.name = "Tower Preview";

        // Disable the tower script on preview
        Tower towerScript = previewTower.GetComponent<Tower>();
        if (towerScript != null)
        {
            towerScript.enabled = false;
        }

        // Make it semi-transparent
        Renderer renderer = previewTower.GetComponent<Renderer>();
        if (renderer != null && previewMaterial != null)
        {
            renderer.material = previewMaterial;
        }
        else if (renderer != null)
        {
            Material mat = renderer.material;
            Color color = mat.color;
            color.a = 0.5f;
            mat.color = color;
        }

        previewTower.SetActive(true);
    }

    private void UpdatePlacementPreview()
    {
        if (previewTower == null) return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector2Int gridPos = gridManager.WorldToGrid(mouseWorldPos);
        Vector3 snapPosition = gridManager.GridToWorld(gridPos);

        previewTower.transform.position = new Vector3(snapPosition.x, 0.5f, snapPosition.z);

        // Change color based on whether placement is valid
        bool canPlace = CanPlaceTowerAt(gridPos);
        Renderer renderer = previewTower.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color color = canPlace ? Color.green : Color.red;
            color.a = 0.5f;
            renderer.material.color = color;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        if (playerCamera == null) return Vector3.zero;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = playerCamera.ScreenPointToRay(mousePos);

        // Raycast to the ground plane (Y = 0)
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }

    public void TryPlaceTower()
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector2Int gridPos = gridManager.WorldToGrid(mouseWorldPos);

        if (CanPlaceTowerAt(gridPos))
        {
            PlaceTower(gridPos);
        }
        else
        {
            Debug.Log("TowerManager: Cannot place tower at this location");
        }
    }

    private bool CanPlaceTowerAt(Vector2Int gridPos)
    {
        return gridManager != null && gridManager.CanBuildAt(gridPos);
    }

    private void PlaceTower(Vector2Int gridPos)
    {
        if (towerPrefab == null) return;

        Vector3 worldPos = gridManager.GridToWorld(gridPos);
        Vector3 spawnPos = new Vector3(worldPos.x, 0.5f, worldPos.z);

        GameObject newTower = Instantiate(towerPrefab, spawnPos, Quaternion.identity, towerParent);
        newTower.SetActive(true);
        newTower.name = $"Tower_{towersBuilt:000}";

        Tower towerComponent = newTower.GetComponent<Tower>();
        if (towerComponent != null)
        {
            activeTowers.Add(towerComponent);
        }

        towersBuilt++;

        Debug.Log($"TowerManager: Placed tower {towersBuilt} at grid ({gridPos.x}, {gridPos.y})");

        // Continue placement mode for multiple towers
        // CancelPlacement(); // Uncomment this if you want to exit placement mode after placing one tower
    }

    public void CancelPlacement()
    {
        isPlacementMode = false;

        if (previewTower != null)
        {
            DestroyImmediate(previewTower);
            previewTower = null;
        }

        Debug.Log("TowerManager: Placement cancelled");
    }

    public void DestroyTower(Tower tower)
    {
        if (tower != null && activeTowers.Contains(tower))
        {
            activeTowers.Remove(tower);
            DestroyImmediate(tower.gameObject);
            Debug.Log("TowerManager: Tower destroyed");
        }
    }

    public void DestroyAllTowers()
    {
        foreach (Tower tower in activeTowers)
        {
            if (tower != null)
            {
                DestroyImmediate(tower.gameObject);
            }
        }

        activeTowers.Clear();
        towersBuilt = 0;

        Debug.Log("TowerManager: All towers destroyed");
    }

    // Context menu for testing
    [ContextMenu("Start Placement Mode")]
    private void StartPlacementModeFromMenu()
    {
        StartPlacementMode();
    }

    [ContextMenu("Cancel Placement")]
    private void CancelPlacementFromMenu()
    {
        CancelPlacement();
    }

    [ContextMenu("Destroy All Towers")]
    private void DestroyAllTowersFromMenu()
    {
        DestroyAllTowers();
    }

    void OnDestroy()
    {
        // Clean up input events
        if (placeTowerAction != null)
        {
            placeTowerAction.action.performed -= OnPlaceTower;
        }

        if (cancelAction != null)
        {
            cancelAction.action.performed -= OnCancel;
        }
    }
}