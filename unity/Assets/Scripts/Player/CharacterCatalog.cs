using System.Collections.Generic;
using UnityEngine;

namespace WhoBrokeIt.Player
{
    /// <summary>
    /// The list of playable characters and the player's saved choice.
    /// Each entry is built from code (CharacterFactory) unless a prefab named Resources/Characters/&lt;key&gt; exists:
    /// drop your own model there (feet at the prefab's origin, about 1.8 m tall, facing +Z) and it replaces the generated one.
    /// </summary>
    public static class CharacterCatalog
    {
        public struct Entry
        {
            public string key;       // also the Resources/Characters/<key> prefab name for custom models
            public string name;      // shown in the menu
            public string blurb;
            public Color accent;     // card / menu colour
            public string tag;       // short role line on the card
        }

        public static readonly Entry[] All =
        {
            new Entry { key = "isci",     name = "Reclaimer İşçisi", blurb = "Turuncu baret, sarı yelek. Kasabanın klasik tamircisi.", accent = new Color(1f,0.62f,0.15f), tag = "TAMİRCİ" },
            new Entry { key = "muhendis", name = "Mühendis",         blurb = "Beyaz baret ve gözlük. Her şeyi ölçer, yine de bozar.", accent = new Color(0.35f,0.65f,1f), tag = "PLANCI" },
            new Entry { key = "asci",     name = "Aşçı",             blurb = "Börek uzmanı. Sigorta kutusunda ne işi var, bilinmez.", accent = new Color(1f,0.45f,0.42f), tag = "ŞÜPHELİ" },
            new Entry { key = "robot",    name = "Robot",            blurb = "Eski belediye otomasyonundan kalma. Antenli ve inatçı.", accent = new Color(0.55f,0.85f,0.9f), tag = "OTOMASYON" },
            new Entry { key = "kedi",     name = "Kedi",             blurb = "Kablo kemirmediğine yemin eder.", accent = new Color(0.95f,0.78f,0.35f), tag = "ŞÜPHELİ" },
            new Entry { key = "kurbaga",  name = "Kurbağa",          blurb = "Su kulesinin yakınlarında yaşar, çok zıplar.", accent = new Color(0.45f,0.85f,0.45f), tag = "ZIPLAYICI" },
        };

        const string PrefKey = "wbi_character";

        public static int Count => All.Length;

        public static int SavedChoice()
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, 0), 0, All.Length - 1);
        }

        public static void Save(int index)
        {
            PlayerPrefs.SetInt(PrefKey, Mathf.Clamp(index, 0, All.Length - 1));
            PlayerPrefs.Save();
        }

        static readonly Dictionary<string, GameObject> custom = new Dictionary<string, GameObject>();

        /// <summary>Builds the visual for a character under 'anchor' (the anchor sits at the character's feet).</summary>
        public static GameObject Build(int index, Transform anchor)
        {
            index = Mathf.Clamp(index, 0, All.Length - 1);
            var key = All[index].key;
            if (!custom.TryGetValue(key, out var prefab))
            {
                prefab = Resources.Load<GameObject>("Characters/" + key);
                custom[key] = prefab;
            }
            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab, anchor);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
            }
            else go = CharacterFactory.Build(index, anchor);

            if (go != null && go.GetComponent<CharacterAnimator>() == null) go.AddComponent<CharacterAnimator>();
            return go;
        }
    }
}
