#!/bin/bash
# Who Broke It? - Doku: son Windows build'ini yayınlar.
#   dist/WhoBrokeIt-<sürüm>.zip            oyun içi otomatik güncelleme paketi
#   dist/WhoBrokeIt-Setup-<sürüm>.exe      oyuncuların indirdiği kurulum sihirbazı (varsayılan: C:\Program Files\CiciByte\WhoBrokeIt)
#   dist/latest.json                       sürüm bildirimi
# Kullanım (Doku'da): ~/whobrokeit/publish.sh "Sürüm notu"
set -eu
cd ~/whobrokeit/project
VER=$(tr -d '\r\n ' < WhoBrokeIt/VERSION)
NOTES=${1:-"Yeni sürüm"}
mkdir -p dist
rm -rf dist/stage && mkdir -p dist/stage/WhoBrokeIt
cp -r WhoBrokeIt/Builds/Windows/. dist/stage/WhoBrokeIt/
rm -rf dist/stage/WhoBrokeIt/*DoNotShip*

# 1) güncelleme zip'i
ZIP="WhoBrokeIt-$VER.zip"
rm -f "dist/$ZIP"
( cd dist/stage && python3 -c "
import zipfile,os
z=zipfile.ZipFile('../$ZIP','w',zipfile.ZIP_DEFLATED,compresslevel=9)
for r,d,f in os.walk('WhoBrokeIt'):
    for n in f: z.write(os.path.join(r,n))
z.close()" )

# 2) kurulum sihirbazı (NSIS, Linux üzerinde derlenir)
SETUP="WhoBrokeIt-Setup-$VER.exe"
docker image inspect wbi-nsis >/dev/null 2>&1 || docker build -q -t wbi-nsis -f ~/whobrokeit/installer/Dockerfile ~/whobrokeit/installer
rm -f "dist/$SETUP"
docker run --rm -v "$HOME/whobrokeit/installer:/inst:ro" -v "$HOME/whobrokeit/project/dist:/work" -w /work wbi-nsis \
  -NOCD -V2 -DVERSION="$VER" -DSTAGE=/work/stage /inst/wbi.nsi
[ -f "dist/$SETUP" ] || { echo "KURULUM DOSYASI URETILEMEDI"; exit 1; }
rm -rf dist/stage

SHA=$(sha256sum "dist/$ZIP" | cut -d' ' -f1)
SIZE=$(stat -c %s "dist/$ZIP")
ISHA=$(sha256sum "dist/$SETUP" | cut -d' ' -f1)
ISIZE=$(stat -c %s "dist/$SETUP")
# JSON'u python üretir: notlardaki ters eğik çizgi/tırnak gibi karakterler dosyayı bozmasın (oyun ve site bunu okur)
VER="$VER" ZIP="$ZIP" SHA="$SHA" SIZE="$SIZE" SETUP="$SETUP" ISHA="$ISHA" ISIZE="$ISIZE" NOTES="$NOTES" python3 - <<'PY'
import json, os, datetime
e = os.environ
data = {
    "version": e["VER"],
    "url": "/builds/" + e["ZIP"], "sha256": e["SHA"], "size": int(e["SIZE"]),
    "installer": "/builds/" + e["SETUP"], "installerSha256": e["ISHA"], "installerSize": int(e["ISIZE"]),
    "date": datetime.datetime.utcnow().strftime("%Y-%m-%d"),
    "notes": e["NOTES"],
}
open("dist/latest.json.tmp", "w", encoding="utf-8").write(json.dumps(data, ensure_ascii=False))
PY
python3 -c "import json; json.load(open('dist/latest.json.tmp', encoding='utf-8')); print('latest.json gecerli')"
ln -sf "$SETUP" dist/WhoBrokeIt-Setup-latest.exe
mv -f dist/latest.json.tmp dist/latest.json
echo "yayinlandi: $SETUP ($ISIZE bayt, sha256=${ISHA:0:16}) + $ZIP ($SIZE bayt)"
