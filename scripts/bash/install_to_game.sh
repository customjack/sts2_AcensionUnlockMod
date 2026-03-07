#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/_load_env.sh"

MOD_BASENAME="${MOD_BASENAME:-AscensionUnlockMod}"
DIST_DIR="${PROJECT_ROOT}/dist/${MOD_BASENAME}"

if [[ -z "${STS2_INSTALL_DIR:-}" ]]; then
  echo "STS2_INSTALL_DIR is not set. Create ${PROJECT_ROOT}/.env from .env.example first." >&2
  exit 1
fi

GAME_MOD_DIR="${1:-${STS2_INSTALL_DIR}/mods/${MOD_BASENAME}}"

if [[ ! -f "${DIST_DIR}/${MOD_BASENAME}.dll" ]]; then
  echo "Missing ${DIST_DIR}/${MOD_BASENAME}.dll. Run scripts/bash/build_and_stage.sh first." >&2
  exit 1
fi

mkdir -p "${GAME_MOD_DIR}"
if ! cp -f "${DIST_DIR}/${MOD_BASENAME}.dll" "${GAME_MOD_DIR}/${MOD_BASENAME}.dll"; then
  echo "Failed to copy ${MOD_BASENAME}.dll. It is likely locked by a running game process." >&2
  exit 1
fi

if [[ -f "${DIST_DIR}/${MOD_BASENAME}.pck" ]]; then
  if ! cp -f "${DIST_DIR}/${MOD_BASENAME}.pck" "${GAME_MOD_DIR}/${MOD_BASENAME}.pck"; then
    echo "Failed to copy ${MOD_BASENAME}.pck. Close Slay the Spire 2 and rerun this script." >&2
    exit 1
  fi
else
  echo "Warning: ${MOD_BASENAME}.pck not found in dist; DLL only was installed."
fi

echo "Installed to: ${GAME_MOD_DIR}"
ls -la "${GAME_MOD_DIR}"
