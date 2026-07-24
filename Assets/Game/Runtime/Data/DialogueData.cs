using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace Game.Runtime.Data
{
    public enum DialogueType
    {
        Normal, // 普通角色对话
        Black, // 黑屏文字
        Null, // 无说话人的旁白
        Choice // 选项
    }

    [Serializable]
    public class DialogueData
    {
        public int dialogueId;
        public List<DialogueLine> lines;
        public int nextDialogueId;
    }

    [Serializable]
    public class DialogueLine
    {
        public DialogueType type;
        public string speakerName;
        public string text;
        public List<Choice> choices; //选项只能跳转到故事节点
    }


    [Serializable]
    public class Choice
    {
        public string text;
        public int nextDialogueId;
    }
}