#!/bin/bash
# Who Broke It? - KALITE KAPISI (Doku). Tek komut, tekrarlanabilir. URETIME DOKUNMAZ (yalnizca yerel Docker aglari/konteynerleri wbi-mp*, wbi-restart*).
#   ~/whobrokeit/mp-tools/gate.sh [hizli]        "hizli": yalniz derleme + sunucu hazirlik + clean profili
# Adimlar: 1) Unity derlemesi (Linux sunucu)  2) sunucu hazirlik (baslat, hazir gor, oldur, yeniden baslat, tekrar hazir gor; server/run.sh Xvfb temizligi)
#          3) iki istemcili test: clean, bad, hell (her biri 17 kontrol)  4) istisna taramasi (tum gunlukler)  5) rapor
# Sonuc: ~/whobrokeit/gate/<zaman>/report.md + result.json ; cikis kodu 0 = GECTI, 1 = KALDI, 3 = derleme basarisiz
# NOT: 4 oyunculu mevcut test takimina 8 oyuncu istegi KONMAZ; olcekleme icin docs/OLCEKLEME_TEST_PLANI.md.
set -u
MODE=${1:-tam}
EXPECTED_CHECKS=${EXPECTED_CHECKS:-17}
cd ~/whobrokeit
TS=$(date +%Y%m%d-%H%M%S)
OUT=$HOME/whobrokeit/gate/$TS
mkdir -p "$OUT"
PROJ=$HOME/whobrokeit/project/WhoBrokeIt
RES=()          # "ad|GECTI/KALDI|ayrinti"
FAIL=0
add() { RES+=("$1|$2|$3"); [ "$2" = "GECTI" ] || FAIL=1; printf '%-46s %-6s %s\n' "$1" "$2" "$3" | tee -a "$OUT/console.txt"; }
fix_owner() { docker run --rm -v "$HOME/whobrokeit/project:/p" alpine chown -R "$(id -u):$(id -g)" /p >/dev/null 2>&1; }
echo "kalite kapisi $TS  surum=$(cat $PROJ/VERSION)  mod=$MODE" | tee "$OUT/console.txt"

# 0) kaynak denetimi: server/run.sh duzeltmesi (Xvfb kilidi) kodda var mi
if grep -q "rm -f /tmp/.X99-lock" ~/whobrokeit/mp-tools/server-run-fixed.sh; then add "0 run.sh Xvfb temizligi (arac kopyasi)" GECTI ""; else add "0 run.sh Xvfb temizligi (arac kopyasi)" KALDI "server-run-fixed.sh eski"; fi

# 1) Unity derlemesi
fix_owner
./unity.sh unity-editor -batchmode -nographics -projectPath /project/WhoBrokeIt -executeMethod ProjectSetup.BuildLinuxServer -quit -logFile - > "$OUT/build.log" 2>&1
BRC=$?
fix_owner
ERRS=$(grep -aE "error CS" "$OUT/build.log" | sed 's/([0-9,]*)//' | sort -u | head -5 | tr '\n' ';')
WARN=$(grep -aE "warning CS" "$OUT/build.log" | sed 's/([0-9,]*)//' | sort -u | wc -l)
if [ "$BRC" = "0" ] && grep -aq "Server build: Succeeded" "$OUT/build.log" && [ -z "$ERRS" ]; then
  add "1 Unity derlemesi (Linux sunucu)" GECTI "uyari turu=$WARN"
else
  add "1 Unity derlemesi (Linux sunucu)" KALDI "cikis=$BRC ${ERRS:0:300}"
fi
if [ "$FAIL" != "0" ]; then echo "derleme basarisiz: testler calistirilmadi" | tee -a "$OUT/console.txt"; fi

# 2) sunucu hazirlik (oda sifirlama sonrasi yeniden baslatma dahil)
if [ "$FAIL" = "0" ]; then
  RT=$(bash ~/whobrokeit/mp-tools/restart-test.sh fixed 2>&1); echo "$RT" > "$OUT/restart-test.txt"
  if echo "$RT" | grep -q "1. baslatma: HAZIR" && echo "$RT" | grep -q "yeniden baslatma sonrasi: HAZIR (sorun yok)"; then
    add "2 sunucu hazirlik (baslat, olum, yeniden baslat)" GECTI ""
  else
    add "2 sunucu hazirlik (baslat, olum, yeniden baslat)" KALDI "$(echo "$RT" | tr '\n' ' ' | cut -c1-240)"
  fi
fi

# 3) iki istemcili testler
PROFILES="clean bad hell"; [ "$MODE" = "hizli" ] && PROFILES="clean"
if [ "$FAIL" = "0" ]; then
  for P in $PROFILES; do
    bash ~/whobrokeit/mp-tools/run.sh $P > "$OUT/mp-$P.txt" 2>&1
    LINE=$(grep -aE "^SONUC:" "$OUT/mp-$P.txt" | tail -1)
    D=$(grep -aoE "cikti=[^ ]+" "$OUT/mp-$P.txt" | head -1 | cut -d= -f2)
    N=$(echo "$LINE" | grep -aoE "\([0-9]+/[0-9]+\)" | tr -d '()')
    PASS=${N%/*}; TOT=${N#*/}
    if [ -z "$N" ]; then add "3 iki istemci: $P" KALDI "sonuc satiri yok ($OUT/mp-$P.txt)"
    elif [ "$TOT" != "$EXPECTED_CHECKS" ]; then add "3 iki istemci: $P" KALDI "kontrol sayisi $TOT (beklenen $EXPECTED_CHECKS): test takimi degismis"
    elif echo "$LINE" | grep -q GECTI; then add "3 iki istemci: $P" GECTI "$PASS/$TOT  kayit=$D"
    else add "3 iki istemci: $P" KALDI "$PASS/$TOT  kayit=$D  $(grep -a '^KALDI' $D/ozet.txt 2>/dev/null | head -3 | tr '\n' ';')"; fi
    [ -n "$D" ] && cp "$D/ozet.txt" "$OUT/ozet-$P.txt" 2>/dev/null
    echo "$D" >> "$OUT/dirs.txt"
  done
fi

# 4) istisna taramasi: derleme gunlugu + tum test gunlukleri
if [ "$FAIL" = "0" ] && [ -f "$OUT/dirs.txt" ]; then
  EX=0; DET=""
  while read -r D; do
    [ -d "$D" ] || continue
    for f in "$D"/*.log; do
      c=$(grep -acE "NotServerException|NullReferenceException|InvalidOperationException|ArgumentException|IndexOutOfRange|MissingReference|Unhandled Exception" "$f")
      if [ "$c" != "0" ]; then EX=$((EX+c)); DET="$DET $(basename $D)/$(basename $f)=$c"; fi
    done
  done < "$OUT/dirs.txt"
  if [ "$EX" = "0" ]; then add "4 istisna taramasi (sunucu, A, B gunlukleri)" GECTI "0"; else add "4 istisna taramasi (sunucu, A, B gunlukleri)" KALDI "$EX:$DET"; fi
fi

# 5) rapor
VER=$(cat $PROJ/VERSION); RC=$FAIL
{
  echo "# Kalite kapisi $TS"
  echo; echo "Surum: $VER · mod: $MODE · sonuc: **$([ $RC = 0 ] && echo GECTI || echo KALDI)**"
  echo; echo "| Kontrol | Sonuc | Ayrinti |"; echo "|---|---|---|"
  for r in "${RES[@]}"; do IFS='|' read -r a b c <<< "$r"; echo "| $a | $b | $c |"; done
  echo; echo "Kayitlar: $OUT"
} > "$OUT/report.md"
python3 - "$OUT" "$VER" "$RC" "${RES[@]}" <<'PY'
import json,sys
out,ver,rc=sys.argv[1:4]
rows=[dict(zip(("check","result","detail"),r.split("|",2))) for r in sys.argv[4:]]
json.dump({"version":ver,"passed":rc=="0","checks":rows},open(out+"/result.json","w"),ensure_ascii=False,indent=1)
PY
printf '%s\t%s\t%s\n' "$TS" "$VER" "$([ $RC = 0 ] && echo GECTI || echo KALDI)" >> ~/whobrokeit/gate/history.tsv
echo; echo "RAPOR: $OUT/report.md"; echo "KALITE KAPISI: $([ $RC = 0 ] && echo GECTI || echo KALDI)"
if [ "$RC" = "0" ]; then exit 0; fi
grep -q "Unity derlemesi.*KALDI" "$OUT/console.txt" && exit 3
exit 1
