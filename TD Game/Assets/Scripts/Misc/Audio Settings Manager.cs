using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettingsManager : MonoBehaviour
{
    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Optional UI Sliders")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;

    private const string SFXKey = "SFXVolume";
    private const string MusicKey = "MusicVolume";

    private void Awake()
    {
        float savedSFX = PlayerPrefs.GetFloat(SFXKey, 1f);
        float savedMusic = PlayerPrefs.GetFloat(MusicKey, 1f);

        ApplySFXVolume(savedSFX);
        ApplyMusicVolume(savedMusic);

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(savedSFX);
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(savedMusic);
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }
    }

    public void SetSFXVolume(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);
        PlayerPrefs.SetFloat(SFXKey, value);
        PlayerPrefs.Save();
        ApplySFXVolume(value);
    }

    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);
        PlayerPrefs.SetFloat(MusicKey, value);
        PlayerPrefs.Save();
        ApplyMusicVolume(value);
    }

    private void ApplySFXVolume(float value)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20f);
    }

    private void ApplyMusicVolume(float value)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20f);
    }
}