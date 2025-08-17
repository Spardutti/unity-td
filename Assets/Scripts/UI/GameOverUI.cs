using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{

    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Game Over Settings")]
    [SerializeField] private string gameOverMessage = "Game Over";
    [SerializeField] private bool pauseGameOnGameOver = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    void Awake()
    {

        // auto find components if not assigned
        if (gameOverPanel == null)
        {
            gameOverPanel = GetComponentInChildren<GameObject>();
        }
        if (gameOverText == null)
        {
            gameOverText = GetComponentInChildren<TextMeshProUGUI>();
        }
        if (restartButton == null)
        {
            restartButton = GetComponentInChildren<Button>();
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Subscribe to player death event
        PlayerHealthManager.OnPlayerDied += ShowGameOver;

        // Setup button listeners
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }

        HideGameOver();

        // Set game over text
        if (gameOverText != null)
        {
            gameOverText.text = gameOverMessage;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from player death event
        PlayerHealthManager.OnPlayerDied -= ShowGameOver;

    }

    private void ShowGameOver()
    {
        if (showDebugLog)
        {
            Debug.Log("GameOverUI: Player died, showing game over panel");
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (pauseGameOnGameOver)
        {
            Time.timeScale = 0f;
            if (showDebugLog)
            {
                Debug.Log("GameOverUI: Paused game");
            }
        }
    }

    private void HideGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Ensure game is not pause
        Time.timeScale = 1f;
    }

    public void RestartGame()
    {
        if (showDebugLog)
        {
            Debug.Log("GameOverUI: Restarting game");
        }

        // Unpause game before restarting
        Time.timeScale = 1f;

        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        if (showDebugLog)
        {
            Debug.Log("GameOverUI: Going to main menu");
        }

        // Unpause game before restarting
        Time.timeScale = 1f;

        // Load main menu scene
        SceneManager.LoadScene("MainMenu");
    }

    // Method for manual testing
    [ContextMenu("Show Game Over Test")]
    private void TestGameOver()
    {
        ShowGameOver();
    }

    // Method for manual testing
    [ContextMenu("Hide Game Over Test")]
    private void TestHideGameOver()
    {
        HideGameOver();
    }
}
