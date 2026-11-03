#!/bin/bash
# Who Broke It? - Doku: son Windows build'ini sürümleyip yayınlar (zip + latest.json).
# Kullanım (Doku'da): ~/whobrokeit/publish.sh "Sürüm notu"
set -eu
cd ~/whobrokeit/project
VER=$(tr -d '\r\n ' < WhoBrokeIt/VERSION)
NOTES=${1:-"Yeni sürüm"}
mkdir -p dist
rm -rf dist/stage && mkdir -p dist/stage/WhoBrokeIt
cp -r WhoBrokeIt/Builds/Windows/. dist/stage/WhoBrokeIt/
rm -rf dist/stage/WhoBrokeIt/*DoNotShip*
ZIP="WhoBrokeIt-$VER.zip"
rm -f "dist/$ZIP"
( cd dist/stage && python3 -c "
import zipfile,os
z=zipfile.ZipFile('../$ZIP','w',zipfile.ZIP_DEFLATED,compresslevel=9)
for r,d,f in os.walk('WhoBrokeIt'):
    for n in f: z.write(os.path.join(r,n))
z.close()" )
rm -rf dist/stage
SHA=$(sha256sum "dist/$ZIP" | cut -d' ' -f1)
SIZE=$(stat -c %s "dist/$ZIP")
cat > dist/latest.json.tmp <<JSON
{"version":"$VER","url":"/builds/$ZIP","sha256":"$SHA","size":$SIZE,"date":"$(date -u +%F)","notes":"$NOTES"}
JSON
mv -f dist/latest.json.tmp dist/latest.json
echo "yayinlandi: $ZIP ($SIZE bayt) sha256=${SHA:0:16}"
