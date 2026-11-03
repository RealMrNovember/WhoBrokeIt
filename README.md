# Who Broke It?

Kooperatif, fizik komedili "dünyayı onar" oyunu (Unity 6, URP, Windows/Steam hedefi).
Hızlı, hafif bir MVP ile başlıyoruz; büyük vizyon için bkz. [GDD](WhoBrokeIt.md), MVP kapsamı için [MVP.md](MVP.md).

## Dosyalar
| Dosya | Amaç |
|---|---|
| `WhoBrokeIt.md` | Uzun vadeli GDD/PRD (vizyon) |
| `MVP.md` | İlk dağıtılabilir sürümün kapsamı |
| `ROADMAP.md` | Aşamalar ve durum |
| `DECISIONS.md` | Mimari/altyapı kararları |
| `CHANGELOG.md` | Yapılan değişiklikler |
| `docs/INFRA.md` | Sunucular ve çalışma düzeni (sır içermez) |
| `docs/STORY.md` | Kurgu ve anlatım taslağı |

## Kurallar
- Ağır işler (Unity build, render, Docker build) laptopta ÇALIŞMAZ → Doku2 / Doku. Bkz. `docs/INFRA.md`.
- Sırlar repoya girmez (`/root/.secrets`, Kale2).
