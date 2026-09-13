#!/usr/bin/env bash
# Linux/macOS counterpart of stage-mods.ps1. Clears $Dest and copies each "name=src" mod
# folder into it as Dest/name. Accepts the same flags as the PowerShell version so the
# VS Code tasks can share one argument list per OS.
#
#   stage-mods.sh -Dest <dir> <name=src> [<name=src> ...]
set -euo pipefail

dest=""
mods=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    -Dest) dest="${2:?-Dest needs a value}"; shift 2 ;;
    *)     mods+=("$1"); shift ;;
  esac
done

[[ -n "$dest" ]] || { echo "Missing -Dest <dir>" >&2; exit 1; }
[[ ${#mods[@]} -gt 0 ]] || { echo "No mods given (expected name=src arguments)" >&2; exit 1; }

rm -rf "$dest"
mkdir -p "$dest"

for entry in "${mods[@]}"; do
  name="${entry%%=*}"
  src="${entry#*=}"
  [[ "$name" != "$entry" ]] || { echo "Bad mod arg (expected name=src): $entry" >&2; exit 1; }
  [[ -d "$src" ]] || { echo "Mod source not found: $src" >&2; exit 1; }
  cp -r "$src" "$dest/$name"
  echo "Staged '$name' from $src"
done

echo "Staged ${#mods[@]} mod(s) into $dest"
