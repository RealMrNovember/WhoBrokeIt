#!/bin/bash
# Who Broke It? - Doku: Greenfield Meydani sahnesini derler, Linux oyununu sanal ekranda (Xvfb) calistirip tur ekran goruntulerini alir.
#   gfshots.sh [proje_klasoru]    varsayilan: ~/whobrokeit/project ; ozel kopya: ~/whobrokeit/project-g
# Sonuc: <proje>/shots_gf/*.jpg ve ~/whobrokeit/logs/gfshots.txt . Unity cagrilari ~/whobrokeit/.unity.lock ile siralanir.
set -u
cd ~/whobrokeit
PROJDIR=${1:-$HOME/whobrokeit/project}
NAME=$(basename "$PROJDIR")
RES=logs/gfshots.txt; : > $RES
IMG=unityci/editor:ubuntu-6000.0.32f1-windows-mono-3.2.2
fix() { docker run --rm -v $PROJDIR:/p alpine chown -R "$(id -u):$(id -g)" /p; }
unity() { # unity argumanlari... (proje: konteyner icinde /project)
  flock ~/whobrokeit/.unity.lock docker run --rm --name wbi-unity-$$ --hostname hadal-unity --mac-address 02:42:ac:48:44:01 \
    -v hadal_unity_license:/root/.local/share/unity3d -v hadal_unity_config:/root/.config/unity3d -v wbi_unity_cache:/root/.cache/unity3d \
    -v $PROJDIR:/project -w /project $IMG unity-editor "$@"
}
fix
unity -batchmode -nographics -projectPath /project/WhoBrokeIt -executeMethod ProjectSetup.BuildLinuxShotsGreenfield -quit -logFile - > logs/build-gfshots.log 2>&1
echo "build exit=$?" | tee -a $RES
grep -a -E "^\[WBI\]" logs/build-gfshots.log | tee -a $RES
echo "error CS:" | tee -a $RES; grep -a "error CS" logs/build-gfshots.log | sed 's/([0-9,]*)//' | sort -u | head -8 | cut -c1-200 | tee -a $RES
fix
rm -rf $PROJDIR/shots_gf; mkdir -p $PROJDIR/shots_gf
docker run --rm -v $PROJDIR:/project -e XDG_RUNTIME_DIR=/tmp $IMG sh -c "Xvfb :99 -screen 0 1280x720x24 -nolisten tcp & sleep 3; DISPLAY=:99 /project/WhoBrokeIt/Builds/ServerGF/wbi-server.x86_64 -screen-width 1280 -screen-height 720 -logFile /project/gfshots.log -gfshots /project/shots_gf" > /dev/null 2>&1
echo "calistirma exit=$?" | tee -a $RES
grep -a -E "\[SHOT\]|Exception|NullReference|\[WBI\]" $PROJDIR/gfshots.log | sort | uniq -c | head -30 | tee -a $RES
fix
ls $PROJDIR/shots_gf | tee -a $RES
echo DONE >> $RES
