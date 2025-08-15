using UnityEngine;

public class TowerRangeIndicator : MonoBehaviour
{
    [Header("Range Visualization")]
    [SerializeField] private Color normalRangeColor = new Color(0f, 1f, 0f, 0.3f);
    [SerializeField] private Color placementRangeColor = new Color(1f, 1f, 0f, 0.3f);
    [SerializeField] private Color hoverRangeColor = new Color(0f, 0.5f, 1f, 0.4f);
    [SerializeField] private int circleSegments = 32;

    private GameObject rangeIndicatorObject;
    private LineRenderer lineRenderer;
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
        // Create range indicator object
        rangeIndicatorObject = new GameObject("Range Indicator");
        rangeIndicatorObject.transform.SetParent(transform);
        rangeIndicatorObject.transform.localPosition = new Vector3(0, 0.1f, 0);

        // Setup LineRenderer
        lineRenderer = rangeIndicatorObject.AddComponent<LineRenderer>();
        lineRenderer.material = CreateSimpleMaterial();
        lineRenderer.startColor = normalRangeColor;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = circleSegments + 1;
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        UpdateCirclePositions();
    }

    private Material CreateSimpleMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = normalRangeColor;
        return mat;
    }

    private void UpdateCirclePositions()
    {
        if (lineRenderer == null || tower == null) return;

        float range = tower.AttackRange;
        Vector3[] positions = new Vector3[circleSegments + 1];

        for (int i = 0; i <= circleSegments; i++)
        {
            float angle = (float)i / circleSegments * 2f * Mathf.PI;
            positions[i] = new Vector3(
                Mathf.Cos(angle) * range,
                0,
                Mathf.Sin(angle) * range
            );
        }

        lineRenderer.SetPositions(positions);
    }

    public void ShowRange(RangeIndicatorType type = RangeIndicatorType.Normal)
    {
        if (rangeIndicatorObject == null) return;

        rangeIndicatorObject.SetActive(true);
        isVisible = true;

        Color targetColor = type switch
        {
            RangeIndicatorType.Placement => placementRangeColor,
            RangeIndicatorType.Hover => hoverRangeColor,
            _ => normalRangeColor
        };

        lineRenderer.startColor = targetColor;
        UpdateCirclePositions();
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

    // For mouse hover - attach this to a collider
    void OnMouseEnter()
    {
        ShowRange(RangeIndicatorType.Hover);
    }

    void OnMouseExit()
    {
        HideRange();
    }

    void OnDestroy()
    {
        if (rangeIndicatorObject != null)
            DestroyImmediate(rangeIndicatorObject);
    }
}

public enum RangeIndicatorType
{
    Normal,
    Placement,
    Hover
}