#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/_load_env.sh"

CONDA_ENV_NAME="${CONDA_ENV_NAME:-sts2-modding}"
if ! command -v conda >/dev/null 2>&1; then
  echo "conda not found on PATH" >&2
  exit 1
fi

# Some conda activation hooks reference unset vars (for example DOTNET_ROOT).
# Temporarily disable nounset so activation hooks can run safely.
had_nounset=0
if [[ "$-" == *u* ]]; then
  had_nounset=1
fi
set +u

# shellcheck disable=SC1091
eval "$(conda shell.bash hook)"
conda activate "${CONDA_ENV_NAME}"

if [[ "${had_nounset}" -eq 1 ]]; then
  set -u
fi

echo "Activated conda env: ${CONDA_ENV_NAME}"
if [[ -n "${STS2_INSTALL_DIR:-}" ]]; then
  echo "STS2_INSTALL_DIR=${STS2_INSTALL_DIR}"
fi
