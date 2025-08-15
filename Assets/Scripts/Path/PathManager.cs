using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

public class PathManager : MonoBehaviour
{
    [Header("Path Configuration")]
    [SerializeField] private bool autoFindWaypoints = true;
    [SerializeField] private List<Waypoint> waypoints = new List<Waypoint>();

    [Header("Visual Settings")]
    [SerializeField] private bool showPath = true;
    [SerializeField] private Color pathLineColor = Color.red;
    [SerializeField] private float pathLineWidth = 0.1f;

    [Header("Grid Integration")]
    [SerializeField] private bool markPathCellsOnGrid = true;
    [SerializeField] private GridManager gridManager;

    [Header("Events")]
    public UnityEvent OnPathSetupComplete;

    public List<Waypoint> Waypoints => waypoints;
    public int WaypointCount => waypoints.Count;

    void Awake()
    {
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupPath();
    }

    private void SetupPath()
    {
        if (autoFindWaypoints)
        {
            FindAllWaypoints();
        }

        SortWaypointsByOrder();
        ValidatePath();

        if (markPathCellsOnGrid)
        {
            MarkPathCellsInGrid();
        }

        Debug.Log($"PathManager: Path setup complete with {waypoints.Count} waypoints");

        OnPathSetupComplete?.Invoke();

    }

    private void FindAllWaypoints()
    {
        Waypoint[] foundWaypoints = FindObjectsByType<Waypoint>(FindObjectsSortMode.None);
        waypoints.Clear();
        waypoints.AddRange(foundWaypoints);

        Debug.Log($"PathManager: Found {foundWaypoints.Length} waypoints");
    }

    private void SortWaypointsByOrder()
    {
        waypoints = waypoints.OrderBy(w => w.Order).ToList();

        Debug.Log($"PathManager: Sorted waypoints by order");

        for (int i = 0; i < waypoints.Count; i++)
        {
            Debug.Log($"PathManager: Waypoint {i} order: {waypoints[i].Order}");
        }
    }

    private void ValidatePath()
    {
        if (waypoints.Count < 2)
        {
            Debug.LogWarning("PathManager: Path has less than 2 waypoints");
            return;

        }

        var orders = waypoints.Select(wp => wp.Order).ToList();
        var duplicates = orders.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        if (duplicates.Any())
        {
            Debug.LogWarning($"PathManager: Duplicate waypoint orders found: {string.Join(", ", duplicates)}");
        }

        foreach (var waypoint in waypoints)
        {
            if (!waypoint.IsAtValidGridPosition())
            {
                Debug.LogWarning($"PathManager: Waypoint {waypoint.Order} is not at a valid grid position");
            }
        }

        Debug.Log($"PathManager: Validated path with {waypoints.Count} waypoints");
    }

    private void MarkPathCellsInGrid()
    {
        if (gridManager == null)
        {
            Debug.LogWarning("PathManager: GridManager not found");
            return;
        }

        foreach (var waypoint in waypoints)
        {
            Vector2Int gridCoords = waypoint.GetGridCoordinates();
            GridCell cell = gridManager.GetCell(gridCoords);

            if (cell != null)
            {
                gridManager.SetCellType(gridCoords.x, gridCoords.y, CellType.Path);
                Debug.Log($"PathManager: Marked cell ({gridCoords.x}, {gridCoords.y}) as path cell");
            }
        }

        MarkPathBetweenCells();
    }

    private void MarkPathBetweenCells()
    {
        if (WaypointCount < 2)
        {
            Debug.Log("PathManager: Cannot mark path between cells - path has less than 2 waypoints");
            return;
        }

        Debug.Log("Pathmanager: Marking path between cells");
        int totalPathCells = 0;

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Vector2Int start = waypoints[i].GetGridCoordinates();
            Vector2Int end = waypoints[i + 1].GetGridCoordinates();

            var pathCells = GetLineBetweenPoints(start, end);

            foreach (var cellCoord in pathCells)
            {
                GridCell cell = gridManager.GetCell(cellCoord);
                if (cell != null)
                {
                    gridManager.SetCellType(cellCoord.x, cellCoord.y, CellType.Path);
                    totalPathCells++;


                }
            }
        }
    }

    private List<Vector2Int> GetLineBetweenPoints(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> line = new List<Vector2Int>();
        int x0 = start.x, y0 = start.y;
        int x1 = end.x, y1 = end.y;

        if (x0 == x1) // Vertical line
        {
            int minY = Mathf.Min(y0, y1);
            int maxY = Mathf.Max(y0, y1);

            for (int y = minY; y <= maxY; y++)
            {
                line.Add(new Vector2Int(x0, y));
            }
            return line;
        }

        if (y0 == y1) // Horizontal line
        {
            int minX = Mathf.Min(x0, x1);
            int maxX = Mathf.Max(x0, x1);

            for (int x = minX; x <= maxX; x++)
            {
                line.Add(new Vector2Int(x, y0));
            }
            return line;
        }

        // Bresenham's line algorithm for diagonal lines
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            line.Add(new Vector2Int(x0, y0));

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        return line;
    }

    public Waypoint GetNextWaypoint(Waypoint currentWaypoint)
    {
        int currentIndex = waypoints.IndexOf(currentWaypoint);

        if (currentIndex == -1 || currentIndex >= waypoints.Count - 1)
        {
            return null;
        }

        return waypoints[currentIndex + 1];
    }

    public Waypoint GetStartWaypoint()
    {
        return waypoints.Count > 0 ? waypoints[0] : null;
    }

    public Waypoint GetEndWaypoint()
    {
        return waypoints.Count > 0 ? waypoints[waypoints.Count - 1] : null;
    }

    public float GetTotalPathLength()
    {
        float totalLength = 0f;

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            totalLength += Vector3.Distance(waypoints[i].Position, waypoints[i + 1].Position);
        }

        return totalLength;
    }

    public Vector3 GetPathDirection(Waypoint waypoint)
    {
        Waypoint nextWaypoint = GetNextWaypoint(waypoint);

        if (nextWaypoint == null)
        {
            return (nextWaypoint.Position - waypoint.Position).normalized;
        }

        return Vector3.forward;
    }

    void OnDrawGizmos()
    {
        if (!showPath || waypoints.Count < 2) return;

        Gizmos.color = pathLineColor;

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Vector3 start = waypoints[i].Position;
            Vector3 end = waypoints[i + 1].Position;

            Gizmos.DrawLine(start, end);

            // Draw arrow head
            Vector3 direction = (end - start).normalized;
            Vector3 arrowHead = end - direction * 0.5f;
            Vector3 right = Vector3.Cross(direction, Vector3.up) * 0.2f;

            Gizmos.DrawLine(end, arrowHead + right);
            Gizmos.DrawLine(end, arrowHead - right);
        }
    }

    // Manual refresh method for editor use
    [ContextMenu("Refresh Path")]
    public void RefreshPath()
    {
        SetupPath();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
