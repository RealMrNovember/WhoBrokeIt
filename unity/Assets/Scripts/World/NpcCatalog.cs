using System.Collections.Generic;
using UnityEngine;
using WhoBrokeIt.Player;

namespace WhoBrokeIt.World
{
    /// <summary>
    /// The looks of the ambient characters (pedestrians and stall vendors). They are separate, lighter models than the playable characters
    /// (Resources/Npcs/&lt;key&gt;.prefab, built by AiAssets from Assets/ThirdParty/Generated/npc_&lt;key&gt;.glb) so a passer-by never looks like a
    /// player. When a prefab is missing the look falls back to one of the playable characters, so the scene always has someone in it.
    /// </summary>
    public static class NpcCatalog
    {
        public struct Entry
        {
            public string key;      // Resources/Npcs/<key> prefab, npc_<key>.glb
            public string name;
            public bool vendor;     // fits behind a stall (stationary NPCs pick these first)
        }

        public static readonly Entry[] All =
        {
            new Entry { key = "meyveci", name = "Manav",     vendor = true },
            new Entry { key = "simitci", name = "Simitçi",   vendor = true },
            new Entry { key = "postaci", name = "Postacı",   vendor = false },
            new Entry { key = "yasli",   name = "Emekli",    vendor = false },
            new Entry { key = "kosucu",  name = "Koşucu",    vendor = false },
            new Entry { key = "turist",  name = "Turist",    vendor = false },
        };

        public static int Count => All.Length;

        /// <summary>A random look; stationary NPCs (vendors) get a vendor look, walkers any of the others.</summary>
        public static int Pick(bool stationary)
        {
            var pool = new List<int>();
            for (var i = 0; i < All.Length; i++) if (All[i].vendor == stationary) pool.Add(i);
            if (pool.Count == 0) return Random.Range(0, All.Length);
            return pool[Random.Range(0, pool.Count)];
        }

        static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

        /// <summary>Builds the visual for NPC look 'index' under 'anchor' (feet at the anchor, facing +Z).</summary>
        public static GameObject Build(int index, Transform anchor)
        {
            index = Mathf.Clamp(index, 0, All.Length - 1);
            var key = All[index].key;
            if (!prefabs.TryGetValue(key, out var prefab))
            {
                prefab = Resources.Load<GameObject>("Npcs/" + key);
                prefabs[key] = prefab;
            }
            if (prefab == null) return CharacterCatalog.Build(index % CharacterCatalog.Count, anchor);
            var go = Object.Instantiate(prefab, anchor);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            if (go.GetComponent<CharacterAnimator>() == null) go.AddComponent<CharacterAnimator>();
            return go;
        }
    }
}
