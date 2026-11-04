#!/usr/bin/env python3
"""Kale site nginx.conf: sabit /download adresi + site dosyaları için no-cache. Tek seferlik, tekrar çalıştırmak güvenli."""
p = "/srv/whobrokeit/site/nginx.conf"
s = open(p).read()

download = '''  # Sabit indirme adresi: her zaman en son kurulum dosyası (JavaScript gerekmez)
  location = /download {
    proxy_pass http://100.83.19.28:8099/WhoBrokeIt-Setup-latest.exe;
    proxy_cache builds; proxy_cache_valid 200 60s;
    proxy_cache_use_stale error timeout updating http_502 http_503 http_504;
    proxy_http_version 1.1;
    proxy_hide_header Content-Disposition;
    add_header Content-Disposition 'attachment; filename="WhoBrokeIt-Setup.exe"' always;
    add_header Cache-Control "no-cache" always;
  }

'''
if "location = /download" not in s:
    s = s.replace("  location /builds/ { return 404; }", download + "  location /builds/ { return 404; }", 1)

old = "  location / { try_files $uri $uri/ =404; }"
new = '  location / { try_files $uri $uri/ =404; add_header Cache-Control "no-cache" always; }'
if old in s:
    s = s.replace(old, new, 1)

open(p, "w").write(s)
print("nginx.conf guncellendi")
