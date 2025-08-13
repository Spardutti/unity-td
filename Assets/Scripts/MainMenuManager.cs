using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public Button startButton;

    void Start()
    {
        startButton.onClick.AddListener(StartGame);
    }

    public void StartGame()
    {
        Debug.Log("Starting Game ");
        SceneManager.LoadScene("GameScene");
    }
}
