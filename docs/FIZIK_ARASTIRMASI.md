# Who Broke It? - Fizik / Netcode / Karakter Araştırması

Tarih: 2026-11-06. Kapsam: Unity 6000.0.32f1 URP, NGO 2.2.0, server-authoritative, 1-6 oyuncu.

**Dürüstlük notu:** Web araması bu oturumda yüzeysel kaldı (standart arama, kaynak sayfalarının tamamı okunmadı). Aşağıda "[KAYNAK]" ile işaretli maddeler bir sayfadan doğrulandı; "[TECRÜBE]" ile işaretliler genel sektör bilgisi/mühendislik yargısıdır ve doğrulanmamıştır. Human Fall Flat / Gang Beasts'in iç uygulaması için güvenilir bir kaynak **bulamadım**; o kısımlar yaygın tekniklerin anlatımıdır, "bu oyunlar şöyle yapıyor" iddiası değildir. Paket/fiyat bilgileri değişebilir, satın almadan önce mağaza sayfasını kontrol edin.

---

## Kısa teşhis (tüm şikâyetlerin kök nedeni)

1. **Taşınan nesne gerideymiş gibi görünmesi** = nesnenin konumu ağdan (NetworkTransform interpolation, ~100 ms geride) geliyor, elin konumu ise yerel prosedürel animasyondan. İki farklı zaman çizgisi. Çözüm: nesne **ağdan konum almamalı**, elin yerel (görsel) konumuna her makinede kendisi bağlanmalı.
2. **Araçlar "ağırlıksız"** = her FixedUpdate'te hız set etmek, kütle/sürtünme/ivme gibi fizik sonuçlarını siliyor; yük (crate) araca sürtünmeyle değil, hiçbir şeyle bağlı değil.
3. **Kolların esnemesi** = skin weight sorunu; kodla çözülmez (bölüm D).
4. **Aletlerin omuzda durması** = alet bir "el kemiği"ne değil karakter köküne/omuza göre ofsetleniyor ve el IK'sı yok.

---

## A) Netcode

### A.1 NGO'nun sunduğu araçlar
- **NetworkRigidbody (NGO 2.x):** NetworkTransform'un yetki modelini izler. Varsayılan server-authoritative'de sadece server simüle eder, diğer örneklerde Rigidbody kinematic yapılır. 2.0'dan beri `AutoUpdateKinematicState` varsayılan açık. Owner-authoritative'de çarpışmalar yalnızca aynı client'a ait NetworkObject'lerle gerçekçi sonuç verir; bazı çarpışma olayları NetworkRigidbody ile tetiklenmeyebilir. [KAYNAK] https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.4/manual/advanced-topics/physics.html , https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.0/api/Unity.Netcode.Components.NetworkRigidbodyBase.html
- **AnticipatedNetworkTransform / AnticipatedNetworkVariable:** İstemci "sunucu bunu onaylayacak" varsayımıyla değeri hemen gösterir. Üç mod: Snap, Smooth (`OnReanticipate` + `Smooth()`), Constant Reanticipation (her yeni yetkili veri geldiğinde kullanıcı kodu tahmini yeniden hesaplar). `StaleDataHandling` (Ignore/Reanticipate) var. [KAYNAK] https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.5/api/Unity.Netcode.Components.AnticipatedNetworkTransform.html
  - **Önemli sınır [TECRÜBE]:** Anticipation tam bir rollback/resimulation sistemi DEĞİL. Basit "RPC gönder, sonucu önceden göster" için uygun. Kendi sürdüğünüz araç gibi sürekli değişen, çarpışmalara giren fizik için Fish-Net PredictionRigidbody veya Photon Fusion resimulation seviyesinde bir şey sunmuyor. Yani NGO'da araç için gerçek tahmin yazmak hâlâ elle.
- **NetworkObject parenting:** `NetworkObject.TrySetParent` + `OnNetworkObjectParentChanged`; ebeveyn değişimi server'dan tüm istemcilere (geç katılanlara da) gider; `worldPositionStays` da senkronize edilir; "NetworkTransform olmadan sabit yerel ofsetle taşıma" bu şekilde mümkün. Ebeveynlik sadece server'da yapılmalı (istemci ServerRpc ile ister). [KAYNAK] https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects%402.5/manual/advanced-topics/networkobject-parenting.html
  - Not: Parent olarak kullanılan şey **NetworkObject** olmak zorunda; el kemiği NetworkObject değil. Yaygın çözüm: oyuncunun NetworkObject'inin altında (el kemiğinden bağımsız) bir "HoldPoint" çocuk transform'u var; nesne ona parent olmaz, **yerel olarak** takip eder (aşağıda).

### A.2 "Taşınan nesne her makinede yapışık görünsün" için en güvenilir desen [TECRÜBE, NGO parenting dokümanıyla uyumlu]
Taşınan nesne için **ağ üzerinden konum göndermeyi bırakın**:
1. Server: pickup'ı doğrular, `NetworkVariable<ulong> HeldBy` (veya NetworkObjectReference) yazar; nesnenin Rigidbody'sini kapatır (isKinematic + NetworkTransform'u devre dışı/`SyncPosition` kapalı veya Interpolate'u kapalı).
2. **Her makine** (host, client, kendi dahil) `LateUpdate`'te (animasyon + IK bittikten SONRA) nesneyi o makinedeki karakterin **görsel el/HoldPoint transform'una** hard-attach eder (`transform.SetPositionAndRotation(hold.position, hold.rotation)` veya gerçek `transform.SetParent(handBone)` yerel olarak). Böylece el nerede çiziliyorsa nesne oradadır; ağ gecikmesi nesnenin konumuna hiç girmez, sadece "kim tutuyor" bilgisine girer.
3. Bırakma/fırlatma: server nesneyi fizik moduna geri alır, hız ve konumu verir; NetworkTransform tekrar açılır. İstemcide bırakma anında küçük bir görsel yumuşatma (0.1 sn lerp) sıçramayı gizler.
4. Aldığınız anda: kendi client'ınızda "tahmini" alma (anticipation): tuşa basınca hemen eli/nesneyi göster, server reddederse bırak. Başkaları için `HeldBy` değişince snap.
- Yukarıdaki forumda önerilen "istemci server cevabını bekleyip sonra göster" yaklaşımı daha basit ama 100-200 ms pickup gecikmesi yaratır. [KAYNAK, topluluk önerisi] https://forum.unity.com/threads/players-picking-up-objects-is-much-harder-than-it-should-be.1343492/
- Bu şu an yaşadığınız "arkada kalma/içinden geçme" sorununun doğrudan çözümüdür. En yüksek getiri/maliyet oranlı maddedir.

### A.3 Server-authoritative vs owner-authoritative fizik nesnesi
- **Sandbox/kooperatif, 1-6 oyuncu, güven sorunu düşük:** Birçok küçük co-op oyunda (yaygın tecrübe) itilen/taşınan fizik nesnelerinde "etkileşen oyuncu geçici sahip" modeli (ownership transfer) kullanılır: nesneyi tutan/süren oyuncu yetkili olur, diğerleri interpolasyonla izler. Avantaj: sahibi için sıfır gecikme. Dezavantaj: başka bir oyuncunun nesneye çarpması gecikmeli/yanlış görünür; hile riski (sizin için önemsiz olabilir). NGO'da bu `NetworkObject.ChangeOwnership` + owner-authoritative NetworkTransform (`AuthorityMode = Owner`) ile yapılır (NGO 2.x'te ClientNetworkTransform'a gerek kalmadan Authority Mode alanı vardır [TECRÜBE - 2.2.0'da alan adını doğrulayın]).
- **Araçlar (bisiklet, kaykay, el arabası) için öneri:** Sürücüye geçici sahiplik ver, aracı sürücü client'ı simüle etsin (sahibinde gecikmesiz his), diğer client'lar interpolasyonla izlesin. Server sürücü yoksa aracı kendisi simüle etsin. Bu, sıfırdan tahmin+uzlaştırma yazmaktan çok daha ucuz. Yük (crate) araçla birlikte **aynı yetkiliye** ait olmalı, yoksa yük araçtan ayrışır (şu anki "yük gelmiyor" sorununun ağ boyutlu nedeni de bu olabilir).
- Çarpışma doğruluğu isteyen oyuncu-oyuncu itişmesi server'da kalsın.

### A.4 Diğer ağ yığınları (kıyas)
- **Fish-Net:** `PredictionRigidbody` ile rigidbody'de gerçek client-side prediction + reconcile; NetworkObject'te Prediction Type = Rigidbody; tetik/çarpışma için NetworkTrigger/NetworkCollision. NGO'dan daha olgun tahmin sağlıyor ama NGO'dan geçiş büyük iş. [KAYNAK] https://fish-networking.gitbook.io/docs/guides/features/prediction/predictionrigidbody
- **Photon Fusion 2:** Physics Addon (resimulation, CPU pahalı) ve "Forecast Physics" (uzak nesneler extrapolate edilir, her client yerel tam fizik koşar, sonuç uzlaştırılır); Forecast dokümanında araç örneği var. Fikir olarak Forecast, 1-6 kişilik co-op için uygun bir model. [KAYNAK] https://doc.photonengine.com/fusion/v2/manual/physics/physics-forecast , https://doc.photonengine.com/fusion/v2/addons/physics/overview
- **Boss Room / Megacity Metro:** Bu sorunun yanıtı olarak bu örneklerde taşınan nesne ya da araç fiziği ile ilgili bir şey **doğrulayamadım**. Boss Room fizik odaklı değil; Megacity Metro Netcode for Entities (ECS) kullanır, NGO'ya doğrudan aktarılamaz. Burada bir kaynak iddia etmiyorum.
- Mirror: bu çalışmada incelenmedi.

**Karar önerisi:** NGO'da kalın. Taşıma için A.2, araçlar için "sürücü sahiplik" modeli. Fusion/Fish-Net geçişi, ancak NGO ile yeterli his alınamazsa son çare.

---

## B) Karakter: el tutma / taşıma / itme

### B.1 Seçenekler
| Yaklaşım | Artı | Eksi | Solo dev uygunluğu |
|---|---|---|---|
| Elin kemiğine **görsel attach** + basit 2-bone IK (kendi kodunuz) | Ucuz, deterministik, ağda ucuz | Fiziksel değil | **En iyi ilk adım** |
| Unity **Animation Rigging** (TwoBoneIKConstraint, MultiParentConstraint, OverrideTransform) | Resmi paket, Animator ile entegre | Kurulum sahne/prefab gerektirir; runtime hedef değişince `RigBuilder.Build()` gerekir; 5 kemikli özel rig'de Animator/Avatar gerekir | Orta; batchmode'da C# ile kurulabilir |
| **Final IK** (Asset Store, ücretli) | Çok olgun (grounder, aim, interaction) | Ücret (güncel fiyat doğrulanmadı) ve yine kemik yapısı gerekir | Orta |
| **Fizik eli** (ConfigurableJoint, Human Fall Flat / Gang Beasts tarzı) | "Gerçek" fizik hissi | Ağda zor, ayarı çok zor, kararsızlık; 6 kişilik NGO'da kırılgan | Düşük (şimdi değil) |

- Animation Rigging: TwoBoneIK = iki kemikli zincir (tip = el) + target + hint (dirsek). Runtime'da target atamak rig'i otomatik güncellemez, `RigBuilder.Build()` çağırmak gerekir. Sabit hedefi hareketli nesneye pinlemek için MultiParentConstraint önerilmiş. [KAYNAK] https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.0/manual/constraints/TwoBoneIKConstraint.html , https://discussions.unity.com/t/is-there-a-way-to-set-the-source-object-target-via-script-for-two-bone-ik-constraint/237302 (paket sürümü 1.0.3 dokümanı; Unity 6'daki güncel sürümü ayrıca doğrulayın).
- **Fizik eli:** ConfigurableJoint ile kinematik bir "el" Rigidbody'sine bağlanan nesne örnekleri var, ancak HFF/Gang Beasts uygulamasına dair kaynak bulamadım. [KAYNAK, genel] https://learn.unity.com/tutorial/attaching-things-with-joints-part-2

### B.2 Sizin durumunuz için öneri (3 aşamalı)
1. **El/alet bağlama:** Karakterin her elinde gerçek bir `HandAnchor` Transform'u olsun (kemiğin çocuğu veya elin kodla hesaplanan ucu). Alet ve taşınan nesne prefab'larında `GripPoint` (alet tutacağının olduğu yer) tanımlı olsun; bağlarken `tool.rotation = hand.rotation * Quaternion.Inverse(grip.localRotation)` ve konumu ona göre ayarlayın. Omuz ofseti sorunu böylece biter.
2. **Kendi iki-kemik IK'nız (~60 satır, analitik):** Hedef el konumu (taşınan nesnenin tutacağı) -> omuz-dirsek-bilek zincirini çözün. Prosedürel animasyonunuz zaten kodla olduğu için Animation Rigging'e gerek olmayabilir; üçüncü paket bağımlılığı olmadan batchmode'da çalışır. Animator tabanlıysa Animation Rigging'e geçin.
3. **İtme:** İtme saldırısında sadece omuz hareket ediyorsa, vuruş anında iki elin hedefini ileri `OverlapSphere` kutusuna sürün (IK), gövdeyi öne eğin ve darbeyi **hitbox ile** uygulayın (görsel ile fizik ayrı). El hedefini hit noktasında kısa süre tutun; his çoğunlukla zamanlamadan gelir ([TECRÜBE]).
4. **Fizik eli (ConfigurableJoint)**: şimdilik **önermiyorum**; ağda ve 6 oyuncuda yönetimi zor.

---

## C) Araçlar (bisiklet / kaykay / el arabası)

### C.1 Tekerlek modeli
- **WheelCollider:** Gerçek süspansiyon/sürtünme, ama bisiklet/kaykay (iki tekerlek, denge) için kötü ayarlanır; arcade araba için uygundur [TECRÜBE].
- **Raycast süspansiyonu (hover/spring raycast):** Arcade araçlar için yaygın; yay-damper kuvveti + yanal sürtünme kuvveti ile yazılır, tamamen kontrol edilebilir, ağda hafif. [TECRÜBE] Kaykay ve bisiklet için en uygun.
- **Kinematik (şu anki):** Hız set etmek ağırlığı öldürür. Yerine **kuvvet/ivme** kullanın: `AddForce(ForceMode.Acceleration)` ile ileri itiş, yanal hız bileşenini sönümleyen kuvvet (`lateral = Vector3.Dot(v, right)`; `AddForce(-right * lateral * grip, Acceleration)`), maksimum hız sınırı, `Rigidbody.mass`/`drag`/`angularDrag`, ağırlık merkezi (`centerOfMass`) alçak. Bu, "ağırlık" hissini verir.

### C.2 Spesifik hatalar
- **Atlama basılı tutulunca kaykayın uçması:** Atlamayı **kenar tetiklemeli** (edge trigger) yapın: `jumpPressed = input.wasPressedThisFrame`, FixedUpdate'te tüket (flag'i sıfırla), ayrıca `grounded` ve `jumpCooldown` kontrolü. Yeni Input System'de `action.WasPressedThisFrame()`.
- **İnişte yana fırlama:** İniş anında (dismount) (a) aracın Rigidbody hızını sıfırlayın veya oyuncuya sadece yatay hızın bir kısmını verin, (b) oyuncuyu araç collider'ından uzak, güvenli bir `DismountPoint`'e ışınlayın, (c) iniş sonrası ~0.3 sn araç-oyuncu çarpışmasını `Physics.IgnoreCollision` ile kapatın. Fırlamanın nedeni genelde çakışan collider'ların itilmesidir.
- **El arabası:** Oyuncuyu araca `ConfigurableJoint` (sadece konum kilitli, açı serbest) ile bağlayıp çekmek mümkündür [TECRÜBE], ama kinematik "sürüş" yerine **kuvvet uygulayarak** (oyuncunun gittiği yöne doğru arabayı çeken spring) daha sağlamdır. Yükte ağırlık varsa araba yavaşlar, bu da "ağırlık" hissidir.

### C.3 Yükün araçla gitmesi
- Forum tecrübesi: dinamik bir nesne, yüksek sürtünmeli malzemeyle hareket eden bir yüzeyde durabilir; kinematik platform nesneleri taşır ama kayabilir; kinematik gövdeyi `MovePosition` ile FixedUpdate'te sürmek diğer cisimlerin tepki vermesini sağlar. Dinamik Rigidbody'yi parent'lamak sorunludur (ebeveynin hareketini yok sayabilir). Tüm bunlar eski forum yazılarıdır (2015-2021), resmi kılavuz bulamadım. [KAYNAK] https://discussions.unity.com/t/unity-player-not-sticking-to-a-moving-platform-when-using-kinematic-platform-type/827470 , https://discussions.unity.com/t/rigidbody-on-a-platform/28527
- **Öneri:** Araç **dinamik Rigidbody** (yük de dinamik) olsun; kasa içeren bir `CargoBed` collider'ı (düşük duvarlı kutu) ve yüksek sürtünmeli PhysicMaterial. Araç kuvvetle sürüldüğü için yük fizikle kendiliğinden gelir. Dinamik yükü parent'lamayın. Kasa yüksek hızlanmada yük düşüyorsa bu, "gerçek" fizik davranışıdır; abartılıysa sürtünme/ivme sınırlayın.
- Ağda: araç + yük aynı yetkili (A.3). Çok sayıda yük için hepsini tek NetworkTransform ile senkronlamak pahalı olabilir; 1-6 oyuncu için sorun olmaz.

### C.4 Hazır paketler
| Paket | Not |
|---|---|
| **Kinematic Character Controller** (Philippe St-Amand) | Şu an ücretsiz olduğu söyleniyor (Unity satın aldı; kaynaklar ikincil, mağaza sayfasını doğrulayın). Kinematik olduğu için rigidbody etkileşimleri elle kodlanır, itme/kuvvet yok; hareketli platform için `PhysicsMover` var. Yani "fizik hissi" sorununu çözmez; dikkatli kullanın. https://phil-sa.itch.io/kinematiccharactercontroller , https://discussions.unity.com/t/released-kinematic-character-controller/678434 |
| **Edy's Vehicle Physics** | Listede $60 (doğrulanmadı güncel), tam kaynak; arabalar için, ağ desteği listede belirtilmemiş. Bisiklet/kaykay için uygun değil. https://assetstore.unity.com/packages/tools/physics/edy-s-vehicle-physics-403 |
| **NWH Vehicle Physics 2** | $64 (liste); Mirror ve PUN2 entegrasyonu belirtilmiş, durumu tek struct paketlenebilir (NGO için elle uyarlama gerekir). Araba odaklı, bisiklet/kaykay için uygun değil. https://assetstore.unity.com/packages/tools/physics/nwh-vehicle-physics-2-166252 |
- **Sonuç:** Yukarıdaki paketler arabalar içindir; sizin bisiklet/kaykay/el arabası için hazır paket almak yerine kendi raycast+kuvvet modelinizi yazmak daha doğru [TECRÜBE]. Açık kaynak bir "bisiklet fiziği" kütüphanesi için sağlam bir kaynak bulamadım.

---

## D) Skinning / mesh (TRELLIS, Meshy vb.)

Web aramasında bu konuda kesin bir kaynak bulunamadı; aşağıdaki bölüm kısmen kaynaklı, kısmen tecrübedir.

- **Neden kollar esniyor:** Üretilen meshlerde topoloji düzensizdir, kol gövdeye yakın/bitişik (A-pose değil), kemik yerleşimi meshle uyumsuz; otomatik ağırlıklar (Blender Automatic Weights = heat diffusion) bitişik parçalar arasında sızar. Meshy da temiz bir poz (T/A-pose) olmasının otomatik skinlemeyi iyileştirdiğini söylüyor (satıcı iddiası). https://www.meshy.ai/features/ai-auto-rigging
- **Aynalama delikleri:** Üretilen mesh simetrik değildir; yarısını aynalayıp (mirror) birleştirince orta çizgide vertex'ler örtüşmez, kaynak/weld edilemeyen bölgelerde delik/yarıklar kalır. Aynalamak yerine ağırlıkları **tam mesh üzerinde** hesaplayın (veya sadece ağırlıkları Blender'da "Mirror Vertex Groups"/Data Transfer ile simetrik kopyalayın, geometriyi değil). [TECRÜBE]
- **Seçenekler:**
  1. **Blender (arka planda, `blender -b -P`) Automatic Weights:** Önce mesh'i temizleyin (merge by distance, non-manifold onar, kolları gövdeden ayıran A/T-pose), sonra `bpy.ops.object.parent_set(type='ARMATURE_AUTO')`. Ardından: `vertex_group_limit_total(limit=4)`, `vertex_group_normalize_all`, `vertex_group_smooth`. Elle fırça düzeltmesi yoksa kalite orta. Batchmode ile otomatikleştirilebilir.
  2. **UniRig** (araştırma, kendi değerlendirmesinde Tripo/Meshy ticari rigger'lardan daha iyi skin L1 hatası; yazarların iddiası): https://www.emergentmind.com/articles/2504.12451 . Kurulum GPU/Python gerektirir; sunucuda (laptopta değil) çalıştırılabilir. Kalitesini kendiniz test etmeden güvenmeyin.
  3. **Mixamo:** T-pose tespiti ve marker koyma gerektirir, tam otomatik değil. https://discourse.threejs.org/t/auto-rigging-still-something-only-mixamo-can-do/43709 ; ayrıca yalnız insansı için, sizin 5 kemikli rig'e uymaz.
  4. **Rigify:** insansı 'metarig' sığdırmak ister; sizin gibi 5 kemikli rig için aşırı.
  5. **UniHumanoid:** VRM/Humanoid avatar odaklı; bu çalışmada doğrulanmadı.
- **Sizin 5 kemikli rig için pratik çözüm [TECRÜBE]:** Ağırlıkları **bölgesel/uzaklık tabanlı kendiniz üretin**: her vertex için en yakın 1-2 kemik segmentine (nokta-segment mesafesi) bağlı yumuşak bir ağırlık (örn. `w = 1/(d+eps)^p`, normalize, max 2-4 kemik) ve eklem çevresinde yumuşatma. Kolu gövdeden ayıran bir **bölge maskesi** (X'e göre |x| > kol eşiği ve yükseklik) ile kol vertex'leri sadece kol kemiklerinden etkilenir, böylece sızıntı olmaz. Bu, Unity editör betiğiyle (C#) mesh.boneWeights atayarak batchmode'da yapılabilir ve mevcut "esneyen kol" sorununu doğrudan çözer. Kalan deformasyon için kolu iki ayrı mesh parçasına bölmek (rubber-hose/Gang Beasts tarzı bağımsız parçalar) en sağlam seçenektir.

---

## E) Etki/Efor sıralaması

| # | İş | Etki | Efor | Not |
|---|---|---|---|---|
| 1 | Taşınan nesneyi her makinede LateUpdate'te elin görsel anchor'ına yerel bağlama (A.2) | Çok yüksek | Düşük | "Gecikme/içinden geçme" biter |
| 2 | Alet GripPoint + el anchor'ı (B.2-1) | Yüksek | Düşük | Omuzdaki aletler biter |
| 3 | Atlama kenar tetiklemesi + iniş güvenliği (C.2) | Yüksek | Düşük | Kaykay uçması, yana fırlama |
| 4 | Araçlarda hız set etmek yerine kuvvet + yanal sürtünme + dinamik yük (C.1/C.3) | Çok yüksek | Orta | "Fizik/ağırlık" hissi |
| 5 | Sürücüye geçici sahiplik (araç + yük aynı yetkili) (A.3) | Yüksek | Orta | Ağda araç gecikmesi |
| 6 | Kendi skin weight üretici (D) | Yüksek (görsel) | Orta | Kol esnemesi |
| 7 | Kendi 2-bone IK + itme animasyonu (B.2) | Orta-yüksek | Orta | |
| 8 | Animation Rigging paketi | Orta | Orta | Animator'a geçilirse |
| 9 | Fizik eli (ConfigurableJoint) | Belirsiz | Yüksek | Ertelenmeli |
| 10 | Ağ yığını değişimi (Fish-Net/Fusion) | Belirsiz | Çok yüksek | Son çare |

---

## Önerilen sıra

**Faz 1 (1-2 gün, en çok his kazancı):**
1. Taşıma: `HeldBy` NetworkVariable + her makinede LateUpdate'te yerel hard-attach; taşınırken NetworkTransform kapat.
2. Alet/nesne `GripPoint` + el `HandAnchor`.
3. Atlama edge-trigger (`WasPressedThisFrame` -> FixedUpdate'te tüket), dismount: `DismountPoint`, hız sıfırlama, `Physics.IgnoreCollision` kısa süre.

**Faz 2 (fizik hissi):**
4. Araçları dinamik Rigidbody + `AddForce(ForceMode.Acceleration)` + yanal sürtünme + `centerOfMass` + raycast yer teması. Yük dinamik, kasa collider, yüksek sürtünme.
5. Sürücü sahipliği (`NetworkObject.ChangeOwnership`, owner-authoritative NetworkTransform + NetworkRigidbody), araç ve yük aynı yetkili.

**Faz 3 (karakter kalitesi):**
6. Skin weight üretici (bölgesel + max 4 kemik + normalize), gerekirse kol parçalarını ayır.
7. 2-bone IK ile iki el taşıma/itme; itme için hitbox + gövde eğimi. Gerekirse Animation Rigging (`TwoBoneIKConstraint`, `MultiParentConstraint`, `RigBuilder`).

**Yapmayın (şimdilik):** ConfigurableJoint fizik eli, ağ yığını değişimi, araç paketi satın alma.

**Belirsizlikler:** NGO 2.2.0'daki tam yetki-modu alan adları, AnticipatedNetworkTransform'un araç için yeterliliği (muhtemelen yetersiz), UniRig'in sizin meshlerde kalitesi ve KCC'nin güncel fiyatı doğrulanmadı. Faz 1'i yapıp playtester'a tekrar verin, ardından Faz 2'ye karar verin.

---

## Kaynaklar
- NGO AnticipatedNetworkTransform: https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.5/api/Unity.Netcode.Components.AnticipatedNetworkTransform.html
- NGO fizik kılavuzu (2.4): https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.4/manual/advanced-topics/physics.html
- NetworkRigidbodyBase (2.0): https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.0/api/Unity.Netcode.Components.NetworkRigidbodyBase.html
- NetworkObject parenting (2.5): https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects%402.5/manual/advanced-topics/networkobject-parenting.html
- Forum, pickup: https://forum.unity.com/threads/players-picking-up-objects-is-much-harder-than-it-should-be.1343492/
- Fish-Net PredictionRigidbody: https://fish-networking.gitbook.io/docs/guides/features/prediction/predictionrigidbody
- Photon Fusion 2 Forecast Physics: https://doc.photonengine.com/fusion/v2/manual/physics/physics-forecast
- Photon Fusion 2 Physics Addon: https://doc.photonengine.com/fusion/v2/addons/physics/overview
- Animation Rigging TwoBoneIK (1.0): https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.0/manual/constraints/TwoBoneIKConstraint.html
- Runtime IK hedef değiştirme forumu: https://discussions.unity.com/t/is-there-a-way-to-set-the-source-object-target-via-script-for-two-bone-ik-constraint/237302
- Unity Learn Joints: https://learn.unity.com/tutorial/attaching-things-with-joints-part-2
- Kinematic Character Controller: https://phil-sa.itch.io/kinematiccharactercontroller , https://discussions.unity.com/t/released-kinematic-character-controller/678434
- NWH Vehicle Physics 2: https://assetstore.unity.com/packages/tools/physics/nwh-vehicle-physics-2-166252
- Edy's Vehicle Physics: https://assetstore.unity.com/packages/tools/physics/edy-s-vehicle-physics-403
- Hareketli platform / yük forumları (eski): https://discussions.unity.com/t/unity-player-not-sticking-to-a-moving-platform-when-using-kinematic-platform-type/827470 , https://discussions.unity.com/t/rigidbody-on-a-platform/28527
- UniRig makalesi (özet sayfası): https://www.emergentmind.com/articles/2504.12451
- Meshy AI rigging: https://www.meshy.ai/features/ai-auto-rigging
- Mixamo tartışması: https://discourse.threejs.org/t/auto-rigging-still-something-only-mixamo-can-do/43709
- Rig It Quick (Blender eklentisi): https://superhivemarket.com/products/rig-it-quick
