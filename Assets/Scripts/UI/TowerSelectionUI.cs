using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TowerSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private Button archerButton;
    [SerializeField] private Button cannonButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button placeTowerButton;

    [Header("Tower Data")]
    [SerializeField] private TowerData archerTowerData;
    [SerializeField] private TowerData cannonTowerData;

    private TowerManager towerManager;

    void Awake()
    {
        towerManager = FindFirstObjectByType<TowerManager>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupButtons();
        HidePanel();
        UpdateButtonTexts();
    }

    private void SetupButtons()
    {
        if (archerButton != null)
        {
            archerButton.onClick.AddListener(() => SelectTower(archerTowerData));
        }

        if (cannonButton != null)
        {
            cannonButton.onClick.AddListener(() => SelectTower(cannonTowerData));
        }

        if (placeTowerButton != null)
        {
            placeTowerButton.onClick.AddListener(ShowPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePanel);
        }
    }

    private void SelectTower(TowerData towerData)
    {
        if (towerManager != null && towerData != null)
        {
            towerManager.StartPlacementMode(towerData);
            HidePanel();
            Debug.Log($"TowerSelectionUI: Selected tower {towerData.towerName}");
        }
    }

    private void UpdateButtonTexts()
    {
        if (archerButton != null && archerTowerData != null)
        {
            TextMeshProUGUI buttonText = archerButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = $"Archer - {archerTowerData.cost}$";
            }
        }

        if (cannonButton != null && cannonTowerData != null)
        {
            TextMeshProUGUI buttonText = cannonButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = $"Cannon - {cannonTowerData.cost}$";
            }
        }
    }

    public void ShowPanel()
    {
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(true);
        }
    }

    public void HidePanel()
    {
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }
    }
}
