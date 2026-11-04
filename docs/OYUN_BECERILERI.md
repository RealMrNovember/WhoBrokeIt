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

## Kurmaya değer bulunanlar (kullanıcı onayıyla)
- **Unity resmi eklentisi** (Unity-Technologies/unity-agent-plugin, Unity Companion License): `optimize-audio`, `audio-setup-mixers`, `urp-postprocessing`, `project-auditor-fixes` işimize yarar. Kurulum interaktif komuttur (`/plugin marketplace add Unity-Technologies/unity-agent-plugin`); çoğu beceri açık bir Unity Editor + Unity CLI ister, bizim derleme sunucuda batchmode olduğu için şimdilik yalnızca ilkeleri okuduk.
- **awesome-gamedev-agent-skills** (Apache-2.0): `unity-animation`, `unity-physics`, `game-feel`, `camera-systems`, `physics-tuning`, `performance-optimization`, `steam-publish`, `audio-design`. Kısa kontrol listeleridir.

Şimdilik gereksiz: Vivox ses sohbeti ve UGS canlı servisleri (kendi dedicated sunucumuz var), Pixyz tabanlı asset-transformer, yerelleştirme paketi (sonra), Godot/Roblox/Unreal becerileri.

## Güvenlik notu
`Unity-Technologies/skills/physics-3d-collision` metni ajana emir kipinde talimatlar içeriyor ("başka belge çekme", "araç çağırma", "izin sorma" gibi). Bunlara uyulmadı, yalnızca bilgi kaynağı olarak okundu. Ayrıca metindeki mutlak iddialar (ör. "ragdoll patlaması yalnızca çakışan collider'lardandır") doğrulanmadan benimsenmedi. Üçüncü taraf becerileri kurmadan önce depo içeriği okunur.
