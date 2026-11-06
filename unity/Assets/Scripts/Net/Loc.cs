using System.Collections.Generic;
using UnityEngine;

namespace WhoBrokeIt.Net
{
    /// <summary>
    /// Turkish / English texts. The source texts in the code are Turkish and double as the keys: Loc.T("Hızlı Oyun") returns "Quick Play" when English is selected.
    /// A whole text is looked up first; texts built from pieces ("Süre: " + time) are translated piece by piece (longest known piece first).
    /// Unknown texts stay as they are, so a missing translation shows Turkish, never an empty string. The pairs live in LocEn.cs.
    /// </summary>
    public static class Loc
    {
        public const int Turkish = 0, English = 1;
        static int lang = -1;
        static readonly Dictionary<string, string> cache = new Dictionary<string, string>();
        static KeyValuePair<string, string>[] byLength;

        public static int Language
        {
            get
            {
                if (lang < 0)
                    lang = PlayerPrefs.GetInt("wbi_lang", Application.systemLanguage == SystemLanguage.Turkish ? Turkish : English);
                return lang;
            }
            set
            {
                if (value == lang) return;
                lang = value; PlayerPrefs.SetInt("wbi_lang", value); PlayerPrefs.Save(); cache.Clear();
            }
        }

        public static string LanguageName => Language == Turkish ? "Türkçe" : "English";
        public static void Toggle() { Language = Language == Turkish ? English : Turkish; }

        public static string T(string tr)
        {
            if (string.IsNullOrEmpty(tr) || Language == Turkish) return tr;
            if (cache.TryGetValue(tr, out var hit)) return hit;
            var en = Translate(tr);
            if (cache.Count > 4000) cache.Clear();
            cache[tr] = en;
            return en;
        }

        static string Translate(string tr)
        {
            if (LocEn.Pairs.TryGetValue(tr, out var whole)) return whole;
            if (byLength == null)
            {
                var list = new List<KeyValuePair<string, string>>(LocEn.Pairs);
                list.Sort((a, b) => b.Key.Length.CompareTo(a.Key.Length));
                byLength = list.ToArray();
            }
            var s = tr;
            foreach (var kv in byLength)
                if (kv.Key.Length >= 4 && s.Contains(kv.Key)) s = s.Replace(kv.Key, kv.Value);
            return s;
        }
    }
}
