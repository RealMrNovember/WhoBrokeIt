# Oyun motorları araştırması

Tarih: 2026-11-06. Yöntem: 4 genişletilmiş + 6 standart web araması, 11 sayfanın doğrudan okunması (resmi dokümanlar ve Wikipedia bilgi kutuları). Her bilgi "doğrulandı" (sayfayı kendim okudum), "özet" (arama özetinden, tam sayfa okunmadı) veya "bilgi" (benim genel bilgim, bu araştırmada doğrulanmadı) olarak işaretli. Bulamadıklarım en sonda ayrı listelendi.

## 1. Piyasada en çok kullanılan motorlar

### 1.1 Geliştirici anketi: GDC 2026 State of the Game Industry (2.300+ yanıt)
- Birincil motor: **Unreal %42, Unity %30**. Unreal'ın bu ankette ilk kez Unity'yi geçtiği söyleniyor. Birincil motor ölçütü doğrulandı ([cinevva](https://app.cinevva.com/news/2026-03-13-gdc-2026-by-the-numbers.html)), orijinal GDC raporuna ulaşamadım.
- Godot %11 ve özel/kendi motor %19 rakamları ikinci el yazılardan (özet), ölçütleri (birincil mi, kullanılan mı) kaynaklarda karışık ([respawn](https://respawn.outlookindia.com/amp/story/gaming/gaming-originals/2026-gdc-survey-exposes-workforce-strain-and-technological-shifts), [gamegen](https://www.gamegen.com/unity-vs-unreal-engine-vs-godot-which-game-engine-should-you-learn-first-in-2026)).
- Unreal, büyük stüdyolarda daha yaygın: AA %59, AAA %47 (özet).
- Perforce anketi (Eylül 2025, özet): herhangi bir kullanım olarak Unreal %65, Unity %53 ([pocketgamer.biz](https://www.pocketgamer.biz/cross-industry-game-engine-adoption-surges-led-by-unreal-unity-and-godot)).

### 1.2 Steam verisi: Video Game Insights "Big Game Engines Report of 2025" (2024 verisi, 13.000+ oyun)
Rapora ve PDF'ine doğrudan ulaşamadım, rakamlar özetlerden ([sensortower](https://sensortower.com/blog/the-big-game-engines-report-of-2025), [vginsights PDF](https://vginsights.com/assets/reports/The_Big_Game_Engines_Report_of_2025.pdf), [gamedevreports](https://gamedevreports.substack.com/p/video-game-insights-game-engines)):
- Çıkan oyunların motoru: **Unity ~%51, Unreal ~%28, Godot ~%5, GameMaker ~%4**, özel motor ~%13.
- Gelir payı: **Unreal ~%31, Unity ~%26** (2018'den beri ilk kez Unreal önde). Özel motorlar ~%41–42 ile hâlâ en büyük tek grup ama ilk kez yarının altına indi (kaynaklar 41 ve 42 arasında ayrışıyor).
- Godot, GameMaker, RPG Maker gibi herkese açık motorlar çıkışların ~%10'u, satılan kopyaların yalnızca ~%2'si.
- Özel motor örnekleri raporda: Frostbite, RAGE, Creation Engine. Özel motorların Unreal'a kaydığı öngörülüyor.
- Başka bir izleyici (GameDiscover): Unity 2025'te ~%49,4; Godot %0,9 (2020) → %7,1 (2025) ([özet](https://newsletter.gamediscover.co/p/hows-pc-game-engine-usage-changing)).

### 1.3 Okuma
Adet olarak Unity (özellikle küçük ve orta oyunlar, mobil), gelir ve büyük bütçede Unreal, büyüme hızında Godot. Kendi motoru olan büyük stüdyolar Unreal'a doğru kayıyor. Rakamlar ölçüte göre değişiyor, tek bir "pazar payı" yok.

## 2. Bizim türümüz için emsaller (kooperatif/fizik komedisi)
Wikipedia bilgi kutularını doğrudan okudum (doğrulandı):
- **Lethal Company: Unity** (bilgi kutusu; 2023 Ekim erken erişim, Ocak 2024'e kadar 10 milyondan fazla kopya tahmini).
- **Gang Beasts: Unity.**
- **Human: Fall Flat: Unity.**
- **R.E.P.O.: Unity** (metin: "developed in the Unity game engine"; PC Gamer'dan alıntı: "Never occurred to me Lethal Company was missing physics until I played this").
- **Peak: Unity.**
- Content Warning ve Phasmophobia için sayfaya ulaşamadım / sayfada yazmıyordu. Benim bilgim Unity olduğu yönünde, doğrulanmadı.
Sonuç: bu türün doğrulanmış büyük örneklerinin hepsi Unity. Godot ile gönderilmiş küçük bir ortak oyun var (Pratfall, 80.lv söyleşisi, özet).

## 3. Motor motor değerlendirme (3B fizik, ağ, lisans)

| Motor | 3B fizik | Ağ ve fizik | Lisans | Bu tür için |
|---|---|---|---|---|
| **Unity 6** | Dahili PhysX tabanlı Rigidbody (bilgi). Bizim kullandığımız. | NGO tam istemci tahmini ve geri sarma **sunmuyor**: "doesn't support full client-side prediction and reconciliation", "full, production-ready prediction loop... advanced users only" (doğrulandı, [resmi manuel](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.7/manual/advanced-topics/client-anticipation.html)). Sunduğu: `AnticipatedNetworkVariable/Transform` ile basit "tahmin et, sunucu düzeltsin" modeli. Sayfa fizik veya rigidbody'den hiç söz etmiyor. Alternatifler: Fish-Net, Mirror, Photon (özet: hepsi "production-ready" deniyor). | Pro $200.000 gelir/fon üstünde zorunlu, 12 Ocak 2026'da %5 zam, ~$2.310/yıl/koltuk (özet, [unity.com](https://unity.com/products/pricing-updates)). Çalışma süresi ücretinin güncel durumunu doğrulayamadım. | Türün fiilî standardı. |
| **Unreal 5** | Chaos. | Mover eklentisi: birleşik geri sarma, istemci yalnızca girdi gönderiyor, sunucu durumu yayınlıyor, istemci geri sarıp yeniden simüle edebiliyor. Chaos Ağ Fiziği ile bir arka uç da var. AMA Epic'in kendi karşılaştırma sayfası: Mover **deneysel** ("use caution when shipping with it"), "Yet untested"; eski Character Movement Component ise "Battle-hardened with many shipped projects" (doğrulandı, [Epic](https://dev.epicgames.com/documentation/unreal-engine/comparing-mover-and-character-movement-component-in-unreal-engine)). Fizik arka ucu Mover için büyük ölçüde dik kapsül karakterlerle sınırlı, Chaos belirlenimciliği forumlarda sorun (özet). | Ücretsiz, $1M üstünde %5 telif (tek ikinci el kaynaktan, resmi EULA ile doğrulanmadı). | En iyi ağ mimarisi kâğıt üstünde Unreal'da, ama fizik kısmı Epic'e göre henüz deneysel. |
| **Godot 4.4+** | **Jolt** dahili ve yeni projelerde varsayılan (doğrulandı, [resmi doküman](https://docs.godotengine.org/en/latest/tutorials/physics/using_jolt_physics.html)); eksik: ilgili eklem özellikleri, tahmini temas impulsları, bazı eklem yumuşaklık ayarları. Forumlarda Jolt altında kapsülün zemine gömülmesi ve karakter/rigidbody fırlaması raporları (özet). | Yüksek seviye çok oyunculu "sahne replikasyonu" var ama RigidBody senkronu için resmi çözüm yok; topluluk `_integrate_forces` ile geçici çözüm kullanıyor, istemci tarafında titreme raporları (özet). | MIT, telifsiz (özet). | Fizik güçlü, ağ olgunluğu ve piyasa payı (gelirde) düşük. |
| **GameMaker, RPG Maker** | 2B odaklı. | Yok/sınırlı. | — | Uygun değil. |
| **CryEngine, O3DE** | O3DE: AWS destekli, Linux Vakfı, büyük ölçekli 3B (özet). | O3DE çok oyunculu ağ özelliği var (özet), kanıt yok. | O3DE Apache 2.0. | Küçük ekip ve bizim ölçek için olgun değil/az kanıt. |
| **Flax, Stride, Bevy** | Hakkında çok az kanıt buldum. Bevy Rust, API sürümler arası değişiyor (özet). | Fizik ve ağ için kanıt bulamadım. | Açık kaynak. | Değerlendiremedim. |
| **Özel motorlar** (Frostbite, RAGE...) | — | — | — | Büyük stüdyo işi, bizim için geçersiz. |

## 4. Sonuç
1. **Bizim türde motor seçimi Unity için güçlü emsale sahip** (5 doğrulanmış örnek). Unreal piyasada ve gelirde önde ama aynı türde doğrulanmış bir örnek bulamadım.
2. **Hiçbir motorda fiziği ağ üzerinde "hazır" bulamadım.** Unity'nin NGO'su tam tahmin sunmuyor, Unreal'ın Mover'ı Epic'in kendi sözüyle deneysel, Godot'ta RigidBody senkronu topluluk geçici çözümü. Yani "fizik yok" hissi motor seçiminden değil, her motorda geliştiricinin kurduğu katmandan geliyor.
3. **Motor değiştirmenin maliyeti** (sahne üretimi, NGO hattı, test altyapısı, sunucu paketleri) burada ölçülmedi. Değiştirmek için fizik ve ağ kalitesinde kanıtlanmış bir üstünlük bulamadım.
4. Unreal'ın Mover + Chaos ağ fiziği ve Unity'deki üçüncü parti ağ kütüphaneleri (Fish-Net, Photon Fusion) fizik tahmini açısından asıl karşılaştırılacak şeyler. Bunları dokümandan öteye (örnek proje, ölçüm) incelemedim.

## 5. Bulamadıklarım ve doğrulanmayanlar
- GDC 2026'nın orijinal raporu ve Video Game Insights PDF'i (özetlerden gittim).
- Godot'un GDC rakamının ölçütü, özel motor %19'u.
- Unreal'ın telif şartları (resmi EULA okunmadı), Unity çalışma süresi ücretinin son durumu.
- Content Warning ve Phasmophobia'nın motoru.
- Flax, Stride, Bevy ve O3DE için fizik/ağ kanıtı.
- Motorlar arası dürüst, güncel bir fizik/ağ performans ölçümü: yok, bulduğum rakamlar düşük kaliteli sayfalardandı.
- Fish-Net, Mirror, Photon Fusion'ın fizik tahminini kendi dokümanlarından incelemedim.

## Kaynaklar
- https://app.cinevva.com/news/2026-03-13-gdc-2026-by-the-numbers.html
- https://respawn.outlookindia.com/amp/story/gaming/gaming-originals/2026-gdc-survey-exposes-workforce-strain-and-technological-shifts
- https://www.gamegen.com/unity-vs-unreal-engine-vs-godot-which-game-engine-should-you-learn-first-in-2026
- https://www.pocketgamer.biz/cross-industry-game-engine-adoption-surges-led-by-unreal-unity-and-godot
- https://sensortower.com/blog/the-big-game-engines-report-of-2025
- https://vginsights.com/assets/reports/The_Big_Game_Engines_Report_of_2025.pdf
- https://gamedevreports.substack.com/p/video-game-insights-game-engines
- https://newsletter.gamediscover.co/p/hows-pc-game-engine-usage-changing
- https://en.wikipedia.org/wiki/Lethal_Company , https://en.wikipedia.org/wiki/Gang_Beasts , https://en.wikipedia.org/wiki/Human:_Fall_Flat , https://en.wikipedia.org/wiki/R.E.P.O. , https://en.wikipedia.org/wiki/Peak_(video_game)
- https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.7/manual/advanced-topics/client-anticipation.html
- https://dev.epicgames.com/documentation/unreal-engine/comparing-mover-and-character-movement-component-in-unreal-engine
- https://dev.epicgames.com/documentation/en-us/unreal-engine/mover-in-unreal-engine
- https://docs.godotengine.org/en/latest/tutorials/physics/using_jolt_physics.html
- https://forums.unrealengine.com/t/should-we-be-using-the-network-prediction-plugin-or-netphysics-predicted-physics/2535018/2
- https://forum.godotengine.org/t/p2p-networked-physics-inaccuracy-seeking-help/45340
- https://unity.com/products/pricing-updates
- https://80.lv/articles/indie-team-breaks-down-shipping-multiplayer-game-with-godot-4-6-using-c
