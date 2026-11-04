#!/bin/bash
set -u
cd ~/whobrokeit
RES=logs/online-smoke.txt; : > $RES
docker run --rm -v "$HOME/whobrokeit/project:/p" alpine chown -R "$(id -u):$(id -g)" /p
./unity.sh unity-editor -batchmode -nographics -projectPath /project/WhoBrokeIt -executeMethod SmokeEditorHook.Run -logFile - -smoke online https://whobrokeit.cicibyte.com/api private ${1:-} > logs/online-smoke-full.log 2>&1
echo "editor exit=$?" | tee -a $RES
grep -a "\[SMOKE\]" logs/online-smoke-full.log | tee -a $RES
echo "--- hata" | tee -a $RES
grep -a -E "Exception|NullReference|error CS" logs/online-smoke-full.log | grep -av -iE "licens|ValidationExceptions|UnifiedRayTracing|Runtime/Exceptions" | sort | uniq -c | sort -rn | head -8 | cut -c1-220 | tee -a $RES
docker run --rm -v "$HOME/whobrokeit/project:/p" alpine chown -R "$(id -u):$(id -g)" /p
echo DONE >> $RES
