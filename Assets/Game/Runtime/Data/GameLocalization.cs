using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime.Data
{
    public static class GameLocalization
    {
        private const string LanguagePreferenceKey = "game.language";
        private static readonly LocalizationData fallbackTable =
            CreateFallbackTable();
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
            if (runtimeTable != null &&
                runtimeTable.TryGet(CurrentLanguage, key, out template))
            {
                return true;
            }

            return fallbackTable.TryGet(CurrentLanguage, key, out template);
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

        private static LocalizationData CreateFallbackTable()
        {
            LocalizationData table = new();
            AddLanguage(
                table,
                Language.Chinese,
                new Dictionary<string, string>
                {
                    ["enemy.intent.attack"] = "意图：攻击 -{0}",
                    ["enemy.intent.charge"] = "意图：蓄力",
                    ["enemy.intent.heavy_attack"] = "意图：强力击 -{0}",
                    ["enemy.intent.steal_item"] =
                        "意图：偷取道具（无道具：-{0}并眩晕）",
                    ["enemy.intent.wait"] = "意图：等待",
                    ["enemy.intent.drink"] = "意图：喝酒（结果未知）",
                    ["enemy.intent.escape_with"] =
                        "意图：携带 {0} 点逃跑",
                    ["enemy.intent.steal_number"] = "意图：偷取 {0}",
                    ["enemy.intent.attack_and_steal"] =
                        "意图：攻击 -{0}，偷取 {1}",
                    ["enemy.action.attack_and_steal"] =
                        "普攻：-{0}，偷取 {1}",
                    ["enemy.intent.peck"] = "意图：啄地 -{0}"
                }
            );
            AddLanguage(
                table,
                Language.English,
                new Dictionary<string, string>
                {
                    ["enemy.intent.attack"] = "Intent: Attack -{0}",
                    ["enemy.intent.charge"] = "Intent: Charge",
                    ["enemy.intent.heavy_attack"] =
                        "Intent: Heavy Attack -{0}",
                    ["enemy.intent.steal_item"] =
                        "Intent: Steal Item (none: -{0} and Stun)",
                    ["enemy.intent.wait"] = "Intent: Wait",
                    ["enemy.intent.drink"] =
                        "Intent: Drink (outcome unknown)",
                    ["enemy.intent.escape_with"] =
                        "Intent: Escape with {0}",
                    ["enemy.intent.steal_number"] = "Intent: Steal {0}",
                    ["enemy.intent.attack_and_steal"] =
                        "Intent: Attack -{0}, Steal {1}",
                    ["enemy.action.attack_and_steal"] =
                        "Attack: -{0}, Steal {1}",
                    ["enemy.intent.peck"] = "Intent: Ground Peck -{0}"
                }
            );
            AddLanguage(
                table,
                Language.Japanese,
                new Dictionary<string, string>
                {
                    ["enemy.intent.attack"] = "予告：攻撃 -{0}",
                    ["enemy.intent.charge"] = "予告：チャージ",
                    ["enemy.intent.heavy_attack"] = "予告：強攻撃 -{0}",
                    ["enemy.intent.steal_item"] =
                        "予告：アイテムを盗む（所持なし：-{0}＋スタン）",
                    ["enemy.intent.wait"] = "予告：待機",
                    ["enemy.intent.drink"] =
                        "予告：酒を飲む（結果は不明）",
                    ["enemy.intent.escape_with"] =
                        "予告：{0}を持って逃走",
                    ["enemy.intent.steal_number"] = "予告：{0}を盗む",
                    ["enemy.intent.attack_and_steal"] =
                        "予告：攻撃 -{0}、{1}を盗む",
                    ["enemy.action.attack_and_steal"] =
                        "攻撃：-{0}、{1}を盗む",
                    ["enemy.intent.peck"] = "予告：地面をつつく -{0}"
                }
            );
            return table;
        }

        private static void AddLanguage(
            LocalizationData table,
            Language language,
            Dictionary<string, string> entries
        )
        {
            foreach (KeyValuePair<string, string> entry in entries)
            {
                table.Add(language, entry.Key, entry.Value);
            }
        }
    }
}
