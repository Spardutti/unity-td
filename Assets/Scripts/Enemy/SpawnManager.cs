using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.ShaderGraph.Internal;
using Mono.Cecil;

public class SpawnManager : MonoBehaviour
{
    [Header("Timeline COnfiguration")]
    [SerializeField] private SpawnTimeLine currentTimeLine;
    [SerializeField] private bool autoStartOnPlay = false;

    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;

    [Header("Continuos Spawning")]
    [SerializeField] private ContinuosSpawnConfig continuousSpawningConfig;
    [SerializeField] private bool enableContinuousSpawning = true;
    [SerializeField] private bool showContinuousSpawningDebug = false;

    [Header("Hybrid System Settings")]
    [SerializeField] private float continuousSpawnMultiplier = 1f; // global multiplier for continuous spawns
    [SerializeField] private bool pauseContinuousSpawningDuringEvents = false;

    [Header("UI References")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private TextMeshProUGUI gameTimeText;
    [SerializeField] private TextMeshProUGUI nextEventText;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showUpcomingEvents = true;

    // Game State
    private bool isGameActive = false;
    private float gameStartTime = 0f;
    private float currentGameTime = 0f;
    private List<bool> eventTriggered; // Track which events have been triggered
    private Coroutine backgroundSpawnCoroutine;
    private Coroutine gameTimerCoroutine;

    // Continuous Spawning State
    private Coroutine continuousSpawnCoroutine;
    private float lastContinuousSpawnTime = 0f;
    private float nextContinuousSpawnTime = 0f;
    private int totalContinuousEnemiesSpawned = 0;

    // Difficulty Scaling
    private float currentDifficultyMultiplier = 1f;
    private float currentEnemyHealthMultiplier = 1f;

    // Events
    public static System.Action<float> OnGameTimeChanged;
    public static System.Action OnGameStarted;
    public static System.Action OnGameEnded;
    public static System.Action<SpawnEvent> OnSpawnEventTriggered;

    // Properties
    public bool IsGameActive => isGameActive;
    public float CurrentGameTime => currentGameTime;
    public float TimeLineProgress => currentTimeLine?.GetTimeLineProgress(currentGameTime) ?? 0f;
    public bool IsTimeLineComplete => currentTimeLine != null && currentTimeLine.IsTimeLineComplete(currentGameTime);
    public bool IsContinuousSpawningActive => continuousSpawnCoroutine != null;
    public int TotalContinuousEnemiesSpawned => totalContinuousEnemiesSpawned;
    public float CurrentDifficultyMultiplier => currentDifficultyMultiplier;
    public ContinuosSpawnConfig ContinuousSpawnConfig => continuousSpawningConfig;

    void Awake()
    {
        if (enemySpawner == null)
        {
            enemySpawner = GetComponent<EnemySpawner>();
        }

        if (currentTimeLine == null)
        {
            Debug.LogError("No Timeline assigned to Spawn Manager");
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupUI();
        InitializeTimeline();

        if (autoStartOnPlay)
        {
            StartGame();
        }
    }

    private void SetupUI()
    {
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(StartGame);
        }
        UpdateUI();
    }

    private void InitializeTimeline()
    {
        if (currentTimeLine == null)
        {
            Debug.LogError("No Timeline assigned to Spawn Manager");
            return;

        }

        // initalize event tracking
        eventTriggered = new List<bool>();
        for (int i = 0; i < currentTimeLine.spawnEvents.Length; i++)
        {
            eventTriggered.Add(false);
        }

        if (showDebugLogs)
        {
            Debug.Log($"Initialized Timeline: {currentTimeLine.timeLineName}");
        }
    }

    public void StartGame()
    {
        if (isGameActive)
        {
            if (showDebugLogs)
            {
                Debug.Log("Game already active");
            }
            return;
        }

        if (currentTimeLine == null)
        {
            Debug.LogError("No Timeline assigned to Spawn Manager");
            return;

        }

        InitializeTimeline();

        isGameActive = true;
        gameStartTime = Time.time;
        currentGameTime = 0f;

        // Reset event tracking
        for (int i = 0; i < eventTriggered.Count; i++)
        {
            eventTriggered[i] = false;
        }

        // Start coroutines
        gameTimerCoroutine = StartCoroutine(GameTimerRoutine());
        // Start continuous spawning if enabled
        if (enableContinuousSpawning && continuousSpawningConfig != null)
        {
            StartContinuousSpawning();
        }

        if (currentTimeLine.enableBackgroundSpawning)
        {
            backgroundSpawnCoroutine = StartCoroutine(BackgroundSpawnRoutine());
        }

        OnGameStarted?.Invoke();
        if (showDebugLogs)
        {
            Debug.Log($"SpawnManager: Game Started");
        }

        UpdateUI();
    }

    private void StartContinuousSpawning()
    {
        if (continuousSpawnCoroutine != null)
        {
            StopCoroutine(continuousSpawnCoroutine);
        }

        continuousSpawnCoroutine = StartCoroutine(ContinuousSpawnRoutine());
        totalContinuousEnemiesSpawned = 0;

        if (showContinuousSpawningDebug)
        {
            Debug.Log("SpawnManager: Continuous Spawning Started");
        }
    }

    private IEnumerator ContinuousSpawnRoutine()
    {
        lastContinuousSpawnTime = 0f;

        while (isGameActive && !continuousSpawningConfig.IsTimelineComplete(currentGameTime))
        {
            // update difficulty multiplier
            currentEnemyHealthMultiplier = continuousSpawningConfig.GetDifficultyMultiplierAtTime(currentGameTime);
            currentEnemyHealthMultiplier *= continuousSpawningConfig.GetEnemyHealthMultiplierAtTime(currentGameTime);

            // check if we should pass continuos spawning during events
            bool shouldSpawn = true;
            if (pauseContinuousSpawningDuringEvents)
            {
                // check if any even is currently active
                shouldSpawn = !IsAnyEventCurrentlyActive();
            }

            if (shouldSpawn)
            {
                // calculate next spawn time
                float spawnInterval = continuousSpawningConfig.GetSpawnIntervalAtTime(currentGameTime);
                spawnInterval *= continuousSpawnMultiplier;

                // add random delay if enabled
                if (continuousSpawningConfig.UseRandomSpawnDelay)
                {
                    Vector2 delayRange = continuousSpawningConfig.RandomDelayRange;
                    float randomDelay = Random.Range(delayRange.x, delayRange.y);
                    spawnInterval += randomDelay;
                }

                nextContinuousSpawnTime = lastContinuousSpawnTime + spawnInterval;

                // check if it is time to spawn
                if (currentGameTime >= nextContinuousSpawnTime)
                {
                    SpawnContinuousEnemy();
                    lastContinuousSpawnTime = currentGameTime;
                }
            }
            yield return new WaitForSeconds(0.1f); // check every 100ms for smooth spawning
        }

        if (showContinuousSpawningDebug)
        {
            Debug.Log("SpawnManager: Continuous Spawning Completed");
        }
        continuousSpawnCoroutine = null;
    }

    private void SpawnContinuousEnemy()
    {
        if (enemySpawner == null || continuousSpawningConfig == null)
        {
            return;
        }

        // Select enemy type based on current time and weights
        GameObject enemyPrefab = continuousSpawningConfig.GetRandomEnemyAtTime(currentGameTime);
        if (enemyPrefab == null)
        {
            if (showContinuousSpawningDebug)
            {
                Debug.LogWarning("SpawnManager: No enemy available for continuous spawn");
            }
            return;
        }

        // Spawn enemy using spawner with the selected prefab and health multiplier
        GameObject spawnedEnemy = enemySpawner.SpawnSpecificEnemy(enemyPrefab, currentEnemyHealthMultiplier);
        
        if (spawnedEnemy != null)
        {
            totalContinuousEnemiesSpawned++;

            if (showContinuousSpawningDebug)
            {
                float spawnRate = continuousSpawningConfig.GetSpawnRateAtTime(currentGameTime);
                Debug.Log($"SpawnManager: Spawned continuous enemy {totalContinuousEnemiesSpawned} ({enemyPrefab.name}) at {currentGameTime:F1} with spawn rate {spawnRate:F2} and health multiplier {currentEnemyHealthMultiplier:F2}");
            }
        }
    }

    private bool IsAnyEventCurrentlyActive()
    {
        // Simple implementation - you can make this more sophisticated
        // For now, assume events are "active" for 5 seconds after triggering
        if (currentTimeLine == null || currentTimeLine.spawnEvents == null) return false;

        for (int i = 0; i < currentTimeLine.spawnEvents.Length; i++)
        {
            if (eventTriggered[i])
            {
                SpawnEvent spawnEvent = currentTimeLine.spawnEvents[i];
                float eventEndTime = spawnEvent.triggerTime + 5f; // assume duration is 5 seconds

                if (currentGameTime >= spawnEvent.triggerTime && currentGameTime < eventEndTime)
                {
                    return true;
                }
            }
        }
        return false;

    }
    public void StopGame()
    {
        if (!isGameActive) return;


        isGameActive = false;

        // stop coroutines
        if (gameTimerCoroutine != null)
        {
            StopCoroutine(gameTimerCoroutine);
            gameTimerCoroutine = null;

        }

        if (backgroundSpawnCoroutine != null)
        {
            StopCoroutine(backgroundSpawnCoroutine);
            backgroundSpawnCoroutine = null;
        }

        if (continuousSpawnCoroutine != null)
        {
            StopCoroutine(continuousSpawnCoroutine);
            continuousSpawnCoroutine = null;
        }

        OnGameEnded?.Invoke();

        if (showDebugLogs)
        {
            Debug.Log($"SpawnManager: Game Ended");
        }

        UpdateUI();
    }

    private IEnumerator GameTimerRoutine()
    {
        while (isGameActive)
        {
            currentGameTime = Time.time - gameStartTime;

            // Check spawn events
            CheckSpawnEvents();

            // check if timeline is complete
            if (IsTimeLineComplete)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"SpawnManager: Timeline Complete");
                }
                StopGame();
                break;
            }
            OnGameTimeChanged?.Invoke(currentGameTime);
            UpdateUI();
            yield return new WaitForSeconds(0.1f); // Update 10 times per second
        }
    }

    private void CheckSpawnEvents()
    {
        if (currentTimeLine.spawnEvents == null) return;

        for (int i = 0; i < currentTimeLine.spawnEvents.Length; i++)
        {
            SpawnEvent spawnEvent = currentTimeLine.spawnEvents[i];

            // Skip if already triggered
            if (eventTriggered[i]) continue;

            // Check if its time to trigger this event
            if (currentGameTime >= spawnEvent.triggerTime)
            {
                TriggerSpawnEvent(spawnEvent, 1);
                eventTriggered[i] = true;
            }
        }
    }

    private void TriggerSpawnEvent(SpawnEvent spawnEvent, int eventIndex)
    {
        if (enemySpawner == null)
        {
            Debug.LogError("SpawnManager: cannot trigger spawn event without EnemySpawner");
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log($"SpawnManager: Triggering Spawn Event: {spawnEvent.eventName}");
        }

        // Execute spam based on pattern
        switch (spawnEvent.spawnPattern)
        {
            case SpawnPattern.Instant:
                StartCoroutine(SpawnInstant(spawnEvent));
                break;
            case SpawnPattern.Staggered:
                StartCoroutine(SpawnStaggered(spawnEvent));
                break;
            case SpawnPattern.Burst:
                StartCoroutine(SpawnBurst(spawnEvent));
                break;

        }

        OnSpawnEventTriggered?.Invoke(spawnEvent);
    }

    private IEnumerator SpawnInstant(SpawnEvent spawnEvent)
    {
        for (int i = 0; i < spawnEvent.enemyCount; i++)
        {
            enemySpawner.SpawnEnemy();
        }

        yield return null;
    }

    private IEnumerator SpawnStaggered(SpawnEvent spawnEvent)
    {
        for (int i = 0; i < spawnEvent.enemyCount; i++)
        {
            enemySpawner.SpawnEnemy();
            if (i < spawnEvent.enemyCount - 1) // Dont wait after the last enemy
            {
                yield return new WaitForSeconds(spawnEvent.spawnInterval);
            }
        }
    }

    private IEnumerator SpawnBurst(SpawnEvent spawnEvent)
    {
        float spawnInterval = spawnEvent.burstDuration / spawnEvent.enemyCount;
        for (int i = 0; i < spawnEvent.enemyCount; i++)
        {
            enemySpawner.SpawnEnemy();

            if (i < spawnEvent.enemyCount - 1)
            {
                yield return new WaitForSeconds(spawnInterval);
            }
        }
    }

    private IEnumerator BackgroundSpawnRoutine()
    {
        yield return new WaitForSeconds(currentTimeLine.backgroundSpawnInterval);

        while (isGameActive)
        {
            for (int i = 0; i < currentTimeLine.backgroundEnemyCount; i++)
            {
                enemySpawner.SpawnEnemy();
            }

            if (showDebugLogs)
            {
                Debug.Log($"SpawnManager: background spawned {currentTimeLine.backgroundEnemyCount} enemies");
            }

        }

        yield return new WaitForSeconds(currentTimeLine.backgroundSpawnInterval);
    }

    private void UpdateUI()
    {
        // Update game time display
        if (gameTimeText != null)
        {
            if (IsGameActive)
            {
                int minutes = Mathf.FloorToInt(currentGameTime / 60f);
                int seconds = Mathf.FloorToInt(currentGameTime % 60f);
                gameTimeText.text = $"{minutes:00}:{seconds:00}";

            }
            else
            {
                gameTimeText.text = "Game Stopped";
            }
        }

        // Update next event display
        if (nextEventText != null && showUpcomingEvents)
        {
            SpawnEvent nextEvent = GetNextSpawnEvent();
            if (nextEvent != null)
            {
                Debug.Log($"Spawn manager {nextEvent.eventName}");
                float timeUntil = nextEvent.triggerTime - currentGameTime;
                nextEventText.text = $"Next Event: {nextEvent.eventName} in {timeUntil:F1}s ";
            }
            else
            {
                nextEventText.text = "No Upcoming Events";
            }
        }
        if (startGameButton != null)
        {
            startGameButton.interactable = !isGameActive;
        }
    }

    private SpawnEvent GetNextSpawnEvent()
    {
        if (currentTimeLine == null || currentTimeLine.spawnEvents == null) return null;


        for (int i = 0; i < currentTimeLine.spawnEvents.Length; i++)
        {
            if (!eventTriggered[i] && currentTimeLine.spawnEvents[i].triggerTime > currentGameTime)
            {
                return currentTimeLine.spawnEvents[i];
            }
        }

        return null;
    }

    // Public methods for external control
    public void PauseGame()
    {
        // TODO: Implement pause functionality
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        // TODO: Implement resume functionality
        Time.timeScale = 1f;
    }

    public void RestartGame()
    {
        StopGame();
        StartGame();
    }


    private System.Collections.IEnumerator RestartGameCoroutine()
    {
        StopGame();
        yield return new WaitForSeconds(0.1f);
        StartGame();
    }

    public void ChangeTimeLine(SpawnTimeLine newTimeline)
    {
        bool wasActive = isGameActive;
        if (wasActive)
        {
            StopGame();
        }
        currentTimeLine = newTimeline;
        InitializeTimeline();

        if (wasActive)
        {
            StartGame();
        }
    }

    // Context menu methods for testing
    [ContextMenu("Start Game")]
    private void StartGameFromMenu()
    {
        StartGame();
    }

    [ContextMenu("Stop Game")]
    private void StopGameFromMenu()
    {
        StopGame();
    }

    void OnDestroy()
    {
        // Clean up coroutines
        if (gameTimerCoroutine != null)
            StopCoroutine(gameTimerCoroutine);
        if (backgroundSpawnCoroutine != null)
            StopCoroutine(backgroundSpawnCoroutine);
    }
}
