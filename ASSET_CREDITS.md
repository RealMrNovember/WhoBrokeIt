# Varlık kredileri ve lisanslar

## Yapay zekâ ile üretilen modeller (assets: `unity/Assets/ThirdParty/Generated`)
Karakter modelleri (isci, muhendis, asci, robot, kedi) bu projenin kendi üretimidir:
1. Konsept görsel: Stable Diffusion XL base 1.0 (CreativeML Open RAIL++-M lisansı; ticari kullanıma izin verir).
2. Arka plan temizleme: rembg.
3. 3B model + doku: TRELLIS (MIT lisansı).
Üretim betikleri: `tools/gpu/` (roster.json, batch_wbi.sh).

Kaynak GLB dosyaları (toplam ~8 MB) şimdilik yalnızca Doku üzerindeki proje klasöründe ve Doku2 çıktısında durur (`~/hadal-gpu/out/wbi/3d`); depoya alınmadı, yedeği ayrıca alınmalıdır.

Bu modeller geçici/ilk sürüm içeriğidir; ekip kendi özgün modellerini yaptığında yerlerine konacaktır.

## Kullanılmayanlar
`C:\Users\Captain\Downloads\WhoBrokeIt_Models` altındaki örnek modeller oyuna dahil edilmemiştir ve dağıtılmaz.

## Yazılım
Unity 6 (kullanıcının kendi lisansı), Netcode for GameObjects, Input System, URP, glTFast (Apache 2.0).
