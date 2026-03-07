# STS2 Log Viewer

Small local webpage for watching the STS2 log file in real time.

## Run
From this folder:

```bash
./run.sh
```

Open:

```text
http://127.0.0.1:8765
```

## Environment variables
- `LOG_VIEWER_LOG_PATH`
  default: auto-detected from `/mnt/c/Users/*/AppData/Roaming/SlayTheSpire2/logs/godot.log`
- `LOG_VIEWER_HOST`
  default: `127.0.0.1`
- `LOG_VIEWER_PORT`
  default: `8765`
- `LOG_VIEWER_POLL_MS`
  default: `1200`
- `LOG_VIEWER_INITIAL_TAIL_BYTES`
  default: `200000`
- `LOG_VIEWER_MAX_RESPONSE_BYTES`
  default: `262144`
- `LOG_VIEWER_MAX_LINES`
  default: `12000`

Example:

```bash
LOG_VIEWER_PORT=8877 LOG_VIEWER_POLL_MS=800 ./run.sh
```

## Features
- Polls the log periodically from the browser.
- Search box filters lines like a simple Ctrl+F.
- Case-sensitive filter option.
- Auto-scroll toggle.
- Pause/resume polling.
