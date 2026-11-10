# Dünya tasarımı: Greenfield bir kasaba, bir sahne değil

Durum: **öneri, onay bekliyor**. Kanon: `NARRATIVE_REDESIGN.md`. Mevcut fiziksel yerleşim (`GREENFIELD_MEYDANI.md`, `GreenfieldBuilder.cs`) **korunur**; bu belge her mekâna anlam, ilişki ve dönüşüm ekler. Yeni bina eklenmez; var olanlar yeniden bağlanır.

## 1. İlke
Greenfield, birbirine yürüyerek bağlanan **altı bölge**dir. Her bölgenin bir **işlevi** (kasabaya ne sağlar), **ilişkisi** (hangi bölgeye bağlı) ve **dönüşümü** (aşamaya göre ne değişir) vardır. Oyuncu bir yeri tamir ettiğinde başka bir yerde **bir şey kıpırdar**.

## 2. Ağ: kasaba tek bir sistemdir
```
        ┌──────────────┐  su hattı   ┌────────────┐
        │ SU KULESİ    │────────────►│  MEYDAN    │◄──── DÜZEN Talep Panosu
        │ (Su İşleri)  │             │ çeşme+saat │
        └──────┬───────┘             └─────┬──────┘
   elektrik    │                           │ yol
        ┌──────▼───────┐             ┌─────▼──────┐   mal taşıma   ┌──────────┐
        │ ATÖLYE       │             │ ANA CADDE  │◄──────────────│  DEPO    │
        │ + Reclaimer  │             │ 4 dükkân   │   (palet,     │ rampa,   │
        │   Avlusu     │             └─────┬──────┘    forklift)  │ elektrik │
        └──────────────┘                   │                      │ panosu   │
                                  yol/tırmanış                    └────┬─────┘
                                     ┌─────▼──────┐   malzeme          │
                                     │ TEPE       │ ◄──────────  ┌─────▼─────┐
                                     │ rampa+Röle │              │ İNŞAAT    │
                                     └────────────┘              │ ALANI     │
                                                                 └───────────┘
```
- **Elektrik:** Röle (tepe) → Depo panosu → Atölye → Meydan fenerleri → Ana Cadde tabelaları.
- **Su:** Su Kulesi → Meydan çeşmesi → Ana Cadde (manav, çaycı) → İnşaat (çimento).
- **Mal:** Depo → (el arabası/forklift) → Ana Cadde ve İnşaat.
- **Bilgi:** Telsiz (Fitil) + DÜZEN terminalleri (Atölye, Meydan panosu, Röle) + Ada'nın notları (kasaba boyunca).

## 3. Bölgeler (mevcut sahne eşlemesi)
| Bölge | Sahnede nerede | İşlev (kasabaya ne sağlar) | Bağlı olduğu | Başlıca etkileşim |
|---|---|---|---|---|
| **Reclaimer Avlusu + Atölye** | Kuzey (Atölye), spawn çevresi | Alet, onarım, ilk ışık; Reclaimer üssü | Depo panosu (elektrik) | Süpürge, çekiç, sigorta kutusu, dolap |
| **Meydan** | Merkez (çeşme, saat kulesi, panolar) | Kasabanın kalbi: toplanma, **DÜZEN Talep Panosu**, çeşme | Su Kulesi (su), Atölye (ışık) | Pano, çeşme, bank, şenlik |
| **Ana Cadde** | Güney (4 dükkân cephesi, satıcı tezgâhları) | Ticaret, kasabalıların dönüş yeri | Meydan (su), Depo (mal) | Tezgâh kurma, el arabası, satıcılar |
| **Depo** | Batı (depo + yükleme rampası, elektrik panosu) | Lojistik: mal, paletler, forklift; **elektriğin ara istasyonu** | Röle (güç), İnşaat (malzeme) | Forklift, iki kişilik palet, panel tamiri |
| **İnşaat Alanı** | Doğu (iskele, yarım duvar, baraka) | Kasabanın büyüme yeri; yıkım/onarım vitrini | Depo (malzeme) | Yarım duvar, iskele, çökme |
| **Tepe + Röle İstasyonu** | Güneydoğu rampa/tepe → röle | Eğlence (rampa) ve **DÜZEN'in çekirdeği** | Röle → tüm elektrik | Bisiklet/kaykay, tırmanış, iki düğme |
| **Su Kulesi (Su İşleri)** | Kuzeybatı landmark | **Su basıncı merkezi**; kuledeki forklift gizemi | Meydan, Ana Cadde | Vanalar, borular, forklift indirme |

> Fiziksel not: "Su İşleri" ve "Röle" şu an **dekor/rampa**. Bu iki mekân için küçük ama gerçek **iç mekân/etkileşim** üretilecek (Faz 3, yeni bina değil, mevcut kule gövdesinin ve tepe kulübesinin içi).

## 4. Dönüşüm aşamaları (kalıcı dünya)
GDD §2.4 beş aşamayı **bölge başına** uygular. Aşama, ana görev bayraklarıyla ilerler ve host'un kayıt dosyasında saklanır.

| Aşama | Genel görünüm | Işık | Ses | Kalabalık | Etkileşim |
|---|---|---|---|---|---|
| 0 **Terk** | Kirli, bozuk; kapılar kilitli; neon sönük | Bulutlu, soluk, gri ton | Rüzgâr, uzak metal | Boş; yalnızca Mehmet ve birkaç yaya | Yalnızca temel |
| 1 **Temizlendi** | Enkaz kalktı, yollar açık | Biraz sıcak | Tek tük aletler | Satıcı tezgâhı belirir | Taşıma/temizlik |
| 2 **Çalışır** | Elektrik/su geldi; lambalar, çeşme | Altın saat başlar | Çeşme, hum, radyo | Yayalar yürür | Pano, tezgâh |
| 3 **Onarıldı** | Cepheler boyalı, tabelalar yeni | Altın saat tam | Müzik kutusu motifi | Kasabalılar işte | Yeni görevler |
| 4 **Canlı** | Fener, çiçek, afiş, kalabalık | Sıcak, canlı, bloom | Kalabalık uğultusu, şenlik | Tam kalabalık, çocuklar | Şenlik etkinlikleri, isteğe bağlı talepler |

### Bölge × görev tablosu (hangi görev hangi görünür sonucu bırakır)
| Görev | Bölge | Görünür sonuç |
|---|---|---|
| M1 Işıklar Yansın | Atölye | Pencereler yanar, terminal açılır, DÜZEN ilk bildirimi |
| M3–M4 Su / Çeşme | Su Kulesi → Meydan | Çeşme akar, ses gelir, bank dolar, Muhtar dönmüş |
| M5 Açık Dükkânlar | Ana Cadde | Tabelalar yanar, tezgâhlar dolu, **4 satıcı** yerinde |
| M6 Depo Düzeni | Depo | Kamyon yuvası açık, kamyon gelir, depo lambaları yanar |
| M7 Yarım Duvar | İnşaat | Duvar tamam, baraka ayakta, iskele düzenli |
| M8 Forklift Kulede | Su Kulesi | Forklift yerde, kule sundurması boşalır; Fitil'in telsiz sesi değişir |
| M9 Posta | Tüm kasaba | Ada'nın notları toplandı: tabela/fener renkli bayraklarla süslenir |
| M10–M11 Röle / Yeterli | Tepe + hepsi | Tüm fenerler yanar, **Şenlik**: kasaba tam kalabalık |

**Kural:** Oyuncu ana görev bitirdiğinde **bir yerde** görünür, işitilir bir değişiklik olur. "Puan arttı" yeterli sonuç değildir.

## 5. Nüfus: kasabalıların işlevi
Anonim yaya yoktur. Her insan bir **neden**le oradadır ve **dönüşün bir aşamasında** gelir (ayrıntı CHARACTER_BIBLE.md).
| Kim | Nerede | İşlevi | Gelir (görev sonrası) |
|---|---|---|---|
| **Muhtar Hayri** | Meydan | Kasabanın başkanı; ana görev vericisi (Perde 2–3) | M4 |
| **Manav Nuri** | Ana Cadde | Mal kaynağı; el arabası görevleri | M5 |
| **Çaycı Zehra** | Ana Cadde | Dedikodu: ipucu ve ipucu merkezi; kasaba defteri **hatırlatıcısı** | M5 |
| **Aletçi Rıza** | Ana Cadde | Alet ve (ileride) basınçlı yıkayıcı; çekiç onarımı | M5 |
| **Çiçekçi Melek** | Ana Cadde → park | Şenlik ve çiçek: **şapka** ödülleri, dekor | M5 / M11 |
| **Postacı** | Kasaba | Ada'nın notları, bisiklet/kaykay görevi (M9) | M9 |
| **Yaşlı Cemal** | Bank (meydan) | Kasaba hafızası: geçmişi anlatır, eski Büyük Kesinti'yi hatırlar | M4 |
| **Koşucu, Turist** | Cadde, tepe | Hafif kalabalık: **rota kullanan** NPC (turistin rotası: kule → tepe), koşucu tepe rampasını kullanır | M5+ |
| **Ada** | Röle (saklı), notlarla her yerde | Teknisyen; M9'dan sonra teknik uzman | M9 |
| **Mehmet (sincap)** | Her yerde, kablolar yakınında | Çevre olayı; kaos kaynağı; M1'de ilk | M1 |

## 6. Ulaşım
| Araç | Kasabadaki **işlevi** |
|---|---|
| **El arabası** | Ana Cadde mal taşımacılığı (Manav ve Aletçi talepleri) |
| **Forklift** | Depo lojistiği; M8'de kuleden indirme; kamyon boşaltma |
| **Bisiklet** | Postacı rotası (M9); kasaba turu; Ada'nın notları |
| **Kaykay** | Tepe rampası; hızlı yaya (Ada'nın notlarına ulaşmak için kestirme) |
| **İki kişilik taşıma** | Ağır nesneler (palet, tezgâh, forklift gövde parçası): koordinasyon |

## 7. İç mekânlar (üretilecek, küçük ve gerçek)
- **Atölye:** DÜZEN terminali, alet duvarı, pano. (Var, terminal eklenecek.)
- **Su İşleri (kule gövdesi içi):** 3 vana, 2 basınç göstergesi, ters akış borusu.
- **Röle kulübesi (tepe):** iki düğme, DÜZEN çekirdeği terminali, Ada'nın çalışma masası.
- **Hırdavatçı** (mevcut D2, içi girilir): Aletçi Rıza'nın dükkânı.
Diğer dükkânlar cephe + küçük sığ iç.

## 8. Ölçek ve yürüme süreleri (korunur)
Mevcut yerleşim 90×70 m; spawn → depo ≤20 sn, depo → inşaat forkliftle ≤25 sn. Yeni etkileşimler bu süreleri artırmaz; yeni yol/binalar eklenmez.

## 9. Faz 3–4 kabul ölçütleri (dünya)
- [ ] Altı bölgenin her biri için aşama 0–4 görünüm listesi (bu belge §4) koda bayrak olarak bağlı.
- [ ] M1–M5 sonucunda **en az 5 görünür dönüşüm** (ışık, su, açık dükkân, ses, kalabalık).
- [ ] Anonim yaya sayısı sıfır; her NPC'nin adı, işlevi, dönüşte geldiği aşama var.
- [ ] Oyuncu bir önceki oturumda onardığı yeri **kayıttan** geri bulur (host kaydı).
