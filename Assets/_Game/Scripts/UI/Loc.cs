using System;
using System.Collections.Generic;
using UnityEngine;
namespace BeforeJericho.UI
{
    /// <summary>ko/en String Table (TSV rows: Table/key, ko, en). Gameplay keeps producing English;
    /// <see cref="Src"/> maps those runtime strings back to their row, so progression code and tests stay untouched.</summary>
    public static class Loc
    {
        static readonly Dictionary<string, string[]> Rows = new();
        static readonly Dictionary<string, string> KeyByEnglish = new();
        static readonly List<(string key, string prefix, string suffix)> Patterns = new();
        public static event Action Changed;
        /// <summary>Development only: show Table/key instead of text (HTML "String Key 보기").</summary>
        public static bool ShowKeys;
        /// <summary>Keys resolved since the last clear; the development string panel lists them.</summary>
        public static readonly HashSet<string> Used = new();
        public static bool Korean => GameSettings.Choice("lang") == "ko";
        public static bool Loaded => Rows.Count > 0;

        public static void Load(IEnumerable<TextAsset> tables)
        {
            Rows.Clear(); KeyByEnglish.Clear(); Patterns.Clear();
            foreach(var table in tables)
            {
                if(!table) { Debug.LogError("Loc: a String Table reference is missing."); continue; }
                foreach(string raw in table.text.Split('\n'))
                {
                    string line = raw.TrimEnd('\r');
                    if(line.Length == 0 || line[0] == '#' || line.StartsWith("key\t")) continue;
                    string[] f = line.Split('\t');
                    if(f.Length != 3) { Debug.LogWarning("Loc: malformed row in " + table.name + ": " + line); continue; }
                    string ko = f[1].Replace("\\n", "\n"), en = f[2].Replace("\\n", "\n");
                    Rows[f[0]] = new[] { ko, en };
                    if(en.Length == 0) continue;
                    int slot = en.IndexOf("{0}", StringComparison.Ordinal);
                    if(slot >= 0) Patterns.Add((f[0], en.Substring(0, slot), en.Substring(slot + 3)));
                    else KeyByEnglish.TryAdd(en, f[0]);
                }
            }
            // Longest literal first so "Sunset Star evidence {0}/5" wins over "{0}".
            Patterns.Sort((a, b) => (b.prefix.Length + b.suffix.Length).CompareTo(a.prefix.Length + a.suffix.Length));
        }

        public static void NotifyChanged() => Changed?.Invoke();
        public static bool Has(string key) => Rows.ContainsKey(key);

        public static string T(string key)
        {
            Used.Add(key);
            if(ShowKeys) return KeyTag(key);
            if(!Rows.TryGetValue(key, out var v)) return key;
            return Korean ? v[0] : v[1];
        }

        /// <summary>English column regardless of display language: in-world text (papers, map titles) is never localized.</summary>
        public static string English(string key)
        {
            Used.Add(key);
            if(ShowKeys) return KeyTag(key);
            return Rows.TryGetValue(key, out var v) ? v[1] : key;
        }

        /// <summary>Raw ko/en column without tracking (development string list).</summary>
        public static string Column(string key, bool korean) => Rows.TryGetValue(key, out var v) ? v[korean ? 0 : 1] : "";

        public static string Format(string key, string arg) => ShowKeys ? KeyTag(key) : T(key).Replace("{0}", arg);

        /// <summary>Localize a runtime English string: exact row, then {0} pattern, then line by line.</summary>
        public static string Src(string english)
        {
            if(string.IsNullOrEmpty(english)) return "";
            if(KeyByEnglish.TryGetValue(english, out string key)) return T(key);
            foreach(var p in Patterns)
                if(english.Length >= p.prefix.Length + p.suffix.Length && english.StartsWith(p.prefix, StringComparison.Ordinal) && english.EndsWith(p.suffix, StringComparison.Ordinal))
                {
                    string inner = english.Substring(p.prefix.Length, english.Length - p.prefix.Length - p.suffix.Length);
                    return ShowKeys ? KeyTag(p.key) + " " + Src(inner) : T(p.key).Replace("{0}", Src(inner));
                }
            if(english.IndexOf('\n') >= 0)
            {
                string[] lines = english.Split('\n');
                for(int i = 0; i < lines.Length; i++) lines[i] = Src(lines[i]);
                return string.Join("\n", lines);
            }
            return english;
        }

        /// <summary>Row key for a runtime string (exact or pattern), used to pick its presentation (Dialogue, Tutorial, Clues...).</summary>
        public static string KeyOf(string english)
        {
            if(string.IsNullOrEmpty(english)) return null;
            if(KeyByEnglish.TryGetValue(english, out string key)) return key;
            foreach(var p in Patterns)
                if(english.StartsWith(p.prefix, StringComparison.Ordinal) && english.EndsWith(p.suffix, StringComparison.Ordinal) && english.Length >= p.prefix.Length + p.suffix.Length) return p.key;
            return null;
        }

        static string KeyTag(string key) => "<size=72%><color=#7fd0c0><mark=#0028288C>" + key + "</mark></color></size>";
    }
}
