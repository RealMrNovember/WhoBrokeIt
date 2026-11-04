#!/bin/bash
# Kale: Doku'dan en son oyun sunucusu paketini çek, imajı derle, tüm çevrimiçi servisleri (yeniden) başlat.
# Kullanım (Kale'de): /srv/whobrokeit/online/deploy.sh
set -eu
cd /srv/whobrokeit/online
curl -fsS http://100.83.19.28:8099/server/wbi-server.tar.gz -o /tmp/wbi-server.tgz
rm -rf game && mkdir game && tar xzf /tmp/wbi-server.tgz -C game && rm -f /tmp/wbi-server.tgz
docker build -q -f Dockerfile.game -t wbi-server:latest .
docker compose -f compose.online.yml up -d --force-recreate
docker image prune -f >/dev/null
docker ps --format '{{.Names}} {{.Status}}' | grep wbi- || true
