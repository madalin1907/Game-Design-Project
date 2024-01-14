using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool isGamePaused = false;
    public GameObject gameplayUI;
    public GameObject pauseMenuUI;
    public GameObject optionsMenuUI;
    public GameObject controlsMenuUI;

    public void TogglePauseMenu(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (isGamePaused)
            {
                if (optionsMenuUI.activeSelf)
                {
                    optionsMenuUI.SetActive(false);
                    pauseMenuUI.SetActive(true);
                    return;
                }
                else if (controlsMenuUI.activeSelf)
                {
                    controlsMenuUI.SetActive(false);
                    pauseMenuUI.SetActive(true);
                    return;
                }
                Resume();
            }
            else
                Pause();
        }
    }

    public void Resume()
    {
        Cursor.lockState = CursorLockMode.Locked;

        gameplayUI.SetActive(true);
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isGamePaused = false;
    }

    public void Pause()
    {
        Cursor.lockState = CursorLockMode.None;

        gameplayUI.SetActive(false);
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isGamePaused = true;
    }

    public void LoadMenu()
    {
        isGamePaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        Debug.Log("Quit game.");
        Application.Quit();
    }
}
