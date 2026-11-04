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

## Daha önce gelen ve sürekli geçerli istekler

- Oyun adı **Who Broke It?**; oyun TEK senaryolu değildir: senaryo ve kullanım oyunculara bağlıdır; arayüz, karakter ve metinler genel kullanımlı olur ("Kimin Alet Çantası?" yalnızca mevcut örnek vaka).
- İlk sürüm hafif olsun; YouTube oyuncularına dağıtılabilir olsun; sonra Steam Erken Erişim.
- Oda kodu + hızlı oyun (IP/VPN yok); kurulum .exe'si `C:\Program Files\CiciByte\WhoBrokeIt`; oyun içi otomatik güncelleme; profesyonel site.
- Tutulan nesneyi arkadaşına fırlatma ve vurma (komik geri itme, hasar yok).
- Birinci şahıs kamera seçilebilir; birden fazla seçilebilir karakter; "Mario gibi olmasın", daha kaliteli görünüm.
- Örnek modeller (`WhoBrokeIt_Models`) yalnızca örnektir, oyunla dağıtılmaz; özgün modeller ileride yapılacak.
- Altyapı: Tailscale; Kale oyuna ayrılmış; grafik işleri Doku/Doku2; Docker; lisans silinmez/kopyalanmaz; ağır iş laptopta çalıştırılmaz (mobil kota); aynı soru tekrar sorulmaz.
- Commit/PR mesajlarına Claude/Anthropic atfı eklenmez.
