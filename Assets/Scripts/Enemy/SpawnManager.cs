using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SpawnManager : MonoBehaviour
{
    [Header("Timeline COnfiguration")]
    [SerializeField] private SpawnTimeLine currentTimeLine;
    [SerializeField] private bool autoStartOnPlay = false;

    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;

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
        if (currentTimeLine != null && currentTimeLine.spawnEvents == null) return null;

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
