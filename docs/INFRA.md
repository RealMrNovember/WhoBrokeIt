# Altyapı (sır içermez)
Ağ: Tailscale. SSH: `ssh -p 6466 root@<tailscale-ip>` (Kale/Kale2); Doku2: `ssh doku2`.

| Rol | Sunucu | Not |
|---|---|---|
| Oyun alanı, site, ileride dedicated | Kale (cicibyte-kale, 31.40.199.47) | `/srv/whobrokeit`, Docker ağı `whobrokeit`. 4 CPU / 7 GB |
| Diğer tüm siteler, mail, sırlar | Kale2 (cicibytetechnology, 187.126.19.251) | sırlar `/root/.secrets` |
| Unity build + grafik | Doku2 (16 çekirdek, RTX 5060 8 GB) | `~/whobrokeit`, imaj `unityci/editor:ubuntu-6000.3.0f1-windows-mono-3` |
| Grafik (paralel) | Doku | |

Site: https://whobrokeit.cicibyte.com (Kale, Docker nginx, Cloudflare Origin sertifikası → 2041).
Ağır transferler sunucudan sunucuya yapılır; kullanıcı laptopu mobil kotalıdır.

## Sürüm yayını ve otomatik güncelleme (2026-11-04)
- Yayın akışı (Doku): `~/whobrokeit/release.sh "not"` → Windows build → `publish.sh` → `dist/WhoBrokeIt-<sürüm>.zip` + `dist/latest.json` (sürüm numarası `unity/VERSION`).
- Doku, `dist/` klasörünü yalnızca Tailscale'e açar: konteyner `wbi-dist` (nginx, 100.83.19.28:8099, `--restart unless-stopped`).
- Kale'deki site (`whobrokeit.cicibyte.com`) `/builds/latest.json` ve `/builds/WhoBrokeIt-*.zip` isteklerini Doku'dan çeker ve diskinde önbelleğe alır (`/srv/whobrokeit/site/cache`; Doku kapalıyken de eski sürümler sunulur). Başka yol 404.
- Oyun açılışta `latest.json`'ı okur, yeni sürümü indirir, SHA-256 doğrular, oturumda değilse kendini günceller (`UpdateChecker`, Windows). Kapatma: `-noupdate`; Steam sürümünde `STEAM_BUILD` ile devre dışı.
- Test araçları (Doku): `smoke.sh` (Linux build + iki süreç), `editor-smoke.sh` (Editor PlayMode, host tarafı tam vaka akışı).
- Site kaynağı: `site/` (statik). Dağıtım: `tar czf - -C site . | ssh -p 6466 root@<kale> 'cd /srv/whobrokeit/site/html && tar xzf -'`.
