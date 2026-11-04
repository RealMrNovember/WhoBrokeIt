#!/bin/bash
# Who Broke It? - Doku: yeniden baslatma sonrasi OLU DOGAN sunucuya bir Editor istemcisi baglanirsa ne gorur? (restart-test.sh imajlari gerekir)
#   ./zombie-client-test.sh old|fixed
set -u
V=${1:-old}
cd ~/whobrokeit
OUT=$HOME/whobrokeit/mp/$(date +%Y%m%d-%H%M%S)-zombie-$V
mkdir -p "$OUT"
docker rm -f wbi-z >/dev/null 2>&1
docker run -d --name wbi-z --memory 700m --cpus 0.7 wbi-restart-$V -server -port 7796 -slot z1 >/dev/null
for i in $(seq 1 25); do sleep 2; [ "$(docker logs wbi-z 2>&1 | grep -ac 'hazır, UDP')" -ge 1 ] && break; done
docker kill wbi-z >/dev/null; sleep 2; docker start wbi-z >/dev/null; sleep 25
IP=$(docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' wbi-z)
echo "[$V] sunucu $IP: hazir=$(docker logs wbi-z 2>&1 | grep -ac 'hazır, UDP') video_hatasi=$(docker logs wbi-z 2>&1 | grep -ac 'No available video device')"
docker run --rm --hostname hadal-unity --mac-address 02:42:ac:48:44:01 \
  -v hadal_unity_license:/root/.local/share/unity3d -v hadal_unity_config:/root/.config/unity3d -v wbi_unity_cache:/root/.cache/unity3d \
  -v $HOME/whobrokeit/project:/project -w /project unityci/editor:ubuntu-6000.0.32f1-windows-mono-3.2.2 \
  unity-editor -batchmode -nographics -projectPath /project/WhoBrokeIt -executeMethod SmokeEditorHook.Run -logFile - -smoke twoB $IP 7796 > "$OUT/editor.log" 2>&1
docker rm -f wbi-z >/dev/null 2>&1
docker run --rm -v "$HOME/whobrokeit/project:/p" alpine chown -R "$(id -u):$(id -g)" /p
grep -a "\[MP\]" "$OUT/editor.log" | grep -aE "verbindung|world ready|FAIL" | head -4 | cut -c1-200
grep -aE "Disconnect|timeout|Timed out|Connection" "$OUT/editor.log" | head -3 | cut -c1-200
grep -a "\[MP\] B t=" "$OUT/editor.log" | tail -1 | cut -c1-160
