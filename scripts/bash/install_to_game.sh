#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/_load_env.sh"

MANIFEST_PATH="${MANIFEST_PATH:-${PROJECT_ROOT}/mod_manifest.json}"
PROJECT_FILE="${PROJECT_FILE:-$(find "${PROJECT_ROOT}" -maxdepth 1 -name '*.csproj' | head -n1)}"

if command -v python3 >/dev/null 2>&1; then
  PYTHON_EXE="python3"
elif command -v python >/dev/null 2>&1; then
  PYTHON_EXE="python"
else
  echo "python not found on PATH" >&2
  exit 1
fi

MOD_ID="$(${PYTHON_EXE} - "${MANIFEST_PATH}" <<'PY'
import json, sys
with open(sys.argv[1], encoding='utf-8') as f:
    data = json.load(f)
print((data.get('id') or data.get('pck_name') or data.get('name') or '').strip())
PY
)"
ASSEMBLY_NAME="$(sed -n 's:.*<AssemblyName>\(.*\)</AssemblyName>.*:\1:p' "${PROJECT_FILE}" | head -n1)"
if [[ -z "${ASSEMBLY_NAME}" ]]; then
  ASSEMBLY_NAME="$(basename "${PROJECT_FILE}" .csproj)"
fi
if [[ -z "${MOD_ID}" ]]; then
  MOD_ID="${ASSEMBLY_NAME}"
fi

DIST_DIR="${PROJECT_ROOT}/dist/${MOD_ID}"
SRC_DLL="${DIST_DIR}/${MOD_ID}.dll"
SRC_PCK="${DIST_DIR}/${MOD_ID}.pck"
SRC_JSON="${DIST_DIR}/${MOD_ID}.json"

if [[ -z "${STS2_INSTALL_DIR:-}" ]]; then
  echo "STS2_INSTALL_DIR is not set. Create ${PROJECT_ROOT}/.env from .env.example first." >&2
  exit 1
fi

GAME_MOD_DIR="${1:-${STS2_INSTALL_DIR}/mods/${MOD_ID}}"
DST_DLL="${GAME_MOD_DIR}/${MOD_ID}.dll"
DST_PCK="${GAME_MOD_DIR}/${MOD_ID}.pck"
DST_JSON="${GAME_MOD_DIR}/${MOD_ID}.json"

if [[ ! -f "${SRC_DLL}" ]]; then
  echo "Missing ${SRC_DLL}. Run scripts/bash/build_and_stage.sh first." >&2
  exit 1
fi
if [[ ! -f "${SRC_JSON}" ]]; then
  echo "Missing ${SRC_JSON}. Run scripts/bash/build_and_stage.sh first." >&2
  exit 1
fi

mkdir -p "${GAME_MOD_DIR}"
cp -f "${SRC_DLL}" "${DST_DLL}"
cp -f "${SRC_JSON}" "${DST_JSON}"
rm -f "${GAME_MOD_DIR}/mod_manifest.json"

if [[ -f "${SRC_PCK}" ]]; then
  cp -f "${SRC_PCK}" "${DST_PCK}"
else
  echo "Warning: ${MOD_ID}.pck not found in dist; DLL only was installed."
fi

if ! cmp -s "${SRC_DLL}" "${DST_DLL}"; then
  echo "ERROR: Copied DLL does not match source after install." >&2
  exit 1
fi
if ! cmp -s "${SRC_JSON}" "${DST_JSON}"; then
  echo "ERROR: Copied JSON does not match source after install." >&2
  exit 1
fi
if [[ -f "${SRC_PCK}" ]] && ! cmp -s "${SRC_PCK}" "${DST_PCK}"; then
  echo "ERROR: Copied PCK does not match source after install." >&2
  exit 1
fi

echo "Installed to: ${GAME_MOD_DIR}"
ls -la "${GAME_MOD_DIR}"
