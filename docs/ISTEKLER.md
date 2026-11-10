# Kullanıcı istekleri (yazılı kayıt)

Bu dosya, oyunu geliştiren kişinin sohbet sırasında söylediği tüm istekleri kaybolmasın diye tutar.
Durum: ✅ bitti · ✅ üzerinde çalışılıyor · ⏳ sırada. Her iş bittiğinde burada işaretlenir.
Son güncelleme: 2026-11-04 (0.4.1). ✅ = kodlandı ve otomatik testle ya da ekran görüntüsüyle doğrulandı; gerçek bilgisayarda oyuncu denemesi hâlâ gerekir (özellikle Esc menüsü, tam ekran ve animasyonların hissi).

## 0.4.0 sonrası gelen istekler (oyun denendikten sonra)

| # | İstek (kullanıcının sözleriyle) | Durum |
|---|---|---|
| 1 | "Oyuna uygun kaliteli bir logomuz da olması lazım." | ✅ Logo çizildi (`site/assets/logo.svg`, `icon.svg`); menüye, siteye, kurulum/oyun ikonuna bağlanacak |
| 2 | "Oyunun ilk açıldığı ekranda arkada o küçük kulübeyi görmeyelim, düzgün bi arkaplanımız olsun, ileride başka haritalarımız da olacak." | ✅ Haritadan bağımsız menü arka planı |
| 3 | "Karakter zıplaya zıplaya biniyor (yürüyor), hiç bir adım atamıyor." | ✅ Modellere iskelet + yürüme döngüsü |
| 4 | "Forklifte binince karakter görünmüyor. Binerken üstüne tırmanıp oturması lazım, bi animasyonu da yok." | ✅ Sürücü görünür kalacak; tırmanma ve oturma animasyonu |
| 5 | "Başka animasyonları da olması lazım, put gibi durması çok saçma." | ✅ Bekleme, yürüme, koşma, zıplama, taşıma, fırlatma, vurma, sersemleme, oturma |
| 6 | "Başka karakter seçemiyorum." | ✅ Kullanıcı sonradan seçebildiğini söyledi (menüde "Karakter" düğmesi); oyun içi Esc menüsüne de eklendi |
| 7 | "Oyun içerisinde kamera açısını değiştirebilmemiz lazım." | ✅ Esc menüsünde mesafe/yükseklik/omuz/görüş alanı/fare hızı + `[` `]` eğme |
| 8 | "ESC tuşunda menü çıkmıyor, direk mouse geliyor ekrana." | ✅ Esc duraklatma menüsü (devam, kamera, tam ekran, karakter, menüye dön, çık) |
| 9 | "Oyunu tam ekran da yapamıyorum, hep küçük." | ✅ Varsayılan tam ekran (pencere), F11 / Alt+Enter, menüden de |
| 10 | "Forklift önündeki rampaya asla çıkamıyor, direk takla atıyor. Öndeki çatalı yükseltmeme rağmen çıkamıyor." | ✅ Sürüş fiziği: rampa tırmanma, devrilmeme |
| 11 | "Forklift tekerleri dönmüyor, çok daha modern ve çok daha güzel tasarlanması lazım, tekerler sağa sola da dönebilecek şekilde." | ✅ Yeni forklift tasarımı, dönen ve yönlenen tekerlekler |
| 12 | "Sana örnek başka modeller de vermiştim bisiklet gibi, kaykay gibi. Onlardan da istiyorum." | ✅ Kaykay ve bisiklet (özgün modeller, sürülebilir, çok oyunculu); örnek modeller kullanılmadı |
| 13 | "Tüm bu isteklerimi kesinlikle yazılı not al." | ✅ Bu dosya |
| 14 | "Karakterlerin elleri kolları hareket etmesi lazım." | ✅ Kemikli modellerde kollar yürürken sallanır, taşırken öne uzanır, fırlatırken savrulur |
| 15 | "Bisiklet ve kaykay'a binerken karakterin duruşları da araca uygun olması lazım. Bisiklet sürerken pedal çevrilmesi, kaykay sürerken ara ara bir ayağını yere indirip kendini ittirmesi lazım." | ✅ Kaykayda yandan duruş ve yerden itme animasyonu, bisiklette oturuş ve pedala bağlı bacaklar |
| 16 | "Oyunda baya bi gecikme var. Tuşa basıyorum neredeyse bir buçuk saniye sonra işlem yapıyo. Arkadaşımı izlerken de mesela bisiklet gidiyo arkadaşım arkadan havada gidiyo gibi görünüyo." | ✅ (0.4.3) Araç sürerken tahmin (sürücünün makinesi hemen tepki verir), uzaktaki sürücü araca bağlı çizilir, girdiler 30 Hz, FPS/ping göstergesi (F3), otomatik/elle grafik kalitesi, sunucu 60 kare, game1–3 başlatma çökmesi düzeltildi. Not: kullanıcının hattı Kale ile ~120 ms ve %50 paket kaybı gösterdi; kalan gecikmenin bir kısmı ağdır |
| 17 | "https://gameskill.dev/skills — Buradan bizim oyun için işimize yarayacağını düşündüğün tüm skilleri çekip öğrenmeni istiyorum." | ✅ 230 beceri tarandı, 28 ilgili dosya okundu; özet: docs/OYUN_BECERILERI.md ve docs/STEAM_HAZIRLIK.md; bulgulardan Esc menüsünde oyun girdisi kapatıldı (0.4.4). Üçüncü taraf eklenti kurulmadı |
| 18 | (Becerilerden çıkan, henüz yapılmamış) Ses (efekt, müzik), RPC doğrulama ve hız sınırı, game feel, yerelleştirme | ⏳ Sırada |
| 19 | "Tüm bu skilleri öğrenmeni istiyorum ve tamamını projemizde kullanacağız." (15 beceri: claude-code-game-studios, unity-mcp-coplay, awesome-gamedev-agent-skills, coop-anything, unity-official-skills, unity-agent-plugin, unity-mcp-ivanmurzak, pixelfx, unity-skills-besty, scenario-skills, gamefactory-3a, glade-mcp, everything-game-dev-code, effekseer-vfx, funplay-skill) | 🔧 Üç araştırmacı hepsini okuyor; ilkeler projeye alınır, araç olarak kullanılabilecekler (ekransız Docker ortamına uyanlar) incelenip kullanıcıya sunulur; üçüncü taraf kodu çalıştıran kurulumlar kullanıcı onayıyla ve inceleme sonrası yapılır |
| 20 | "WHO BROKE IT — EXECUTION ORDER 001: P0 (NotServerException, çok oyunculu dünya senkronu, iki istemcili otomatik test), mevcut düzeltmelerin gerçek oyunda doğrulanması, kalite kapıları; yayın/dağıtım yok" | 🔧 Rapor: docs/P0_RAPOR.md; üretimde ölü sunucu hatası bulundu, düzeltme (server/run.sh) onay bekliyor |
| 20 | "WHO BROKE IT — COMPLETE GAME QUALITY TRANSFORMATION": tam kalite dönüşümü (stilize 3B görsel kimlik, yeniden tasarlanmış ana menü [Hızlı Oyun, Arkadaşlarla, Tek Kişilik, Karakter Seçimi, Ayarlar, Profil, Emeği Geçenler, Çıkış + onay], 3B karakter seçim ekranı, Greenfield dünyasının baştan kurulması, fizik/araç/animasyon iyileştirmeleri, 8 fazlı plan, Unity/Blender/UI/animasyon/fizik/ağ/aydınlatma/ses/QA skill'leri kurulup kullanılacak). Örnek görseller (Fall Guys benzeri lobi, hayvan karakter seçimi, koltuktaki kedi-köpek menüsü) yalnızca lobi mantığı ve sanat yönü örneği | 🔧 Faz 1 (denetim) ve Faz 3'ün menü + karakter seçimi + ayarlar + profil + çıkış onayı kısmı yapıldı (0.4.5 çalışma kopyası, Doku'da ekran görüntüsüyle doğrulandı). Dünya (Faz 5), animasyon/fizik (Faz 4, 6), çok oyunculu test (Faz 7) sırada |
| 21 | "Aşçıda neden şüpheli yazıyor? Bu oyun tek bir senaryolu olmayacak... Genel kullanıma çevir oyunu.. Senaryo ve kullanım oyuncularla alakalı." | ✅ Karakter etiketleri tarafsız (İNSAN/ROBOT/HAYVAN), açıklamalarda ve menüde senaryoya özgü ima yok. Oyun içeriği (vaka, suçlu, kim ne yaptı) karaktere/arayüze bağlanmaz |
| 22 | "AUTONOMOUS STUDIO DIRECTIVE — EXECUTION ORDER 004: DURMA, OYUNU KUR" (Greenfield Meydanı, etkileşim mimarisi, sandık/varil/palet/elektrik panosu/atölye ekipmanı için modüler yıkım ve onarım, seçili çevre) + "Ben uyumaya gidiyorum, sen durma, sabaha kadar yapabileceğin her şeyi yap" | 🔧 Greenfield sahnesi, sözleşmeler, yıkım/onarım çalışıyor ve test edildi (docs/GREENFIELD_MEYDANI.md, bölüm 12); 4-8 oyunculu ölçek, Windows istemcisinde gerçek pencere testi ve sürüm yayını bekliyor |
| 23 | Windows istemcisi zip değil çıplak exe olarak, bu laptopta derlenebilir; yeni sürüm yayını ve Kale güncellemesi (0.4.6). | yayınlandı (0.4.6) |
| 24 | Oyun yeniden kontrol: karakterlerin kolları yok / Mühendis gözü sorunlu; oyunda ses ve müzik yok; Emeği Geçenler tamamen kaldırılsın; bisiklet ve kaykaya binişte gecikme ve saçma hareket. | ses (AudioListener) ve Emeği Geçenler yapıldı; sürüş gecikmesi ölçüldü (ağda belirgin hata yok, nedeni araştırılıyor); kollar/göz açık |
| 25 | El arabası kullanılamıyor (tutma ve hareket yok); görevlerde yukarıda ok: alınacak şeyler ve bırakılacak yer gösterilsin; elde kutu varken karakter takılıyor; ekran ortasında nişan noktası (ne alınacağı/tutulacağı hedeflenebilsin); dışarıda NPCler olsun. | kodlandı, Dokuda derleme/test bekliyor |
| 26 | Forklift paletleri kaldıramıyor: çatal yeterince alçalmalı ya da paletler büyümeli; çatal çok alçakken engellere takılmamalı | palet 3 ayak + tabla şeklinde, 1,45x yükseltildi, dayanıklılığı 400, forkMin 0,04; Doku testi hâlâ geçmiyor (forklift yana kayıyor, araştırılıyor) |
| 27 | Karakter kolları hâlâ gövdeye yapışık ve modellerin iki yanı birbirini tutmuyor; bir yanı üretip aynalamak daha mantıklı | AiAssets.Rig: mesh sol yarıdan aynalanıyor, kollar 14° açılıyor; kodlandı, derlenmedi/görülmedi |
| 28 | Çevrimiçi oyunda arkadaşların isimleri başlarının üstünde görünmeli | PlayerNet.playerName + OnGUI etiketi; kodlandı, iki istemci testi bekliyor |
| 29 | Binaların önünde NPC satıcılar (fikir) | 0.4.7'de NpcVendor ile eklendi; oyunda elle denenmedi |
| 30 | Kırılan şeyler çekiçle tamir edilebilmeli | Repairable zaten çalışıyor (DestructSmoke ile doğrulanıyor); oyunda denenmedi |
| 31 | Doku'daki Claude oturumuna sunucu işleri verilsin; skill/plugin'ler orada sıfırdan kurulsun | oturum izin denetimi `plugin marketplace add`'i engelledi, kullanıcı izni bekliyor |
| 32 | "Oyun tamamlanana kadar onay bekleme, durma" | kalıcı: onay sormadan devam edilir (kalıcı kurallar ve güvenlik sınırları hariç) |
| 33 | El arabası gerçekten kullanılabilsin (hareket + yük) | 0.4.9: kök neden bulundu (tekerleklere de tutucu sürtünme verilmişti, araba duruyordu); sadece kasa tutucu; test: yükle, 3,9 m sür, indir geçiyor |
| 34 | 0.4.12 oynayış geri bildirimi (karakter ekranı ve oyun): **Kedi**: kollar düzelmemiş, kafada kocaman delik, çanta ve kuyruk bozuk/eksik. **Neredeyse tüm karakterlerin kafasında delik, gövdelerinde parça parça eksik alanlar** (aynalama düzgün birleşmiyor, modeller aynalamaya uygun üretilmemiş). **İşçi** kolları bozuk | Sebep bizim üst gövde aynalamamız (sol yarı atılıp sağ yarı yansıtılıyor). Aynalama KALDIRILACAK, orijinal mesh'e dönülecek; kollar için doğru kemik ağırlığı (rigging) yapılacak |
| 35 | Sağ tık itme: yalnızca omuz oynuyor ve bozularak ileri uzuyor; **el ile itilmeli**. Süpürge ve çekiç **eldeyken değil omuz üstünde** hareket ediyor | Aletler el kemiğine bağlanacak, itme/vurma kolu doğru animasyonla (eller ileri) yapılacak |
| 36 | Elde nesne tutarak yürüyünce nesne **havada, karakterin içinden geçip arkada kalıyor, geç geliyor**: karakter gerçekte tutamıyor gibi | Taşınan nesne her makinede oyuncunun görsel köküne/ellerine yerel olarak bağlanacak (ağ gecikmesine bağlı olmayacak) |
| 37 | **Bisiklet**: sürerken eller değil **kollar uzuyor**. Bisikletten inince bisiklet **yatay fırlıyor** (düz gitmeli) | iniş anında hız sıfırlanacak; kol ağırlıkları düzeltilecek |
| 38 | **El arabası**: yüklenen malzeme asla arabayla ilerlemiyor, baskı/ağırlık hissedilmiyor; arabayı bırakınca **araba saçma şekilde fırlıyor** | ağ tahmininde araba görseli ile yük farklı pozda: yük arabaya yerel bağlanacak/tahmin kapatılacak; bırakınca hız sıfırlanacak |
| 39 | **Kaykay**: Space basılı tutunca havada uçuyor; olması gereken: **tek atlama**, hatta atlarken kaykayı ayakların altında çevirme (trick) | Space kenar tetikli + bekleme süresi; atlayışta tahta dönüş animasyonu |
| 40 | **GENEL (en önemli)**: "Fizik diye bir şey yok, oyun motoru hissi yok." Çok kaliteli oyun motoru/fizik araştırması yapılacak (karakter kontrolü, tutma, araçlar, ağ üzerinde fizik) ve oyun bu temele oturtulacak | araştırma docs/FIZIK_ARASTIRMASI.md'ye yazılacak, bulgulara göre fizik altyapısı yeniden kurulacak |
| 41 | Karakterleri kollar açık yeniden üret (A şıkkı), NPC karakterleri oyuncu karakterlerinden ayrı yarat; Doku oturumuna devredildi. Piyasadaki en çok kullanılan motorları araştır ve raporla | docs/OYUN_MOTORLARI.md yazıldı; karakter işi Doku oturumunda |
| 42 | Roadmap devamı: telemetri, dış test rehberi, Türkçe/İngilizce yerelleştirme (Doku oturumuyla çakışmadan) | telemetri ve docs/PLAYTEST.md bitti; Loc altyapısı + 212 çeviri + dil düğmesi (menü sağ alt, duraklatma menüsü) bitti, ekranda görülmedi |
| 43 | Karakterler: kollar açık, rig, NPC ayrı set (Doku oturumu); iki kişilik taşıma; yerelleştirme, sohbet, ifadeler, telemetri | 0.4.14 yayında. Bekleyen: kurbağa yeni hatla, npc_kosucu düzeltme, diz kemiği, NPC kol pozları (Doku oturumunda) |
| 44 | Kozmetik şapkalar (MVP S12) | 5 şapka: Parti, Kasket, Silindir, Pervaneli Bere, Taç; 1/2/4/6/10 bitirilen sözleşmeyle açılır; Esc menüsü ve profil sayfasından seçilir; 0.4.16 |
| 45 | Online oyunda arkadaşın karakteri donmuş görünüyor (uzaktan vurulabiliyor, ona hissettiriyor), benim karakterim onun ekranında gecikmeli/donuk | Kale günlüğünde hata ve yük yok (sunucu 60 fps, 14 % CPU). Neden henüz belirsiz. 0.4.18: NetDiag [NET] günlüğü (fps, rtt, uzak oyuncunun pozisyon yaşı). Player.log her iki oyuncudan istendi |

## Daha önce gelen ve sürekli geçerli istekler

- Oyun adı **Who Broke It?**; oyun TEK senaryolu değildir: senaryo ve kullanım oyunculara bağlıdır; arayüz, karakter ve metinler genel kullanımlı olur ("Kimin Alet Çantası?" yalnızca mevcut örnek vaka).
- İlk sürüm hafif olsun; YouTube oyuncularına dağıtılabilir olsun; sonra Steam Erken Erişim.
- Oda kodu + hızlı oyun (IP/VPN yok); kurulum .exe'si `C:\Program Files\CiciByte\WhoBrokeIt`; oyun içi otomatik güncelleme; profesyonel site.
- Tutulan nesneyi arkadaşına fırlatma ve vurma (komik geri itme, hasar yok).
- Birinci şahıs kamera seçilebilir; birden fazla seçilebilir karakter; "Mario gibi olmasın", daha kaliteli görünüm.
- Örnek modeller (`WhoBrokeIt_Models`) yalnızca örnektir, oyunla dağıtılmaz; özgün modeller ileride yapılacak.
- Altyapı: Tailscale; Kale oyuna ayrılmış; grafik işleri Doku/Doku2; Docker; lisans silinmez/kopyalanmaz; ağır iş laptopta çalıştırılmaz (mobil kota); aynı soru tekrar sorulmaz.
- Commit/PR mesajlarına Claude/Anthropic atfı eklenmez.
