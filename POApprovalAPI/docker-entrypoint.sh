#!/bin/sh
# Render scans 0.0.0.0:$PORT (10000 unless the dashboard overrides it).
# The Dockerfile ENV cannot see that value, so bind here at startup.
PORT="${PORT:-10000}"
export ASPNETCORE_URLS="http://0.0.0.0:${PORT}"
export DISPLAY="${DISPLAY:-:99}"

echo "PO Approval API binding ${ASPNETCORE_URLS}"

# Virtual display for the GST portal browser. The API must still come up if Xvfb fails.
Xvfb "$DISPLAY" -screen 0 1920x1080x24 -ac +extension GLX +render -noreset >/tmp/xvfb.log 2>&1 &

exec dotnet POApprovalAPI.dll
