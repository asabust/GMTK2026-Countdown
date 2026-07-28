# Feishu Game Data Sync

将飞书中的多 Sheet 工作簿完整导出为
`Assets/GameData/database.xlsx`。飞书是唯一在线数据源，本地 XLSX 是进入
Unity 导表流程的发布快照。

## 安全设计

- App Secret、access token 和 refresh token 仅保存在 macOS 钥匙串。
- 项目仓库和日志中不保存任何凭据。
- 非敏感配置保存在被 Unity `.gitignore` 排除的 `UserSettings`。
- 每次覆盖 `database.xlsx` 前自动备份到被忽略的 `Library/FeishuSync/backups`。
- 如果 `database.xlsx` 在上次拉取后又被本地修改，默认拒绝覆盖。
  单纯由上一次拉取产生的 Git 修改不会阻止下一次拉取。
- `push` 前会对比上次 `pull` 快照与飞书当前内容；远端变化时默认停止。
- `push` 会先完整导出一份远端 XLSX 备份，写入后再回读逐格校验。
- `push` 会读取 `Tools/FeishuSync/unused_keys.json`，把清单中的 Key
  同步成浅灰底、灰色文字；仅样式变化也会被识别并推送。

## 初次配置

```bash
Tools/feishu-sync configure \
  --document-url "https://example.feishu.cn/wiki/..."
Tools/feishu-sync login
Tools/feishu-sync pull
```

`configure` 会隐藏输入 App Secret。不要把 App Secret 写进命令、聊天或仓库。

## 日常使用

```bash
Tools/feishu-sync status
Tools/feishu-sync pull
Tools/feishu-sync push --check
Tools/feishu-sync push
```

拉取完成后，在 Unity 执行：

```text
Tools > Export Database To JSON
```

本地编辑 `Assets/GameData/database.xlsx` 后，可运行 `push` 将配置中的
`Localization` 工作表写回飞书。默认只推送这一张表，不会改动其它工作表。
`push --check` 只比较本地、飞书和上次同步快照，不会写入。
若远端确实被其他人修改，工具会停止；只有确认以本地为准时才使用：

```bash
Tools/feishu-sync push --force
```

## 飞书应用要求

- App ID：`cli_aae23350ab381cfd`
- 重定向 URL：`http://127.0.0.1:53682/callback`
- 用户身份权限：
  - `wiki:node:read`
  - `drive:export:readonly`
  - `sheets:spreadsheet`
  - `offline_access`

当前版本支持完整 XLSX 拉取，以及带远端备份、冲突检测和回读校验的
`Localization` 工作表写回。重新审计未使用 Key 后，同时更新
`Tools/FeishuSync/unused_keys.json` 即可让本地 XLSX 和飞书保持一致。
