#!/bin/bash
# Who Broke It? - Doku: iki istemcili ag testi (yerel test sunucusu + A + B). URETIME DOKUNMAZ (yayin/kurulum/latest.json/Kale yok).
#   ./run.sh <profil> [build]     profil: clean | wifi | mobile | bad | hell      "build": once Linux sunucu build'ini yeniden uretir
# Her kapsayicinin cikisina netem uygulanir (iki yon): bad = 50ms+-20ms ve %4 kayip her yonde (~100 ms gidis-donus, ~%8 kayip).
# Sonuc: ~/whobrokeit/mp/<zaman>-<profil>/ (server.log, A.log, B.log, ozet.txt)
set -u
PROFILE=${1:-clean}; BUILD=${2:-}
cd ~/whobrokeit
PROJ=$HOME/whobrokeit/project/WhoBrokeIt
case $PROFILE in
  clean)  NETEM="";;
  wifi)   NETEM="delay 10ms 3ms loss 0.5%";;
  mobile) NETEM="delay 40ms 15ms loss 2%";;
  bad)    NETEM="delay 50ms 20ms loss 4%";;
  hell)   NETEM="delay 100ms 40ms loss 12%";;
  *) echo "bilinmeyen profil: $PROFILE"; exit 2;;
esac
if [ "$BUILD" = "build" ] || [ ! -x "$PROJ/Builds/Server/wbi-server.x86_64" ]; then
  docker run --rm -v "$HOME/whobrokeit/project:/p" alpine chown -R "$(id -u):$(id -g)" /p
  ./unity.sh unity-editor -batchmode -nographics -projectPath /project/WhoBrokeIt -executeMethod ProjectSetup.BuildLinuxServer -quit -logFile - > logs/build-mp.log 2>&1
  BRC=$?
  echo "build exit=$BRC"; grep -E "^\[WBI\]" logs/build-mp.log
  docker run --rm -v "$HOME/whobrokeit/project:/p" alpine chown -R "$(id -u):$(id -g)" /p
  if [ "$BRC" != "0" ]; then echo "DERLEME BASARISIZ - test calistirilmadi"; grep -aE "error CS" logs/build-mp.log | sort -u | head -8; exit 3; fi
fi
docker image inspect wbi-mptest >/dev/null 2>&1 || docker build -q -t wbi-mptest -f ~/whobrokeit/mp-tools/Dockerfile ~/whobrokeit/mp-tools

OUT=$HOME/whobrokeit/mp/$(date +%Y%m%d-%H%M%S)-$PROFILE
mkdir -p "$OUT"
docker rm -f wbi-mp-server wbi-mp-A wbi-mp-B >/dev/null 2>&1
docker network rm wbi-mp >/dev/null 2>&1; docker network create wbi-mp >/dev/null
PORT=7790
run() { # ad oyun-argumanlari...
  local name=$1; shift
  docker run -d --name wbi-mp-$name --hostname $name --network wbi-mp --cap-add NET_ADMIN -e NETEM="$NETEM" \
    -v "$PROJ/Builds/Server:/srv/game:ro" -v "$HOME/whobrokeit/mp-tools:/mp:ro" -v "$OUT:/results" wbi-mptest sh /mp/entry.sh $name "$@" >/dev/null
}
run server -server -port $PORT -slot t1
sleep 12
SIP=$(docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' wbi-mp-server)
echo "profil=$PROFILE netem='$NETEM' sunucu=$SIP:$PORT cikti=$OUT" | tee "$OUT/ozet.txt"
if [ -z "$SIP" ]; then echo "sunucu konteyneri calismiyor"; docker logs wbi-mp-server 2>&1 | tail -5; fi
run A -smoke twoA $SIP $PORT
sleep 3
run B -smoke twoB $SIP $PORT
for i in $(seq 1 60); do
  sleep 3
  [ "$(docker ps -q -f name=wbi-mp-A -f name=wbi-mp-B | wc -l)" = "0" ] && break
done
docker rm -f wbi-mp-server wbi-mp-A wbi-mp-B >/dev/null 2>&1
docker network rm wbi-mp >/dev/null 2>&1
python3 ~/whobrokeit/mp-tools/analyze.py "$OUT" | tee -a "$OUT/ozet.txt"
