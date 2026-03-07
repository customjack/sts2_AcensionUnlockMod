#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
ENV_NAME="${CONDA_ENV_NAME:-sts2-modding}"

if ! command -v conda >/dev/null 2>&1; then
  echo "conda not found on PATH" >&2
  exit 1
fi

if conda env list | awk '{print $1}' | grep -Fxq "${ENV_NAME}"; then
  echo "Updating conda env '${ENV_NAME}' from environment.yml..."
  conda env update -n "${ENV_NAME}" -f "${PROJECT_ROOT}/environment.yml" --prune
else
  echo "Creating conda env '${ENV_NAME}' from environment.yml..."
  conda env create -n "${ENV_NAME}" -f "${PROJECT_ROOT}/environment.yml"
fi

echo "Done. Activate with: source scripts/bash/activate_env.sh"
