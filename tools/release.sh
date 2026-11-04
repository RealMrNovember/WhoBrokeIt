#!/bin/bash
# Who Broke It? - Doku: sürümü derle ve yayınla.
#   1) Windows build  -> dist/WhoBrokeIt-<sürüm>.zip + dist/latest.json   (oyuncular, otomatik güncelleme)
#   2) Linux dedicated server build -> dist/server/wbi-server.tar.gz       (Kale'deki oyun sunucuları çeker)
# Kullanım (Doku'da): ~/whobrokeit/release.sh "Sürüm notu"      (sürüm numarası: project/WhoBrokeIt/VERSION)
# Sonuç: ~/whobrokeit/logs/release.txt
set -u
cd ~/whobrokeit
RES=logs/release.txt
: > "$RES"
fix_owner() { docker run --rm -v "$HOME/whobrokeit/project:/p" alpine chown -R "$(id -u):$(id -g)" /p; }
fix_owner
echo "== Windows build (sürüm $(cat project/WhoBrokeIt/VERSION))" | tee -a "$RES"
./unity.sh unity-editor -batchmode -nographics -projectPath /project/WhoBrokeIt -executeMethod ProjectSetup.BuildWindows -quit -logFile - > logs/build.log 2>&1
echo "build exit=$?" | tee -a "$RES"
grep -E "^\[WBI\]" logs/build.log | tee -a "$RES"
fix_owner
if ! grep -q "Build result: Succeeded" logs/build.log; then
  echo "WINDOWS BUILD BASARISIZ - yayinlanmadi" | tee -a "$RES"; echo DONE >> "$RES"; exit 1
fi

echo "== Linux dedicated server build" | tee -a "$RES"
./unity.sh unity-editor -batchmode -nographics -projectPath /project/WhoBrokeIt -executeMethod ProjectSetup.BuildLinuxServer -quit -logFile - > logs/build-server.log 2>&1
echo "server build exit=$?" | tee -a "$RES"
grep -E "^\[WBI\]" logs/build-server.log | tee -a "$RES"
fix_owner
if grep -q "Server build: Succeeded" logs/build-server.log; then
  mkdir -p project/dist/server
  tar czf project/dist/server/wbi-server.tar.gz -C project/WhoBrokeIt/Builds/Server .
  ls -l project/dist/server/wbi-server.tar.gz | awk '{print "sunucu paketi: "$5" bayt"}' | tee -a "$RES"
else
  echo "SUNUCU BUILD BASARISIZ" | tee -a "$RES"
fi

./publish.sh "${1:-Yeni sürüm}" 2>&1 | tee -a "$RES"
echo DONE >> "$RES"
