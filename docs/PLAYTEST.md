# Dış oyun testi (P4) rehberi

Kapı: MVP.md §4. 5–10 kişi, 3–4 grup, her grup oyunu ilk kez oynar, test sırasında yardım yok.

## Başarı ölçütleri (MVP.md §4)
| Soru | Geçme ölçütü | Nasıl ölçülür |
|---|---|---|
| 20 dk içinde bir sözleşme döngüsünü bitirebildiler mi? | ≥%70 grup | telemetri `contract_done` ve gözlemci notu |
| Gülme/şaşırma anı | grup başına ≥3 | gözlemci sayar |
| "Birbirimize ihtiyaç duyduk" | ≥%80 oyuncu | anket madde 3 |
| "Tekrar oynarım / arkadaşa öneririm" | ≥%60 | anket madde 5 |
| Çökme veya soft-lock | 0 kritik | gözlemci + `Player.log` |

## Hazırlık
1. Son sürüm kurulumunu (`WhoBrokeIt-Setup-<sürüm>.exe`) indirme sayfasından ver. Oda kodu ile aynı odaya girsinler.
2. Telemetri **yalnızca host'ta** yazılır: `%USERPROFILE%\AppData\LocalLow\CiciByte\Who Broke It\telemetry.jsonl` (Unity'nin `persistentDataPath` klasörü). Ağa gönderilmez, ad ve adres içermez. Kapatmak için `PlayerPrefs` "wbi_telemetry" = 0.
3. Test öncesi oyunculara söyle: "Yardım yok, takılırsanız takılın. Takıldığınız yeri yüksek sesle söyleyin."

## Telemetri olayları (`telemetry.jsonl`, satır başına bir JSON)
`session_start`, `player_join` (players), `player_leave` (players), `session_end` (seconds, peak), `contract_start` (id), `contract_done` (id, score, left), `contract_fail` (id), `reset`, `bonk`, `spark`. Her satırda `t` (unix saniye) ve `v` (sürüm).

Özet çıkarma (PowerShell):
```powershell
Get-Content telemetry.jsonl | ForEach-Object { $_ | ConvertFrom-Json } | Group-Object e | Sort-Object Count -Descending | Format-Table Name,Count
```

## Gözlemci formu (grup başına)
- Grup no / oyuncu sayısı / tarih / sürüm:
- İlk sözleşmeyi kaç dakikada başlattılar? Bitirdiler mi? (evet/hayır, kaç dk)
- Gülme / şaşırma anları (zaman ve ne oldu, en az 3 bekleniyor):
- Takıldıkları yerler (menü, kontroller, sözleşme panosu, forklift, el arabası, alet seçimi):
- Soft-lock / çökme / garip fizik anları (zaman, ne yapıyorlardı):
- Kendiliğinden söylenenler (olduğu gibi yaz):

## Oyuncu anketi (1 = hiç, 5 = çok)
1. Oyun eğlenceliydi. (1–5)
2. Ne yapacağımı anlıyordum. (1–5)
3. Arkadaşlarıma ihtiyaç duydum. (1–5)
4. Kontroller (yürü, taşı, araçlar) rahattı. (1–5)
5. Tekrar oynarım / arkadaşa öneririm. (1–5, "4–5" = olumlu)
6. En güldüğüm an:
7. En sinir bozucu an:
8. Bir şeyi değiştirebilseydim:
9. Bu oyuna ne öderdim? (ücretsiz / 5 / 10 / 15 / 20 USD ve üstü)

## Sonuç tablosu (test sonunda doldurulur)
| Ölçüt | Hedef | Sonuç | Geçti mi |
|---|---|---|---|
| Döngüyü bitiren grup | ≥%70 | | |
| Gülme anı / grup | ≥3 | | |
| "İhtiyaç duyduk" | ≥%80 | | |
| "Tekrar oynarım" | ≥%60 | | |
| Kritik hata | 0 | | |

Geçilmezse P3'e dönülür, Steam'e çıkılmaz.
