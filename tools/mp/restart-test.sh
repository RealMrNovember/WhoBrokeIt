#!/bin/bash
# Who Broke It? - Doku: "oda sifirlama sonrasi sunucu yeniden baslatma" hatasini yerelde yeniden uretir ve duzeltmeyi sinar. Uretime dokunmaz.
#   ./restart-test.sh old|fixed     old = uretimdeki mevcut run.sh (tools/mp/prod-run.sh), fixed = server/run.sh (duzeltilmis)
# Yontem: konteyneri baslat, "hazir" gor, docker kill (oyun cikinca Xvfb de SIGKILL ile olur, kilit/soket artar), docker start, tekrar "hazir" bekle.
set -u
V=${1:-old}
cd ~/whobrokeit
CTX=$HOME/whobrokeit/mp-prod-$V
rm -rf $CTX; mkdir -p $CTX
cp -r $HOME/whobrokeit/project/WhoBrokeIt/Builds/Server $CTX/game
cp ~/whobrokeit/mp-tools/Dockerfile.game $CTX/
if [ "$V" = "fixed" ]; then cp ~/whobrokeit/mp-tools/server-run-fixed.sh $CTX/run.sh; else cp ~/whobrokeit/mp-tools/prod-run.sh $CTX/run.sh; fi
docker build -q -f $CTX/Dockerfile.game -t wbi-restart-$V $CTX >/dev/null
docker rm -f wbi-restart >/dev/null 2>&1
docker run -d --name wbi-restart --memory 700m --cpus 0.7 wbi-restart-$V -server -port 7796 -slot r1 >/dev/null
wait_ready() { for i in $(seq 1 25); do sleep 2; n=$(docker logs wbi-restart 2>&1 | grep -ac "hazır, UDP"); [ "$n" -ge "$1" ] && return 0; done; return 1; }
if wait_ready 1; then echo "[$V] 1. baslatma: HAZIR"; else echo "[$V] 1. baslatma: hazir olmadi"; fi
docker kill wbi-restart >/dev/null
sleep 2
docker start wbi-restart >/dev/null
if wait_ready 2; then echo "[$V] yeniden baslatma sonrasi: HAZIR (sorun yok)"; else echo "[$V] yeniden baslatma sonrasi: HAZIR OLMADI (hata yeniden uretildi)"; fi
echo "video aygiti hatasi sayisi: $(docker logs wbi-restart 2>&1 | grep -ac 'No available video device')   pencere: $(docker logs wbi-restart 2>&1 | grep -a 'selected window backend' | tr '\n' ' ')"
docker rm -f wbi-restart >/dev/null 2>&1
