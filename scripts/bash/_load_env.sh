#!/usr/bin/env bash
# Shared .env loader for AscensionUnlockMod bash scripts.

if [[ -n "${ASCENSION_UNLOCK_MOD_ENV_LOADED:-}" ]]; then
  return 0
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
ENV_FILE="${ASCENSION_UNLOCK_MOD_ENV_FILE:-${PROJECT_ROOT}/.env}"

if [[ -f "${ENV_FILE}" ]]; then
  had_allexport=0
  if [[ "$-" == *a* ]]; then
    had_allexport=1
  fi

  set -a
  # shellcheck disable=SC1090
  source "${ENV_FILE}"
  if [[ "${had_allexport}" -eq 0 ]]; then
    set +a
  fi
fi

if [[ -n "${STS2_INSTALL_DIR:-}" ]]; then
  # Accept either WSL paths (/mnt/c/...) or Windows paths (C:\...).
  if [[ "${STS2_INSTALL_DIR}" =~ ^([A-Za-z]):\\ ]]; then
    drive_letter="$(printf '%s' "${BASH_REMATCH[1]}" | tr '[:upper:]' '[:lower:]')"
    path_tail="${STS2_INSTALL_DIR:2}"
    path_tail="${path_tail//\\//}"
    STS2_INSTALL_DIR="/mnt/${drive_letter}${path_tail}"
  fi

  STS2_INSTALL_DIR="${STS2_INSTALL_DIR%/}"
  export STS2_INSTALL_DIR
fi

export ASCENSION_UNLOCK_MOD_ENV_FILE="${ENV_FILE}"
export ASCENSION_UNLOCK_MOD_ENV_LOADED=1
