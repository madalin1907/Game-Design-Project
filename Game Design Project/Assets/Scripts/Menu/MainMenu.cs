using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private Slider volumeSlider;

    void Start()
    {
        Debug.Log("Main menu loaded.");
        if (!PlayerPrefs.HasKey("volume"))
        {
            PlayerPrefs.SetFloat("volume", 0.5f);
            LoadVolume();
        }
        else
            LoadVolume();
    }


    //  Volume functions
    public void ChangeVolume(float volume)
    {
        AudioListener.volume = volume;
        SaveVolume();
    }

    private void LoadVolume() =>
        volumeSlider.value = PlayerPrefs.GetFloat("volume");

    private void SaveVolume() =>
        PlayerPrefs.SetFloat("volume", volumeSlider.value);


    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1); // Loads the next scene in the build order
    }
    
    public void QuitGame()
    {
        Debug.Log("Quit game.");
        Application.Quit();
    }
}
