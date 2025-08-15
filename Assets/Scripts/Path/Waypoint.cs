using UnityEngine;

public class Waypoint : MonoBehaviour
{
    [Header("Waypoint Configuration")]
    [SerializeField] private int waypointOrder = 0;
    [SerializeField] private float waypointRadius = 0.5f;

    [Header("Visual Settings")]
    [SerializeField] private Color waypointColor = Color.green;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private bool showOrderNumber = true;

    public int Order => waypointOrder;
    public Vector3 Position => transform.position;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SnapToGrid();
    }

    private void SnapToGrid()
    {
        GridManager gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager != null)
        {
            Vector2Int gridCoors = gridManager.WorldToGrid(transform.position);
            Vector3 gridWorldPos = gridManager.GridToWorld(gridCoors);

            transform.position = gridWorldPos;

            Debug.Log($"Waypoint {waypointOrder} snapped to grid cell ({gridCoors.x}, {gridCoors.y})");
        }
    }

    public void SetOrder(int newOrder)
    {
        waypointOrder = newOrder;
    }

    public bool IsAtValidGridPosition()
    {
        GridManager gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager != null)
        {
            Vector2Int gridCoors = gridManager.WorldToGrid(transform.position);
            return gridManager.IsValidGridPosition(gridCoors);
        }
        return false;
    }

    public Vector2Int GetGridCoordinates()
    {
        GridManager gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager != null)
        {
            return gridManager.WorldToGrid(transform.position);
        }

        return Vector2Int.zero;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = waypointColor;
        Gizmos.DrawWireSphere(transform.position, waypointRadius);

        Gizmos.color = new Color(waypointColor.r, waypointColor.g, waypointColor.b, 0.3f);
        Gizmos.DrawSphere(transform.position, waypointRadius);
    }

    void OawGizmosSelected()
    {
        Gizmos.color = selectedColor;
        Gizmos.DrawWireSphere(transform.position, waypointRadius);

        Gizmos.color = new Color(selectedColor.r, selectedColor.g, selectedColor.b, 0.5f);
        Gizmos.DrawSphere(transform.position, waypointRadius);

        if (showOrderNumber)
        {
            // Note: This would need a custom editor script to show text properly
            // For now, the order is visible in the Inspector

        }
    }

    void OnValidate()
    {
        if (waypointOrder < 0)
        {
            waypointOrder = 0;
        }
        if (waypointRadius <= 0)
        {
            waypointRadius = 0.5f;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
