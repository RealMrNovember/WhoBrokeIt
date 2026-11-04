using System.Collections.Generic;
using UnityEngine;
using WhoBrokeIt.Audio;

namespace WhoBrokeIt.Net
{
    /// <summary>
    /// Menu design system (IMGUI, resolution independent): one palette, one type scale, rounded panels and buttons with
    /// normal / hover / pressed / focused / disabled states, eased hover animation, keyboard focus and UI sounds.
    /// Rounded shapes use GUI.DrawTexture's border radius, so no texture assets are needed.
    /// </summary>
    public static class UiKit
    {
        // ---------- palette ----------
        public static readonly Color Ink = new Color(0.10f, 0.12f, 0.22f);
        public static readonly Color Panel = new Color(0.09f, 0.11f, 0.22f, 0.93f);
        public static readonly Color Cream = new Color(1f, 0.96f, 0.86f);
        public static readonly Color Sun = new Color(1f, 0.78f, 0.20f);
        public static readonly Color SunDeep = new Color(0.95f, 0.56f, 0.10f);
        public static readonly Color Sky = new Color(0.32f, 0.72f, 0.98f);
        public static readonly Color Coral = new Color(1f, 0.42f, 0.38f);
        public static readonly Color Mint = new Color(0.36f, 0.86f, 0.62f);
        public static readonly Color Slate = new Color(0.22f, 0.26f, 0.44f);

        public enum Kind { Normal, Primary, Danger, Ghost }

        // ---------- shared state ----------
        public static int Focus;            // keyboard focus index within the current page
        public static bool KeyActivate;     // Enter/Space pressed this frame
        public static int Index;            // running button index during one OnGUI pass
        public static int Count;            // buttons seen in the previous pass (for wrapping)
        public static bool Locked;          // a modal is open: page controls draw dimmed and ignore input (and are skipped by keyboard focus)
        static int countNow;
        static Vector2 lastMouse;
        static bool mouseMoved;
        static readonly Dictionary<int, float> hoverAnim = new Dictionary<int, float>();
        static GUIStyle label, shadow, btnText;

        public static void BeginPass()
        {
            Index = 0;
            countNow = 0;
            var m = Event.current.mousePosition;
            if (Event.current.type == EventType.Layout) { mouseMoved = (m - lastMouse).sqrMagnitude > 0.5f; lastMouse = m; }
        }

        public static void EndPass() { Count = countNow; }

        static void EnsureStyles()
        {
            if (label != null) return;
            label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, wordWrap = true, richText = true };
            shadow = new GUIStyle(label);
            btnText = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, clipping = TextClipping.Clip };
        }

        // ---------- drawing helpers ----------
        public static void Fill(Rect r, Color c, float radius = 0f)
        {
            if (c.a <= 0.001f) return;
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, Vector4.zero, Vector4.one * radius);
        }

        public static void Outline(Rect r, Color c, float width, float radius)
        {
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, Vector4.one * width, Vector4.one * radius);
        }

        /// <summary>Rounded panel with a soft drop shadow.</summary>
        public static void Card(Rect r, Color fill, float radius = 28f)
        {
            Fill(new Rect(r.x - 2, r.y + 6, r.width + 4, r.height + 4), new Color(0, 0, 0, 0.22f), radius + 4);
            Fill(r, fill, radius);
        }

        public static void Text(Rect r, string text, int size, Color c, TextAnchor anchor = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal, bool shade = true)
        {
            EnsureStyles();
            label.fontSize = size; label.alignment = anchor; label.fontStyle = style;
            if (shade)
            {
                shadow.fontSize = size; shadow.alignment = anchor; shadow.fontStyle = style;
                shadow.normal.textColor = new Color(0f, 0f, 0f, 0.45f);
                GUI.Label(new Rect(r.x + 2, r.y + 3, r.width, r.height), text, shadow);
            }
            label.normal.textColor = c;
            GUI.Label(r, text, label);
        }

        // ---------- controls ----------
        static float Anim(int key, bool on, float speed = 10f)
        {
            hoverAnim.TryGetValue(key, out var v);
            v = Mathf.MoveTowards(v, on ? 1f : 0f, Time.unscaledDeltaTime * speed);
            hoverAnim[key] = v;
            return v;
        }

        public static float Ease(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        /// <summary>A rounded button. Returns true when clicked (mouse) or activated (Enter/Space while focused).</summary>
        public static bool Button(Rect r, string text, Kind kind = Kind.Normal, bool enabled = true, int fontSize = 30, string hint = null)
        {
            EnsureStyles();
            var e = Event.current;
            var id = GUIUtility.GetControlID(FocusType.Passive);
            if (Locked) enabled = false;
            var my = Locked ? -1 : Index++;
            if (!Locked) countNow++;
            var hover = enabled && r.Contains(e.mousePosition);
            if (hover && mouseMoved && e.type == EventType.Layout) Focus = my;
            var focused = enabled && Focus == my;
            var clicked = false;

            if (enabled)
            {
                if (e.type == EventType.MouseDown && e.button == 0 && hover) { GUIUtility.hotControl = id; e.Use(); }
                else if (e.type == EventType.MouseUp && GUIUtility.hotControl == id)
                {
                    GUIUtility.hotControl = 0; e.Use();
                    if (hover) clicked = true;
                }
                if (KeyActivate && focused && e.type == EventType.Layout) { clicked = true; KeyActivate = false; }
            }

            if (e.type == EventType.Repaint)
            {
                var key = id * 31 + my;
                var h = Anim(key, hover || focused);
                var pressed = GUIUtility.hotControl == id && hover;
                Color top, edge, ink;
                switch (kind)
                {
                    case Kind.Primary: top = Color.Lerp(Sun, new Color(1f, 0.88f, 0.4f), h); edge = SunDeep; ink = Ink; break;
                    case Kind.Danger: top = Color.Lerp(new Color(0.78f, 0.28f, 0.30f), Coral, h); edge = new Color(0.5f, 0.12f, 0.16f); ink = Color.white; break;
                    case Kind.Ghost: top = Color.Lerp(new Color(1, 1, 1, 0.07f), new Color(1, 1, 1, 0.20f), h); edge = new Color(1, 1, 1, 0.0f); ink = Cream; break;
                    default: top = Color.Lerp(Slate, new Color(0.34f, 0.40f, 0.68f), h); edge = new Color(0.10f, 0.12f, 0.28f); ink = Cream; break;
                }
                if (!enabled) { top = new Color(0.25f, 0.27f, 0.36f, 0.7f); edge = new Color(0.15f, 0.16f, 0.24f, 0.7f); ink = new Color(1, 1, 1, 0.35f); }
                var lift = enabled && !pressed ? h * 4f : 0f;
                var body = new Rect(r.x - h * 6f, r.y - lift, r.width + h * 12f, r.height);
                if (pressed) body = new Rect(body.x + 2, body.y + 4, body.width - 4, body.height - 4);
                var depth = pressed ? 2f : 7f;
                if (kind != Kind.Ghost) Fill(new Rect(body.x, body.y + depth, body.width, body.height), edge, 20f);
                Fill(body, top, 20f);
                if (kind != Kind.Ghost) Fill(new Rect(body.x + 8, body.y + 5, body.width - 16, body.height * 0.34f), new Color(1, 1, 1, 0.10f + 0.06f * h), 14f);
                if (focused) Outline(new Rect(body.x - 4, body.y - 4, body.width + 8, body.height + 8 + depth), new Color(1f, 1f, 1f, 0.85f), 3f, 24f);
                btnText.fontSize = fontSize;
                btnText.normal.textColor = new Color(0, 0, 0, kind == Kind.Primary ? 0.0f : 0.35f);
                GUI.Label(new Rect(body.x, body.y + 2, body.width, body.height), text, btnText);
                btnText.normal.textColor = ink;
                GUI.Label(body, text, btnText);
                if (!string.IsNullOrEmpty(hint) && h > 0.01f)
                    Text(new Rect(r.xMax + 24, r.y, 520, r.height), hint, 20, new Color(1, 1, 1, 0.85f * h), TextAnchor.MiddleLeft, FontStyle.Italic);
            }
            if (e.type == EventType.Layout && my >= 0) HoverSound(my, hover || focused);
            if (clicked) GameAudio.PlayUi(kind == Kind.Danger ? Sfx.UiBack : Sfx.UiClick);
            return clicked;
        }

        static int lastHoverIndex = -1;
        static void HoverSound(int index, bool on)
        {
            if (on && lastHoverIndex != index) { lastHoverIndex = index; GameAudio.PlayUi(Sfx.UiHover); }
            if (!on && lastHoverIndex == index) lastHoverIndex = -1;
        }

        public static void ResetHover() { lastHoverIndex = -1; }

        /// <summary>Horizontal slider 0..1 with a rounded track and knob. Mouse drag, or left/right when focused.</summary>
        public static float Slider(Rect r, float value, string name)
        {
            var e = Event.current;
            var id = GUIUtility.GetControlID(FocusType.Passive);
            var my = Locked ? -1 : Index++;
            if (!Locked) countNow++;
            var hover = !Locked && r.Contains(e.mousePosition);
            if (hover && mouseMoved && e.type == EventType.Layout) Focus = my;
            var focused = !Locked && Focus == my;
            var track = new Rect(r.x, r.center.y - 9, r.width, 18);
            if (e.type == EventType.MouseDown && e.button == 0 && hover) { GUIUtility.hotControl = id; e.Use(); }
            if (!Locked && GUIUtility.hotControl == id && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
            {
                value = Mathf.Clamp01((e.mousePosition.x - track.x) / track.width);
                e.Use();
            }
            if (e.type == EventType.MouseUp && GUIUtility.hotControl == id) { GUIUtility.hotControl = 0; e.Use(); GameAudio.PlayUi(Sfx.UiClick); }
            if (focused && e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.LeftArrow) { value = Mathf.Clamp01(value - 0.05f); e.Use(); }
                if (e.keyCode == KeyCode.RightArrow) { value = Mathf.Clamp01(value + 0.05f); e.Use(); }
            }
            if (e.type == EventType.Repaint)
            {
                var h = Anim(id * 17 + my, hover || focused);
                Fill(track, new Color(1, 1, 1, 0.14f), 9f);
                Fill(new Rect(track.x, track.y, Mathf.Max(18f, track.width * value), track.height), Color.Lerp(Sky, Sun, value), 9f);
                var kx = track.x + track.width * value;
                Fill(new Rect(kx - 17 - h * 2, track.center.y - 17 - h * 2, 34 + h * 4, 34 + h * 4), Cream, 20f);
                if (focused) Outline(new Rect(r.x - 6, r.y - 2, r.width + 12, r.height + 4), new Color(1, 1, 1, 0.5f), 2f, 16f);
            }
            return value;
        }

        public static bool Toggle(Rect r, string text, bool value)
        {
            var label = text + ":  " + (value ? "AÇIK" : "KAPALI");
            return Button(r, label, value ? Kind.Normal : Kind.Ghost, true, 26) ? !value : value;
        }

        /// <summary>Animated three-dot spinner for waiting states.</summary>
        public static void Spinner(Vector2 center, float size)
        {
            for (var i = 0; i < 8; i++)
            {
                var a = i / 8f * Mathf.PI * 2f - Time.unscaledTime * 6f;
                var f = Mathf.Repeat(i / 8f - Time.unscaledTime * 1.0f, 1f);
                var p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * size;
                var s = size * (0.22f + 0.18f * f);
                Fill(new Rect(p.x - s, p.y - s, s * 2, s * 2), new Color(1f, 0.85f, 0.35f, 0.25f + 0.75f * f), s);
            }
        }
    }
}
