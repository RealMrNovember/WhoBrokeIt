# Mutabakat raporu (2026-11-04) — Faz 0

Amaç: iki önceki oturumun (A: P0 kararlılık, B: görsel/arayüz) raporlarını gerçek depo ve test kayıtlarıyla karşılaştırmak. Bu rapor **yalnızca okuma ve güvenli kurtarma etiketi** içerir; hiçbir dosya silinmedi, hiçbir dal birleştirilmedi/sıfırlanmadı, üretime dokunulmadı.

Doğrulama düzeyleri: **U** uygulandı · **D** derlendi · **E** entegrasyon testi (gerçek süreçler, Docker/Doku) · **G** görsel inceleme (ekran görüntüsü) · **O** gerçek oyun penceresinde (Windows) denendi.

## A. Depo ve dal durumu
- Dal: `p0/stability` (HEAD `b27f6d8`), uzak `origin/p0/stability` ile aynı. Çalışma ağacı **temiz** (bekleyen değişiklik yok), stash yok, tek worktree.
- `main` = `origin/main` = `56412e0` (0.4.4, üretimle aynı). `p0/stability`, `main`'in 2 commit ilerisinde, çatışma yok:
  - `91cb6b4` "Kurtarma noktası" — Oturum A'nın P0 öncesi **bekleyen her şeyi tek commit'e topladığı** nokta: NotServerException düzeltmesi, çıkış düğmesi, omuz düzeltmesi, GameAudio, ekran sarsıntısı, kayıplı ağ testi, GLB kalite raporu, 24 beceri dosyası **ve Oturum B'nin o ana kadarki arayüz çalışması (UiKit, menü)**.
  - `b27f6d8` — P0: iki istemcili test düzeneği (`tools/mp`, `TwoClientSmoke.cs`), `server/run.sh` düzeltmesi, `P0_RAPOR.md` **ve Oturum B'nin sonraki menü/karakter seçimi yeniden yazımı (NetworkMenu.cs ≈ 880 satır)**.
- Yani iki oturumun çalışması **aynı ortak çalışma kopyasında** yürümüş ve Oturum A'nın commit'leri ikisini birlikte almış. Hiçbir iş kaybolmamış; ama commit'ler konuya göre ayrılmamış (geriye dönük ayıklama zor).
- Doku test ağacı (`~/whobrokeit/project/WhoBrokeIt`) ile depodaki **tüm 42 .cs dosyası satır sonu normalleştirilerek birebir aynı** (karma karşılaştırması). `TwoClientSmoke.cs` ve `tools/mp` mevcut ve commit'li.
- **Kurtarma noktası:** yerel etiket `recovery/before-vertical-slice-20261104` → `b27f6d8` (itilmedi). Ayrıca iki commit zaten `origin/p0/stability` üzerinde.

## B. P0 düzeltmeleri — gerçekten var mı?
| Madde | Kodda | Test | Düzey |
|---|---|---|---|
| NotServerException (`PlayerNet.ByOwner`, Forklift/RideableVehicle'da kullanılıyor, `GetPlayerNetworkObject` çağrısı kalmadı) | ✅ | 17/17 (clean, bad, hell), 3 günlükte 0 istisna, uzak sürücü 32–33/33 örnekte araca bağlı | U D E |
| Ölü sunucu kök nedeni (Xvfb kilidi) + `server/run.sh` düzeltmesi (`pkill Xvfb`, kilit/soket sil) | ✅ dosya değişti | yerel Docker: eski betik "HAZIR OLMADI", yeni "HAZIR" (`restart-test.sh`, `zombie-client-test.sh`) | U D E |
| Üretime dağıtım | ❌ yapılmadı | — | **onay bekliyor** |
| Ana menü çıkış düğmesi (onaylı) | ✅ `NetworkMenu.QuitGame` + Esc menüsü | Linux'ta ekran görüntüsü | U D G (Windows'ta çıkışın gerçekten kapattığı **denenmedi**) |
| Omuz/kol ağırlığı, ekran sarsıntısı | ✅ kodda | görülmedi | U D |
| Ses (GameAudio) | ✅ kodda (+ menü sesleri/müziği Oturum B) | **hiç dinlenmedi** | U D |
| Tek seferlik fizik anomalisi (4 koşunun 1'inde A 1,6 m havaya fırladı) | — | tekrar etmedi, çarpışma adı günlüğü eklendi | açık |

## C. Arayüz — gerçekten var mı?
Hepsi 857 satırlık `NetworkMenu.cs` + `UiKit.cs` içinde ve **Doku'da Linux build'i derlenip sekiz ekranın ekran görüntüsü alındı** (ben bizzat inceledim; kaydı `~/whobrokeit/project/shots_menu`):
- Ana menü (8 madde), çıkış onayı, karakter seçimi (3B önizleme, 6 küçük resimli kart, ◀ ▶, sürükleyerek döndürme, Onayla), ayarlar (3 ses kaydırıcısı, kamera, ekran, grafik), profil, emeği geçenler, arkadaşlarla oyna/oda kodu, bağlanma ekranı ve iptal, UI sesleri ve menü müziği.
- **Düzey: U D G (Linux).** Windows oyuncusunda, gerçek fare/klavye/gamepad, gerçek ekran çözünürlüklerinde **denenmedi**. Ses ve müzik duyulmadı. Bağlanma/zaman aşımı akışı gerçek pencerede görülmedi.
- Eksik: oyuncu adı kaydediliyor ama oyunda gösterilmiyor; TR/EN yerelleştirme yok (metinler koda gömülü Türkçe); gamepad desteği yok; çok oyunculu lobi (oyuncuların birbirini görmesi) yok; sahne pozlaması hâlâ yer yer fazla parlak.
- Doğrulanmayan küçük not: çıkış penceresinin son metin değişikliği derlenmedi (yalnızca bir dize).

## D. Gerçekten geçen testler (kayıtlar Doku'da `~/whobrokeit/mp/*`, `logs/mp-run.txt`)
- Docker'da iki istemci + sunucu senaryosu, ağ profilleri `clean`, `bad`, `hell` (tc netem): **17/17 üç profilde**; `hell` koşusu: dünya 1,8–2,0 sn'de hazır, 45 nesne, A=B=sunucu, forklift ortalama fark 0,05 m, sandık son konum farkı 0,00 m. (Ben yeniden **koşturmadım**; kayıtları okudum.)
- Önceki oturumlardan: host duman testi ve çevrimiçi duman testi 0.3.1/0.3.2'de geçmişti (eski kayıt, bugün yeniden doğrulanmadı).
- Bu oturumda ben: Linux sunucu/ekran görüntüsü derlemesi hatasız, menü ekran görüntüleri alındı. Başka test koşmadım.
- **Hiç denenmemiş:** Windows oyuncusu (üç oturum boyunca), 4'ten fazla oyuncu, gerçek 8 oyunculu yük, ses, gamepad, kurulum sihirbazı ve otomatik güncellemenin kendini yenilemesi, yeniden bağlanma.

## E. Üretim riskleri
1. **Üretim 0.4.4 ve `server/run.sh` düzeltmesi dağıtılmadı.** P0 raporuna göre oda sıfırlanınca sunucu "ölü" kalıyor; raporda game1–4 bozuk deniyordu. **Bugünkü canlı durumu bu oturumda kontrol etmedim** (Kale'ye dokunmadım).
2. Yayın otomasyonu: Doku `release.sh` → Kale `deploy.sh`. Bu iki komut yalnızca onayla çalıştırılmalı.
3. `MaxPlayers = 4` (`ConnectionGate`), hedef 4–8; 6 sunucu x 4 kişi ve bant genişliği/NetworkTransform maliyeti 8 kişi için ölçülmedi.
4. Karakter GLB'leri (8 MB, `Assets/ThirdParty/Generated`) **depoda yok** (yalnızca Doku'da); yedek yok. Kaynak: SDXL + TRELLIS, geçici içerik.
5. Birden çok oturum aynı çalışma kopyasını eşzamanlı düzenledi (ortak dosyalar, Doku'ya elle kopya). Dallar/commit'ler konuya göre ayrılmadı. Sonraki işler için tek yazar kuralı veya ayrı worktree gerekir.
6. `.claude/skills/` içinde üçüncü taraf metinler commit'li (Apache-2.0, Unity Companion License); özel depoda sorun yok, ama depo açılırsa lisans/atıf gözden geçirilmeli.
7. Bellek notları 4 oyuncu/Steam MVP'yi anlatıyor; yeni vizyon (8 oyuncu, yıkım, dünya) ile çelişen eski kapsam belgeleri (`MVP.md`, `ROADMAP.md`) güncellenmeli.

## F. Mevcut mimari
- Unity 6000.0.32f1, URP 17, NGO 2.2.0 + Unity Transport, glTFast, yeni Input System. **Tek sahne** (`Workshop.unity`) tamamen `ProjectSetup.BuildWorkshopScene()` ile **kodla üretiliyor** (ilkel nesneler + 5 AI modeli + prosedürel dokular). Elle düzenlenen sahne/prefab yok.
- Ağ: sunucu-otoriter; host veya Linux "dedicated" (Xvfb altında normal oyuncu build'i). Oyuncu `OwnerNetworkTransform`; fiziksel nesneler `NetworkTransform`+`NetworkRigidbody` (sunucuda), 45 dünya nesnesi. Bağlantı onayı sürüm + 4 kişi sınırı. Araç sürerken istemci tahmini (`VehiclePrediction`).
- Çevrimiçi: Kale'de Python matchmaker (`/api`), 6 oyun sunucusu (UDP 7771–7776), oda kodu ve hızlı oyun; 120 sn boş oda sıfırlanır.
- Oyun: `CaseManager` (tek sabit vaka), `Carryable`, `CarrySystem`, `ToolController` (süpürge/çekiç, fırlatma, vurma), `Forklift`, `RideableVehicle` (kaykay/bisiklet), `CharacterFactory/Animator` (prosedürel kemik animasyonu), `GameAudio` (kodla üretilen sesler).
- Arayüz: IMGUI (`UiKit`, `NetworkMenu`, `PauseMenu`); 3B lobi özel kamerayla RenderTexture'a çiziliyor (RT örnek sayısı ve MSAA, URP ile uyumlu olmalı — Linux'ta bunun yüzünden lobi siyah kalmıştı).
- Yıkım/hasar altyapısı **yok**; fizik ayarları varsayılan; katman çarpışma matrisi açık; ses kodla üretilen basit dalga şekilleri.
- Derleme/yayın: Doku (Docker + Unity), Kale (site + sunucular), Doku2 (GPU).

## G. Önerilen dikey dilim — "Greenfield Meydanı"
Tek, çok cilalı, ~60x60 m açık-iç karışık alan; veri odaklı, **senaryo bağımsız** (görevler "sözleşme" olarak oyunculara/oturuma göre seçilir):
- **Yer:** köşe dükkânları (fırın, hırdavatçı) ve renkli cepheler, depo + yükleme rampası (forklift, palet rafları), açık atölye, küçük çeşme/saat kulesi (landmark), iskeleli yapı alanı, kaldırım, bank, lamba, çit; gün batımı ışığı, tutarlı stilize renk paleti.
- **Etkileşimli nesneler:** kasa, varil, palet, raf, kapı, çit paneli, sigorta kutusu, el arabası, kaykay/bisiklet, forklift.
- **İşbirliği etkinliği:** "Sözleşme" sistemi (veri): (1) Taşıma — paletleri süre içinde depoya götür; (2) Onarım — kırık çit/kapıyı malzeme+çekiçle onar. Mevcut vaka bir sözleşme türü olarak kalır.
- **Yıkım mekaniği (Seviye 1–2):** kırılan kasa/raf/varil ve çit paneli/kapı; forklift veya düşen yükle kırılır, çekiç+malzemeyle onarılır.
- **Dünya hayatı:** hafif NPC'ler (sokakta yürüyen, dükkân önünde duran; tamamen yerel/gösterim amaçlı, ağ maliyeti sıfır).
- **Akış:** menü → lobi (oyuncular podyumda, adlarıyla) → sözleşme seç → oyun → sonuç → lobi.

## H. Varlık ve yıkım hattı
**Varlık hattı:** Blender'ı Doku'da komut satırından (headless) kullanan, stil kılavuzlu **modüler kit** (duvar, kapı, pencere, çit, raf, kasa, palet, varil, mobilya) ve **ortak renk paleti/trimsheet** (az malzeme, tek SRP batcher). Poligon bütçesi: prop 300–2k üçgen, bina modülü 2–6k. TRELLIS (SDXL) organik karakterler için kalır; yıkılabilir/modüler yapı için uygun değil (tutarsız dokular, yüksek poligon). Hazır paket (CC0: Kenney, Quaternius vb.) hız için kullanılabilir; **indirme için sizden açık onay istenecek**, dosya adı/kaynak/boyut belirtilerek. Her varlık için `[WBI] qa` raporu (mevcut GLB raporunun genişletilmişi).
**Yıkım mimarisi (ölçeklenebilir):**
- `Destructible` (NetworkBehaviour): sunucuda hasar = çarpma darbesi (impulse) − eşik; durum `NetworkVariable<byte>` (Sağlam→Çatlak→Kırık[→Onarıldı]). Yalnızca **anlamlı durum** senkronlanır.
- Kırılınca sunucu `ClientRpc` yollar (konum, yön, malzeme, şiddet); parçalar **her istemcide yerel**, havuzdan (64–128 parça), ömür 6–10 sn, sonra küçülüp havuza döner; **parçalar ağda senkronlanmaz**. Geç katılan istemci yalnızca durum baytını görür (kırık hâl).
- Seviye 1: tek mesh + önceden kırılmış (Blender cell-fracture, ≤8 parça) prefab. Seviye 2: modüler panel (kapı/çit/pencere) hasar durumları + onarım. Seviye 3–4: araştırma aşaması, maliyet ölçülmeden yapılmaz.
- Malzeme etiketi (ahşap, metal, beton, cam, plastik) → ses, parçacık ve hasar eşiği tablosu (ScriptableObject).
- Bütçe: etkin rigidbody üst sınırı, uyku eşikleri, katman çarpışma matrisinin kısılması, sunucu fizik süresi ölçümü, kare başına en fazla N kırılma.

## I. Öncelikli yürütme planı
0. ✅ Mutabakat (bu rapor) + kurtarma etiketi.
1. **Kullanıcı onayı gerektiren:** (a) üretime `run.sh` düzeltmesi (yalnızca `server/run.sh` ve sunucu yeniden başlatma; istemci sürümü değişmez); (b) Windows build'ini sizin makinenizde bir kez açıp bakmanız (87 MB, kotalı internet nedeniyle Tailscale/Doku üzerinden isteğe bağlı). Onay gelmeden dokunulmaz.
2. Faz 1 kalanı: matchmaker ölü yuvayı listeden çıkarıyor mu (salt okunur inceleme + yerel test); tek komut kalite kapısı `tools/mp/gate.sh` (derleme + 17 denetim + istisna taraması + 8 oyunculu bağlantı testi).
3. Faz 2: Windows oyuncu doğrulaması (çıkış, menü, ses, gamepad) + otomatik menü/Esc testi.
4. Faz 3: sanat yönü belgesi (palet, oran, poligon bütçesi, ışık ön ayarı), Blender headless kit hattı ve varlık QA raporu.
5. Faz 4: Greenfield Meydanı dikey dilimi (yukarıdaki G), önce gri kutu yerleşim, sonra kit varlıkları, ışık, NPC'ler.
6. Faz 5: `Destructible` Seviye 1–2, havuz, malzeme tablosu, ses/parçacık, iki istemcili senkron testi.
7. Faz 6: karakter animasyon karışımı/IK, araç hissi, ses sistemi (malzemeye özel).
8. Faz 7: sözleşme sistemi, tekrar oynanabilirlik (rastgele olaylar, kozmetikler).
9. Faz 8: 8 oyuncu yükü, yerelleştirme (TR/EN), gamepad, performans, QA raporu.
Her faz sonunda: çalışan build, ekran görüntüsü/video, test sonuçları, bilinen sınırlar, değişen dosya listesi.

## Değişiklikler bu oturumda
Yalnızca: `docs/MUTABAKAT_RAPORU.md` (yeni) ve yerel etiket `recovery/before-vertical-slice-20261104`. Commit yapılmadı, itme yapılmadı, üretime ve Kale'ye dokunulmadı.
