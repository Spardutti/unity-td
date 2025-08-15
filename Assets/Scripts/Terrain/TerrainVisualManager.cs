using UnityEngine;

public class TerrainVisualManager : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private Material grassMaterial;
    [SerializeField] private Material dirtPathMaterial;
    [SerializeField] private Material buildableMaterial;

    [Header("Auto-Creation Settings")]
    [SerializeField] private bool autoCreateMaterials = true;
    [SerializeField] private Color grassColor = new Color(0.4f, 0.8f, 0.2f); // Green
    [SerializeField] private Color dirtColor = new Color(0.6f, 0.4f, 0.2f);  // Brown
    [SerializeField] private Color buildableColor = new Color(0.8f, 0.8f, 0.6f); // Light brown

    [Header("3D Settings")]
    [SerializeField] private float tileHeight = 0.1f;
    [SerializeField] private bool castShadows = false;

    private GridManager gridManager;
    private PathManager pathManager;
    private Transform tilesParent;
    private GameObject[,] tileGameObjects;

    void Awake()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        pathManager = FindFirstObjectByType<PathManager>();

        if (gridManager == null)
        {
            Debug.LogError("Terrain3DVisualManager: GridManager not found!");
        }

        if (pathManager == null)
        {
            Debug.LogError("Terrain3DVisualManager: PathManager not found!");
        }
    }

    void Start()
    {
        SetupParent();

        if (autoCreateMaterials)
        {
            CreateSimpleMaterials();
        }

        if (pathManager != null)
        {
            pathManager.OnPathSetupComplete.AddListener(OnPathSetupComplete);
            Debug.Log("Terrain3DVisualManager: Attached to PathManager");
        }

        GenerateTerrainVisuals();
    }

    void OnDestroy()
    {
        if (pathManager != null)
        {
            pathManager.OnPathSetupComplete.RemoveListener(OnPathSetupComplete);
        }
    }

    private void OnPathSetupComplete()
    {
        Debug.Log("Terrain3DVisualManager: Path setup complete");
        GenerateTerrainVisuals();
    }

    private void SetupParent()
    {
        GameObject parentObj = new GameObject("Terrain Tiles");
        parentObj.transform.SetParent(transform);
        tilesParent = parentObj.transform;

        tileGameObjects = new GameObject[gridManager.Width, gridManager.Height];
    }

    private void CreateSimpleMaterials()
    {
        grassMaterial = CreateColoredMaterial(grassColor, "Grass Material");
        dirtPathMaterial = CreateColoredMaterial(dirtColor, "Dirt Path Material");
        buildableMaterial = CreateColoredMaterial(buildableColor, "Buildable Material");

        Debug.Log("Terrain3DVisualManager: Created simple colored materials");
    }

    private Material CreateColoredMaterial(Color color, string name)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        mat.name = name;
        return mat;
    }

    public void GenerateTerrainVisuals()
    {
        if (gridManager == null)
        {
            Debug.LogError("Terrain3DVisualManager: Missing GridManager!");
            return;
        }

        Debug.Log($"Starting 3D terrain generation for {gridManager.Width}x{gridManager.Height} grid");

        // Clear existing tiles
        ClearExistingTiles();

        int tilesCreated = 0;
        int pathTiles = 0;
        int grassTiles = 0;

        // Generate terrain based on grid cell types
        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                GridCell cell = gridManager.GetCell(x, y);
                CellType cellType = cell?.cellType ?? CellType.Buildable;

                GameObject tileObj = CreateTileGameObject(x, y, cellType);
                tileGameObjects[x, y] = tileObj;

                tilesCreated++;
                if (cellType == CellType.Path) pathTiles++;
                else grassTiles++;

                // Debug first few tiles
                if (tilesCreated <= 5)
                {
                    Debug.Log($"Created tile at ({x},{y}) - Type: {cellType}, Position: {tileObj.transform.position}");
                }
            }
        }

        Debug.Log($"Terrain3DVisualManager: Created {tilesCreated} tiles ({pathTiles} path, {grassTiles} grass)");
    }

    private GameObject CreateTileGameObject(int gridX, int gridY, CellType cellType)
    {
        // Create a plane GameObject
        GameObject tileObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
        tileObj.name = $"Tile_{gridX}_{gridY}";
        tileObj.transform.SetParent(tilesParent);

        // Position the tile
        Vector3 worldPos = gridManager.GridToWorld(gridX, gridY);
        tileObj.transform.position = new Vector3(worldPos.x, 0, worldPos.z);

        // Scale the tile to match grid cell size
        float scale = gridManager.CellSize / 10f; // Plane is 10x10 units by default
        tileObj.transform.localScale = new Vector3(scale, 1f, scale);

        // Apply appropriate material
        Material materialToUse = GetMaterialForCellType(cellType);
        if (materialToUse != null)
        {
            Renderer renderer = tileObj.GetComponent<Renderer>();
            renderer.material = materialToUse;
            renderer.shadowCastingMode = castShadows ?
                UnityEngine.Rendering.ShadowCastingMode.On :
                UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // Remove collider if we don't need it
        Collider collider = tileObj.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }

        return tileObj;
    }

    private Material GetMaterialForCellType(CellType cellType)
    {
        return cellType switch
        {
            CellType.Path => dirtPathMaterial,
            CellType.Buildable => buildableMaterial ?? grassMaterial,
            CellType.Blocked => dirtPathMaterial,
            CellType.Occupied => grassMaterial,
            _ => grassMaterial
        };
    }

    private void ClearExistingTiles()
    {
        if (tilesParent != null)
        {
            for (int i = tilesParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(tilesParent.GetChild(i).gameObject);
            }
        }

        if (tileGameObjects != null)
        {
            for (int x = 0; x < tileGameObjects.GetLength(0); x++)
            {
                for (int y = 0; y < tileGameObjects.GetLength(1); y++)
                {
                    tileGameObjects[x, y] = null;
                }
            }
        }
    }

    // Method to update specific cell visual
    public void UpdateCellVisual(int x, int y, CellType newCellType)
    {
        if (tileGameObjects != null &&
            x >= 0 && x < tileGameObjects.GetLength(0) &&
            y >= 0 && y < tileGameObjects.GetLength(1))
        {
            GameObject tileObj = tileGameObjects[x, y];
            if (tileObj != null)
            {
                Material newMaterial = GetMaterialForCellType(newCellType);
                if (newMaterial != null)
                {
                    Renderer renderer = tileObj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = newMaterial;
                    }
                }
            }
        }
    }

    public void UpdateCellVisual(Vector2Int gridCoords, CellType newCellType)
    {
        UpdateCellVisual(gridCoords.x, gridCoords.y, newCellType);
    }

    // Method to refresh all visuals
    [ContextMenu("Refresh Terrain Visuals")]
    public void RefreshTerrainVisuals()
    {
        GenerateTerrainVisuals();
    }

    void OnValidate()
    {
        // Update materials in editor when colors change
        if (Application.isPlaying && autoCreateMaterials)
        {
            CreateSimpleMaterials();
            RefreshTerrainVisuals();
        }
    }
}