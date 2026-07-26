using System.Collections.Generic;
using Game.Runtime.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SettingsPanel : UIPanel, IPointerClickHandler
{
    private static readonly Language[] Languages =
    {
        Language.English,
        Language.Chinese,
        Language.Japanese
    };

    [Header("Audio")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Language")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    [Header("Localized text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text musicLabel;
    [SerializeField] private TMP_Text sfxLabel;
    [SerializeField] private TMP_Text languageLabel;

    private PlayerGridController playerController;

    public override void OnInit()
    {
        ConfigureSlider(musicSlider);
        ConfigureSlider(sfxSlider);

        musicSlider?.onValueChanged.AddListener(HandleMusicVolumeChanged);
        sfxSlider?.onValueChanged.AddListener(HandleSfxVolumeChanged);
        languageDropdown?.onValueChanged.AddListener(
            HandleLanguageChanged
        );
        GameLocalization.LanguageChanged += RefreshLocalizedContent;
    }

    public override void OnOpen(object data = null)
    {
        playerController = FindObjectOfType<PlayerGridController>();
        playerController?.SetMenuInputLocked(true);

        AudioManager audioManager = AudioManager.Instance;
        musicSlider?.SetValueWithoutNotify(
            audioManager != null ? audioManager.MusicVolume : 1f
        );
        sfxSlider?.SetValueWithoutNotify(
            audioManager != null ? audioManager.SFXVolume : 1f
        );

        RefreshLocalizedContent();
    }

    public override void OnClose()
    {
        EventSystem.current?.SetSelectedGameObject(null);
        playerController?.SetMenuInputLocked(false);
        playerController = null;
    }

    private void Update()
    {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
        {
            Close();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.pointerCurrentRaycast.gameObject == gameObject)
        {
            Close();
        }
    }

    private void OnDestroy()
    {
        playerController?.SetMenuInputLocked(false);
        musicSlider?.onValueChanged.RemoveListener(
            HandleMusicVolumeChanged
        );
        sfxSlider?.onValueChanged.RemoveListener(
            HandleSfxVolumeChanged
        );
        languageDropdown?.onValueChanged.RemoveListener(
            HandleLanguageChanged
        );
        GameLocalization.LanguageChanged -= RefreshLocalizedContent;
    }

    private static void ConfigureSlider(Slider slider)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    private static void HandleMusicVolumeChanged(float value)
    {
        AudioManager.Instance?.SetMusicVolume(value);
    }

    private static void HandleSfxVolumeChanged(float value)
    {
        AudioManager.Instance?.SetSFXVolume(value);
    }

    private static void HandleLanguageChanged(int index)
    {
        if (index < 0 || index >= Languages.Length)
        {
            return;
        }

        GameLocalization.SetLanguage(Languages[index]);
    }

    private static void Close()
    {
        UIManager.Instance?.Close<SettingsPanel>();
    }

    private void RefreshLocalizedContent()
    {
        SetText(titleText, "title.settings", "Settings");
        SetText(musicLabel, "settings.volume", "Music");
        SetText(sfxLabel, "settings.sfx", "SFX");
        SetText(languageLabel, "settings.language", "Language");

        if (languageDropdown != null)
        {
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(
                new List<string>
                {
                    GameLocalization.GetOrDefault(
                        "language.english",
                        "English"
                    ),
                    GameLocalization.GetOrDefault(
                        "language.chinese",
                        "简体中文"
                    ),
                    GameLocalization.GetOrDefault(
                        "language.japanese",
                        "日本語"
                    )
                }
            );
            languageDropdown.SetValueWithoutNotify(
                GetLanguageIndex(GameLocalization.CurrentLanguage)
            );
            languageDropdown.RefreshShownValue();
        }

        LocalizationFontManager.ApplyTo(gameObject);
    }

    private static int GetLanguageIndex(Language language)
    {
        for (int i = 0; i < Languages.Length; i++)
        {
            if (Languages[i] == language)
            {
                return i;
            }
        }

        return 0;
    }

    private static void SetText(
        TMP_Text target,
        string key,
        string fallback
    )
    {
        if (target != null)
        {
            target.text = GameLocalization.GetOrDefault(key, fallback);
        }
    }
}
