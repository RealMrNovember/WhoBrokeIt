using UnityEngine;
using WhoBrokeIt.Audio;

namespace WhoBrokeIt.Props
{
    public enum MaterialKind : byte { Wood, Metal, Concrete, Glass, Ceramic, Plastic, Canvas, Stone, Foliage }

    /// <summary>Per-material tuning: how fast something must hit to count, how it breaks up, what it looks and sounds like.</summary>
    public struct MaterialInfo
    {
        public float minSpeed;     // m/s relative speed below which a hit is ignored (resting, dropped from the hands, gentle bumps)
        public int debris;         // default chunk count when it breaks
        public float chunk;        // chunk size (m)
        public Color color;        // chunk / dust colour
        public float massMul;      // multiplies the base mass in Destructibles.Make
        public Sfx hit, smash;     // sound ids
    }

    public static class MaterialTable
    {
        static readonly MaterialInfo[] table =
        {
            new MaterialInfo { minSpeed = 3.5f, debris = 8, chunk = 0.22f, color = new Color(0.78f, 0.54f, 0.30f), massMul = 1.0f, hit = Sfx.WoodCrack, smash = Sfx.WoodSmash },
            new MaterialInfo { minSpeed = 6.0f, debris = 5, chunk = 0.18f, color = new Color(0.56f, 0.61f, 0.67f), massMul = 1.6f, hit = Sfx.MetalClang, smash = Sfx.MetalClang },
            new MaterialInfo { minSpeed = 7.0f, debris = 9, chunk = 0.25f, color = new Color(0.72f, 0.70f, 0.65f), massMul = 2.2f, hit = Sfx.ConcreteThud, smash = Sfx.ConcreteThud },
            new MaterialInfo { minSpeed = 2.5f, debris = 14, chunk = 0.12f, color = new Color(0.66f, 0.87f, 0.95f), massMul = 0.8f, hit = Sfx.GlassShatter, smash = Sfx.GlassShatter },
            new MaterialInfo { minSpeed = 3.0f, debris = 10, chunk = 0.15f, color = new Color(0.95f, 0.90f, 0.80f), massMul = 0.7f, hit = Sfx.CeramicSmash, smash = Sfx.CeramicSmash },
            new MaterialInfo { minSpeed = 5.0f, debris = 6, chunk = 0.20f, color = new Color(0.95f, 0.50f, 0.30f), massMul = 0.5f, hit = Sfx.PlasticKnock, smash = Sfx.PlasticKnock },
            new MaterialInfo { minSpeed = 4.0f, debris = 4, chunk = 0.30f, color = new Color(0.91f, 0.85f, 0.69f), massMul = 0.4f, hit = Sfx.CanvasFlap, smash = Sfx.CanvasFlap },
            new MaterialInfo { minSpeed = 7.0f, debris = 9, chunk = 0.25f, color = new Color(0.62f, 0.60f, 0.58f), massMul = 2.4f, hit = Sfx.ConcreteThud, smash = Sfx.ConcreteThud },
            new MaterialInfo { minSpeed = 5.0f, debris = 5, chunk = 0.20f, color = new Color(0.30f, 0.60f, 0.33f), massMul = 0.4f, hit = Sfx.CanvasFlap, smash = Sfx.CanvasFlap },
        };

        /// <summary>Kit manifest materialTag (wood, metal, concrete, glass, ceramic, plastic, canvas, stone, foliage) to MaterialKind. Unknown tags fall back to Wood.</summary>
        public static MaterialKind Parse(string tag)
        {
            switch ((tag ?? "").Trim().ToLowerInvariant())
            {
                case "metal": return MaterialKind.Metal;
                case "concrete": return MaterialKind.Concrete;
                case "glass": return MaterialKind.Glass;
                case "ceramic": return MaterialKind.Ceramic;
                case "plastic": return MaterialKind.Plastic;
                case "canvas": return MaterialKind.Canvas;
                case "stone": return MaterialKind.Stone;
                case "foliage": return MaterialKind.Foliage;
                default: return MaterialKind.Wood;
            }
        }

        /// <summary>Default hit points from mass: a 4 kg wooden crate ~28, a 5 kg steel drum ~60, a 25 kg door ~175. Clamped to 8..600.</summary>
        public static float DefaultStrength(MaterialKind k, float massKg)
        {
            float perKg;
            switch (k)
            {
                case MaterialKind.Metal: perKg = 12f; break;
                case MaterialKind.Concrete: perKg = 20f; break;
                case MaterialKind.Stone: perKg = 24f; break;
                case MaterialKind.Glass: perKg = 3f; break;
                case MaterialKind.Ceramic: perKg = 4f; break;
                case MaterialKind.Plastic: perKg = 6f; break;
                case MaterialKind.Canvas: perKg = 3f; break;
                case MaterialKind.Foliage: perKg = 4f; break;
                default: perKg = 7f; break;
            }
            return Mathf.Clamp(massKg * perKg, 8f, 600f);
        }

        public static MaterialInfo Get(MaterialKind k)
        {
            var i = (int)k;
            return table[i >= 0 && i < table.Length ? i : 0];
        }
    }
}
