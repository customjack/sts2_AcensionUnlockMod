#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/_load_env.sh"

MANIFEST_PATH="${MANIFEST_PATH:-${PROJECT_ROOT}/mod_manifest.json}"
PROJECT_FILE="${PROJECT_FILE:-$(find "${PROJECT_ROOT}" -maxdepth 1 -name '*.csproj' | head -n1)}"
CONDA_ENV_NAME="${CONDA_ENV_NAME:-sts2-modding}"
USE_CONDA_DOTNET="${USE_CONDA_DOTNET:-1}"
CONFIG="${1:-Debug}"
TFM="net9.0"

if [[ -z "${STS2_INSTALL_DIR:-}" ]]; then
  echo "STS2_INSTALL_DIR is not set. Create ${PROJECT_ROOT}/.env from .env.example first." >&2
  exit 1
fi

if [[ ! -f "${MANIFEST_PATH}" ]]; then
  echo "Missing manifest: ${MANIFEST_PATH}" >&2
  exit 1
fi

if [[ ! -f "${PROJECT_FILE}" ]]; then
  echo "Could not find a .csproj in ${PROJECT_ROOT}." >&2
  exit 1
fi

if command -v python3 >/dev/null 2>&1; then
  PYTHON_EXE="python3"
elif command -v python >/dev/null 2>&1; then
  PYTHON_EXE="python"
else
  echo "python not found on PATH" >&2
  exit 1
fi

if [[ "${USE_CONDA_DOTNET}" == "1" ]] && command -v conda >/dev/null 2>&1; then
  BUILD_CMD=(conda run --no-capture-output -n "${CONDA_ENV_NAME}" dotnet)
elif [[ -n "${DOTNET_EXE:-}" ]]; then
  BUILD_CMD=("${DOTNET_EXE}")
elif command -v dotnet >/dev/null 2>&1; then
  BUILD_CMD=(dotnet)
else
  echo "dotnet executable not found. Install dotnet or enable conda env usage." >&2
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
BUILD_OUT="${PROJECT_ROOT}/bin/${CONFIG}/${TFM}"
ROOT_JSON="${PROJECT_ROOT}/${MOD_ID}.json"

echo "Building ${MOD_ID} (${CONFIG})..."
"${BUILD_CMD[@]}" build "${PROJECT_FILE}" -c "${CONFIG}" -p:Sts2InstallDir="${STS2_INSTALL_DIR}"

mkdir -p "${DIST_DIR}"
cp -f "${BUILD_OUT}/${ASSEMBLY_NAME}.dll" "${DIST_DIR}/${MOD_ID}.dll"
cp -f "${PROJECT_ROOT}/mod_manifest.json" "${DIST_DIR}/${MOD_ID}.json"
cp -f "${PROJECT_ROOT}/mod_manifest.json" "${ROOT_JSON}"
rm -f "${DIST_DIR}/mod_manifest.json"

if [[ -f "${PROJECT_ROOT}/${MOD_ID}.pck" ]]; then
  cp -f "${PROJECT_ROOT}/${MOD_ID}.pck" "${DIST_DIR}/${MOD_ID}.pck"
else
  rm -f "${DIST_DIR}/${MOD_ID}.pck"
fi

if [[ "${ASSEMBLY_NAME}" != "${MOD_ID}" ]]; then
  rm -f "${DIST_DIR}/${ASSEMBLY_NAME}.dll"
fi

echo "Staged files in: ${DIST_DIR}"
ls -la "${DIST_DIR}"
