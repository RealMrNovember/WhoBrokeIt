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
