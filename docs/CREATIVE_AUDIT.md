# Yaratıcı ve teknik denetim (Faz 1)

Tarih: 2026-11-10 · Sürüm: 0.4.18 · Yöntem: depodaki tüm belgeler (GDD, STORY, MVP, ROADMAP, DECISIONS, CHANGELOG, GREENFIELD_MEYDANI, SANAT_YONU, OYUN_BECERILERI, PLAYTEST, ISTEKLER), oyun kodu (Assets/Scripts, Assets/Editor) ve son iki haftanın pencereli ekran görüntüleri okundu. Belge ile kodun uyuşmadığı yerlerde **kod esas alındı**. Bu belgedeki bir hüküm "gördüm" ise ekran görüntüsü ya da kod satırıyla doğrulanmıştır; "tahmin" ise öyle yazıldı.

Bu belge **teşhistir**. Çözüm önerileri `NEW_GAME_VISION.md`, `NARRATIVE_REDESIGN.md`, `WORLD_DESIGN.md`, `CHARACTER_BIBLE.md`, `ART_DIRECTION.md`, `GAMEPLAY_CONTINUITY.md` içindedir. Onay beklenen kararlar sonda.

---

## 1. Tek cümlelik teşhis
Oyun **çok sayıda çalışan sistem** ve **neredeyse hiç kimlik** barındırıyor. Teknik temel sağlam (çok oyunculu ağ, sunucu otoriter fizik, araçlar, tamir, yıkım, taşıma, sohbet); ama bunlar bir hikâyeye, bir yere ve birbirine bağlı değil. Oyuncu "ne için buradayım, burası neresi, yaptığım iş neyi değiştirdi" sorularına oyun içinden cevap alamıyor.

## 2. Oyuncunun gerçekten yaşadığı deneyim (ilk 10 dakika)
Oynanan sürüm 0.4.x, Greenfield sahnesi, tek kişilik ya da çevrimiçi:

1. **Menü:** profesyonel görünüyor (logo, karakter podyumu, Türkçe/İngilizce). Oyunun ne hakkında olduğunu söyleyen tek şey slogan: "Birlikte kur, birlikte bozdur, birlikte çöz".
2. **Meydana doğuş:** parlak, renkli bir meydan, çeşme, saat kulesi, çevrede yayalar. Ekranda **iki ayrı görev arayüzü aynı anda**: sol üstte eski "GÖREV: Atölyeyi Aç" kutusu (4 madde) ve üstte yeni "Sözleşme" afişi (Palet Taşıma vb.). Biri ilk sürümden kalma sabit vakaya, diğeri sonradan eklenen rastgele sözleşme sistemine ait. Birbirini tanımıyorlar. İkisinin üstüne **telsiz mesajı** biniyordu (0.4.11'de düzelttim, ama ikisi hâlâ yan yana).
3. **Hikâye:** toplam **dört cümle** (üç Fitil telsiz mesajı, bir ifşa metni). Fitil'in ilk mesajı iyi ("kimse yok, kimse de şikâyet etmiyor"), ama devamı yok; "su kulesindeki forklift" ve "sonra konuşuruz" hiçbir yerde karşılığını bulmuyor.
4. **İlk eğlence:** nesne taşıma, kırma, forklift, kaykay ve bisiklet gerçekten eğlenceli olabilecek ham maddeler. Fakat yaptığın hiçbir şey **dünyayı değiştirmiyor**: ışıklar yalnızca eski vaka bitince yanıyor, sözleşme bitince yalnızca puan artıyor.
5. **Bitiş:** eski vakada "Suçlu: SİNCAP MEHMET" ifşası (esprili, tek seferlik). Sözleşmelerde bitiş yok: yeni sözleşme çıkıyor.

Bir oyuncu 10 dakika sonunda "güzel fizik oyuncağı" der; "güzel bir oyun" demez. Eksik olan, oyuncuya **neden** sorusunun cevabı.

## 3. Belge ile uygulama çelişkileri

| # | Belgede | Kodda / oyunda | Sonuç |
|---|---|---|---|
| C1 | `WhoBrokeIt.md` §2: "The Collapse" küresel çöküş, bölgeler, Reclaimers ağı; §2.4 beş aşamalı kalıcı dünya | Tek küçük bölge; **hiçbir kalıcılık yok** (oturum kapanınca her şey sıfırlanıyor); aşama sistemi yok | Vizyonun merkezi (dünyanın gözünün önünde canlanması) oyunda yok |
| C2 | `STORY.md`: "Mükemmel Düzen" belediye otomasyonu, DOKUNMA düğmesi, "Biz" katmanı, vaka havuzundan değişen nedenler | Koda yalnızca Fitil'in 3 cümlesi, Muhtar'ın börek cümlesi, sincap girdi. Otomasyon, düğme, havuz yok | Hikâye taslak olarak kaldı |
| C3 | `STORY.md` §5: her bozuk şeyin **dünyaya göre değişen** nedeni | `CaseManager`: nedeni **sabit** (Mehmet). `ContractDirector`: rastgele ama hikâyesiz | İki farklı model, hiçbiri anlatı sunmuyor |
| C4 | `MVP.md` v0.2: "Dinamik suçlu sistemi, NPC'ler, hava/gün döngüsü MVP dışı" | NPC'ler (9 yaya + 4 satıcı) eklendi, sözleşme sistemi eklendi; MVP.md güncellenmedi | MVP kapsamı sessizce aşıldı, belge geride |
| C5 | `ISTEKLER.md` (kalıcı istek): "Oyun TEK senaryolu değildir; arayüz, karakter ve metinler genel kullanımlı olur" | Yeni yönerge (bu brif): tek, tutarlı, kanonik ana hikâye | **Doğrudan çelişki.** Yeni yönerge sonradan geldiği için üstün sayıldı; yine de onay gerekiyor (Karar K1) |
| C6 | `ROADMAP.md`: "P1 oynanış testi bekliyor"; `DECISIONS.md`: motor 6000.3.0f1 | Gerçek: 6000.0.32f1, 0.4.18 yayında, 5+ ay değil 7 gün | Yol haritası ve kararlar geçersiz |
| C7 | `SANAT_YONU.md` §4: karakter ≤8k üçgen, 1024² doku; "mevcut AI modelleri aşıyor, geçici" | Oyuncu karakterleri 16–26k üçgen, **2048²** doku (kasıtlı yükseltildi, belgelenmedi) | Sanat kuralı ihlali, onaylanmadı |
| C8 | `SANAT_YONU.md` §10: animasyon klip tabanlı, prosedürel yalnızca ince katman | Tüm animasyon **prosedürel** (kod), klip yok | Sanat yönü ile üretim yolu ayrışmış |
| C9 | `SANAT_YONU.md` §12: kalıcı Türkçe yazı tipi, UiKit | OnGUI + Unity varsayılan yazı tipi; HUD yer yer ham (ör. alt köşedeki kontrol dizisi) | Arayüz "premium" değil |
| C10 | `GDD`: lobi, parti, arkadaş listesi, Steam daveti, profil, onboarding, tutorial | Oda kodu + hızlı oyun var; lobi/parti/arkadaş yok; tutorial yok | Sosyal katman ve ilk kullanıcı deneyimi eksik |
| C11 | `GREENFIELD_MEYDANI.md`: 90×70 m, 7 zon, "Waterworks" ve "Hilltop Relay" | Su kulesi ve tepe **sadece dekor/rampa**; "Waterworks" projesi ve "Relay" yok | İki ana hikâye mekânı oyuncu için boş |
| C12 | `PLAYTEST.md` + telemetri | Telemetri var; **gerçek dış test yapılmadı**; kullanıcının tek gerçek oturumunda arkadaşın karakteri donuk göründü (ağ tanılaması 0.4.18'de) | P4 kapısı geçilmedi |

## 4. Var olup oyun deneyiminde anlamı olmayan sistemler

| Sistem | Durum | Sorun |
|---|---|---|
| **Kozmetik şapkalar** (5) | Çalışıyor, sözleşme sayısıyla açılıyor | Hikâyeyle ve dünyayla bağı yok; "Taç" neden var? |
| **İfadeler** (selam, işaret, yaşasın, dans) | Çalışıyor | İyi sosyal araç, ama oyunun hiçbir anı onları gerektirmiyor |
| **Kaykay / bisiklet** | Sürülüyor, çoklu oyuncuda görünüyor | Hiçbir görev kullanmıyor; yalnızca "serbest eğlence". Tepedeki rampa oyunun hiçbir hikâye adımına bağlı değil |
| **El arabası** | Yük taşıyor | Sözleşmelerde yok; meyveci ile bağlantısız |
| **Forklift** | Palet kaldırıyor | Yalnızca "Palet Taşıma" sözleşmesinde; zorunlu değil. Su kulesindeki forklift dekor |
| **Süpürge** | Enkaz topluyor | Yalnızca eski vakanın 16 adet "enkaz"ında |
| **Sözleşme panosu** | 5 rastgele sözleşme | Sözleşme gerekçesiz: "neden palet taşıyorum?" |
| **Kasaba Defteri** (Tab) | Eski vakadaki 3 ipucunu gösteriyor | Oyunun merkezi bellek aracı olması gerekirken 3 satırlık not defteri |
| **NPC yayalar (6)** | Dolaşıyor, farklı modeller | Adsız, amaçsız, konuşmuyor; "kalabalık" işlevi bile görsel olarak zayıf |
| **NPC satıcılar (4)** | Etkileşimde 3 rastgele cümle | Adları ve ilişkileri yok; dükkânlar dükkân gibi işlemiyor |
| **İki kişilik taşıma** | Çalışıyor | Yalnızca paletlerde; palet görevleri iki kişi şartı koymuyor |
| **Telemetri** | Yazıyor | Doğru, ama oyuncuya görünmüyor (iyi) |

## 5. Yaratıcı kimlik: şu an ne var
- **Ad:** "Who Broke It?" ("Kim bozdu?") güçlü bir soru ve oyun bu soruya **hiç yaklaşmıyor**. Soru yalnızca eski vakanın ifşa ekranında soruluyor.
- **Ton:** STORY.md'nin tonu ("kuru mizah, bürokratik resmî dil + kasaba ağzı") **tutarlı ve değerli**. Oyunda yalnızca Fitil'in telsiz cümlesi bu tonu taşıyor.
- **Mekân kimliği:** Greenfield bölgeleri GDD'de isimli (Reclaimer Yard, Main Street, Old Workshop, Waterworks, Community Park, Freight Depot, Hilltop Relay), sahnede ise meydan, ana cadde, atölye, depo, inşaat, tepe, su kulesi var. İsimlendirme iki dilde ve iki sözlükte dağınık.
- **Karakter kimliği:** 5 oyuncu karakteri + 6 NPC modeli (Doku üretimi) kaliteli ve ayrık; ama **hiçbirinin bir adı, geçmişi, rolü yok**. İşçi, mühendis, aşçı, robot, kedi, kurbağa: jenerik "dilediğin kahraman".
- **Görsel dil:** Menü, meydan, ana cadde, atölye gayet uyumlu pastel sıva/kiremit dünyası. Karakterler (yapay zekâ ürettiği, daha gerçekçi doku) ile prop'lar (palet atlası) arasında **stil kayması** var (ekran görüntülerinde işçi, daha yumuşak/gerçekçi boyalı; props düz renkli).

## 6. Gerçek oyuncu açısından temel eksikler (önem sırasıyla)
1. **Amaç yok.** Hikâye cümleleri 4; hedefler birbirinden kopuk ve gerekçesiz.
2. **Dünya tepki vermiyor.** Hiçbir görev görünür bir sonuç bırakmıyor, hiçbir şey kalıcı değil.
3. **Karakterler boş.** Fitil dışında konuşan, ilişkisi olan, gelişen kimse yok.
4. **Mekânlar bağlı değil.** Su kulesi, tepe, depo, inşaat, cadde birbirinden bağımsız dekor/oyun alanı.
5. **Sistemler hikâye taşımıyor.** Forklift, bisiklet, çekiç, el arabası görevsiz.
6. **Arayüz tutarsız.** İki görev paneli, ham kontrol satırı, varsayılan yazı tipi.
7. **Fizik/araç hissi kanıtlanmadı.** Kullanıcı geri bildirimi: "fizik diye bir şey yok." (Hâlâ açık; ayrı bir teknik iş.)
8. **İlk deneyim kılavuzsuz.** Tutorial yok, ilk 60 saniyede yönlendirme yok.
9. **Gerçek çok oyunculu doğrulama yok.** Tek gerçek internet oturumunda uzak oyuncu donuk göründü (neden belirsiz).

## 7. Korunması gerekenler (silinmeyecek)
Çok oyunculu altyapı (NGO, Kale sunucuları, eşleştirme), sunucu otoriter taşıma/tamir/yıkım/forklift, el arabası/bisiklet/kaykay, iki kişilik taşıma, sohbet ve ifadeler, şapkalar, yerelleştirme (TR/EN), NPC altyapısı (Npc, NpcVendor, NpcCatalog), karakter hattı (T-poz rig, diz kemikleri), sözleşme altyapısı (ContractDirector, bölgeler, oklar), tanılama (NetDiag, telemetri), yayın hattı. **Yeniden yazılmayacak**; yalnızca yeniden **bağlanacak** ve hikâyeye hizmet ettirilecek.

## 8. Teknik gerçeklik özeti (dürüst)
- Derleme: yeşil; 17/17 iki istemcili test, palet/el arabası/yerleşim testleri geçiyor.
- Doğrulanmayan: gerçek oyuncu hissi (araç sürüşü, taşıma), gerçek internet çok oyunculu kalitesi, yeni karakterlerin tüm pozları, Steam yok.
- Teknik borç: araç fiziği "elle yazılmış hız" modelinde; ağ tahmini sunucuya yumuşak yaklaşıyor; kalıcı dünya (kayıt) yok.

## 9. Onay beklenen kararlar
| # | Karar | Önerim |
|---|---|---|
| **K1** | `ISTEKLER.md`'deki "tek senaryolu değildir / metinler genel kullanımlı" ilkesini kaldırıp tek kanonik hikâyeye geçmek | **Evet**; rastgelelik yalnızca isteğe bağlı görevlerde (brif md. 3) |
| **K2** | İki görev sistemini (eski vaka + sözleşmeler) tek bir "Kasaba Defteri" çatısında birleştirmek | Evet. Eski vaka, ana hikâyenin 1. bölümü olur; sözleşmeler "DÜZEN Talepleri" olur |
| **K3** | Ana hikâye: DÜZEN, Fitil'in sırrı, Ada, Muhtar, sincap Mehmet (`NARRATIVE_REDESIGN.md`) | Onayına sunuyorum; değişiklik istersen yazarım |
| **K4** | Karakter dokuları 2048² kalsın mı (kalite) yoksa 1024²'ye inip SANAT_YONU'na mı uyulsun? | Kalsın; belgeyi güncelleyeyim |
| **K5** | Dünya kalıcılığı: host'un yerel kaydı (sürümlü JSON) | Evet, MVP'de bu |
| **K6** | NPC'ler yeniden adlandırılıp rol verilecek, anonim yayalar azaltılacak | Evet (`CHARACTER_BIBLE.md`) |
| **K7** | Ana görev sayısı: dikey dilim için **12 ana görev, 6 bölge** | Evet |

## 10. Faz 1 kabul ölçütleri (bu belge için)
- [x] Tüm yaratıcı belgeler okundu ve çelişkiler listelendi (§3).
- [x] Gerçek oyuncu deneyimi ekran görüntüleri ve koddan çıkarıldı (§2).
- [x] Amaçsız sistemler listelendi (§4).
- [x] Korunacaklar açıkça yazıldı (§7).
- [ ] **Kurucu onayı** (K1–K7) → Faz 2 kilidi.
