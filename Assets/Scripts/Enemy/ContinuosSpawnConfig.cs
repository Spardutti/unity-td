using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Continuous Spawning Config", menuName = "Tower Defense/Continuous Spawning Config")]
public class ContinuosSpawnConfig : ScriptableObject
{
    [Header("Basic Configuration")]
    [SerializeField] private string configName = "Default Continuos Spawn";
    [SerializeField] private float totalDuration = 1200f;

    [Header("Spawn Rate Over Time")]
    [SerializeField] private AnimationCurve spawnRateCurve = AnimationCurve.Linear(0f, 1f, 1200f, 10f);
    [SerializeField] private float baseSpawnInterval = 1f;
    [SerializeField] private float minimumSpawnInterval = 0.1f; // Never spawn faster than this

    [Header("Enemy Pool Evolution")]
    [SerializeField] private EnemySpawnEntry[] enemySpawnEntries;

    [Header("Difficulty Scaling")]
    [SerializeField] private AnimationCurve difficultyMultiplierCurve = AnimationCurve.Linear(0f, 1f, 1200f, 3f);
    [SerializeField] private AnimationCurve enemyHealthMultiplierCurve = AnimationCurve.Linear(0f, 1f, 1200f, 2f);

    [Header("Spawn Position Variation")]
    [SerializeField] private float spawnPositionVariation = 2f;
    [SerializeField] private bool useRandomSpawnDelay = true;
    [SerializeField] private Vector2 randomDelayRange = new Vector2(0f, 0.5f);

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    public string ConfigName => configName;
    public float TotalDuration => totalDuration;
    public float SpawnPositionVariation => spawnPositionVariation;
    public bool UseRandomSpawnDelay => useRandomSpawnDelay;
    public Vector2 RandomDelayRange => randomDelayRange;
    public bool ShowDebugInfo => showDebugInfo;

    public float GetSpawnRateAtTime(float currentTime)
    {
        float normalizedTime = Mathf.Clamp01(currentTime / totalDuration);
        float spawnRate = spawnRateCurve.Evaluate(normalizedTime);
        return spawnRate;
    }

    public float GetSpawnIntervalAtTime(float currentTime)
    {
        float spawnRate = GetSpawnRateAtTime(currentTime);
        if (spawnRate <= 0f) return float.MaxValue;

        float interval = baseSpawnInterval / spawnRate;
        return Mathf.Max(interval, minimumSpawnInterval);
    }

    public float GetDifficultyMultiplierAtTime(float currentTime)
    {
        float normalizedTime = Mathf.Clamp01(currentTime / totalDuration);
        return difficultyMultiplierCurve.Evaluate(normalizedTime);
    }

    public float GetEnemyHealthMultiplierAtTime(float currentTime)
    {
        float normalizedTime = Mathf.Clamp01(currentTime / totalDuration);
        return enemyHealthMultiplierCurve.Evaluate(normalizedTime);
    }

    public EnemySpawnEntry[] GetAvailableEnemiesAtTime(float currentTime)
    {
        if (enemySpawnEntries == null || enemySpawnEntries.Length == 0)
        {
            return new EnemySpawnEntry[0];
        }

        List<EnemySpawnEntry> availableEnemies = new List<EnemySpawnEntry>();

        foreach (EnemySpawnEntry entry in enemySpawnEntries)
        {
            if (entry.IsAvailableAtTime(currentTime))
            {
                availableEnemies.Add(entry);
            }
        }

        return availableEnemies.ToArray();
    }

    public GameObject GetRandomEnemyAtTime(float currentTime)
    {
        EnemySpawnEntry[] availableEnemies = GetAvailableEnemiesAtTime(currentTime);

        if (availableEnemies.Length == 0)
        {
            return null;
        }

        // calculate total weight
        float totalWeight = 0f;
        foreach (EnemySpawnEntry entry in availableEnemies)
        {
            totalWeight += entry.GetWeightAtTime(currentTime);
        }

        if (totalWeight <= 0f)
        {
            return availableEnemies[0].enemyPrefab; // fallback to first enemy
        }

        // select random enemy based on weight
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (EnemySpawnEntry entry in availableEnemies)
        {
            currentWeight += entry.GetWeightAtTime(currentTime);
            if (randomValue <= currentWeight)
            {
                return entry.enemyPrefab;
            }
        }

        // fallback
        return availableEnemies[availableEnemies.Length - 1].enemyPrefab;
    }

    public bool IsTimelineComplete(float currentTIme)
    {
        return currentTIme >= totalDuration;
    }

    public float GetTimelineProgress(float currentTIme)
    {
        return Mathf.Clamp01(currentTIme / totalDuration);
    }
}

[System.Serializable]
public class EnemySpawnEntry
{
    [Header("Enemy Configuration")]
    public GameObject enemyPrefab;
    public string enemyName = "Enemy";

    [Header("Availability Window")]
    public float unlockTime = 0f; // When this enemy first appears
    public float lockTime = -1f; // When this enemy stops spawning ( - 1 =  never)

    [Header("Spawn Weight Over Time")]
    public AnimationCurve spawnWeightCurve = AnimationCurve.Constant(0f, 1f, 1f);
    public float baseWeight = 1f;

    [Header("Special Properties")]
    public bool isEliteEnemy = false;
    public bool isBossEnemy = false;

    // Check if enemy is available at a given time
    public bool IsAvailableAtTime(float currentTIme)
    {
        if (currentTIme < unlockTime) return false;

        if (lockTime >= 0f && currentTIme >= lockTime) return false;

        return enemyPrefab != null;
    }

    // get the spawn weight at a given time
    public float GetWeightAtTime(float currentTime)
    {
        if (!IsAvailableAtTime(currentTime)) return 0f;

        float normalizedTime = currentTime / 1200f; // normalize to 20 minutes
        float currentValue = spawnWeightCurve.Evaluate(normalizedTime);

        return baseWeight * currentValue;
    }

}