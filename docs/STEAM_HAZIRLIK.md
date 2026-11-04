# Steam Erken Erişim hazırlık listesi

(gameskill.dev `steam-publish` ve `unity-build-pipeline` becerilerinden; rakamlar kaynak metindendir, Steamworks'te tekrar doğrulanmalı.)
Steamworks hesabı henüz yok: kullanıcı "en sonda yapalım" dedi. Bu liste o günkü iş içindir.

## Hesap ve ücret (erken başla, beklemeler var)
- Steam Direct ücreti uygulama başına yaklaşık 100 USD, iade edilmez; 1000 USD gelirde mahsup edilir.
- Vergi ve banka doğrulaması 30 güne kadar sürebilir. İlk oyunlarda ücret ödendikten sonra yayın için yaklaşık 21 gün bekleme var: **ücreti erken öde**.
- Yükleme için ayrı, düşük yetkili bir build hesabı (yalnızca "Edit App Metadata" + "Publish"). `config.vdf` ve parolalar repoya girmez (Kale2 `/.secrets` benzeri bir yerde durur).

## Mağaza sayfası ve inceleme
- Mağaza sayfası build'den ÖNCE incelemeye gönderilir. İnceleme yaklaşık 3–5 iş günü (planda 7 gün bırak).
- "Coming Soon" en az 2 hafta önceden açık olmalı. Yayın manuel ("Release App"); `default` dalı otomatik canlıya alınamaz.
- Mağazada işaretlenen her özellik ve listelenen her işletim sistemi incelenen build'de gerçekten çalışmalı.
- **Yapay zekâ içeriği beyanı (Content Survey):** önceden üretilmiş içerik (yapay zekâ ile üretilen karakter/eşya modelleri, dokular, ses, metin) "pre-generated" olarak beyan edilmeli. Bizdeki karakter GLB'leri tam bu durumda (SDXL + TRELLIS; kayıt: `ASSET_CREDITS.md`). Ekip özgün modelleri yapınca beyan güncellenir.

## Build ve depolar
- Windows istemci ve Linux sunucu için ayrı depot / işletim sistemi süzgeci. Depot'lar yayınlanır (Publish) ve geliştirici paketine eklenir. `steamcmd` ile yükle, önce beta dalı.
- Build'de `BuildReport.summary.result` kontrol edilir (zaten ediliyor), sahne listesi doğrulanır, stripping'den kaynaklanan yalnızca-build hataları için `link.xml` (IL2CPP'ye geçilirse C++ araç zinciri gerekir).
- Kurulum .exe'si yerine Steam istemcisi dağıtır; kendi güncelleyicimiz ve `C:\Program Files\CiciByte` kurucusu Steam sürümünde **kapatılmalı** (`InstallGuard`, `UpdateChecker`).
- Çok oyunculu: Steam davetleri/lobi, mevcut oda kodu ve hızlı oyun ile birlikte düşünülecek.

## Diğer
- Yerelleştirme: TR + EN yeterli; metinler koddan dışarı alınır.
- İlk dış oyuncularla koçlamadan test et; "başarısız sayılma ölçütleri" yaz (prototype-fast).
- Ses, game feel, kalite ayarları ve RPC doğrulaması Steam'den önce tamamlanmalı (bkz. `OYUN_BECERILERI.md`).
