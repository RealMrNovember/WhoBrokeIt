# Oyun motorları: piyasa ve bizim durumumuz

Tarih: 2026-11-06. Kaynaklar web aramasıyla bulundu (standart arama, özetler okundu, tam sayfalar değil). Rakamlar kaynaktan kaynağa değişiyor, yön gösterir. Doğrulanmayanlar açıkça işaretli.

## 1. En çok kullanılan motorlar

| Ölçüt | Sonuç | Kaynak |
|---|---|---|
| Steam'e 2024'te çıkan oyunların motoru (Video Game Insights) | Unity ~%51, Unreal ~%28, Godot ~%5, GameMaker ~%4, kalanı özel/diğer | [gamedevreports](https://gamedevreports.substack.com/p/video-game-insights-game-engines), [Big Game Engines Report 2025](https://vginsights.com/assets/reports/The_Big_Game_Engines_Report_of_2025.pdf) |
| Steam gelirinde pay (2024) | Unreal ~%31, Unity ~%26 (2018'den beri ilk kez Unreal önde) | aynı |
| Geliştirici anketi (Perforce, Eylül 2025) | Unreal %65, Unity %53 (herhangi bir kullanım) | [pocketgamer.biz](https://www.pocketgamer.biz/cross-industry-game-engine-adoption-surges-led-by-unreal-unity-and-godot) |
| GDC 2026 (ikinci elden özet, doğrulanmadı) | Unreal %42, Unity %30, ilk kez Unreal önde | [gamegen](https://www.gamegen.com/unity-vs-unreal-engine-vs-godot-which-game-engine-should-you-learn-first-in-2026) |
| Mobil | Unity ~%66, Unreal ~%22, Godot ~%8 (zayıf kaynak) | [llcbuddy](https://llcbuddy.com/?p=291665) |
| Godot büyümesi | Steam'deki tüm oyunların %0,9'undan (2020) %7,1'ine (2025); GMTK Jam 2025'te %39 | [gamediscover](https://newsletter.gamediscover.co/p/hows-pc-game-engine-usage-changing), [llcbuddy](https://llcbuddy.com/?p=291665) |
| Küçük motorların satış payı | Godot, GameMaker, RPG Maker vb. çıkan oyunların ~%10'u ama satılan kopyaların yalnızca ~%2'si | Video Game Insights raporu (yukarıdaki bağlantılar) |
| Özel motor | Steam çıkışlarında %71 (2012) → %13 (2024); Unreal bu pazarı alıyor | aynı |

Özet: Adet olarak Unity, gelir ve AAA'da Unreal öne geçiyor, Godot hızla büyüyor ama gelirde çok küçük.

## 2. Bizim türümüz için (kooperatif fizik komedisi) ne söylüyor

- **Unreal (Chaos + Network Prediction/Mover):** Ağ fiziği için "Network Physics" arka ucu var, fizik nesneleriyle etkileşim gerektiren projelere uygun bulunuyor. Sınırları: Mover desteği çoğunlukla dik duran kapsül karakterlerle sınırlı, sabit fizik adımı şart, Chaos'un belirlenimciliği forumlarda sorun olarak anılıyor ([forum 1](https://forums.unrealengine.com/t/should-we-be-using-the-network-prediction-plugin-or-netphysics-predicted-physics/2535018/2), [forum 2](https://forums.unrealengine.com/t/chaos-async-not-deterministic-enough-for-network-prediction/729633)). Yani hazır "sihirli" çözüm değil.
- **Unity (NGO):** Resmi bir fizik tahmini dokümanı bulamadım. Bir ortak oyun geliştiricisi forumda aynı sorunu anlatıyor; Unity 6'daki AnticipatedNetworkTransform'un yardım edebileceği söyleniyor ama fizikle ne kadar iyi çalıştığı doğrulanmadı ([forum](https://discussions.unity.com/t/netcode-for-gameobjects-custom-client-prediction-tips/1535760)). Aynı konuda "çok az online oyun gerçekten senkron fizik kullanıyor, çoğu çözümü etrafından dolaşıyor" yorumu var (anekdot).
- **Godot 4 (Jolt):** Jolt 4.4'te motora entegre edildi, 4.6'da yeni 3B projelerde varsayılan olduğu söyleniyor (tek dilli, doğrulanmadı) ([devclass](https://devclass.com/2025/03/05/godot-4-4-released-open-source-game-engine-adds-jolt-physics-net-8-and-more/), [gamefromscratch](https://gamefromscratch.com/godot-4-4-gets-native-jolt-physics-support/)). Çok oyunculu yığını Unity'ninkinden olgunluk olarak geride ([rivet](https://rivet.dev/blog/godot-multiplayer-compared-to-unity/)). Küçük bir ekibin Godot 4.6 ve C# ile gönderdiği bir ortak oyun var (Pratfall) ([80.lv](https://80.lv/articles/indie-team-breaks-down-shipping-multiplayer-game-with-godot-4-6-using-c)).
- Bulamadığım: Chaos ağ fiziği ile NGO'nun, ya da Godot+Jolt ile Unity'nin dürüst, güncel bir kıyas ölçümü. Bulduğum performans rakamları düşük kaliteli sayfalardan. Güvenmedim.

Benim bilgim (kaynaksız, doğrulanmadı): Gang Beasts, Human Fall Flat ve Lethal Company Unity'de yapıldı. Yani bizim türde ana akım motor Unity.

## 3. Yorum ve öneri

1. **Motor değiştirmek şu an yanlış hamle.** Sorunumuz motor değil: araçları elle yazılmış hız ayarıyla sürüyoruz, taşımayı ağ gecikmesine bağlı bıraktık, karakterin iskeleti 5 kemik. Bunlar Unity'de de, Unreal'da da, Godot'ta da aynı şekilde kötü olur. 0.4.13'te taşıma, alet ve iniş hataları motor değiştirmeden düzeldi.
2. **Unity'de kalmanın gerekçesi:** Steam'de en çok kullanılan motor, NGO + dedicated sunucu hattımız (Kale, Doku testleri, 17/17 ağ testi) çalışıyor, tüm sahneler C# editör betikleriyle üretiliyor. Unreal'a geçmek tüm bunları sıfırlamak demek ve Unreal'ın ağ fiziği de "hazır çözüm" değil.
3. **Gerçek eksikler (docs/FIZIK_ARASTIRMASI.md ile uyumlu):** (a) karakterleri kollar açık üretip düzgün rig'lemek (Doku oturumuna devredildi), (b) araçları kuvvet tabanlı Rigidbody fiziğine çevirip sürücüye yetki devretmek, (c) taşınan ve yüklenen nesneleri aynı yetki altına almak, (d) elde tutma için el IK'si (iki kemikli analitik IK).
4. **Godot'u izle:** Büyüyor ve Jolt güçlü, ama çok oyunculu olgunluğu ve Steam'deki satış payı henüz küçük. Bir sonraki oyun için aday, bu oyun için değil.

## Kaynaklar
- https://gamedevreports.substack.com/p/video-game-insights-game-engines
- https://vginsights.com/assets/reports/The_Big_Game_Engines_Report_of_2025.pdf
- https://www.pocketgamer.biz/cross-industry-game-engine-adoption-surges-led-by-unreal-unity-and-godot
- https://newsletter.gamediscover.co/p/hows-pc-game-engine-usage-changing
- https://www.gamegen.com/unity-vs-unreal-engine-vs-godot-which-game-engine-should-you-learn-first-in-2026
- https://llcbuddy.com/?p=291665
- https://forums.unrealengine.com/t/should-we-be-using-the-network-prediction-plugin-or-netphysics-predicted-physics/2535018/2
- https://forums.unrealengine.com/t/chaos-async-not-deterministic-enough-for-network-prediction/729633
- https://discussions.unity.com/t/netcode-for-gameobjects-custom-client-prediction-tips/1535760
- https://devclass.com/2025/03/05/godot-4-4-released-open-source-game-engine-adds-jolt-physics-net-8-and-more/
- https://gamefromscratch.com/godot-4-4-gets-native-jolt-physics-support/
- https://rivet.dev/blog/godot-multiplayer-compared-to-unity/
- https://80.lv/articles/indie-team-breaks-down-shipping-multiplayer-game-with-godot-4-6-using-c
