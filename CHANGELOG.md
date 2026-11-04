# Değişiklik günlüğü

## 2026-11-04
- Proje adı Who Broke It? oldu; MVP kapsamı, altyapı belgeleri ve repo iskeleti eklendi.

- Kurgu ve anlatım taslağı (docs/STORY.md) eklendi.

- MVP.md v0.2: kesin kapsam, teknik/uretim asamalari ve oncelik sirasi; ROADMAP guncellendi.

## P0 (2026-11-04)
- Unity projesi (unity/): URP + Input System, oyuncu hareketi, kamera, etkilesim sistemi, test avlusu, sigorta kutusu. Doku uzerinde Windows build basarili (87 MB).

## P1 (2026-11-04)
- Atolye dongusu: ipucu/defter, supurge, cekic+sigorta kutusu (yanlis sirada kivilcim kazasi), al-tasi-birak, el arabasi, palet+alet dolabi, forklift (arcade surus, catal kaldirma), sifirlama (X), gorev listesi, sonuc ekrani. Sahne: Workshop.unity.

## 0.2.0 (2026-11-04)
- P2: Netcode for GameObjects, host/katil menusu, sunucu-otoriter vaka/forklift/aletler, otomatik guncelleme, yeni site.

## 0.3.1 (2026-11-04)
- Cevrimici: oda kodu + hizli oyun (Kale dedicated sunucular + eslestirme servisi), surum uyumu ve kapasite onayi, oda kodu ekrani.
- Windows kurulum sihirbazi (Program Files\CiciByte\WhoBrokeIt), kurulum korumasi.
- Site: oda kodu anlatimi, canli sunucu durumu, kurulum indirme.

## 0.3.2 (2026-11-04)
- Firlatma (sol tik, tasirken) ve vurma (sag tik): geri itme + kisa sersemleme, hasar yok. Dostane bonk odulu.
- latest.json artik python ile uretiliyor (not icindeki ters egik cizgi dosyayi bozuyordu: site ve otomatik guncelleme sürüm bilgisini okuyamiyordu). Sabit /download adresi, varlik dosyalarina surum eki.

## 0.4.0 (2026-11-04)
- Karakterler: oyuncu menuden karakter secer (isci, muhendis, asci, robot, kedi, kurbaga). Bes karakter yapay zeka ile uretilmis dokulu 3B modeldir; kurbaga yedek (ilkel sekil) modelle gelir.
- Birinci sahis kamera: V tusu veya menuden secilir, kendi govdemiz gizlenir.
- Forklift duzeltildi: gövdenin yer surtunmesi yuzunden kipirdamamasi giderildi, tekerlekler zeminin ustunde, catal kalkiyor.
- Gorsel: cim/zemin dokusu, isik, bloom, ACES ton esleme, sis.

## 0.4.1 (2026-11-04)
- Karakterler: yapay zeka modellerine otomatik iskelet (bacak/kol); yürüme, bekleme, zıplama, taşıma, fırlatma, vurma, sersemleme animasyonları.
- Forklift: yeni modern tasarım (kabin, aydınlatma, direk, basamak), dönen ve sağa sola yönlenen tekerlekler, rampaya tırmanma (devrilmez), sürücü tırmanıp oturur ve görünür.
- Esc duraklatma menüsü: kamera (mesafe, yükseklik, omuz, görüş alanı, fare hızı), tam ekran (F11), karakter değiştirme, menüye dön / çık. Varsayılan tam ekran penceresi.
- Logo ve oyun ikonu, haritadan bağımsız yeni ana menü arka planı; site logosu.
- Kullanıcı istekleri docs/ISTEKLER.md dosyasında tutulur.
