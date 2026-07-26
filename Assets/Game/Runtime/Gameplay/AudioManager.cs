using System.Collections.Generic;
using Game.Runtime.Core;
using Game.Runtime.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AudioManager : Singleton<AudioManager>
{
    public const string SFXVolumeKey = "SFXVolume";
    public const string MusicVolumeKey = "MusicVolume";

    [Header("音频数据")]
    public AudioInfoListSO audioInfoListSO;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    private float musicVolume = 1f;
    private float sfxVolume = 1f;
    private AudioName currentMusic;
    private readonly HashSet<int> boundUiControls = new();
    private float nextSliderSfxTime;

    public float MusicVolume => musicVolume;
    public float SFXVolume => sfxVolume;

    protected override void Awake()
    {
        base.Awake();
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        sfxSource.playOnAwake = false;
    }

    private void Start()
    {
        LoadAudioSettings();
        RefreshSceneAudio();
    }

    public void PlayMusic(AudioName name)
    {
        if (musicSource == null)
        {
            return;
        }

        if (name == AudioName.None)
        {
            musicSource.Stop();
            currentMusic = AudioName.None;
            return;
        }

        if (name == currentMusic && musicSource.isPlaying)
        {
            return;
        }

        AudioInf info = audioInfoListSO?.GetAudioInfo(name);
        if (info?.clip == null)
        {
            Debug.LogWarning($"Audio clip is not configured for {name}.", this);
            return;
        }

        currentMusic = name;
        musicSource.clip = info.clip;
        musicSource.volume = info.volume * musicVolume;
        musicSource.loop = info.loop;
        musicSource.Play();
    }

    public void PlaySFX(AudioName name)
    {
        if (name == AudioName.None || sfxSource == null)
        {
            return;
        }

        AudioInf info = audioInfoListSO?.GetAudioInfo(name);
        if (info?.clip == null)
        {
            return;
        }

        sfxSource.PlayOneShot(info.clip, info.volume);
    }

    public void PlaySliderSFX()
    {
        if (Time.unscaledTime < nextSliderSfxTime)
        {
            return;
        }

        nextSliderSfxTime = Time.unscaledTime + 0.06f;
        PlaySFX(AudioName.UiSlider);
    }

    public void BindUIAudio(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (button == null || !boundUiControls.Add(button.GetInstanceID()))
            {
                continue;
            }

            AudioName sound = IsCloseButton(button.name)
                ? AudioName.UiClose
                : AudioName.UiClick;
            button.onClick.AddListener(() => PlaySFX(sound));
        }

        foreach (Slider slider in root.GetComponentsInChildren<Slider>(true))
        {
            if (slider == null || !boundUiControls.Add(slider.GetInstanceID()))
            {
                continue;
            }

            slider.onValueChanged.AddListener(_ => PlaySliderSFX());
        }
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateAudioVolumes();
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateAudioVolumes();
        PlayerPrefs.SetFloat(SFXVolumeKey, sfxVolume);
        PlayerPrefs.Save();
    }

    private void UpdateAudioVolumes()
    {
        if (musicSource != null)
        {
            AudioInf info = audioInfoListSO?.GetAudioInfo(currentMusic);
            musicSource.volume = (info?.volume ?? 1f) * musicVolume;
        }
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    private void LoadAudioSettings()
    {
        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        sfxVolume = PlayerPrefs.GetFloat(SFXVolumeKey, 1f);
        UpdateAudioVolumes();
    }

    private void OnEnable()
    {
        EventHandler.AfterSceneLoadEvent += OnAfterSceneLoad;
        SceneManager.sceneLoaded += OnUnitySceneLoaded;
        UIManager.PanelOpened += BindUIAudio;
    }

    private void OnDisable()
    {
        EventHandler.AfterSceneLoadEvent -= OnAfterSceneLoad;
        SceneManager.sceneLoaded -= OnUnitySceneLoaded;
        UIManager.PanelOpened -= BindUIAudio;
    }

    private void OnAfterSceneLoad(string _) => RefreshSceneAudio();

    private void OnUnitySceneLoaded(Scene _, LoadSceneMode __) =>
        RefreshSceneAudio();

    private void RefreshSceneAudio()
    {
        GameManager gameManager = GameManager.Instance;
        PlayMusic(
            gameManager != null && gameManager.IsGameplay
                ? AudioName.BgmGameplay
                : AudioName.BgmTitle
        );

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
            {
                continue;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                BindUIAudio(root);
            }
        }
    }

    private static bool IsCloseButton(string objectName)
    {
        string lower = objectName?.ToLowerInvariant() ?? string.Empty;
        return lower.Contains("close") ||
               lower.Contains("leave") ||
               lower.Contains("back") ||
               lower.Contains("quit") ||
               lower.Contains("cancel");
    }
}
