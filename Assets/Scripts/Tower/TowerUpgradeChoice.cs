using UnityEngine;

[CreateAssetMenu(fileName = "New Tower Upgrade Choice", menuName = "Tower Defense/Tower Upgrade Choice")]
public class TowerUpgradeChoice : ScriptableObject
{
    [Header("Basic Information")]
    public string upgradeName = "Damage Boost";
    [TextArea(2, 3)]
    public string description = "Increase tower damage by 25%";
    public Sprite upgradeIcon;

    [Header("Stat Modifiers")]
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float rangeMultiplier = 1f;
    [SerializeField] private float fireRateMultiplier = 1f;
    [SerializeField] private float splashRadiusMultiplier = 1f;

    [Header("Flat Bonuses")]
    [SerializeField] private float damageBonus = 0f;
    [SerializeField] private float rangeBonus = 0f;
    [SerializeField] private float fireRateBonus = 0f;
    [SerializeField] private float splashRadiusBonus = 0f;

    [Header("Special Properties")]
    [SerializeField] private bool grantsAreaDamage = false;
    [SerializeField] private bool grantsPiercing = false;
    [SerializeField] private AttackType newAttackType;
    [SerializeField] private bool changesAttackType = false;

    [Header("Visual Effects")]
    [SerializeField] private Material upgradeMaterial;
    [SerializeField] private GameObject upgradeParticleEffect;
    [SerializeField] private Color upgradeGlowColor;

    // Public accessors
    public float DamageMultiplier => damageMultiplier;
    public float RangeMultiplier => rangeMultiplier;
    public float FireRateMultiplier => fireRateMultiplier;
    public float SplashRadiusMultiplier => splashRadiusMultiplier;

    public float DamageBonus => damageBonus;
    public float RangeBonus => rangeBonus;
    public float FireRateBonus => fireRateBonus;
    public float SplashRadiusBonus => splashRadiusBonus;

    public bool GrantsAreaDamage => grantsAreaDamage;
    public bool GrantsPiercing => grantsPiercing;
    public AttackType NewAttackType => newAttackType;
    public bool ChangesAttackType => changesAttackType;

    public Material UpgradeMaterial => upgradeMaterial;
    public GameObject UpgradeParticleEffect => upgradeParticleEffect;
    public Color UpgradeGlowColor => upgradeGlowColor;

    public string GetFormattedDescription()
    {
        string result = description;

        // Add stat changes to description
        if (damageMultiplier != 1f)
        {
            float percent = (damageMultiplier - 1f) * 100f;
            result += $"\n+{percent:F0}% Damage";
        }

        if (rangeMultiplier != 1f)
        {
            float percent = (rangeMultiplier - 1f) * 100f;
            result += $"\n+{percent:F0}% Range";
        }

        if (fireRateMultiplier != 1f)
        {
            float percent = (fireRateMultiplier - 1f) * 100f;
            result += $"\n+{percent:F0}% Fire Rate";
        }

        return result;
    }
}
