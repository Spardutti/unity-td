using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TowerUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button placeTowerButton;
    [SerializeField] private Button cancelPlacementButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI towerCountText;

    [Header("Auto-Creation Settings")]
    [SerializeField] private bool autoCreateUI = true;

    private TowerManager towerManager;

    void Awake()
    {
        towerManager = FindFirstObjectByType<TowerManager>();
    }

    void Start()
    {
        if (autoCreateUI && placeTowerButton == null)
        {
            CreateSimpleUI();
        }

        SetupUI();
    }

    void Update()
    {
        UpdateUI();
    }

    private void CreateSimpleUI()
    {
        // Find existing canvas or create one
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("TowerUIManager: No Canvas found for UI creation");
            return;
        }

        // Create Place Tower button
        GameObject buttonObj = new GameObject("PlaceTowerButton");
        buttonObj.transform.SetParent(canvas.transform, false);

        placeTowerButton = buttonObj.AddComponent<Button>();
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.8f, 0.2f, 0.8f); // Green

        // Position the button
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0, 1);
        buttonRect.anchorMax = new Vector2(0, 1);
        buttonRect.anchoredPosition = new Vector2(100, -50);
        buttonRect.sizeDelta = new Vector2(150, 40);

        // Add button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Place Tower (T)";
        buttonText.fontSize = 16;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Create status text
        GameObject statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(canvas.transform, false);

        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "Ready";
        statusText.fontSize = 18;
        statusText.color = Color.white;

        RectTransform statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 1);
        statusRect.anchorMax = new Vector2(0, 1);
        statusRect.anchoredPosition = new Vector2(100, -100);
        statusRect.sizeDelta = new Vector2(300, 30);

        // Create tower count text
        GameObject countObj = new GameObject("TowerCountText");
        countObj.transform.SetParent(canvas.transform, false);

        towerCountText = countObj.AddComponent<TextMeshProUGUI>();
        towerCountText.text = "Towers: 0";
        towerCountText.fontSize = 16;
        towerCountText.color = Color.white;

        RectTransform countRect = countObj.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0, 1);
        countRect.anchorMax = new Vector2(0, 1);
        countRect.anchoredPosition = new Vector2(100, -130);
        countRect.sizeDelta = new Vector2(200, 25);

        Debug.Log("TowerUIManager: Created simple UI");
    }

    private void SetupUI()
    {
        if (placeTowerButton != null && towerManager != null)
        {
            placeTowerButton.onClick.AddListener(OnPlaceTowerButtonClicked);
        }

        if (cancelPlacementButton != null && towerManager != null)
        {
            cancelPlacementButton.onClick.AddListener(OnCancelButtonClicked);
        }
    }

    private void UpdateUI()
    {
        if (towerManager == null) return;

        // Update status text
        if (statusText != null)
        {
            if (towerManager.IsPlacementMode)
            {
                statusText.text = "Click to place tower, ESC to cancel";
                statusText.color = Color.yellow;
            }
            else
            {
                statusText.text = "Press T or button to place towers";
                statusText.color = Color.white;
            }
        }

        // Update tower count
        if (towerCountText != null)
        {
            towerCountText.text = $"Towers: {towerManager.ActiveTowerCount}";
        }

        // Update button text
        if (placeTowerButton != null)
        {
            TextMeshProUGUI buttonText = placeTowerButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = towerManager.IsPlacementMode ? "Placing..." : "Place Tower (T)";
            }

            // Change button color based on mode
            Image buttonImage = placeTowerButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = towerManager.IsPlacementMode ?
                    new Color(0.8f, 0.8f, 0.2f, 0.8f) : // Yellow when placing
                    new Color(0.2f, 0.8f, 0.2f, 0.8f);  // Green when ready
            }
        }
    }

    private void OnPlaceTowerButtonClicked()
    {
        if (towerManager != null)
        {
            towerManager.TogglePlacementMode();
        }
    }

    private void OnCancelButtonClicked()
    {
        if (towerManager != null)
        {
            towerManager.CancelPlacement();
        }
    }

    // Public method to show tower info (for future use)
    public void ShowTowerInfo(Tower tower)
    {
        if (statusText != null && tower != null)
        {
            statusText.text = tower.GetTowerInfo();
        }
    }
}