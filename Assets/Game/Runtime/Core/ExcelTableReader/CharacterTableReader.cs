using Game.Runtime.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Game.Runtime.Core.ExcelTableReader
{
    [ExcelSheet("Character")]
    public class CharacterTableReader : IExcelTableReader
    {
        private const int DataStartRow = 3;

        public void Read(DataTable table, ExcelTableContext context)
        {
            // 只在这里建一次 schema，后续行复用
            ColumnSchema schema = ColumnSchema.Build(table);

            for (int i = DataStartRow; i < table.Rows.Count; i++)
            {
                //按列名取值，自动转换
                var row = new SmartRow(table.Rows[i], schema);

                if (row.IsEmpty("CharacterId")) continue;

                int id = row.GetInt("CharacterId");
                if (context.characters.ContainsKey(id))
                    throw new Exception($"角色数据重复 character_id={id}");

                context.characters[id] = new CharacterData
                {
                    id = id,
                    name = row.GetString("Name"),
                    species = row.GetString("Species"),
                    description = row.GetString("Desc"),
                    homePlanet = row.GetInt("Planet"),
                    planetOption = row.GetIntArray("PlanetOption").ToList(),
                    itemIds = row.GetIntArray("Items").ToList(),
                    questions =
                        new List<string>
                        {
                            row.GetString("Question1"), row.GetString("Question2"), row.GetString("Question3"),
                        },
                    shortQuestions =
                        new List<string>
                        {
                            row.GetString("Keyword1"), row.GetString("Keyword2"), row.GetString("Keyword3"),
                        },
                    answers = new List<string>
                    {
                        row.GetString("Answer1"), row.GetString("Answer2"), row.GetString("Answer3"),
                    },
                    glitterPrefab = row.GetString("GlitterPrefab"),
                    profile = row.GetString("Profile"),
                    portrait = row.GetString("Portrait"),
                    fullBody = row.GetString("FullBody"),
                    xray = row.GetString("Xray"),
                };
            }
        }
    }
}