#!/bin/bash
# Who Broke It? - Doku: sürümü derle ve yayınla (Windows build -> zip + latest.json).
# Kullanım (Doku'da): ~/whobrokeit/release.sh "Sürüm notu"      (sürüm numarası: project/WhoBrokeIt/VERSION)
# Sonuç: ~/whobrokeit/logs/release.txt
set -u
cd ~/whobrokeit
RES=logs/release.txt
: > "$RES"
docker run --rm -v "$HOME/whobrokeit/project:/p" alpine chown -R "$(id -u):$(id -g)" /p
echo "== Windows build (sürüm $(cat project/WhoBrokeIt/VERSION))" | tee -a "$RES"
./unity.sh unity-editor -batchmode -nographics -projectPath /project/WhoBrokeIt -executeMethod ProjectSetup.BuildWindows -quit -logFile - > logs/build.log 2>&1
echo "build exit=$?" | tee -a "$RES"
grep -E "^\[WBI\]" logs/build.log | tee -a "$RES"
docker run --rm -v "$HOME/whobrokeit/project:/p" alpine chown -R "$(id -u):$(id -g)" /p
if grep -q "Build result: Succeeded" logs/build.log; then
  ./publish.sh "${1:-Yeni sürüm}" 2>&1 | tee -a "$RES"
else
  echo "BUILD BASARISIZ - yayinlanmadi" | tee -a "$RES"
fi
echo DONE >> "$RES"
