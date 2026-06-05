#!/usr/bin/env bash
set -euo pipefail

# ---- Dedicated server config ----
# Adjust these values before starting the server.
PORT="${PORT:-7777}"
MAX_PLAYERS="${MAX_PLAYERS:-8}"
SERVER_NAME="${SERVER_NAME:-Derail Valley Dedicated Server}"
PASSWORD="${PASSWORD:-}"
DETAILS="${DETAILS:-Started by start-dedicated-server.sh}"
VISIBILITY="${VISIBILITY:-Public}"          # Public, Hidden, Private depending on the mod enum
PUBLIC_GAME="${PUBLIC_GAME:-true}"
HOST_TRANSPORT_MODE="${HOST_TRANSPORT_MODE:-Direct}" # Direct = IP hosting
# Set HEADLESS=false for debugging with a visible window/menu.
HEADLESS="${HEADLESS:-true}"

# ---- Paths ----
# Default: script is in <Derail Valley>/Mods/Multiplayer/scripts.
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
MOD_DIR="$(cd -- "$SCRIPT_DIR/.." && pwd)"
GAME_DIR="$(cd -- "$MOD_DIR/../.." && pwd)"
GAME_EXE="${GAME_EXE:-$GAME_DIR/DerailValley.x86_64}"
LOG_FILE="${LOG_FILE:-$GAME_DIR/dedicated-server.log}"
CONFIG_FILE="$MOD_DIR/dedicated-server.json"

if [[ ! -x "$GAME_EXE" ]]; then
  echo "Game executable not found or not executable: $GAME_EXE"
  echo "Set GAME_EXE=/path/to/DerailValley.x86_64 before running this script."
  exit 1
fi

json_escape() {
  local value="$1"
  value="${value//\\/\\\\}"
  value="${value//\"/\\\"}"
  value="${value//$'\n'/\\n}"
  value="${value//$'\r'/}"
  printf '%s' "$value"
}

cat > "$CONFIG_FILE" <<JSON
{
  "serverName": "$(json_escape "$SERVER_NAME")",
  "password": "$(json_escape "$PASSWORD")",
  "details": "$(json_escape "$DETAILS")",
  "port": $PORT,
  "maxPlayers": $MAX_PLAYERS,
  "visibility": "$(json_escape "$VISIBILITY")",
  "publicGame": $PUBLIC_GAME,
  "hostTransportMode": "$(json_escape "$HOST_TRANSPORT_MODE")"
}
JSON

args=(-dvmp-dedicated -logFile "$LOG_FILE")
if [[ "$HEADLESS" == "true" ]]; then
  args=(-batchmode -nographics "${args[@]}")
fi

echo "Wrote dedicated server config: $CONFIG_FILE"
echo "Starting Derail Valley dedicated server on port $PORT ..."
echo "Log file: $LOG_FILE"
echo "Press Ctrl+C to stop the server."

cd "$GAME_DIR"
: > "$LOG_FILE"
"$GAME_EXE" "${args[@]}" &
SERVER_PID=$!
TAIL_PID=""

cleanup() {
  local code=$?
  trap - INT TERM EXIT
  if [[ -n "${TAIL_PID:-}" ]] && kill -0 "$TAIL_PID" 2>/dev/null; then
    kill "$TAIL_PID" 2>/dev/null || true
  fi
  if kill -0 "$SERVER_PID" 2>/dev/null; then
    echo
    echo "Stopping Derail Valley dedicated server ..."
    kill "$SERVER_PID" 2>/dev/null || true
    # Give Unity a moment to shut down gracefully, then force it.
    for _ in {1..20}; do
      kill -0 "$SERVER_PID" 2>/dev/null || break
      sleep 0.25
    done
    kill -9 "$SERVER_PID" 2>/dev/null || true
    wait "$SERVER_PID" 2>/dev/null || true
  fi
  exit "$code"
}
trap cleanup INT TERM EXIT

tail -n +1 -F "$LOG_FILE" &
TAIL_PID=$!

wait "$SERVER_PID"
EXIT_CODE=$?
if [[ -n "${TAIL_PID:-}" ]] && kill -0 "$TAIL_PID" 2>/dev/null; then
  kill "$TAIL_PID" 2>/dev/null || true
  wait "$TAIL_PID" 2>/dev/null || true
fi
trap - EXIT
exit "$EXIT_CODE"
