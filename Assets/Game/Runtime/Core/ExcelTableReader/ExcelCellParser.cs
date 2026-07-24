using System;
using System.Data;
using System.Linq;

namespace Game.Runtime.Core.ExcelTableReader
{
    public static class ExcelCellParser
    {
        public static bool IsEmpty(DataRow row, int col)
        {
            return row.IsNull(col) || string.IsNullOrWhiteSpace(row[col].ToString());
        }

        public static string GetString(DataRow row, int col)
        {
            if (row.IsNull(col))
                return string.Empty;

            return row[col].ToString();
        }

        public static int GetInt(DataRow row, int col)
        {
            if (row.IsNull(col))
                return 0;

            if (int.TryParse(row[col].ToString(), out var value))
                return value;

            throw new Exception($"Excel解析失败，无法转换为int: {row[col]}");
        }

        public static float GetFloat(DataRow row, int col)
        {
            if (row.IsNull(col))
                return 0f;

            if (float.TryParse(row[col].ToString(), out var value))
                return value;

            throw new Exception($"Excel解析失败，无法转换为float: {row[col]}");
        }

        public static bool GetBool(DataRow row, int col)
        {
            if (row.IsNull(col))
                return false;

            var str = row[col].ToString().Trim();

            if (str == "1") return true;
            if (str == "0") return false;

            if (bool.TryParse(str, out var value))
                return value;

            throw new Exception($"无法解析bool: {str}");
        }

        public static T GetEnum<T>(DataRow row, int col) where T : struct
        {
            if (row.IsNull(col))
                return default;

            var str = row[col].ToString();

            if (Enum.TryParse<T>(str, true, out var value))
                return value;

            throw new Exception($"Excel解析失败，无法转换为枚举 {typeof(T).Name}: {str}");
        }

        /// <summary>
        ///     读取 “1|2|3” -> int[]
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public static int[] GetIntArray(DataRow row, int col)
        {
            if (row.IsNull(col))
                return Array.Empty<int>();

            var str = row[col].ToString();

            if (string.IsNullOrWhiteSpace(str))
                return Array.Empty<int>();

            return str
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => int.TryParse(s, out _))
                .Select(int.Parse)
                .ToArray();
        }

        /// <summary>
        ///     读取 “A|B|C” -> string[]
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public static string[] GetStringArray(DataRow row, int col)
        {
            if (row.IsNull(col))
                return Array.Empty<string>();

            var str = row[col].ToString();

            if (string.IsNullOrWhiteSpace(str))
                return Array.Empty<string>();

            return str.Split('|')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
        }
    }
}