#!/bin/bash
# Who Broke It? - projeye beceri (SKILL.md) kurulumu. Yalnızca düz metin SKILL.md dosyaları indirilir (betik/ikili dosya YOK),
# depo sürümü (commit) sabitlenir, indirilenler şüpheli ifadeler için taranır. Hedef: <repo>/.claude/skills/<ad>/SKILL.md
# Kaynaklar: gamedev-skills/awesome-gamedev-agent-skills (Apache-2.0), Unity-Technologies/skills (Unity Companion License).
# Bilerek kurulmayanlar: Unity-Technologies/skills physics-3d-collision (ajana zorlayıcı talimatlar içeriyor).
set -eu
cd "$(dirname "$0")/../.."
DEST=.claude/skills
mkdir -p "$DEST"
R1=gamedev-skills/awesome-gamedev-agent-skills
R2=Unity-Technologies/skills
S1=$(git ls-remote "https://github.com/$R1" HEAD | cut -f1)
S2=$(git ls-remote "https://github.com/$R2" HEAD | cut -f1)
echo "$R1 @ $S1"; echo "$R2 @ $S2"

get() { # repo sha yol ad
  mkdir -p "$DEST/$4"
  curl -fsSL "https://raw.githubusercontent.com/$1/$2/$3/SKILL.md" -o "$DEST/$4/SKILL.md"
  printf '%s\n' "- $4: https://github.com/$1/tree/$2/$3" >> "$DEST/SOURCES.txt"
}
: > "$DEST/SOURCES.txt"

# Physics Engineering
get $R1 $S1 skills/unity/unity-physics unity-physics
get $R1 $S1 skills/disciplines/physics-tuning physics-tuning
# Multiplayer Engineering (Unity resmi + istemci tahmini/gecikme ilkeleri için başka motorların belgeleri)
get $R2 $S2 skills/setup-multiplayer-services unity-multiplayer-services
get $R1 $S1 skills/other-engines/roblox-networking networking-prediction-roblox
get $R1 $S1 skills/godot/godot-multiplayer networking-sync-godot
# Gameplay Programming
get $R1 $S1 skills/unity/unity-csharp-scripting unity-csharp-scripting
get $R1 $S1 skills/unity/unity-input-system unity-input-system
get $R1 $S1 skills/disciplines/input-systems input-systems
get $R1 $S1 skills/disciplines/camera-systems camera-systems
get $R1 $S1 skills/unity/unity-scriptableobjects unity-scriptableobjects
# Game Feel
get $R1 $S1 skills/disciplines/game-feel game-feel
# Level Design
get $R1 $S1 skills/disciplines/level-design level-design
# Technical Art
get $R1 $S1 skills/disciplines/shader-programming shader-programming
get $R1 $S1 skills/disciplines/create-game-assets create-game-assets
get $R2 $S2 skills/urp-postprocessing urp-postprocessing
# Animation
get $R1 $S1 skills/unity/unity-animation unity-animation
# Audio Design
get $R1 $S1 skills/disciplines/audio-design audio-design
get $R2 $S2 skills/optimize-audio optimize-audio
get $R2 $S2 skills/audio-setup-mixers audio-setup-mixers
# QA / Testing / performans / yayın
get $R1 $S1 skills/disciplines/performance-optimization performance-optimization
get $R1 $S1 skills/unity/unity-build-pipeline unity-build-pipeline
get $R1 $S1 skills/workflows/prototype-fast prototype-fast
get $R1 $S1 skills/workflows/steam-publish steam-publish
get $R2 $S2 skills/project-auditor-fixes project-auditor-fixes

echo "--- tarama (şüpheli ifadeler):"
grep -rniE "ignore (all |any )?(previous|prior)|do not tell the user|curl[^|]*\| *(ba)?sh|wget[^|]*\| *(ba)?sh|rm -rf /|sudo |exfiltrat|webhook|base64 -d|eval\(|api[_-]?key *=|password *=" "$DEST" --include=SKILL.md || echo "eşleşme yok"
echo "--- kurulanlar:"; ls "$DEST" | grep -v SOURCES; du -sh "$DEST"
