using UnityEngine;

public class TowerRangeIndicator : MonoBehaviour
{
    [Header("Range Visualization")]
    [SerializeField] private GameObject rangeIndicatorPrefab; // Prefab for the range indicator 

    private GameObject rangeIndicatorObject;
    private Tower tower;
    private bool isVisible = false;

    void Awake()
    {
        tower = GetComponent<Tower>();
    }

    void Start()
    {
        CreateRangeIndicator();
        HideRange();
    }

    private void CreateRangeIndicator()
    {
        if (rangeIndicatorPrefab == null)
        {
            Debug.LogError("Range Indicator Prefab is not assigned!");
            return;
        }

        // Instantiate the prefab
        rangeIndicatorObject = Instantiate(rangeIndicatorPrefab, transform);
        rangeIndicatorObject.transform.localPosition = new Vector3(0, 0.1f, 0);

        UpdateRangeScale();
    }

    private void UpdateRangeScale()
    {
        if (rangeIndicatorObject == null || tower == null) return;

        float range = tower.AttackRange;

        // Scale the cylinder to match the tower's range
        // Cylinder default radius is 0.5, so we need range * 2 for diameter
        rangeIndicatorObject.transform.localScale = new Vector3(range * 2, 0.01f, range * 2);
    }

    public void ShowRange()
    {
        if (rangeIndicatorObject == null) return;

        rangeIndicatorObject.SetActive(true);
        isVisible = true;

        UpdateRangeScale();
    }

    public void HideRange()
    {
        if (rangeIndicatorObject != null)
            rangeIndicatorObject.SetActive(false);
        isVisible = false;
    }

    public void ToggleRange()
    {
        if (isVisible) HideRange();
        else ShowRange();
    }

    // Mouse hover is handled by TowerManager to avoid conflicts
    // Keeping these methods commented out for reference
    /*
    void OnMouseEnter()
    {
        ShowRange(RangeIndicatorType.Hover);
    }

    void OnMouseExit()
    {
        HideRange();
    }
    */

    void OnDestroy()
    {
        if (rangeIndicatorObject != null)
            DestroyImmediate(rangeIndicatorObject);
    }

    [ContextMenu("Force Show Range")]
    private void DebugShowRange()
    {
        ShowRange();
        Debug.Log("Force showing range - Check if red cylinder appears");
    }

    [ContextMenu("Force Hide Range")]
    private void DebugHideRange()
    {
        HideRange();
        Debug.Log("Force hiding range");
    }

    [ContextMenu("Log Range Info")]
    private void DebugLogInfo()
    {
        Debug.Log($"Tower range: {(tower != null ? tower.AttackRange.ToString() : "NULL")}");
        Debug.Log($"Prefab assigned: {(rangeIndicatorPrefab != null ? "YES" : "NO")}");
        Debug.Log($"Instance created: {(rangeIndicatorObject != null ? "YES" : "NO")}");
        Debug.Log($"Is visible: {isVisible}");
    }
}

