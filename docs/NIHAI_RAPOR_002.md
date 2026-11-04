# Emir 002 — nihai rapor (2026-11-04)

Kapsam: üretim sağlığı (salt okunur), karakter varlık yedeği, kalite kapısı, sanat yönü, Blender hattı, Greenfield Meydanı tasarımı. **Yapılmayanlar (talimat gereği):** üretim dağıtımı, yayın, sunucu yeniden başlatma, Kale'de değişiklik, silme, yıkıcı Git işlemi. Değişiklikler **commit edilmedi**.
Doğrulama düzeyleri: **U** uygulandı · **D** derlendi · **E** entegrasyon testi · **G** görsel · **O** gerçek Windows penceresi.

## 1. Üretim sağlığı (Kale, 16:57–16:58 UTC, yalnızca okuma)
| Konteyner | Durum | Yeniden başlatma | Hazır mı? |
|---|---|---|---|
| wbi-game1 (UDP 7771) | "Up" | 1 (son başlangıç 14:23) | **HAYIR — ölü sunucu** |
| wbi-game2 (7772) | "Up" | 2 (14:40) | **HAYIR — ölü** |
| wbi-game3 (7773) | "Up" | 1 (14:34) | **HAYIR — ölü** |
| wbi-game4 (7774) | "Up" | 1 (15:56) | **HAYIR — ölü** |
| wbi-game5 (7775) | "Up" 3 sa | 0 | evet ("hazır, UDP 7775") |
| wbi-game6 (7776) | "Up" 3 sa | 0 | evet |
| wbi-matchmaker | "Up" 3 sa | — | sağlıklı, `/health` yanıt veriyor |
- Ölü sunucuların günlüğü: `Error creating MainPlayerWindow: No available video device` (Xvfb kilidi kök nedeni, P0 raporuyla birebir). Süreç yaşıyor, **kendini toparlamıyor**; her biri CPU'nun yaklaşık %69'unda dönüyor (5/6 sunucuda 3 oyun sunucusu boşa çekirdek yiyor; Kale yük ortalaması 3,9).
- Şu an canlı oyuncu: **0**; boş ve hazır slot: **2** (s5, s6). Yani üretimin fiilî kapasitesi **2 oda x 4 oyuncu = 8 oyuncu**, 6 değil.
- Hiçbir konteynerde Docker `healthcheck` tanımlı değil (`health=none`).
- **Dikkat:** P0 kök nedenine göre s5 ve s6 de ilk oda sıfırlamasından sonra aynı şekilde ölecek. Düzeltme dağıtılmazsa kapasite sıfıra doğru gidiyor.
- Hiçbir şey değiştirilmedi: yalnızca `docker ps/inspect/logs/stats` ve matchmaker `/health`, `/status` GET'leri.

## 2. Matchmaker incelemesi (`server/matchmaker/mm.py`, canlı durumla doğrulandı)
- **Ölü/hazır olmayan slotlar otomatik dışlanıyor:** slot yalnızca son 20 sn'de kalp atışı geldiyse "alive"; hızlı oyun, özel oda kurma ve koda katılma `alive` şartı arar. Canlı `/health`: s1–s4 `alive:false`, s5–s6 `alive:true`; `/status`: `servers:2, free:2`. Oyuncular ölü sunucuya **yönlendirilmiyor**.
- Dışlama yalnızca etki: ölü sunucuyu **iyileştirmiyor** ve kimseyi uyarmıyor; kapasite sessizce düşüyor. Alarm/yeniden başlatma mantığı yok.
- Kalp atışı oyun sunucusunun "hazır" olmasından sonra gönderiliyor (`DedicatedServer.Run`), bu yüzden yarım açılmış sunucu hiç kalp atışı göndermiyor: dışlama güvenilir.
- Özel oda, atanmış slot ölürse koda katılan oyuncu `503 "Oda şu an yanıt vermiyor"` alır; slot kalp atışı gelmeyince atama sıfırlanmaz (yeniden canlanınca 120 sn boş kalmasıyla sıfırlanır).
- Boş oda 120 sn sonra sıfırlanır → sunucu `Application.Quit(0)` → Docker aynı konteyneri yeniden başlatır → **hata tam burada tetikleniyor**.
- **Mevcut odalar yeniden başlatmadan kesilir mi?** Oda sıfırlaması yalnızca **boş** odada olur → oynayan oda kesilmez. Kesen durumlar: `deploy.sh` (`--force-recreate` tüm 6 konteyneri yeniler: dolu odalar düşer), Kale yeniden başlatması, tekil konteyner ölümü. Matchmaker yeniden başlarsa bellekteki oda kodları kaybolur (çalışan oyunlar sürer, yeni katılımcı koda ulaşamaz). **Şu an hiç oyuncu olmadığı için bugün dağıtım kimseyi kesmezdi** (karar sizde).
- Öneri (uygulanmadı): `run.sh` düzeltmesi + Docker healthcheck + oyun açılışta pencere hatasında `exit 1` + matchmaker durumunu diske yazma + 4 ölü slot için uyarı.

## 3. Karakter varlıkları ve yedek
Unity'nin kullandığı 5 GLB (`Assets/ThirdParty/Generated/`, 8 427 248 bayt, her biri tek mesh, tek malzeme, **gömülü** doku, harici bağımlılık yok):
| Dosya | Boyut (bayt) | Üçgen | SHA-256 |
|---|---|---|---|
| char_asci.glb | 1 701 712 | 21 484 | d7e4d2e52f943b46…987e904fea |
| char_isci.glb | 1 491 024 | 6 950 | ee6a9729fb422fab…e8be8812c |
| char_kedi.glb | 1 774 076 | 16 084 | b4ff2ce1cf7e621e…2a6a791 |
| char_muhendis.glb | 1 553 892 | 7 976 | 30535ab009bb429b…ca1c5bc0 |
| char_robot.glb | 1 912 544 | 18 382 | 6c59bdc2ece7fd66…e3433aa24 |
(tam karmalar yedeklerdeki `SHA256-unity-glb.txt` içinde.) Bunlar Doku2'deki `*_textured.glb` kaynaklarıyla **birebir aynı** (karma eşleşti). `.meta` dosyaları da yedeklendi (Unity GUID'leri için).
**Bulgular:** (a) **Kurbağa ve sandık modelleri üretilememiş** (`char_kurbaga`, `prop_sandik` klasörlerinde yalnızca günlük var); kurbağa kodla üretilen ilkel karakter olarak kalıyor. (b) **Unity'ye hiç alınmamış 7 prop modeli** Doku2'de duruyor (`prop_bank/cop_kutusu/dolap/konteyner/lamba/muslugu/varil`): `AiAssets.Model` bulamayınca ilkel şekle düşüyor. (c) Karakter üçgenleri sanat bütçesini (8 000) aşıyor.
**Yedekler (hiçbiri silinmedi, orijinallere dokunulmadı):**
1. Laptop: `C:\Users\Captain\CicibyteProjects\WhoBrokeIt-backups\assets-20261104\` (5 GLB + 6 .meta + `SHA256-unity-glb.txt` + `roster.json` + `batch_wbi.sh`).
2. Kale2: `/root/backups/whobrokeit-assets-20261104/` — aynı içerik, **`sha256sum -c` ile 11/11 OK**.
3. Doku2'deki **tüm kaynak çıktılar** (`~/hadal-gpu/out/wbi`: lowpoly/shape/textured GLB'ler, konsept PNG'ler, 164 MB): sunucudan sunucuya Doku'ya `~/whobrokeit/backup/wbi-sources-20261104.tgz` (105 957 908 bayt, 157 giriş), SHA-256 `98fc0129…2e87b84a` gönderen ve alan tarafta **aynı** (`.sha256` dosyası yanında).
Not: 164 MB'lık paket laptoptan geçmedi (kotalı internet). Henüz depoda değil (GLB'ler `.gitignore`'da değil ama commit'lenmedi; karar: LFS/ayrı depo).

## 4. Kalite kapısı
`tools/mp/gate.sh` (Doku'da çalışır) + `tools/mp/sync-to-doku.sh` (laptop; `--check` ile depo/Doku farkı). Adımlar: 0 run.sh Xvfb temizliği var mı · 1 Unity derlemesi · 2 sunucu hazırlık (başlat, öldür, yeniden başlat, tekrar hazır) · 3 iki istemci clean/bad/hell (kontrol sayısı 17 olmalı) · 4 istisna taraması · rapor (`report.md`, `result.json`, `history.tsv`). Çıkış kodu 0/1/3.
**Gerçek koşu (`20261104-200238`, sürüm 0.4.5, yeni menü dâhil):** derleme geçti (3 tür uyarı) · hazırlık geçti · clean **17/17** · bad **17/17** · hell **17/17** · istisna taraması 0 → **KALITE KAPISI: GECTI** (D, E).
Sınırlar: negatif yol (kasten bozup KALDI üretme) bu oturumda **denenmedi**; Windows oyuncusu, ses, 4+ oyuncu kapsam dışı. 8 oyunculu istek kapıya eklenmedi; **MaxPlayers değişmedi (4)**. Ölçekleme planı: [OLCEKLEME_TEST_PLANI.md](OLCEKLEME_TEST_PLANI.md).

## 5. Sanat yönü
[SANAT_YONU.md](SANAT_YONU.md) + tek kaynak [`tools/blender/spec/style.json`](../tools/blender/spec/style.json): palet (8 marka + 26 malzeme), malzeme kuralları (palet atlası 256², trim 1024²), karakter oranları, ortam stili, 5 ışık ön ayarı, üçgen/doku/LOD bütçeleri, animasyon stili, adlandırma. Not: mevcut lobi sahnesinin beyaza yanan güneş/zemin renkleri bu belgeye aykırı (düzeltilecek).

## 6. Blender hattı (bir alt ajan kurdu; kendi gözümle önizlemeyi ve raporu inceledim)
`tools/blender/`: `kit_lib.py`, `generate_kit.py`, `validate_asset.py`, `render_preview.py`, `build_kit.sh`, `sync-to-doku.sh` + `unity/Assets/Editor/KitImportValidator.cs`. Blender 4.5.14 Doku'da `~/opt/blender` (başka projelerin ortamına dokunulmadı; çalışma alanı `~/whobrokeit/blender/`).
Gerçek koşu: **20 GLB üretildi, 20/20 doğrulamayı geçti** (mesh, normal, UV, malzeme, bütçe, ölçek, adlandırma; 132–616 üçgen), **özsınama 5/5** (bilerek bozulmuş 5 varlığı yakaladı), **Unity içe aktarma doğrulaması 20 OK / 0 FAIL** (`[KITCHECK]`, palet atlası 256²). Kit: kapı x2, duvar (düz/kapılı/pencereli), pencere, çit paneli, varil, koni, kasa, sigorta kutusu, palet, kavalet, raf, masa, tezgâh, alet çantası; kasa/varil/çit için ≤8 parçalı **kırık varyant**.
Dürüst değerlendirme: önizleme (`docs/blender-kit-preview/preview_kit.jpg`) **boru hattı kanıtı**; düz gölgeli, hafif pah, sade duvarlar, trim dokusu yok → nihai sanat değil. Unity yan etkisi: `--install` Doku proje kopyasında `Assets/Art/Kit/` (izlenmeyen GLB'ler) bıraktı. Unity doğrulaması UV'yi denetlemiyor (Blender tarafı denetliyor). Dış paket indirilmedi.

## 7. Greenfield Meydanı önerisi
[GREENFIELD_MEYDANI.md](GREENFIELD_MEYDANI.md): ≈ 90×70 m, 7 zon (meydan + saat kulesi, depo/yükleme rampası, atölye, dükkân caddesi, inşaat alanı, kaykay/bisiklet rampası, su kulesi), oynanış akışı (lobi → meydan → sözleşme → sonuç → serbest oyun), senaryo bağımsız **sözleşme sistemi** (`ContractDef` + sunucu `ContractDirector` + `IObjective`; mevcut vaka havuzda bir sözleşme olur), yıkım L1–L2 + tek L3 vitrin, forklift/taşıma ağırlık sınıfları, M0–M3 aşamaları ve doğrulama ölçütleri. **Henüz hiçbir ortam üretilmedi.**

## 8. Kalan riskler
1. **Üretim hâlâ bozuk:** 4/6 sunucu ölü, s5/s6 ilk sıfırlamada ölecek; düzeltme onay bekliyor (şu an oda yok, dağıtım kimseyi kesmez).
2. **Windows oyuncusu hiç doğrulanmadı** (menü, çıkış, ses, gamepad, kurulum, güncelleme).
3. **Hiçbir şey commit/yedek değil depoda:** bu oturumun 9 yeni dosya/klasörü izlenmiyor; GLB'ler depoda yok (yedekler var).
4. Karakter GLB'leri bütçeyi aşıyor; kurbağa/sandık yok; 7 prop modeli Unity'de kullanılmıyor.
5. Fizik anomalisi (4 koşunun birinde) açık; yıkım/animasyon/ses iyileştirmeleri başlamadı.
6. MaxPlayers=4, 8 oyuncu ölçülmedi; matchmaker alarm/iyileştirme mantığı yok.
7. Kapı negatif sınanmadı; çok oturumlu aynı çalışma kopyası riski sürüyor.

## 9. Önerilen sonraki adımlar
1. **Bu çalışmayı commit'le** (yalnızca belgeler, kapı, Blender hattı, doğrulayıcı) ve yedek GLB'leri depo dışı güvenli yerde tut.
2. **Üretim düzeltmesi için onay penceresi:** oda sayısı 0 iken `server/run.sh` + healthcheck + yeniden dağıtım (siz karar verin).
3. Windows build'ini sizin makinenizde bir kez açıp menü/ses/çıkışı doğrulamak (87 MB; kota için Tailscale + Doku).
4. M0 grey-box (`GreyboxBuilder`) + `ContractDirector` iskeleti + `Destructible` L1.
5. Kit için trim dokuları ve ilk "premium" varlıklar (forklift, dükkân cephesi), 7 prop modelini içe alma/yeniden topoloji kararı.
6. Sonra: ölçekleme testleri (4/6/8), animasyon/ses fazları.
