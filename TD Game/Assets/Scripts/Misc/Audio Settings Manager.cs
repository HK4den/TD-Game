using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
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
    private const string SFXMixerParameter = "SFXVolume";
    private const string MusicMixerParameter = "MusicVolume";
    private const string DefaultMixerResourcePath = "MainAudioMixer";
    private const float MinimumAudibleVolume = 0.0001f;
    private const float MutedDecibels = -80f;
    private const float StartupReapplyDuration = 1f;
    private const float StartupReapplyInterval = 0.1f;

    private static AudioMixer cachedMixer;
    private static AudioSettingsManager runtimeApplier;
    private static bool runtimeInitialized;
    private static bool warnedMissingMixer;

    private Coroutine delayedApplyRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        cachedMixer = null;
        runtimeApplier = null;
        runtimeInitialized = false;
        warnedMissingMixer = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RuntimeInitialize()
    {
        if (runtimeInitialized)
            return;

        runtimeInitialized = true;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureRuntimeApplier();
        ApplySavedSettingsToMixer();
        QueueRuntimeDelayedApply();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RuntimeApplyAfterFirstSceneLoad()
    {
        EnsureRuntimeApplier();
        ApplySavedSettingsToMixer();
        QueueRuntimeDelayedApply();
    }

    private void Awake()
    {
        CacheMixer(audioMixer);
        ApplySavedSettings();
        QueueDelayedApply();
    }

    private void OnEnable()
    {
        CacheMixer(audioMixer);
        ApplySavedSettings();
        QueueDelayedApply();

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }
    }

    private void OnDisable()
    {
        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);

        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
    }

    private void ApplySavedSettings()
    {
        float savedSFX = GetSavedVolume(SFXKey);
        float savedMusic = GetSavedVolume(MusicKey);

        ApplySavedSettingsToMixer(audioMixer);

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(savedSFX);
        }

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(savedMusic);
        }
    }

    public void SetSFXVolume(float value)
    {
        value = ClampVolume(value);
        PlayerPrefs.SetFloat(SFXKey, value);
        PlayerPrefs.Save();
        ApplySFXVolume(value, audioMixer);
    }

    public void SetMusicVolume(float value)
    {
        value = ClampVolume(value);
        PlayerPrefs.SetFloat(MusicKey, value);
        PlayerPrefs.Save();
        ApplyMusicVolume(value, audioMixer);
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySavedSettingsToMixer();
        QueueRuntimeDelayedApply();
    }

    private static void ApplySavedSettingsToMixer(AudioMixer preferredMixer = null)
    {
        ApplySFXVolume(GetSavedVolume(SFXKey), preferredMixer);
        ApplyMusicVolume(GetSavedVolume(MusicKey), preferredMixer);
    }

    private static void ApplySFXVolume(float value, AudioMixer preferredMixer = null)
    {
        ApplyMixerVolume(SFXMixerParameter, value, preferredMixer);
    }

    private static void ApplyMusicVolume(float value, AudioMixer preferredMixer = null)
    {
        ApplyMixerVolume(MusicMixerParameter, value, preferredMixer);
    }

    private static void ApplyMixerVolume(string parameterName, float value, AudioMixer preferredMixer)
    {
        AudioMixer targetMixer = GetMixer(preferredMixer);
        if (targetMixer == null)
        {
            if (!warnedMissingMixer)
            {
                warnedMissingMixer = true;
                Debug.LogWarning("Audio settings could not find MainAudioMixer. Put it in Assets/Resources or assign it on AudioSettingsManager.");
            }

            return;
        }

        targetMixer.SetFloat(parameterName, VolumeToDecibels(value));
    }

    private static AudioMixer GetMixer(AudioMixer preferredMixer = null)
    {
        if (preferredMixer != null)
            return CacheMixer(preferredMixer);

        if (cachedMixer == null)
            cachedMixer = Resources.Load<AudioMixer>(DefaultMixerResourcePath);

        return cachedMixer;
    }

    private static AudioSettingsManager EnsureRuntimeApplier()
    {
        if (runtimeApplier != null)
            return runtimeApplier;

        GameObject runtimeObject = new GameObject("Audio Settings Runtime Applier");
        DontDestroyOnLoad(runtimeObject);
        runtimeObject.hideFlags = HideFlags.HideAndDontSave;
        runtimeApplier = runtimeObject.AddComponent<AudioSettingsManager>();
        return runtimeApplier;
    }

    private static void QueueRuntimeDelayedApply()
    {
        AudioSettingsManager applier = EnsureRuntimeApplier();
        if (applier != null)
            applier.QueueDelayedApply();
    }

    private void QueueDelayedApply()
    {
        if (!isActiveAndEnabled)
            return;

        if (delayedApplyRoutine != null)
            StopCoroutine(delayedApplyRoutine);

        delayedApplyRoutine = StartCoroutine(ApplySavedSettingsDuringStartup());
    }

    private IEnumerator ApplySavedSettingsDuringStartup()
    {
        ApplySavedSettingsToMixer(audioMixer);
        yield return null;

        ApplySavedSettingsToMixer(audioMixer);
        yield return new WaitForEndOfFrame();

        ApplySavedSettingsToMixer(audioMixer);

        float elapsed = 0f;
        while (elapsed < StartupReapplyDuration)
        {
            yield return new WaitForSecondsRealtime(StartupReapplyInterval);
            elapsed += StartupReapplyInterval;
            ApplySavedSettingsToMixer(audioMixer);
        }

        delayedApplyRoutine = null;
    }

    private static AudioMixer CacheMixer(AudioMixer mixer)
    {
        if (mixer != null)
            cachedMixer = mixer;

        return cachedMixer;
    }

    private static float GetSavedVolume(string key)
    {
        return ClampVolume(PlayerPrefs.GetFloat(key, 1f));
    }

    private static float ClampVolume(float value)
    {
        return Mathf.Clamp01(value);
    }

    private static float VolumeToDecibels(float value)
    {
        value = ClampVolume(value);
        if (value <= MinimumAudibleVolume)
            return MutedDecibels;

        return Mathf.Log10(value) * 20f;
    }
}
