using UnityEngine;

[CreateAssetMenu(fileName = "New Tower Data", menuName = "Tower Defense/Tower Data")]
public class TowerData : ScriptableObject
{
    [Header("Basic Information")]
    public string towerName = "Basic Tower";
    [TextArea(2, 4)]
    public string description = "Standard arrow tower";
    public Sprite towerIcon;

    [Header("Stats")]
    public float damage = 10f;
    public float range = 5f;
    public float fireRate = 1f;
    public int cost = 50;

    [Header("Attack Type")]
    public AttackType attackType = AttackType.Single;
    public TargetingMode defaultTargetingMode = TargetingMode.Closest;
    public float splashRadius = 0f;

    [Header("Upgrade System")]
    public int upgradeLevel = 1;
    public int maxUpgradeLevel = 3;
    public TowerData nextUpgrade;
    public int upgradeCost = 75;

    [Header("Prefab References")]
    public GameObject towerPrefab;
    public GameObject projectilePrefab;
    public GameObject muzzleFlashEffect;
    [SerializeField] private GameObject explosionEffectPrefab;


    [Header("Area Damage Settings")]
    [SerializeField] private bool enableDamageFalloff = true;
    [SerializeField] private float minimumDamagePercent = 0.5f; // 50% damage at splash edge


    [Header("Audio")]
    public AudioClip fireSound;
    public AudioClip placementSound;
    public AudioClip upgradeSound;
    [SerializeField] private AudioClip explosionSound;


    [Header("Visual Upgrades")]
    public Material[] levelMaterials;
    public float[] levelScales = { 1f, 1.1f, 1.2f }; // Size increase per level

    public string GetLevelDisplayName()
    {
        return $"{towerName} (Level {upgradeLevel})";
    }

    public float GetDamagePerSecond()
    {
        return damage * fireRate;
    }

    // Calculated properties
    public float FireCooldown => 1f / fireRate;
    public bool CanBeUpgraded => upgradeLevel < maxUpgradeLevel && nextUpgrade != null;
    public bool IsMaxLevel => upgradeLevel >= maxUpgradeLevel;

    public GameObject ExplosionEffectPrefab => explosionEffectPrefab;
    public AudioClip ExplosionSound => explosionSound;
    public bool EnableDamageFalloff => enableDamageFalloff;
    public float MinimumDamagePercent => minimumDamagePercent;
}

public enum AttackType
{
    Single,    // Your basic tower
    Area,      // Your cannon tower
    Pierce,    // Shoots through multiple enemies
    Chain      // Lightning/chain attacks
}
