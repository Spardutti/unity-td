using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class GridTester : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Camera playerCamera;

    [Header("UI Display")]
    [SerializeField] private TextMeshProUGUI coordinatesText;
    [SerializeField] private TextMeshProUGUI instructionText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (instructionText != null)
        {
            instructionText.text = "Move mouse to see grid coordinates";
        }

        if (gridManager == null)
        {
            Debug.LogError("GridManager not found");
        }
        if (playerCamera == null)
        {
            Debug.LogError("Player camera not found");
        }
    }

    // Update is called once per frame
    void Update()
    {
        TestMousePosition();
        HandleMouseClick();
    }

    private void TestMousePosition()
    {
        // Get mouse position in screen space
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        // Convert screen position to world position
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);

        // Create a plane at Y=0 ground level to raycast against
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            // Get the world position where the ray hit the ground
            Vector3 worldPosition = ray.GetPoint(distance);

            // Convert world position to grid coordinates
            Vector2Int gridCoors = gridManager.WorldToGrid(worldPosition);

            // check if the grid position is valid
            bool isValid = gridManager.IsValidGridPosition(gridCoors);

            // update UI text
            if (coordinatesText != null)
            {
                string coordText = $"Mouse World Pos: ({worldPosition.x:F2}, {worldPosition.z:F2}\n";
                coordText += $"Grid Coordinates: ({gridCoors.x}, {gridCoors.y})\n";
                coordText += $"Valid Position: {(isValid ? "Yes" : "No")}";

                if (isValid)
                {
                    GridCell cell = gridManager.GetCell(gridCoors);
                    coordText += $"\n Cell Type: {cell.cellType}";
                    coordText += $"\n Occupied: {(cell.isOccupied ? "Yes" : "No")}";
                }

                coordinatesText.text = coordText;
            }
        }
    }

    private void HandleMouseClick()
    {
        // check for mouse click
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = playerCamera.ScreenPointToRay(mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPosition = ray.GetPoint(distance);
                Vector2Int gridCoors = gridManager.WorldToGrid(worldPosition);
                bool isValid = gridManager.IsValidGridPosition(gridCoors);
                if (isValid)
                {
                    // Test: Convert grid coordinates to world position (Vector2Int version)
                    Vector3 cellCenter = gridManager.GridToWorld(gridCoors);
                    Debug.Log($"Clicked on grid cell at ({gridCoors.x}, {gridCoors.y})");
                    Debug.Log($"Cell center: {cellCenter}");

                    CreateVisualMarker(cellCenter);
                }
                else
                {
                    Debug.Log($"Clicked outside valid grid area: ({gridCoors.x}, {gridCoors.y})");
                }
            }
        }
    }

    private void CreateVisualMarker(Vector3 position)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "GridMarker";
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * 0.8f;

        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.red;
        }
        // Destroy the marker after 3 seconds
        Destroy(marker, 3f);
    }
}
