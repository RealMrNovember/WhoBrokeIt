#!/usr/bin/env bash
# Who Broke It? - Doku2 (RTX 5060, 8 GB): SDXL konsept -> arka plan temizleme -> TRELLIS ile dokulu .glb
# HADAL hattinin imajlarini/betiklerini KULLANIR ama onun depo dosyalarina dokunmaz (rosterimiz ~/hadal-gpu/wbi/ altinda).
#   nohup ~/whobrokeit/gpu/batch_wbi.sh wbi [id,id,...] > ~/hadal-gpu/out/wbi.log 2>&1 &
# Cikti: ~/hadal-gpu/out/<set>/concept/<id>{,_rgba}.png  ve  ~/hadal-gpu/out/<set>/3d/<id>/<id>_textured.glb
# Biten varliklar atlanir; yarim kalan ise kaldigi yerden devam eder. Ayni anda yalnizca bir agir is calistir.
set -uo pipefail
SET=${1:-wbi}
ONLY=${2:-}
ROSTER_HOST=$HOME/hadal-gpu/wbi/roster.json
ROSTER=/work/wbi/roster.json
HOST_OUT=$HOME/hadal-gpu/out/$SET
mkdir -p "$HOST_OUT/concept" "$HOST_OUT/3d"

ids=$(python3 -c "import json; r=json.load(open('$ROSTER_HOST')); o=set(filter(None,'$ONLY'.split(','))); print(' '.join(a['id'] for a in r['assets'] if not o or a['id'] in o))")

stage() {
  docker run --rm --gpus all --memory 12g --memory-swap 12g -v "$HOME/hadal-gpu:/work" hadal-gpu-cutout \
    python /work/repo/art/gpu/concept.py --batch $ROSTER --out /work/out/$SET/concept --only "$1" "$2" \
    2>&1 | grep -E '^\[concept\]|Error|error|Killed'
}

for id in $ids; do
  [ -f "$HOST_OUT/concept/$id.png" ] || { echo "[batch] $(date +%T) konsept $id"; stage "$id" --no-cutout; }
done
for id in $ids; do
  [ -f "$HOST_OUT/concept/${id}_rgba.png" ] || { echo "[batch] $(date +%T) arka plan temizleme $id"; stage "$id" --cutout-only; }
done

for id in $ids; do
  if [ -f "$HOST_OUT/3d/$id/${id}_textured.glb" ]; then echo "[batch] $id zaten hazir"; continue; fi
  [ -f "$HOST_OUT/concept/${id}_rgba.png" ] || { echo "[batch] $id icin konsept yok, atlandi"; continue; }
  echo "[batch] $(date +%T) trellis $id"
  "$HOME/hadal-gpu/repo/art/gpu/run_3d.sh" hadal-3d-trellis --model trellis --texture \
    --image "/work/out/$SET/concept/${id}_rgba.png" --out "/work/out/$SET/3d/$id" --name "$id" | tail -2
  if [ ! -f "$HOST_OUT/3d/$id/${id}_textured.glb" ]; then
    echo "[batch] $(date +%T) trellis $id tekrar, daha kaba (--max-voxels 9000)"
    "$HOME/hadal-gpu/repo/art/gpu/run_3d.sh" hadal-3d-trellis --model trellis --texture --max-voxels 9000 \
      --image "/work/out/$SET/concept/${id}_rgba.png" --out "/work/out/$SET/3d/$id" --name "$id" | tail -2
  fi
done
echo "[batch] $(date +%T) bitti"
