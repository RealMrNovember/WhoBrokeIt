# Who Broke It? — MVP (v0.2): Steam Erken Erişim'e giden en kısa yol

**Tek soru:** Bir grup arkadaş oyuna girince ilk 15–20 dakikada gülüyor mu, beklenmedik şeyler yaşıyor mu, birbirine ihtiyaç duyuyor mu, tekrar açmak istiyor mu?
Bu soruya "evet" diyemeyen hiçbir özellik MVP'ye girmez. `WhoBrokeIt.md` (GDD) ve `docs/STORY.md` uzun vadeli vizyondur; MVP onların **kesitidir**, bütünü değil.

---

## 1. MVP'nin kesin kapsamı

### 1.1 Ürün tek cümleyle
**1–4 kişilik, Steam daveti ile katılınan, tek küçük bölgede (Greenfield "Atölye Avlusu") geçen, ~15–20 dakikalık bir kooperatif fizik-komedisi vakası: "Kimin Alet Çantası?"**

### 1.2 Bölge (tek)
**Atölye Avlusu** = Reclaimer Yard'ın bir köşesi + Old Workshop + bir sokak parçası + su kulesi silueti (arka planda, tepesinde forklift: kimlik/merak öğesi).
Yaklaşık 60×60 m. Düşük poligon, renkli, sabit aydınlatma (baked), tek gün-batımı hissi.

### 1.3 Oyuncu kapasitesi
- **Tasarım hedefi: 4 oyuncu.** Test edilecek üst sınır: 6.
- Host oyuncu = sunucu (listen server). Daha büyük kapasite ve dedicated sunucu **sonraki sürüm** (mimari buna hazır, bkz. §2.3).

### 1.4 Tek oynanış döngüsü: "Atölyeyi Aç"
1. **Giriş (≤1 dk):** Kamyondan iniş, telsiz (Fitil) 3 cümle, su kulesindeki forklift görünür, oyun başlar. Sinematik yok.
2. **İncele (3–4 dk):** 3 sabit ipucu bul (sigorta kutusunda börek, kapıda çamurlu ayak izi, kopuk kablo ısırığı). İpuçları kasaba defterinde birleşir; **sabit** sonuç (kontrollü, rastgelelik yok).
3. **Temizle (3–4 dk):** Süpürge + taşıma ile enkazı toplama alanına it/taşı. Fizik komedisi burada başlar.
4. **Onar (3–4 dk):** Sigorta kutusu iki kişi ister (biri kolu tutar, biri çekici kullanır). Yanlış sırada yaparsanız kıvılcım/kaza.
5. **Taşı (3–4 dk):** Ağır alet dolabını forklift veya el arabası ile atölyeye taşı; yük düşebilir, devrilebilir, **kurtarılabilir**.
6. **Finale (1–2 dk):** Işıklar yanar, atölye "canlanır", telsiz kısa bir ifşa + "Kimin Alet Çantası?" cevabı, sonuç ekranı (esprili ödüller).

### 1.5 Mekanikler (MVP'ye GİREN)
| # | Mekanik | Neden |
|---|---|---|
| 1 | Fizik tabanlı karakter hareketi (yürü, zıpla, eğil, it), hafif "wobble" | kimlik/komedi |
| 2 | Al-taşı-bırak (tek kişi) ve **ikili taşıma** (ağır nesne) | iş birliği |
| 3 | Süpürge (itme alanı) | kaos + keyif |
| 4 | Tamir çekici + **iki kişilik sigorta kutusu** | birbirine ihtiyaç |
| 5 | El arabası | taşıma kaosu |
| 6 | **Forklift** (imza araç): sür, kaldır, yük düşür | komik an üreteci |
| 7 | İncele (E) → ipucu → defter | merak |
| 8 | Emote'lar (el salla, işaret et, high-five) + bas/it | sosyal komedi |
| 9 | Kaza kurtarma: "sıfırla" (takılan nesne/araç), checkpoint | sinir bozmayan |
| 10 | Davet: Steam arkadaş daveti + oda kodu; lobi (hazır/ayrıl); metin sohbeti | arkadaşla oynama |
| 11 | Basit HUD, ses, müzik, sonuç ekranı | bütünlük |

### 1.6 İlerleme ve kalıcılık (MVP seviyesi)
- **Dünya durumu:** vaka tamamlandı mı, ışıklar açık mı → **host'un yerel kayıt dosyası** (backend yok).
- **Oyuncu:** yerel kayıt; 3–5 kozmetik (şapka) isteğe bağlı hedeflerle açılır. Ekonomi/seviye yok.
- Backend, hesap, sunucudan kalıcı dünya: **sonraki sürüm**.

### 1.7 MVP'de OLMAYANLAR (açıkça kesilenler)
Dinamik suçlu sistemi · oyuncu kazalarının vaka olarak aktarılması · birden çok bölge/vaka · gelişmiş ekonomi/envanter · karakter editörü · inşa sistemi · görev tahtası/instance'lar · sesli sohbet · dedicated sunucu · hesap/backend · tam lokalizasyon (**MVP: metin anahtarları + Türkçe/İngilizce minimum**) · NPC'ler · hava/gün döngüsü · basınçlı yıkayıcı (M3'e "olsa iyi olur").

### 1.8 "Bitti" tanımı (Steam'e çıkmaya hazır)
- Windows build, temiz makinede kurulumsuz açılır; Steam Overlay + davet çalışır.
- 4 kişilik oturum **20 dakika kesintisiz** (çökme/kilitlenme yok) tamamlanır.
- Soft-lock yok: her nesne için "sıfırla" yolu.
- 5–10 kişilik dış oyun testinden **olumlu** sonuç (bkz. §4 ölçütleri).
- Steam mağaza sayfası, kapsül görselleri, trailer, 5+ ekran görüntüsü hazır.

---

## 2. Teknik ve üretim planı

### 2.1 Seçimler (sebepleriyle)
| Konu | Seçim | Neden |
|---|---|---|
| Motor | **Unity 6.3 (6000.3.0f1), URP** | kurulu, hızlı, GDD ile uyumlu |
| Ağ | **Netcode for GameObjects (NGO)** + **Steam transport (Steam P2P/relay)** | Steam daveti/lobi ile doğal, relay/backend maliyeti yok, host-sunucu |
| Steam | **Steamworks (Facepunch.Steamworks)**; geliştirme sırasında test AppID 480 | lobi, davet, rich presence, başarım |
| Yetki modeli | Etkileşilen **her şey sunucu-otoriter** (host), oyuncu hareketi istemci-tahmini | sonradan dedicated'a çevrilebilir |
| Fizik | PhysX, **az sayıda** ağ-senkronlu rigidbody (≤40), geri kalan yerel dekor | ağ yükünü sınırlar |
| Varlıklar | CC0 paketler (Kenney, Quaternius) + minimal Blender; hepsi `ASSET_CREDITS.md`'de | hız + hukuki güvenlik |
| Ses | CC0 SFX/müzik (Kenney, Freesound CC0), kayıt altına alınır | hız |
| Build | **Doku2'de Docker (GameCI) ile Windows build**, laptopta ağır iş yok | kullanıcı kuralı |
| Kod | C#, ScriptableObject'lerle tanımlar (vaka, ipucu, nesne), küçük modüller | sonradan dinamik vakaya genişler |
| Dağıtım (test) | `whobrokeit.cicibyte.com/builds` zip + Steam playtest anahtarı | Steam onayını beklemeden |

### 2.2 Üretim aşamaları (her birinin sonunda ÇALIŞAN çıktı)
> Süreler tahmindir (günlük, kesintisiz çalışma varsayımı); gerçekleşenle güncellenecek.

| Aşama | İçerik | Çıktı (kabul ölçütü) | Tahmin |
|---|---|---|---|
| **P0 Temel** | Unity lisansı → Doku2'de proje + CI build; karakter hareketi, kamera, test alanı, tek etkileşim | Doku2'den üretilmiş **çalışan Windows .exe**: yürü, zıpla, bir nesneyi it/al | 3–5 gün |
| **P1 Tek kişilik çekirdek** | al/taşı, süpürge, çekici, el arabası, forklift, ipucu→defter, Atölye grayboxu | Tek kişi **Atölye döngüsünü baştan sona** bitirir | 1–2 hafta |
| **P2 Çok oyunculu temel** | NGO + Steam lobi/davet, oyuncu senkron, nesne yetkisi, ikili taşıma, sigorta kutusu (2 kişi), bağlantı kopması | **2–4 gerçek makinede** döngü bitirilir; sinsi bug yok | 1–2 hafta |
| **P3 Eğlence + içerik** | Karakter wobble/emote, forklift fiziği ayarı, kaza zincirleri, ses/müzik, sanat geçişi, sonuç ekranı, ödüller | Test grubu **güldüğünü ve tekrar istediğini** söyler (iç test) | 2 hafta |
| **P4 Dış test** | 5–10 kişilik playtest + YouTuber paketi, telemetri ve anket, düzeltmeler | §4 ölçütleri geçti; kritik hata 0 | 1 hafta |
| **P5 Steam** | Mağaza sayfası, görseller, trailer, build yükleme/inceleme, Erken Erişim ayarları | Mağaza sayfası canlı → yayın | paralel + ~1–2 hafta |

**Toplam tahmin:** ~7–9 hafta, Steam inceleme/bekleme süreleri hariç. Steam mağaza sayfası **P1'den itibaren paralel** başlatılır (bekleme süresi uzun olabilir).

### 2.3 Çok oyunculu mimari (baştan doğru, ama küçük)
- **Tüm oyun mantığı sunucu-otoriter** yazılır (host sunucudur). İstemci sadece girdi/istek gönderir.
- Taşıma katmanı **soyut** (Steam transport şimdi; sonra UTP/dedicated). `-batchmode -nographics` ile dedicated başlatma **ilk günden** desteklenecek şekilde kodlanır; MVP'de kullanılmaz.
- **Kapasite:** tasarım 4, test üst sınır 6. Dedicated + bölge/instance ölçeklemesi (Kale'de Docker, UDP 7770–7789) Erken Erişim sonrası.
- **Durum kaydı** ayrı bir `SaveService` arayüzü: bugün yerel dosya, yarın backend.

### 2.4 Altyapı (mevcut)
- Build/grafik: **Doku2** (Docker, Unity 6000.3.0f1 imajı hazır) + Doku (paralel).
- Site/indirme/ileride dedicated: **Kale** (`/srv/whobrokeit`, Docker, `whobrokeit.cicibyte.com`).
- Kaynak: GitHub özel repo `RealMrNovember/WhoBrokeIt` (push Kale2 üzerinden).
- Sırlar Kale2'de `/root/.secrets`; repoya girmez.

---

## 3. Öncelik sırası (şimdiden Steam'e)

### Kritik yol (bloklayıcılar — hemen başlat)
1. **Unity lisansı (.ulf)** → Doku2'de aktivasyon. *(Sende: license.unity3d.com/manual'a `Unity_v6000.3.0f1.alf` yüklenecek.)*
2. **Steamworks hesabı + uygulama kaydı** (kimlik/vergi/banka doğrulaması, uygulama ücreti). Bekleme süreleri uzun olabilir; **P0 ile aynı gün başlamalı.** *(Sende: hesap sahibi olarak. Güncel ücret/bekleme koşullarını Steamworks dokümanından doğrula.)*
3. **Steam adı / fiyat / hedef kitle kararı** (Erken Erişim, fiyat bandı).

### Yapım sırası (M = Must, S = Should, C = Could)
| Sıra | İş | Öncelik |
|---|---|---|
| 1 | Unity projesi + Doku2 CI build (P0) | M |
| 2 | Karakter, kamera, fizik hissi (wobble, it, zıpla) | M |
| 3 | Al/taşı/bırak + ikili taşıma | M |
| 4 | Süpürge, çekici, el arabası | M |
| 5 | Forklift (sür, kaldır, yük) | M |
| 6 | İncele→ipucu→defter, sabit vaka | M |
| 7 | **NGO + Steam lobi/davet**, oyuncu senkron, nesne yetkisi | M |
| 8 | Sigorta kutusu (2 kişilik), kaza zincirleri | M |
| 9 | Atölye avlusu sanatı (düşük poligon), ışık, ses, müzik | M |
| 10 | Sıfırla/checkpoint, kopma yönetimi | M |
| 11 | Emote, bas/it, high-five | S |
| 12 | Sonuç ekranı + esprili ödüller + 3–5 kozmetik | S |
| 13 | Türkçe+İngilizce metin anahtarları | S |
| 14 | Basınçlı yıkayıcı, ek kaza olayları | C |
| 15 | Telemetri (oturum süresi, tamamlama, kopma, sıfırlama) | S |
| 16 | Steam sayfası, trailer, ekran görüntüleri, playtest anahtarları | M (paralel) |

**Kesme kuralı:** Zaman daralırsa önce C, sonra S düşer; M asla.

---

## 4. Oynanabilirlik ölçütleri (P4 kapısı)
5–10 kişilik dış test (3–4 grup, her grup ilk kez oynar, yardım yok):
| Soru | Geçme ölçütü |
|---|---|
| 20 dk içinde döngüyü bitirebildiler mi? | ≥%70 grup |
| Gülme/şaşırma anı sayısı (gözlem) | grup başına ≥3 |
| "Birbirimize ihtiyaç duyduk" | ≥%80 oyuncu |
| "Tekrar oynarım / arkadaşa öneririm" (anket) | ≥%60 |
| Çökme veya soft-lock | 0 kritik |

Geçilmezse P3'e dönülür; Steam'e çıkılmaz.
