#!/bin/bash
# Who Broke It? - Doku: TEMIZ kaynaktan yayin. Belirli bir git commit'in unity/ klasorunu relproj'e acar (calisma agacindaki yarim isler, deneysel dosyalar GIRMEZ).
#   ~/whobrokeit/release-clean.sh build              relproj'u hazirla, Windows + Linux sunucu build'i, ~/whobrokeit/relproj/out/ altina paketle
#   ~/whobrokeit/release-clean.sh publish "not"      paketleri project/dist'e koy ve latest.json'u guncelle (CANLI olur: oyuncular otomatik guncellenir)
# Oncesi (laptop):  git archive <commit> unity | ssh doku 'cat > ~/whobrokeit/rel-src.tar'
# Unity cagrilari ~/whobrokeit/.unity.lock ile siralanir. Kale'ye DOKUNMAZ (deploy ayri: Kale'de /srv/whobrokeit/online/deploy.sh).
set -eu
cd ~/whobrokeit
PHASE=${1:-build}
REL=$HOME/whobrokeit/relproj
IMG=unityci/editor:ubuntu-6000.0.32f1-windows-mono-3.2.2
fix() { docker run --rm -v "$REL:/p" alpine chown -R "$(id -u):$(id -g)" /p >/dev/null 2>&1; }
unity() {
  flock ~/whobrokeit/.unity.lock docker run --rm --name wbi-rel-$$ --hostname hadal-unity --mac-address 02:42:ac:48:44:01 \
    -v hadal_unity_license:/root/.local/share/unity3d -v hadal_unity_config:/root/.config/unity3d -v wbi_unity_cache:/root/.cache/unity3d \
    -v $REL:/project -w /project $IMG unity-editor "$@"
}
if [ "$PHASE" = "build" ]; then
  [ -f rel-src.tar ] || { echo "rel-src.tar yok"; exit 2; }
  if [ ! -d "$REL/WhoBrokeIt/Library" ]; then   # ilk kez: Library onbellegi icin mevcut projeyi kopyala (dist ve ekran goruntuleri haric)
    mkdir -p "$REL"; (cd project && tar cf - --exclude=./dist --exclude='./shots*' --exclude='*.log' .) | (cd "$REL" && tar xf -)
  fi
  fix
  W=$REL/WhoBrokeIt
  rm -rf "$W/Assets/Scripts" "$W/Assets/Editor" "$W/Assets/Art" "$W/Assets/Scenes" "$W/Assets/Prefabs" "$W/Assets/Resources/Characters" "$W/Assets/Resources/Brand" "$W/Builds"
  rm -rf rel-src && mkdir rel-src && tar xf rel-src.tar -C rel-src
  cp -r rel-src/unity/Assets/Scripts rel-src/unity/Assets/Editor "$W/Assets/"
  mkdir -p "$W/Assets/Resources" && cp -r rel-src/unity/Assets/Resources/Brand "$W/Assets/Resources/"
  cp -r rel-src/unity/Assets/Art "$W/Assets/"   # Greenfield kit GLB (Blender)
  cp -r rel-src/unity/ProjectSettings/. "$W/ProjectSettings/"; cp rel-src/unity/Packages/manifest.json "$W/Packages/manifest.json"; cp rel-src/unity/VERSION "$W/VERSION"
  # dogrulama: Scripts + Editor, arsivle birebir ayni olmali (satir sonlari yok sayilir)
  h() { (cd "$1" && find Assets/Scripts Assets/Editor -name '*.cs' | sort | while read -r f; do echo "$(tr -d '\r' < "$f" | sha1sum | cut -c1-12) $f"; done); }
  if ! diff <(h rel-src/unity) <(h "$W") >/dev/null; then echo "KAYNAK ARSIVLE UYUSMUYOR"; exit 3; fi
  echo "kaynak dogrulandi: $(h "$W" | wc -l) .cs dosyasi arsivle ayni, surum $(cat $W/VERSION)"
  fix
  unity -batchmode -nographics -projectPath /project/WhoBrokeIt -executeMethod ProjectSetup.BuildWindows -quit -logFile - > logs/rel-build-win.log 2>&1 || true
  fix
  grep -a -E "^\[WBI\] Build result" logs/rel-build-win.log || { echo "WINDOWS BUILD BASARISIZ"; grep -a "error CS" logs/rel-build-win.log | sort -u | head -5; exit 4; }
  grep -aq "Build result: Succeeded" logs/rel-build-win.log || { echo "WINDOWS BUILD BASARISIZ"; exit 4; }
  unity -batchmode -nographics -projectPath /project/WhoBrokeIt -executeMethod ProjectSetup.BuildLinuxServer -quit -logFile - > logs/rel-build-srv.log 2>&1 || true
  fix
  grep -aq "Server build: Succeeded" logs/rel-build-srv.log || { echo "SUNUCU BUILD BASARISIZ"; exit 5; }
  mkdir -p "$REL/out" && tar czf "$REL/out/wbi-server.tar.gz" -C "$W/Builds/Server" .
  echo "build tamam: Windows + sunucu (sunucu paketi $(stat -c %s $REL/out/wbi-server.tar.gz) bayt)"
elif [ "$PHASE" = "publish" ]; then
  NOTES=${2:-"Yeni sürüm"}
  W=$REL/WhoBrokeIt
  # publish.sh'in kopyasi: Windows build'i relproj'dan, cikti project/dist'e (oyunculara servis edilen klasor)
  sed -e "s#cp -r WhoBrokeIt/Builds/Windows/. dist/stage/WhoBrokeIt/#cp -r $W/Builds/Windows/. dist/stage/WhoBrokeIt/#" -e "s#< WhoBrokeIt/VERSION#< $W/VERSION#" publish.sh > publish-rel.sh
  grep -q "$W/Builds/Windows" publish-rel.sh && grep -q "$W/VERSION" publish-rel.sh || { echo "publish-rel.sh uretilemedi"; exit 6; }
  mkdir -p project/dist/server && cp "$REL/out/wbi-server.tar.gz" project/dist/server/wbi-server.tar.gz
  bash publish-rel.sh "$NOTES"
else
  echo "kullanim: release-clean.sh build | publish \"not\""; exit 2
fi
