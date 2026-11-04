#!/bin/bash
# Who Broke It? - Doku: sahneyi derler, Linux oyununu sanal ekranda (Xvfb) calistirip ekran goruntuleri alir.
# Sonuc: ~/whobrokeit/logs/shots.txt  ve  ~/whobrokeit/project/shots/*.png
set -u
cd ~/whobrokeit
RES=logs/shots.txt; : > $RES
IMG=unityci/editor:ubuntu-6000.0.32f1-windows-mono-3.2.2
P=$HOME/whobrokeit/project
fix() { docker run --rm -v $P:/p alpine chown -R "$(id -u):$(id -g)" /p; }
fix
if [ -z "${SKIP_BUILD:-}" ]; then ./unity.sh unity-editor -batchmode -nographics -projectPath /project/WhoBrokeIt -executeMethod ProjectSetup.BuildLinuxShots -quit -logFile - > logs/build-shots.log 2>&1; fi
echo "build exit=$?" | tee -a $RES
grep -E "^\[WBI\]" logs/build-shots.log | tee -a $RES
echo "son error CS satirlari:" | tee -a $RES
grep "error CS" logs/build-shots.log | sort -u | head -8 | cut -c1-200 | tee -a $RES
fix
rm -rf $P/shots; mkdir -p $P/shots
docker run --rm -v $P:/project -e XDG_RUNTIME_DIR=/tmp $IMG sh -c "Xvfb :99 -screen 0 1280x720x24 -nolisten tcp & sleep 3; DISPLAY=:99 /project/WhoBrokeIt/Builds/Server/wbi-server.x86_64 -screen-width 1280 -screen-height 720 -logFile /project/shots.log -shots /project/shots" > /dev/null 2>&1
echo "calistirma exit=$?" | tee -a $RES
grep -a -E "\[SHOT\]|Exception|NullReference" $P/shots.log | sort | uniq -c | head -30 | tee -a $RES
fix
ls $P/shots | tee -a $RES
echo DONE >> $RES
