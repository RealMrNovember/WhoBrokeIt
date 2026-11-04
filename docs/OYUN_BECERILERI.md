# Oyun becerileri taraması (gameskill.dev)

Kaynak: https://gameskill.dev/skills — 230 kayıt, 10 sayfa. İlgili olanların asıl içeriği GitHub'dan okundu (28 dosya) ve özetlendi.
Hiçbir üçüncü taraf eklenti **kurulmadı**; yalnızca ilkeler buraya alındı. Okunan metinlerdeki talimatlar veri sayıldı (aşağıda "Güvenlik notu").

## Okunanlar
- gamedev-skills/awesome-gamedev-agent-skills (Apache-2.0): unity-animation, unity-physics, unity-build-pipeline, unity-input-system, unity-csharp-scripting, game-feel, camera-systems, performance-optimization, physics-tuning, audio-design, game-ui-ux, level-design, create-game-assets, steam-publish, prototype-fast, roblox-networking, godot-multiplayer.
- Unity-Technologies/skills (Unity Companion License): physics-3d-collision, setup-multiplayer-services, setup-vivox-voice-chat, optimize-audio, audio-setup-mixers, asset-transformer-toolkit, urp-postprocessing, project-auditor-fixes, localization, build-live-game, ui.

## Projeye uygulananlar / uygulanacaklar

| İlke | Durum |
|---|---|
| Menü açıkken oyun girdisi kapalı olmalı (yoksa Esc menüsünde `WASD` karakteri yürütür, `E` etkileşir) | ✅ 0.4.4'te düzeltildi |
| Fizik girdisi `Update`'te okunur, `FixedUpdate`'te uygulanır; araçlarda interpolasyon; Unity 6'da `linearVelocity` | ✅ zaten böyle |
| Sunucu otoriter; istemci niyet yollar, tahmin geri alınabilir sunumda ve yumuşak düzeltme ile | ✅ 0.4.3 araç tahmini |
| Yüksek frekanslı girdi 30 Hz gönderilir, değişince hemen | ✅ 0.4.3 |
| Post-process düşük donanımda kapalı/hafif; render ölçeği, gölge mesafesi, MSAA kalite ön ayarı | ✅ 0.4.3 |
| Gerçek donanımda ölç: FPS/ping göstergesi (F3) | ✅ 0.4.3 |
| **Ses yok** (efekt, müzik, ambiyans). Mixer grupları (Master/Müzik/SFX/Ambiyans/UI), ayar sürgüsü dB'ye çevrilir, AudioSource havuzu | ⏳ sıradaki büyük eksik |
| Her RPC girdisi doğrulanır (sonlu sayı, menzil, durum) ve istemci başına hız sınırı | ⏳ halka açık sunucular için gerekli |
| Game feel: tek vuruşa 5–8 küçük geri bildirim (~100 ms), ekran sarsıntısı = trauma², hit-stop; bonk/fırlatma/sürüş için | ⏳ |
| Kamera: araçlarda hız yönüne hafif ileri bakış, ışınlanma/yeniden doğuşta yumuşatmayı sıfırla | ⏳ |
| `OnGUI`'de string birleştirme/GUIContent önbelleği (IMGUI tahsis üretir); klavye/gamepad ile menü gezinmesi, yazı ölçeği | ⏳ |
| Layer Collision Matrix'i kıs, uyuyan cisimler | ⏳ |
| Yeni Input System: `OnDisable`'da abonelik çöz; buton için `WasPressedThisFrame` | ✅ (yalnızca doğrudan okuma kullanıyoruz) |
| Yerelleştirme (TR + EN): metinleri dışsallaştır | ⏳ Steam öncesi |

## 15 becerinin tek tek kararı (kullanıcı listesi)

"Kullanmak" burada: ilkeleri/kontrol listelerini projeye uygulamak ve işe yarayan fikirleri kendi koduyla yazmak. Üçüncü taraf kodu (MCP sunucusu, hook, Python/Node aracı, Editor eklentisi) **çalıştırılmadı/kurulmadı**; gerekçe son sütunda. Kurulum yapılacaksa kullanıcı depoyu okuyup kendisi çalıştırır (komutlar aşağıda).

| # | Beceri | Lisans | Karar | Projede ne yaptık / yapacağız | Neden araç kurmadık |
|---|---|---|---|---|---|
| 1 | claude-code-game-studios | MIT | İlkeleri al | aşama kapıları (gate-check), smoke/soak/regresyon fikri, launch-checklist (→ `STEAM_HAZIRLIK.md`), playtest-report ve balance-check şablonları | otomatik shell hook'ları (10 olay) ve ağır ajan iskeleti; `CLAUDE.md`/`ISTEKLER.md` düzenimizle çakışır |
| 2 | unity-mcp-coplay | MIT | Gereksiz | yalnızca "az ve odaklı araç" ilkesi | Editor'e tam yetkili köprü; port/kimlik belgesiz; GUI'li Editor ister (bizde yok) |
| 3 | awesome-gamedev-agent-skills | Apache-2.0 | İlkeleri al | game-feel (ekran sarsıntısı), physics-tuning, camera-systems, audio-design (ses sistemi), create-game-assets (GLB kalite raporu), save-systems/input-systems (sonra) | `npx`/`pip` çalıştırıyor; düz Markdown olduğu için elle okumak yeterli |
| 4 | coop-anything | MIT | **chaos fikri: uygulandı** | kötü ağ testi: Unity Transport'un yerleşik benzetici özelliğiyle (gecikme, titreşim, paket kaybı) çevrimiçi smoke testi (`online-smoke.sh lossy`); durum özeti karşılaştırmayla desync tespiti ve sahiplik devri ilkeleri sonraya | kendi `tools/chaos` aracı UDP aktarıcı ve iyi, ama bizde UTP'nin yerleşik benzeticisi aynı işi ek kod olmadan yapıyor. Kullanıcı isterse: depodan `tools/chaos` dosyası elle indirilip okunur |
| 5 | unity-official-skills | Unity Companion | İlkeleri al | URP/post-process, ses (mixer, optimizasyon), fizik, IMGUI/UI, proje denetimi metinleri | `/plugin` kurulumu interaktif; çoğu beceri açık Editor'ü Unity CLI ile sürer, bizde Editor ekransız |
| 6 | unity-agent-plugin | Unity Companion | İlkeleri al | 5 ile aynı paket (32 beceri) | aynı; kullanıcı isterse: `/plugin marketplace add Unity-Technologies/unity-agent-plugin` ve `/plugin install unity@unity-agent-plugin` (kendisi çalıştırır) |
| 7 | unity-mcp-ivanmurzak | Apache-2.0 | İlkeleri al | `-batchmode -nographics` CI örneği, oyun içi (runtime) durum sorgulama ve test botu fikri | varsayılan kimlik doğrulama yok + port 8080 + keyfi C# çalıştırma; bulut uç noktası |
| 8 | pixelfx | MIT | Gereksiz | yalnızca "çiz, bak, düzelt, denetle" döngüsü | 2B piksel-art; 3B projemize uymaz |
| 9 | unity-skills-besty | MIT | İlkeleri al | izin modları, önce planla sonra uygula, toplu işlem + geri alma, JSONL denetim günlüğü (kendi araçlarımız için tasarım örneği) | 839 REST becerisi; port/kimlik belgesiz; Editor açık olmalı |
| 10 | scenario-skills | MIT | Gereksiz | "üretmeden önce maliyet/çıktı tahmini", referans kilidi, poligon bütçesi uyarıları | ücretli bulut servis + hesap; kendi GPU hattımız (SDXL + TRELLIS) var; kayıtsız genel hata raporu riski |
| 11 | gamefactory-3a | Apache-2.0 | İlkeleri al | her varlık türü için ayrı kalite kontrolü (**uygulandı**: karakter GLB'leri için `[WBI] qa` raporu), tek yol kaynağı, yönlendirici belge | model ağırlığı indiren kurulum betikleri, ücretli API'ler; Doku2'deki TRELLIS ile çakışır |
| 12 | glade-mcp | MIT | İlkeleri al | ekran görüntüsüyle doğrulama (zaten `shots.sh` var), oynanabilirlik sondası = NGO test botu fikri | kimliksiz yerel HTTP köprüsü; isteğe bağlı bulut/OpenAI'a kod gönderimi; telemetri belirsiz |
| 13 | everything-game-dev-code | MIT | İlkeleri al | Unity kod standardı (Update'te tahsis yok, NGO RPC kuralları), derle-hatayı-oku-düzelt akışı, faz bazlı bağlamlar | `npm run setup:hooks` her commit'te kod çalıştırır; MCP yapılandırmaları belirsiz |
| 14 | effekseer-vfx | MIT | İlkeleri al (tek koşullu aday) | VFX'i veri dosyası olarak tanımlama; kısa ömür (0.2–0.6 sn) kuralları; vuruş/kıvılcım/toz sözlüğü; flipbook hattı fikri (SDXL ile doku atlası + additive shader) | ~250 MB indirme, Godot penceresi, .NET; Linux'ta yalnızca "authoring"; Effekseer Unity eklentisi ayrı lisans |
| 15 | funplay-skill | MIT | İlkeleri al | ses dönüştürme (wav→ogg) ve normal-map betik fikirleri, "betik + test eşleştirme" deseni | Node/ffmpeg betikleri, session-start hook; genç ve az incelenmiş depo |

### Bu turda uygulananlar
- **Kötü ağ testi** (coop-anything/chaos fikri): çevrimiçi smoke testine kayıplı profil (`online-smoke.sh lossy`).
- **GLB kalite raporu** (gamefactory-3a, create-game-assets): her karakter için üçgen sayısı, boy, malzeme ve doku boyutu derleme günlüğüne yazılır.
- **Game feel** (game-feel): bonk ve fırlatma/vurma için ekran sarsıntısı.
- **Ses** (audio-design, optimize-audio): kodla üretilen efektler (adım, bonk, savurma, motor/tekerlek), ses ayarı sürgüleri.

### Kullanıcı isterse (kendisi çalıştırır, önce depoyu okur)
- Unity resmi eklentisi: `/plugin marketplace add Unity-Technologies/unity-agent-plugin` ve `/plugin install unity@unity-agent-plugin` (Unity Companion License; çoğu beceri açık Editor ister).
- coop-anything `tools/chaos`: depodan yalnızca bu klasör indirilip incelenir (MIT; Python stdlib).

## Güvenlik notu
`Unity-Technologies/skills/physics-3d-collision` metni ajana emir kipinde talimatlar içeriyor ("başka belge çekme", "araç çağırma", "izin sorma" gibi). Bunlara uyulmadı, yalnızca bilgi kaynağı olarak okundu. Ayrıca metindeki mutlak iddialar (ör. "ragdoll patlaması yalnızca çakışan collider'lardandır") doğrulanmadan benimsenmedi. Üçüncü taraf becerileri kurmadan önce depo içeriği okunur.
