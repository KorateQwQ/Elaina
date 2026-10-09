"""Loopback-only bridge; the HTTP API cannot choose a destination file."""
from __future__ import annotations
import argparse
import json
import math
import os
from pathlib import Path
import re
import tempfile
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlsplit

ROOT = Path(__file__).resolve().parents[2]
CONFIG_PATH = ROOT / "ElainaModSkills" / "Data" / "SkillBalance.json"
HTML_PATH = Path(__file__).with_name("index.html")
SCHEMA = "elaina-mage-balance-v2"
MAX_BYTES = 32 * 1024

def number(obj, key, lo, hi, integer=False):
    value = obj.get(key)
    if isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(value) or not lo <= value <= hi:
        raise ValueError(f"{key} 必须在 {lo}..{hi} 范围内。")
    if integer and value != int(value):
        raise ValueError(f"{key} 必须是整数。")
    return value

def validate_payload(payload):
    if not isinstance(payload, dict) or payload.get("schema") != SCHEMA:
        raise ValueError("不支持的配置格式。")
    c = payload.get("configuration")
    if not isinstance(c, dict) or c.get("version") != 2:
        raise ValueError("配置版本无效。")
    number(c, "rate", 0, 20)
    progression = c.get("progression")
    if not isinstance(progression, dict): raise ValueError("progression 缺失。")
    number(progression, "phaseSize", 1, 20, True)
    number(progression, "previewLevel", 1, 1000, True)
    rows = c.get("skills")
    if not isinstance(rows, list) or len(rows) > 32:
        raise ValueError("skills 必须是最多 32 项的数组。")
    basics = c.get("basicAttacks")
    if not isinstance(basics, list) or not 1 <= len(basics) <= 32:
        raise ValueError("basicAttacks 必须是 1–32 项的数组。")
    basic_ids = set()
    for row in basics:
        if not isinstance(row, dict):
            raise ValueError("普攻必须是对象。")
        key, name = row.get("id"), row.get("name")
        if not isinstance(key, str) or not re.fullmatch(r"[A-Za-z][A-Za-z0-9_.-]{0,63}", key) or key in basic_ids:
            raise ValueError("普攻 ID 无效或重复。")
        if not isinstance(name, str) or not name.strip() or len(name.encode("utf-16-le")) // 2 > 64:
            raise ValueError("普攻名称不能为空或超过 64 字符。")
        basic_ids.add(key)
        number(row, "ratio", 0, 5)
        number(row, "cost", 0, 10000, True)
        number(row, "interval", .05, 10)
        number(row, "shots", 1, 100, True)
        number(row, "unlockStageCap", 1, 1000, True)
        number(row, "unlockLevel", 1, 1000, True)
        number(row, "levelsPerPhase", 1, 10, True)
    if not isinstance(c.get("selectedBasicAttackId"), str) or c["selectedBasicAttackId"] not in basic_ids:
        raise ValueError("参考普攻 ID 无效。")
    ids = set()
    for row in rows:
        if not isinstance(row, dict):
            raise ValueError("技能必须是对象。")
        key, name = row.get("id"), row.get("name")
        if not isinstance(key, str) or not re.fullmatch(r"[A-Za-z][A-Za-z0-9_.-]{0,63}", key) or key in ids or key in basic_ids:
            raise ValueError("技能 ID 无效或与普攻重复；普通攻击使用独立参数。")
        if not isinstance(name, str) or not name.strip() or len(name.encode("utf-16-le")) // 2 > 64:
            raise ValueError("技能名称不能为空或超过 64 字符。")
        ids.add(key)
        number(row, "cost", 0, 10000, True)
        number(row, "cd", .1, 600)
        number(row, "k", 0, 3)
        number(row, "cast", 0, 60)
        number(row, "shots", 1, 100, True)
        number(row, "unlockStageCap", 1, 1000, True)
        number(row, "unlockLevel", 1, 1000, True)
        number(row, "levelsPerPhase", 1, 10, True)
    return {"schema": SCHEMA, "configuration": c}

def atomic_save(path, payload):
    data = (json.dumps(validate_payload(payload), ensure_ascii=False, indent=2, allow_nan=False) + "\n").encode("utf-8")
    if len(data) > MAX_BYTES:
        raise ValueError("参数超过 32 KiB，不能保存包含事件日志的试算结果。")
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = None
    try:
        with tempfile.NamedTemporaryFile(dir=path.parent, prefix=".SkillBalance-", suffix=".tmp", delete=False) as file:
            temporary = Path(file.name)
            file.write(data)
            file.flush()
            os.fsync(file.fileno())
        os.replace(temporary, path)
        temporary = None
    finally:
        if temporary is not None:
            temporary.unlink(missing_ok=True)

class Handler(BaseHTTPRequestHandler):
    def allowed(self):
        expected = f"127.0.0.1:{self.server.server_port}"
        if self.headers.get("Host") != expected:
            self.reply(403, {"error": "只接受本机编辑器请求。"})
            return False
        origin = self.headers.get("Origin")
        if origin is not None and origin != f"http://{expected}":
            self.reply(403, {"error": "不允许跨站请求。"})
            return False
        return True

    def reply(self, status, body, content_type="application/json; charset=utf-8"):
        data = body if isinstance(body, bytes) else json.dumps(body, ensure_ascii=False).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(data)))
        self.send_header("Cache-Control", "no-store")
        self.send_header("X-Content-Type-Options", "nosniff")
        self.send_header("Content-Security-Policy", "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; connect-src 'self'; img-src 'self' data:")
        self.end_headers()
        self.wfile.write(data)

    def do_GET(self):
        if not self.allowed(): return
        route = urlsplit(self.path).path
        if route in ("/", "/index.html"):
            self.reply(200, HTML_PATH.read_bytes(), "text/html; charset=utf-8")
        elif route == "/favicon.ico":
            self.reply(204, b"", "image/x-icon")
        elif route == "/api/info":
            self.reply(200, {"tool": "elaina-mage-balance", "schema": SCHEMA, "path": str(self.server.config_path)})
        elif route == "/api/skill-balance":
            try:
                if self.server.config_path.stat().st_size > MAX_BYTES: raise ValueError("配置超过 32 KiB。")
                data = validate_payload(json.loads(self.server.config_path.read_text(encoding="utf-8-sig")))
                self.reply(200, {"path": str(self.server.config_path), "data": data})
            except (OSError, ValueError) as error:
                self.reply(422, {"error": str(error)})
        else:
            self.reply(404, {"error": "没有此接口。"})

    def do_PUT(self):
        if not self.allowed(): return
        if urlsplit(self.path).path != "/api/skill-balance":
            self.reply(404, {"error": "没有此接口。"}); return
        if self.headers.get("Content-Type", "").split(";")[0].strip() != "application/json":
            self.reply(415, {"error": "需要 application/json。"}); return
        try:
            length = int(self.headers.get("Content-Length", "0"))
            if not 0 < length <= MAX_BYTES: raise ValueError("请求为空或超过 32 KiB。")
            payload = json.loads(self.rfile.read(length).decode("utf-8-sig"))
            atomic_save(self.server.config_path, payload)
            self.reply(200, {"saved": True, "path": str(self.server.config_path)})
        except (OSError, ValueError) as error:
            self.reply(422, {"error": str(error)})

    def log_message(self, format, *args):
        print(format % args, flush=True)

def make_server(port=8768, config_path=CONFIG_PATH):
    server = ThreadingHTTPServer(("127.0.0.1", port), Handler)
    server.config_path = Path(config_path).resolve()
    server.daemon_threads = True
    return server

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--port", type=int, default=8768)
    args = parser.parse_args()
    with make_server(args.port) as server:
        print(f"MageBalance: http://127.0.0.1:{server.server_port}/", flush=True)
        print(f"Fixed JSON: {server.config_path}", flush=True)
        try: server.serve_forever()
        except KeyboardInterrupt: pass
