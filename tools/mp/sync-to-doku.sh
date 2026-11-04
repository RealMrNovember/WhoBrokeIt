#!/bin/bash
# Who Broke It? - laptop: kaynak kodunu Doku test agacina kopyalar (Doku ~/whobrokeit/project/WhoBrokeIt) ve arac betiklerini mp-tools'a.
#   tools/mp/sync-to-doku.sh            kopyala        |   tools/mp/sync-to-doku.sh --check   yalniz farki goster (satir sonlari yok sayilir)
# Silme yapmaz. Assets/ThirdParty (yapay zeka GLB'leri) ve Library kopyalanmaz.
set -eu
cd "$(dirname "$0")/../.."
if [ "${1:-}" = "--check" ]; then
  git ls-files unity/Assets/Scripts unity/Assets/Editor | grep '\.cs$' | while read -r f; do echo "$(tr -d '\r' < "$f" | sha1sum | cut -c1-12) ${f#unity/}"; done | sort -k2 > /tmp/wbi_local.txt
  ssh doku 'cd ~/whobrokeit/project/WhoBrokeIt && find Assets/Scripts Assets/Editor -name "*.cs" | sort | while read f; do echo "$(tr -d "\r" < $f | sha1sum | cut -c1-12) $f"; done' | sort -k2 > /tmp/wbi_doku.txt
  diff /tmp/wbi_local.txt /tmp/wbi_doku.txt && echo "AYNI: depo == Doku test agaci" ; exit 0
fi
tar czf - -C unity Assets/Scripts Assets/Editor Assets/Resources VERSION | ssh doku 'cd ~/whobrokeit/project/WhoBrokeIt && tar xzf -'
tar czf - -C tools/mp . | ssh doku 'mkdir -p ~/whobrokeit/mp-tools && cd ~/whobrokeit/mp-tools && tar xzf -'
# gercek server/run.sh, restart-test'te "fixed" olarak kullanilsin (tek dogruluk kaynagi; arac kopyasi sapamaz)
scp -q "$(dirname "$0")/../../server/run.sh" doku:~/whobrokeit/mp-tools/server-run-fixed.sh
ssh doku 'mkdir -p ~/whobrokeit/gate; chmod +x ~/whobrokeit/mp-tools/*.sh'
echo "kopyalandi."
