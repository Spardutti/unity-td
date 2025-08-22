using System.Data;
using JetBrains.Annotations;
using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "Tower Defense/Skills/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Basic Information")]
    public string skillId = "";
    public string skillName = "New Skill";
    [TextArea(2, 4)]
    public string description = "Skill Description";
    public Sprite skillIcon;

    [Header("Prerequisites")]
    public string[] prerequisiteSkillIds = new string[0];

    [Header("Cost")]
    public int skillPointCost = 0;

    [Header("Tower Specificity")]
    [Tooltip("Leave null for generic skills that apply to all towers")]
    public TowerData targetTowerType;

    [Header("Stat Modifiers")]
    public SkillModifier[] modifiers = new SkillModifier[0];

    [Header("Unlock Status (Runtime)")]
    [SerializeField] private bool isUnlocked = false;
    [SerializeField] private bool canUnlock = false;

    public bool IsUnlocked => isUnlocked;
    public bool CanUnlock => canUnlock;
    public bool IsGenericSkill => targetTowerType == null;
    public bool IsTowerSpecific => targetTowerType != null;

    public void SetUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;
    }

    public void SetCanUnlock(bool canUnlock)
    {
        this.canUnlock = canUnlock;
    }

    public bool HasPrerequisites()
    {
        return prerequisiteSkillIds != null && prerequisiteSkillIds.Length > 0;
    }

    public float GetModifierValue(StatType statType)
    {
        foreach (var modifier in modifiers)
        {
            if (modifier.statType == statType)
            {
                return modifier.value;
            }
        }
        return 0f;
    }

    public bool HasModifier(StatType statType)
    {
        foreach (var modifier in modifiers)
        {
            if (modifier.statType == statType)
            {
                return true;
            }
        }

        return false;
    }

}
[System.Serializable]
public class SkillModifier
{
    public StatType statType = StatType.Damage;
    public ModifierType modifierType = ModifierType.Percentage;
    public float value = 0f;

    [Header("Display")]
    public string displayText = "+10% Damage";
}


public enum StatType
{
    Damage,
    Range,
    FireRate,
    TurretRotationSpeed,
    BuildCost,
    ExpGain,
    GoldDrop,
    SpecialDrop,
    SplashRadius,
    ProjectileSpeed
}

public enum ModifierType
{
    Flat,           // +5 damage
    Percentage,     // +15% damage
    Multiplier      // 1.5x damage
}