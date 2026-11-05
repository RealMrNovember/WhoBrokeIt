#!/bin/bash
# Who Broke It? - Doku: Windows istemcisini SADECE derler (yayinlamaz, surum atlatmaz, dagitmaz).
#   winbuild.sh [proje_klasoru]   varsayilan: ~/whobrokeit/project-g
set -u
cd ~/whobrokeit
PROJDIR=${1:-$HOME/whobrokeit/project-g}
RES=logs/winbuild.txt; : > $RES
IMG=unityci/editor:ubuntu-6000.0.32f1-windows-mono-3.2.2
fix() { docker run --rm -v $PROJDIR:/p alpine chown -R "$(id -u):$(id -g)" /p; }
fix
rm -rf $PROJDIR/WhoBrokeIt/Builds/Windows
flock ~/whobrokeit/.unity.lock docker run --rm --name wbi-unity-win-$$ --hostname hadal-unity --mac-address 02:42:ac:48:44:01 \
  -v hadal_unity_license:/root/.local/share/unity3d -v hadal_unity_config:/root/.config/unity3d -v wbi_unity_cache:/root/.cache/unity3d \
  -v $PROJDIR:/project -w /project $IMG unity-editor -batchmode -nographics -projectPath /project/WhoBrokeIt -executeMethod ProjectSetup.BuildWindows -quit -logFile - > logs/build-win.log 2>&1
echo "build exit=$?" | tee -a $RES
grep -a -E "^\[WBI\]" logs/build-win.log | tee -a $RES
grep -a "error CS" logs/build-win.log | sed 's/([0-9,]*)//' | sort -u | head -8 | cut -c1-200 | tee -a $RES
fix
if grep -aq "Build result: Succeeded" logs/build-win.log; then
  V=$(cat $PROJDIR/WhoBrokeIt/VERSION)
  du -sh $PROJDIR/WhoBrokeIt/Builds/Windows | tee -a $RES
else
  echo "WINDOWS BUILD BASARISIZ" | tee -a $RES
fi
echo DONE >> $RES
