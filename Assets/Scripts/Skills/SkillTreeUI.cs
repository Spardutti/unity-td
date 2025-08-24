using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

public class SkillTreeUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform skillNodesParent;
    [SerializeField] private GameObject skillNodePrefab;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Tree Info Display")]
    [SerializeField] private TextMeshProUGUI treeNameText;
    [SerializeField] private TextMeshProUGUI treeDescriptionText;
    [SerializeField] private Image treeIconImage;

    [Header("Resource Display")]
    [SerializeField] private TextMeshProUGUI skillPointsText;

    [Header("Layout Settings")]
    [SerializeField] private float nodeSpacingX = 150f;
    [SerializeField] private float nodeSpacingY = 150f;
    [SerializeField] private float tierSpacingY = 200f;
    [SerializeField] private bool autoFitContent = true;

    [Header("Connection Lines")]
    [SerializeField] private GameObject connectionLinePrefab;
    [SerializeField] private Transform connectionsParent;
    [SerializeField] private Color connectionLineColor = Color.white;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private SkillTreeData currentSkillTree;
    private Dictionary<string, SkillNodeUI> skillNodes = new Dictionary<string, SkillNodeUI>();
    private List<GameObject> connectionLines = new List<GameObject>();

    private void Awake()
    {
        // Ensure we have required components
        if (skillNodesParent == null)
        {
            skillNodesParent = transform;
        }

        if (connectionsParent == null)
        {
            connectionsParent = skillNodesParent;
        }
    }

    private void OnEnable()
    {
        // Subscribe to skill manager events
        SkillManager.OnSkillUnlocked += OnSkillUnlocked;
        SkillManager.OnSkillLocked += OnSkillLocked;
        SkillManager.OnSkillPointsChanged += OnSkillPointsChanged;
        SkillManager.OnSkillTreesUpdated += OnSkillTreesUpdated;
    }

    private void OnDisable()
    {
        // Unsubscribe from skill manager events
        SkillManager.OnSkillUnlocked -= OnSkillUnlocked;
        SkillManager.OnSkillLocked -= OnSkillLocked;
        SkillManager.OnSkillPointsChanged -= OnSkillPointsChanged;
        SkillManager.OnSkillTreesUpdated -= OnSkillTreesUpdated;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateResourceDisplay();
    }

    public void SetSkillTree(SkillTreeData skillTree)
    {
        if (skillTree == null)
        {
            Debug.LogWarning("SkillTreeUI.SetSkillTree: skillTree is null");
            return;
        }

        currentSkillTree = skillTree;
        GenerateSkillTreeUI();
        UpdateTreeInfoDisplay();

        if (debugMode)
        {
            Debug.Log("SkillTreeUI.SetSkillTree: SkillTree set to " + skillTree.name);
        }
    }

    private void GenerateSkillTreeUI()
    {
        if (debugMode)
        {
            Debug.Log("SkillTreeUI: SkillTree set to " + currentSkillTree.name);
        }
        if (currentSkillTree == null)
        {
            Debug.LogError("SkillTreeUI: currentSkillTree is null");
            return;
        }
        ;

        if (debugMode)
        {
            Debug.Log($"SkillTreeUI: Current skill tree has {currentSkillTree.skills?.Length ?? 0} skills");
            Debug.Log($"SkillTreeUI: skillNodePrefab is {(skillNodePrefab != null ? "assigned" : "NULL")}");
            Debug.Log($"SkillTreeUI: skillNodesParent is {(skillNodesParent != null ? "assigned" : "NULL")}");

        }

        ClearExistingUI();

        // Group skills by tier for layout
        var skillsByTier = GroupSkillsByTier();
        if (debugMode)
        {
            Debug.Log($"SkillTreeUI: Grouped into {skillsByTier.Count} tiers");
        }

        // Create skill nodes
        CreateSkillNodes(skillsByTier);
        if (debugMode)
        {
            Debug.Log("SkillTreeUI: CreateSkillNodes completed");
        }

        CreateConnectionLines();
        if (debugMode)
        {
            Debug.Log("SkillTreeUI: CreateConnectionLines completed");
        }

        if (autoFitContent && scrollRect != null)
        {
            AdjustContentSize();
        }

        UpdateAllNodeStates();
        if (debugMode)
        {
            Debug.Log("SkillTreeUI: GenerateSkillTreeUI completed");
        }
    }

    private Dictionary<int, List<SkillData>> GroupSkillsByTier()
    {
        var skillsByTier = new Dictionary<int, List<SkillData>>();

        foreach (var skill in currentSkillTree.skills)
        {
            if (skill == null) continue;

            int tier = currentSkillTree.GetSkillTier(skill);

            if (!skillsByTier.ContainsKey(tier))
            {
                skillsByTier[tier] = new List<SkillData>();
            }

            skillsByTier[tier].Add(skill);
        }

        return skillsByTier;
    }


    private void CreateSkillNodes(Dictionary<int, List<SkillData>> skillsByTier)
    {
        if (debugMode)
        {
            Debug.Log($"SkillTreeUI: CreateSkillNodes called with {skillsByTier.Count} tiers");
        }
        foreach (var tierPair in skillsByTier.OrderBy(x => x.Key))
        {
            int tier = tierPair.Key;
            List<SkillData> skills = tierPair.Value;
            if (debugMode)
            {
                Debug.Log($"SkillTreeUI: Processing tier {tier} with {skills.Count} skills");
            }

            float tierY = -tier * tierSpacingY;
            float startX = -(skills.Count - 1) * nodeSpacingX * 0.5f;

            for (int i = 0; i < skills.Count; i++)
            {
                SkillData skill = skills[i];
                Vector2 position = new Vector2(startX + (i * nodeSpacingX), tierY);

                if (debugMode)
                {
                    Debug.Log($"SkillTreeUI: Creating skill '{skill.skillName}' at tier {tier}, position {position}");
                }
                CreateSkillNode(skill, position);
            }
        }
        if (debugMode)
        {
            Debug.Log($"SkillTreeUI: Total skill nodes in dictionary: {skillNodes.Count}");
        }
    }

    private void CreateSkillNode(SkillData skill, Vector2 position)
    {
        if (skillNodePrefab == null)
        {
            Debug.LogError("SkillTreeUI.CreateSkillNode: skillNodePrefab is null");
            return;
        }

        GameObject nodeObj = Instantiate(skillNodePrefab, skillNodesParent);
        nodeObj.name = $"SkillNode_{skill.skillId}";

        RectTransform rectTransform = nodeObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = position;
        }

        SkillNodeUI nodeUI = nodeObj.GetComponent<SkillNodeUI>();
        if (nodeUI != null)
        {
            nodeUI.Initialize(skill);
            skillNodes[skill.skillId] = nodeUI;
        }
        else
        {
            Debug.LogError("SkillTreeUI.CreateSkillNode: nodeUI is null");
        }

    }

    private void CreateConnectionLines()
    {
        if (connectionLinePrefab == null)
        {
            Debug.LogError("SkillTreeUI.CreateConnectionLines: connectionLinePrefab is null");
            return;
        }

        foreach (var skill in currentSkillTree.skills)
        {
            if (skill == null || !skill.HasPrerequisites()) continue;

            foreach (string prereqId in skill.prerequisiteSkillIds)
            {
                if (skillNodes.TryGetValue(skill.skillId, out SkillNodeUI targetNode) && skillNodes.TryGetValue(prereqId, out SkillNodeUI sourceNode))
                {
                    CreateConnectionLine(sourceNode.transform, targetNode.transform);
                }
            }
        }
    }

    private void CreateConnectionLine(Transform fromNode, Transform toNode)
    {
        GameObject lineObj = Instantiate(connectionLinePrefab, connectionsParent);
        lineObj.name = $"ConnectionLine_{fromNode.name}_to_{toNode.name}";

        // Position line at midpoint
        Vector3 midpoint = (fromNode.position + toNode.position) * 0.5f;
        lineObj.transform.position = midpoint;

        // Set line color if it has an Image component
        Image lineImage = lineObj.GetComponent<Image>();
        if (lineImage != null)
        {
            lineImage.color = connectionLineColor;
        }

        // Adjust line rotation and scale to connect nodes
        Vector3 direction = toNode.position - fromNode.position;
        float distance = direction.magnitude;
        lineObj.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);

        RectTransform lineRect = lineObj.GetComponent<RectTransform>();
        if (lineRect != null)
        {
            lineRect.sizeDelta = new Vector2(lineRect.sizeDelta.x, distance);
        }

        connectionLines.Add(lineObj);
    }

    private void AdjustContentSize()
    {
        if (scrollRect == null || scrollRect.content == null) return;

        // Calculate bounds of all skill nodes 
        Bounds bounds = new Bounds();
        bool boundSet = false;

        foreach (var nodeUI in skillNodes.Values)
        {
            RectTransform rect = nodeUI.GetComponent<RectTransform>();
            if (rect != null)
            {
                if (!boundSet)
                {
                    bounds = new Bounds(rect.anchoredPosition, rect.sizeDelta);
                    boundSet = true;
                }
                else
                {
                    bounds.Encapsulate(new Bounds(rect.anchoredPosition, rect.sizeDelta));
                }
            }
        }

        if (boundSet)
        {
            // Add padding
            Vector2 padding = new Vector2(100f, 100f);
            Vector2 contentSize = new Vector2(bounds.size.x, bounds.size.y) + padding;
            scrollRect.content.sizeDelta = contentSize;
        }
    }

    private void ClearExistingUI()
    {
        // Clear skill nodes
        foreach (var nodeUI in skillNodes.Values)
        {
            if (nodeUI != null)
            {
                DestroyImmediate(nodeUI.gameObject);
            }
        }

        skillNodes.Clear();

        // Clear connection lines
        foreach (var line in connectionLines)
        {

            if (line != null)
            {
                DestroyImmediate(line);
            }
        }
        connectionLines.Clear();

    }

    private void UpdateTreeInfoDisplay()
    {
        if (currentSkillTree == null) return;

        if (treeNameText != null)
        {
            treeNameText.text = currentSkillTree.name;
        }

        if (treeDescriptionText != null)
        {
            treeDescriptionText.text = currentSkillTree.treeDescription;
        }

        if (treeIconImage != null && currentSkillTree.treeIcon != null)
        {
            treeIconImage.sprite = currentSkillTree.treeIcon;
        }
    }

    private void UpdateResourceDisplay()
    {
        if (SkillManager.Instance == null) return;

        if (skillPointsText != null)
        {
            skillPointsText.text = $"Skill Points: {SkillManager.Instance.SkillPoints}";
        }
    }

    private void UpdateAllNodeStates()
    {
        foreach (var nodeUI in skillNodes.Values)
        {
            nodeUI.UpdateVisualState();
        }
    }

    // Event Handlers
    private void OnSkillUnlocked(SkillData skill)
    {
        UpdateAllNodeStates();
    }

    private void OnSkillLocked(SkillData skill)
    {
        UpdateAllNodeStates();
    }

    private void OnSkillPointsChanged(int newAmount)
    {
        UpdateResourceDisplay();
        UpdateAllNodeStates();
    }



    private void OnSkillTreesUpdated()
    {
        UpdateAllNodeStates();
    }

    [ContextMenu("Regenerate UI")]
    private void RegenerateUI()
    {
        if (currentSkillTree != null)
        {
            GenerateSkillTreeUI();
        }
    }
}
