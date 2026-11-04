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

## 0.4.2 (2026-11-04)
- Kaykay ve bisiklet: sahnede sürülebilir (E ile bin, W/S hız, A/D yön, kaykayda Boşluk zıpla); çok oyunculu, herkes sürücüyü görür.
- Araca uygun duruş: kaykayda yandan duruş, dönüşe yatma, ara ara yerden itme; bisiklette oturuş, gidonda eller, pedal ile bacakların senkron dönmesi; tekerler döner, bisiklette gidon ve ön tekerlek yönlenir.
- Site: kontrol tablosuna araçlar, Esc menüsü ve F11.

## 0.4.3 (2026-11-04)
- Gecikme: araç sürerken istemci tarafı tahmin (kaykay, bisiklet, forklift sürücünün makinesinde anında tepki verir, sunucu pozuna yumuşakça yaklaşır); sürücü girdileri saniyede 30 kez gönderilir.
- Uzaktaki sürücü artık araca bağlı çizilir (arkadan havada giden arkadaş sorunu).
- F3: FPS ve ping göstergesi; grafik kalitesi Düşük/Orta/Yüksek (ilk açılışta donanıma göre seçilir, FPS uzun süre düşükse otomatik düşer; Esc menüsünden değişir).
- Sunucu: 60 kare, başlatma yarışı düzeltildi (game1–3 "video aygıtı yok" ile çöküp yeniden başlıyordu).

## 0.4.4 (2026-11-04)
- Esc menüsü açıkken oyun girdisi kapatıldı (karakter yürümez, E etkileşmez, fare tıkı fırlatmaz).
- docs: OYUN_BECERILERI.md (gameskill.dev taraması özeti), STEAM_HAZIRLIK.md (Steam öncesi liste).
