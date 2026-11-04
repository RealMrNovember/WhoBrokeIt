#!/bin/bash
# Who Broke It? - laptop: tools/blender klasorunu Doku'ya (~/whobrokeit/blender/tools) kopyalar. Silme yapmaz.
# Unity tarafi (Assets/Editor/KitImportValidator.cs) icin ayrica: bash tools/mp/sync-to-doku.sh
set -eu
cd "$(dirname "$0")/../.."
ssh doku 'mkdir -p ~/whobrokeit/blender/tools'
tar czf - -C tools/blender --exclude='__pycache__' . | ssh doku 'cd ~/whobrokeit/blender/tools && tar xzf - && chmod +x *.sh'
echo "kopyalandi: doku:~/whobrokeit/blender/tools"
