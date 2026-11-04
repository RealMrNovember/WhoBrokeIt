# P0 raporu (Emir 001) — ara durum

Dal: `p0/stability` (kurtarma noktası `91cb6b4`). Yayın yok: `main`, `latest.json`, kurucu ve Kale dağıtımı değişmedi.
Yerel test sürümü: 0.4.5 (yalnızca Doku'daki test derlemeleri; üretimde 0.4.4).

Doğrulama düzeyleri: **U** uygulandı · **D** derlendi · **E** entegrasyon testinde (gerçek sunucu/istemci süreçleri, Docker) · **O** gerçek oyunda elle/oyun penceresinde.

## Görev 1 — NotServerException
- **Kök neden:** `NetworkSpawnManager.GetPlayerNetworkObject(clientId)` yalnızca sunucuda başka istemcinin oyuncusunu bulabilir. İstemcide, başka biri araç sürerken `RideableVehicle`/`Forklift.LateUpdate` her karede bunu çağırıp hata fırlatıyordu (senin günlüğünde 6061 kez, 0.4.2).
- **Düzeltme:** `PlayerNet.ByOwner(clientId)`: her makinede çalışan, önbellekli arama (`FindObjectsByType`). Dosyalar: `Player/PlayerNet.cs`, `Vehicles/Forklift.cs`, `Vehicles/RideableVehicle.cs`.
- **Testler:** iki gerçek istemci + sunucu (Docker, üretim imajıyla aynı kütüphaneler, ekransız): A forklifte biner, sürer, çatalı kaldırır, iner, sandığı alır, taşır, bırakır; B izler. Üç günlükte (A, B, sunucu) `Exception` taraması.
- **Sonuç:** 17/17 kontrol geçti; üç günlükte sıfır istisna; uzaktaki sürücü 33/33 örnekte araca bağlı (≤3 m); forklift görüşü A/B ortalama fark 0,01–0,05 m, son konum farkı 0,00 m.
- **Düzey:** U, D, E. (O: gerçek pencerede henüz yok.)

## Görev 2 — Dünya senkronu / kayıp
- **Gözlem:** Editor istemcisi + Unity'nin yerleşik benzeticisi + **üretim** sunucusu: 45 sn sonra 1 nesne, oda kodu yok.
- **Elenen nedenler** (aynı Editor istemcisi, yerel sunucu): yerleşik benzetici (100 ms + 40 ms + %8) ✗ · 0,7 CPU sınırı ✗ · üretim sunucu imajı + Docker UDP NAT ✗ · yol MTU (senin hattında DF'li 1472 bayt geçiyor) ✗ · gerçek UDP kaybı: `bad` (≈100 ms RTT, ≈%8) 0,6/1,5 sn, `hell` (≈200 ms, ≈%23 gidiş-dönüş) 1,8/2,0 sn ✗ — hepsinde 45/45 nesne, A=B=sunucu sayıları.
- **Kök neden (bulundu):** üretimde bir oda boşalınca sunucu çıkar (`oda boşaldı`), Docker **aynı konteyneri** yeniden başlatır ve `/tmp` korunur. Önceki çalışmadan kalan Xvfb kilidi/soketi yüzünden yeni Xvfb açılmıyor; oyun `The selected window backend is (null)` / `No available video device` ile yarım açılıp **ölü sunucu** olarak kalıyor ("hazır" demiyor, süreç yaşıyor). İlk açılış sağlam, her oda sıfırlamasından sonra yuva bozuluyor. **Canlıda şu an:** game1 (14:23'ten beri), game2 (14:40), game3 (14:34), game4 (15:56) bozuk; yalnızca hiç sıfırlanmamış game5/game6 sağlam. İstemci bunlara bağlanınca dünya hiç gelmiyor (0 nesne) veya yarım geliyor.
- **Yerel yeniden üretim:** `tools/mp/restart-test.sh`: mevcut `run.sh` → yeniden başlatma sonrası **HAZIR OLMADI** (pencere "(null)"); düzeltilmiş `server/run.sh` → **HAZIR**. `zombie-client-test.sh`: ölü sunucuya bağlanan istemci 0 nesne; düzeltilmişte 0,4 sn / 45 nesne.
- **Düzeltme (yazıldı, yayınlanmadı):** `server/run.sh` — başlamadan önce eski `Xvfb` kilidi ve soketini temizler. **Üretime uygulanması onay bekliyor.**
- **Kalan:** matchmaker ölü yuvayı (kalp atışı kesilince) listeden çıkarıyor mu, ayrıca bakılacak. Bağlantı geri bildirimi (zaman aşımı) menüde 15 sn'de var; gerçek pencerede doğrulanmadı.
- **Düzey:** kök neden E (Docker'da yeniden üretildi); düzeltme U, D, E (yerel imajla); üretim: onay bekliyor.

## Görev 3 — İki istemcili otomatik test
- `tools/mp/` (`run.sh`, `entry.sh`, `analyze.py`, `Dockerfile`, `sim-test.sh`, `prod-like.sh`, `restart-test.sh`, `zombie-client-test.sh`), oyun tarafı `Net/TwoClientSmoke.cs`, sunucu `[MPSRV]` günlüğü.
- Profiller: `clean`, `wifi`, `mobile`, `bad`, `hell` (`tc netem`, her yönde).
- Denetlenenler: dünya hazır süresi ≤10 sn, 45 dünya nesnesi, A/B/sunucu nesne sayıları, forklift A=B, sandık A=B, uzak sürücü araca bağlı, taşıma ve bırakma görüldü, yer değiştirmeyen sandık, hayalet araç yok, final sürücü yok, üç günlükte istisna yok.
- **Sonuç:** `clean` 17/17 · `bad` 17/17 · `hell` 17/17.
- Kalan: aynı koşuda A'nın bir koşuda `(1,4; −8,0)` civarında 1,6 m havaya fırlaması (4 koşunun birinde, çarpışma adı günlüğü şimdi kayıtta, tekrar etmedi).

## Mevcut düzeltmeler (Bölüm 3)
| Düzeltme | Düzey | Durum |
|---|---|---|
| Ana menü "Oyundan çık" | U, D | gerçek oyunda doğrulanmadı |
| Omuz/kol ağırlık düzeltmesi | U, D | gerçek oyunda/ekran görüntüsünde doğrulanmadı |
| Ses (GameAudio) | U, D | **hiç dinlenmedi** (cihazsız ortam) |
| Ekran sarsıntısı | U, D | gerçek oyunda doğrulanmadı |
| Esc menüsü girdisi kapalı (0.4.4) | U, D | otomatik girdi testi yok |

## Açık işler
1. Üretim sunucu düzeltmesinin dağıtımı (onay gerekli).
2. Mevcut düzeltmelerin gerçek oyun penceresinde doğrulanması + otomatik menü/çıkış/Esc testleri.
3. Kalite kapıları: tek komutla `tools/mp/gate.sh` (yapılacak).
