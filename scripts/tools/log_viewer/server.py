#!/usr/bin/env python3
from __future__ import annotations

import json
import os
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from typing import Any
from urllib.parse import parse_qs, urlparse


def _default_log_path() -> str:
    fallback = "/mnt/c/Users/custo/AppData/Roaming/SlayTheSpire2/logs/godot.log"
    users_dir = Path("/mnt/c/Users")
    if not users_dir.is_dir():
        return fallback

    for candidate in users_dir.glob("*/AppData/Roaming/SlayTheSpire2/logs/godot.log"):
        if candidate.is_file():
            return str(candidate)
    return fallback


DEFAULT_LOG_PATH = _default_log_path()


def _env_int(name: str, default: int, min_value: int, max_value: int) -> int:
    raw = os.environ.get(name, str(default))
    try:
        value = int(raw)
    except ValueError:
        return default
    return max(min_value, min(max_value, value))


LOG_PATH = os.environ.get("LOG_VIEWER_LOG_PATH", DEFAULT_LOG_PATH)
HOST = os.environ.get("LOG_VIEWER_HOST", "127.0.0.1")
PORT = _env_int("LOG_VIEWER_PORT", 8765, 1, 65535)
POLL_MS = _env_int("LOG_VIEWER_POLL_MS", 1200, 200, 60000)
INITIAL_TAIL_BYTES = _env_int("LOG_VIEWER_INITIAL_TAIL_BYTES", 200000, 1024, 10_000_000)
MAX_RESPONSE_BYTES = _env_int("LOG_VIEWER_MAX_RESPONSE_BYTES", 262144, 1024, 2_000_000)
MAX_LINES = _env_int("LOG_VIEWER_MAX_LINES", 12000, 200, 200000)

ROOT_DIR = Path(__file__).resolve().parent
WEB_DIR = ROOT_DIR / "web"


class LogViewerHandler(BaseHTTPRequestHandler):
    def do_GET(self) -> None:
        parsed = urlparse(self.path)
        path = parsed.path

        if path == "/":
            self._serve_static(WEB_DIR / "index.html", "text/html; charset=utf-8")
            return
        if path == "/app.js":
            self._serve_static(WEB_DIR / "app.js", "application/javascript; charset=utf-8")
            return
        if path == "/app.css":
            self._serve_static(WEB_DIR / "app.css", "text/css; charset=utf-8")
            return
        if path == "/api/config":
            self._send_json(
                {
                    "log_path": LOG_PATH,
                    "poll_ms": POLL_MS,
                    "initial_tail_bytes": INITIAL_TAIL_BYTES,
                    "max_lines": MAX_LINES,
                }
            )
            return
        if path == "/api/log":
            self._handle_log_api(parsed.query)
            return

        self.send_error(404, "Not Found")

    def _handle_log_api(self, query: str) -> None:
        params = parse_qs(query)
        offset = self._parse_optional_int(params.get("offset", [None])[0])
        tail = self._parse_optional_int(params.get("tail", [None])[0])

        log_path = Path(LOG_PATH)
        if not log_path.exists():
            self._send_json(
                {
                    "exists": False,
                    "path": str(log_path),
                    "offset": 0,
                    "reset": False,
                    "content": "",
                }
            )
            return

        stat = log_path.stat()
        size = stat.st_size
        reset = False

        if offset is None:
            tail_bytes = INITIAL_TAIL_BYTES if tail is None else max(1, min(10_000_000, tail))
            start_offset = max(0, size - tail_bytes)
        else:
            start_offset = max(0, offset)
            if start_offset > size:
                reset = True
                start_offset = max(0, size - INITIAL_TAIL_BYTES)

        read_size = max(0, min(MAX_RESPONSE_BYTES, size - start_offset))
        with log_path.open("rb") as handle:
            handle.seek(start_offset)
            chunk = handle.read(read_size)

        next_offset = start_offset + len(chunk)

        self._send_json(
            {
                "exists": True,
                "path": str(log_path),
                "size": size,
                "mtime_ms": stat.st_mtime_ns // 1_000_000,
                "start_offset": start_offset,
                "offset": next_offset,
                "reset": reset,
                "truncated": next_offset < size,
                "content": chunk.decode("utf-8", errors="replace"),
            }
        )

    @staticmethod
    def _parse_optional_int(raw: str | None) -> int | None:
        if raw is None:
            return None
        try:
            return int(raw)
        except (TypeError, ValueError):
            return None

    def _serve_static(self, file_path: Path, content_type: str) -> None:
        if not file_path.exists():
            self.send_error(404, "Missing file")
            return
        payload = file_path.read_bytes()
        self.send_response(200)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(payload)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(payload)

    def _send_json(self, body: dict[str, Any], status_code: int = 200) -> None:
        payload = json.dumps(body, separators=(",", ":"), ensure_ascii=False).encode("utf-8")
        self.send_response(status_code)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(payload)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(payload)

    def log_message(self, fmt: str, *args: object) -> None:
        return


def main() -> None:
    server = ThreadingHTTPServer((HOST, PORT), LogViewerHandler)
    print(f"STS2 Log Viewer running at http://{HOST}:{PORT}")
    print(f"Watching: {LOG_PATH}")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()


if __name__ == "__main__":
    main()
