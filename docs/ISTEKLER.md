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

## Daha önce gelen ve sürekli geçerli istekler

- Oyun adı **Who Broke It?**; senaryo ("Kimin Alet Çantası?", Greenfield atölye avlusu) şimdilik aynı kalır.
- İlk sürüm hafif olsun; YouTube oyuncularına dağıtılabilir olsun; sonra Steam Erken Erişim.
- Oda kodu + hızlı oyun (IP/VPN yok); kurulum .exe'si `C:\Program Files\CiciByte\WhoBrokeIt`; oyun içi otomatik güncelleme; profesyonel site.
- Tutulan nesneyi arkadaşına fırlatma ve vurma (komik geri itme, hasar yok).
- Birinci şahıs kamera seçilebilir; birden fazla seçilebilir karakter; "Mario gibi olmasın", daha kaliteli görünüm.
- Örnek modeller (`WhoBrokeIt_Models`) yalnızca örnektir, oyunla dağıtılmaz; özgün modeller ileride yapılacak.
- Altyapı: Tailscale; Kale oyuna ayrılmış; grafik işleri Doku/Doku2; Docker; lisans silinmez/kopyalanmaz; ağır iş laptopta çalıştırılmaz (mobil kota); aynı soru tekrar sorulmaz.
- Commit/PR mesajlarına Claude/Anthropic atfı eklenmez.
