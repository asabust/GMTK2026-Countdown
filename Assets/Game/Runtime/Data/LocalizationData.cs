using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

namespace Game.Runtime.Data
{
    [Serializable]
    public class LocalizationData
    {
        // lang -> key -> text
        public readonly Dictionary<Language, Dictionary<string, string>> data = new();

        public void Add(Language lang, string key, string text)
        {
            if (!data.TryGetValue(lang, out var dict))
            {
                dict = new Dictionary<string, string>();
                data[lang] = dict;
            }

            dict[key] = text;
        }

        public string Get(Language lang, string key)
        {
            if (data == null || !data.ContainsKey(lang))
            {
                Debug.LogError("[LocalizationData]: No data found for language " + lang);
                return string.Empty;
            }

            if (data.TryGetValue(lang, out var dict) &&
                dict.TryGetValue(key, out var text))
                return text;

            return $"[MISSING:{lang}:{key}]"; // 方便发现漏填
        }
    }

    public enum Language
    {
        English,
        Chinese,
        Japanese
    }
}