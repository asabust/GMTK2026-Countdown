using Game.Runtime.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LocalizationFontManager
{
    private const string SettingsPath = "UI/LocalizationFontSettings";

    private static LocalizationFontSettings settings;
    private static bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        GameLocalization.LanguageChanged += ApplyToLoadedTexts;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        LoadSettings();
    }

    public static void ApplyTo(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        TMP_FontAsset font = GetCurrentFont();
        if (font == null)
        {
            return;
        }

        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            text.font = font;
        }
    }

    public static void ApplyToLoadedTexts()
    {
        TMP_FontAsset font = GetCurrentFont();
        if (font == null)
        {
            return;
        }

        foreach (TMP_Text text in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (text == null || !text.gameObject.scene.IsValid())
            {
                continue;
            }

            text.font = font;
        }
    }

    private static TMP_FontAsset GetCurrentFont()
    {
        LoadSettings();
        return settings != null
            ? settings.GetFont(GameLocalization.CurrentLanguage)
            : null;
    }

    private static void LoadSettings()
    {
        if (settings == null)
        {
            settings = Resources.Load<LocalizationFontSettings>(
                SettingsPath
            );
        }
    }

    private static void HandleSceneLoaded(Scene _, LoadSceneMode __)
    {
        ApplyToLoadedTexts();
    }
}
