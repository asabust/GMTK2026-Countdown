using System;
using System.Data;
using Game.Runtime.Core.Attributes;
using Game.Runtime.Data;

namespace Game.Runtime.Core.ExcelTableReader
{
    [ExcelSheet("Item")]
    public class ItemTableReader : IExcelTableReader
    {
        private const int DataStartRow = 3; //0是表头，1是类型，2是说明 

        public void Read(DataTable table, ExcelTableContext context)
        {
            ColumnSchema schema = ColumnSchema.Build(table);
            for (var i = DataStartRow; i < table.Rows.Count; i++)
            {
                var row = new SmartRow(table.Rows[i], schema);

                // 跳过空行
                if (row.IsEmpty("ItemId")) continue;

                int id = row.GetInt("ItemId");

                if (context.items.ContainsKey(id))
                    throw new Exception($"道具数据重复 item_id={id} ");

                var item = new ItemData
                {
                    id = id,
                    name = row.GetString("ItemName"),
                    description = row.GetString("Desc"),
                    iconName = row.GetString("Icon"),
                };

                context.items[item.id] = item;
            }
        }
    }
}