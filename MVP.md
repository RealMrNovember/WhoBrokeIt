# Who Broke It? — MVP Kapsamı (v0.1)

Amaç: **En kısa sürede, hafif, oynanabilir** bir build'i YouTube oyuncularına dağıtmak.
`WhoBrokeIt.md` (GDD) uzun vadeli vizyon olarak aynen kalır; senaryo değişmez. MVP onun küçük bir kesitidir.

## Kural
GDD'deki her şey "sonra"dır, aksi yazılmadıkça. MVP'ye giren her şey tek cümleyle gerekçelendirilmelidir: *"YouTuber'ın 10 dk'lık videosunda bunu gösterir mi?"*

## MVP'ye GİRENLER
- Tek harita: Greenfield'ın küçük bir kesiti (Reclaimer Yard + Workshop), primitive/low-poly.
- 2–4 oyunculu co-op (arkadaşıyla katıl: oda kodu / Steam'siz davet linki).
- Araçlar: eller (taşı), süpürge, tamir çekici. Forklift (imza komedi aracı).
- Tek proje: "Workshop'u yeniden aç" (GDD bölüm 24, adım 14–22).
- Görsel dönüşüm: karanlık/kirli → temiz/ışıklı.
- Temel HUD + sonuç ekranı. Windows build, ~200 MB altı hedef.

## MVP'den ÇIKANLAR (sonra)
Steam entegrasyonu, hesap/profil backend'i, kalıcı dünya, karakter özelleştirme, ekonomi, çok bölge, görev instance'ları, ses sohbeti, bina yapma, lokalizasyon (TR/EN hariç), moderasyon.

## Teknik karar önerileri (onay bekliyor)
| Konu | Öneri | Neden |
|---|---|---|
| Motor | Unity 6 (6000.3.0f1 kurulu) + URP | GDD ile uyumlu, kurulu |
| Ağ | Netcode for GameObjects + Unity Relay, **host oyuncu** | Dedicated sunucu/backend yok → en hızlı yol. Dedicated sonra gelir. |
| Dağıtım | itch.io / Steam anahtarı yerine önce **doğrudan zip + Cloudflare R2/Pages indirme sayfası** | Steam onayı beklemeden YouTuber'a gider |
| Sunucular | **Doku2: Unity build (Docker, GameCI imajı) + render**; Doku: ikinci grafik işçisi; Kale: indirme/landing, ilerde dedicated | Kullanıcı kararı |
| Ağır işler | Build/render/Unity import **laptopta çalışmaz**; Kale (build, Docker), Doku/Doku2 (grafik) | Kullanıcı kuralı |
| Saklama | Kalıcılık yok; sadece yerel ayar | Hız |

## Aşamalar
1. **M0** Unity projesi, karakter+kamera, test yard, 1 etkileşim, build.
2. **M1** Tek oyunculu Workshop döngüsü (süpür, tamir, taşı, ışıklar yanar).
3. **M2** 2–4 kişilik host/join co-op.
4. **M3** Forklift + komik fizik, cila, ses.
5. **M4** Landing sayfası + zip dağıtımı + YouTuber paketi (basın kiti, 3 ekran görüntüsü).

## Açık sorular
- Oyun adının "kim bozdu?" esprisi MVP'de nasıl görünür? (öneri: Workshop'ta kime ait olduğu belirsiz "suçlu ipuçları" notları — senaryoya dokunmadan sadece ton.)
