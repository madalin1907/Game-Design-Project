using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private Slider volumeSlider;

    [SerializeField]
    private Toggle fullScreenToggle;

    void Start()
    {
        if (!PlayerPrefs.HasKey("volume"))
        {
            PlayerPrefs.SetFloat("volume", 0.5f);
            LoadVolume();
        }
        else
            LoadVolume();

        if (!PlayerPrefs.HasKey("isFullScreen"))
        {
            PlayerPrefs.SetInt("isFullScreen", 1);
            LoadFullScreen();
        }
        else
            LoadFullScreen();
    }


    //  Fullscreen functions
    public void SetFullScreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
        SaveFullScreen();
    }
    
    private void LoadFullScreen()
    {
        if (PlayerPrefs.GetInt("isFullScreen") == 1)
            fullScreenToggle.isOn = true;
        else
            fullScreenToggle.isOn = false;
    }

    private void SaveFullScreen()
    {
        if (fullScreenToggle.isOn)
            PlayerPrefs.SetInt("isFullScreen", 1);
        else
            PlayerPrefs.SetInt("isFullScreen", 0);
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
        SceneManager.LoadScene(1);
    }
    

    public void QuitGame()
    {
        Debug.Log("Quit game.");
        Application.Quit();
    }
}
