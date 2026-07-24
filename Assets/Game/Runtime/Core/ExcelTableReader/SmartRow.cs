// SmartRow.cs

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Game.Runtime.Core.ExcelTableReader
{
    /// <summary>
    /// 按列名取值，自动转换
    /// </summary>
    public class SmartRow
    {
        private readonly DataRow _row;
        private readonly ColumnSchema _schema;

        public SmartRow(DataRow row, ColumnSchema schema)
        {
            _row = row;
            _schema = schema;
        }

        public bool IsEmpty(string colName)
        {
            if (!_schema.TryGetIndex(colName, out int idx)) return true;
            return ExcelCellParser.IsEmpty(_row, idx);
        }

        // 自动根据 schema 中的类型信息返回 object，
        // 推荐下面这些强类型方法直接使用：

        public string GetString(string col) => Get<string>(col);
        public int GetInt(string col) => Get<int>(col);
        public float GetFloat(string col) => Get<float>(col);
        public bool GetBool(string col) => Get<bool>(col);
        public int[] GetIntArray(string col) => Get<int[]>(col);
        public string[] GetStringArray(string col) => Get<string[]>(col);

        private T Get<T>(string colName)
        {
            if (!_schema.TryGetIndex(colName, out int idx))
                throw new Exception($"列 '{colName}' 不存在于表头中");

            return typeof(T) switch
            {
                var t when t == typeof(string) => (T)(object)ExcelCellParser.GetString(_row, idx),
                var t when t == typeof(int) => (T)(object)ExcelCellParser.GetInt(_row, idx),
                var t when t == typeof(float) => (T)(object)ExcelCellParser.GetFloat(_row, idx),
                var t when t == typeof(bool) => (T)(object)ExcelCellParser.GetBool(_row, idx),
                var t when t == typeof(int[]) => (T)(object)ExcelCellParser.GetIntArray(_row, idx),
                var t when t == typeof(string[]) => (T)(object)ExcelCellParser.GetStringArray(_row, idx),
                var t when t == typeof(List<int>) => (T)(object)ExcelCellParser.GetIntArray(_row, idx).ToList(),
                var t when t == typeof(List<string>) => (T)(object)ExcelCellParser.GetStringArray(_row, idx).ToList(),
                _ => throw new Exception($"不支持的类型 {typeof(T).Name}")
            };
        }
    }
}