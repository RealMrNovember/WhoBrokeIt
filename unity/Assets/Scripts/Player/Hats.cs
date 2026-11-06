using UnityEngine;

namespace WhoBrokeIt.Player
{
    /// <summary>
    /// Cosmetic hats: built from primitives at runtime (no assets), worn on the head by every character, chosen in the Esc menu or the profile page.
    /// They are unlocked by finishing contracts (counted on this computer). The chosen hat is replicated as one byte (PlayerNet.hat).
    /// </summary>
    public static class Hats
    {
        public struct Entry { public string name; public int contractsNeeded; }

        public static readonly Entry[] All =
        {
            new Entry { name = "Şapkasız", contractsNeeded = 0 },
            new Entry { name = "Parti Şapkası", contractsNeeded = 1 },
            new Entry { name = "Kasket", contractsNeeded = 2 },
            new Entry { name = "Silindir Şapka", contractsNeeded = 4 },
            new Entry { name = "Pervaneli Bere", contractsNeeded = 6 },
            new Entry { name = "Taç", contractsNeeded = 10 },
        };

        public static int Count => All.Length;

        public static int ContractsDone => PlayerPrefs.GetInt("wbi_contracts_done", 0);
        public static bool Unlocked(int id) => id >= 0 && id < All.Length && ContractsDone >= All[id].contractsNeeded;
        public static int UnlockedCount { get { var n = 0; for (var i = 0; i < All.Length; i++) if (Unlocked(i)) n++; return n; } }

        /// <summary>The hat saved for this computer (falls back to none when it is not unlocked).</summary>
        public static int Selected
        {
            get { var h = PlayerPrefs.GetInt("wbi_hat", 0); return Unlocked(h) ? h : 0; }
            set { PlayerPrefs.SetInt("wbi_hat", Unlocked(value) ? value : 0); PlayerPrefs.Save(); }
        }

        /// <summary>The next unlocked hat after the given one (wraps around to none).</summary>
        public static int NextUnlocked(int current)
        {
            for (var i = 1; i <= All.Length; i++)
            {
                var id = (current + i) % All.Length;
                if (Unlocked(id)) return id;
            }
            return 0;
        }

        /// <summary>One more contract finished: unlock what it earns and tell the player.</summary>
        public static void NoteContractDone()
        {
            var before = ContractsDone;
            PlayerPrefs.SetInt("wbi_contracts_done", before + 1);
            PlayerPrefs.Save();
            for (var i = 1; i < All.Length; i++)
                if (All[i].contractsNeeded == before + 1)
                    Case.CaseManager.Toast("Yeni şapka açıldı: " + All[i].name + "  (Esc menüsünden tak)", 5f);
        }

        // ---------- building ----------
        static Material baseMat;

        static Material Mat(Color c)
        {
            if (baseMat == null) baseMat = Resources.Load<Material>("WBI/CharacterBase");
            var m = baseMat != null ? new Material(baseMat) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }

        static Transform Part(PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 scale, Color c)
        {
            var go = GameObject.CreatePrimitive(t);
            go.name = name;
            var col = go.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
            go.GetComponent<Renderer>().sharedMaterial = Mat(c);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            return go.transform;
        }

        /// <summary>Builds the hat with its base on the point (0,0,0) of the returned root, sized for a head about 0.4 m wide.</summary>
        public static GameObject Build(int id)
        {
            if (id <= 0 || id >= All.Length) return null;
            var root = new GameObject("Hat_" + All[id].name);
            switch (id)
            {
                case 1:      // party hat: a stack of discs that narrows to a point, with a pompom
                    for (var i = 0; i < 5; i++)
                        Part(PrimitiveType.Cylinder, "Cone" + i, root.transform, new Vector3(0f, 0.04f + i * 0.07f, 0f),
                             new Vector3(0.30f - i * 0.055f, 0.04f, 0.30f - i * 0.055f), i % 2 == 0 ? new Color(0.95f, 0.30f, 0.55f) : new Color(1f, 0.85f, 0.25f));
                    Part(PrimitiveType.Sphere, "Pompom", root.transform, new Vector3(0f, 0.37f, 0f), Vector3.one * 0.08f, new Color(0.3f, 0.8f, 1f));
                    break;
                case 2:      // cap: dome and a visor
                    Part(PrimitiveType.Sphere, "Dome", root.transform, new Vector3(0f, 0.03f, 0f), new Vector3(0.40f, 0.26f, 0.40f), new Color(0.2f, 0.45f, 0.85f));
                    Part(PrimitiveType.Cube, "Visor", root.transform, new Vector3(0f, 0.00f, 0.22f), new Vector3(0.30f, 0.025f, 0.22f), new Color(0.12f, 0.27f, 0.55f));
                    break;
                case 3:      // top hat
                    Part(PrimitiveType.Cylinder, "Brim", root.transform, new Vector3(0f, 0.01f, 0f), new Vector3(0.50f, 0.012f, 0.50f), new Color(0.08f, 0.08f, 0.1f));
                    Part(PrimitiveType.Cylinder, "Crown", root.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.32f, 0.14f, 0.32f), new Color(0.1f, 0.1f, 0.12f));
                    Part(PrimitiveType.Cylinder, "Band", root.transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.325f, 0.03f, 0.325f), new Color(0.75f, 0.15f, 0.2f));
                    break;
                case 4:      // propeller beanie: dome with a spinning propeller
                    Part(PrimitiveType.Sphere, "Dome", root.transform, new Vector3(0f, 0.03f, 0f), new Vector3(0.38f, 0.24f, 0.38f), new Color(0.95f, 0.55f, 0.15f));
                    Part(PrimitiveType.Cylinder, "Stem", root.transform, new Vector3(0f, 0.17f, 0f), new Vector3(0.025f, 0.04f, 0.025f), new Color(0.9f, 0.9f, 0.9f));
                    var prop = new GameObject("Propeller").transform;
                    prop.SetParent(root.transform, false); prop.localPosition = new Vector3(0f, 0.22f, 0f);
                    Part(PrimitiveType.Cube, "Blade1", prop, Vector3.zero, new Vector3(0.34f, 0.01f, 0.06f), new Color(0.2f, 0.75f, 0.9f));
                    Part(PrimitiveType.Cube, "Blade2", prop, Vector3.zero, new Vector3(0.06f, 0.01f, 0.34f), new Color(0.95f, 0.8f, 0.2f));
                    root.AddComponent<HatSpin>().target = prop;
                    break;
                case 5:      // crown: a golden ring with points and jewels
                    Part(PrimitiveType.Cylinder, "Ring", root.transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.34f, 0.04f, 0.34f), new Color(1f, 0.8f, 0.15f));
                    for (var i = 0; i < 6; i++)
                    {
                        var a = i * Mathf.PI * 2f / 6f;
                        var spike = Part(PrimitiveType.Cube, "Spike" + i, root.transform, new Vector3(Mathf.Cos(a) * 0.15f, 0.15f, Mathf.Sin(a) * 0.15f), new Vector3(0.05f, 0.17f, 0.05f), new Color(1f, 0.8f, 0.15f));
                        spike.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                        Part(PrimitiveType.Sphere, "Jewel" + i, root.transform, new Vector3(Mathf.Cos(a) * 0.15f, 0.25f, Mathf.Sin(a) * 0.15f), Vector3.one * 0.045f, i % 2 == 0 ? new Color(0.9f, 0.1f, 0.2f) : new Color(0.15f, 0.5f, 0.95f));
                    }
                    break;
            }
            return root;
        }
    }

}
