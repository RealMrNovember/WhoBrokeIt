#!/bin/bash
# Who Broke It? - Doku üzerinde: Windows build + Linux test build + iki konteynerlik çok oyunculu duman testi.
# Kullanım (Doku'da): ~/whobrokeit/smoke.sh   -> sonuç: ~/whobrokeit/logs/smoke-result.txt
set -u
cd ~/whobrokeit
LOG=logs/smoke-run.log
RES=logs/smoke-result.txt
IMAGE=unityci/editor:ubuntu-6000.0.32f1-windows-mono-3.2.2
PROJ=$HOME/whobrokeit/project/WhoBrokeIt
: > "$RES"

fix_owner() { docker run --rm -v "$HOME/whobrokeit/project:/p" alpine chown -R "$(id -u):$(id -g)" /p; }

if [ -z "${SKIP_WIN:-}" ]; then
echo "== 1) Windows build" | tee -a "$RES"
fix_owner
./unity.sh unity-editor -batchmode -nographics -projectPath /project/WhoBrokeIt -executeMethod ProjectSetup.BuildWindows -quit -logFile - > logs/build.log 2>&1
echo "windows build exit=$?" | tee -a "$RES"
grep -E "^\[WBI\]" logs/build.log | tee -a "$RES"
fi

echo "== 2) Linux test build" | tee -a "$RES"
fix_owner
./unity.sh unity-editor -batchmode -nographics -projectPath /project/WhoBrokeIt -executeMethod ProjectSetup.BuildLinuxSmoke -quit -logFile - > logs/build-linux.log 2>&1
echo "linux build exit=$?" | tee -a "$RES"
grep -E "^\[WBI\]" logs/build-linux.log | tee -a "$RES"
fix_owner

echo "== 3) Multiplayer smoke test" | tee -a "$RES"
docker rm -f wbi-smoke-host wbi-smoke-client >/dev/null 2>&1
docker network rm wbi-smoke >/dev/null 2>&1
docker network create wbi-smoke >/dev/null
BIN=/project/WhoBrokeIt/Builds/Linux/wbi.x86_64
docker run -d --name wbi-smoke-host --network wbi-smoke -v "$HOME/whobrokeit/project:/project" "$IMAGE" \
  "$BIN" -batchmode -nographics -logFile /project/WhoBrokeIt/Builds/Linux/host.log -smoke host >/dev/null
sleep 8
HOSTIP=$(docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' wbi-smoke-host)
echo "host ip: $HOSTIP" | tee -a "$RES"
docker run -d --name wbi-smoke-client --network wbi-smoke -v "$HOME/whobrokeit/project:/project" "$IMAGE" \
  "$BIN" -batchmode -nographics -logFile /project/WhoBrokeIt/Builds/Linux/client.log -smoke client "$HOSTIP" >/dev/null

for i in $(seq 1 60); do
  H=$(docker inspect -f '{{.State.Running}}' wbi-smoke-host 2>/dev/null)
  C=$(docker inspect -f '{{.State.Running}}' wbi-smoke-client 2>/dev/null)
  [ "$H" = "false" ] && [ "$C" = "false" ] && break
  sleep 5
done
echo "host exit code:   $(docker inspect -f '{{.State.ExitCode}}' wbi-smoke-host)" | tee -a "$RES"
echo "client exit code: $(docker inspect -f '{{.State.ExitCode}}' wbi-smoke-client)" | tee -a "$RES"
echo "--- host log ([SMOKE])" | tee -a "$RES"
grep -a "\[SMOKE\]" project/WhoBrokeIt/Builds/Linux/host.log | tee -a "$RES"
echo "--- client log ([SMOKE])" | tee -a "$RES"
grep -a "\[SMOKE\]" project/WhoBrokeIt/Builds/Linux/client.log | tee -a "$RES"
echo "--- hata/istisna (host+client, ilk 12)" | tee -a "$RES"
grep -a -hE "Exception|NullReference|Error|ERROR" project/WhoBrokeIt/Builds/Linux/host.log project/WhoBrokeIt/Builds/Linux/client.log | grep -av -iE "licens|CommandLine" | sort | uniq -c | sort -rn | head -12 | cut -c1-220 | tee -a "$RES"
docker rm -f wbi-smoke-host wbi-smoke-client >/dev/null 2>&1
docker network rm wbi-smoke >/dev/null 2>&1
echo DONE >> "$RES"
