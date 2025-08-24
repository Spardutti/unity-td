using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.PlasticSCM.Editor.WebApi;

public class UpgradeMenuController : MonoBehaviour
{

    [Header("UI References")]
    [SerializeField] private GameObject upgradeMenuPanel;
    [SerializeField] private Button upgradesButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private SkillTreeUI skillTreeUI;
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Skill Trees")]
    [SerializeField] private SkillTreeData[] availableSkilLTrees;
    [SerializeField] private Transform skillTreeButtonsParent;
    [SerializeField] private GameObject skillTreeButtonPrefab;

    [Header("Navigation")]
    [SerializeField] private Button previousTreeButton;
    [SerializeField] private Button nextTreeButton;

    private List<Button> skillTreeButtons = new List<Button>();
    private int currentTreeIndex = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Setup initial state
        if (upgradeMenuPanel != null)
        {
            upgradeMenuPanel.SetActive(false);
        }

        // Setup button listener
        if (upgradesButton != null)
        {
            upgradesButton.onClick.AddListener(OpenUpgradeMenu);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseUpgradeMenu);
        }

        if (previousTreeButton != null)
        {
            previousTreeButton.onClick.AddListener(ShowPreviousTree);
        }

        if (nextTreeButton != null)
        {
            nextTreeButton.onClick.AddListener(ShowNextTree);
        }

        CreateSkillTreeButtons();

        // Show first available tree
        if (availableSkilLTrees != null && availableSkilLTrees.Length > 0)
        {
            ShowSkillTree(0);
        }
    }


    private void CreateSkillTreeButtons()
    {
        if (skillTreeButtonsParent == null || skillTreeButtonPrefab == null || availableSkilLTrees == null) return;

        // Clear existing buttons
        foreach (Button button in skillTreeButtons)
        {
            if (button != null)
            {
                DestroyImmediate(button.gameObject);
            }
        }

        skillTreeButtons.Clear();

        // Create buttons for each skill tree
        for (int i = 0; i < availableSkilLTrees.Length; i++)
        {
            SkillTreeData skillTree = availableSkilLTrees[i];
            if (skillTree == null) continue;

            GameObject buttonObj = Instantiate(skillTreeButtonPrefab, skillTreeButtonsParent);
            Button button = buttonObj.GetComponent<Button>();

            if (button != null)
            {
                Text buttonText = button.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = skillTree.treeName;
                }

                int treeIndex = i; // closure
                button.onClick.AddListener(() => ShowSkillTree(treeIndex));

                skillTreeButtons.Add(button);
            }
        }

        UpdateNavigationButtons();
    }

    public void OpenUpgradeMenu()
    {
        if (upgradeMenuPanel != null)
        {
            // Hide main menu
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);

            upgradeMenuPanel.SetActive(true);

            // Pause game if needed
            Time.timeScale = 0f;

            // Refresh current skill tree display
            if (currentTreeIndex >= 0 && currentTreeIndex < availableSkilLTrees.Length)
            {
                ShowSkillTree(currentTreeIndex);
            }
        }
    }

    public void CloseUpgradeMenu()
    {
        if (upgradeMenuPanel != null)
        {
            upgradeMenuPanel.SetActive(false);


            // Show main menu again
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(true);

            // Resume game
            Time.timeScale = 1f;
        }
    }

    private void ShowSkillTree(int treeIndex)
    {
        if (availableSkilLTrees == null || treeIndex < 0 || treeIndex >= availableSkilLTrees.Length) return;

        currentTreeIndex = treeIndex;

        if (skillTreeUI != null && availableSkilLTrees[treeIndex] != null)
        {
            skillTreeUI.SetSkillTree(availableSkilLTrees[treeIndex]);
        }

        UpdateNavigationButtons();
        UpdateSkillTreeButtonsHighlights();
    }

    private void ShowPreviousTree()
    {
        if (availableSkilLTrees == null || availableSkilLTrees.Length <= 1) return;

        currentTreeIndex--;
        if (currentTreeIndex < 0)
        {
            currentTreeIndex = availableSkilLTrees.Length - 1;
        }

        ShowSkillTree(currentTreeIndex);
    }

    private void ShowNextTree()
    {
        if (availableSkilLTrees == null || availableSkilLTrees.Length <= 1) return;
        currentTreeIndex++;

        if (currentTreeIndex >= availableSkilLTrees.Length)
        {
            currentTreeIndex = 0;
        }

        ShowSkillTree(currentTreeIndex);
    }

    private void UpdateNavigationButtons()
    {
        bool hasMultipleTrees = availableSkilLTrees != null && availableSkilLTrees.Length > 1;

        if (previousTreeButton != null)
        {
            previousTreeButton.gameObject.SetActive(hasMultipleTrees);
        }

        if (nextTreeButton != null)
        {
            nextTreeButton.gameObject.SetActive(hasMultipleTrees);
        }
    }

    private void UpdateSkillTreeButtonsHighlights()
    {
        for (int i = 0; i < skillTreeButtons.Count; i++)
        {
            Button button = skillTreeButtons[i];
            if (button != null)
            {
                // Highlight current tree
                ColorBlock colors = button.colors;
                colors.normalColor = (i == currentTreeIndex) ? Color.yellow : Color.white;
                button.colors = colors;
            }
        }
    }

    private void OnDestroy()
    {
        // clean up listeners
        if (upgradesButton != null)
        {
            upgradesButton.onClick.RemoveAllListeners();
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
        }

        foreach (Button button in skillTreeButtons)
        {
            if (button != null)
                button.onClick.RemoveAllListeners();
        }
    }

    [ContextMenu("Open Upgrade Menu")]
    private void TestOpenMenu()
    {
        OpenUpgradeMenu();
    }

    [ContextMenu("Close Upgrade Menu")]
    private void TestCloseMenu()
    {
        CloseUpgradeMenu();
    }
}
