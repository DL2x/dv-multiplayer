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
# Hide noisy Unity rendering/headless messages that are expected with -nographics.
FILTER_RENDER_LOGS="${FILTER_RENDER_LOGS:-true}"
# Write only the filtered console output to disk. Set false for zero server-log writes.
WRITE_FILTERED_LOG="${WRITE_FILTERED_LOG:-true}"

# ---- Paths ----
# Default: script is in <Derail Valley>/Mods/Multiplayer/scripts.
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
MOD_DIR="$(cd -- "$SCRIPT_DIR/.." && pwd)"
GAME_DIR="$(cd -- "$MOD_DIR/../.." && pwd)"
GAME_EXE="${GAME_EXE:-$GAME_DIR/DerailValley.x86_64}"
# This is now a small FILTERED log, not Unity's raw log.
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

args=(-dvmp-dedicated -logFile -)
if [[ "$HEADLESS" == "true" ]]; then
  args=(-batchmode -nographics "${args[@]}")
fi

echo "Wrote dedicated server config: $CONFIG_FILE"
echo "Starting Derail Valley dedicated server on port $PORT ..."
echo "Unity raw log: disabled (using -logFile -)"
if [[ "$WRITE_FILTERED_LOG" == "true" ]]; then
  echo "Filtered log file: $LOG_FILE"
  : > "$LOG_FILE"
fi
echo "Press Ctrl+C to stop the server."

cd "$GAME_DIR"

filter_log_stream() {
  local suppress_stack=0
  local suppress_resolution_list=0
  local skip_next_filename=0
  while IFS= read -r line; do
    if [[ "$FILTER_RENDER_LOGS" == "true" ]]; then
      if [[ $skip_next_filename -eq 1 && "$line" == "(Filename:"* ]]; then
        skip_next_filename=0
        continue
      fi
      skip_next_filename=0

      if [[ $suppress_stack -eq 1 ]]; then
        [[ -z "$line" ]] && suppress_stack=0
        continue
      fi

      if [[ $suppress_resolution_list -eq 1 ]]; then
        if [[ "$line" =~ ^[0-9]+[[:space:]]x[[:space:]][0-9]+[[:space:]]*$ || "$line" == "(Filename:"* || -z "$line" ]]; then
          [[ -z "$line" ]] && suppress_resolution_list=0
          continue
        fi
        suppress_resolution_list=0
      fi

      case "$line" in
        "Supported resolutions:"*)
          suppress_resolution_list=1
          skip_next_filename=1
          continue
          ;;
        "Mono path"*|"Mono config path"*|"Initialize engine version:"*|"[Subsystems] Discovering subsystems"*|"Forcing GfxDevice:"*|"GfxDevice:"*|"NullGfxDevice:"*|"    Version:"*|"    Renderer:"*|"    Vendor:"*|"Begin MonoManager ReloadAssembly"*|\
        "[Manager] Injection"*|"[Manager] Initialize"*|"[Manager] Version:"*|"[Manager] OS:"*|"[Manager] Net Framework:"*|"[Manager] Unity Engine:"*|"[Manager] Game:"*|"[Manager] IsSupportOnSession"*|"[Manager] Mods path:"*|"[Manager] Parsing mods"*|"[Manager] Reading file"*|"[Manager] Sorting mods"*|"[Manager] Loading mods"*|"[Manager] FINISH"*|"[Manager] Checking updates"*|\
        "- Completed reload"*|"UnloadTime:"*|"Unloading "*|"Total: "*|"Texture streaming is enabled"*|"[MemoryMonitoring]"*|\
        *"Microsoft Media Foundation video decoding to texture disabled:"*|\
        *"Fallback handler could not load library"*|\
        *"WARNING: Shader Unsupported:"*|\
        *"WARNING: Shader Did you use #pragma only_renderers"*|\
        *"Shader '"*"uses "*" texture parameters, more than the "*" supported by the current graphics device."*|\
        *"RenderTexture.Create failed: format unsupported for random writes"*|\
        *"Kernel 'MergeInstancedIndirectBuffers' not found"*|\
        *"Shader 'Oculus/OVRMRCameraFrameLit': fallback shader 'Alpha-Diffuse' not found"*|\
        *"The referenced script"*|\
        *"A scripted object"*|\
        *"Did you #ifdef UNITY_EDITOR"*|\
        *"Duplicate save file UID"*|\
        *"SaveLoadController session is null"*|\
        *"RefreshData fallback to empty lists"*|\
        *"Wrote game preferences configuration:"*|\
        *"Wrote file: Users"*|\
        *"Unable to find style 'miniButton'"*|\
        "Creating "*" singleton instance"*|\
        *"doesn't implement AllowAutoCreate method"*|\
        "[Globals] fetching default config"*|\
        "Build version:"*|"Mod managers:"*|"Build destination:"*|"Build timestamp:"*|"Build number:"*|"Build type:"*|"Build GUID:"*|"App version:"*|"App identifier:"*|"Build tags:"*|"OS:"*|"CPU:"*|"CPU freq:"*|"RAM:"*|"GPU:"*|"GPU vendor:"*|"GPU memory:"*|"GeForce NOW:"*|"Steam Deck:"*|"Log timestamp:"*|\
        "Command line args:"*|*"DerailValley.exe"|"-batchmode"|"-nographics"|"-dvmp-dedicated"|"-logFile"|\
        "/"*"dedicated-server.log"|"C:"*"dedicated-server.log"|\
        "C:\\Program Files"*|"[LocalizationLoader]"*|"Checking for save imports"*|"No new save imports"*|"Save importing phase done"*|"Check for unsaved difficulties"*|"Unsaved difficulties phase done"*|"ManualDataLoader:"*|"VR is not enabled"*|"UIMenuController proceeding"*|"Recalculating DVObjectModel caches"*|"Using ScenarioCRUD path"*|"RequestUnload on scene"*|"[SceneSwitcher]"*|"[bootstrap]"*|"[MainMenuMusicFadeout]"*|\
        "[Loading] loading start game data"*|"[Loading] initializing vegetation"*|"[Loading] initializing terrains"*|"[Loading] loading railway layout"*)
          skip_next_filename=1
          continue
          ;;
        *"ArgumentException: Kernel 'MergeInstancedIndirectBuffers' not found."*|\
        *"Failed to load user from directory"*)
          suppress_stack=1
          continue
          ;;
      esac
    fi
    printf '%s\n' "$line"
  done
}

if [[ "$WRITE_FILTERED_LOG" == "true" ]]; then
  "$GAME_EXE" "${args[@]}" 2>&1 | filter_log_stream | tee -a "$LOG_FILE"
  exit "${PIPESTATUS[0]}"
else
  "$GAME_EXE" "${args[@]}" 2>&1 | filter_log_stream
  exit "${PIPESTATUS[0]}"
fi
