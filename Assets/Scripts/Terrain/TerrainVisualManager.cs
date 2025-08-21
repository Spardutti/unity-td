using UnityEngine;

public class TerrainVisualManager : MonoBehaviour
{
    [Header("Terrain Prefabs")]
    [SerializeField] private GameObject grassTilePrefab;
    [SerializeField] private GameObject pathTilePrefab;
    [SerializeField] private GameObject buildableTilePrefab;
    [SerializeField] private GameObject pathCornerTilePrefab;

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
        if (pathCornerTilePrefab != null)
        {
            Debug.Log("TerrainVisualManager: Path Corner Tile Prefab validated successfully.");
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
        GameObject prefabToUse = GetPrefabForCellType(cellType, gridX, gridY);

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

        if (cellType == CellType.Path)
        {
            if (IsCornerPath(gridX, gridY))
            {
                RotateCornerTile(tileObj, gridX, gridY);
            }
            else
            {
                RotatePathTile(tileObj, gridX, gridY);
            }
        }

        return tileObj;
    }

    private void RotateCornerTile(GameObject tileObj, int gridX, int gridY)
    {
        bool hasUp = HasPathNeighbor(gridX, gridY + 1);
        bool hasDown = HasPathNeighbor(gridX, gridY - 1);
        bool hasLeft = HasPathNeighbor(gridX - 1, gridY);
        bool hasRight = HasPathNeighbor(gridX + 1, gridY);

        // Determine rotation based on which two directions have neighbors
        // Assuming default corner prefab connects up+right
        float rotationY = 0f;
        if (hasUp && hasRight)
        {
            rotationY = 90f;
        }
        else if (hasDown && hasRight)
        {
            rotationY = 90f;
        }
        else if (hasDown && hasLeft)
        {
            rotationY = 270f;
        }
        else if (hasUp && hasLeft)
        {
            rotationY = 180f;
        }

        tileObj.transform.rotation = Quaternion.Euler(0, rotationY, 0);

    }

    private void RotatePathTile(GameObject tileObj, int gridX, int gridY)
    {
        // check if path flows horizontally by looking at left/right neighbors
        bool hasLeftPath = HasPathNeighbor(gridX - 1, gridY);
        bool hasRightPath = HasPathNeighbor(gridX + 1, gridY);

        // if path has horizontal neighbors, rotate 90 degrees
        if (hasLeftPath || hasRightPath)
        {
            tileObj.transform.rotation = Quaternion.Euler(0, 90, 0);
        }
    }

    private bool HasPathNeighbor(int x, int y)
    {
        if (!gridManager.IsValidGridPosition(x, y))
        {
            return false;
        }

        GridCell cell = gridManager.GetCell(x, y);
        return cell != null && cell.cellType == CellType.Path;
    }

    private GameObject GetPrefabForCellType(CellType cellType, int gridX, int gridY)
    {
        if (cellType == CellType.Path)
        {
            return IsCornerPath(gridX, gridY) ? pathCornerTilePrefab : pathTilePrefab;
        }
        return cellType switch
        {
            CellType.Buildable => buildableTilePrefab ?? grassTilePrefab,
            CellType.Blocked => pathTilePrefab,
            CellType.Occupied => grassTilePrefab,
            _ => grassTilePrefab
        };
    }

    private bool IsCornerPath(int gridX, int gridY)
    {
        // count path neighbors in 4 directions
        bool hasUp = HasPathNeighbor(gridX, gridY + 1);
        bool hasDown = HasPathNeighbor(gridX, gridY - 1);
        bool hasLeft = HasPathNeighbor(gridX - 1, gridY);
        bool hasRight = HasPathNeighbor(gridX + 1, gridY);

        int neighborCount = (hasUp ? 1 : 0) + (hasDown ? 1 : 0) + (hasLeft ? 1 : 0) + (hasRight ? 1 : 0);

        // Corner: exactly 2 neighbors that are perpendicular to each other
        if (neighborCount == 2)
        {
            return (hasUp && hasLeft) || (hasUp && hasRight) || (hasDown && hasLeft) || (hasDown && hasRight);
        }

        return false;
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