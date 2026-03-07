#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

default_log_path="/mnt/c/Users/custo/AppData/Roaming/SlayTheSpire2/logs/godot.log"
for candidate in /mnt/c/Users/*/AppData/Roaming/SlayTheSpire2/logs/godot.log; do
  if [[ -f "${candidate}" ]]; then
    default_log_path="${candidate}"
    break
  fi
done

export LOG_VIEWER_LOG_PATH="${LOG_VIEWER_LOG_PATH:-${default_log_path}}"
export LOG_VIEWER_HOST="${LOG_VIEWER_HOST:-127.0.0.1}"
export LOG_VIEWER_PORT="${LOG_VIEWER_PORT:-8765}"
export LOG_VIEWER_POLL_MS="${LOG_VIEWER_POLL_MS:-1200}"
export LOG_VIEWER_INITIAL_TAIL_BYTES="${LOG_VIEWER_INITIAL_TAIL_BYTES:-200000}"
export LOG_VIEWER_MAX_RESPONSE_BYTES="${LOG_VIEWER_MAX_RESPONSE_BYTES:-262144}"
export LOG_VIEWER_MAX_LINES="${LOG_VIEWER_MAX_LINES:-12000}"

exec python3 "${SCRIPT_DIR}/server.py"
