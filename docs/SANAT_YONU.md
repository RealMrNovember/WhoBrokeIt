# Who Broke It? — Sanat yönü (nihai görsel kimlik)

Hedef: **renkli, premium, stilize 3B kooperatif oyun.** Karakter, araç, bina, prop ve arayüz aynı sanat evreninden gelir. Sayısal değerlerin tek kaynağı [`tools/blender/spec/style.json`](../tools/blender/spec/style.json); bu belge gerekçeyi ve kuralları anlatır. Değişiklik ikisinde birlikte yapılır.

Kaçınılacaklar: boş yeşil zemin, rastgele ilkel nesne, tutarsız stil, jenerik yer tutucu karakter, düz ışık, solmuş renk, rastgele dizilmiş sahne.

## 1. Stil sütunları
1. **Tombul ve okunaklı biçimler.** Kalın silüetler, yuvarlatılmış kenarlar, 30 m'den tanınan siluet.
2. **Doygun ama kontrollü renk.** Orta değerli doygun ana renkler, sıcak ışık / soğuk gölge. Bir kümede en çok 3 ton ailesi.
3. **El yapımı kusur.** Hafif eğrilik (±2°), aşınmış kenar, boya sıyrığı. Mükemmel düz hiçbir şey yok.
4. **Etkileşim dili.** Oyuncunun kullanabileceği/taşıyabileceği nesnelerde sıcak sarı-turuncu vurgu (`M_hazard_yellow`, `M_paint_orange`); kırılabilir nesneler görünür şekilde eski ahşap/seramik/cam; kırılmaz olanlar masif çelik/beton. Oyuncu bir nesnenin ne yapacağını renginden okur.
5. **Mizah ölçeği.** Abartılı oran, büyük eller, yumuşak (squash & stretch) hareket, neşeli ama alaycı olmayan.

## 2. Renk paleti
Marka (arayüz + vurgu): **Güneş** `#FFC933`, **Koyu turuncu** `#F28F1A`, **Gök** `#52B8FA`, **Mercan** `#FF6B61`, **Nane** `#5CDB9E`, **Krem** `#FFF5DB`, **Mürekkep** `#1A1F38`, **Arduvaz** `#383F70`.

Dünya malzemeleri (26 adlı palet malzemesi, `style.json`): ahşap (sıcak/koyu/açık), çelik, koyu metal, boya (kırmızı, sarı, mavi, yeşil, turuncu), beton, asfalt, sıva (krem, pembe, turkuaz, kum), tuğla, kiremit, cam, kauçuk, branda, çim, yaprak, su, uyarı sarısı, sıcak lamba.
Kurallar: albedo **0,04–0,92** (saf beyaz/siyah yok — mevcut lobide "beyaza yanan" zemin ve güneş diski bu kuraldan çıkıyor), emisyon yalnızca lamba/ışıklı nesnelerde.

## 3. Malzeme kuralları
- **Palet atlası iş akışı:** Düz renkli yüzeyler 256×256 palet dokusundan (8×8 renk karesi) örneklenir; tüm props tek URP Lit malzemesini paylaşır → az çizim çağrısı. Yüzey detayı (ahşap damarı, tuğla, beton) 1024² **trim atlas** ile.
- URP Lit, metalik yalnızca metal malzemelerde (0,7–0,8), diğerleri 0. Pürüzlülük 0,45–0,95; cam 0,05 + alfa 0,35.
- Blender'da malzeme adı = palet adı (`M_wood_warm`); Unity içe aktarıcı adla ortak malzemeye bağlar.
- Eski AI karakter dokuları (1 malzeme, 1 gömülü doku, 1024²) karakter için kalır; yeni karakterler ≤2 malzeme.

## 4. Karakter oran kılavuzu
- Boy **1,8 m** (oyun sabiti), **kafa boyun %30'u** (≈3 kafa boyu), kısa tombul gövde, **eller 1,3×**, kalın ayakkabı/bot. Yüz büyük gözlerle ifade taşır.
- Tüm karakterler (insan, robot, hayvan) aynı ölçeği ve **aynı iskelet** adlarını (hips, iki bacak, iki kol, kafa) paylaşır; ayak tabanda orijin; +Z ileri.
- Üçgen bütçe **≤8 000** (LOD0). **Mevcut AI modelleri aşıyor** (aşçı 21 484, robot 18 382, kedi 16 084, mühendis 7 976, işçi 6 950): yeniden topolojiye veya sadeleştirmeye (LOD0 hedefi 8k) alınmalı; şimdilik "geçici" etiketli.
- Ekip kimliği: silüet, baş aksesuarı ve ana renk ile ayırt edilir (6 karakter, 6 vurgu rengi menüde zaten var).
- Kişiselleştirme için yuvalar: şapka, sırt, yüz, gövde boyası (`SOCK_hat`, `SOCK_back`, `SOCK_face`).

## 5. Ortam stil kılavuzu
- **Dil:** küçük bir kasabanın canlı meydanı. Pastel sıva cepheler (krem/pembe/turkuaz/kum) + canlı tabelalar, kiremit çatılar, tuğla vurgular, çelik/boya yüklenme alanı.
- **Zemin hiçbir zaman düz yeşil değil:** asfalt, parke, kum, çim yamaları, boya çizgileri, yağ lekeleri, su birikintileri (decal), kaldırım kenarları.
- **Doluluk:** yürünebilir her ~3 m'de 1–2 hikâye veren prop (kasa yığını, askıdaki çamaşır, yarım bırakılmış iş).
- **Modüler ızgara:** 0,5 m; duvar 3×3×0,3 m, kapı boşluğu 1×2,2, pencere 1,2×1,2 (denizlik 0,9), kat 3 m, çit 2 m × 1,1 m.
- **Mekân hikâyesi:** tabelalar, el yazısı panolar, onarım izleri — senaryodan bağımsız, oyunculara bırakılmış boşluklar.
- **Ağaç/bitki:** basit yuvarlak taçlar, 2–3 ton yeşil; asla tek renk.

## 6. Işık ön ayarları (URP, tek Güneş + ortam)
| Ön ayar | Güneş (renk, şiddet, açı) | Ortam | Sis | Post |
|---|---|---|---|---|
| **Altın saat** (menü, vitrin, varsayılan) | `#FFD9A3`, 1,1 (URP ana ışık), (50°, −30°), yumuşak gölge 0,9 | Gradyan: gök `#9CCBF5` / ufuk `#F5E3C8` / zemin `#A3B58A`, ≤1,0 | `#F1C9A0` hafif | Bloom 0,25 eşik 1,05; ACES; doygunluk +8; kontrast +8; vinyet 0,18 |
| **Öğle** | `#FFF3DC`, 1,3, (65°) | Parlak mavi gradyan | `#BFD8F0` | Bloom 0,2; doygunluk +10 |
| **Bulutlu** | `#DCE6F0`, 0,7 | Düz soğuk | Yoğun gri-mavi | Kontrast +4 |
| **Akşam/gece** | `#6F86C9`, 0,35 + sokak lambaları (sıcak nokta ışık `#FFD79A`, aralık 10–13 m) | Koyu mavi gradyan | Mor-mavi | Bloom 0,5 |
| **İç mekân** | Pencereden soğuk dolgu + sıcak lambalar (3000 K nokta ışık, 6–11 m) | Düşük | Yok | Bloom 0,3 |
Kurallar: sahnedeki **tek** yönlü ışık ana ışıktır (menü sahnesi kendi ışığını `RenderSettings.sun` olarak ayarlar); gölge uzaklığı ≤60 m; düşük kalite ön ayarında MSAA/gölge kısılır; RenderTexture örnek sayısı URP ile uyumlu olmalı (menü kamerası MSAA kapalı).

## 7. Çokgen (üçgen) bütçeleri — LOD0
Küçük prop (kasa, varil, kova, koni, lamba) **≤800** · orta prop (palet, kavalet, sandalye, el arabası, bank, yangın musluğu, çöp kutusu) **≤1 500** · mobilya (raf, masa, tezgâh, dolap) **≤2 500** · modül (kapı, pencere, çit) **≤1 200** · duvar/zemin/çatı modülü **≤800** · bina (birleşik cephe) **≤15 000** · araç (forklift) **≤12 000** · karakter **≤8 000**. Toplam görünür ekran bütçesi hedefi: **≤1,5 M üçgen** (orta kalite, 8 oyuncu dâhil).

## 8. Doku çözünürlük bütçeleri
Palet atlası **256²**, trim atlası **1024²** (albedo; gerekirse normal), kahraman prop **512²**, karakter **1024²**, arayüz ikonu **256²**. Toplam benzersiz doku **≤120 MB VRAM** (orta). Sıkıştırma BC7 / ASTC 6×6, mip + anizotropi 4. Doku boyutu ihtiyaçtan büyükse doğrulayıcı uyarır.

## 9. LOD kılavuzu
Küçük prop: LOD yok, 40 m'de gizle · orta prop: LOD1 %50 (ekran boyu %25'te), %2'de gizle · bina: LOD1 %40 (%20), %1'de gizle · karakter: LOD1 %50 (%30), LOD2 %25 (%10). Çarpıştırıcı: ayrı basit şekil (kutu/kapsül/≤32 köşeli dışbükey), adı `COL_<ad>_<n>`, çizilmez. Fizikli nesnede çarpıştırıcı mesh'ten bağımsız ve basit (kararlı simülasyon için).

## 10. Animasyon stili
- **Abartı ve netlik:** 0,08–0,15 sn hazırlık (anticipation), vuruşta squash & stretch, takip hareketi (alet, kol, kuyruk, anten).
- **Ağırlık hissi:** ağır yük taşırken geniş duruş, geriye eğilme, yavaş adım; hafif nesnede seken adım.
- **Kişilik:** 3 farklı boşta (idle) pozu her karakter için; koşma döngüsü hız eşlemeli (6 poz); karakter türüne göre (robot: kesik, kedi: yumuşak, kurbağa: zıplayan).
- **Fiziksel tepki:** büyük çarpışmada 0,4 sn'lik "yarı-ragdoll" karışımı; ayak IK'si ile eğime uyum; iniş sönümleme; araca biniş/inişte kısa geçiş.
- **Üretim:** 30 fps, yumuşak eğriler, kök hareket yok (hareket kontrolcüde). Prosedürel animasyon yalnızca ince katman (nefes, sallanma) olarak kalır.

## 11. Adlandırma
`<kategori>_<ad>[_<varyant>][_LODn].glb` küçük harf ASCII: kategoriler `char, prop, bld, fnc, veh, fx, env`. Malzeme `M_<ad>`; çarpıştırıcı `COL_<ad>_<n>`; yuva `SOCK_<ad>`; yıkım `<ad>_intact`, `<ad>_broken`, `<ad>_chunk_01..08`; Unity yolu `Assets/Art/Kit/<kategori>/`; prefab `P_<ad>`. Örnek: `prop_crate_a.glb`, `bld_wall_door_a.glb`, `veh_forklift_lod1.glb`.

## 12. Arayüz ile uyum
Arayüz paleti marka renkleriyle aynı (UiKit: Güneş, Gök, Mercan, Nane, Krem, Arduvaz). Yeni ekranlar `UiKit` kullanır; yazı tipi ve boyutlar tek ölçekte kalır. Sonraki iş: Türkçe karakterli kalıcı yazı tipi dosyası (şu an Unity varsayılanı) ve yerelleştirme anahtarları.

## 13. Doğrulama (nasıl ölçülür)
`tools/blender/validate_asset.py` bütçe, ölçek, malzeme, UV, normal ve adlandırmayı denetler; `KitImportValidator` Unity içe aktarmasını denetler; her faz sonunda ekran görüntüsü karşılaştırması (aynı kamera noktaları) ile "solmuş/beyaza yanmış" kontrolü yapılır.
