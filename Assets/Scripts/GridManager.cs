using UnityEditor.ShaderGraph.Internal;
using UnityEditor.U2D.Aseprite;
using UnityEngine;
using UnityEngine.Rendering;

public class GridManager : MonoBehaviour
{
    [Header("Grid Configuration")]
    [SerializeField] private int gridWidth = 20;
    [SerializeField] private int gridHeight = 15;
    [SerializeField] private float cellSize = 1f;

    [Header("Visual Settings")]
    [SerializeField] private bool showGridInEditor = true;
    [SerializeField] private Color gridLineColor = Color.white;

    // 2D array to store the grid
    private GridCell[,] grid;

    public int Width => gridWidth;
    public int Height => gridHeight;
    public float CellSize => cellSize;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Awake()
    {
        CreateGrid();
    }
    void Start()
    {
        Debug.Log($"Grid created: {gridWidth}x{gridHeight} cells, each {cellSize} units");

    }

    private void CreateGrid()
    {
        grid = new GridCell[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                grid[x, y] = new GridCell(x, y);
            }
        }

        Debug.Log("Grid created");
    }

    // Convert world position to grid coordinates
    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        int gridX = Mathf.FloorToInt(worldPosition.x / cellSize);
        int gridY = Mathf.FloorToInt(worldPosition.z / cellSize); // Note: using Z for 2.5D

        return new Vector2Int(gridX, gridY);
    }

    // Convert grid coordinates to world position (center of cell)
    public Vector3 GridToWorld(int gridX, int gridY)
    {
        float worldX = gridX * cellSize + (cellSize * 0.5f);
        float worldY = gridY * cellSize + (cellSize * 0.5f);

        return new Vector3(worldX, 0, worldY);
    }

    // Convert grid coordinates to world position (Vector2Int version)
    public Vector3 GridToWorld(Vector2Int gridCoordinates)
    {
        return GridToWorld(gridCoordinates.x, gridCoordinates.y);
    }

    public bool IsValidGridPosition(int gridX, int gridY)
    {
        return gridX >= 0 && gridX < gridWidth && gridY >= 0 && gridY < gridHeight;
    }

    public bool IsValidGridPosition(Vector2Int gridCoordinates)
    {
        return IsValidGridPosition(gridCoordinates.x, gridCoordinates.y);
    }


    public GridCell GetCell(int gridX, int gridY)
    {
        if (IsValidGridPosition(gridX, gridY))
        {
            return grid[gridX, gridY];
        }

        Debug.LogWarning($"Tried to access invalid grid position ({gridX}, {gridY})");
        return null;
    }

    public GridCell GetCell(Vector2Int gridCoordinates)
    {
        return GetCell(gridCoordinates.x, gridCoordinates.y);
    }

    // Draw grid lines in the Scene view (for debugging)
    void OnDrawGizmos()
    {
        if (!showGridInEditor) return;

        Gizmos.color = gridLineColor;

        // Draw vertical lines (including the right boundary)
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = new Vector3(x * cellSize, 0, 0);
            Vector3 end = new Vector3(x * cellSize, 0, gridHeight * cellSize);
            Gizmos.DrawLine(start, end);
        }

        // Draw horizontal lines (including the top boundary)
        for (int y = 0; y <= gridHeight; y++)
        {
            Vector3 start = new Vector3(0, 0, y * cellSize);
            Vector3 end = new Vector3(gridWidth * cellSize, 0, y * cellSize);
            Gizmos.DrawLine(start, end);
        }

    }

    // Update is called once per frame
    void Update()
    {

    }
}

[System.Serializable]
public class GridCell
{
    public int x;
    public int y;
    public bool isOccupied;
    public CellType cellType;

    public GridCell(int x, int y)
    {
        this.x = x;
        this.y = y;
        this.isOccupied = false;
        this.cellType = CellType.Buildable;
    }
}

public enum CellType
{
    Buildable,
    Path,
    Blocked,
    Occupied
}