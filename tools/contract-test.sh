#!/bin/bash
# Who Broke It? - Doku: sozlesme sistemi senaryosu (host modu, Linux oyuncusu Xvfb altinda). Once gfshots.sh ile Greenfield Linux build'i uretilmis olmali.
#   contract-test.sh [proje_klasoru]      varsayilan ~/whobrokeit/project-g
set -u
PROJDIR=${1:-$HOME/whobrokeit/project-g}
OUT=$HOME/whobrokeit/contract-results/$(date +%Y%m%d-%H%M%S); mkdir -p "$OUT"
IMG=unityci/editor:ubuntu-6000.0.32f1-windows-mono-3.2.2
docker run --rm --name wbi-contract-$$ -v $PROJDIR:/project:ro -v $OUT:/results -e XDG_RUNTIME_DIR=/tmp $IMG sh -c "Xvfb :99 -screen 0 64x64x24 -nolisten tcp & sleep 3; DISPLAY=:99 /project/WhoBrokeIt/Builds/ServerGF/wbi-server.x86_64 -batchmode -screen-width 64 -screen-height 64 -logFile /results/contract.log -smoke contract" > /dev/null 2>&1
echo "cikis=$?  kayit=$OUT"
grep -a "\[CONTRACT\]" $OUT/contract.log | cut -c1-200
grep -a -c -E "Exception|NullReference" $OUT/contract.log | xargs echo "istisna satiri:"
