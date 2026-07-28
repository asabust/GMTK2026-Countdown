# 《发条归零之前》策划配置与关卡编辑指南

> 面向：关卡策划、数值策划、叙事与本地化维护人员
>
> Unity：2022.3.62f3
>
> 最后核对：2026-07-28

这份文档回答“在哪里改、怎么改、改完如何验证”。游戏规则与最终数值请查阅 [《发条归零之前》完赛版游戏设计与代码索引](Zero_GDD.md)。

策划不需要修改 C# 才能完成常规的关卡摆放、敌人数值、技能数值、商店、供奉、宝藏和文本配置。涉及新机制、新敌人行为、新道具效果或新调试功能时，再交给程序处理。

---

## 0. 最常用入口

| 想做的事 | 打开位置 | 修改对象 |
|---|---|---|
| 编辑正式关卡 | `Assets/Scenes/GameScene.unity` | Tilemap、敌人和世界事件 Prefab 实例 |
| 完整试玩 | `Assets/Scenes/Persistent.unity` | 打开后进入 Play |
| 修改玩家初始 / 最大 Point | `Persistent.unity` → `Player` | `NumberResource` |
| 修改移动成本 | `Persistent.unity` → `Player` | `PlayerGridController` |
| 修改普攻、挣扎、基础贪婪 | `Persistent.unity` → `Player` | `EncounterController` |
| 修改视野 | `GameScene.unity` → `GridMap` | `FogOfWarSystem` |
| 修改敌人数值与意图循环 | `Assets/Game/Data/Enemies` | `EnemyDefinition` 资产 |
| 修改技能数值 | `Assets/Game/Data/Skills` | `SkillDefinition` 资产 |
| 修改道具 / 藏品 | `Assets/Game/Data/Collectibles` | `CollectibleDefinition` 资产 |
| 修改商店商品 | `Assets/Game/Data/Shops` | `ShopInventoryDefinition` 资产 |
| 修改供奉 | `Assets/Game/Data/Offerings/DefaultOffering.asset` | `OfferingDefinition` |
| 修改篝火恢复 | `Assets/Resources/World/TestCampfire.prefab` | `CampfireInteractable` |
| 修改宝藏奖励 | `GameScene.unity` 中的宝藏实例 | `TreasureInteractable` |
| 修改文本 | `Assets/GameData/database.xlsx` | `Localization` Sheet |
| 导出本地化 JSON | Unity 菜单 `Tools/Export Database To JSON` | 生成 `database.json` |
| 查看音频映射 | `Assets/Game/Editor/AudioInfoListSynchronizer.cs` | `AudioName` → 音频文件 |

---

## 1. 编辑前必须知道

### 1.1 三个主要场景

| 场景 | 用途 | 是否作为关卡编辑 |
|---|---|---|
| `Persistent.unity` | 常驻系统、玩家、UI、GameManager、AudioManager | 只改全局参数，不摆关卡 |
| `TitleScene.unity` | 标题场景 | 不摆关卡 |
| `GameScene.unity` | 正式 12×12 游戏关卡 | 是 |

`GameSceneOrigin.unity` 是工程中的旧版 / 参考场景，不是当前 `GameManager.firstGameScene`。除非明确要做备份或对照，不要把正式修改只做在这里。

Build Settings 当前顺序：

1. `Persistent`
2. `TitleScene`
3. `GameScene`
4. `GameSceneOrigin`

### 1.2 完整试玩方式

1. 保存所有场景和资产。
2. 打开 `Persistent.unity`。
3. 点击 Play。
4. 游戏会先进入标题，再由 `GameManager` 加载 `GameScene`。

不要把“直接 Play GameScene”作为最终验收方式。单独运行关卡场景可能缺少常驻玩家、UI、音频和流程管理对象。

### 1.3 安全编辑原则

- 只通过 Unity Inspector、Scene、Tile Palette 和 Project 窗口修改，不直接编辑 `.unity`、`.prefab` 或 `.asset` 的 YAML。
- 摆放关卡内容时使用现成 Prefab，不要新建空物体后手动拼组件。
- 修改 Prefab 实例上的引用属于该关卡的 Override；只想改一个点时不要 Apply 到整个 Prefab。
- 修改共用 Prefab 会影响所有实例；执行前先确认影响范围。
- 不要删除或重命名 `GridMap`、`Ground`、`Collision` 等基础对象。
- 不要让两个敌人 / 事件占据同一格。
- 改完先看 Console；网格放置失败会输出红色错误。

---

## 2. 地图和 Tilemap

### 2.1 当前地图规格

- Ground 有效范围为 12×12。
- 格子坐标：`x = 0～11`，`y = -11～0`。
- Grid Cell Size 为 `1 × 1`。
- 玩家出生格由 `GridMap.playerStartCell` 决定，当前为 `(0, 0)`。
- `Ground` 有 Tile 的格子才可作为地面。
- `Collision` 有 Tile 的格子不可行走。

### 2.2 编辑地面与墙

1. 打开 `GameScene.unity`。
2. 在 Hierarchy 找到 Grid 下的 `Ground` 或 `Collision`。
3. 打开 `Window > 2D > Tile Palette`。
4. 选中正确的 Tilemap 再绘制。
5. 绘制后确认 Ground 和 Collision 没有误画到相反层。

规则：

- 只画 Ground：可行走。
- Ground 与 Collision 同格：不可行走。
- 没有 Ground：视为地图外 / 不可行走。
- 删除地面前先检查该格是否放置了敌人或事件。

### 2.3 调整出生点

1. 选中 `GameScene` 中带 `GridMap` 组件的对象。
2. 修改 `Player Start Cell`。
3. 确认目标格有 Ground、没有 Collision、没有敌人或事件占位。
4. 从 `Persistent` 完整 Play，确认玩家出现并可移动。

出生点使用格子坐标，不是 Transform 世界坐标。

---

## 3. 放置敌人和地图事件

### 3.1 通用摆放流程

1. 从 `Assets/Resources/Enemies` 或 `Assets/Resources/World` 将 Prefab 拖入 `GameScene`。
2. 把根物体放到目标格内部。
3. 普通 1×1 对象建议将 Transform 对齐到格子中心：
   - `world x = cell x + 0.5`
   - `world y = cell y + 0.5`
4. 保持 `GridObject.snapToCellCenter` 开启。
5. 进入 Play 后，系统会根据物体所在格自动吸附到格子中心。
6. 查看 Console，确认没有 `Could not place ...`。

`GridObject` 使用 Transform 当前所在的格子作为占位原点。Scene 视图里看起来接近目标格还不够，必须确认 Transform 确实落在正确格子内。

### 3.2 1×1 与 Boss 2×2

- 普通怪和事件的 `GridEntity.size` 保持 `1,1`。
- Boss Prefab 已配置为 `2,2`，不要改为 1×1。
- Boss 根物体所在格是 2×2 占位的左下原点。
- Boss 的右侧、上方和右上方三个格也必须：
  - 有 Ground。
  - 没有 Collision。
  - 没有其他占位物。
- Play 时 Boss 根 Transform 会移动到整个 2×2 footprint 的中心，这是正常行为。

### 3.3 推荐检查

放置后至少检查：

- 玩家能从四方向中至少一个方向接触目标。
- 目标不会完全被 Collision 包围。
- 关键奖励不会被放在不可到达区域。
- Boss 周围留出玩家可以接触的相邻格。
- 迷雾中目标首次出现的位置符合教学节奏。

---

## 4. 敌人配置

### 4.1 放置哪种敌人

| Prefab | 对应默认配置 |
|---|---|
| `TestSmallChicken.prefab` | 小绒鸡；场景实例可以 Override 为易 / 难配置 |
| `DrunkenRaider.prefab` | 发条油掠夺者 |
| `HorrorBox.prefab` | 惊魂匣 |
| `Hamster.prefab` | 鼓鼓仓鼠 |
| `Boss.prefab` | 哀叹之钟第一阶段，并引用第二阶段 |

同一 Prefab 实例可以在 `EnemyActor > Definition` 上指定不同的 `EnemyDefinition`。

### 4.2 EnemyDefinition 常用字段

| Inspector 字段 | 策划含义 |
|---|---|
| `Enemy Id` | 唯一识别 ID；同时影响本地化键 |
| `Display Name` | 本地化缺失时的回退名称 |
| `Min HP / Max HP` | 普通怪 Roll 区间 |
| `Can Roll HP` | 是否打开战前 Roll 面板 |
| `Fixed HP` | `Can Roll HP` 关闭时使用 |
| `Reward Mode` | 普通回合倍率 / 生命倍率 / 旧固定模式 |
| `Attack Damage` | 普攻伤害 |
| `Special Damage` | 小鸡啄击或 Boss 强力击 |
| `Behavior Type` | 选择已经写好的怪物行为 |
| `Intent Sequence` | 固定意图循环 |
| `Item Drop Table` | 惊魂匣等可掉落的道具池 |
| `Item Drop Count` | 掉落数量 |

稳定生命不是直接读取普通怪的 `Fixed HP`，而是运行时计算：

```text
StableHP = min(MaxHP, floor((MinHP + MaxHP) / 2) + 1)
```

### 4.3 特殊字段

发条油掠夺者：

- `Raider Next Attack Bonus`
- `Raider Self Damage`
- 三个喝酒结果权重

鼓鼓仓鼠：

- `Hamster Steal Amount`

惊魂匣：

- `Health Reward Multiplier`
- `Horror Explosion Multiplier`
- `Item Drop Table`

Boss：

- `Boss Phase`
- `Boss Next Phase`
- `Boss No Item Damage`

不要通过更换 `Behavior Type` 期待自动生成全新行为。枚举只对应程序已经支持的五种逻辑。

---

## 5. 世界事件配置

### 5.1 篝火

Prefab：`Assets/Resources/World/TestCampfire.prefab`

组件：`CampfireInteractable`

| 字段 | 当前值 | 含义 |
|---|---:|---|
| `Minimum Restore` | 5 | 随机恢复下限 |
| `Maximum Restore` | 10 | 随机恢复上限 |

改 Prefab 会影响所有篝火。只想让某个篝火不同，可在场景实例上 Override。

### 5.2 商店

Prefab：`Assets/Resources/World/TestShop.prefab`

组件：`ShopInteractable`

场景实例上的 `Inventory Definition` 决定该商店出售什么。库存资产位于：

```text
Assets/Game/Data/Shops
```

编辑商品：

1. 选中一个 `ShopInventoryDefinition`。
2. 展开 `Products`。
3. 每行指定：
   - `Collectible`
   - `Price`
   - `Stock`
4. 回到场景，把商店实例的 `Inventory Definition` 指向该资产。

同一种商品需要多份库存时，可以提高 `Stock`；当前部分资产也使用“重复两行、每行库存 1”的方式。

### 5.3 供奉

Prefab：`Assets/Resources/World/TestOffering.prefab`

配置：`Assets/Game/Data/Offerings/DefaultOffering.asset`

| 字段 | 含义 |
|---|---|
| `Maximum Amount` | 单次最高供奉，程序硬上限 15 |
| `Outcomes` | 结果和整数权重 |
| `Item Pool` | 随机道具与权重 |

重要规则：

- 所有 Outcome 权重必须正好合计 100，否则供奉判为配置错误。
- `Item Pool` 应只放 `CollectibleKind.Item`。
- 当前正式概率为 35 / 20 / 30 / 15。
- 当前没有使用 `Attack Increase` 结果。
- 供奉 1～5 / 6～10 / 11～15 自动给 1 / 2 / 3 件道具，这个分段目前写在程序中，不是 Inspector 字段。

### 5.4 交换点

Prefab：`Assets/Resources/World/ExchangeEvent.prefab`

交换候选自动来自四种战斗道具，不需要逐实例配置。地图策划只负责位置和数量。

### 5.5 技能宝藏

直接使用三个固定 Prefab：

- `Treasure_Bloodlust.prefab`
- `Treasure_Parasite.prefab`
- `Treasure_Revenge.prefab`

它们已经正确引用技能资产。除非要更换奖励，不需要修改 Inspector。

### 5.6 道具 / 藏品宝藏

推荐使用 `Treasure_Item.prefab` 或 `CollectibleTreasure.prefab`。

选中场景实例，在 `TreasureInteractable` 上设置：

| 字段 | 设置 |
|---|---|
| `Reward Type` | `Collectible` |
| `Skill` | 留空 |
| `Collectible Reward` | 指定道具或藏品资产 |

只改场景实例即可让多个问号宝藏分别给予不同奖励，不必复制代码或 Prefab。

如果 `Reward Type` 与引用不一致，宝藏可能无法领取并在 Console 报错。

---

## 6. 玩家与战斗基础参数

这些参数位于 `Persistent.unity` 的根对象 `Player`。

### 6.1 NumberResource

| Inspector 字段 | 当前值 | 含义 |
|---|---:|---|
| `Initial Value` | 100 | 新局初始 Point |
| `Minimum Value` | -1 | 失败边界；代码会固定为 -1 |
| `Maximum Value` | 100 | Point 上限 |

### 6.2 PlayerGridController

| Inspector 字段 | 当前值 | 含义 |
|---|---:|---|
| `Move Cost` | 1 | 每次成功移动成本 |
| `Move Duration` | 0.12 | 单步移动演出时间 |
| `Move Action Path` | `Player/Move` | Input System Action |

键位在 `Assets/InputSystem_Actions.inputactions` 中配置。

### 6.3 EncounterController

| Inspector 字段 | 当前值 | 含义 |
|---|---:|---|
| `Basic Attack Cost` | 1 | 普攻 Point 成本 |
| `Basic Attack Damage` | 3 | 主角普攻基础伤害 |
| `Struggle Damage` | 1 | 0 点挣扎伤害 |
| `Enemy Action Duration` | 0.45 | 敌人行动表现时长 |
| `Enemy Action Impact Normalized Time` | 0.5 | 动画进行到多少比例时结算命中 |
| `Auto Pass Delay` | 0.7 | 无合法行动时自动跳过前的等待 |
| `Greedy Success Chance` | 0.5 | 基础贪婪成功率 |
| `Greedy Multiplier` | 2.5 | 基础贪婪倍率 |

主角普攻数字就在这里，不在 `SkillDefinition` 里。`skill.basic_attack.*` 本地化只负责显示名称与说明。

### 6.4 背包

`PlayerInventory`：

- `Limit Item Slots` 当前关闭。
- `Item Slot Capacity = 4` 只在启用限制后生效。
- 不要在正式场景的 `Stacks` 中预填内容；新局会清空。

---

## 7. 技能、道具和藏品

### 7.1 技能

路径：`Assets/Game/Data/Skills`

| 字段 | 用途 |
|---|---|
| `Skill Id` | 唯一 ID |
| `Display Name / Description` | 本地化缺失时的回退 |
| `Icon` | 战斗菜单图标 |
| `Skill Type` | 已实现的技能行为 |
| `Number Cost` | Point 消耗 |
| `Base Damage` | 直接伤害 |
| `Cooldown Turns` | 冷却 |
| `Bloodlust Basic Attacks / Multiplier` | 嗜血专用 |
| `Kill Restore` | 寄生击杀回复 |
| `Minimum / Maximum Hits` | 复仇攻击次数 |
| `Extra Hit Chance` | 复仇额外一击概率 |

更换 `Skill Type` 不会自动让所有字段生效；程序只读取该类型对应的一组字段。

### 7.2 道具与藏品

路径：`Assets/Game/Data/Collectibles`

| 字段 | 用途 |
|---|---|
| `Collectible Id` | 唯一 ID |
| `Kind` | Item 或 Relic |
| `Maximum Stacks` | 单种堆叠上限 |
| `Inventory Order` | HUD / 道具菜单排序 |
| `Effect Type` | 已实现的效果 |
| `Effect Value` | 数值，例如扳手 +2、盾 6 |
| `Effect Duration` | 持续玩家行动数 |
| `Relic Greed Battle Durability` | 参与多少场贪婪后破碎；0 表示不使用该耐久 |

修改机制数值后还要同步 Excel 中的中英日说明，避免玩家看到旧数字。

---

## 8. 本地化

### 8.1 正确工作流

1. 打开 `Assets/GameData/database.xlsx`。
2. 编辑 `Localization` Sheet。
3. 保持第一列 Key 不重复。
4. 填写 English、Chinese、Japanese。
5. 保存并关闭 Excel，避免文件锁定。
6. 回到 Unity 执行：

```text
Tools > Export Database To JSON
```

7. 确认 Console 输出成功。
8. 检查 `Assets/Resources/GameData/database.json` 已更新。
9. 在游戏设置中轮流切换三种语言检查。

### 8.2 占位符

- `{0}`、`{1}` 的数量和顺序必须在三种语言中保持兼容。
- 不要把中文全角括号写进占位符。
- 不要删除仍被代码使用的 Key。
- 换行可直接在单元格中输入，导出后会成为 `\n`。

### 8.3 名称与说明的来源

多数 `EnemyDefinition`、`SkillDefinition`、`CollectibleDefinition` 会优先读取本地化 Key，Inspector 中的中文名称 / 说明只是回退。

因此只改 ScriptableObject 的中文说明，并不能保证游戏里显示变化。

---

## 9. 音频配置

音频播放和映射以代码仓库当前内容为准。

### 9.1 文件与映射

| 内容 | 位置 |
|---|---|
| 音频文件 | `Assets/Arts/CountDownFX` |
| AudioName 枚举 | `Assets/Game/Runtime/Data/AudioInfoListSO.cs` |
| AudioName → 文件映射 | `Assets/Game/Editor/AudioInfoListSynchronizer.cs` |
| 生成的音频列表 | `Assets/GameData/AudioInfoListSO.asset` |
| 播放、音量与 UI 自动绑定 | `Assets/Game/Runtime/Gameplay/AudioManager.cs` |

`AudioInfoListSynchronizer` 会在 Unity Editor 加载后自动同步，也可手动执行：

```text
Tools > Zero > Synchronize Audio Info List
```

### 9.2 更换已有音效

最稳妥方式：

1. 保留同步器中约定的相对路径与文件名。
2. 用新音频替换 `Assets/Arts/CountDownFX` 下对应文件。
3. 等待 Unity 导入。
4. 执行 Synchronize。
5. Play 验证触发时机和音量。

如果修改文件名或路径，必须同时修改 `AudioInfoListSynchronizer.ClipPaths`，否则下次同步会把 Clip 清空。

### 9.3 新增音效

需要程序配合完成：

1. 在 `AudioName` 增加枚举。
2. 在 `ClipPaths` 增加文件路径。
3. 在正确的游戏事件调用 `PlaySFX` 或 `PlayMusic`。
4. 同步 `AudioInfoListSO.asset`。
5. 验证循环和音量。

仅把音频文件拖进 Project 不会自动播放。

### 9.4 当前空音频槽

以下 `AudioName` 当前存在，但仓库没有配置 Clip：

- `BgmBoss`
- `UiDeathPanel`
- `SfxOilDrink`

运行时处理：

- 空 SFX 会保持静默。
- 请求播放空 BGM 时不会切断当前正在播放的有效 BGM，因此 Boss 战会继续此前的 Gameplay BGM。

### 9.5 音量注意

- SettingsPanel 的音乐 / 音效音量存入 `PlayerPrefs`。
- `AudioInfoListSynchronizer` 当前会把每条资源音量重建为 1。
- 因此不要只在 `AudioInfoListSO.asset` Inspector 中长期调整单条音量；下次同步可能覆盖。
- 如果需要长期保留每条音频的独立音量，应修改同步器的映射数据结构或程序配置。

---

## 10. 调试与快速验证

### 10.1 当前已有能力

当前工程没有独立的“策划调试面板”或一键作弊开关。已有能力包括：

- Unity Inspector 中临时修改场景 / 资产参数。
- Console 中的配置错误和网格占位错误。
- `GameRandom.SetFixedSeed(int)` 与 `ClearFixedSeed()` 程序接口。
- Play Mode 中切换语言、音乐和音效。
- Scene 视图 Gizmos；当前没有自定义的网格占位 Gizmo。

固定随机种子目前没有 Inspector 或菜单入口，策划无法在不改代码的情况下启用。

### 10.2 可用的临时测试办法

以下修改只建议用于本地测试，完成后必须还原：

| 测试目标 | 临时设置 |
|---|---|
| 快速看完整地图 | 将 `GameScene > FogOfWarSystem > Vision Radius` 临时改为 20 |
| 快速击杀敌人 | 将 `Persistent > Player > EncounterController > Basic Attack Damage` 临时调高 |
| 测试低 Point | 将 `NumberResource.Initial Value` 改低后重新进入 Play |
| 测试大量 Point | 同时提高 `Initial Value` 与 `Maximum Value` 后重新进入 Play |
| 保证贪婪成功 | 将 `Greedy Success Chance` 临时改为 1 |
| 保证贪婪失败 | 将 `Greedy Success Chance` 临时改为 0 |
| 加速走路演出 | 将 `Move Duration` 临时改为 0 |
| 快速测试某敌人 | 复制一个敌人 Prefab 实例到出生点相邻格 |
| 快速测试某事件 | 复制对应 World Prefab 到出生点相邻格 |

注意：

- 这些都是场景 / 资产修改，会出现在 Git 变更中。
- 测试完成后不要依赖记忆手动恢复，提交前用版本控制检查差异。
- 在 Play Mode 中修改的值通常会在退出 Play 后恢复；在 Edit Mode 修改会保存。

### 10.3 推荐以后增加的策划调试面板

如果项目继续开发，建议新增一个只在 Editor / Development Build 生效的 `DesignerDebugSettings`：

- 开局跳过标题并直接进入 GameScene。
- 显示全部迷雾。
- 移动不扣 Point。
- 无敌 / Point 不会低于 0。
- 初始 Point Override。
- 普攻伤害 Override。
- 贪婪强制成功 / 失败。
- 固定随机 Seed。
- 一键给予全部技能。
- 一键给予指定道具 / 藏品。
- 一键传送到指定格。
- 一键开始指定敌人或 Boss 阶段。
- 显示每个 GridObject 的占位范围和格子坐标。

这些目前只是建议，不是已经存在的开关。

---

## 11. Unity Tools 菜单：哪些能用

### 11.1 日常可以使用

| 菜单 | 用途 |
|---|---|
| `Tools/Export Database To JSON` | 将本地化 Excel 导出为运行时 JSON |
| `Tools/Zero/Synchronize Audio Info List` | 按代码映射重建音频列表 |

执行前仍应保存文件并在执行后检查 Git Diff。

### 11.2 不要作为日常配置工具

以下是开发期生成 / 重建脚本，会修改多个资产、Prefab 或场景：

- `Tools/Zero/Build Battle Item Feature`
- `Tools/Zero/Build Offering Feature`
- `Tools/Zero/Combat/Generate Missing UI Prefabs`

特别注意：

- `Build Battle Item Feature` 内仍包含旧商品、旧堆叠和旧供奉写入逻辑。
- `Build Offering Feature` 会把供奉覆盖为旧版 10/30/20/20/20，并重新写入场景 / Prefab。
- UI Generator 可能升级或生成 UI Prefab。

除非程序明确要重建这些功能，并准备逐项审查差异，否则策划不要点击。

---

## 12. 提交前验收

### 12.1 关卡

- [ ] 从 `Persistent` 完整进入游戏。
- [ ] 玩家出生在有效 Ground。
- [ ] 没有 `Could not place`、空引用或重复占位错误。
- [ ] 新敌人可以从相邻格触发。
- [ ] 新事件可以打开正确面板。
- [ ] Boss 四格全部有效，周围存在接触位置。
- [ ] 关键路线可达，没有被 Collision 封死。
- [ ] 迷雾发现顺序符合预期。

### 12.2 数值与配置

- [ ] 普攻、技能、敌人、商店和供奉显示文本与实际数字一致。
- [ ] 普通怪 Roll 区间与稳定生命符合预期。
- [ ] 供奉权重合计 100。
- [ ] 商店商品引用、价格与库存正确。
- [ ] 宝藏 Reward Type 与奖励引用匹配。
- [ ] 道具 / 藏品没有超过合理堆叠上限。

### 12.3 文本与音频

- [ ] Excel 已导出 JSON。
- [ ] 英文、简体中文、日文都没有缺 Key 或溢出。
- [ ] `{0}`、`{1}` 格式化正常。
- [ ] 新 AudioName 同时具有文件映射和播放调用。
- [ ] BGM 循环、SFX 音量和 Settings 滑条正常。

### 12.4 版本控制

- [ ] 检查 `.unity`、`.prefab`、`.asset`、`.xlsx`、`.json` 的实际差异。
- [ ] 没有误改 `GameSceneOrigin`。
- [ ] 没有把临时测试数值提交。
- [ ] 没有意外运行旧 Builder 后产生的大量覆盖。
- [ ] 若规则发生改变，已同步更新 GDD。

---

## 13. 文档分工

| 文档 | 回答的问题 |
|---|---|
| `Docs/Zero_GDD.md` | 当前游戏机制与最终规则是什么？对应程序模块在哪里？ |
| `Docs/Designer_Config_and_Level_Guide.md` | 策划如何摆关卡、改参数、导文本、配音频和验证？ |
| `Docs/Localization_Key_Dictionary.md` | 某段文本使用哪个 Key、有哪些占位符、当前是否仍在使用？ |

未来如果内容继续增长，可以再拆出：

- 《关卡流程与摆放表》：逐格记录正式关卡坐标、事件、奖励和设计目的。
- 《数值变更记录》：记录版本、修改前后、原因和测试结果。
- 《发布与构建清单》：WebGL / Windows / macOS 的构建、上传和回归步骤。

当前阶段不建议再复制一份完整数值表；GDD、配置资产和本指南已经覆盖职责，重复维护反而容易再次漂移。
