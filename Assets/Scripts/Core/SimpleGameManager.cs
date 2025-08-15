using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SimpleGameManager : MonoBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("Game Manager Started");
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void GoBackToMainMenu()
    {
        Debug.Log("Going back to Main Menu");
        SceneManager.LoadScene("MainMenu");
    }
}
