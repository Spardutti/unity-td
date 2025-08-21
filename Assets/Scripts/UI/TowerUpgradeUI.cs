using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class TowerUpgradeUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private Button[] upgradeChoiceButtons = new Button[2];
    [SerializeField] private Image[] upgradeIcons = new Image[2];
    [SerializeField] private TextMeshProUGUI[] upgradeNames = new TextMeshProUGUI[2];
    [SerializeField] private TextMeshProUGUI[] upgradeDescriptions = new TextMeshProUGUI[2];

    [Header("Tower Info Display")]
    [SerializeField] private TextMeshProUGUI towerNameText;
    [SerializeField] private TextMeshProUGUI currentLevelText;
    [SerializeField] private TextMeshProUGUI currentXPText;

    [Header("Panel Settings")]
    [SerializeField] private bool pauseGameDuringUpgrade = true;
    [SerializeField] private float showAnimationDuration = 0.3f;

    private Tower currentTower;
    private TowerUpgradeChoice[] currentChoices = new TowerUpgradeChoice[2];
    private bool isShowing = false;

    public static TowerUpgradeUI Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

    }
    void Start()
    {
        // Setup buttons now that the panel is active
        SetupButtons();

        // Ensure panel is hidden on startup
        HideUpgradePanel();

        // Force panel inactive if it's somehow active
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }
    }

    private void SetupButtons()
    {
        if (upgradeChoiceButtons == null)
        {
            Debug.LogWarning("TowerUpgradeUI: upgradeChoiceButtons array is null - assign in Inspector");
            return;
        }

        if (upgradeChoiceButtons.Length < 2)
        {
            Debug.LogWarning("TowerUpgradeUI: upgradeChoiceButtons array needs at least 2 elements");
            return;
        }
        for (int i = 0; i < upgradeChoiceButtons.Length; i++)
        {
            int index = i; // Capture index for closure
            upgradeChoiceButtons[i].onClick.AddListener(() => SelectUpgrade(index));
        }
    }

    public void ShowUpgradePanel(Tower tower)
    {
        if (tower == null || !tower.IsReadyToLevelUp || isShowing) return;

        currentTower = tower;
        isShowing = true;

        if (pauseGameDuringUpgrade)
        {
            Time.timeScale = 0f;
        }

        GenerateUpgradeChoices();
        DisplayTowerInfo();
        DisplayUpgradeChoices();

        upgradePanel.SetActive(true);
    }

    private void GenerateUpgradeChoices()
    {
        if (currentTower?.TowerData?.UpgradeChoicePool == null || currentTower.TowerData.UpgradeChoicePool.Length < 2)
        {
            Debug.LogError("TowerUpgradeUI: No upgrade choices found");
            HideUpgradePanel();
            return;
        }

        // Get random choices
        var availableChoices = currentTower.TowerData.UpgradeChoicePool.Where(choice => choice != null).ToList();

        if (availableChoices.Count < 2)
        {
            Debug.LogError("TowerUpgradeUI: Not enough upgrade choices available");
            HideUpgradePanel();
            return;

        }

        // Randomly select 2 different choices
        var shuffled = availableChoices.OrderBy(x => Random.value).Take(2).ToArray();
        currentChoices[0] = shuffled[0];
        currentChoices[1] = shuffled[1];

        Debug.Log($"TowerUpgradeUI: Generated upgrade choices:");
        Debug.Log($"  Choice 0: Name='{currentChoices[0].upgradeName}', Description='{currentChoices[0].GetFormattedDescription()}'");
        Debug.Log($"  Choice 1: Name='{currentChoices[1].upgradeName}', Description='{currentChoices[1].GetFormattedDescription()}'");
        
        // Verify the upgrade names are not empty
        if (string.IsNullOrEmpty(currentChoices[0].upgradeName))
        {
            Debug.LogWarning("TowerUpgradeUI: Choice 0 has empty upgrade name!");
        }
        if (string.IsNullOrEmpty(currentChoices[1].upgradeName))
        {
            Debug.LogWarning("TowerUpgradeUI: Choice 1 has empty upgrade name!");
        }
    }

    private void DisplayTowerInfo()
    {
        if (currentTower == null) return;
        if (towerNameText != null)
        {
            towerNameText.text = currentTower.TowerData.towerName;
        }
        if (currentLevelText != null)
        {
            currentLevelText.text = $"Level {currentTower.CurrentLevel} -> {currentTower.CurrentLevel + 1}";
        }
        if (currentXPText != null)
        {
            int requiredXP = currentTower.TowerData.GetXpRequiredForLevel(currentTower.CurrentLevel + 1);
            currentXPText.text = $"XP: {currentTower.CurrentXP}/{requiredXP}";

        }
    }

    private void DisplayUpgradeChoices()
    {
        // Check if UI arrays are properly assigned
        if (upgradeNames == null || upgradeNames.Length < 2)
        {
            Debug.LogError("TowerUpgradeUI: upgradeNames array not properly assigned in Inspector!");
        }
        if (upgradeDescriptions == null || upgradeDescriptions.Length < 2)
        {
            Debug.LogError("TowerUpgradeUI: upgradeDescriptions array not properly assigned in Inspector!");
        }

        for (int i = 0; i < 2; i++)
        {
            if (currentChoices[i] == null)
            {
                Debug.LogWarning($"TowerUpgradeUI: Choice {i} is null");
                continue;
            }

            var choice = currentChoices[i];

            if (upgradeIcons != null && i < upgradeIcons.Length && upgradeIcons[i] != null)
            {
                upgradeIcons[i].sprite = choice.upgradeIcon;
                upgradeIcons[i].enabled = choice.upgradeIcon != null;
            }

            if (upgradeNames != null && i < upgradeNames.Length)
            {
                if (upgradeNames[i] != null)
                {
                    upgradeNames[i].text = choice.upgradeName;
                    Debug.Log($"TowerUpgradeUI: Set upgrade name {i} to '{choice.upgradeName}'");
                }
                else
                {
                    Debug.LogError($"TowerUpgradeUI: upgradeNames[{i}] TextMeshProUGUI component is not assigned in Inspector!");
                }
            }

            if (upgradeDescriptions != null && i < upgradeDescriptions.Length)
            {
                if (upgradeDescriptions[i] != null)
                {
                    upgradeDescriptions[i].text = choice.GetFormattedDescription();
                }
                else
                {
                    Debug.LogError($"TowerUpgradeUI: upgradeDescriptions[{i}] TextMeshProUGUI component is not assigned in Inspector!");
                }
            }
        }
    }

    private void SelectUpgrade(int choiceIndex)
    {
        if (currentTower == null || choiceIndex < 0 || choiceIndex >= currentChoices.Length) return;

        var selectedUpgrade = currentChoices[choiceIndex];
        if (selectedUpgrade == null) return;

        currentTower.ApplyUpgrade(selectedUpgrade);

        HideUpgradePanel();

    }

    public void HideUpgradePanel()
    {
        isShowing = false;
        currentTower = null;

        // Always restore time scale
        if (pauseGameDuringUpgrade && Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }

        // Always hide panel
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }
    }

    void OnDestroy()
    {
        // Ensure game isn't left paused
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }
}
