#!/bin/bash
# Who Broke It? - Doku: Kit GLB'lerini IZOLE bir Unity projesinde (yalniz Assets/Art/Kit + KitImportValidator; oyun kodu YOK) iceri aktarir ve dogrular.
#   ~/whobrokeit/blender/tools/kit_unity_check.sh [staging-klasoru]        (varsayilan: ~/whobrokeit/blender/staging)
# Neden izole: oyun kodundaki yarim kalmis degisiklikler (baska calismalar) kit dogrulamasini bozmasin; .meta (GUID) dosyalari burada uretilir ve
# ~/whobrokeit/blender/kit-installed/ altina toplanir (depo ve ana Unity agaci AYNI .meta dosyalarini kullanir => GUID'ler sabit).
# Unity her zaman ~/whobrokeit/.unity.lock ile siralanir. URETIME DOKUNMAZ.
set -u
STG=${1:-$HOME/whobrokeit/blender/staging}
ROOT=$HOME/whobrokeit/blender
KP=$HOME/whobrokeit/kitproj
MAIN=$HOME/whobrokeit/project/WhoBrokeIt
OUT=$ROOT/kit-installed
IMG=unityci/editor:ubuntu-6000.0.32f1-windows-mono-3.2.2
LOCK=$HOME/whobrokeit/.unity.lock
fix_owner() { docker run --rm -v "$KP:/p" alpine chown -R "$(id -u):$(id -g)" /p >/dev/null 2>&1; }

mkdir -p "$KP/WhoBrokeIt/Assets/Editor" "$KP/WhoBrokeIt/Assets/Art/Kit" "$OUT"
if [ ! -d "$KP/WhoBrokeIt/Library" ]; then
  echo "izole proje kuruluyor (Library kopyasi, tek seferlik)..."
  flock "$LOCK" bash -c "cp -a '$MAIN/Library' '$MAIN/Packages' '$MAIN/ProjectSettings' '$KP/WhoBrokeIt/'"
fi
# var olan .meta dosyalarini (ana agactaki Kit) tohum olarak al: GUID'ler korunur
if [ -d "$MAIN/Assets/Art" ]; then cp -an "$MAIN/Assets/Art/." "$KP/WhoBrokeIt/Assets/Art/" 2>/dev/null; cp -an "$MAIN/Assets/Art.meta" "$KP/WhoBrokeIt/Assets/" 2>/dev/null; fi
if [ -d "$OUT/Assets_Art" ]; then cp -an "$OUT/Assets_Art/." "$KP/WhoBrokeIt/Assets/Art/" 2>/dev/null; cp -an "$OUT/Art.meta" "$KP/WhoBrokeIt/Assets/" 2>/dev/null; fi
cp -f "$ROOT/tools/KitImportValidator.cs" "$KP/WhoBrokeIt/Assets/Editor/KitImportValidator.cs"

KIT=$KP/WhoBrokeIt/Assets/Art/Kit
for f in "$STG"/*.glb; do
  n=$(basename "$f"); c=${n%%_*}
  mkdir -p "$KIT/$c"; cp -f "$f" "$KIT/$c/$n"
done
cp -f "$STG/WBI_Palette.png" "$KIT/" 2>/dev/null
cp -f "$STG/manifest.json" "$KIT/" 2>/dev/null
fix_owner
N=$(find "$KIT" -name '*.glb' | wc -l)
echo "Unity dogrulamasi: $N GLB"
flock "$LOCK" docker run --rm --hostname hadal-unity --mac-address 02:42:ac:48:44:01 \
  -v hadal_unity_license:/root/.local/share/unity3d -v hadal_unity_config:/root/.config/unity3d -v wbi_unity_cache:/root/.cache/unity3d \
  -v "$KP:/project" -w /project "$IMG" \
  unity-editor -batchmode -nographics -projectPath /project/WhoBrokeIt -executeMethod KitImportValidator.Run -quit -logFile - > "$ROOT/unity-kitcheck.log" 2>&1
URC=$?
fix_owner
grep -a "\[KITCHECK\]" "$ROOT/unity-kitcheck.log" | sed 's/.*\[KITCHECK\] //'
grep -aE "error CS" "$ROOT/unity-kitcheck.log" | sort -u | head -5
echo "unity cikis kodu=$URC"
# .meta + GLB + palet + manifest toplama (depoya/ana agaca kopyalanacak set)
rm -rf "$OUT/Assets_Art.new" 2>/dev/null
mkdir -p "$OUT/Assets_Art.new"
cp -a "$KP/WhoBrokeIt/Assets/Art/." "$OUT/Assets_Art.new/"
cp -a "$KP/WhoBrokeIt/Assets/Art.meta" "$OUT/Art.meta" 2>/dev/null
rm -rf "$OUT/Assets_Art"; mv "$OUT/Assets_Art.new" "$OUT/Assets_Art"
echo "toplandi: $OUT/Assets_Art ($(find "$OUT/Assets_Art" -name '*.glb' | wc -l) glb, $(find "$OUT/Assets_Art" -name '*.meta' | wc -l) meta)"
exit $URC
