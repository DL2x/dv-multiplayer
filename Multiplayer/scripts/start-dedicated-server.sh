#!/usr/bin/env bash
set -euo pipefail

# ---- Dedicated server config ----
# Adjust these values before starting the server.
PORT="7777"
MAX_PLAYERS="8"
SERVER_NAME="Derail Valley Dedicated Server"
PASSWORD=""
DETAILS="Started by start-dedicated-server.sh"
VISIBILITY="Public"          # Public, Hidden, Private depending on the mod enum
PUBLIC_GAME="true"
HOST_TRANSPORT_MODE="Direct" # Direct = IP hosting
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

cat > "$CONFIG_FILE" <<JSON
{
  "serverName": "$SERVER_NAME",
  "password": "$PASSWORD",
  "details": "$DETAILS",
  "port": $PORT,
  "maxPlayers": $MAX_PLAYERS,
  "visibility": "$VISIBILITY",
  "publicGame": $PUBLIC_GAME,
  "hostTransportMode": "$HOST_TRANSPORT_MODE"
}
JSON

echo "Wrote dedicated server config: $CONFIG_FILE"
echo "Starting Derail Valley dedicated server on port $PORT ..."
cd "$GAME_DIR"
args=(-dvmp-dedicated -logFile "$LOG_FILE")
if [[ "$HEADLESS" == "true" ]]; then
  args=(-batchmode -nographics "${args[@]}")
fi
exec "$GAME_EXE" "${args[@]}"
