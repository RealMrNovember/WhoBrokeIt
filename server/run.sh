#!/bin/sh
# Sanal ekranı başlat, oyun sunucusunu onun altında çalıştır (parametreler compose'tan gelir).
Xvfb :99 -screen 0 64x64x24 -nolisten tcp >/dev/null 2>&1 &
sleep 2
exec /srv/game/wbi-server.x86_64 -batchmode -screen-width 64 -screen-height 64 -logFile - "$@"
