using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MultiPathManager : MonoBehaviour
{
    [Header("Path Detection")]
    [SerializeField] private Transform pathSystemParent;
    [SerializeField] private bool autoFindPathSystem = true;

    [Header("Validation")]
    [SerializeField] private float maxDestinationDistance = 2f;
    [SerializeField] private bool showDebugInfo = true;

    [Header("Gizmos")]
    [SerializeField] private bool showPathGizmos = true;
    [SerializeField] private Color[] pathColors = { Color.red, Color.green, Color.yellow, Color.magenta };

    private List<PathData> detectedPaths = new List<PathData>();
    private GridManager gridManager;

    public int PathCount => detectedPaths.Count;
    public List<PathData> AllPaths => new List<PathData>(detectedPaths);

    void Awake()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        if (autoFindPathSystem && pathSystemParent == null)
        {
            FindPathSystemParent();
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DetectAllPaths();
        ValidatePaths();
        MarkPathCellsInGrid();
    }

    private void FindPathSystemParent()
    {
        GameObject pathSystemObj = GameObject.Find("Path System");
        if (pathSystemObj != null)
        {
            pathSystemParent = pathSystemObj.transform;
            if (showDebugInfo)
            {
                Debug.Log($"MultiPathManager: Found Path System at {pathSystemParent.position}");
            }
        }
        else
        {
            Debug.LogWarning("MultiPathManager: Path System not found");
        }
    }

    private void DetectAllPaths()
    {
        detectedPaths.Clear();

        if (pathSystemParent == null)
        {
            Debug.LogError("MultiPathManager: Path system parent not found");
            return;
        }

        for (int i = 0; i < pathSystemParent.childCount; i++)
        {
            Transform pathTransform = pathSystemParent.GetChild(i);
            PathData pathData = CreatePathFromTransform(pathTransform);

            if (pathData != null && pathData.waypoints.Count > 0)
            {
                detectedPaths.Add(pathData);
                if (showDebugInfo)
                {
                    Debug.Log($"MultiPathManager: Detected path {pathData.pathName} with {pathData.waypoints.Count} waypoints");
                }
            }
        }
        if (showDebugInfo)
        {
            Debug.Log($"MultiPathManager: Total of {detectedPaths.Count} paths detected");
        }
    }

    private PathData CreatePathFromTransform(Transform pathTransform)
    {
        List<Waypoint> waypoints = new List<Waypoint>();

        // collect all waypoints from this path
        Waypoint[] foundWaypoints = pathTransform.GetComponentsInChildren<Waypoint>();

        if (foundWaypoints.Length == 0)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"MultiPathManager: No waypoints found in path {pathTransform.name}");
            }
            return null;
        }

        // Sort waypoints by order
        waypoints = foundWaypoints.OrderBy(w => w.Order).ToList();

        return new PathData
        {
            pathName = pathTransform.name,
            pathTransform = pathTransform,
            waypoints = waypoints
        };
    }

    private void ValidatePaths()
    {
        if (detectedPaths.Count < 2)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("MultiPathManager: Path has less than 2 waypoints");
            }
            return;
        }

        // Check that all paths have valid waypoint sequences
        foreach (PathData path in detectedPaths)
        {
            ValidatePathSequence(path);
        }

        // Check that all paths have valid waypoint sequences
        ValidateDestinations();
    }

    private void ValidatePathSequence(PathData path)
    {
        if (path.waypoints.Count < 2)
        {
            Debug.LogWarning($"MultiPathManager: Path {path.pathName} has less than 2 waypoints");
            return;
        }

        // check for gaps in waypoint ordering
        for (int i = 0; i < path.waypoints.Count; i++)
        {
            if (path.waypoints[i].Order != i)
            {
                Debug.LogWarning($"MultiPathManager: Waypoint {path.waypoints[i].Order} in path {path.pathName} is not at the correct order");
            }
        }
    }

    private void ValidateDestinations()
    {
        if (detectedPaths.Count < 2) return;

        Vector3 firstDestination = detectedPaths[0].GetStartPosition();

        for (int i = 1; i < detectedPaths.Count; i++)
        {
            Vector3 destination = detectedPaths[i].GetEndPosition();
            float distance = Vector3.Distance(firstDestination, destination);

            if (distance > maxDestinationDistance)
            {
                Debug.LogWarning($"MultiPathManager: Path {detectedPaths[i].pathName} has a destination that is too far away from the first path destination");
            }
        }
    }

    private void MarkPathCellsInGrid()
    {
        if (gridManager == null) return;

        foreach (PathData path in detectedPaths)
        {
            MarkSinglePathInGrid(path);
        }
    }

    private void MarkSinglePathInGrid(PathData path)
    {
        // mark all waypoints
        foreach (Waypoint waypoint in path.waypoints)
        {
            Vector2Int gridCoords = waypoint.GetGridCoordinates();
            gridManager.SetCellType(gridCoords.x, gridCoords.y, CellType.Path);
        }

        // Mark path lines between waypoints
        MarkPathLinesBetweenWaypoints(path);
    }

    private void MarkPathLinesBetweenWaypoints(PathData path)
    {
        if (path.waypoints.Count < 2)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"MultiPathManager: Path {path.pathName} has less than 2 waypoints");
            }
            return;
        }

        int totalPathCells = 0;

        for (int i = 0; i < path.waypoints.Count - 1; i++)
        {
            Vector2Int start = path.waypoints[i].GetGridCoordinates();
            Vector2Int end = path.waypoints[i + 1].GetGridCoordinates();

            // Get all cells between these two waypoints
            Vector2Int[] pathCells = GetCellsBetweenPoints(start, end);

            foreach (Vector2Int cell in pathCells)
            {
                if (gridManager.IsValidGridPosition(cell.x, cell.y))
                {
                    gridManager.SetCellType(cell.x, cell.y, CellType.Path);
                    totalPathCells++;
                }
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"MultiPathManager: Marked {totalPathCells} cells in grid as path");
        }
    }

    private Vector2Int[] GetCellsBetweenPoints(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        int dx = Mathf.Abs(end.x - start.x);
        int dy = Mathf.Abs(end.y - start.y);
        int x = start.x;
        int y = start.y;
        int n = 1 + dx + dy;
        int x_inc = (end.x > start.x) ? 1 : -1;
        int y_inc = (end.y > start.y) ? 1 : -1;
        int error = dx - dy;

        dx *= 2;
        dy *= 2;

        for (; n > 0; --n)
        {
            cells.Add(new Vector2Int(x, y));

            if (error > 0)
            {
                x += x_inc;
                error -= dy;
            }
            else
            {
                y += y_inc;
                error += dx;
            }
        }

        return cells.ToArray();

    }

    public PathData GetRandomPath()
    {
        if (detectedPaths.Count == 0) return null;

        int randomIndex = Random.Range(0, detectedPaths.Count);
        return detectedPaths[randomIndex];
    }

    public PathData GetPath(int index)
    {
        if (index < 0 || index >= detectedPaths.Count) return null;
        return detectedPaths[index];
    }

    public PathData GetPathByName(string pathName)
    {
        return detectedPaths.FirstOrDefault(p => p.pathName.Equals(pathName, System.StringComparison.OrdinalIgnoreCase));
    }

    void OnDrawGizmos()
    {
        if (!showPathGizmos || detectedPaths == null) return;

        for (int pathIndex = 0; pathIndex < detectedPaths.Count; pathIndex++)
        {
            PathData path = detectedPaths[pathIndex];
            Color pathColor = pathColors[pathIndex % pathColors.Length];

            Gizmos.color = pathColor;

            for (int i = 0; i < path.waypoints.Count - 1; i++)
            {
                Vector3 current = path.waypoints[i].Position;
                Vector3 next = path.waypoints[i + 1].Position;

                Gizmos.DrawLine(current, next);
                Gizmos.DrawWireSphere(current, 0.3f);
            }

            // Draw final waypoint
            if (path.waypoints.Count > 0)
            {
                Gizmos.DrawWireSphere(path.waypoints[path.waypoints.Count - 1].Position, 0.3f);
            }
        }
    }
}

[System.Serializable]
public class PathData
{
    public string pathName;
    public Transform pathTransform;
    public List<Waypoint> waypoints;

    public Vector3 GetStartPosition()
    {
        return waypoints.Count > 0 ? waypoints[0].Position : Vector3.zero;
    }

    public Vector3 GetEndPosition()
    {
        return waypoints.Count > 0 ? waypoints[waypoints.Count - 1].Position : Vector3.zero;
    }

    public Waypoint GetStartWaypoint()
    {
        return waypoints.Count > 0 ? waypoints[0] : null;
    }

    public Waypoint GetEndWaypoint()
    {
        return waypoints.Count > 0 ? waypoints[waypoints.Count - 1] : null;
    }

    public int WaypointCount => waypoints.Count;
}