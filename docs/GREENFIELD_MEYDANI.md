# Greenfield Meydanı — dikey dilim tasarımı

Durum: **tasarım** (kod/ortam üretilmedi). Amaç: nihai kalite çıtasını gösteren tek, çok cilalı oynanabilir alan. Senaryodan bağımsızdır: alanın içeriği oyunculara ve oturuma göre değişen **sözleşmelerle** (contracts) doldurulur. Önce gri kutu (greybox), sonra kit varlıkları. Sanat kuralları: [SANAT_YONU.md](SANAT_YONU.md).

## 1. Tasarım hedefleri
- Çok oyunculu keşif (4 → ileride 8), forklift kullanımı, nesne taşıma, kooperatif onarım, çevresel yıkım, çok sayıda etkileşimli nesne, komik fizik.
- **Her oturum farklı hikâye:** sözleşme havuzu + rastgele "kaos olayları" + serbest oyun.
- 60 sn içinde ilk eğlence: spawn noktasından 20 m içinde etkileşilecek nesne, 40 m içinde araç.
- Okunaklı: renk dili (sıcak sarı = tutulabilir/etkileşimli, soluk ahşap = kırılabilir, masif çelik = kırılmaz).

## 2. Grey-box yerleşimi (≈ 90 × 70 m, X doğu, Z kuzey, 1 birim = 1 m)
```
 Z+                                        KUZEY
 70 ┌──────────────────────────────────────────────────────────────┐
    │ [G] SU KULESİ      [C] ATÖLYE (açık cepheli salon 14×10)      │
    │  (-34, 58)          (14..28, 50..60)   tezgâh, alet duvarı,    │
    │                     sigorta kutusu, onarım istasyonu           │
 45 │ ┌───[B] DEPO + YÜKLEME RAMPASI (24×16)───┐        ┌──[E] İNŞAAT ALANI 22×14──┐
    │ │ raf koridorları, forklift, palet,      │ ◄───►  │ iskele, yarım duvar,    │
    │ │ kamyon yuvası (açık arka)              │  cadde │ çimento, tahta yığını,  │
    │ └───────────(−44..−20, 38..54)───────────┘        │ kırık çit hattı         │
    │                                                    └──(48..70, 36..50)───────┘
 30 │            ┌────────[A] MEYDAN (daire r=14)───────────┐
    │            │ çeşme + SAAT KULESİ (landmark, 0,0 ofset) │  ← spawn (8 nokta, halka)
    │            │ sözleşme panosu, bank, lamba, pazar tezgâhı│
    │            └───────────────────────────────────────────┘
 12 │ ─── [D] ANA CADDE (asfalt, 8 m) ───  dükkân cepheleri sürekli ───
    │ [D1] FIRIN (pembe) [D2] HIRDAVATÇI (turkuaz, İÇİ GİRİLİR) [D3] MEYVECİ (kum) [D4] BOŞ DÜKKÂN
  0 │ ───────────── kaldırım ──────────── [F] RAMPA/TEPE (kaykay-bisiklet) (50..78, 2..14)
    └──────────────────────────────────────────────────────────────┘
     0                       45                                    90   X+
```
Zonlar (merkez/boyut kabaca; greybox sırasında ayarlanır):
| Zon | Konum | Boyut | İşlev |
|---|---|---|---|
| **A Meydan** | (45, 28) | r = 14 | Spawn, sözleşme panosu, serbest oyun; çeşme + saat kulesi (kırılmaz landmark); 8 spawn noktası halkada |
| **B Depo** | (22, 46) | 24×16 | Forklift başlangıcı, 2 raf koridoru, palet yığınları, kamyon yuvası; yükleme rampası (eğim ≤ 12°) |
| **C Atölye** | (21, 55) | 14×10 | Onarım istasyonu (malzeme + alet), sigorta kutusu, tezgâh; iç mekân ışığı |
| **D Cadde** | z = 8–16 | 90×8 | Dükkânlar (D2'nin içi girilir, diğerleri cephe + küçük iç), pazar tezgâhları, kaldırım |
| **E İnşaat** | (59, 43) | 22×14 | Kırık çit, yarım duvar, iskele (kırılabilir), çimento torbaları, tahta yığını: onarım/yıkım alanı |
| **F Rampa** | (64, 8) | 28×12 | Kaykay/bisiklet rampası, eğimli tepe: fizik oyunu, varil yuvarlama |
| **G Kule** | (11, 62) | r=5 | Su kulesi (landmark, görsel); üstünde "ünlü forklift" sürprizi (yalnızca dekor) |
Rota süreleri (hedef): spawn → depo yürüyüş ≤ 20 sn, forklift ile depo → inşaat ≤ 25 sn (≈ 55 m), böylece taşıma döngüsü 60–90 sn.

## 3. Oynanış akışı
1. **Lobi** (menü): oyuncular podyumda adlarıyla görünür (lobi bölümü ayrı iş), oda kodu/hızlı oyun.
2. **Meydana spawn** (serbest oyun başlar). Sözleşme panosu 1–3 sözleşme gösterir (oturum tohumundan çekilir). Oyuncular oylar ya da ev sahibi seçer; **sözleşmeyi hiç seçmeden de oynanabilir** (sandbox).
3. **Sözleşme** (5–12 dk): hedefler (taşı, onar, topla, keşfet...), süre, **kaos olayları** (tetiklenen/rastgele).
4. **Sonuç:** puan + komik ödüller ("En çok yıkan", "En iyi sürücü", "Yük kralı", "Kaza uzmanı"), kozmetik jetonu.
5. **Serbest oyun:** dünya hasarlı kalır; "Onar/Sıfırla" ile yeni tur. (Kalıcı hasar yalnızca oturum içi.)

## 4. Sözleşme sistemi (senaryodan bağımsız, veri odaklı)
**Veri (ScriptableObject `ContractDef`):** `id`, yerelleştirme anahtarları (başlık/açıklama), `type` etiketi (Taşıma, Onarım, Toplama, Keşif, Karma), `objectives[]`, `timeLimit`, `minPlayers/maxPlayers`, `difficulty` ve oyuncu sayısı ölçekleme eğrisi, `spawnRules[]` (hangi prefab havuzundan, kaç tane, hangi zona, hangi malzeme/ağırlık), `chaosEvents[]` (ağırlıklı havuz), `rewards` (puan, kozmetik jetonu).
**Hedef türleri (`IObjective`, sunucuda değerlendirilir):** `DeliverToZone` (nesne etiketi + sayı + bölge), `RepairTarget` (Destructible'ı `Repaired` durumuna getir), `CollectCount`, `ReachAndInteract` (keşif), `SurviveTime`, `DontBreak` (kırma sınırı: bonus/ceza), `Custom` (kod). Hedefler birleştirilebilir ve isteğe bağlı bonus hedefler olabilir.
**Çalışma (ağ):** `ContractDirector` (NetworkBehaviour, sunucu otoriter). Eşitlenenler: `NetworkVariable<byte> state`, `NetworkList<ObjectiveProgress>` (id, ilerleme ushort), kalan süre, puan. Olaylar `ClientRpc` (toast/ses). Geç katılan istemci yalnızca bu değişkenlerden durumu okur.
**Mevcut sistemle ilişki:** `CaseManager` ("Kimin Alet Çantası?") bir **sözleşme türü** olarak yeniden kullanılır (İpucu toplama hedefi); sabit senaryo kaldırılmaz, *havuzda bir sözleşme* olur. Karakter/arayüz hiçbir senaryoya bağlanmaz.
**Çeşitlilik:** havuzdan rastgele sözleşme + rastgele spawn düzeni + kaos olayı = her oturum farklı. Tohum (seed) oda kodundan türetilir (tekrar oynatılabilir/hata ayıklanabilir).
**Başlangıç havuzu (7):** (1) Palet Taşıma (6 palet depoya/inşaata, süre 5 dk) (2) Çit Onarımı (3 panel, malzeme taşı + çekiçle) (3) Işıklar Gitti (sigorta + kablo onarımı, karanlık sahne) (4) Pazar Düzeni (tezgâh ve kasaları yerine diz, kırmadan) (5) Kule Yükü (ağır dolabı forklift ile rampadan çıkar) (6) Kayıp Alet (keşif/toplama; mevcut vaka uyarlaması) (7) Yıkım Ekibi (belirli yapıyı kontrollü yık, sonra yeniden kur — sabotaj/eğlence).
**Sabotaj/rekabet (isteğe bağlı):** sözleşme modifikasyonu "Çatlak İş Birliği": gizli ek hedef (puan çalma) — ilk sürümde kapalı, tasarım ilke olarak hazır.

## 5. Yıkım ve onarım (kapsam: Seviye 1–2, bir tane Seviye 3 vitrin)
- **L1:** kasa, varil, raf, bank, pazar tezgâhı, çöp kutusu, tabela, saksı.
- **L2:** ahşap çit paneli, kapı, pencere, iskele tahtası, tente/branda.
- **L3 (tek vitrin):** inşaattaki **yarım duvar + baraka**: önceden yazılmış çökme (3–4 parça grubu), sunucu "çöktü" durumunu verir.
- Durum makinesi: `Sağlam → Çatlak → Kırık → (Onarıldı)`. Hasar = çarpma darbesi − malzeme eşiği (ahşap düşük, çelik yüksek). Onarım: doğru malzeme taşıma + çekiç etkileşimi (süreli, işbirliğiyle hızlanır).
- Mimari ve ağ kuralları: sunucu yalnızca durum baytını eşitler; parçalar istemcide yerel/havuzdan (≤128 parça, 6–10 sn ömür); geç katılımda kırık hâl doğrudan kurulur. Ses/parçacık malzeme etiketine göre.

## 6. Forklift ve taşıma
- Mevcut forklift (çatal kaldırma, tahmin, çok oyunculu doğrulanmış) korunur; rampa tırmanışı ve devrilme sınırları ayarlanır (rampa ≤ 12°).
- Palet/kasa/dolap ağırlık sınıfları: hafif (elde taşınır), orta (iki kişi), ağır (yalnızca forklift). **Yük kayması:** çatalda kötü dengelenen yük düşebilir (komik/kontrollü).
- Taşıma görevi ana döngü: depo → cadde → inşaat/atölye; kamyon yuvasından yeni palet "teslimatı" olayı.

## 7. Etkileşimli nesneler listesi (kapsam)
Kasa (3 boy), varil (patlamaz, yuvarlanır), palet, raf (devrilir), el arabası, kaykay, bisiklet, forklift, sigorta kutusu, tezgâh, çit paneli, kapı (aç/kapa/kır), pazar tezgâhı, meyve sandığı (dağılan meyve = kayma), halat/zincir (v2), tahterevalli tahtası, yağ lekesi (kaygan), çeşme (suya düşürme: geri yerleştirme).

## 8. Komik fizik anları (tasarlanmış olasılıklar, senaryosuz)
Kasa kulesi devrilir · varil rampadan yuvarlanır · forklift çatalı katapult · raf domino · el arabası zinciri · iki kişinin aynı nesneyi zıt yöne çekmesi · yağ lekesinde kayan oyuncu · yarım duvar yıkımı · kaykayla çit atlama. Kontrol bozulmaz: oyuncu kontrolü her zaman öncelikli, fiziksel "komedi" nesnelerde (kontrollü kaos).

## 9. Dünyayı canlı gösteren öğeler
Yerel/gösterim amaçlı **NPC'ler** (ağ maliyeti yok): sokakta yürüyenler, dükkân önünde duranlar, fırın dumanı, kuşlar, tabelalar, çamaşır, hafif araç trafiği (yalnızca arka plan). NPC'ler fizikle etkileşmez (v1), yalnızca kaçınma/ses tepkisi.

## 10. Uygulama aşamaları
- **M0 Grey-box (1 tur):** `GreyboxBuilder` (Editor, veri tablosundan zonları ilkel + renkli kutularla üretir, 1 m ızgara dokusu), 8 spawn noktası, navigasyon/sürüş testi. *Çıkış:* forklift rotası ≤ 25 sn, sıkışma yok, 8 oyuncu spawn'ı sığıyor.
- **M1 Oynanış:** `ContractDirector` + 3 sözleşme + `Destructible` L1–L2 + pano + sonuç ekranı; iki istemcili test (yıkım durumu senkron).
- **M2 Sanat:** Blender kiti ile gri kutuları değiştir (ışık "Altın saat"), zemin dokuları/decal'lar, prop doluluğu.
- **M3 Cila:** NPC'ler, malzeme sesleri, parçacıklar, kamera/his, animasyon, performans (bütçe: ≤ 1,5 M üçgen, ≤ 250 çizim çağrısı), kalite kapısı.
Her aşama sonunda: çalışan build, ekran görüntüsü, test sonucu, bilinen sınırlar, değişen dosyalar.

## 11. Doğrulama ölçütleri
Grey-box: sıkışma/boşluk yok, rota süreleri, 8 spawn, görüş açıları (meydandan 3 zon görünür). Oynanış: bir sözleşme baştan sona 2 oyuncuyla otomatik botla tamamlanır (kalite kapısına eklenir). Ağ: yıkım durumu A=B=sunucu; geç katılım kırık hâli görür; fizik anomalisi sayacı (nesne ≥ 5 m beklenmedik hareket) 0. Performans: Doku'da Windows hedefi ölçülemez → kullanıcı makinesinde F3 FPS/ping gözlemi + sunucu kare süresi.

## 12. Durum ve kanıtlar (2026-11-05)
Uygulandı: Blender kitinden 44 varlıkla kurulan sahne (`GreenfieldBuilder`), veri odaklı sözleşme sistemi (3 pano, 5 bölge, 5 sözleşme), modüler yıkım/onarım (`Destructible`, `Repairable`, malzeme tablosu, havuzlu yerel enkaz), forklift, elektrik panosu, ağ nesnesi sayısı 87.

Doğrulama (Doku, Linux oyuncusu, Xvfb; `tools/gfshots.sh`, `tools/contract-test.sh`, `tools/mp/run.sh`, `tools/mp/destruct.sh`):
- Sözleşme senaryosu: teklif, başlat, teslim, puan, süre dolunca başarısız, yeniden teklif, çift başlatma reddi → GEÇTİ (istisna 0).
- İki istemcili ağ testi: clean / bad / hell → 17/17.
- Yıkım senaryosu (A fırlatıp kırar, geç katılan C kırık hâli görür, çekiçle onarım): clean / bad / hell → 24/24.
- Ekran görüntüsü turu: 10 kare (`docs/blender-kit-preview/` ve `shots_gf`).

Bulunan ve düzeltilen hatalar:
1. **Sahne bozulması ("level0 is corrupted")**: derlemede rastgele (aynı girdiyle bazen çöküyor). Zaman aşımlı, tekrarlı bisect (aynı varyant 6'şar kez) ile bulundu: `RepairPrompt` sınıfı `Repairable.cs` içinde duruyordu. Sahnede kayıtlı her bileşenin kendi adını taşıyan dosyası olmalıdır. `RepairPrompt.cs` ve `RepairMaterial.cs` ayrıldı → tam sahne 0/6 çökme (önceden 8/8). Onarım tetikleyicisi artık alt nesne değil, köke eklenen devre dışı bir `BoxCollider`; istemi `Carryable` (kırıkken onarım ipucu) ya da kökteki `RepairPrompt` verir.
2. **Başlangıçta kırılan sandıklar**: rastgele yerleşen sandıklar çakışıp 51 m/s'lik itmeyle oyun başlamadan kırılıyordu. Gevşek ızgara yerleşimi + doğuşta 4 sn / onarım sonrası 1,5 sn hasar koruması.
3. Yıkım testi Workshop'un eski sunucu derlemesini kullanıyordu: `BUILD_DIR`/`TOOLS_DIR` parametreleri eklendi.

Bilinen sınırlar: Windows istemcisi gerçek pencerede test edilmedi; 4 oyuncu sınırı (MaxPlayers=4) değişmedi; karakter GLB üçgen bütçesi >8000; sözleşme testi bazen "ikinci sözleşme başlamadı" ile dalgalanıyor (araştırılacak).

