#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/_load_env.sh"

MOD_BASENAME="${MOD_BASENAME:-AscensionUnlockMod}"
CONDA_ENV_NAME="${CONDA_ENV_NAME:-sts2-modding}"
USE_CONDA_DOTNET="${USE_CONDA_DOTNET:-1}"
CONFIG="${1:-Debug}"
TFM="net9.0"

if [[ -z "${STS2_INSTALL_DIR:-}" ]]; then
  echo "STS2_INSTALL_DIR is not set. Create ${PROJECT_ROOT}/.env from .env.example first." >&2
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

echo "Building ${MOD_BASENAME} (${CONFIG})..."
"${BUILD_CMD[@]}" build "${PROJECT_ROOT}/${MOD_BASENAME}.csproj" -c "${CONFIG}" -p:Sts2InstallDir="${STS2_INSTALL_DIR}"

BUILD_OUT="${PROJECT_ROOT}/bin/${CONFIG}/${TFM}"
DIST_DIR="${PROJECT_ROOT}/dist/${MOD_BASENAME}"
mkdir -p "${DIST_DIR}"

cp -f "${BUILD_OUT}/${MOD_BASENAME}.dll" "${DIST_DIR}/${MOD_BASENAME}.dll"
cp -f "${PROJECT_ROOT}/mod_manifest.json" "${DIST_DIR}/mod_manifest.json"

if [[ -f "${PROJECT_ROOT}/${MOD_BASENAME}.pck" ]]; then
  cp -f "${PROJECT_ROOT}/${MOD_BASENAME}.pck" "${DIST_DIR}/${MOD_BASENAME}.pck"
else
  rm -f "${DIST_DIR}/${MOD_BASENAME}.pck"
fi

echo "Staged files in: ${DIST_DIR}"
ls -la "${DIST_DIR}"
