#!/usr/bin/env bash
set -euo pipefail

ts() { date +"%Y-%m-%dT%H:%M:%S"; }
root_dir() { cd "$(dirname "$0")/.." && pwd -P; }

parse_env_port() {
  # $1 is var name (API_PORT/WEB_PORT)
  local name="$1"
  local val=""
  if [[ -f .env ]]; then
    # Read last assignment for the key and trim whitespace/CRLF
    val="$(grep -E "^${name}=" .env | tail -n1 | cut -d= -f2- | tr -d '\r' | xargs)" || true
  fi
  echo "$val"
}

start_api() {
  local port="$1"
  local log="run-api.log" pidf="run-api.pid"
  echo "[$(ts)] starting API on :$port"
  ASPNETCORE_URLS="http://localhost:${port}" DOTNET_ROLL_FORWARD=Major nohup dotnet run --project src/EsgAsAService.Api --no-build >"$log" 2>&1 & echo $! > "$pidf"
}

start_web() {
  local http_port="$1" https_port="$2"
  local log="run-web.log" pidf="run-web.pid"
  echo "[$(ts)] starting Web on http:${http_port} https:${https_port}"
  ASPNETCORE_URLS="http://localhost:${http_port};https://localhost:${https_port}" \
  ASPNETCORE_HTTPS_PORT="${https_port}" \
  DOTNET_ROLL_FORWARD=Major \
    nohup dotnet run --project src/EsgAsAService.Web --no-build >"$log" 2>&1 & echo $! > "$pidf"
}

stop_if_running() {
  local pidf="$1"
  if [[ -f "$pidf" ]]; then
    local pid
    pid="$(cat "$pidf" 2>/dev/null || true)"
    if [[ -n "${pid:-}" ]] && kill -0 "$pid" 2>/dev/null; then
      echo "[$(ts)] stopping PID $pid ($pidf)"
      kill "$pid" 2>/dev/null || true
      sleep 1
      kill -9 "$pid" 2>/dev/null || true
    fi
    rm -f "$pidf"
  fi
}

run_apps() {
  local api_port web_port
  api_port="${API_PORT:-}"; web_port="${WEB_PORT:-}"
  [[ -z "$api_port" ]] && api_port="$(parse_env_port API_PORT)"
  [[ -z "$web_port" ]] && web_port="$(parse_env_port WEB_PORT)"
  [[ -z "$api_port" ]] && api_port=5198
  [[ -z "$web_port" ]] && web_port=5097
  local web_https_port
  web_https_port="${WEB_PORT_HTTPS:-}"
  [[ -z "$web_https_port" ]] && web_https_port="$(parse_env_port WEB_PORT_HTTPS)"
  [[ -z "$web_https_port" ]] && web_https_port=7036

  stop_if_running run-api.pid
  stop_if_running run-web.pid

  start_api "$api_port"
  start_web "$web_port" "$web_https_port"

  echo "[$(ts)] running…"
  echo "  API: http://localhost:${api_port} (logs: run-api.log)"
  echo "  Web: http://localhost:${web_port} and https://localhost:${web_https_port} (logs: run-web.log)"
  echo "Press Ctrl+C to stop both."

  trap 'echo; echo "[$(ts)] stopping apps"; stop_if_running run-api.pid; stop_if_running run-web.pid; exit 0' INT TERM
  # Wait on both PIDs; if one exits, we exit
  local api_pid web_pid
  api_pid="$(cat run-api.pid)"
  web_pid="$(cat run-web.pid)"
  while true; do
    if ! kill -0 "$api_pid" 2>/dev/null; then echo "[$(ts)] API exited"; break; fi
    if ! kill -0 "$web_pid" 2>/dev/null; then echo "[$(ts)] Web exited"; break; fi
    sleep 1
  done
  stop_if_running run-api.pid
  stop_if_running run-web.pid
}

main() {
  local root run_flag=${1:-}
  root="$(root_dir)"
  cd "$root"

  echo "[$(ts)] ESG build+test started in $root"
  export DOTNET_ROLL_FORWARD=Major

  echo "[$(ts)] dotnet --info (short)"; dotnet --version || true

  echo "[$(ts)] restore"; dotnet restore

  echo "[$(ts)] build (Debug)"; dotnet build -c Debug

  echo "[$(ts)] test (Debug, no-build)"; dotnet test -c Debug --no-build

  if [[ "$run_flag" == "--run" ]]; then
    run_apps
  else
    echo "[$(ts)] done ✅ (use '--run' to start API + Web)"
  fi
}

main "$@"
