using Game.Runtime.Core.Attributes;
using Game.Runtime.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Game.Runtime.Core.ExcelTableReader
{
    [ExcelSheet("Localization")]
    public class LocalizationTableReader : IExcelTableReader
    {
        private const int DataStartRow = 3;

        public void Read(DataTable table, ExcelTableContext context)
        {
            ColumnSchema schema = ColumnSchema.Build(table);

            // 找出所有语言列，即除了 Key 以外的所有列
            List<(Language lang, string column)> langColumns = schema.GetAllColumns()
                .Where(col => !col.Equals("Key", StringComparison.OrdinalIgnoreCase))
                .Select(col =>
                {
                    if (!Enum.TryParse<Language>(col, true, out var lang))
                    {
                        throw new Exception($"Invalid language column: {col}");
                    }

                    return (lang, col);
                })
                .ToList();

            for (int i = DataStartRow; i < table.Rows.Count; i++)
            {
                var row = new SmartRow(table.Rows[i], schema);

                if (row.IsEmpty("Key")) continue;

                string key = row.GetString("Key");

                foreach ((Language lang, string column) in langColumns)
                {
                    string text = row.GetString(column);
                    context.localizationData.Add(lang, key, text);
                }
            }
        }
    }
}