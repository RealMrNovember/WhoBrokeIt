#!/bin/sh
# Konteyner girisi: (varsa) netem uygula, sanal ekrani hazir olana kadar bekle, oyunu calistir. Kullanim: entry.sh <ad> <oyun argumanlari...>
[ -z "$NETEM" ] || tc qdisc add dev eth0 root netem $NETEM
Xvfb :99 -screen 0 64x64x24 -nolisten tcp >/dev/null 2>&1 &
i=0
while [ ! -e /tmp/.X11-unix/X99 ] && [ $i -lt 100 ]; do sleep 0.2; i=$((i+1)); done
NAME=$1; shift
exec /srv/game/wbi-server.x86_64 -batchmode -screen-width 64 -screen-height 64 -logFile /results/$NAME.log "$@"
