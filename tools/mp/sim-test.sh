#!/bin/bash
# Who Broke It? - Doku: "Editor istemcisi + Unity'nin yerlesik ag benzeticisi" deneyi, YEREL test sunucusuna karsi (uretime baglanmaz).
# Amac: kayipli agda dunya eslesmesi sorununun benzeticiye mi, sunucuya/aga mi ait oldugunu ayirmak.
#   ./sim-test.sh [lossy|clean]     (Linux sunucu build'i hazir olmali: run.sh once calismis olmali)
set -u
MODE=${1:-lossy}
cd ~/whobrokeit
PROJ=$HOME/whobrokeit/project/WhoBrokeIt
OUT=$HOME/whobrokeit/mp/$(date +%Y%m%d-%H%M%S)-editor-$MODE
mkdir -p "$OUT"
docker rm -f wbi-mp-server >/dev/null 2>&1; docker network rm wbi-mp >/dev/null 2>&1; docker network create wbi-mp >/dev/null
docker run -d --name wbi-mp-server --hostname server --network wbi-mp ${CPUS:+--cpus $CPUS --memory 700m} -e NETEM="" -v "$PROJ/Builds/Server:/srv/game:ro" -v "$HOME/whobrokeit/mp-tools:/mp:ro" -v "$OUT:/results" wbi-mptest sh /mp/entry.sh server -server -port 7790 -slot t1 >/dev/null
sleep 12
SIP=$(docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' wbi-mp-server)
echo "sunucu=$SIP mod=$MODE cikti=$OUT"
EXTRA=""; [ "$MODE" = "lossy" ] && EXTRA="lossy"
docker run --rm --hostname hadal-unity --mac-address 02:42:ac:48:44:01 --network wbi-mp \
  -v hadal_unity_license:/root/.local/share/unity3d -v hadal_unity_config:/root/.config/unity3d -v wbi_unity_cache:/root/.cache/unity3d \
  -v $HOME/whobrokeit/project:/project -w /project unityci/editor:ubuntu-6000.0.32f1-windows-mono-3.2.2 \
  unity-editor -batchmode -nographics -projectPath /project/WhoBrokeIt -executeMethod SmokeEditorHook.Run -logFile - -smoke twoB $SIP 7790 $EXTRA > "$OUT/editor.log" 2>&1
docker logs wbi-mp-server > "$OUT/server-docker.log" 2>&1
docker rm -f wbi-mp-server >/dev/null 2>&1; docker network rm wbi-mp >/dev/null 2>&1
docker run --rm -v "$HOME/whobrokeit/project:/p" alpine chown -R "$(id -u):$(id -g)" /p
echo "--- editor istemcisi:"
grep -a "\[MP\]" "$OUT/editor.log" | grep -aE "benzetici|world ready|FAIL|verbindung" | head -6
grep -a "\[MP\] B t=" "$OUT/editor.log" | tail -1 | cut -c1-200
