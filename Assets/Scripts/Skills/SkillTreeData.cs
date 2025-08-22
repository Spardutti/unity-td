using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.EditorTools;
using UnityEditor.ShaderGraph.Internal;

[CreateAssetMenu(fileName = "New Skill Tree", menuName = "Tower Defense/Skills/Skill Tree Data")]
public class SkillTreeData : ScriptableObject
{
    [Header("Tree Information")]
    public string treeId = "";
    public string treeName = "New Skill Tree";
    [TextArea(2, 3)]
    public string treeDescription = "Description of the skill tree";
    public Sprite treeIcon;

    [Header("Tree Type")]
    [Tooltip("Leave null for generic skill trees")]
    public TowerData associatedTowerType;

    [Header("Skills")]
    public SkillData[] skills = new SkillData[0];

    [Header("UI Layout Hints")]
    public int maxSkillsPerRow = 4;
    public Vector2 skillSpacing = new Vector2(12f, 100f);

    [Header("Tree Progression")]
    public bool requiresSequentialUnlock = false;
    [Tooltip("If true, skills must be unlocked in order they appear in the skills array")]

    public bool IsGenericTree => associatedTowerType == null;
    public bool IsTowerSpecificTree => associatedTowerType != null;
    public int SkillCount => skills != null ? skills.Length : 0;

    public SkillData GetSkillById(string skillId)
    {
        if (skills == null || string.IsNullOrEmpty(skillId)) return null;

        return System.Array.Find(skills, skill => skill.skillId == skillId);
    }

    public SkillData[] GetRootSkills()
    {
        if (skills == null) return new SkillData[0];

        return skills.Where(skill => skill != null && !skill.HasPrerequisites()).ToArray();
    }

    public SkillData[] GetSkillsWithPrerequisites(string prerequisiteSkillId)
    {
        if (skills == null || string.IsNullOrEmpty(prerequisiteSkillId)) return new SkillData[0];

        return skills.Where(skill => skill != null && skill.prerequisiteSkillIds != null && skill.prerequisiteSkillIds.Contains(prerequisiteSkillId)).ToArray();
    }

    public bool AreAllPrerequisitesMet(SkillData skill)
    {
        if (skill == null || !skill.HasPrerequisites()) return true;

        foreach (string prereqId in skill.prerequisiteSkillIds)
        {
            SkillData prerequisite = GetSkillById(prereqId);
            if (prerequisite == null || !prerequisite.IsUnlocked)
            {
                return false;
            }
        }
        return true;
    }

    public int GetSkillTier(SkillData skill)
    {
        if (skill == null || !skill.HasPrerequisites()) return 0;

        int maxPrereqTier = 0;
        foreach (string prereqId in skill.prerequisiteSkillIds)
        {
            SkillData prerequisite = GetSkillById(prereqId);
            if (prerequisite != null)
            {
                maxPrereqTier = Mathf.Max(maxPrereqTier, GetSkillTier(prerequisite));
            }
        }

        return maxPrereqTier + 1;
    }

    public List<SkillData> GetSkillsByTier()
    {
        if (skills == null) return new List<SkillData>();

        return skills.Where(skill => skill != null).OrderBy(skill => GetSkillTier(skill)).ThenBy(skill => System.Array.IndexOf(skills, skill)).ToList();
    }

    public bool ValidateSkillTree()
    {
        if (skills == null) return true;

        // Check for duplicate skill IDS
        var skillIds = skills.Where(skill => skill != null).Select(skill => skill.skillId).ToList();
        if (skillIds.Count != skillIds.Distinct().Count())
        {
            Debug.LogError($"Skill tree {treeName} contains duplicate skill IDs!");
            return false;
        }

        // Check for valid prerequisites
        foreach (var skill in skills)
        {
            if (skill == null) continue;

            foreach (string prereqId in skill.prerequisiteSkillIds)
            {
                if (GetSkillById(prereqId) == null)
                {
                    Debug.LogError($"SKill {skill.skillName} has an invalid prerequisite ID: {prereqId}!");
                    return false;
                }
            }

        }
        return true;
    }

    [ContextMenu("Validate SKill Tree")]
    public void ValidateInEditor()
    {
        if (ValidateSkillTree())
        {
            Debug.Log($"Skill tree {treeName} is valid!");
        }
    }
}
