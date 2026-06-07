#!/usr/bin/env bash
#
# DEPRECATED. This script targeted a native Linux build (DerailValley.x86_64), which does
# not exist for Derail Valley. The supported Linux deployment runs the Windows build under
# Wine + Xvfb (no GPU, no Steam client). See LINUX-DEDICATED-SERVER.md.
#
# First-time setup:  ./setup-dedicated-linux.sh
# Start the server:  ./start-dedicated-linux.sh   (or the dv-dedicated.service systemd unit)
#
# Kept as a thin redirect so the old name still works.

exec "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)/start-dedicated-linux.sh" "$@"
