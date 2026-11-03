#!/bin/bash
# Who Broke It? - Doku: Unity Editor içinde batchmode PlayMode duman testi (host tarafı, tek süreç).
# Önkoşul: Workshop.unity sahnesi üretilmiş olmalı (BuildWindows bir kez çalışmış).
# Sonuç: ~/whobrokeit/logs/editor-smoke.txt
set -u
cd ~/whobrokeit
RES=logs/editor-smoke.txt
: > "$RES"
docker run --rm -v "$HOME/whobrokeit/project:/p" alpine chown -R "$(id -u):$(id -g)" /p
./unity.sh unity-editor -batchmode -nographics -projectPath /project/WhoBrokeIt -executeMethod SmokeEditorHook.Run -logFile - -smoke host1 > logs/editor-smoke-full.log 2>&1
echo "editor exit=$?" | tee -a "$RES"
grep -a "\[SMOKE\]" logs/editor-smoke-full.log | tee -a "$RES"
echo "--- hata/istisna" | tee -a "$RES"
grep -a -E "Exception|NullReference|error CS" logs/editor-smoke-full.log | grep -av -iE "licens|ValidationExceptions|UnifiedRayTracing|Runtime/Exceptions" | sort | uniq -c | sort -rn | head -12 | cut -c1-240 | tee -a "$RES"
docker run --rm -v "$HOME/whobrokeit/project:/p" alpine chown -R "$(id -u):$(id -g)" /p
echo DONE >> "$RES"
