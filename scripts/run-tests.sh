#!/usr/bin/env bash
# Linux/macOS counterpart of run-tests.ps1. Runs the test suite per game version, each version's
# projects in parallel. Mods stay single-target; legacy versions build the test projects with
# -p:Legacy=true and that version's TFM. Each build auto-provisions its game version on demand.
# A green run records each project's test count per version in .exmod/census/<branch>.json; a later
# run below a recorded count fails unless --accept-drop records the lower count.
#
#   run-tests.sh [latest|all|1.22|1.21|1.20] [--accept-drop]
set -euo pipefail

usage="Usage: run-tests.sh [latest|all|1.22|1.21|1.20] [--accept-drop]"
version=latest
accept_drop=false
for arg in "$@"; do
  case "$arg" in
    latest|all|1.22|1.21|1.20) version="$arg" ;;
    --accept-drop) accept_drop=true ;;
    *) echo "$usage" >&2; exit 1 ;;
  esac
done
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"

declare -A tfms=( [1.22]=net10.0 [1.21]=net8.0 [1.20]=net7.0 )
projects=(ExpandedLib.Tests PipesAndPowerExpanded.Tests SteelmakingExpanded.Tests Integration.Tests)

case "$version" in
  latest) wanted=(1.22) ;;
  all)    wanted=(1.22 1.21 1.20) ;;
  *)      wanted=("$version") ;;
esac

# Pick the dotnet host: the system one if it already carries every runtime major the wanted
# versions need, else a self-contained .dotnet (provisioned on demand) and its own muxer - the
# global muxer ignores DOTNET_ROOT, so a local muxer is the only reliable way to run on
# locally-installed runtimes. Lets a fresh clone without .NET 7/8 run the legacy suites.
declare -A majors=( [1.22]=10 [1.21]=8 [1.20]=7 )
sys_runtimes="$(dotnet --list-runtimes 2>/dev/null || true)"
missing=()
for v in "${wanted[@]}"; do
  grep -q "Microsoft.NETCore.App ${majors[$v]}\." <<< "$sys_runtimes" || missing+=("${majors[$v]}")
done
dotnet_bin="dotnet"
if [[ ${#missing[@]} -gt 0 ]]; then
  echo "Missing .NET runtime major(s) system-wide: ${missing[*]} - provisioning a local .dotnet..."
  "$script_dir/provision-dotnet.sh" "$version"
  dotnet_bin="$("$script_dir/provision-dotnet.sh" --print-root)/dotnet"
fi
echo "Using dotnet host: $dotnet_bin"

log_dir="$(mktemp -d)"
trap 'rm -rf "$log_dir"' EXIT

# Build phase, serial: the test projects share the mod projects (exlib/ppex/smex), so building them
# concurrently would race on the same intermediate DLLs. Building here also auto-provisions each
# version's game binaries once. The test phase then runs in parallel with --no-build, and only over
# the targets that built: a failed build leaves the previous DLLs in place.
combos=()
for v in "${wanted[@]}"; do for p in "${projects[@]}"; do combos+=("$v/$p"); done; done
echo "Building ${#combos[@]} test target(s) across version(s): ${wanted[*]}"
declare -A built=()
for c in "${combos[@]}"; do
  v="${c%%/*}"; p="${c##*/}"; tfm="${tfms[$v]}"
  args=(build "$repo_root/tests/$p/$p.csproj" -f "$tfm" --nologo -v q)
  [[ "$tfm" != "net10.0" ]] && args+=(-p:Legacy=true)
  if "$dotnet_bin" "${args[@]}" > "$log_dir/${c//\//_}.build.log" 2>&1; then
    built[$c]=1
  else
    echo "build-failed:$c" >> "$log_dir/buildfail"
  fi
done
if [[ -s "$log_dir/buildfail" ]]; then
  { cat "$log_dir/buildfail"; for f in "$log_dir"/*.build.log; do echo "--- $f"; tail -20 "$f"; done; } >&2
fi

echo "Running tests in parallel..."
declare -A pids=()
for c in "${combos[@]}"; do
  [[ -n "${built[$c]:-}" ]] || continue
  v="${c%%/*}"; p="${c##*/}"; tfm="${tfms[$v]}"
  args=(test "$repo_root/tests/$p/$p.csproj" -f "$tfm" --no-build --nologo)
  [[ "$tfm" != "net10.0" ]] && args+=(-p:Legacy=true)
  "$dotnet_bin" "${args[@]}" > "$log_dir/${c//\//_}.log" 2>&1 &
  pids[$c]=$!
done
fail=0
declare -A totals=()
for c in "${combos[@]}"; do
  log="$log_dir/${c//\//_}.log"
  if [[ -z "${built[$c]:-}" ]]; then
    status=FAIL; line="build failed"
  else
    if wait "${pids[$c]}"; then status=PASS; else status=FAIL; fi
    line="$(grep -hE 'Passed!|Failed!' "$log" | tail -1 | tr -s ' ' || true)"
    total="$(sed -nE 's/.*Total: *([0-9]+).*/\1/p' <<< "$line")"
    if [[ -z "$total" ]]; then
      status=FAIL; line="no test summary"
      err="$(grep -hE 'error' "$log" | tail -1 | tr -s ' ' || true)"
      [[ -z "$err" ]] || line+=": $err"
    else
      totals[$c]=$total
    fi
  fi
  [[ $status == PASS ]] || fail=$((fail+1))
  printf '%s  %-40s %s\n' "$status" "$c" "$line"
done

# The census: the last green test count per version/project on this branch, a flat JSON object that
# run-tests.ps1 reads and writes as well.
branch="$(git -C "$repo_root" rev-parse --abbrev-ref HEAD 2>/dev/null || echo detached)"
census="$repo_root/.exmod/census/${branch//\//-}.json"
declare -A recorded=()
if [[ -f "$census" ]]; then
  while read -r key count; do recorded[$key]=$count; done \
    < <(sed -nE 's/^[[:space:]]*"([^"]+)"[[:space:]]*:[[:space:]]*([0-9]+).*/\1 \2/p' "$census")
fi
drops=0
for c in "${combos[@]}"; do
  [[ -n "${totals[$c]:-}" ]] || continue
  was="${recorded[$c]:-}"
  if [[ -z "$was" ]]; then
    note="new"
  elif [[ "${totals[$c]}" -ge "$was" ]]; then
    note="was $was"
  elif $accept_drop; then
    note="was $was, drop accepted"
  else
    note="was $was, a drop"; drops=$((drops+1))
  fi
  printf 'census  %-40s %s (%s)\n' "$c" "${totals[$c]}" "$note"
done

if [[ $fail -gt 0 || $drops -gt 0 ]]; then
  [[ $fail -eq 0 ]] || echo "$fail test run(s) failed." >&2
  [[ $drops -eq 0 ]] || echo "$drops test count(s) fell below $census; --accept-drop records them." >&2
  exit 1
fi
for c in "${!totals[@]}"; do recorded[$c]=${totals[$c]}; done
mkdir -p "$(dirname "$census")"
{
  echo "{"
  mapfile -t keys < <(printf '%s\n' "${!recorded[@]}" | sort)
  for i in "${!keys[@]}"; do
    sep=","; [[ $i -eq $((${#keys[@]} - 1)) ]] && sep=""
    printf '  "%s": %s%s\n' "${keys[$i]}" "${recorded[${keys[$i]}]}" "$sep"
  done
  echo "}"
} > "$census"
echo "All ${#pids[@]} test run(s) passed."
