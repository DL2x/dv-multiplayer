#!/usr/bin/env bash
#
# setup-dedicated-linux.sh
# One-time (idempotent) preparation of a Linux host to run the Derail Valley
# Multiplayer dedicated server headless under Wine. Safe to re-run.
#
# Steps:
#   1. Install OS packages (wine, Xvfb, Mesa software GL, winetricks, fonts).
#   2. Create a dedicated 64-bit Wine prefix.
#   3. Cap Wine's reported video memory so texture streaming stays bounded
#      (software GL keeps textures in system RAM; uncapped Wine reports a huge
#      fake VRAM and the game never evicts -> OOM).
#   4. Force minimum in-game graphics in every player Preferences profile.
#   5. Link the Unity save location (AppData/LocalLow/Altfuture) to the real
#      saves directory so the world/config/logs live where you expect.
#
# Run as the service user (NOT root). It uses sudo only for apt.

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
MOD_DIR="$(cd -- "$SCRIPT_DIR/.." && pwd)"
GAME_DIR="${GAME_DIR:-$(cd -- "$MOD_DIR/../.." && pwd)}"
# saves/Altfuture sits next to the game dir by default: <root>/game and <root>/saves
SAVES_ALTFUTURE="${SAVES_ALTFUTURE:-$(cd -- "$GAME_DIR/.." && pwd)/saves/Altfuture}"

export WINEPREFIX="${WINEPREFIX:-$HOME/.dv-wine}"
export WINEARCH=win64
export WINEDEBUG="${WINEDEBUG:--all}"
VRAM_MB="${VRAM_MB:-2048}"

echo "== Derail Valley dedicated server — Linux setup =="
echo "   GAME_DIR=$GAME_DIR"
echo "   SAVES_ALTFUTURE=$SAVES_ALTFUTURE"
echo "   WINEPREFIX=$WINEPREFIX  VRAM_MB=$VRAM_MB"
echo

### 1. Packages -------------------------------------------------------------
if command -v apt-get >/dev/null; then
  echo "-- installing packages (sudo apt-get) --"
  sudo dpkg --add-architecture i386 || true
  sudo apt-get update -y
  sudo apt-get install -y --no-install-recommends \
    wine wine64 xvfb winetricks \
    libgl1-mesa-dri mesa-vulkan-drivers libosmesa6 \
    fonts-liberation fonts-dejavu-core || true
else
  echo "!! Non-apt distro: install equivalents of wine, xvfb, mesa software GL, winetricks, fonts yourself." >&2
fi

### 2. Wine prefix ----------------------------------------------------------
echo "-- creating wine prefix --"
if [[ ! -f "$WINEPREFIX/system.reg" ]]; then
  WINEDLLOVERRIDES="mscoree=d;mshtml=d" wineboot --init
  # wait for wineserver to settle
  wineserver -w || true
fi

### 3. Cap reported VRAM ----------------------------------------------------
echo "-- capping Wine VideoMemorySize=${VRAM_MB} MB --"
wine reg add "HKCU\\Software\\Wine\\Direct3D" /v VideoMemorySize /t REG_SZ /d "$VRAM_MB" /f
wineserver -w || true

### 4. Minimum graphics in player preferences -------------------------------
echo "-- forcing minimum graphics in player Preferences --"
PREF_DIR="$SAVES_ALTFUTURE/Derail Valley/Preferences"
if [[ -d "$PREF_DIR" ]]; then
  shopt -s nullglob
  for INI in "$PREF_DIR"/*.ini; do
    [[ -f "$INI.orig" ]] || cp "$INI" "$INI.orig"
    python3 - "$INI" <<'PY'
import sys, re
p = sys.argv[1]
desired = {
 "AnisotropicFiltering":"0","ShadowsQualityIndex":"0","TerrainLightingQualityIndex":"0",
 "ReflectionQualityIndex":"0","RainQualityIndex":"0","VegetationQualityIndex":"0",
 "AntiAliasingForwardLevelsIndex":"0","AntiAliasingDeferredLevelsIndex":"0","DetailLevel":"0",
 "LightingQualityIndex":"0","PostProcessing":"False","MotionBlur":"False",
 "AmbientOcclusionQualityIndex":"0","ScreenResolutionWidth":"1280","ScreenResolutionHeight":"720",
 "TextureStreamingEnabled":"True","TextureStreamingMemoryBudget":"0.3",
}
lines = open(p).read().splitlines()
out, insec = [], False
for ln in lines:
    s = ln.strip()
    if s.startswith("[") and s.endswith("]"):
        insec = (s == "[Non-VR_Graphics]")
        out.append(ln); continue
    if insec:
        m = re.match(r"^(\s*)([A-Za-z]+)(\s*=\s*).*$", ln)
        if m and m.group(2) in desired:
            out.append(f"{m.group(1)}{m.group(2)}{m.group(3)}{desired[m.group(2)]}"); continue
    out.append(ln)
open(p, "w").write("\n".join(out) + "\n")
print("   set:", p)
PY
  done
else
  echo "   (no Preferences dir yet at $PREF_DIR — it will be created on first run; re-run this script afterwards, or lower graphics in-game once.)"
fi

### 5. Link Unity save location -> real saves -------------------------------
echo "-- linking AppData/LocalLow/Altfuture -> saves --"
LL="$WINEPREFIX/drive_c/users/$USER/AppData/LocalLow"
mkdir -p "$LL"
if [[ -e "$LL/Altfuture" && ! -L "$LL/Altfuture" ]]; then
  echo "   WARNING: $LL/Altfuture exists and is not a symlink; leaving it untouched." >&2
elif [[ ! -e "$LL/Altfuture" ]]; then
  ln -s "$SAVES_ALTFUTURE" "$LL/Altfuture"
  echo "   linked $LL/Altfuture -> $SAVES_ALTFUTURE"
else
  echo "   link already present: $(readlink "$LL/Altfuture")"
fi

echo
echo "== Setup complete. Start the server with: $SCRIPT_DIR/start-dedicated-linux.sh =="
