using System.Collections.Generic;
using UnityEngine;

namespace WhoBrokeIt.Player
{
    /// <summary>Original low-poly characters made of primitives (feet at the root's origin, ~1.8 m, facing +Z). Limbs hang from pivots so CharacterAnimator can swing them.</summary>
    public static class CharacterFactory
    {
        enum Hat { HardHat, HardHatGoggles, Chef, Antenna, CatEars, FrogEyes }

        struct Look
        {
            public Color shirt, pants, skin, accent;
            public Hat hat;
            public bool boxy, tail, animalNose, visor;
        }

        static Material baseMaterial;
        static readonly Dictionary<Color, Material> cache = new Dictionary<Color, Material>();

        static Material Mat(Color c)
        {
            if (cache.TryGetValue(c, out var m) && m != null) return m;
            if (baseMaterial == null) baseMaterial = Resources.Load<Material>("WBI/CharacterBase");
            m = baseMaterial != null ? new Material(baseMaterial) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.color = c;
            cache[c] = m;
            return m;
        }

        static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Color color, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            return go;
        }

        static Transform Pivot(string name, Transform parent, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go.transform;
        }

        static Look LookFor(int id)
        {
            var skin = new Color(0.95f, 0.78f, 0.62f);
            switch (id)
            {
                case 1: // mühendis
                    return new Look { shirt = new Color(0.20f, 0.62f, 0.62f), pants = new Color(0.35f, 0.37f, 0.42f), skin = new Color(0.82f, 0.62f, 0.46f), accent = new Color(0.96f, 0.96f, 0.94f), hat = Hat.HardHatGoggles };
                case 2: // aşçı
                    return new Look { shirt = new Color(0.97f, 0.97f, 0.95f), pants = new Color(0.20f, 0.22f, 0.28f), skin = new Color(0.90f, 0.70f, 0.55f), accent = new Color(0.97f, 0.97f, 0.95f), hat = Hat.Chef };
                case 3: // robot
                    return new Look { shirt = new Color(0.62f, 0.68f, 0.74f), pants = new Color(0.40f, 0.44f, 0.50f), skin = new Color(0.72f, 0.77f, 0.82f), accent = new Color(0.90f, 0.25f, 0.25f), hat = Hat.Antenna, boxy = true, visor = true };
                case 4: // kedi
                    return new Look { shirt = new Color(0.93f, 0.55f, 0.20f), pants = new Color(0.30f, 0.32f, 0.55f), skin = new Color(0.96f, 0.66f, 0.30f), accent = new Color(1f, 0.70f, 0.75f), hat = Hat.CatEars, tail = true, animalNose = true };
                case 5: // kurbağa
                    return new Look { shirt = new Color(0.30f, 0.62f, 0.28f), pants = new Color(0.82f, 0.62f, 0.25f), skin = new Color(0.45f, 0.80f, 0.38f), accent = new Color(0.45f, 0.80f, 0.38f), hat = Hat.FrogEyes };
                default: // işçi
                    return new Look { shirt = new Color(0.98f, 0.82f, 0.12f), pants = new Color(0.22f, 0.38f, 0.72f), skin = skin, accent = new Color(0.95f, 0.45f, 0.10f), hat = Hat.HardHat };
            }
        }

        public static GameObject Build(int id, Transform anchor)
        {
            var look = LookFor(id);
            var root = new GameObject("Character_" + CharacterCatalog.All[Mathf.Clamp(id, 0, CharacterCatalog.Count - 1)].key);
            root.transform.SetParent(anchor, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            var t = root.transform;
            var dark = new Color(0.14f, 0.14f, 0.17f);
            var white = Color.white;

            // legs hang from hip pivots
            var legL = Pivot("LegL", t, new Vector3(-0.14f, 0.86f, 0f));
            var legR = Pivot("LegR", t, new Vector3(0.14f, 0.86f, 0f));
            foreach (var leg in new[] { legL, legR })
            {
                if (look.boxy) Part(PrimitiveType.Cube, "Leg", leg, new Vector3(0f, -0.36f, 0f), new Vector3(0.20f, 0.66f, 0.20f), look.pants);
                else Part(PrimitiveType.Capsule, "Leg", leg, new Vector3(0f, -0.40f, 0f), new Vector3(0.21f, 0.38f, 0.21f), look.pants);
                Part(PrimitiveType.Cube, "Foot", leg, new Vector3(0f, -0.80f, 0.05f), new Vector3(0.22f, 0.12f, 0.34f), dark);
            }

            // torso
            if (look.boxy) Part(PrimitiveType.Cube, "Torso", t, new Vector3(0f, 1.16f, 0f), new Vector3(0.54f, 0.72f, 0.36f), look.shirt);
            else Part(PrimitiveType.Capsule, "Torso", t, new Vector3(0f, 1.16f, 0f), new Vector3(0.52f, 0.40f, 0.36f), look.shirt);
            if (id == 0) Part(PrimitiveType.Cube, "VestStripe", t, new Vector3(0f, 1.12f, 0.175f), new Vector3(0.40f, 0.07f, 0.03f), new Color(0.85f, 0.88f, 0.9f));
            if (id == 3) Part(PrimitiveType.Cube, "Panel", t, new Vector3(0f, 1.22f, 0.185f), new Vector3(0.28f, 0.20f, 0.03f), new Color(0.2f, 0.75f, 0.85f));
            if (id == 2) Part(PrimitiveType.Cube, "Apron", t, new Vector3(0f, 1.05f, 0.18f), new Vector3(0.36f, 0.42f, 0.03f), new Color(0.9f, 0.9f, 0.88f));
            if (look.tail) Part(PrimitiveType.Capsule, "Tail", t, new Vector3(0f, 0.95f, -0.30f), new Vector3(0.08f, 0.26f, 0.08f), look.skin, new Vector3(-50f, 0f, 0f));

            // arms hang from shoulder pivots
            var armL = Pivot("ArmL", t, new Vector3(-0.37f, 1.46f, 0f));
            var armR = Pivot("ArmR", t, new Vector3(0.37f, 1.46f, 0f));
            foreach (var arm in new[] { armL, armR })
            {
                if (look.boxy) Part(PrimitiveType.Cube, "Arm", arm, new Vector3(0f, -0.28f, 0f), new Vector3(0.14f, 0.56f, 0.14f), look.shirt);
                else Part(PrimitiveType.Capsule, "Arm", arm, new Vector3(0f, -0.28f, 0f), new Vector3(0.13f, 0.30f, 0.13f), look.shirt);
                Part(PrimitiveType.Sphere, "Hand", arm, new Vector3(0f, -0.60f, 0f), Vector3.one * 0.17f, id == 3 ? dark : look.skin);
            }

            // head
            if (look.boxy) Part(PrimitiveType.Cube, "Head", t, new Vector3(0f, 1.72f, 0f), new Vector3(0.42f, 0.38f, 0.40f), look.skin);
            else Part(PrimitiveType.Sphere, "Head", t, new Vector3(0f, 1.72f, 0f), new Vector3(0.44f, 0.44f, 0.42f), look.skin);

            if (look.visor)
            {
                Part(PrimitiveType.Cube, "Visor", t, new Vector3(0f, 1.74f, 0.205f), new Vector3(0.34f, 0.10f, 0.03f), new Color(0.25f, 0.95f, 1f));
            }
            else if (look.hat != Hat.FrogEyes)
            {
                Part(PrimitiveType.Sphere, "EyeL", t, new Vector3(-0.095f, 1.75f, 0.19f), Vector3.one * 0.105f, white);
                Part(PrimitiveType.Sphere, "EyeR", t, new Vector3(0.095f, 1.75f, 0.19f), Vector3.one * 0.105f, white);
                Part(PrimitiveType.Sphere, "PupilL", t, new Vector3(-0.095f, 1.75f, 0.235f), Vector3.one * 0.055f, dark);
                Part(PrimitiveType.Sphere, "PupilR", t, new Vector3(0.095f, 1.75f, 0.235f), Vector3.one * 0.055f, dark);
            }
            if (look.animalNose) Part(PrimitiveType.Sphere, "Nose", t, new Vector3(0f, 1.69f, 0.215f), Vector3.one * 0.065f, new Color(1f, 0.55f, 0.6f));
            if (!look.visor && look.hat != Hat.FrogEyes) Part(PrimitiveType.Cube, "Mouth", t, new Vector3(0f, 1.63f, 0.20f), new Vector3(0.13f, 0.025f, 0.02f), dark);

            // headgear
            switch (look.hat)
            {
                case Hat.HardHat:
                case Hat.HardHatGoggles:
                    Part(PrimitiveType.Sphere, "Hat", t, new Vector3(0f, 1.88f, 0f), new Vector3(0.50f, 0.30f, 0.50f), look.accent);
                    Part(PrimitiveType.Cylinder, "Brim", t, new Vector3(0f, 1.83f, 0.03f), new Vector3(0.58f, 0.015f, 0.58f), look.accent);
                    if (look.hat == Hat.HardHatGoggles)
                    {
                        Part(PrimitiveType.Cylinder, "GoggleL", t, new Vector3(-0.10f, 1.80f, 0.205f), new Vector3(0.15f, 0.02f, 0.15f), dark, new Vector3(90f, 0f, 0f));
                        Part(PrimitiveType.Cylinder, "GoggleR", t, new Vector3(0.10f, 1.80f, 0.205f), new Vector3(0.15f, 0.02f, 0.15f), dark, new Vector3(90f, 0f, 0f));
                        Part(PrimitiveType.Cube, "GoggleBand", t, new Vector3(0f, 1.80f, 0.0f), new Vector3(0.46f, 0.04f, 0.42f), dark);
                    }
                    break;
                case Hat.Chef:
                    Part(PrimitiveType.Cylinder, "Hat", t, new Vector3(0f, 1.95f, 0f), new Vector3(0.32f, 0.12f, 0.32f), look.accent);
                    Part(PrimitiveType.Sphere, "HatPuff", t, new Vector3(0f, 2.16f, 0f), new Vector3(0.44f, 0.30f, 0.44f), look.accent);
                    break;
                case Hat.Antenna:
                    Part(PrimitiveType.Cylinder, "Antenna", t, new Vector3(0f, 2.00f, 0f), new Vector3(0.025f, 0.12f, 0.025f), dark);
                    Part(PrimitiveType.Sphere, "AntennaBall", t, new Vector3(0f, 2.14f, 0f), Vector3.one * 0.10f, look.accent);
                    break;
                case Hat.CatEars:
                    Part(PrimitiveType.Cube, "EarL", t, new Vector3(-0.15f, 1.96f, 0f), new Vector3(0.15f, 0.15f, 0.05f), look.skin, new Vector3(0f, 0f, 45f));
                    Part(PrimitiveType.Cube, "EarR", t, new Vector3(0.15f, 1.96f, 0f), new Vector3(0.15f, 0.15f, 0.05f), look.skin, new Vector3(0f, 0f, 45f));
                    Part(PrimitiveType.Cube, "EarInL", t, new Vector3(-0.15f, 1.95f, 0.02f), new Vector3(0.09f, 0.09f, 0.04f), look.accent, new Vector3(0f, 0f, 45f));
                    Part(PrimitiveType.Cube, "EarInR", t, new Vector3(0.15f, 1.95f, 0.02f), new Vector3(0.09f, 0.09f, 0.04f), look.accent, new Vector3(0f, 0f, 45f));
                    break;
                case Hat.FrogEyes:
                    foreach (var sx in new[] { -1f, 1f })
                    {
                        Part(PrimitiveType.Sphere, "FrogEye", t, new Vector3(0.13f * sx, 1.93f, 0.06f), Vector3.one * 0.20f, look.skin);
                        Part(PrimitiveType.Sphere, "FrogEyeWhite", t, new Vector3(0.13f * sx, 1.95f, 0.13f), Vector3.one * 0.13f, white);
                        Part(PrimitiveType.Sphere, "FrogPupil", t, new Vector3(0.13f * sx, 1.95f, 0.185f), Vector3.one * 0.06f, dark);
                    }
                    Part(PrimitiveType.Cube, "FrogMouth", t, new Vector3(0f, 1.63f, 0.19f), new Vector3(0.26f, 0.03f, 0.03f), new Color(0.15f, 0.30f, 0.12f));
                    break;
            }

            var anim = root.AddComponent<CharacterAnimator>();
            anim.legL = legL; anim.legR = legR; anim.armL = armL; anim.armR = armR;
            return root;
        }
    }
}
