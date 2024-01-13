using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [SerializeField]
    private Slider volumeSlider;

    void Start() {
       if (!PlayerPrefs.HasKey("volume")) {
            PlayerPrefs.SetFloat("volume", 0.5f);
            LoadVolume();
        }
        else
            LoadVolume();
    }


    //  Volume functions
    public void ChangeVolume(float volume) {
        AudioListener.volume = volume;
        SaveVolume();
    }

    private void LoadVolume() =>
        volumeSlider.value = PlayerPrefs.GetFloat("volume");

    private void SaveVolume() =>
        PlayerPrefs.SetFloat("volume", volumeSlider.value);
}
