using UnityEngine;

public class TerrainVisualManager : MonoBehaviour
{
    [Header("Terrain Prefabs")]
    [SerializeField] private GameObject grassTilePrefab;
    [SerializeField] private GameObject pathTilePrefab;
    [SerializeField] private GameObject buildableTilePrefab;

    private GridManager gridManager;
    private MultiPathManager multiPathManager;
    private Transform tilesParent;
    private GameObject[,] tileGameObjects;

    void Awake()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        multiPathManager = FindFirstObjectByType<MultiPathManager>();

        if (gridManager == null)
        {
            Debug.LogError("Terrain3DVisualManager: GridManager not found!");
        }

        if (multiPathManager == null)
        {
            Debug.LogError("Terrain3DVisualManager: multiPathManager not found!");
        }
    }

    void Start()
    {
        SetupParent();

        ValidatePrefabs();

        GenerateTerrainVisuals();
    }

    private void SetupParent()
    {
        GameObject parentObj = new GameObject("Terrain Tiles");
        parentObj.transform.SetParent(transform);
        tilesParent = parentObj.transform;

        tileGameObjects = new GameObject[gridManager.Width, gridManager.Height];
    }

    private void ValidatePrefabs()
    {
        if (grassTilePrefab == null)
        {
            Debug.LogError("TerrainVisualManager: Grass Tile Prefab is not assigned!");
        }

        if (pathTilePrefab == null)
        {
            Debug.LogError("TerrainVisualManager: Path Tile Prefab is not assigned!");
        }

        if (buildableTilePrefab == null)
        {
            Debug.LogError("TerrainVisualManager: Buildable Tile Prefab is not assigned!");
        }

        if (grassTilePrefab != null && pathTilePrefab != null && buildableTilePrefab != null)
        {
            Debug.Log("TerrainVisualManager: All terrain prefabs validated successfully.");
        }
    }

    public void GenerateTerrainVisuals()
    {
        if (gridManager == null)
        {
            Debug.LogError("Terrain3DVisualManager: Missing GridManager!");
            return;
        }


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
                if (tileObj != null)
                {
                    tileGameObjects[x, y] = tileObj;

                    tilesCreated++;
                    if (cellType == CellType.Path) pathTiles++;
                    else grassTiles++;


                }
            }
        }

    }

    private GameObject CreateTileGameObject(int gridX, int gridY, CellType cellType)
    {
        GameObject prefabToUse = GetPrefabForCellType(cellType);

        if (prefabToUse == null)
        {
            Debug.LogError($"TerrainVisualManager: No prefab assigned for cell type {cellType} at ({gridX}, {gridY})!");
            return null;
        }

        GameObject tileObj = Instantiate(prefabToUse);
        tileObj.name = $"Tile_{gridX}_{gridY}";
        tileObj.transform.SetParent(tilesParent);

        // Position the tile
        Vector3 worldPos = gridManager.GridToWorld(gridX, gridY);
        tileObj.transform.position = new Vector3(worldPos.x, 0, worldPos.z);

        return tileObj;
    }

    private GameObject GetPrefabForCellType(CellType cellType)
    {
        return cellType switch
        {
            CellType.Path => pathTilePrefab,
            CellType.Buildable => buildableTilePrefab ?? grassTilePrefab,
            CellType.Blocked => pathTilePrefab,
            CellType.Occupied => grassTilePrefab,
            _ => grassTilePrefab
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
            GameObject oldTile = tileGameObjects[x, y];
            if (oldTile != null)
            {
                DestroyImmediate(oldTile);
            }

            // Create new tile with correct type
            GameObject newTile = CreateTileGameObject(x, y, newCellType);
            if (newTile != null)
            {
                tileGameObjects[x, y] = newTile;
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
        if (Application.isPlaying)
        {
            RefreshTerrainVisuals();
        }
    }
}