using System;
using UnityEngine;

namespace Game.Runtime.Data
{
    public static class GameLocalization
    {
        private const string LanguagePreferenceKey = "game.language";
        private static LocalizationData runtimeTable;

        public static Language CurrentLanguage { get; private set; } =
            LoadSavedLanguage();

        public static event Action LanguageChanged;

        public static void SetLanguage(Language language)
        {
            if (CurrentLanguage == language)
            {
                return;
            }

            CurrentLanguage = language;
            PlayerPrefs.SetInt(LanguagePreferenceKey, (int)language);
            PlayerPrefs.Save();
            LanguageChanged?.Invoke();
        }

        public static void UseTable(LocalizationData table)
        {
            runtimeTable = table;
            LanguageChanged?.Invoke();
        }

        public static string Get(string key, params object[] arguments)
        {
            if (!TryGet(key, out string template))
            {
                return $"[MISSING:{CurrentLanguage}:{key}]";
            }

            return Format(template, key, arguments);
        }

        public static string GetOrDefault(
            string key,
            string fallback,
            params object[] arguments
        )
        {
            string template = TryGet(key, out string localized)
                ? localized
                : fallback;
            return Format(template, key, arguments);
        }

        public static bool HasKey(string key) => TryGet(key, out _);

        private static bool TryGet(string key, out string template)
        {
            if (runtimeTable == null)
            {
                template = null;
                return false;
            }

            return runtimeTable.TryGet(CurrentLanguage, key, out template);
        }

        private static string Format(
            string template,
            string key,
            object[] arguments
        )
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                return $"[MISSING:{CurrentLanguage}:{key}]";
            }

            if (arguments == null || arguments.Length == 0)
            {
                return template;
            }

            try
            {
                return string.Format(template, arguments);
            }
            catch (FormatException exception)
            {
                Debug.LogError(
                    $"Invalid localization format for '{key}': {exception.Message}"
                );
                return $"[FORMAT:{CurrentLanguage}:{key}]";
            }
        }

        private static Language LoadSavedLanguage()
        {
            int saved = PlayerPrefs.GetInt(
                LanguagePreferenceKey,
                (int)Language.English
            );
            return Enum.IsDefined(typeof(Language), saved)
                ? (Language)saved
                : Language.English;
        }
    }
}
