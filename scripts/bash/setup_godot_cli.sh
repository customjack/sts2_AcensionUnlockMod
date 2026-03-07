#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

GODOT_VERSION="${GODOT_VERSION:-4.5.1-stable}"
GODOT_PLATFORM="${GODOT_PLATFORM:-linux.x86_64}"
GODOT_BASE="Godot_v${GODOT_VERSION}_${GODOT_PLATFORM}"
GODOT_URL="https://github.com/godotengine/godot/releases/download/${GODOT_VERSION}/${GODOT_BASE}.zip"

TOOLS_DIR="${PROJECT_ROOT}/.tools/godot/${GODOT_VERSION}"
ZIP_PATH="${TOOLS_DIR}/${GODOT_BASE}.zip"
BIN_PATH="${TOOLS_DIR}/${GODOT_BASE}"
LINK_PATH="${PROJECT_ROOT}/.tools/godot/godot"

mkdir -p "${TOOLS_DIR}" "$(dirname "${LINK_PATH}")"

if [[ ! -x "${BIN_PATH}" ]]; then
  echo "Downloading ${GODOT_URL}"
  curl -fL "${GODOT_URL}" -o "${ZIP_PATH}"
  unzip -o "${ZIP_PATH}" -d "${TOOLS_DIR}" >/dev/null
  chmod +x "${BIN_PATH}"
fi

ln -sfn "${BIN_PATH}" "${LINK_PATH}"

echo "Godot CLI ready: ${LINK_PATH}"
