#!/bin/bash
# Who Broke It? - Doku: URETIM SUNUCU IMAJININ (server/Dockerfile.game + run.sh) yerel kopyasi, Docker UDP port yayini (NAT) ve 0.7 CPU ile.
# Editor istemcisi (yerlesik benzetici acik) ona ana makinenin docker0 adresi uzerinden baglanir. Uretime dokunmaz.
#   ./prod-like.sh [lossy|clean]
set -u
MODE=${1:-lossy}
cd ~/whobrokeit
PROJ=$HOME/whobrokeit/project/WhoBrokeIt
CTX=$HOME/whobrokeit/mp-prod
rm -rf $CTX; mkdir -p $CTX
cp -r "$PROJ/Builds/Server" $CTX/game
cp ~/whobrokeit/mp-tools/Dockerfile.game ~/whobrokeit/mp-tools/prod-run.sh $CTX/
mv $CTX/prod-run.sh $CTX/run.sh
docker build -q -f $CTX/Dockerfile.game -t wbi-server-local $CTX
OUT=$HOME/whobrokeit/mp/$(date +%Y%m%d-%H%M%S)-prodlike-$MODE
mkdir -p "$OUT"
docker rm -f wbi-prodlike >/dev/null 2>&1
docker run -d --name wbi-prodlike --restart no --memory 700m --cpus 0.7 -p 7795:7795/udp wbi-server-local -server -port 7795 -slot p1 >/dev/null
sleep 14
echo "sunucu konteyneri: $(docker ps --format '{{.Status}}' -f name=wbi-prodlike)  cikti=$OUT"
EXTRA=""; [ "$MODE" = "lossy" ] && EXTRA="lossy"
docker run --rm --hostname hadal-unity --mac-address 02:42:ac:48:44:01 \
  -v hadal_unity_license:/root/.local/share/unity3d -v hadal_unity_config:/root/.config/unity3d -v wbi_unity_cache:/root/.cache/unity3d \
  -v $HOME/whobrokeit/project:/project -w /project unityci/editor:ubuntu-6000.0.32f1-windows-mono-3.2.2 \
  unity-editor -batchmode -nographics -projectPath /project/WhoBrokeIt -executeMethod SmokeEditorHook.Run -logFile - -smoke twoB 172.17.0.1 7795 $EXTRA > "$OUT/editor.log" 2>&1
docker logs wbi-prodlike > "$OUT/server-docker.log" 2>&1
docker rm -f wbi-prodlike >/dev/null 2>&1
docker run --rm -v "$HOME/whobrokeit/project:/p" alpine chown -R "$(id -u):$(id -g)" /p
echo "--- editor istemcisi:"
grep -a "\[MP\]" "$OUT/editor.log" | grep -aE "benzetici|world ready|FAIL|verbindung" | head -6
grep -a "\[MP\] B t=" "$OUT/editor.log" | tail -1 | cut -c1-200
echo "--- sunucu uyarilari:"
grep -aiE "netcode|exception|deferred" "$OUT/server-docker.log" | head -5 | cut -c1-200
