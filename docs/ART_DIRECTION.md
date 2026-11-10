# Sanat yönü: Greenfield'in görsel kimliği

Durum: **öneri, onay bekliyor** (CREATIVE_AUDIT.md K4). `SANAT_YONU.md` (nihai görsel kimlik, ölçüler, bütçeler) **geçerlidir ve korunur**; bu belge onun üstüne **dünya aşaması, bölge kimliği, ışık/ses/arayüz entegrasyonu** ve **mevcut stil kaymasının çözümünü** ekler. Çelişki olursa burası (sonra tarihli) esas alınır ve SANAT_YONU güncellenir.

## 1. Tek cümlelik görsel kimlik
**"Sıcak ışıkta, biraz dağınık, çok sevilen küçük bir kasaba."** Pastel sıva, kiremit, tuğla, el yazısı tabela; doygun ama kontrollü renk; kalın silüet; her yerde **bir el değmiş** izi. Kasaba **soluk gri-yeşilden** başlar ve **altın saatin sıcak renklerine** yükselir. Renk, hikâyenin ilerlemesinin **birincil göstergesidir**.

## 2. Bugünkü durum (denetimden)
| Öğe | Gördüğüm | Sorun |
|---|---|---|
| Ortam | Pastel kasaba, kaldırım, parke, ağaçlar, saat kulesi: tutarlı ve güçlü | Baştan sona **aynı canlılıkta**: terk edilmiş hissi yok |
| Props | Palet atlaslı, düz renk, temiz | İyi; "el değmişlik" detayı az |
| Karakterler | Yapay zekâ üretimi, gerçekçi doku, 16–26k üçgen, 2048² | Ortamla **stil kayması**: karakterler "gerçekçi-sevimli", ortam "çizgi film düz". Yan yana tutarlı hissetmiyor |
| NPC'ler | Karakterlerle aynı hat | Aynı kayma |
| Işık | Tek güneş, altın saat | Aşama/zaman yok |
| Arayüz | UiKit paleti (Güneş/Gök/Mercan/Nane) menüde güzel; oyun içinde ham OnGUI, iki görev paneli | Menü premium, **oyun içi ham** |
| Animasyon | Prosedürel, 7 kemik | Karakter başına kişilik yok; ağırlık hissi zayıf |

## 3. Çözüm: "uyum geçişi" (yeni varlık üretmeden)
Brif md. 7: "ilgisiz yapay zekâ varlıkları sanatsal bütünleşme olmadan eklenmeyecek."
1. **Ortak renk dili:** Karakter dokularına **palet sınırlı renk ayarı** (editörde otomatik, yeniden boyama değil): doygunluğu +8–12, kontrastı −6, sıcak ton kaydırma; her karakterin **3 ton ailesi** kuralına yaklaştırılması. Ölçüt: karakter ile en yakın props yan yana, aynı ekran görüntüsünde "aynı oyundan".
2. **Ortak ışık:** karakter malzemesi için aynı URP Lit ayarı, aynı gölge; **hafif dış çizgi (rim)** veya yumuşak kontur (isteğe bağlı kapatılır) ile siluet okunurluğu (30 m kuralı).
3. **Oran kılavuzu:** 1,8 m, baş %30, eller 1,3× zaten ilke; mevcut modellerin **eller/ayaklar** ölçeği doğrulanıp (Doku oturumu) gerekirse ölçek ayarı.
4. **Doku bütçesi kararı (K4):** karakterlerde 2048² **kalır** (kahraman kalitesi); SANAT_YONU §4, §8 "karakter 1024², ≤8k" satırı **güncellenir**; toplam VRAM ölçülüp (hedef ≤120 MB) gerekirse NPC'ler 512² kalır (zaten öyle).
5. **Stil kayması düzelmezse:** NPC'ler için ikinci yol: daha düz renkli (palet atlaslı) **sadeleştirilmiş** NPC modelleri. Karar, uyum geçişinden sonra ekran görüntüsü karşılaştırmasıyla.

## 4. Bölge kimlikleri (renk/malzeme/ışık)
| Bölge | Ana renk ailesi | Malzeme ağırlığı | Işık karakteri | Ayırt edici öğe |
|---|---|---|---|---|
| **Meydan** | Krem + kırmızı + çeşme mavisi | Parke, taş, bronz | Altın saat merkezi | Saat kulesi, çeşme, fenerler |
| **Ana Cadde** | Pembe, turkuaz, kum, bordo tente | Sıva, kiremit, boyalı ahşap | Sıcak, tente gölgeleri | Dükkân tabelaları, tezgâh renkleri |
| **Atölye + Avlu** | Sarı-turuncu, mavi tulum rengi | Ahşap, çelik, pano | İç mekân sıcak lamba | Alet duvarı, DÜZEN terminali |
| **Depo** | Sarı-siyah uyarı, kahverengi | Karton, çelik, beton | Serin, tepe lambaları | Palet yığınları, rampa |
| **İnşaat** | Gri beton + turuncu | Tuğla, iskele, çimento | Gün ışığı, toz | Yarım duvar, iskele çelik |
| **Tepe + Röle** | Gökyüzü mavisi, yeşil çim | Çim, toprak, kablolar | Açık gökyüzü, rüzgâr | Anten, rampa, iki düğme |
| **Su Kulesi** | Pas kırmızısı + mavi | Çelik, boru | Serin, su yansıması | Kule, boru ağı |

## 5. Aşama = renk (kalıcı dünya)
Işık/sis/post ön ayarı **bölge aşamasına** bağlanır (SANAT_YONU §6 ön ayarlarını kullanır):
| Aşama | Ön ayar | Doygunluk | Ek |
|---|---|---|---|
| 0 Terk | **Bulutlu** | −18 | Gri-yeşil sis, soluk tabela, sönük neon |
| 1 Temizlendi | Bulutlu → Öğle | −8 | Yer parlaklığı artar |
| 2 Çalışır | **Altın saat** (varsayılan) | 0 | Lamba/çeşme ışığı; ses başlar |
| 3 Onarıldı | Altın saat | +8 | Boya, çiçek, afiş |
| 4 Canlı | Altın saat + akşam fenerleri | +12 | Fener bloom, konfeti |
Geçiş **yumuşak** (8–15 sn), görev bitiş anında olur ve **kutlama anı**dır (kısa ses, ışık dalgası, telsiz).

## 6. Arayüz (HUD + menü)
Hedef: **tek tasarım dili.** Menünün güzelliği oyuna taşınır.
- **Yazı tipi:** Türkçe karakterli, kalın, yuvarlak bir yazı tipi dosyası (OFL lisanslı; ör. *Baloo*, *Nunito*) projeye alınır; varsayılan Unity yazı tipi kalkar.
- **Görev arayüzü tek:** Sol üstteki eski "GÖREV" kutusu ve ayrı sözleşme afişi **kalkar**; yerine **"Kasaba Defteri çubuğu"**: ana görev (1 satır) + ilerleme + isteğe bağlı talep (ikinci satır). Şekil: UiKit kartı (Arduvaz zemin, Güneş başlık).
- **Telsiz (Fitil):** köşede yuvarlak telsiz ikonu + dalga; mesaj şeridi sarı; M8 sonrası sinyal bozulması.
- **DÜZEN kartları:** sarı-turuncu CRT stili, "DUYURU No. 4311".
- **Kontrol ipucu satırı:** ham beyaz satır yerine **bağlamsal ipucu** (yalnızca ilgili an: araçta araç tuşları, taşırken taşıma tuşları).
- **Kontrast ve kenar:** 16:9 ve 21:9, ölçek 0,6–1,3 (UiKit ölçeği).

## 7. Animasyon (yaklaşım dürüst)
- **Mevcut:** prosedürel (7 kemik, kod). Kişilik zayıf, ağırlık zayıf.
- **Hedef:** karakter başına **idle kişiliği** (3 poz), ağırlık taşırken geniş duruş, araçlara biniş/iniş geçişleri; robot kesik, kedi yumuşak, kurbağa zıplayan.
- **Üretim yolu:** prosedürel altyapı kalır (hepsi çevrimiçi senkron ve hafif); üzerine **Blender'da hazırlanmış 8–10 anahtar klip** (taşıma, itme, oturma, el sallama, dans) **karıştırılır**. Klip yoksa prosedürel çalışmaya devam eder (güvenli).
- **Doğrulama:** `hud.sh` poz çekimleri; kol uzaması, yırtılma, delik: 0.

## 8. Ses ve müzik (arayüz, ışık ve hikâye ile aynı kimlik)
- **Müzik:** tek motif, **müzik kutusu** (DÜZEN'in "uyku şarkısı"); aşama ilerledikçe çalgı eklenir: 0 yalnız müzik kutusu → 2 + gitar → 3 + akordeon → 4 + kalabalık. Menü müziği aynı motifin özeti.
- **Telsiz:** bant sınırlı filtre; DÜZEN: kısık, düz anons; Ada: hiç ses yok (not).
- **SFX:** mevcut kodla üretilen efektler korunur; çeşme, lamba vızıltısı, kapı gıcırtısı (aşama 0→2) eklenir.

## 9. Yapmayacaklarımız
- Yeni, ilgisiz yapay zekâ modeli eklemek (uyum geçişi tamamlanmadan).
- Dokuları körü körüne değiştirmek (önce karşılaştırmalı çekim).
- Bina/bölge çoğaltmak (kapsam: mevcut yerleşim).
- Prosedürel animasyonu silmek (klipler üzerine karışır).

## 10. Kabul ölçütleri (Faz 5)
- [ ] Karakter ve ortam aynı kare içinde "aynı oyun" (kurucu onayı, 6 çekim).
- [ ] 5 aşama için ışık/post ön ayarları bölgeye bağlı, geçişler yumuşak.
- [ ] Türkçe karakterli kalıcı yazı tipi kullanılıyor; iki görev paneli birleşti.
- [ ] Menü ve HUD aynı tasarım dilinde (UiKit); ham beyaz kontrol satırı kalktı.
- [ ] 8–10 klip karıştırılıyor; her karakter için 3 idle.
- [ ] Müzik motifi aşamaya göre katman ekliyor.
