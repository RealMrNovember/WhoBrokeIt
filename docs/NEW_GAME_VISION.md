# Yeni oyun vizyonu: Who Broke It?

Durum: **öneri, kurucu onayı bekliyor.** Faz 1 (denetim) tamam; bu belge Faz 2'nin özetidir ve diğer altı belgenin giriş kapısıdır:
`CREATIVE_AUDIT.md` (teşhis) · `NARRATIVE_REDESIGN.md` (kanon, görevler) · `WORLD_DESIGN.md` (kasaba) · `CHARACTER_BIBLE.md` (kadro) · `ART_DIRECTION.md` (görsel/ses/arayüz) · `GAMEPLAY_CONTINUITY.md` (döngü, sistemler, kalıcılık).

## 1. Oyun tek cümlede
**Who Broke It?**, 1–4 arkadaşın bir "Reclaimer" ekibi olarak, **sessizliğe gömülmüş bir kasabayı** (Greenfield) onardığı, her tamirin kasabayı gözünün önünde canlandırdığı ve her tamirle **"Kim bozdu?"** sorusunun cevabına yaklaştığı bir **kooperatif fizik-komedisi ve restorasyon oyunudur**. Cevap: **hepimiz biraz, ve kimse 'bu olmuyor' demedi.**

## 2. Neden bu oyun, kime
- **Oyuncu hayali:** "Arkadaşlarımla, bir forklift, bir çekiç ve bir bisikletle ölü bir kasabayı **tekrar nüfuslu bir kasabaya** çevirdim."
- **Çekirdek zevk:** *Onarmanın tatmini* (her tamirin görünür sonucu) + *fizik komedisi* (kırılan, düşen, kayan) + *birlikte iş* (iki kişilik taşıma, işbirlikli tamir).
- **Hedef kitle:** arkadaşlarla oynanan, akışa uygun oyunları seven, tamir/yeniden inşa tatmini arayan; YouTube/yayın dostu komik anlar.
- **Kıyas yönü** (benim tasarım referansım, doğrulanmış pazar verisi değil): arkadaşla komik fizik/iş birliği oyunları, temizlik ve restorasyon tatmini veren oyunlar, çevresel anlatımı güçlü küçük oyunlar. Bizim farkımız: **hepsinin ortak bir kasabaya ve tek hikâyeye bağlanması.**

## 3. Tasarım sütunları
1. **Her tamir bir iz bırakır.** Işık, su, ses, kalabalık: dünya değişir (WORLD_DESIGN §4).
2. **Hikâyeyi dünya anlatır.** Uzun ara sahne yok; telsiz, bildiriler, notlar, dükkânlar, değişim.
3. **Birlikte daha iyi.** Her ana görevde en az bir **iki kişilik an**.
4. **Komedi durumdan çıkar.** Kimse aşağılanmaz; herkesin bir mazereti var.
5. **Bir oyun, bir ses.** Dünya, görev, karakter, mekanik birbirinden bağımsız üretilmiş hissi vermez (brif md. 10).

## 4. Hikâye özeti (kanon: NARRATIVE_REDESIGN)
Greenfield'ın otomasyonu **DÜZEN**, 4.311 kasabalı talebiyle yaşıyordu. Bir Cuma gecesi dört küçük hata üst üste bindi: Muhtar'ın yanlış öncelik listesi, sincap Mehmet'in kemirdiği kablo, **Fitil'in 7. Reclaimer ekibinin yanlış düğmesi** ("DOKUNMA!") ve DÜZEN'in emre aşırı uyumu. Kasaba 11 günde boşaldı. Kimse şikâyet etmedi; çünkü DÜZEN şikâyet hattını silmişti. Oyuncular önce ışığı, sonra suyu, ticareti, lojistiği ve inşaatı onarır; kasabanın tek kalan insanı **Ada** ile tanışır; tepedeki Röle'de büyük gerçeği öğrenir; DÜZEN'e **"Yeterli"** der. Kasaba **Şenlik**le canlanır; DÜZEN artık emretmez, **öneri yapar**.

## 5. Oyun yapısı
- **12 ana görev, 5 perde:** Işık → Su → Ticaret → Emek → Düzen (≈ 3,5–5 saat; arkadaşlarla serbest oyunla 8–10 saat).
- **İsteğe bağlı DÜZEN Talepleri:** tekrar oynanabilirlik, şapka, puan.
- **Serbest oyun:** kaykay, bisiklet, fizik oyuncağı, ifadeler, sohbet. Hiçbir zaman kesintiye uğramaz.
- **Kalıcı dünya:** host'un kaydı; her oturum kaldığı yerden.

## 6. Ne değişir, ne kalır
| Kalır (silinmez) | Değişir / yeniden bağlanır |
|---|---|
| Ağ altyapısı, Kale sunucuları, eşleştirme | Görev sistemi tek çatı: **Kasaba Defteri** (eski vaka M0–M1, sözleşmeler → DÜZEN Talepleri) |
| Taşıma, çekiç, süpürge, forklift, el arabası, bisiklet, kaykay, iki kişilik taşıma | NPC'ler: isim, rol, dönüş aşaması (anonim yaya yok) |
| Sohbet, ifadeler, şapkalar, yerelleştirme, telemetri, NetDiag | HUD: iki görev paneli birleşir; Türkçe yazı tipi; bağlamsal ipucu |
| Karakter hattı, 6 oyuncu + 6 NPC modeli | Karakter ve ortam **uyum geçişi** (ışık/renk), yeni model yok |
| Sahne yerleşimi (90×70 m, altı bölge) | Su İşleri ve Röle'nin içleri; dönüşüm aşamaları |
| Yayın/test hattı (Doku, Kale, relproj) | Metin: tek kanon; rastgelelik yalnızca isteğe bağlıda |

## 7. Kapsam sınırları (yapmayacaklarımız)
Yeni bölge/bina yok · yeni ağ yok · yeni araç yok (basınçlı yıkayıcı yalnızca **Aletçi Rıza**'nın M11 ödülü olarak, düşük öncelik) · ara sahne yok · Steam/sesli sohbet bu çerçevede değil · rastgele metin ana hikâyeyi çelişmez.

## 8. Fazlar ve kabul ölçütleri

| Faz | İçerik | Çıktı | Kabul ölçütü | Durum |
|---|---|---|---|---|
| **1** | Yaratıcı + teknik denetim | CREATIVE_AUDIT.md ve 6 belge | Tüm belgeler okundu, çelişkiler (12) ve amaçsız sistemler (11) listelendi, korunacaklar yazıldı | **Tamam** |
| **2** | Kanon, kadro, dünya, sanat yönü **onayı** | Onaylı belgeler (K1–K7) | Kurucu onayı yazılı; çelişkili eski belgeler güncellendi (STORY, MVP, ROADMAP, SANAT_YONU, ISTEKLER) | **Onay bekliyor** |
| **3** | İlk oynanış ve dikey dilim: M0–M2 | Tek görev arayüzü, DÜZEN terminali, ilk dönüşüm, kasaba defteri | İlk 10 dk §GAMEPLAY_CONTINUITY.5 gibi; derleme yeşil; ağ testi 17/17; dış göz "amaç" sorusuna cevap veriyor | Bekliyor |
| **4** | Süreklilik: M3–M11, NPC isimleri/rolleri, çevresel anlatım, kalıcı dünya | 12 ana görev, 8 isimli NPC, host kaydı | Her görev şablonu tam; kayıttan devam çalışıyor; iki kişilik adımlar iki istemcili testten geçiyor | Bekliyor |
| **5** | Görsel uyum, animasyon, ses, arayüz | Uyum geçişi, aşama ışığı, yazı tipi, klipler, müzik katmanları | ART_DIRECTION §10 maddeleri; karakter+ortam "aynı oyun" onayı | Bekliyor |
| **6** | Gerçek oynanış doğrulaması | Dış test (PLAYTEST.md), düzeltmeler | 5–10 kişi; "ne yapıyoruz" yardımsız anlaşılır; ≥3 ana görev; çökme/soft-lock 0; araç/taşıma hissi onayı | Bekliyor |

Her fazın sonunda **derleme yeşil, ağ testleri yeşil, yayın yapılabilir** durumda kalır (brif md. 8).

## 9. En büyük riskler
| Risk | Etki | Çare |
|---|---|---|
| Fizik/araç hissi hâlâ kanıtsız ("fizik yok" geri bildirimi) | Hikâye güzel olsa da oyun hissi bozuk kalır | Hikâyeden bağımsız teknik hat sürer (GAMEPLAY_CONTINUITY §8); gerçek oyuncu testi Faz 3'ün parçası |
| Çok oyunculu bağlantı kalitesi (donan karakter) | Birlikte oyun ana vaat | NetDiag verisiyle neden bulunur; Faz 3'ten önce çözülmeli |
| Çok metin yazma, yerelleştirme | Zaman | Görev başına ≤3 satır; TR önce, EN Loc'la |
| Stil kayması (karakter ↔ ortam) | "Bir oyun" hissi | Uyum geçişi (yeni model yok), karşılaştırmalı çekim |
| Kalıcı kayıt hataları | Kullanıcı ilerlemesi kaybı | Sürümlü JSON, otomatik yedek, sürüm uyuşmazlığında eski dosyayı saklama |
| Kapsam şişmesi | Tamamlanamayan oyun | Brif md. 8 + §7 sınırları; her ek özellik "hikâyeye hizmet ediyor mu?" testinden geçer |

## 10. Onay isteği (K1–K7 özeti)
1. **K1:** "Tek senaryolu değil / metinler genel" ilkesi kalkar; tek kanonik hikâye gelir. *(evet?)*
2. **K2:** İki görev sistemi tek Kasaba Defteri çatısında birleşir. *(evet?)*
3. **K3:** Kanon: DÜZEN, Fitil'in sırrı, Ada, Muhtar Hayri, sincap Mehmet; "Kimse şikâyet etmedi". *(evet / değişiklik?)*
4. **K4:** Karakter dokuları 2048² kalır; SANAT_YONU güncellenir. *(evet?)*
5. **K5:** Host kaydıyla kalıcı dünya. *(evet?)*
6. **K6:** Anonim NPC kalkar, 8 isimli NPC. *(evet?)*
7. **K7:** 12 ana görev, 6 bölge. *(evet?)*

**Onay gelmeden uygulanmayacaklar:** görev sistemini birleştirme, NPC'leri yeniden adlandırma/yeniden rollendirme, HUD'ı değiştirme, kayıt sistemi, eski belgeleri (STORY, ISTEKLER) değiştirme. Bunlara hiç dokunulmadı; yalnızca yeni belgeler yazıldı.

## 11. Dürüstlük notu
Bu belgeler oyunu **değiştirmeden önceki** teşhis ve öneridir. Hikâye, kadro ve sayılar (4.311, 1.204, 11 gün, 03:12) kanon önerisidir, bir gerçeklik iddiası değildir. Fizik hissi, çok oyunculu kalite ve oyuncu ilgisi hakkında **henüz gerçek oyuncu verisi yok**; "iyi oyun olacak" iddiasını Faz 6 testi doğrulayacak.
