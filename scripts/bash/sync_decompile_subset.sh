#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/_load_env.sh"

SOURCE_ROOT_DEFAULT="${PROJECT_ROOT}/../../decompile"
if [[ -n "${STS2_INSTALL_DIR:-}" && -d "${STS2_INSTALL_DIR}/modding/decompile" ]]; then
  SOURCE_ROOT_DEFAULT="${STS2_INSTALL_DIR}/modding/decompile"
fi
SOURCE_ROOT="${SOURCE_ROOT:-${SOURCE_ROOT_DEFAULT}}"
TARGET_ROOT="${PROJECT_ROOT}/decompile"

FILES=(
  "MegaCrit.Sts2.Core.Saves/SaveManager.cs"
  "MegaCrit.Sts2.Core.Saves/ProgressState.cs"
  "MegaCrit.Sts2.Core.Saves/CharacterStats.cs"
  "MegaCrit.Sts2.Core.DevConsole.ConsoleCommands/UnlockConsoleCmd.cs"
  "MegaCrit.Sts2.Core.Nodes.Screens.MainMenu/NMainMenu.cs"
)

for rel in "${FILES[@]}"; do
  src="${SOURCE_ROOT}/${rel}"
  dst="${TARGET_ROOT}/${rel}"
  mkdir -p "$(dirname "${dst}")"
  cp -f "${src}" "${dst}"
  echo "Synced: ${rel}"
done
