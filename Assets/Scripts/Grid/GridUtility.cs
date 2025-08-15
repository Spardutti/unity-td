using UnityEngine;

public static class GridUtility
{
    public static void SnapToGrid(Transform transform, GridManager gridManager, float yOffset = 0f)
    {
        if (gridManager != null)
        {
            Vector2Int gridPosition = gridManager.WorldToGrid(transform.position);
            Vector3 worldPos = gridManager.GridToWorld(gridPosition);
            transform.position = new Vector3(worldPos.x, yOffset, worldPos.z);
        }
    }

    public static Vector2Int GetGridPosition(Transform transform, GridManager gridManager)
    {
        if (gridManager != null)
        {
            return gridManager.WorldToGrid(transform.position);
        }
        return Vector2Int.zero;
    }

    public static Vector3 GetSnappedWorldPosition(Vector3 worldPosition, GridManager gridManager, float yOffset = 0f)
    {
        if (gridManager != null)
        {
            Vector2Int gridPos = gridManager.WorldToGrid(worldPosition);
            Vector3 snappedPos = gridManager.GridToWorld(gridPos);
            return new Vector3(snappedPos.x, yOffset, snappedPos.z);
        }
        return worldPosition;
    }
}