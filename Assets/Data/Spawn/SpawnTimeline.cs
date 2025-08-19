using System.IO.Enumeration;
using UnityEngine;

[CreateAssetMenu(fileName = "New Wave Timeline", menuName = "Tower Defense/Spawn Time Line")]
public class SpawnTimeLine : ScriptableObject
{
    [Header("Timeline Configuration")]
    public string timeLineName = "5 Minute Rush";
    [TextArea(2, 3)]
    public string waveDescription = "Continuous enemy spawning over 5 minutes";

    [Header("Game Duration")]
    public float totalGameTime = 300f; // 5 minutes in seconds
    public bool useInfiniteTime = false;

    [Header("Spawn Events")]
    public SpawnEvent[] spawnEvents;

    [Header("Background Spawning (Optional)")]
    public bool enableBackgroundSpawning = false;
    [ShowIf("enableBackgroundSpawning")]
    public float backgroundSpawnInterval = 10f;
    [ShowIf("enableBackgroundSpawning")]
    public int backgroundEnemyCount = 1;
    [ShowIf("enableBackgroundSpawning")]
    public GameObject backgroundEnemyPrefab;

    [Header("Difficulty Scaling")]
    public bool enableDifficultyScaling = false;
    [ShowIf("enableDifficultyScaling")]
    public AnimationCurve spawnRateMultiplier = AnimationCurve.Linear(0f, 1f, 1f, 2f);

    public float GetTimeLineProgress(float currentTIme)
    {
        if (useInfiniteTime) return 0f;
        return Mathf.Clamp01(currentTIme / totalGameTime);
    }

    public bool IsTimeLineComplete(float currentTime)
    {
        if (useInfiniteTime) return false;
        return currentTime >= totalGameTime;
    }

    public float GetSpawnRateMultiplierAtTime(float currentTime)
    {
        if (!enableDifficultyScaling) return 1f;
        float progress = GetTimeLineProgress(currentTime);
        return spawnRateMultiplier.Evaluate(progress);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}

[System.Serializable]
public class SpawnEvent
{
    [Header("Timing")]
    public float triggerTime = 0f;
    public string eventName = "Spawn Event";

    [Header("Enemy Spawning")]
    public GameObject enemyPrefab;
    public int enemyCount = 1;

    [Header("Spawn Pattern")]
    public SpawnPattern spawnPattern = SpawnPattern.Instant;
    [ShowIf("spawnPattern", SpawnPattern.Staggered)]
    public float spawnInterval = 0.5f; // time between enemies
    [ShowIf("spawnPattern", SpawnPattern.Burst)]
    public float burstDuration = 2f; // how long burst lasts

    [Header("Visual Feedback")]
    public bool showWarning = false;
    [ShowIf("showWarning")]
    public float warningTime = 3f; // warn the palyer x seconds before spawning
    [ShowIf("showWarning")]
    public string warningMessage = "Large enemy group incoming!";

    [Header("Audio")]
    public AudioClip spawnSound;
    public bool playSpawnEffect;
}

public enum SpawnPattern
{
    Instant,    // All enemies spawn at once
    Staggered,  // Enemies spawn with intervals
    Burst       // Fast spawning over a duration
}

public class ShowIfAttribute : PropertyAttribute
{
    public string conditionFieldName;
    public object conditionValue;

    public ShowIfAttribute(string conditionFieldName)
    {
        this.conditionFieldName = conditionFieldName;
    }

    public ShowIfAttribute(string conditionFieldName, object conditionValue)
    {
        this.conditionFieldName = conditionFieldName;
        this.conditionValue = conditionValue;
    }
}