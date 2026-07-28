#!/usr/bin/env python3
"""Synchronize the project's game-data workbook with Feishu.

Secrets and OAuth tokens are stored in the macOS Keychain. Non-secret project
configuration and pull snapshots live in ignored Unity folders.
"""

from __future__ import annotations

import argparse
import getpass
import hashlib
import http.server
import json
import os
import posixpath
import re
import secrets
import shutil
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.parse
import urllib.request
import webbrowser
import zipfile
import xml.etree.ElementTree as ET
from datetime import datetime
from pathlib import Path
from typing import Any


APP_ID = "cli_aae23350ab381cfd"
REDIRECT_URI = "http://127.0.0.1:53682/callback"
SCOPES = (
    "wiki:node:read",
    "drive:export:readonly",
    "sheets:spreadsheet",
    "offline_access",
)
API_BASE = "https://open.feishu.cn/open-apis"
AUTHORIZE_URL = "https://accounts.feishu.cn/open-apis/authen/v1/authorize"
PROJECT_ROOT = Path(__file__).resolve().parents[2]
CONFIG_PATH = PROJECT_ROOT / "UserSettings" / "FeishuSync" / "config.json"
STATE_DIR = PROJECT_ROOT / "Library" / "FeishuSync"
SNAPSHOT_PATH = STATE_DIR / "last-pulled.xlsx"
STATE_PATH = STATE_DIR / "state.json"
DEFAULT_DESTINATION = PROJECT_ROOT / "Assets" / "GameData" / "database.xlsx"
UNUSED_KEYS_PATH = PROJECT_ROOT / "Tools" / "FeishuSync" / "unused_keys.json"
KEYCHAIN_SERVICE = f"GameDataSync.Feishu.{APP_ID}"


class SyncError(RuntimeError):
    pass


def keychain_get(account: str) -> str | None:
    result = subprocess.run(
        [
            "security",
            "find-generic-password",
            "-s",
            KEYCHAIN_SERVICE,
            "-a",
            account,
            "-w",
        ],
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        return None
    return result.stdout.rstrip("\n")


def keychain_set(account: str, value: str) -> None:
    result = subprocess.run(
        [
            "security",
            "add-generic-password",
            "-U",
            "-s",
            KEYCHAIN_SERVICE,
            "-a",
            account,
            "-w",
            value,
        ],
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        raise SyncError(f"无法写入 macOS 钥匙串：{result.stderr.strip()}")


def load_config() -> dict[str, Any]:
    if not CONFIG_PATH.exists():
        raise SyncError("尚未配置。请先运行：Tools/feishu-sync configure")
    return json.loads(CONFIG_PATH.read_text(encoding="utf-8"))


def save_config(config: dict[str, Any]) -> None:
    CONFIG_PATH.parent.mkdir(parents=True, exist_ok=True)
    CONFIG_PATH.write_text(
        json.dumps(config, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


def api_request(
    method: str,
    path: str,
    *,
    access_token: str | None = None,
    body: dict[str, Any] | None = None,
    query: dict[str, str] | None = None,
    expect_binary: bool = False,
) -> Any:
    url = f"{API_BASE}{path}"
    if query:
        url += "?" + urllib.parse.urlencode(query)
    headers = {"Content-Type": "application/json; charset=utf-8"}
    if access_token:
        headers["Authorization"] = f"Bearer {access_token}"
    data = None if body is None else json.dumps(body).encode("utf-8")
    request = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            payload = response.read()
    except urllib.error.HTTPError as error:
        detail = error.read().decode("utf-8", errors="replace")
        raise SyncError(f"飞书 API 请求失败（HTTP {error.code}）：{detail}") from error
    except urllib.error.URLError as error:
        raise SyncError(f"无法连接飞书 API：{error.reason}") from error

    if expect_binary:
        return payload
    result = json.loads(payload.decode("utf-8"))
    if result.get("code", 0) != 0:
        raise SyncError(
            f"飞书 API 返回错误 {result.get('code')}：{result.get('msg', '未知错误')}"
        )
    return result


def get_app_secret() -> str:
    value = keychain_get("app-secret")
    if not value:
        raise SyncError("钥匙串中没有 App Secret。请重新运行 configure。")
    return value


def load_tokens() -> dict[str, Any] | None:
    raw = keychain_get("oauth-tokens")
    return json.loads(raw) if raw else None


def save_tokens(payload: dict[str, Any]) -> dict[str, Any]:
    tokens = {
        "access_token": payload["access_token"],
        "refresh_token": payload.get("refresh_token"),
        "expires_at": int(time.time()) + int(payload.get("expires_in", 0)) - 60,
        "refresh_token_expires_at": int(time.time())
        + int(payload.get("refresh_token_expires_in", 0))
        - 60,
        "scope": payload.get("scope", ""),
    }
    keychain_set("oauth-tokens", json.dumps(tokens, separators=(",", ":")))
    return tokens


def exchange_token(body: dict[str, Any]) -> dict[str, Any]:
    body.update({"client_id": APP_ID, "client_secret": get_app_secret()})
    return api_request("POST", "/authen/v2/oauth/token", body=body)


def get_access_token() -> str:
    tokens = load_tokens()
    if not tokens:
        raise SyncError("尚未登录。请先运行：Tools/feishu-sync login")
    if int(tokens.get("expires_at", 0)) > int(time.time()):
        return str(tokens["access_token"])

    refresh_token = tokens.get("refresh_token")
    if not refresh_token:
        raise SyncError("登录已过期且没有刷新凭据，请重新运行 login。")
    payload = exchange_token(
        {"grant_type": "refresh_token", "refresh_token": refresh_token}
    )
    return str(save_tokens(payload)["access_token"])


class OAuthCallbackHandler(http.server.BaseHTTPRequestHandler):
    result: dict[str, str] = {}
    expected_state = ""

    def do_GET(self) -> None:  # noqa: N802
        parsed = urllib.parse.urlparse(self.path)
        params = urllib.parse.parse_qs(parsed.query)
        state = params.get("state", [""])[0]
        if parsed.path != "/callback" or state != self.expected_state:
            self.send_response(400)
            self.end_headers()
            self.wfile.write("Invalid OAuth callback.".encode())
            return
        self.result = {
            "code": params.get("code", [""])[0],
            "error": params.get("error", [""])[0],
        }
        self.__class__.result = self.result
        self.send_response(200)
        self.send_header("Content-Type", "text/html; charset=utf-8")
        self.end_headers()
        self.wfile.write(
            "<meta charset='utf-8'><h2>飞书授权完成</h2>"
            "<p>可以关闭此页面并返回终端。</p>".encode("utf-8")
        )

    def log_message(self, _format: str, *_args: Any) -> None:
        return


def command_configure(args: argparse.Namespace) -> None:
    document_url = args.document_url or input("飞书工作簿链接：").strip()
    parsed = urllib.parse.urlparse(document_url)
    if "/wiki/" not in parsed.path and "/sheets/" not in parsed.path:
        raise SyncError("链接必须是飞书 /wiki/ 或 /sheets/ 工作簿地址。")

    secret = getpass.getpass("App Secret（输入不会显示）：").strip()
    if not secret:
        raise SyncError("App Secret 不能为空。")
    keychain_set("app-secret", secret)
    save_config(
        {
            "app_id": APP_ID,
            "document_url": document_url,
            "destination": str(DEFAULT_DESTINATION.relative_to(PROJECT_ROOT)),
            "localization_sheet": "Localization",
            "redirect_uri": REDIRECT_URI,
        }
    )
    print(f"配置已保存：{CONFIG_PATH}")
    print("App Secret 已保存到 macOS 钥匙串，未写入项目。")


def command_login(_args: argparse.Namespace) -> None:
    load_config()
    get_app_secret()
    state = secrets.token_urlsafe(24)
    OAuthCallbackHandler.expected_state = state
    OAuthCallbackHandler.result = {}
    params = {
        "client_id": APP_ID,
        "redirect_uri": REDIRECT_URI,
        "scope": " ".join(SCOPES),
        "state": state,
    }
    url = AUTHORIZE_URL + "?" + urllib.parse.urlencode(params)
    server = http.server.HTTPServer(("127.0.0.1", 53682), OAuthCallbackHandler)
    print("正在打开飞书授权页面……")
    if not webbrowser.open(url):
        print(f"请在浏览器打开：{url}")
    server.timeout = 180
    server.handle_request()
    result = OAuthCallbackHandler.result
    if not result:
        raise SyncError("等待飞书授权超时，请重新运行 login。")
    if result.get("error"):
        raise SyncError(f"飞书授权失败：{result['error']}")
    if not result.get("code"):
        raise SyncError("飞书没有返回授权码。")

    payload = exchange_token(
        {
            "grant_type": "authorization_code",
            "code": result["code"],
            "redirect_uri": REDIRECT_URI,
        }
    )
    tokens = save_tokens(payload)
    print("飞书登录成功。")
    print(f"已授权范围：{tokens.get('scope') or '由飞书返回的默认范围'}")


def parse_document_token(document_url: str) -> tuple[str, str]:
    parsed = urllib.parse.urlparse(document_url)
    parts = [part for part in parsed.path.split("/") if part]
    if len(parts) < 2:
        raise SyncError("无法从工作簿链接解析 token。")
    if parts[0] == "wiki":
        return "wiki", parts[1]
    if parts[0] == "sheets":
        return "sheet", parts[1]
    raise SyncError("工作簿链接必须包含 /wiki/ 或 /sheets/。")


def resolve_spreadsheet_token(access_token: str, document_url: str) -> str:
    kind, token = parse_document_token(document_url)
    if kind == "sheet":
        return token
    result = api_request(
        "GET",
        "/wiki/v2/spaces/get_node",
        access_token=access_token,
        query={"token": token},
    )
    node = result.get("data", {}).get("node", {})
    if node.get("obj_type") != "sheet":
        raise SyncError(
            f"知识库节点不是电子表格，实际类型：{node.get('obj_type', '未知')}"
        )
    obj_token = node.get("obj_token")
    if not obj_token:
        raise SyncError("飞书未返回电子表格 obj_token。")
    return str(obj_token)


def create_export(access_token: str, spreadsheet_token: str) -> str:
    result = api_request(
        "POST",
        "/drive/v1/export_tasks",
        access_token=access_token,
        body={
            "file_extension": "xlsx",
            "token": spreadsheet_token,
            "type": "sheet",
        },
    )
    ticket = result.get("data", {}).get("ticket")
    if not ticket:
        raise SyncError("飞书未返回导出任务 ticket。")
    return str(ticket)


def await_export(
    access_token: str, spreadsheet_token: str, ticket: str
) -> tuple[str, str]:
    deadline = time.time() + 120
    while time.time() < deadline:
        result = api_request(
            "GET",
            f"/drive/v1/export_tasks/{urllib.parse.quote(ticket)}",
            access_token=access_token,
            query={"token": spreadsheet_token},
        )
        export_result = result.get("data", {}).get("result", {})
        file_token = export_result.get("file_token")
        if file_token:
            return str(file_token), str(
                export_result.get("file_name") or "database.xlsx"
            )
        job_status = export_result.get("job_status")
        if job_status not in (None, 0, 1, 2):
            raise SyncError(
                f"飞书导出任务失败，状态 {job_status}："
                f"{export_result.get('job_error_msg', '未知错误')}"
            )
        time.sleep(1)
    raise SyncError("等待飞书导出超时。")


def download_export(access_token: str, file_token: str) -> bytes:
    return api_request(
        "GET",
        f"/drive/v1/export_tasks/file/{urllib.parse.quote(file_token)}/download",
        access_token=access_token,
        expect_binary=True,
    )


def validate_xlsx(data: bytes) -> None:
    with tempfile.NamedTemporaryFile(suffix=".xlsx") as temp:
        temp.write(data)
        temp.flush()
        if not zipfile.is_zipfile(temp.name):
            raise SyncError("飞书返回的文件不是有效的 XLSX。")
        with zipfile.ZipFile(temp.name) as workbook:
            names = set(workbook.namelist())
            if "xl/workbook.xml" not in names:
                raise SyncError("XLSX 中缺少 xl/workbook.xml。")


def git_path_is_dirty(path: Path) -> bool:
    relative = str(path.relative_to(PROJECT_ROOT))
    unstaged = subprocess.run(
        ["git", "diff", "--quiet", "--", relative], cwd=PROJECT_ROOT
    ).returncode
    staged = subprocess.run(
        ["git", "diff", "--cached", "--quiet", "--", relative], cwd=PROJECT_ROOT
    ).returncode
    return unstaged != 0 or staged != 0


def file_sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for chunk in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def load_unused_key_style() -> tuple[set[str], str, str]:
    if not UNUSED_KEYS_PATH.exists():
        return set(), "#F2F2F2", "#808080"
    try:
        payload = json.loads(UNUSED_KEYS_PATH.read_text(encoding="utf-8"))
    except json.JSONDecodeError as error:
        raise SyncError(f"未使用 Key 清单不是有效 JSON：{error}") from error
    keys = payload.get("keys")
    style = payload.get("style", {})
    if not isinstance(keys, list) or any(
        not isinstance(key, str) or not key.strip() for key in keys
    ):
        raise SyncError("未使用 Key 清单的 keys 必须是非空字符串数组。")
    normalized_keys = {key.strip() for key in keys}
    if len(normalized_keys) != len(keys):
        raise SyncError("未使用 Key 清单中存在重复项。")
    fill = str(style.get("fill", "#F2F2F2"))
    font_color = str(style.get("font_color", "#808080"))
    if not re.fullmatch(r"#[0-9A-Fa-f]{6}", fill) or not re.fullmatch(
        r"#[0-9A-Fa-f]{6}", font_color
    ):
        raise SyncError("未使用 Key 样式必须使用 #RRGGBB 颜色。")
    return normalized_keys, fill.upper(), font_color.upper()


def contiguous_ranges(row_numbers: list[int]) -> list[tuple[int, int]]:
    if not row_numbers:
        return []
    ranges: list[tuple[int, int]] = []
    start = previous = row_numbers[0]
    for row_number in row_numbers[1:]:
        if row_number == previous + 1:
            previous = row_number
            continue
        ranges.append((start, previous))
        start = previous = row_number
    ranges.append((start, previous))
    return ranges


def normalize_matrix(values: list[list[Any]]) -> list[list[Any]]:
    normalized: list[list[Any]] = []
    for source_row in values:
        row = ["" if value is None else value for value in source_row]
        while row and row[-1] == "":
            row.pop()
        normalized.append(row)
    while normalized and not normalized[-1]:
        normalized.pop()
    width = max((len(row) for row in normalized), default=0)
    return [row + [""] * (width - len(row)) for row in normalized]


def matrix_sha256(values: list[list[Any]]) -> str:
    payload = json.dumps(
        normalize_matrix(values),
        ensure_ascii=False,
        separators=(",", ":"),
    ).encode("utf-8")
    return hashlib.sha256(payload).hexdigest()


def matrix_differences(
    expected: list[list[Any]],
    actual: list[list[Any]],
    *,
    limit: int = 10,
) -> list[dict[str, Any]]:
    expected = normalize_matrix(expected)
    actual = normalize_matrix(actual)
    row_count = max(len(expected), len(actual))
    column_count = max(
        max((len(row) for row in expected), default=0),
        max((len(row) for row in actual), default=0),
    )
    differences: list[dict[str, Any]] = []
    for row_index in range(row_count):
        for column_index in range(column_count):
            expected_value = (
                expected[row_index][column_index]
                if row_index < len(expected)
                and column_index < len(expected[row_index])
                else ""
            )
            actual_value = (
                actual[row_index][column_index]
                if row_index < len(actual)
                and column_index < len(actual[row_index])
                else ""
            )
            if expected_value != actual_value:
                differences.append(
                    {
                        "cell": (
                            f"{excel_column_name(column_index + 1)}"
                            f"{row_index + 1}"
                        ),
                        "expected": expected_value,
                        "actual": actual_value,
                    }
                )
                if len(differences) >= limit:
                    return differences
    return differences


def xlsx_sheet_values(path: Path, sheet_name: str) -> list[list[Any]]:
    main_ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    office_rel_ns = (
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
    )
    package_rel_ns = (
        "http://schemas.openxmlformats.org/package/2006/relationships"
    )

    with zipfile.ZipFile(path) as workbook:
        root = ET.fromstring(workbook.read("xl/workbook.xml"))
        relationship_id = None
        for sheet in root.findall(f".//{{{main_ns}}}sheet"):
            if sheet.get("name") == sheet_name:
                relationship_id = sheet.get(f"{{{office_rel_ns}}}id")
                break
        if not relationship_id:
            raise SyncError(f"XLSX 中找不到工作表：{sheet_name}")

        rel_root = ET.fromstring(workbook.read("xl/_rels/workbook.xml.rels"))
        target = None
        for relationship in rel_root.findall(f"{{{package_rel_ns}}}Relationship"):
            if relationship.get("Id") == relationship_id:
                target = relationship.get("Target")
                break
        if not target:
            raise SyncError(f"XLSX 中无法解析工作表文件：{sheet_name}")
        sheet_path = (
            posixpath.normpath(target.lstrip("/"))
            if target.startswith("/")
            else posixpath.normpath(posixpath.join("xl", target))
        )

        shared_strings: list[str] = []
        if "xl/sharedStrings.xml" in workbook.namelist():
            shared_root = ET.fromstring(workbook.read("xl/sharedStrings.xml"))
            for item in shared_root.findall(f"{{{main_ns}}}si"):
                shared_strings.append(
                    "".join(
                        node.text or ""
                        for node in item.iter(f"{{{main_ns}}}t")
                    )
                )

        sheet_root = ET.fromstring(workbook.read(sheet_path))
        cells: dict[tuple[int, int], Any] = {}
        max_row = 0
        max_column = 0
        for cell in sheet_root.findall(f".//{{{main_ns}}}c"):
            reference = cell.get("r", "")
            match = re.fullmatch(r"([A-Z]+)([0-9]+)", reference)
            if not match:
                continue
            column = 0
            for character in match.group(1):
                column = column * 26 + ord(character) - ord("A") + 1
            row = int(match.group(2))
            cell_type = cell.get("t")
            value_node = cell.find(f"{{{main_ns}}}v")
            if cell_type == "inlineStr":
                value: Any = "".join(
                    node.text or ""
                    for node in cell.iter(f"{{{main_ns}}}t")
                )
            elif value_node is None:
                value = ""
            elif cell_type == "s":
                value = shared_strings[int(value_node.text or "0")]
            elif cell_type == "b":
                value = value_node.text == "1"
            elif cell_type in ("str", "e"):
                value = value_node.text or ""
            else:
                raw = value_node.text or ""
                try:
                    number = float(raw)
                    value = int(number) if number.is_integer() else number
                except ValueError:
                    value = raw
            if value != "":
                cells[(row, column)] = value
                max_row = max(max_row, row)
                max_column = max(max_column, column)

    values = [[""] * max_column for _ in range(max_row)]
    for (row, column), value in cells.items():
        values[row - 1][column - 1] = value
    return normalize_matrix(values)


def destination_changed_since_last_pull(destination: Path) -> bool:
    if not STATE_PATH.exists():
        return True
    try:
        state = json.loads(STATE_PATH.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, OSError):
        return True
    last_pulled_sha = state.get("sha256")
    return not last_pulled_sha or file_sha256(destination) != last_pulled_sha


def get_sheet(
    access_token: str, spreadsheet_token: str, sheet_name: str
) -> dict[str, Any]:
    result = api_request(
        "GET",
        f"/sheets/v3/spreadsheets/{urllib.parse.quote(spreadsheet_token)}/sheets/query",
        access_token=access_token,
    )
    for sheet in result.get("data", {}).get("sheets", []):
        if sheet.get("title") == sheet_name:
            return sheet
    available = ", ".join(
        str(sheet.get("title"))
        for sheet in result.get("data", {}).get("sheets", [])
    )
    raise SyncError(
        f"飞书中找不到工作表 {sheet_name}。现有工作表：{available or '无'}"
    )


def read_remote_values(
    access_token: str,
    spreadsheet_token: str,
    sheet_id: str,
    last_column: str,
) -> list[list[Any]]:
    cell_range = f"{sheet_id}!A:{last_column}"
    result = api_request(
        "GET",
        "/sheets/v2/spreadsheets/"
        f"{urllib.parse.quote(spreadsheet_token)}/values/"
        f"{urllib.parse.quote(cell_range, safe='')}",
        access_token=access_token,
    )
    values = (
        result.get("data", {})
        .get("valueRange", {})
        .get("values", [])
    )
    return normalize_matrix(values)


def write_remote_values(
    access_token: str,
    spreadsheet_token: str,
    sheet_id: str,
    values: list[list[Any]],
) -> dict[str, Any]:
    row_count = len(values)
    column_count = max((len(row) for row in values), default=0)
    if not row_count or not column_count:
        raise SyncError("本地工作表没有可推送的数据。")
    if row_count > 5000 or column_count > 100:
        raise SyncError("单次 push 目前最多支持 5000 行、100 列。")
    padded = [row + [""] * (column_count - len(row)) for row in values]
    last_column = excel_column_name(column_count)
    cell_range = f"{sheet_id}!A1:{last_column}{row_count}"
    return api_request(
        "PUT",
        f"/sheets/v2/spreadsheets/{urllib.parse.quote(spreadsheet_token)}/values",
        access_token=access_token,
        body={
            "valueRange": {
                "range": cell_range,
                "values": padded,
            }
        },
    )


def excel_column_name(column_count: int) -> str:
    result = ""
    value = column_count
    while value:
        value, remainder = divmod(value - 1, 26)
        result = chr(ord("A") + remainder) + result
    return result


def set_remote_style(
    access_token: str,
    spreadsheet_token: str,
    cell_range: str,
    style: dict[str, Any],
) -> None:
    api_request(
        "PUT",
        f"/sheets/v2/spreadsheets/{urllib.parse.quote(spreadsheet_token)}/style",
        access_token=access_token,
        body={
            "appendStyle": {
                "range": cell_range,
                "style": style,
            }
        },
    )


def unmerge_remote_cells(
    access_token: str,
    spreadsheet_token: str,
    sheet_id: str,
    merge: dict[str, Any],
) -> None:
    start_column = int(merge["start_column_index"]) + 1
    end_column = int(merge["end_column_index"]) + 1
    start_row = int(merge["start_row_index"]) + 1
    end_row = int(merge["end_row_index"]) + 1
    cell_range = (
        f"{sheet_id}!{excel_column_name(start_column)}{start_row}:"
        f"{excel_column_name(end_column)}{end_row}"
    )
    api_request(
        "POST",
        f"/sheets/v2/spreadsheets/"
        f"{urllib.parse.quote(spreadsheet_token)}/unmerge_cells",
        access_token=access_token,
        body={"range": cell_range},
    )


def create_remote_backup(
    access_token: str, spreadsheet_token: str
) -> Path:
    print("正在备份推送前的飞书工作簿……")
    ticket = create_export(access_token, spreadsheet_token)
    file_token, _remote_name = await_export(
        access_token, spreadsheet_token, ticket
    )
    data = download_export(access_token, file_token)
    validate_xlsx(data)
    backup_dir = STATE_DIR / "backups"
    backup_dir.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    backup_path = backup_dir / f"remote-before-push-{stamp}.xlsx"
    backup_path.write_bytes(data)
    return backup_path


def command_pull(args: argparse.Namespace) -> None:
    config = load_config()
    destination = PROJECT_ROOT / config["destination"]
    local_changes = (
        destination.exists()
        and git_path_is_dirty(destination)
        and destination_changed_since_last_pull(destination)
    )
    if local_changes and not args.force:
        raise SyncError(
            f"{destination.relative_to(PROJECT_ROOT)} 在上次拉取后又被本地修改。"
            "为避免覆盖，请先提交或使用 pull --force（仍会自动备份）。"
        )

    access_token = get_access_token()
    spreadsheet_token = resolve_spreadsheet_token(
        access_token, config["document_url"]
    )
    print("已解析知识库节点，正在创建 XLSX 导出任务……")
    ticket = create_export(access_token, spreadsheet_token)
    file_token, remote_name = await_export(access_token, spreadsheet_token, ticket)
    data = download_export(access_token, file_token)
    validate_xlsx(data)

    STATE_DIR.mkdir(parents=True, exist_ok=True)
    backup_path = None
    if destination.exists():
        backup_dir = STATE_DIR / "backups"
        backup_dir.mkdir(parents=True, exist_ok=True)
        stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
        backup_path = backup_dir / f"database-{stamp}.xlsx"
        shutil.copy2(destination, backup_path)

    destination.parent.mkdir(parents=True, exist_ok=True)
    temp_path = destination.with_suffix(".xlsx.tmp")
    temp_path.write_bytes(data)
    os.replace(temp_path, destination)
    SNAPSHOT_PATH.write_bytes(data)
    digest = hashlib.sha256(data).hexdigest()
    STATE_PATH.write_text(
        json.dumps(
            {
                "pulled_at": datetime.now().astimezone().isoformat(),
                "spreadsheet_token": spreadsheet_token,
                "remote_file_name": remote_name,
                "sha256": digest,
                "content_sha256": matrix_sha256(
                    xlsx_sheet_values(
                        destination,
                        config.get("localization_sheet", "Localization"),
                    )
                ),
                "destination": str(destination.relative_to(PROJECT_ROOT)),
            },
            ensure_ascii=False,
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )
    print(f"已更新：{destination.relative_to(PROJECT_ROOT)}")
    print(f"SHA-256：{digest}")
    if backup_path:
        print(f"旧文件备份：{backup_path.relative_to(PROJECT_ROOT)}")
    print("Unity 检测到文件变化后，可执行 Tools > Export Database To JSON。")


def command_push(args: argparse.Namespace) -> None:
    config = load_config()
    destination = PROJECT_ROOT / config["destination"]
    sheet_name = config.get("localization_sheet", "Localization")
    if not destination.exists():
        raise SyncError(
            f"本地工作簿不存在：{destination.relative_to(PROJECT_ROOT)}"
        )
    if not SNAPSHOT_PATH.exists() and not args.force:
        raise SyncError(
            "缺少上次 pull 的快照，无法检查远端冲突。"
            "请先运行 pull，或确认风险后使用 push --force。"
        )

    local_values = xlsx_sheet_values(destination, sheet_name)
    if not local_values:
        raise SyncError(f"本地工作表 {sheet_name} 没有数据。")
    baseline_values = (
        xlsx_sheet_values(SNAPSHOT_PATH, sheet_name)
        if SNAPSHOT_PATH.exists()
        else []
    )

    access_token = get_access_token()
    spreadsheet_token = resolve_spreadsheet_token(
        access_token, config["document_url"]
    )
    sheet = get_sheet(access_token, spreadsheet_token, sheet_name)
    sheet_id = str(sheet.get("sheet_id") or "")
    if not sheet_id:
        raise SyncError(f"飞书未返回工作表 {sheet_name} 的 sheet_id。")

    max_columns = max(
        max((len(row) for row in local_values), default=0),
        max((len(row) for row in baseline_values), default=0),
    )
    remote_values = read_remote_values(
        access_token,
        spreadsheet_token,
        sheet_id,
        excel_column_name(max_columns),
    )

    remote_changed = (
        bool(baseline_values)
        and normalize_matrix(remote_values) != normalize_matrix(baseline_values)
    )
    local_values_changed = (
        not baseline_values
        or normalize_matrix(local_values) != normalize_matrix(baseline_values)
    )
    local_file_changed = (
        not SNAPSHOT_PATH.exists()
        or file_sha256(destination) != file_sha256(SNAPSHOT_PATH)
    )
    local_changed = local_values_changed or local_file_changed
    if args.check:
        remote_matches_local = (
            normalize_matrix(remote_values) == normalize_matrix(local_values)
        )
        print(f"本地行数：{len(local_values)}")
        print(f"远端行数：{len(remote_values)}")
        print(
            "本地相对上次同步："
            + ("有变化" if local_changed else "无变化")
            + f"（内容：{'有' if local_values_changed else '无'}；"
            + f"文件/样式：{'有' if local_file_changed else '无'}）"
        )
        print(f"远端相对上次 pull：{'有变化' if remote_changed else '无变化'}")
        print(f"远端与本地：{'一致' if remote_matches_local else '不一致'}")
        merges = sheet.get("merges", [])
        if merges:
            print(
                "远端合并区域："
                + json.dumps(merges[:20], ensure_ascii=False)
            )
        if not remote_matches_local:
            print(
                "前几处差异："
                + json.dumps(
                    matrix_differences(local_values, remote_values),
                    ensure_ascii=False,
                )
            )
        return
    if remote_changed and not args.force:
        raise SyncError(
            "飞书工作表在上次 pull 后发生了变化。为避免覆盖，push 已停止。"
            "如确认以本地为准，可使用 push --force。"
        )
    if not local_changed and not args.force:
        print("本地工作表与上次 pull 相同，无需 push。")
        return

    backup_path = create_remote_backup(access_token, spreadsheet_token)
    print(f"远端备份：{backup_path.relative_to(PROJECT_ROOT)}")

    write_rows = max(len(local_values), len(remote_values))
    write_columns = max(
        max((len(row) for row in local_values), default=0),
        max((len(row) for row in remote_values), default=0),
    )
    write_values = [
        (
            local_values[index]
            if index < len(local_values)
            else []
        )
        + [""] * (
            write_columns
            - len(local_values[index] if index < len(local_values) else [])
        )
        for index in range(write_rows)
    ]

    print(
        f"正在将本地 {sheet_name} 写入飞书"
        f"（{len(local_values)} 行 × {write_columns} 列）……"
    )
    intersecting_merges = [
        merge
        for merge in sheet.get("merges", [])
        if int(merge.get("start_column_index", write_columns)) < write_columns
        and int(merge.get("start_row_index", write_rows)) < write_rows
    ]
    for merge in intersecting_merges:
        unmerge_remote_cells(
            access_token,
            spreadsheet_token,
            sheet_id,
            merge,
        )
    if intersecting_merges:
        print(f"已拆分 {len(intersecting_merges)} 个旧合并区域。")
    write_remote_values(
        access_token,
        spreadsheet_token,
        sheet_id,
        write_values,
    )

    if write_rows >= 4:
        set_remote_style(
            access_token,
            spreadsheet_token,
            f"{sheet_id}!A4:{excel_column_name(write_columns)}{write_rows}",
            {
                "font": {"bold": False, "clean": False},
                "foreColor": "#000000",
                "backColor": "#FFFFFF",
                "clean": False,
            },
        )
    module_header_rows = [
        index + 1
        for index, row in enumerate(local_values)
        if index >= 3
        and (not row or not str(row[0]).strip())
        and len(row) > 1
        and str(row[1]).strip()
    ]
    for row_number in module_header_rows:
        set_remote_style(
            access_token,
            spreadsheet_token,
            f"{sheet_id}!A{row_number}:"
            f"{excel_column_name(write_columns)}{row_number}",
            {
                "font": {"bold": True, "clean": False},
                "foreColor": "#1F2937",
                "backColor": "#DDEBF7",
                "clean": False,
            },
        )

    unused_keys, unused_fill, unused_font_color = load_unused_key_style()
    keyed_rows = {
        str(row[0]).strip(): index + 1
        for index, row in enumerate(local_values)
        if row and str(row[0]).strip()
    }
    missing_unused_keys = sorted(unused_keys - keyed_rows.keys())
    if missing_unused_keys:
        raise SyncError(
            "未使用 Key 清单与本地工作表不一致，以下 Key 不存在："
            + ", ".join(missing_unused_keys)
        )
    unused_row_numbers = sorted(keyed_rows[key] for key in unused_keys)
    for first_row, last_row in contiguous_ranges(unused_row_numbers):
        set_remote_style(
            access_token,
            spreadsheet_token,
            f"{sheet_id}!A{first_row}:"
            f"{excel_column_name(write_columns)}{last_row}",
            {
                "font": {"bold": False, "clean": False},
                "foreColor": unused_font_color,
                "backColor": unused_fill,
                "clean": False,
            },
        )

    verified_values = read_remote_values(
        access_token,
        spreadsheet_token,
        sheet_id,
        excel_column_name(write_columns),
    )
    if normalize_matrix(verified_values) != normalize_matrix(local_values):
        differences = matrix_differences(local_values, verified_values)
        raise SyncError(
            "飞书写入后的回读校验不一致。"
            f"前几处差异：{json.dumps(differences, ensure_ascii=False)}。"
            f"推送前备份位于：{backup_path.relative_to(PROJECT_ROOT)}"
        )

    STATE_DIR.mkdir(parents=True, exist_ok=True)
    shutil.copy2(destination, SNAPSHOT_PATH)
    state: dict[str, Any] = {}
    if STATE_PATH.exists():
        try:
            state = json.loads(STATE_PATH.read_text(encoding="utf-8"))
        except json.JSONDecodeError:
            state = {}
    state.update(
        {
            "pushed_at": datetime.now().astimezone().isoformat(),
            "spreadsheet_token": spreadsheet_token,
            "sha256": file_sha256(destination),
            "content_sha256": matrix_sha256(local_values),
            "unused_keys_sha256": (
                file_sha256(UNUSED_KEYS_PATH)
                if UNUSED_KEYS_PATH.exists()
                else None
            ),
            "destination": str(destination.relative_to(PROJECT_ROOT)),
            "localization_sheet": sheet_name,
        }
    )
    STATE_PATH.write_text(
        json.dumps(state, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(
        f"push 完成并已回读校验：{len(local_values)} 行，"
        f"{len(module_header_rows)} 个模块标题，"
        f"{len(unused_row_numbers)} 个未使用 Key 标灰，"
        f"拆分 {len(intersecting_merges)} 个旧合并区域。"
    )


def command_status(_args: argparse.Namespace) -> None:
    print(f"项目：{PROJECT_ROOT}")
    print(f"App ID：{APP_ID}")
    print(f"配置：{'已完成' if CONFIG_PATH.exists() else '未完成'}")
    print(f"App Secret：{'钥匙串中已保存' if keychain_get('app-secret') else '未保存'}")
    tokens = load_tokens()
    if not tokens:
        print("飞书登录：未登录")
    else:
        expired = int(tokens.get("expires_at", 0)) <= int(time.time())
        refresh_available = bool(tokens.get("refresh_token"))
        print(
            "飞书登录："
            + ("访问令牌待刷新" if expired else "有效")
            + ("（可自动刷新）" if refresh_available else "")
        )
    if STATE_PATH.exists():
        state = json.loads(STATE_PATH.read_text(encoding="utf-8"))
        print(f"上次拉取：{state.get('pulled_at')}")
        if state.get("pushed_at"):
            print(f"上次推送：{state.get('pushed_at')}")
        print(f"工作簿 SHA-256：{state.get('sha256')}")
    else:
        print("上次拉取：无")


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="飞书游戏数据库同步工具（凭据存储于 macOS 钥匙串）"
    )
    subparsers = parser.add_subparsers(dest="command", required=True)

    configure = subparsers.add_parser("configure", help="保存工作簿配置和 App Secret")
    configure.add_argument("--document-url", help="飞书 /wiki/ 或 /sheets/ 链接")
    configure.set_defaults(func=command_configure)

    login = subparsers.add_parser("login", help="通过浏览器授权飞书账号")
    login.set_defaults(func=command_login)

    pull = subparsers.add_parser("pull", help="下载完整 XLSX 到 Assets/GameData")
    pull.add_argument(
        "--force",
        action="store_true",
        help="即使本地 database.xlsx 已修改也继续（仍会备份）",
    )
    pull.set_defaults(func=command_pull)

    push = subparsers.add_parser(
        "push",
        help="将本地 Localization 工作表安全写回飞书",
    )
    push.add_argument(
        "--force",
        action="store_true",
        help="即使远端在上次 pull 后变化也以本地覆盖（仍会备份）",
    )
    push.add_argument(
        "--check",
        action="store_true",
        help="只检查本地、远端与上次 pull 快照，不写入",
    )
    push.set_defaults(func=command_push)

    status = subparsers.add_parser("status", help="查看配置、登录和同步状态")
    status.set_defaults(func=command_status)
    return parser


def main() -> int:
    try:
        args = build_parser().parse_args()
        args.func(args)
        return 0
    except (SyncError, json.JSONDecodeError, OSError, KeyError) as error:
        print(f"错误：{error}", file=sys.stderr)
        return 1
    except KeyboardInterrupt:
        print("\n已取消。", file=sys.stderr)
        return 130


if __name__ == "__main__":
    raise SystemExit(main())
