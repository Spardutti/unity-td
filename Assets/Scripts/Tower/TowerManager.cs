using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class TowerManager : MonoBehaviour
{
    [Header("Tower Settings")]
    [SerializeField] private Transform towerParent;
    [SerializeField] private TowerData[] availableTowers;

    [Header("Placement Settings")]
    [SerializeField] private Material previewMaterial;
    [SerializeField] private bool showPlacementPreview = true;

    [Header("Range Visualization")]
    [SerializeField] private bool ShowRangeDuringPlacement = true;
    [SerializeField] private bool showRangeOnHover = true;
    [SerializeField] private KeyCode toggleRangeKey = KeyCode.R;

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

    private Tower hoveredTower;
    private TowerData selectedTowerData;

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
        ValidateTowerPrefab();
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
        else if (showRangeOnHover)
        {
            HandleMouseHover();
        }
    }

    private void HandleMouseHover()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = playerCamera.ScreenPointToRay(mousePos);

        Tower newHoveredTower = null;

        // Raycast to find towers
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            newHoveredTower = hit.collider.GetComponent<Tower>();
        }

        // Handle hover state changes
        if (newHoveredTower != hoveredTower)
        {
            // Hide previous tower's range
            if (hoveredTower != null)
            {
                TowerRangeIndicator oldIndicator = hoveredTower.GetComponent<TowerRangeIndicator>();
                oldIndicator?.HideRange();
            }

            // Show new tower's range
            if (newHoveredTower != null)
            {
                TowerRangeIndicator newIndicator = newHoveredTower.GetComponent<TowerRangeIndicator>();
                newIndicator?.ShowRange();
            }

            hoveredTower = newHoveredTower;
        }
    }

    private void ValidateTowerPrefab()
    {
        if (availableTowers == null || availableTowers.Length == 0)  // CHANGE THIS SECTION
        {
            Debug.LogError("TowerManager: No tower data assigned! Please assign tower data in the inspector.");
            return;
        }

        foreach (TowerData towerData in availableTowers)
        {
            if (towerData?.towerPrefab != null)
            {
                if (towerData.towerPrefab.GetComponent<Tower>() == null)
                {
                    Debug.LogWarning($"TowerManager: {towerData.towerName} prefab doesn't have a Tower component!");
                }
            }
        }
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
            if (Keyboard.current.escapeKey.wasPressedThisFrame && isPlacementMode)
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
        if (isPlacementMode)
        {
            CancelPlacement();
        }
    }

    public void StartPlacementMode(TowerData towerData = null)
    {
        selectedTowerData = towerData != null ? towerData : (availableTowers?.Length > 0 ? availableTowers[0] : null);


        if (selectedTowerData == null || selectedTowerData.towerPrefab == null)
        {
            Debug.LogWarning("TowerManager: No tower prefab available");
            return;
        }

        isPlacementMode = true;

        // Hide hover ranges when entering placement mode
        if (hoveredTower != null)
        {
            TowerRangeIndicator indicator = hoveredTower.GetComponent<TowerRangeIndicator>();
            if (indicator != null)
            {
                indicator.HideRange();
            }
            hoveredTower = null;
        }
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
        if (!showPlacementPreview || selectedTowerData == null || selectedTowerData.towerPrefab == null) return;

        if (previewTower != null)
        {
            DestroyImmediate(previewTower);
        }

        // Use the selected tower's prefab
        GameObject prefabToUse = selectedTowerData.towerPrefab;
        previewTower = Instantiate(prefabToUse);
        previewTower.name = "Tower Preview";

        // Disable the tower script on preview but apply its size
        Tower towerScript = previewTower.GetComponent<Tower>();
        if (towerScript != null)
        {
            // Apply the tower size before disabling
            previewTower.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
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

        // Show range indicator during placement if enabled
        if (ShowRangeDuringPlacement)
        {
            StartCoroutine(ShowRangeNextFrame(previewTower));
        }
    }

    private System.Collections.IEnumerator ShowRangeNextFrame(GameObject towerObj)
    {
        yield return null; // Wait one frame

        if (towerObj != null)
        {
            TowerRangeIndicator rangeIndicator = towerObj.GetComponent<TowerRangeIndicator>();
            if (rangeIndicator != null)
            {
                rangeIndicator.ShowRange();
            }
        }
    }

    private void UpdatePlacementPreview()
    {
        if (previewTower == null) return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector2Int gridPos = gridManager.WorldToGrid(mouseWorldPos);
        Vector3 snapPosition = gridManager.GridToWorld(gridPos);

        previewTower.transform.position = new Vector3(snapPosition.x, 0.5f, snapPosition.z);

        // Change color based on whether placement is valid
        bool canPlaceOnGrid = CanPlaceTowerAt(gridPos);
        bool hasEnoughGold = EconomyManager.Instance?.HasEnoughGold(selectedTowerData.cost) ?? false;

        // Try to find renderer in children
        Renderer renderer = previewTower.GetComponentInChildren<Renderer>();

        if (renderer != null)
        {
            Debug.Log($"TowerManager: CanPlace: {canPlaceOnGrid}, HasEnoughGold: {hasEnoughGold}");
            Color color;
            if (!canPlaceOnGrid)
            {
                color = Color.red;
            }
            else if (!hasEnoughGold)
            {
                color = Color.yellow;
            }
            else
            {
                color = Color.green;
            }
            Debug.Log($"TowerManager: Preview tower color: {color}");
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

        if (!CanPlaceTowerAt(gridPos))
        {
            Debug.Log("TowerManager: Cannot place tower at this location");
            return;
        }

        // Check if player has enough gold
        int towerCost = selectedTowerData.cost;

        if (EconomyManager.Instance == null)
        {
            Debug.LogError("TowerManager: EconomyManager not found");
            return;
        }

        if (!EconomyManager.Instance.HasEnoughGold(towerCost))
        {
            Debug.Log($"TowerManager: Cannot place tower, not enough gold ({towerCost} needed)");
            return;
        }

        if (EconomyManager.Instance.TryToSpendGold(towerCost))
        {
            PlaceTower(gridPos);
        }

    }

    private bool CanPlaceTowerAt(Vector2Int gridPos)
    {
        return gridManager != null && gridManager.CanBuildAt(gridPos);
    }

    private void PlaceTower(Vector2Int gridPos)
    {
        if (selectedTowerData?.towerPrefab == null) return;

        Vector3 worldPos = gridManager.GridToWorld(gridPos);
        Vector3 spawnPos = new Vector3(worldPos.x, 0.5f, worldPos.z);

        // Ensure tower spawns with no rotation (upright)
        GameObject newTower = Instantiate(selectedTowerData.towerPrefab, spawnPos, Quaternion.Euler(0, 0, 0), towerParent);
        newTower.SetActive(true);
        newTower.name = $"{selectedTowerData.towerName}_{towersBuilt:000}";

        Tower towerComponent = newTower.GetComponent<Tower>();
        if (towerComponent != null)
        {
            activeTowers.Add(towerComponent);
        }

        // Grid cell will be occupied by the Tower's RegisterWithGrid method

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
            // Hide range indicator before destroying
            TowerRangeIndicator rangeIndicator = previewTower.GetComponent<TowerRangeIndicator>();
            if (rangeIndicator != null)
            {
                rangeIndicator.HideRange();
            }

            DestroyImmediate(previewTower);
            previewTower = null;
        }

        Debug.Log("TowerManager: Placement cancelled");
    }

    public void ToggleAllTowerRanges()
    {
        foreach (Tower tower in activeTowers)
        {
            if (tower != null)
            {
                TowerRangeIndicator indicator = tower.GetComponent<TowerRangeIndicator>();
                if (indicator != null)
                {
                    indicator.ToggleRange();
                }
            }
        }
    }

    public void ShowAllTowerRanges()
    {
        foreach (Tower tower in activeTowers)
        {
            TowerRangeIndicator indicator = tower.GetComponent<TowerRangeIndicator>();
            if (indicator != null)
            {

                indicator.ShowRange();
            }
        }
    }

    public void HideAllTowerRanges()
    {
        foreach (Tower tower in activeTowers)
        {
            TowerRangeIndicator indicator = tower.GetComponent<TowerRangeIndicator>();
            indicator.HideRange();
        }
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