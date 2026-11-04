#!/bin/sh
# Sanal ekranı başlat, hazır olana kadar bekle, oyun sunucusunu onun altında çalıştır (parametreler compose'tan gelir).
# Not: 6 sunucu aynı anda açılınca sabit "sleep 2" yetmiyordu (video aygıtı yok hatası, yeniden başlama döngüsü).
Xvfb :99 -screen 0 64x64x24 -nolisten tcp >/dev/null 2>&1 &
i=0
while [ ! -e /tmp/.X11-unix/X99 ] && [ $i -lt 100 ]; do sleep 0.2; i=$((i+1)); done
sleep 1
exec /srv/game/wbi-server.x86_64 -batchmode -screen-width 64 -screen-height 64 -logFile - "$@"
