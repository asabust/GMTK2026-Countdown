using System;
using System.Collections.Generic;
using Game.Runtime.Data;

namespace Game.Runtime.Core.ExcelTableReader
{
    [Serializable]
    public class ExcelTableContext
    {
        public Dictionary<int, CharacterData> characters = new();
        public Dictionary<int, DialogueData> dialogues = new();
        public Dictionary<int, ItemData> items = new();
        public LocalizationData localizationData = new();
    }
}