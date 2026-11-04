#!/bin/bash
# Who Broke It? - Doku: ana menunun ekran goruntusu (Linux oyunu sanal ekranda, pencere 1920x1080). Once shots.sh/BuildLinuxShots calismis olmali.
set -u
P=$HOME/whobrokeit/project
IMG=unityci/editor:ubuntu-6000.0.32f1-windows-mono-3.2.2
docker run --rm -v "$P:/p" alpine chown -R "$(id -u):$(id -g)" /p
rm -rf $P/shots_menu; mkdir -p $P/shots_menu
docker run --rm -v $P:/project -e XDG_RUNTIME_DIR=/tmp $IMG sh -c "Xvfb :99 -screen 0 1920x1080x24 -nolisten tcp & sleep 3; DISPLAY=:99 /project/WhoBrokeIt/Builds/Server/wbi-server.x86_64 -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 -logFile /project/shots_menu.log -menushot /project/shots_menu" > /dev/null 2>&1
echo "menu exit=$?"
docker run --rm -v "$P:/p" alpine chown -R "$(id -u):$(id -g)" /p
ls $P/shots_menu
