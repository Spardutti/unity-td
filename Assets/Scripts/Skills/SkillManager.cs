using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Unity.VisualScripting;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    [Header("Skill Trees")]
    [SerializeField] private SkillTreeData[] availableSkillTrees;

    [Header("Player Resources")]
    [SerializeField] private int skillPoints = 0;

    [Header("Debug")]
    [SerializeField] private bool enableDebug = true;

    // Events
    public static event Action<SkillData> OnSkillUnlocked;
    public static event Action<SkillData> OnSkillLocked;
    public static event Action<int> OnSkillPointsChanged;
    public static event Action OnSkillTreesUpdated;

    // Runtime data
    private HashSet<string> unlockedSkillIds = new HashSet<string>();
    private Dictionary<string, SkillTreeData> skillTreeLookup = new Dictionary<string, SkillTreeData>();
    private Dictionary<string, SkillData> allSkillsLookup = new Dictionary<string, SkillData>();

    public int SkillPoints => skillPoints;
    public IReadOnlyCollection<string> UnlockedSkillIds => unlockedSkillIds;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSkillTrees();
            LoadProgress();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSkillTrees()
    {

        if (enableDebug)
            Debug.Log($"SkillManager: Starting InitializeSkillTrees - Available trees: {availableSkillTrees?.Length ?? 0}");

        skillTreeLookup.Clear();
        allSkillsLookup.Clear();

        if (availableSkillTrees == null)
        {
            if (enableDebug)
                Debug.LogWarning("SkillManager: availableSkillTrees is null");
            return;
        }

        foreach (var skillTree in availableSkillTrees)
        {
            if (skillTree == null)
            {
                if (enableDebug)
                    Debug.LogWarning("SkillManager: Found null skill tree in array");
                continue;
            }

            if (enableDebug)
                Debug.Log($"SkillManager: Processing skill tree '{skillTree.treeName}' with {skillTree.skills?.Length ?? 0} skills");


            skillTreeLookup[skillTree.treeId] = skillTree;

            if (skillTree.skills != null)
            {
                foreach (var skill in skillTree.skills)
                {
                    if (skill != null)
                    {
                        allSkillsLookup[skill.skillId] = skill;
                        if (enableDebug)
                            Debug.Log($"SkillManager: Registered skill '{skill.skillName}' ({skill.skillId})");

                    }
                    else if (enableDebug)
                    {
                        Debug.LogWarning("SkillManager: Found null skill in skill tree");
                    }
                }
            }
            // Validate the skill tree
            bool isValid = skillTree.ValidateSkillTree();
            if (enableDebug)
                Debug.Log($"SkillManager: Skill tree '{skillTree.treeName}' validation: {(isValid ? "PASSED" : "FAILED")}");

        }

        if (enableDebug)
            Debug.Log($"SkillManager: Initialization complete - {skillTreeLookup.Count} trees, {allSkillsLookup.Count} skills total");


        UpdateSkillAvailability();
        OnSkillTreesUpdated?.Invoke();
    }

    public bool CanUnlockSkill(string skillId)
    {
        if (!allSkillsLookup.TryGetValue(skillId, out SkillData skill)) return false;

        if (skill.IsUnlocked) return false;

        // check resources
        if (skillPoints < skill.skillPointCost) return false;

        // check prerequisites
        return ArePrerequisitesMet(skill);
    }

    public bool UnlockSkill(string skillId)
    {
        if (!CanUnlockSkill(skillId))
        {
            if (enableDebug) Debug.LogWarning($"Cannot unlock skill {skillId}!");
            return false;
        }

        SkillData skill = allSkillsLookup[skillId];

        // spend resources
        AddSkillPoints(-skill.skillPointCost);

        // unlock skill
        unlockedSkillIds.Add(skillId);
        skill.SetUnlocked(true);

        if (enableDebug) Debug.Log($"Unlocked skill {skill.skillName}!");

        UpdateSkillAvailability();
        OnSkillUnlocked?.Invoke(skill);

        SaveProgress();
        return true;
    }

    public bool LockSkill(string skillId)
    {
        if (!allSkillsLookup.TryGetValue(skillId, out SkillData skill)) return false;

        if (!skill.IsUnlocked) return false;

        // Check if any unlocked skills depend on this one
        foreach (var unlockedId in unlockedSkillIds)
        {
            if (unlockedId == skillId) continue;

            if (allSkillsLookup.TryGetValue(unlockedId, out SkillData dependentSkill))
            {
                if (dependentSkill.prerequisiteSkillIds != null && dependentSkill.prerequisiteSkillIds.Contains(skillId))
                {
                    if (enableDebug) Debug.LogWarning($"Cannot lock skill {skill.skillName} because it is a prerequisite of {dependentSkill.skillName}!");
                    return false;

                }
            }
        }

        // refund resources
        AddSkillPoints(skill.skillPointCost);

        // lock skill
        unlockedSkillIds.Remove(skillId);
        skill.SetUnlocked(false);

        if (enableDebug) Debug.Log($"Locked skill {skill.skillName}!");

        UpdateSkillAvailability();
        OnSkillLocked?.Invoke(skill);

        SaveProgress();
        return true;

    }

    private bool ArePrerequisitesMet(SkillData skill)
    {
        if (!skill.HasPrerequisites()) return true;

        foreach (string prereqId in skill.prerequisiteSkillIds)
        {
            if (!unlockedSkillIds.Contains(prereqId))
            {
                return false;
            }
        }
        return true;
    }

    public void UpdateSkillAvailability()
    {
        foreach (var skill in allSkillsLookup.Values)
        {
            bool canUnlock = !skill.IsUnlocked && skillPoints >= skill.skillPointCost && ArePrerequisitesMet(skill);
            skill.SetCanUnlock(canUnlock);
        }
    }

    private void AddSkillPoints(int amount)
    {
        skillPoints = Mathf.Max(0, skillPoints + amount);
        UpdateSkillAvailability();
        OnSkillPointsChanged?.Invoke(skillPoints);

    }



    public SkillTreeData GetSkillTree(string treeId)
    {
        skillTreeLookup.TryGetValue(treeId, out SkillTreeData skill);
        return skill;
    }

    public SkillData GetSkill(string skillId)
    {
        allSkillsLookup.TryGetValue(skillId, out SkillData skill);
        return skill;
    }

    public SkillTreeData[] GetAllSkillTrees()
    {
        return availableSkillTrees;
    }

    public float GetModifierValue(StatType statType, TowerData towerType = null)
    {
        float totalModifier = 0f;

        foreach (string skillId in unlockedSkillIds)
        {
            if (allSkillsLookup.TryGetValue(skillId, out SkillData skill))
            {
                // Apply generic skill or tower-specific skill modifiers that match the stat type
                if (skill.IsGenericSkill || (skill.IsTowerSpecific && skill.targetTowerType == towerType))
                {
                    totalModifier += skill.GetModifierValue(statType);
                }
            }
        }

        return totalModifier;
    }

    public bool IsSkillUnlocked(string skillId)
    {
        return unlockedSkillIds.Contains(skillId);
    }

    public void SaveProgress()
    {
        SkillPersistenceData saveData = new SkillPersistenceData(skillPoints, unlockedSkillIds);
        SkillSaveSystem.SaveSkillProgress(saveData);

        if (enableDebug) Debug.Log($"Saved skill progress!");
    }

    public void LoadProgress()
    {
        SkillPersistenceData loadData = SkillSaveSystem.LoadSkillProgress();

        // Aplly loaded data
        skillPoints = loadData.skillPoints;

        // clear current unlocked skills
        unlockedSkillIds.Clear();
        foreach (var skill in allSkillsLookup.Values)
        {
            skill.SetUnlocked(false);
        }

        // Apply unlocked skills from save data
        foreach (string skillId in loadData.unlockedSkillIds)
        {
            if (allSkillsLookup.TryGetValue(skillId, out SkillData skill))
            {
                unlockedSkillIds.Add(skillId);
                skill.SetUnlocked(true);
            }
            else if (enableDebug)
            {
                Debug.LogWarning($"Could not find skill {skillId} in skill tree data!");
            }
        }

        UpdateSkillAvailability();

        // trigger events to udpate UI
        OnSkillPointsChanged?.Invoke(skillPoints);
        OnSkillTreesUpdated?.Invoke();

        if (enableDebug)
            Debug.Log($"Skill progress loaded - Skills: {unlockedSkillIds.Count}, Points: {skillPoints}");

    }

    public void ResetProgress()
    {
        skillPoints = 0;

        // clear and reset skills
        foreach (string skillId in unlockedSkillIds.ToList())
        {
            if (allSkillsLookup.TryGetValue(skillId, out SkillData skill))
            {
                skill.SetUnlocked(false);
            }
        }

        unlockedSkillIds.Clear();

        UpdateSkillAvailability();

        SaveProgress();
        OnSkillPointsChanged?.Invoke(skillPoints);
        OnSkillTreesUpdated?.Invoke();

    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveProgress();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveProgress();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SaveProgress();
        }
    }


    [ContextMenu("Add 100 Skill Points")]
    private void AddTestSkillPoints()
    {
        AddSkillPoints(100);
    }

    [ContextMenu("Reset All Skills")]
    private void ResetAllSkills()
    {
        ResetProgress();
    }

    [ContextMenu("Save Progress")]
    private void SaveProgressManual()
    {
        SaveProgress();
    }

    [ContextMenu("Load Progress")]
    private void LoadProgressManual()
    {
        LoadProgress();
    }

    [ContextMenu("Delete Save Data")]
    private void DeleteSaveData()
    {
        SkillSaveSystem.DeleteSave();
        Debug.Log("Save data deleted - restart scene to see default values");
    }
}
