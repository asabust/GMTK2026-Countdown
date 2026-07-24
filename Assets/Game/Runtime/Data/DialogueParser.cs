using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Runtime.Data
{
    public static class DialogueParser
    {
        private static readonly Regex SpeakerRegex =
            new(@"^(.+?)(\d+)$", RegexOptions.Compiled);

        public static List<DialogueLine> Parse(string rawText)
        {
            var lines = new List<DialogueLine>();

            if (string.IsNullOrWhiteSpace(rawText))
                return lines;

            rawText = rawText.Replace("\r\n", "\n");
            var rows = rawText.Split('\n');

            for (var i = 0; i < rows.Length; i++)
            {
                var line = NormalizeLine(rows[i]);
                if (string.IsNullOrEmpty(line))
                    continue;

                if (line.Equals("Choice", StringComparison.OrdinalIgnoreCase))
                {
                    lines.Add(ParseChoice(rows, ref i));
                    break; // Choice 一定是最后
                }

                var dialogueLine = ParseDialogueLine(line, i + 1);
                if (dialogueLine != null)
                    lines.Add(dialogueLine);
            }

            return lines;
        }

        private static DialogueLine ParseDialogueLine(string line, int lineNumber)
        {
            var idx = line.IndexOf(':');

            if (idx < 0)
            {
                Debug.LogError($"对话解析错误，没有冒号 {lineNumber}: {line}");
                return null;
            }

            var speaker = line[..idx].Trim();
            var text = line[(idx + 1)..].Trim();

            var dialogueLine = new DialogueLine { text = text };

            if (speaker.Equals("black", StringComparison.OrdinalIgnoreCase))
            {
                dialogueLine.type = DialogueType.Black;
                return dialogueLine;
            }

            if (speaker.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                dialogueLine.type = DialogueType.Null;
                return dialogueLine;
            }


            dialogueLine.type = DialogueType.Normal;
            dialogueLine.speakerName = speaker;

            return dialogueLine;
        }

        private static DialogueLine ParseChoice(string[] rows, ref int index)
        {
            var line = new DialogueLine { type = DialogueType.Choice, choices = new List<Choice>() };

            for (var i = index + 1; i < rows.Length; i++)
            {
                var row = NormalizeLine(rows[i]);

                if (string.IsNullOrEmpty(row))
                    continue;

                var parts = ParseCommand(row);

                if (parts.Length != 2)
                {
                    Debug.LogError($"选项解析错误: {row}");
                    continue;
                }

                if (!int.TryParse(parts[1], out var value))
                    value = 0;

                line.choices.Add(new Choice { text = parts[0], nextDialogueId = value });

                index = i;
            }

            return line;
        }

        private static string[] ParseCommand(string rawText)
        {
            return rawText.Split('|')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
        }

        private static string NormalizeLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return string.Empty;

            // var commentIndex = line.IndexOf('#');
            // if (commentIndex >= 0)
            //     line = line.Substring(0, commentIndex);

            //去掉首尾空格
            line = line.Trim();

            if (line.Length == 0)
                return string.Empty;

            line = line.Replace('：', ':');

            return line;
        }
    }
}