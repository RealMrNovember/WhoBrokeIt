#!/bin/bash
# Who Broke It? - Doku: Blender kit hatti. Uret -> dogrula -> hazirla (-> Unity'ye kur ve Unity icinde dogrula).
#   ~/whobrokeit/blender/tools/build_kit.sh [--only a,b] [--no-preview] [--install]
# Sonuc: ~/whobrokeit/blender/out/<zaman>/ (report.md, report.json, glb/, preview_kit.png), gecenler ~/whobrokeit/blender/staging/
# --install: gecen GLB'leri Unity projesine (Assets/Art/Kit/<kategori>/) kopyalar ve Unity'de KitImportValidator'i calistirir.
# URETIME DOKUNMAZ. Mevcut dosyalari silmez (staging uzerine yazar, Unity'ye yalniz ayni adli kit dosyalarini yazar).
set -u
BL=${BLENDER:-$HOME/opt/blender/blender}
ROOT=$HOME/whobrokeit/blender
TOOLS=$ROOT/tools
ONLY=""; PREVIEW=1; INSTALL=0
while [ $# -gt 0 ]; do case $1 in --only) ONLY=$2; shift;; --no-preview) PREVIEW=0;; --install) INSTALL=1;; esac; shift; done
TS=$(date +%Y%m%d-%H%M%S); OUT=$ROOT/out/$TS
mkdir -p "$OUT/glb" "$OUT/selftest" "$ROOT/staging"
RC=0
say() { echo "$@" | tee -a "$OUT/console.txt"; }
say "kit hatti $TS  blender=$($BL -b --version 2>/dev/null | head -1)"

# 1) dogrulayici oz-sinavi: bilerek bozuk varliklar KALMALI (yoksa dogrulayici bozuktur)
$BL -b -P "$TOOLS/generate_kit.py" -- --out "$OUT" --selftest > "$OUT/selftest-gen.log" 2>&1
$BL -b -P "$TOOLS/validate_asset.py" -- "$OUT"/selftest/*.glb --report "$OUT/selftest.json" > "$OUT/selftest-validate.log" 2>&1
python3 - "$OUT/selftest.json" > "$OUT/selftest.txt" <<'PY'
import json, sys
want = {"prop_bad_flipped": "normal", "prop_crate_overbudget": "butce", "prop_crate_bigscale": "olcek",
        "prop_bad_material": "malzeme", "prop_bad_floating": "olcek"}
d = json.load(open(sys.argv[1]))
ok = True
got = {f["name"]: {c["group"] for c in f["checks"] if c["status"] == "FAIL"} for f in d["files"]}
for k, g in want.items():
    hit = g in got.get(k, set())
    ok &= hit
    print(("YAKALANDI " if hit else "KACIRILDI ") + k + " (beklenen: %s, bulunan: %s)" % (g, sorted(got.get(k, []))))
print("OZSINAV:", "GECTI" if ok else "KALDI")
sys.exit(0 if ok else 1)
PY
ST=$?
tee -a "$OUT/console.txt" < "$OUT/selftest.txt"
[ $ST -ne 0 ] && RC=1

# 2) uret
GARGS=(--out "$OUT/glb")
[ -n "$ONLY" ] && GARGS+=(--only "$ONLY")
$BL -b -P "$TOOLS/generate_kit.py" -- "${GARGS[@]}" > "$OUT/generate.log" 2>&1 || { say "URETIM HATASI (bkz. generate.log)"; RC=1; }
grep -a "^\[KIT\]" "$OUT/generate.log" | tee -a "$OUT/console.txt"

# 3) dogrula
$BL -b -P "$TOOLS/validate_asset.py" -- "$OUT"/glb/*.glb --report "$OUT/report.json" > "$OUT/validate.log" 2>&1
VRC=$?
grep -aE "^(PASS|FAIL|     |\[VALIDATE\])" "$OUT/validate.log" | tee -a "$OUT/console.txt"
[ $VRC -ne 0 ] && RC=1

# 4) gecenleri hazirla
python3 - "$OUT" "$ROOT/staging" <<'PY'
import json, os, shutil, sys
out, stg = sys.argv[1:3]
d = json.load(open(out + "/report.json"))
n = 0
for f in d["files"]:
    if f["passed"]:
        shutil.copy2(f["path"], stg)
        n += 1
for extra in ("WBI_Palette.png", "manifest.json"):
    if os.path.exists(out + "/glb/" + extra):
        shutil.copy2(out + "/glb/" + extra, stg)
print("[STAGING] %d varlik -> %s" % (n, stg))
PY

# 5) onizleme (gruplara bolunmus sayfalar: kucuk proplar ile 24 m'lik binalar ayni sayfada okunmaz)
if [ $PREVIEW -eq 1 ]; then
  python3 - "$OUT" > "$OUT/preview-groups.txt" <<'PY'
import json, re, sys
out = sys.argv[1]
d = json.load(open(out + "/report.json"))
groups = {"props": [], "modules": [], "buildings": [], "landmarks": [], "env": []}
for f in d["files"]:
    if not f["passed"]:
        continue
    n = f["name"]
    if n.startswith("env_"):
        g = "env"
    elif re.match(r"^(bld_clocktower|bld_watertower|prop_fountain)", n):
        g = "landmarks"
    elif re.match(r"^(bld_shopfront|bld_workshop|bld_warehouse|bld_shed|bld_house)", n):
        g = "buildings"
    elif n.startswith(("bld_", "fnc_")):
        g = "modules"
    else:
        g = "props"
    groups[g].append(f["path"])
for g, files in groups.items():
    if files:
        print(g + " " + " ".join(files))
PY
  while read -r G FILES; do
    if [ "$G" = "buildings" ] || [ "$G" = "landmarks" ]; then
      for F in $FILES; do
        N=$(basename "$F" .glb)
        if $BL -b -P "$TOOLS/render_preview.py" -- "$OUT/preview_one_$N.png" "$F" --single --samples 16 --width 1000 --height 640 > "$OUT/preview-$N.log" 2>&1; then
          say "[PREVIEW] $OUT/preview_one_$N.png"
        else
          say "onizleme cizilemedi: $N (bkz. preview-$N.log)"
        fi
      done
      continue
    fi
    ROW=20; W=1600; H=900
    [ "$G" = "buildings" ] && ROW=52
    [ "$G" = "landmarks" ] && ROW=34
    [ "$G" = "env" ] && ROW=60
    if $BL -b -P "$TOOLS/render_preview.py" -- "$OUT/preview_$G.png" $FILES --row $ROW --samples 16 --width $W --height $H > "$OUT/preview-$G.log" 2>&1; then
      say "[PREVIEW] $OUT/preview_$G.png"
    else
      say "onizleme cizilemedi: $G (bkz. preview-$G.log)"
    fi
  done < "$OUT/preview-groups.txt"
fi

# 6) Unity'ye kur + Unity'de dogrula
if [ $INSTALL -eq 1 ] && [ $RC -eq 0 ]; then
  while pgrep -f "[m]p-tools/gate.sh" >/dev/null 2>&1; do say "kalite kapisi calisiyor, Unity icin bekleniyor..."; sleep 20; done
  KIT=$HOME/whobrokeit/project/WhoBrokeIt/Assets/Art/Kit
  mkdir -p "$KIT"
  python3 - "$OUT" "$KIT" <<'PY'
import json, os, shutil, sys
out, kit = sys.argv[1:3]
for f in json.load(open(out + "/report.json"))["files"]:
    if f["passed"]:
        cat = f["name"].split("_")[0]
        os.makedirs(kit + "/" + cat, exist_ok=True)
        shutil.copy2(f["path"], kit + "/" + cat + "/")
if os.path.exists(out + "/glb/WBI_Palette.png"):
    shutil.copy2(out + "/glb/WBI_Palette.png", kit + "/")
PY
  cd ~/whobrokeit
  fix_owner() { docker run --rm -v "$HOME/whobrokeit/project:/p" alpine chown -R "$(id -u):$(id -g)" /p >/dev/null 2>&1; }
  fix_owner
  ./unity.sh unity-editor -batchmode -nographics -projectPath /project/WhoBrokeIt -executeMethod KitImportValidator.Run -quit -logFile - > "$OUT/unity-kitcheck.log" 2>&1
  URC=$?
  fix_owner
  grep -a "\[KITCHECK\]" "$OUT/unity-kitcheck.log" | tee -a "$OUT/console.txt"
  grep -aE "error CS" "$OUT/unity-kitcheck.log" | sort -u | head -5 | tee -a "$OUT/console.txt"
  say "unity cikis kodu=$URC"
  [ $URC -ne 0 ] && RC=1
fi

# 7) rapor
python3 - "$OUT" "$RC" "$INSTALL" <<'PY'
import json, sys
out, rc, inst = sys.argv[1], int(sys.argv[2]), sys.argv[3]
d = json.load(open(out + "/report.json"))
st = open(out + "/selftest.txt").read().strip().splitlines()[-1]
L = ["# Kit raporu " + out.split("/")[-1], "",
     "Sonuc: **%s** - %d/%d varlik gecti - %s" % ("GECTI" if rc == 0 else "KALDI", d["total"] - d["failed"], d["total"], st), "",
     "| Varlik | Ucgen | Boyut (x,y,z m) | Sonuc | Not |", "|---|---|---|---|---|"]
for f in d["files"]:
    notes = "; ".join("%s: %s" % (c["group"], c["detail"]) for c in f["checks"] if c["status"] != "PASS")
    dim = "%.2f, %.2f, %.2f" % tuple(f["dim"]) if f.get("dim") else "-"
    L.append("| %s | %s | %s | %s | %s |" % (f["name"], f.get("tris"), dim, "GECTI" if f["passed"] else "KALDI", notes))
try:
    lines = [l.split("[KITCHECK]")[1].strip() for l in open(out + "/unity-kitcheck.log", errors="ignore") if "[KITCHECK]" in l]
    L += ["", "## Unity ice aktarma dogrulamasi", "```"] + lines + ["```"]
except OSError:
    if inst == "1":
        L += ["", "Unity dogrulamasi calismadi."]
open(out + "/report.md", "w", encoding="utf-8").write("\n".join(L) + "\n")
PY
say "RAPOR: $OUT/report.md"
say "KIT HATTI: $([ $RC -eq 0 ] && echo GECTI || echo KALDI)"
exit $RC
