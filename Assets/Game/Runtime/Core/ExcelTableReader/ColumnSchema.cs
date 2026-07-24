using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Game.Runtime.Core.ExcelTableReader
{
    public enum ExcelColumnType
    {
        String,
        Int,
        Float,
        Bool,
        IntArray, // "int[]" 或 "int|"
        StringArray // "string[]" 或 "string|"
    }

    /// <summary>
    /// 解析表头和类型行
    /// </summary>
    public class ColumnSchema
    {
        private readonly Dictionary<string, int> _nameToIndex
            = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, ExcelColumnType> _nameToType
            = new(StringComparer.OrdinalIgnoreCase);

        public static ColumnSchema Build(DataTable table)
        {
            var schema = new ColumnSchema();
            DataRow headerRow = table.Rows[0];
            DataRow typeRow = table.Rows[1];

            for (int col = 0; col < table.Columns.Count; col++)
            {
                //Build 时，防止比对失败所有环节都 .ToLower()
                string name = headerRow[col]?.ToString()?.Trim().ToLower() ?? "";
                if (string.IsNullOrEmpty(name)) continue;

                schema._nameToIndex[name] = col;

                string typeStr = typeRow[col]?.ToString()?.Trim() ?? "";
                schema._nameToType[name] = ParseType(typeStr);
            }

            return schema;
        }

        // 查询时
        public bool TryGetIndex(string name, out int index)
            => _nameToIndex.TryGetValue(name.ToLower(), out index);

        public ExcelColumnType GetColumnType(string name)
            => _nameToType.GetValueOrDefault(name.ToLower(), ExcelColumnType.String);

        public List<string> GetAllColumns()
            => _nameToIndex.Keys.ToList();

        private static ExcelColumnType ParseType(string raw) => raw.ToLower() switch
        {
            "int" => ExcelColumnType.Int,
            "float" => ExcelColumnType.Float,
            "bool" => ExcelColumnType.Bool,
            "int[]" or "intarray" => ExcelColumnType.IntArray,
            "string[]" or "stringarray" => ExcelColumnType.StringArray,
            _ => ExcelColumnType.String
        };
    }
}