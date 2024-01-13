using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool isGamePaused = false;
    public GameObject gameplayUI;
    public GameObject pauseMenuUI;

    public void TogglePauseMenu(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (isGamePaused)
                Resume();
            else
                Pause();
        }
    }

    public void Resume()
    {
        // deactivate cursor
        Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;

        gameplayUI.SetActive(true);
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isGamePaused = false;
    }

    public void Pause()
    {
        // activate cursor
        Cursor.lockState = CursorLockMode.None;
     //   Cursor.visible = true;

        gameplayUI.SetActive(false);
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isGamePaused = true;
    }

    public void LoadMenu()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }

    public void QuitGame()
    {
        Debug.Log("Quit game.");
        Application.Quit();
    }
}
