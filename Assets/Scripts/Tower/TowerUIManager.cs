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

    private TowerManager towerManager;

    void Awake()
    {
        towerManager = FindFirstObjectByType<TowerManager>();
    }

    void Start()
    {
        ValidateUIReferences();
        SetupUI();
    }

    void Update()
    {
        UpdateUI();
    }

    private void ValidateUIReferences()
    {
        bool hasAllReferences = true;

        if (placeTowerButton == null)
        {
            Debug.LogWarning("TowerUIManager: Place Tower Button is not assigned!");
            hasAllReferences = false;
        }

        if (statusText == null)
        {
            Debug.LogWarning("TowerUIManager: Status Text is not assigned!");
            hasAllReferences = false;
        }

        if (towerCountText == null)
        {
            Debug.LogWarning("TowerUIManager: Tower Count Text is not assigned!");
            hasAllReferences = false;
        }

        if (!hasAllReferences)
        {
            Debug.LogError("TowerUIManager: Missing UI references! Please assign all UI elements in the inspector or create a UI prefab.");
        }
        else
        {
            Debug.Log("TowerUIManager: All UI references validated successfully.");
        }
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
                buttonText.text = "Buy Tower";
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


        TowerSelectionUI towerSelection = FindFirstObjectByType<TowerSelectionUI>(FindObjectsInactive.Include);

        if (towerSelection != null)
        {
            towerSelection.ShowPanel();
        }
        else
        {
            Debug.LogError("TowerSelectionUI component not found!");
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