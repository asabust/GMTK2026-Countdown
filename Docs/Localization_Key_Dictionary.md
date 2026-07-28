# 《发条归零之前》本地化 Key 字典

> 语言：English / Chinese / Japanese
>
> Key 总数：232
>
> 源表：`Assets/GameData/database.xlsx` 的 `Localization` Sheet
>
> 运行时数据：`Assets/Resources/GameData/database.json`
>
> 最后核对：2026-07-28

本文档用于快速确认某段文本对应的 Key、格式参数和当前调用位置。翻译内容仍以 Excel 源表为唯一来源；本文档只记录当前中文，避免形成第二份翻译源。

机制语义参见 [完赛版 GDD](Zero_GDD.md)；Excel 导出和三语检查步骤参见 [策划配置与关卡编辑指南](Designer_Config_and_Level_Guide.md)。

---

## 1. 使用方法

### 1.1 修改现有文本

1. 在本文档搜索界面、中文片段或 Key。
2. 打开 `Assets/GameData/database.xlsx`。
3. 在 `Localization` Sheet 搜索第一列 Key。
4. 同时维护 English、Chinese、Japanese。
5. 保留所有 `{0}`、`{1}` 等格式参数。
6. Unity 中执行 `Tools > Export Database To JSON`。
7. 在游戏中切换三种语言验证。

### 1.2 新增 Key

- 命名使用小写功能域开头：`功能.子功能.语义`。
- 优先复用现有前缀，不创建含义重复的 Key。
- 按用途命名，不把中文或具体数值写进 Key。
- 带格式参数时从 `{0}` 开始连续编号。
- 三种语言必须在同一行补齐。
- 新增后重新导出 JSON，并同步更新本文档。

### 1.3 使用状态

| 状态 | 含义 |
|---|---|
| `使用：File.cs` | 在代码或序列化资源中发现直接 Key 引用 |
| `动态：Definition...` | Key 由配置 ID 拼接生成，不能因为搜不到完整字符串就删除 |
| `历史：...` | 已确认当前流程不读取，但仍保留在语言表 |
| `候选遗留` | 未发现直接或已知动态引用；删除前仍需回归对应界面 |

---

## 2. 本次审计结果

| 检查项 | 结果 |
|---|---|
| Excel 有效 Key | 232 |
| JSON 有效 Key | 232 |
| 重复 Key | 0 |
| 空翻译 | 0 |
| 三语占位符不一致 | 0 |
| Excel / JSON 内容差异 | 0 |
| 代码引用但语言表缺失 | 0 |
| 配置动态 Key 缺失 | 0 |
| 候选遗留 Key | 38 |

结构审计通过。`GameLocalization` 不再维护手写 fallback；运行时只读取导出的 JSON。Key 不存在或当前语言为空时显示 `[MISSING:语言:Key]`，格式参数错误时显示 `[FORMAT:语言:Key]`。

### 2.1 非阻断文案一致性建议

| Key | 建议 |
|---|---|
| `campfire.result` | English 使用 `HP`，而中文、日文和玩家资源机制使用 Point |
| `collectible.magic_potion.description` | English 使用 `HP`，建议与 Point 机制统一 |
| `offering.result.return` | English 使用 `HP`，实际为返还 Point |
| `battle.reward.prompt` | `{0}` 实际传入敌人名称；English 可改为 `You defeated {0}. Attempt a Greed check?` |
| `enemy.intent.attack_and_steal` | English / Chinese 建议补充分隔标点，便于区分攻击伤害与偷取数量 |

---

## 3. 占位符规范

- `{0}`、`{1}` 等由 `string.Format` 在运行时注入。
- 三种语言可以调整参数顺序，但不能遗漏参数。
- 不要把半角花括号改成全角符号。
- 当前系统使用数字索引，不使用 `{name}`。
- 没有动态参数的文本不需要添加 `{0}`。

---

## 4. 动态 Key 规则

| 类型 | 拼接规则 | ID 来源 |
|---|---|---|
| 技能名称 | `skill.{skillId}.name` | `SkillDefinition.skillId` |
| 技能说明 | `skill.{skillId}.description` | `SkillDefinition.skillId` |
| 道具 / 藏品名称 | `collectible.{collectibleId}.name` | `CollectibleDefinition.collectibleId` |
| 道具 / 藏品说明 | `collectible.{collectibleId}.description` | `CollectibleDefinition.collectibleId` |
| 普通敌人名称 | `enemy.{enemyId}.name` | `EnemyDefinition.enemyId` |
| 普通敌人说明 | `enemy.{enemyId}.description` | `EnemyDefinition.enemyId` |
| Boss 名称 / 说明 | `enemy.Boss.name / description` | Boss 行为强制使用 `Boss` |

`enemy.BossPhase1.*` 与 `enemy.BossPhase2.*` 当前不会显示：Boss 的 `LocalizationId` 会统一返回 `Boss`。

---

## 5. 前缀索引

| 前缀 | 数量 | 用途 |
|---|---:|---|
| `battle.*` | 62 | 战斗菜单、行动、掉落、Roll、奖励与战斗状态 |
| `campfire.*` | 5 | 篝火休整与恢复反馈 |
| `collectible.*` | 14 | 道具与藏品的动态名称和说明 |
| `common.*` | 13 | 跨界面通用文本 |
| `enemy.*` | 49 | 敌人动态名称、奖励、意图与行动反馈 |
| `exchange.*` | 10 | 交换事件 |
| `game_over.*` | 8 | 失败与胜利面板 |
| `language.*` | 3 | 语言名称 |
| `offering.*` | 22 | 供奉事件 |
| `settings.*` | 3 | 设置面板 |
| `shop.*` | 17 | 商店界面 |
| `skill.*` | 10 | 普攻与技能的动态名称和说明 |
| `title.*` | 4 | 标题界面入口 |
| `treasure.*` | 12 | 技能、道具与藏品宝藏 |

---

## 6. 完整 Key 字典

### 6.1 `battle.*`

战斗菜单、行动、掉落、Roll、奖励与战斗状态

| Key | 当前中文 | 占位符 | 使用状态 |
|---|---|---|---|
| `battle.action.attack` | 下劈 | — | `候选遗留：未发现直接或动态引用` |
| `battle.action.attack_preview` | 普通攻击 消耗 {0} 伤害 {1}<br>  敌人生命：{2} > {3} | {0} {1} {2} {3} | 使用：`BattleActionPanel.cs` |
| `battle.action.auto_pass` | 点数为 0，挣扎已经用尽<br>  自动跳过回合…… | — | 使用：`BattleActionPanel.cs` |
| `battle.action.cannot_struggle` | 现在无法挣扎 | — | 使用：`BattleActionPanel.cs` |
| `battle.action.insufficient` | 点数不足，无法攻击 | — | 使用：`BattleActionPanel.cs` |
| `battle.action.struggle` | 挣扎 | — | 使用：`BattleActionPanel.cs` |
| `battle.action.struggle_preview` | 濒死挣扎 消耗 {0} 伤害 {1}<br>  敌人生命：{2} > {3} | {0} {1} {2} {3} | 使用：`BattleActionPanel.cs` |
| `battle.damage_suffix` | 伤害 {0} | {0} | `候选遗留：未发现直接或动态引用` |
| `battle.drop.failed` | {0}：未获得 | {0} | 使用：`EncounterController.cs` |
| `battle.drop.inventory_full` | {0}：道具栏已满，未获得 | {0} | 使用：`EncounterController.cs` |
| `battle.drop.maximum` | {0}：已达持有上限，未获得 | {0} | 使用：`EncounterController.cs` |
| `battle.drop.received` | 获得 {0} | {0} | 使用：`EncounterController.cs` |
| `battle.drop.summary` | 道具：{0} | {0} | `候选遗留：未发现直接或动态引用` |
| `battle.item.already_protected` | 少女的心事已经在保护你 | — | 使用：`EncounterController.cs` |
| `battle.item.attack_negated` | 少女的心事保护了你，免疫了本次攻击。 | — | 使用：`EncounterController.cs` |
| `battle.item.changed` | 道具数量发生变化，请重新选择 | — | 使用：`EncounterController.cs` |
| `battle.item.invalid` | 这个道具还没有配置战斗效果 | — | 使用：`EncounterController.cs` |
| `battle.item.not_owned` | 背包中没有这个道具 | — | 使用：`EncounterController.cs` |
| `battle.item.number_full` | 点数已满，暂时无法使用 | — | 使用：`EncounterController.cs` |
| `battle.item.shield_active` | 本回合的护盾已经生效 | — | 使用：`EncounterController.cs` |
| `battle.item.shield_blocked` | 守护者之盾抵挡了{0}点伤害。 | {0} | 使用：`EncounterController.cs` |
| `battle.items.empty_description` | 背包中的道具会显示在这里。 | — | 使用：`BattleActionPanel.cs` |
| `battle.items.none` | 没有道具 | — | 使用：`BattleActionPanel.cs` |
| `battle.player.button.back` | 返回 | — | 使用：`BattleActionPanel.cs` |
| `battle.player.items` | 道具 | — | 使用：`BattleActionPanel.cs` |
| `battle.player.skills` | 技能 | — | `候选遗留：未发现直接或动态引用` |
| `battle.player_stunned` | 玩家眩晕：跳过本回合 | — | 使用：`EncounterController.cs` |
| `battle.reward.additional` | 追加贪婪<br>  {0}% 本次 +{1}（累计 {2}）<br>  {3}% 本次 +0 | {0} {1} {2} {3} | 使用：`BattleRewardPanel.cs` |
| `battle.reward.additional_failed` | 本次贪婪失败，追加结束<br>  已获得：+{0} | {0} | 使用：`BattleRewardPanel.cs` |
| `battle.reward.additional_prompt` | 你成功了，就此收手，还是再赌一把？ | — | 使用：`BattleRewardPanel.cs` |
| `battle.reward.additional_success` | 你成功了，当前累计：{0}<br>  就此收手，还是再赌一把？ | {0} | 使用：`BattleRewardPanel.cs` |
| `battle.reward.button.again` | 再赌一把 | — | `候选遗留：未发现直接或动态引用` |
| `battle.reward.button.greedy` | 贪婪 | — | 使用：`BattleRewardPanel.cs` |
| `battle.reward.button.safe` | 见好就收 | — | 使用：`BattleRewardPanel.cs` |
| `battle.reward.greedy` | {0}% 获得 {1}<br>  {2}% 获得 0 | {0} {1} {2} | 使用：`BattleRewardPanel.cs` |
| `battle.reward.greedy_description` | 50% 概率获得 2.5 倍点数；50% 概率失去本轮全部点数收获（保留道具）。 | — | `候选遗留：未发现直接或动态引用` |
| `battle.reward.item_safety` | 道具不会因贪婪失败而丢失 | — | 使用：`BattleRewardPanel.cs` |
| `battle.reward.mirror_hint` | 破碎的镜子给予你机会 | — | `候选遗留：未发现直接或动态引用` |
| `battle.reward.prompt` | 你击败了{0}，是否进行贪婪检定？ | {0} | 使用：`BattleRewardPanel.cs` |
| `battle.reward.result.failed` | 检定失败，真可惜，你一无所有。 | — | 使用：`BattleRewardPanel.cs` |
| `battle.reward.result.success` | 检定成功，恭喜你，幸运之人。你获得了{0}点。 | {0} | 使用：`BattleRewardPanel.cs` |
| `battle.reward.safe` | 获得 {0}（100%） | {0} | 使用：`BattleRewardPanel.cs` |
| `battle.reward.safe_result` | 安全领取：+{0} | {0} | 使用：`BattleRewardPanel.cs` |
| `battle.reward.stop` | 收手，获得 {0} | {0} | 使用：`BattleRewardPanel.cs` |
| `battle.reward.stopped_result` | 收手领取：+{0} | {0} | 使用：`BattleRewardPanel.cs` |
| `battle.reward.summary.fixed` | 本场点数：{0} | {0} | `候选遗留：未发现直接或动态引用` |
| `battle.reward.summary.health` | 锁定生命：{0}<br>  生命掉落：50%<br>  本场点数：{1} | {0} {1} | `候选遗留：未发现直接或动态引用` |
| `battle.reward.summary.turn` | 锁定生命：{0}<br>  击杀回合：{1}<br>  掉落倍率：{2}%<br>  本场点数：{3} | {0} {1} {2} {3} | `候选遗留：未发现直接或动态引用` |
| `battle.roll.button.roll` | 是<br> 敌方血量会在{0}～{1}内随机取值 | {0} {1} | 使用：`PreBattleRollPanel.cs` |
| `battle.roll.button.stable` | 否<br> 敌方血量为 {0} | {0} | 使用：`PreBattleRollPanel.cs` |
| `battle.roll.encounter` | 你遇到了{0}。 | {0} | 使用：`PreBattleRollPanel.cs` |
| `battle.roll.health_range` | 点数范围：{0}～{1} | {0} {1} | `候选遗留：未发现直接或动态引用` |
| `battle.roll.health_range_with_description` | {0}<br> <br> 是否要决定{1}的命运？ | {0} {1} | 使用：`PreBattleRollPanel.cs` |
| `battle.roll.resolved` | {0}的血量为{1}，祝你好运。 | {0} {1} | `候选遗留：未发现直接或动态引用` |
| `battle.roll.reward` | 掉落：{0} | {0} | 使用：`PreBattleRollPanel.cs` |
| `battle.skill.cooldown` | 冷却中：还需等待 {0} 回合 | {0} | 使用：`EncounterController.cs` |
| `battle.skill.invalid` | 这个技能无法在技能栏中使用 | — | 使用：`EncounterController.cs` |
| `battle.skill.not_learned` | 尚未掌握这个技能 | — | 使用：`EncounterController.cs` |
| `battle.status.shield` | 护盾 {0} | {0} | 使用：`PlayerBattleStatusWorldUI.cs` |
| `battle.validation.insufficient` | 点数不足，无法使用 | — | 使用：`EncounterController.cs` |
| `battle.validation.used` | 已使用 {0} | {0} | 使用：`EncounterController.cs` |
| `battle.validation.wrong_phase` | 只能在战斗回合使用 | — | 使用：`EncounterController.cs` |

### 6.2 `campfire.*`

篝火休整与恢复反馈

| Key | 当前中文 | 占位符 | 使用状态 |
|---|---|---|---|
| `campfire.description` | 休息后恢复 {0}～{1} | {0} {1} | 使用：`CampfirePanel.cs` |
| `campfire.full_confirmation` | 点数已满，仍要消耗这处篝火吗？ | — | 使用：`CampfirePanel.cs` |
| `campfire.rest_anyway` | 仍要休息 | — | 使用：`CampfirePanel.cs` |
| `campfire.result` | 沐浴在神秘的力量下，你回复了{0}点。 | {0} | 使用：`CampfireInteractable.cs` |
| `campfire.title` | 休整 | — | 使用：`CampfirePanel.cs` |

### 6.3 `collectible.*`

道具与藏品的动态名称和说明

| Key | 当前中文 | 占位符 | 使用状态 |
|---|---|---|---|
| `collectible.broken_mirror.description` | 首次贪婪成功后可再次贪婪；追加成功率依次为40%/30%/20%/10%，每次收益独立结算。参与3场贪婪战斗后破碎。 | — | `动态：CollectibleDefinition.collectibleId` |
| `collectible.broken_mirror.name` | 破碎的镜子 | — | `动态：CollectibleDefinition.collectibleId` |
| `collectible.girls_thoughts.description` | 替你承受一次攻击后销毁。 | — | `动态：CollectibleDefinition.collectibleId` |
| `collectible.girls_thoughts.name` | 少女的心事 | — | `动态：CollectibleDefinition.collectibleId` |
| `collectible.guardian_shield.description` | 增加6点护盾。 | — | `动态：CollectibleDefinition.collectibleId` |
| `collectible.guardian_shield.name` | 守护者之盾 | — | `动态：CollectibleDefinition.collectibleId` |
| `collectible.hungry_wolf.description` | 将贪婪奖励倍率改为2.4倍；参与3场贪婪战斗后破碎。 | — | `动态：CollectibleDefinition.collectibleId` |
| `collectible.hungry_wolf.name` | 饿狼 | — | `动态：CollectibleDefinition.collectibleId` |
| `collectible.lucky_coin.description` | 每个使首次贪婪成功率增加7%，最多持有2个。 | — | `动态：CollectibleDefinition.collectibleId` |
| `collectible.lucky_coin.name` | 幸运硬币 | — | `动态：CollectibleDefinition.collectibleId` |
| `collectible.magic_potion.description` | 回复6点。 | — | `动态：CollectibleDefinition.collectibleId` |
| `collectible.magic_potion.name` | 魔药 | — | `动态：CollectibleDefinition.collectibleId` |
| `collectible.wrench.description` | 接下来 3 次玩家行动中，下劈伤害 +2。可叠加。 | — | `动态：CollectibleDefinition.collectibleId` |
| `collectible.wrench.name` | 扳手 | — | `动态：CollectibleDefinition.collectibleId` |

### 6.4 `common.*`

跨界面通用文本

| Key | 当前中文 | 占位符 | 使用状态 |
|---|---|---|---|
| `common.cannot_claim` | 无法领取 | — | 使用：`CampfirePanel.cs` |
| `common.claim` | 领取 | — | 使用：`CampfirePanel.cs` |
| `common.current_number` | 当前点数：{0} | {0} | 使用：`ShopPanel.cs` |
| `common.empty` | 空 | — | 使用：`BattleActionPanel.cs` |
| `common.item` | 道具 | — | 使用：`CampfirePanel.cs`、`ShopPanel.cs` |
| `common.kind_description` | 【{0}】{1}<br>  {2} | {0} {1} {2} | `候选遗留：未发现直接或动态引用` |
| `common.leave` | 离开 | — | 使用：`CampfirePanel.cs` |
| `common.list_separator` | ； | — | 使用：`EncounterController.cs` |
| `common.relic` | 信物 | — | 使用：`CampfirePanel.cs`、`ShopPanel.cs` |
| `common.rest` | 休息 | — | 使用：`CampfirePanel.cs` |
| `common.skill` | 技能 | — | 使用：`BattleActionPanel.cs` |
| `common.unknown_item` | 未知道具 | — | 使用：`OfferingPanel.cs` |
| `common.unknown_reward` | 未知奖励 | — | 使用：`CampfirePanel.cs` |

### 6.5 `enemy.*`

敌人动态名称、奖励、意图与行动反馈

| Key | 当前中文 | 占位符 | 使用状态 |
|---|---|---|---|
| `enemy.Boss.description` | 在最后的倒计时前，掌握你自己的命运吧。 | — | `动态：EnemyDefinition.LocalizationId` |
| `enemy.Boss.name` | 哀叹之钟 | — | `动态：EnemyDefinition.LocalizationId` |
| `enemy.BossPhase1.description` | 在最后的倒计时前，掌握你自己的命运吧。 | — | `历史：Boss 当前统一读取 enemy.Boss.*` |
| `enemy.BossPhase1.name` | 哀叹之钟·一阶段 | — | `历史：Boss 当前统一读取 enemy.Boss.*` |
| `enemy.BossPhase2.description` | 时间...倒数...永恒！我要偷走你的一切。 | — | `历史：Boss 当前统一读取 enemy.Boss.*` |
| `enemy.BossPhase2.name` | 哀叹之钟·二阶段 | — | `历史：Boss 当前统一读取 enemy.Boss.*` |
| `enemy.DrunkenRaider.description` | 这家伙成天烂醉如泥。殴打他人、拿头撞墙，或者单纯地发呆……没有它做不到的事情。 | — | `动态：EnemyDefinition.LocalizationId` |
| `enemy.DrunkenRaider.name` | 发条油掠夺者 | — | `动态：EnemyDefinition.LocalizationId` |
| `enemy.Hamster.description` | 胆小如鼠的典型？小心它偷走你最重要的东西，然后……一去不回。 | — | `动态：EnemyDefinition.LocalizationId` |
| `enemy.Hamster.name` | 鼓鼓仓鼠 | — | `动态：EnemyDefinition.LocalizationId` |
| `enemy.HorrorBox.description` | 别被吓到，它能承受的压力比你想象的大。 | — | `动态：EnemyDefinition.LocalizationId` |
| `enemy.HorrorBox.name` | 惊魂匣 | — | `动态：EnemyDefinition.LocalizationId` |
| `enemy.SmallChicken.description` | 一只无害的可爱小鸡？友情提醒：它的喙可不好惹。 | — | `动态：EnemyDefinition.LocalizationId` |
| `enemy.SmallChicken.name` | 小绒鸡 | — | `动态：EnemyDefinition.LocalizationId` |
| `enemy.action.actual_stolen` | {0}（实际偷取 {1}） | {0} {1} | 使用：`EncounterController.cs` |
| `enemy.action.attack` | 敌人发动攻击，你损失了{0}点。 | {0} | 使用：`EnemyActor.cs` |
| `enemy.action.attack_and_steal` | 你损失了{0}点，被偷走了 {1}点。 | {0} {1} | 使用：`EnemyActor.cs` |
| `enemy.action.basic_attack` | 敌人发动攻击，你损失了{0}点。 | {0} | 使用：`EnemyActor.cs` |
| `enemy.action.boss_phase_transition` | 钟声未止：哀叹之钟·二阶段 | — | 使用：`EncounterController.cs` |
| `enemy.action.charge` | 正在蓄力中...... | — | 使用：`EnemyActor.cs` |
| `enemy.action.drink_self_damage` | 酒醉中打了自己一圈拳，自己损失{0}点。 | {0} | 使用：`EnemyActor.cs` |
| `enemy.action.drink_strengthen` | 喝了一口酒，感觉兴致勃勃，下次攻击点数 增加{0}。 | {0} | 使用：`EnemyActor.cs` |
| `enemy.action.drink_stunned` | 喝的酩酊大醉，进入晕眩，本回合无事发生。 | — | 使用：`EnemyActor.cs` |
| `enemy.action.escape` | 鼓鼓仓鼠带着你的 {0} 点逃跑了。 | {0} | 使用：`EnemyActor.cs` |
| `enemy.action.heavy_attack` | 给予你强力一击，你损失了{0}点。 | {0} | 使用：`EnemyActor.cs` |
| `enemy.action.item_stolen` | 钟声回荡，你永久失去 了{0} | {0} | 使用：`EncounterController.cs` |
| `enemy.action.no_item` | 你没有道具可供偷取，遭到击打，损失了{0}点。 | {0} | 使用：`EncounterController.cs` |
| `enemy.action.no_item_stunned` | 你没有道具可供偷取，遭到击打，损失了{0}点并眩晕 1 回合。 | {0} | 使用：`EncounterController.cs` |
| `enemy.action.overload_explosion` | 你被爆炸波及，损失了{0}点。 | {0} | 使用：`EncounterController.cs` |
| `enemy.action.peck` | 狠狠的啄了你一下，你损失了{0}点。 | {0} | 使用：`EnemyActor.cs` |
| `enemy.action.try_steal_item` | 哀叹之钟正在偷取你的道具。 | — | 使用：`EnemyActor.cs` |
| `enemy.action.wait` | 呆呆的看着前方，注意力似乎不在你身上。 | — | 使用：`EnemyActor.cs` |
| `enemy.intent.attack` | 意图：攻击 -{0} | {0} | 使用：`EnemyActor.cs` |
| `enemy.intent.attack_and_steal` | 意图：攻击 -{0} 偷取 {1} | {0} {1} | 使用：`EnemyActor.cs` |
| `enemy.intent.charge` | 意图：蓄力 | — | 使用：`EnemyActor.cs` |
| `enemy.intent.drink` | 意图：喝酒 | — | 使用：`EnemyActor.cs` |
| `enemy.intent.escape_with` | 意图：携带 {0} 点逃跑 | {0} | 使用：`EnemyActor.cs` |
| `enemy.intent.heavy_attack` | 意图：强力击 -{0} | {0} | 使用：`EnemyActor.cs` |
| `enemy.intent.peck` | 意图：啄地 -{0} | {0} | 使用：`EnemyActor.cs` |
| `enemy.intent.steal_item` | 意图：偷取道具（无道具：-{0}并眩晕） | {0} | 使用：`EnemyActor.cs` |
| `enemy.intent.steal_point` | 意图：偷取 {0} | {0} | `候选遗留：未发现直接或动态引用` |
| `enemy.intent.wait` | 意图：等待 | — | 使用：`EnemyActor.cs` |
| `enemy.reward.health_scaled` | 生命×{0}% | {0} | 使用：`EnemyDefinition.cs` |
| `enemy.reward.items` | + 道具×{0} | {0} | 使用：`EnemyDefinition.cs` |
| `enemy.reward.none` | 无掉落 | — | 使用：`EnemyDefinition.cs` |
| `enemy.reward.turn_result` | {0}（第{1}回合） | {0} {1} | 使用：`EnemyActor.cs` |
| `enemy.reward.turn_scaled` | 生命×80%～45% | — | 使用：`EnemyDefinition.cs` |
| `enemy.world.health` | HP {0}/{1} | {0} {1} | 使用：`EnemyWorldUI.cs` |
| `enemy.world.reward` | 惊魂匣的赠礼： {0} | {0} | 使用：`BattleRewardPanel.cs` |

### 6.6 `exchange.*`

交换事件

| Key | 当前中文 | 占位符 | 使用状态 |
|---|---|---|---|
| `exchange.button.confirm` | 交换 | — | 使用：`ShopPanel.cs` |
| `exchange.button.leave` | 离开 | — | 使用：`ShopPanel.cs` |
| `exchange.failed` | 交换失败，请重新选择 | — | 使用：`ShopPanel.cs` |
| `exchange.item_description` | 【{0}】{1}<br>{2}<br><br>要交出「{1}」，随机换取一件其他道具吗？ | {0} {1} {2} | 使用：`ShopPanel.cs` |
| `exchange.no_alternative` | 暂时没有可以交换的其他道具 | — | 使用：`ShopPanel.cs` |
| `exchange.prompt` | 是否用你拥有的道具换取另一个道具？ | — | 使用：`ShopPanel.cs` |
| `exchange.result.no_items` | 真遗憾，你没有足够的道具和我交换。 | — | 使用：`ShopPanel.cs` |
| `exchange.result.success` | 以物易物，很划算吧。{0}属于你了。 | {0} | 使用：`ShopPanel.cs` |
| `exchange.success` | 用 {0} 换到了 {1} | {0} {1} | `候选遗留：未发现直接或动态引用` |
| `exchange.title` | 交换 | — | `候选遗留：未发现直接或动态引用` |

### 6.7 `game_over.*`

失败与胜利面板

| Key | 当前中文 | 占位符 | 使用状态 |
|---|---|---|---|
| `game_over.final_number` | 最终点数：{0} | {0} | `候选遗留：未发现直接或动态引用` |
| `game_over.reason.combat` | 倒数的秒针追上了你，你再一次坠入了无尽的深渊。 | — | `候选遗留：未发现直接或动态引用` |
| `game_over.reason.default` | 倒数的秒针追上了你，你再一次坠入了无尽的深渊。 | — | 使用：`GameManager.cs` |
| `game_over.reason.movement` | 倒数的秒针追上了你，你再一次坠入了无尽的深渊。 | — | `候选遗留：未发现直接或动态引用` |
| `game_over.reason.victory` | 恭喜你，在不断倒数的钟声里拯救了所有人。 | — | 使用：`EncounterController.cs` |
| `game_over.retry` | 重新开始 | — | 使用：`GameOverPanel.cs` |
| `game_over.return_title` | 返回标题 | — | 使用：`GameOverPanel.cs` |
| `game_over.victory_title` | 胜利通关 | — | 使用：`GameManager.cs` |

### 6.8 `language.*`

语言名称

| Key | 当前中文 | 占位符 | 使用状态 |
|---|---|---|---|
| `language.chinese` | 简体中文 | — | 使用：`SettingsPanel.cs` |
| `language.english` | English | — | 使用：`SettingsPanel.cs` |
| `language.japanese` | 日本語 | — | 使用：`SettingsPanel.cs` |

### 6.9 `offering.*`

供奉事件

| Key | 当前中文 | 占位符 | 使用状态 |
|---|---|---|---|
| `offering.button.confirm` | 供奉 | — | 使用：`OfferingPanel.cs` |
| `offering.button.leave` | 离开 | — | 使用：`OfferingPanel.cs` |
| `offering.error.amount` | 供奉点数必须至少为 1，且不能超过当前点数。 | — | 使用：`OfferingPanel.cs` |
| `offering.error.configuration` | 供奉配置无效，请检查结果概率。 | — | 使用：`OfferingPanel.cs` |
| `offering.error.insufficient` | 点数不足，无法完成供奉。 | — | 使用：`OfferingPanel.cs` |
| `offering.item.invalid` | 没有可用的供奉道具奖励，请检查配置。 | — | 使用：`OfferingPanel.cs` |
| `offering.item.inventory_full` | 道具栏已满，本次奖励已放弃。 | — | 使用：`OfferingPanel.cs` |
| `offering.item.maximum` | {0} 已达到最大数量，本次奖励已放弃。 | {0} | 使用：`OfferingPanel.cs` |
| `offering.item.success` | 获得了道具：{0}。 | {0} | `候选遗留：未发现直接或动态引用` |
| `offering.no_number` | 你已没有可以供奉的点数，正处于濒死状态。 | — | 使用：`OfferingPanel.cs` |
| `offering.preview` | 供奉后：{0} > {1} | {0} {1} | `候选遗留：未发现直接或动态引用` |
| `offering.prompt` | 是否为了信仰或回报，划动发条，献上你的祭品？ | — | 使用：`OfferingPanel.cs` |
| `offering.result.attack` | 本局基础攻击力提高 {0} 点。 | {0} | 使用：`OfferingPanel.cs` |
| `offering.result.complete` | 供奉已经完成。 | — | 使用：`OfferingPanel.cs` |
| `offering.result.double` | 获得双倍返还：{0} 点点数。 | {0} | 使用：`OfferingPanel.cs` |
| `offering.result.item` | 神的眷顾，你获得了{0}个道具{1}。 | {0} {1} | 使用：`OfferingPanel.cs` |
| `offering.result.lose_all` | 供奉没有得到回报，失去了 {0} 点点数。 | {0} | 使用：`OfferingPanel.cs` |
| `offering.result.no_blessing` | 很可惜，你似乎并没有被赐福。 | — | `候选遗留：未发现直接或动态引用` |
| `offering.result.number` | 神的眷顾，你获得了{0}点。 | {0} | `候选遗留：未发现直接或动态引用` |
| `offering.result.return` | 神似乎拒绝了你，返还了你{0}点。 | {0} | 使用：`OfferingPanel.cs` |
| `offering.result_title` | 供奉结果 | — | `候选遗留：未发现直接或动态引用` |
| `offering.title` | 供奉 | — | 使用：`OfferingPanel.cs` |

### 6.10 `settings.*`

设置面板

| Key | 当前中文 | 占位符 | 使用状态 |
|---|---|---|---|
| `settings.language` | 语言 | — | 使用：`SettingsPanel.cs` |
| `settings.sfx` | 音效 | — | 使用：`SettingsPanel.cs` |
| `settings.volume` | 音乐 | — | 使用：`SettingsPanel.cs` |

### 6.11 `shop.*`

商店界面

| Key | 当前中文 | 占位符 | 使用状态 |
|---|---|---|---|
| `shop.button.leave` | 离开 | — | 使用：`ShopPanel.cs` |
| `shop.button.sold_out` | 已售罄 | — | 使用：`ShopPanel.cs` |
| `shop.buy_for` | 购买 {0} | {0} | 使用：`ShopPanel.cs` |
| `shop.feedback.insufficient` | 点数不足，当前无法购买 | — | 使用：`ShopPanel.cs` |
| `shop.feedback.purchase_success` | 购买成功 | — | 使用：`ShopPanel.cs` |
| `shop.feedback.sold_out` | 这件商品已经售罄 | — | 使用：`ShopPanel.cs` |
| `shop.greeting` | 用你最宝贵的点数换取这些小玩意，很不错，不是吗？ | — | `候选遗留：未发现直接或动态引用` |
| `shop.point_preview` | 点数：{0} → {1} | {0} {1} | `候选遗留：未发现直接或动态引用` |
| `shop.product_description` | 【{0}】{1}<br>  {2}<br>  点数：{3} > {4} | {0} {1} {2} {3} {4} | 使用：`ShopPanel.cs` |
| `shop.purchase.insufficient` | 点数不足，购买后不能低于 0 | — | `候选遗留：未发现直接或动态引用` |
| `shop.purchase.inventory_full` | 道具栏已满，无法购买 | — | 使用：`ShopPanel.cs` |
| `shop.purchase.item_maximum` | 该道具已达到最大数量 | — | 使用：`ShopPanel.cs` |
| `shop.purchase.relic_maximum` | 该藏品已达到最大层数 | — | 使用：`ShopPanel.cs` |
| `shop.purchase.sold_out` | 这件商品已经售罄 | — | `候选遗留：未发现直接或动态引用` |
| `shop.purchase.success` | 购买成功 | — | `候选遗留：未发现直接或动态引用` |
| `shop.purchase.unavailable` | 当前无法购买 | — | 使用：`ShopPanel.cs` |
| `shop.state.sold_out` | 售罄 | — | 使用：`ShopPanel.cs` |

### 6.12 `skill.*`

普攻与技能的动态名称和说明

| Key | 当前中文 | 占位符 | 使用状态 |
|---|---|---|---|
| `skill.basic_attack.description` | 消耗1点，对敌方造成3点伤害。<br> 冷却：0回合。 | — | 使用：`BattleActionPanel.cs` |
| `skill.basic_attack.name` | 下劈 | — | 使用：`BattleActionPanel.cs` |
| `skill.bloodlust.description` | 消耗1点，后两次下劈的伤害增加100%。<br> 冷却：2回合。 | — | `动态：SkillDefinition.skillId` |
| `skill.bloodlust.name` | 喋血 | — | `动态：SkillDefinition.skillId` |
| `skill.defense.description` | 消耗 1 点，减免受到伤害的 45%。冷却：0 回合。 | — | `候选遗留：未发现直接或动态引用` |
| `skill.defense.name` | 防御 | — | `候选遗留：未发现直接或动态引用` |
| `skill.parasite.description` | 消耗4点，对敌方造成6点伤害。若此次攻击击败敌方，则额外回复3点。<br> 冷却：2回合。 | — | `动态：SkillDefinition.skillId` |
| `skill.parasite.name` | 寄生 | — | `动态：SkillDefinition.skillId` |
| `skill.revenge.description` | 消耗3点，对敌方连续攻击2～3次，每次造成5点伤害；50%概率攻击3次。<br> 冷却：3回合。 | — | `动态：SkillDefinition.skillId` |
| `skill.revenge.name` | 复仇 | — | `动态：SkillDefinition.skillId` |

### 6.13 `title.*`

标题界面入口

| Key | 当前中文 | 占位符 | 使用状态 |
|---|---|---|---|
| `title.language` | 语言：{0} | {0} | `候选遗留：未发现直接或动态引用` |
| `title.new_game` | 开始游戏 | — | 使用：`TitlePanel.cs` |
| `title.quit` | 退出游戏 | — | 使用：`SettingsPanel.cs`、`TitlePanel.cs` |
| `title.settings` | 设置 | — | 使用：`SettingsPanel.cs`、`TitlePanel.cs` |

### 6.14 `treasure.*`

技能、道具与藏品宝藏

| Key | 当前中文 | 占位符 | 使用状态 |
|---|---|---|---|
| `treasure.cannot_claim_kind` | 现在无法获得该{0} | {0} | 使用：`CampfirePanel.cs` |
| `treasure.collectible_missing` | 宝藏中没有配置道具或藏品。 | — | 使用：`CampfirePanel.cs` |
| `treasure.learn_skill` | 学习技能 | — | 使用：`CampfirePanel.cs` |
| `treasure.maximum_reached` | 该{0}已达到持有上限 | {0} | 使用：`CampfirePanel.cs` |
| `treasure.receive_failed` | 未能获得奖励 | — | 使用：`CampfirePanel.cs` |
| `treasure.received` | 真幸运，你获得了{0}！ | {0} | 使用：`CampfirePanel.cs` |
| `treasure.skill_description` | 消耗 {0}{1} CD {2}<br>  {3} | {0} {1} {2} {3} | `候选遗留：未发现直接或动态引用` |
| `treasure.skill_missing` | 宝藏中没有配置技能。 | — | 使用：`CampfirePanel.cs` |
| `treasure.skill_owned` | 已经掌握该技能 | — | 使用：`CampfirePanel.cs` |
| `treasure.skill_owned_short` | 已掌握 | — | 使用：`CampfirePanel.cs` |
| `treasure.title` | 宝藏 | — | 使用：`CampfirePanel.cs` |
| `treasure.title_named` | 宝藏：{0} | {0} | `候选遗留：未发现直接或动态引用` |

---

## 7. 候选遗留 Key

以下 Key 未发现直接引用，也不属于已知动态拼接规则。它们可能来自旧版界面或已取消流程；删除前应打开对应界面回归一次。

- `battle.action.attack`
- `battle.damage_suffix`
- `battle.drop.summary`
- `battle.player.skills`
- `battle.reward.button.again`
- `battle.reward.greedy_description`
- `battle.reward.mirror_hint`
- `battle.reward.summary.fixed`
- `battle.reward.summary.health`
- `battle.reward.summary.turn`
- `battle.roll.health_range`
- `battle.roll.resolved`
- `common.kind_description`
- `enemy.BossPhase1.description`
- `enemy.BossPhase1.name`
- `enemy.BossPhase2.description`
- `enemy.BossPhase2.name`
- `enemy.intent.steal_point`
- `exchange.success`
- `exchange.title`
- `game_over.final_number`
- `game_over.reason.combat`
- `game_over.reason.movement`
- `offering.item.success`
- `offering.preview`
- `offering.result.no_blessing`
- `offering.result.number`
- `offering.result_title`
- `shop.greeting`
- `shop.point_preview`
- `shop.purchase.insufficient`
- `shop.purchase.sold_out`
- `shop.purchase.success`
- `skill.defense.description`
- `skill.defense.name`
- `title.language`
- `treasure.skill_description`
- `treasure.title_named`

---

## 8. 维护检查清单

- [ ] Excel 中 English / Chinese / Japanese 均非空。
- [ ] 同一 Key 只出现一次。
- [ ] 三种语言的占位符集合一致。
- [ ] 已执行 `Tools > Export Database To JSON`。
- [ ] Excel 与 JSON 的 Key 数量和内容一致。
- [ ] 新增的技能、道具、藏品和敌人 ID 均有动态名称与说明 Key。
- [ ] 游戏内切换三种语言后，关键界面没有 `[MISSING]` 或 `[FORMAT]`。
- [ ] 修改机制数值后，同步检查描述文本是否过期。
- [ ] 删除 Key 前先确认它不是动态 Key。

