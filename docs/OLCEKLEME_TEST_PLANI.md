# Ölçekleme test planı: 4, 6 ve 8 oyuncu

Durum: **plan**. `MaxPlayers` hâlâ **4** (`ConnectionGate.MaxPlayers`; matchmaker `CAPACITY=4`). Bu plan çalıştırılıp sonuçlar kabul kriterlerini geçmeden değer artırılmaz. Mevcut 4 oyunculu kalite kapısına ([`tools/mp/gate.sh`](../tools/mp/gate.sh), 17 kontrol) 8 oyuncu isteği **eklenmez**; ölçekleme ayrı bir takımdır.

## Neden ayrı
Mevcut senaryo iki istemcilidir (A sürer/taşır, B gözler). 4–8 oyuncu; bant genişliği, sunucu CPU'su (0,7 çekirdek sınırı), ağ nesnesi sayısı (NetworkTransform), sahiplik yarışları ve kalabalık fizik gibi *başka* riskler getirir. Bunları karıştırmak hem kapıyı kırılgan yapar hem de sonucu anlamsız kılar.

## Test düzeneği (yeni, `tools/mp/scale/`)
- Aynı Docker + `tc netem` mantığı: 1 sunucu konteyneri + N istemci konteyneri (`-smoke bot <rol> <ip> <port>`). Bot rolleri: **yürüyen** (dünyayı dolaşır), **taşıyıcı** (nesne alır/bırakır/fırlatır), **sürücü** (forklift), **kaotik** (rastgele, vurma/itme), **boşta**.
- Sunucu kaynak sınırı üretimdeki gibi (`--cpus 0.7 --memory 700m`), ağ profilleri `clean / bad / hell`.
- Toplanan ölçümler (sunucu `[MPSRV]` satırının genişletilmişi, 5 sn'de bir): bağlı oyuncu, ağ nesnesi, **kare süresi p50/p95/p99**, **giden/gelen bayt/sn** (UTP istatistiği), fizik süresi, aktif rigidbody sayısı, GC, istemci başına RTT; istemcilerde: kare süresi, dünyayla uyum farkı (konum), kayma/zıplama olay sayısı.

## Senaryolar
| Kod | Oyuncu | İçerik | Süre |
|---|---|---|---|
| S4 | 4 | 1 sürücü, 2 taşıyıcı, 1 yürüyen | 5 dk |
| S6 | 6 | 1 sürücü, 3 taşıyıcı, 1 kaotik, 1 yürüyen | 5 dk |
| S8 | 8 | 2 sürücü (forklift + bisiklet), 3 taşıyıcı, 2 kaotik, 1 yürüyen | 5 dk |
| S8-churn | 8 | S8 + her 30 sn bir oyuncu çıkıp yeniden girer (yeniden bağlanma) | 10 dk |
| S8-heavy | 8 | S8 + yıkım olayları (yıkım sistemi hazır olunca) | 5 dk |
Her biri `clean`, `bad`, `hell` profillerinde; ayrıca **4 sunucuyu aynı anda** (Kale'deki gibi 6 konteyner) çalıştırma testi (yalnızca Doku'da, üretimde değil).

## Kabul kriterleri (kapı)
| Ölçüt | S4 | S6 | S8 |
|---|---|---|---|
| Sunucu kare süresi p95 | ≤ 20 ms | ≤ 25 ms | ≤ 30 ms (60 Hz hedefi için ≤ 16,6 ms p50) |
| Sunucu CPU (0,7 çekirdek sınırında) | ≤ %70 | ≤ %85 | ≤ %95 ve kare düşmesi yok |
| Oyuncu başına giden bant | ölç | ölç | toplam giden ≤ 1,5 Mbit/sn sunucu başına (ölçümle onaylanır) |
| Dünya uyumu (A=B=sunucu, nesne sayısı) | %100 | %100 | %100 |
| Forklift/sandık konumu farkı | ort < 1,5 m, son < 0,5 m | aynı | aynı |
| İstisna | 0 | 0 | 0 |
| Katılım süresi (dünya hazır) | ≤ 10 sn | ≤ 10 sn | ≤ 12 sn |
| `hell` profilinde bağlantı kopması | yok | yok | ≤ %5 oyuncu |
Eşikler **ilk ölçümden sonra** gerçek veriyle yeniden ayarlanabilir (kural: gevşetmek için gerekçe yazılır).

## Bilinen riskler / araştırılacaklar
- Her `NetworkTransform` + `NetworkRigidbody` her istemciye aktarılır; yıkım parçaları senkronlanmaz (tasarım gereği), ama 8 oyuncuda *taşınan/fırlatılan* nesneler artacak.
- 6 sunucu x 0,7 çekirdek aynı makinede; 8 oyuncuyla CPU yetmeyebilir → oyun sunucusu başına 1 çekirdek veya daha az sunucu (karar ölçümle).
- Matchmaker `CAPACITY` ve `ConnectionGate.MaxPlayers` birlikte değişmeli; eski istemciler 4 sanıyorsa arayüz/etiketler (sürüm uyumu zaten zorunlu).
- Ağ bütçesi: tick 30 Hz; NGO istatistiği ve kuyruk uyarıları izlenir.
- Fizik: kalabalıkta rigidbody sayısı ve uyku eşikleri; katman matrisi kısılması gerekebilir.

## Uygulama sırası
1. Bot rolleri + genişletilmiş `[MPSRV]` (oyun kodu; yalnızca `-smoke` modunda etkin).
2. `tools/mp/scale/run.sh <S4|S6|S8> <profil>` + analiz betiği (yukarıdaki eşikler).
3. Yalnızca ölç; MaxPlayers değişmez. Sonuç raporu `docs/OLCEKLEME_SONUCLARI.md`.
4. Kapıyı geçerse `MaxPlayers`/`CAPACITY` artışı **ayrı bir onayla** (üretim değişikliği olduğu için).
