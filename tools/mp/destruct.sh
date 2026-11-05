#!/bin/bash
# Who Broke It? - Doku: YIKIM senaryosu (yerel test sunucusu + B izleyici + A oyuncu + C gec katilan). URETIME DOKUNMAZ. 17 kontrollu iki istemci takimindan BAGIMSIZ.
#   ./destruct.sh [profil]     profil: clean | bad | hell  (varsayilan clean). Linux sunucu build'i hazir olmali (run.sh / gate.sh uretir).
# Akis: B baslangictan izler; A Crate_1'i firlatip kirar ("A BROKEN"); bu satir gorunce C KATILIR (gec katilan kirik durumu gormeli, enkaz tekrar oynamamali);
#       A uc oyuncu gorunce capla onarir; hepsi durumu (st=) ve enkaz sayaclarini yazar. Analiz: analyze-destruct.py
set -u
PROFILE=${1:-clean}
cd ~/whobrokeit
PROJ=${PROJ_DIR:-$HOME/whobrokeit/project/WhoBrokeIt}
BUILD_DIR=${BUILD_DIR:-Server}                       # Greenfield: BUILD_DIR=ServerGF
TOOLS_DIR=${TOOLS_DIR:-$HOME/whobrokeit/mp-tools}   # Greenfield kopyasi: TOOLS_DIR=$HOME/whobrokeit/mp-tools-g
case $PROFILE in
  clean) NETEM="";;
  bad)   NETEM="delay 50ms 20ms loss 4%";;
  hell)  NETEM="delay 100ms 40ms loss 12%";;
  *) echo "bilinmeyen profil: $PROFILE"; exit 2;;
esac
[ -x "$PROJ/Builds/$BUILD_DIR/wbi-server.x86_64" ] || { echo "Linux sunucu build'i yok"; exit 3; }
docker image inspect wbi-mptest >/dev/null 2>&1 || docker build -q -t wbi-mptest -f ~/whobrokeit/mp-tools/Dockerfile ~/whobrokeit/mp-tools
OUT=$HOME/whobrokeit/mp/$(date +%Y%m%d-%H%M%S)-destruct-$PROFILE
mkdir -p "$OUT"
docker rm -f wbi-mp-b-server wbi-mp-b-A wbi-mp-b-B wbi-mp-b-C >/dev/null 2>&1
docker network rm wbi-mp-b >/dev/null 2>&1; docker network create wbi-mp-b >/dev/null
PORT=7790
run() {
  local name=$1; shift
  docker run -d --name wbi-mp-b-$name --hostname $name --network wbi-mp-b --cap-add NET_ADMIN -e NETEM="$NETEM" \
    -v "$PROJ/Builds/$BUILD_DIR:/srv/game:ro" -v "$TOOLS_DIR:/mp:ro" -v "$OUT:/results" wbi-mptest sh /mp/entry.sh $name "$@" >/dev/null
}
run server -server -port $PORT -slot t1
sleep 12
SIP=$(docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' wbi-mp-b-server)
echo "yikim profil=$PROFILE netem='$NETEM' sunucu=$SIP:$PORT cikti=$OUT" | tee "$OUT/ozet.txt"
run B -smoke destB $SIP $PORT
sleep 3
run A -smoke destA $SIP $PORT
# C yalniz A "BROKEN" yazinca katilir (gec katilan)
CSTARTED=0
for i in $(seq 1 80); do
  sleep 2
  if [ $CSTARTED = 0 ] && grep -aq "A BROKEN" "$OUT/A.log" 2>/dev/null; then run C -smoke destC $SIP $PORT; CSTARTED=1; echo "C katildi (i=$i)" | tee -a "$OUT/ozet.txt"; fi
  [ "$(docker ps -q -f name=wbi-mp-b-A -f name=wbi-mp-b-B -f name=wbi-mp-b-C | wc -l)" = "0" ] && break
done
docker rm -f wbi-mp-b-server wbi-mp-b-A wbi-mp-b-B wbi-mp-b-C >/dev/null 2>&1
docker network rm wbi-mp-b >/dev/null 2>&1
python3 "$TOOLS_DIR/analyze-destruct.py" "$OUT" | tee -a "$OUT/ozet.txt"
