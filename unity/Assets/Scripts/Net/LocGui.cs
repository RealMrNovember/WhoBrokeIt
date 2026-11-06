using UnityEngine;

namespace WhoBrokeIt.Net
{
    /// <summary>GUI.Label / GUI.Button that pass their text through Loc first (the texts in the code are Turkish).</summary>
    public static class LocGui
    {
        public static void Label(Rect r, string text) => GUI.Label(r, Loc.T(text));
        public static void Label(Rect r, string text, GUIStyle style) => GUI.Label(r, Loc.T(text), style);
        public static bool Button(Rect r, string text) => GUI.Button(r, Loc.T(text));
        public static bool Button(Rect r, string text, GUIStyle style) => GUI.Button(r, Loc.T(text), style);
    }
}
