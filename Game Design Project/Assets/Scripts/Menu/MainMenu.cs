using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1); // Loads the next scene in the build order
    }
    
    public void QuitGame()
    {
        Debug.Log("Quit"); // Prints "Quit" to the console
        Application.Quit(); // Quits the game
    }
}
