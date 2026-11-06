using Unity.Netcode;
using UnityEngine;
using WhoBrokeIt.Net;
using WhoBrokeIt.Player;

namespace WhoBrokeIt.Contracts
{
    /// <summary>
    /// Waypoint arrows for the active contract: where the things to pick up are (until the player holds one) and where the delivery area is.
    /// On screen: a marker above the target with the distance; off screen: an arrow on the screen edge that points the way.
    /// </summary>
    public static class ContractMarkers
    {
        static ContractItem[] items;
        static float refreshAt;

        static ContractItem[] Items()
        {
            if (items == null || Time.unscaledTime > refreshAt) { items = Object.FindObjectsByType<ContractItem>(FindObjectsSortMode.None); refreshAt = Time.unscaledTime + 1f; }
            return items;
        }

        static bool Carrying(PlayerNet me, string tag)
        {
            if (me == null || me.carry == null || !me.carry.IsCarrying || NetworkManager.Singleton == null) return false;
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(me.carry.heldId.Value, out var no) || no == null) return false;
            var it = no.GetComponent<ContractItem>();
            return it != null && it.itemTag == tag;
        }

        public static void Draw(ContractDef d, ContractZone[] zones, int progress)
        {
            var cam = Camera.main;
            var me = PlayerNet.Local;
            if (cam == null || !cam.enabled || me == null || PauseMenu.Open) return;
            ContractZone zone = null;
            if (zones != null) foreach (var z in zones) if (z != null && z.zoneId == d.zoneId) { zone = z; break; }
            var carrying = Carrying(me, d.itemTag);

            if (!carrying && progress < d.goal)
            {
                // the three closest things still to be collected (not delivered yet, not in someone's hands)
                var picked = new System.Collections.Generic.List<ContractItem>();
                for (var n = 0; n < 3; n++)
                {
                    ContractItem best = null; var bd = float.MaxValue;
                    foreach (var it in Items())
                    {
                        if (it == null || it.itemTag != d.itemTag || picked.Contains(it)) continue;
                        if (zone != null && (it.spentZone == zone.zoneId || zone.Area.bounds.Contains(it.transform.position))) continue;
                        var rb = it.Body;
                        if (rb != null && !rb.useGravity) continue;
                        var dist = (it.transform.position - me.transform.position).sqrMagnitude;
                        if (dist < bd) { bd = dist; best = it; }
                    }
                    if (best == null) break;
                    picked.Add(best);
                }
                foreach (var it in picked)
                    Mark(cam, it.transform.position + Vector3.up * 1.6f, "AL", new Color(1f, 0.82f, 0.2f), me.transform.position, it.transform.position);
            }
            if (zone != null)
                Mark(cam, zone.Area.bounds.center + Vector3.up * 3f, "TESLİM · " + d.zoneName, new Color(0.35f, 0.9f, 0.6f), me.transform.position, zone.Area.bounds.center);
        }

        static void Mark(Camera cam, Vector3 world, string label, Color color, Vector3 from, Vector3 distPoint)
        {
            var s = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            var m = Mathf.Max(0.7f, s);
            var sp = cam.WorldToScreenPoint(world);
            var behind = sp.z < 0f;
            var x = sp.x; var y = Screen.height - sp.y;                      // GUI space: y grows downward
            if (behind) { x = Screen.width - x; y = Screen.height - y; }
            var margin = 70f * m;
            var inside = !behind && x > margin && x < Screen.width - margin && y > margin && y < Screen.height - margin;
            var meters = Mathf.RoundToInt(Vector3.Distance(from, distPoint));
            var text = label + "  " + meters + " m";
            var old = GUI.color; var oldMatrix = GUI.matrix;
            GUI.color = color;
            if (inside)
            {
                LocGui.Label(new Rect(x - 120f * m, y - 34f * m, 240f * m, 30f * m), text, Style(m, color));
                Arrow(new Vector2(x, y), 180f, m);                              // pointing down at the target
            }
            else
            {
                var c = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                var dir = new Vector2(x - c.x, y - c.y);
                if (dir.sqrMagnitude < 1f) dir = Vector2.up;
                dir.Normalize();
                // intersect the ray with the margin rectangle
                var hx = c.x - margin; var hy = c.y - margin;
                var k = Mathf.Min(Mathf.Abs(dir.x) > 0.0001f ? hx / Mathf.Abs(dir.x) : float.MaxValue, Mathf.Abs(dir.y) > 0.0001f ? hy / Mathf.Abs(dir.y) : float.MaxValue);
                var p = c + dir * k;
                var angle = Mathf.Atan2(dir.x, -dir.y) * Mathf.Rad2Deg;
                Arrow(p, angle, m);
                var lp = p - dir * 40f * m;
                LocGui.Label(new Rect(lp.x - 120f * m, lp.y - 15f * m, 240f * m, 30f * m), text, Style(m, color));
            }
            GUI.color = old; GUI.matrix = oldMatrix;
        }

        static GUIStyle style;
        static GUIStyle Style(float m, Color c)
        {
            if (style == null) style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            style.fontSize = Mathf.RoundToInt(20f * m);
            style.normal.textColor = c;
            return style;
        }

        static Texture2D px;
        /// <summary>A solid triangle arrow (drawn as a rotated stack of bars) centred on p, tip toward 'angle' degrees clockwise from up.</summary>
        static void Arrow(Vector2 p, float angle, float m)
        {
            if (px == null) { px = new Texture2D(1, 1); px.SetPixel(0, 0, Color.white); px.Apply(); }
            var col = GUI.color;
            var oldMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, p);
            const int rows = 12;
            for (var i = 0; i < rows; i++)
            {
                var t = i / (float)(rows - 1);                                   // 0 = tip, 1 = base
                var w = Mathf.Lerp(3f, 30f, t) * m;
                var h = 2.6f * m;
                GUI.color = new Color(0f, 0f, 0f, 0.55f * col.a); GUI.DrawTexture(new Rect(p.x - w / 2f - 2f, p.y - 16f * m + i * h - 2f, w + 4f, h + 4f), px);
            }
            for (var i = 0; i < rows; i++)
            {
                var t = i / (float)(rows - 1);
                var w = Mathf.Lerp(3f, 30f, t) * m;
                var h = 2.6f * m;
                GUI.color = col; GUI.DrawTexture(new Rect(p.x - w / 2f, p.y - 16f * m + i * h, w, h), px);
            }
            GUI.matrix = oldMatrix;
            GUI.color = col;
        }
    }
}
