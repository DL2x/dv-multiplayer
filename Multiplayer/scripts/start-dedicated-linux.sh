#!/usr/bin/env bash
#
# start-dedicated-linux.sh
# Run the Derail Valley Multiplayer dedicated server HEADLESS on Linux,
# using Wine + a virtual X display (Xvfb). No GPU and no Steam client required.
#
# Why this works (see LINUX-DEDICATED-SERVER.md for the full story):
#   * The game is the Windows build; Wine runs it.
#   * Doorstop hijacks winhttp.dll to inject UnityModManager. Wine only loads
#     the game's bundled winhttp.dll if we force it native -> WINEDLLOVERRIDES.
#   * We do NOT use -batchmode/-nographics: on Linux those give Unity a
#     NullGfxDevice and the game crashes in its compute shaders. Instead we give
#     it a real (virtual) display via Xvfb and let Wine render through the Mesa
#     software GL stack (llvmpipe). That renders nothing useful but keeps Unity
#     happy and costs only CPU.
#   * Software GL has no VRAM, so all textures land in system RAM. We cap Wine's
#     reported VRAM (see setup-dedicated-linux.sh) and force minimum graphics so
#     the world fits in RAM instead of swapping.
#   * Steam: the Steam build's SteamApi_Init fails cleanly with no Steam client
#     present, and the mod's dedicated path skips the entitlement check. Keep
#     steam_api64.dll in place (deleting it makes the bootstrap throw).
#
# Configure via environment variables (all optional). Defaults shown.

set -uo pipefail

### ---- Dedicated server settings (written into dedicated-server.json) ----
PORT="${PORT:-7777}"
MAX_PLAYERS="${MAX_PLAYERS:-8}"
SERVER_NAME="${SERVER_NAME:-Derail Valley Dedicated Server}"
PASSWORD="${PASSWORD:-}"
DETAILS="${DETAILS:-Linux headless dedicated (wine)}"
VISIBILITY="${VISIBILITY:-Public}"
PUBLIC_GAME="${PUBLIC_GAME:-true}"          # false = do not register with the public lobby list
HOST_TRANSPORT_MODE="${HOST_TRANSPORT_MODE:-Direct}"  # Direct = LiteNetLib IP hosting

### ---- Paths ----
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
MOD_DIR="$(cd -- "$SCRIPT_DIR/.." && pwd)"
GAME_DIR="${GAME_DIR:-$(cd -- "$MOD_DIR/../.." && pwd)}"
GAME_EXE="${GAME_EXE:-$GAME_DIR/DerailValley.exe}"
CONFIG_FILE="$MOD_DIR/dedicated-server.json"

### ---- Wine / display runtime ----
export WINEPREFIX="${WINEPREFIX:-$HOME/.dv-wine}"
export WINEDEBUG="${WINEDEBUG:--all}"
# CRITICAL: make Wine load the game's native Doorstop winhttp.dll, otherwise
# UnityModManager never injects and -dvmp-dedicated does nothing.
export WINEDLLOVERRIDES="${WINEDLLOVERRIDES:-winhttp=n,b}"
DISPLAY_NUM="${DISPLAY_NUM:-99}"
export DISPLAY=":${DISPLAY_NUM}"
XVFB_RES="${XVFB_RES:-1280x720x24}"   # must be a resolution the game lists, else a (non-fatal) prefs exception

LOG_FILE="${LOG_FILE:-$GAME_DIR/dedicated-server.log}"

if [[ ! -f "$GAME_EXE" ]]; then
  echo "ERROR: game executable not found: $GAME_EXE" >&2
  echo "Set GAME_EXE=/path/to/DerailValley.exe" >&2
  exit 1
fi
if [[ ! -d "$WINEPREFIX" ]]; then
  echo "ERROR: wine prefix $WINEPREFIX missing. Run setup-dedicated-linux.sh first." >&2
  exit 1
fi

### ---- Refuse to start a second instance on the same display/port ----
if pgrep -f "DerailValley.exe -dvmp-dedicated" >/dev/null; then
  echo "ERROR: a dedicated server is already running (pgrep DerailValley.exe)." >&2
  exit 1
fi

### ---- Write the dedicated server config ----
json_escape() { local v="$1"; v="${v//\\/\\\\}"; v="${v//\"/\\\"}"; printf '%s' "$v"; }
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

### ---- Virtual display ----
OWN_XVFB=0
start_xvfb() {
  if pgrep -f "Xvfb :${DISPLAY_NUM} " >/dev/null; then return; fi
  Xvfb ":${DISPLAY_NUM}" -screen 0 "$XVFB_RES" -nolisten tcp >"/tmp/dv-xvfb-${DISPLAY_NUM}.log" 2>&1 &
  XVFB_PID=$!
  OWN_XVFB=1
  sleep 2
}

cleanup() {
  [[ -n "${WINE_PID:-}" ]] && kill "$WINE_PID" 2>/dev/null
  # Give Unity a moment to flush its save, then make sure wine is gone.
  for _ in 1 2 3 4 5 6 7 8 9 10; do pgrep -f "DerailValley.exe -dvmp-dedicated" >/dev/null || break; sleep 1; done
  pkill -f "DerailValley.exe -dvmp-dedicated" 2>/dev/null
  if [[ "$OWN_XVFB" == "1" ]]; then pkill -f "Xvfb :${DISPLAY_NUM} " 2>/dev/null; fi
}
trap cleanup EXIT INT TERM

start_xvfb

echo "Derail Valley dedicated server (Linux/wine)"
echo "  port=$PORT  players=$MAX_PLAYERS  transport=$HOST_TRANSPORT_MODE  public=$PUBLIC_GAME"
echo "  prefix=$WINEPREFIX  display=$DISPLAY  res=$XVFB_RES"
echo "  Unity log (raw): $WINEPREFIX/drive_c/users/$USER/AppData/LocalLow/Altfuture/Derail Valley/Player.log"
echo "  Console log: $LOG_FILE"
echo

cd "$GAME_DIR"
# -logFile - sends Unity's log to stdout (so journald/your terminal sees it).
# The full raw log is always also written to Player.log in the save dir.
wine "$GAME_EXE" -dvmp-dedicated -logFile - 2>&1 | tee "$LOG_FILE" &
WINE_PID=$!
wait "$WINE_PID"
