using System;
using System.Collections.Generic;
using UnityEngine;
namespace BeforeJericho.UI
{
    /// <summary>Player options (HTML prototype OPT/RANGE tables) persisted in PlayerPrefs. Changes apply immediately through <see cref="Changed"/>.</summary>
    public static class GameSettings
    {
        public static readonly Dictionary<string, string[]> Options = new()
        {
            ["lang"] = new[]{"ko","en"}, ["subs"] = new[]{"on","off"}, ["subSize"] = new[]{"s","m","l"}, ["subBg"] = new[]{"on","off"},
            ["speaker"] = new[]{"on","off"}, ["captions"] = new[]{"on","off"}, ["direction"] = new[]{"on","off"},
            ["hud"] = new[]{"dynamic","always","off"}, ["prompts"] = new[]{"on","off"}, ["tutorial"] = new[]{"on","off"}, ["crosshair"] = new[]{"dot","cross","off"},
            ["invert"] = new[]{"off","on"}, ["shake"] = new[]{"on","off"}, ["blur"] = new[]{"off","on"}, ["mode"] = new[]{"full","win"},
            ["hold"] = new[]{"hold","toggle"}, ["aimassist"] = new[]{"off","on"}, ["flash"] = new[]{"off","on"}, ["contrast"] = new[]{"off","on"},
        };
        public static readonly Dictionary<string, Vector2Int> Ranges = new()
        {
            ["hudop"] = new(0,100), ["sens"] = new(1,100), ["aimsens"] = new(1,100), ["fov"] = new(60,100), ["bright"] = new(0,100),
            ["master"] = new(0,100), ["dialogue"] = new(0,100), ["music"] = new(0,100), ["sfx"] = new(0,100), ["ambient"] = new(0,100),
        };
        // Prototype defaults, except FOV which keeps the shipped 68° camera.
        static readonly Dictionary<string, string> Defaults = new()
        {
            ["lang"]="ko", ["hud"]="dynamic", ["hudop"]="100", ["prompts"]="on", ["tutorial"]="on", ["crosshair"]="dot",
            ["subs"]="on", ["subSize"]="m", ["subBg"]="on", ["speaker"]="on", ["captions"]="on", ["direction"]="on",
            ["sens"]="50", ["aimsens"]="40", ["invert"]="off", ["shake"]="on", ["blur"]="off", ["fov"]="68", ["bright"]="50", ["mode"]="full",
            ["master"]="90", ["dialogue"]="100", ["music"]="60", ["sfx"]="85", ["ambient"]="75", ["hold"]="hold", ["aimassist"]="off", ["flash"]="off", ["contrast"]="off",
            ["firstRun"]="0", ["binds"]="",
        };
        const string PrefsKey = "bj-settings-v1";
        [Serializable] class Data { public List<string> keys = new(), values = new(); }
        static Dictionary<string, string> _values;
        static bool _transient, _dirty;
        /// <summary>Persist slider values changed since the last save (called when Options closes).</summary>
        public static void Flush() { if(_dirty && !_transient) Save(); _dirty = false; }
        public static event Action Changed;

        static Dictionary<string, string> Values
        {
            get
            {
                if(_values != null) return _values;
                _values = new Dictionary<string, string>(Defaults);
                try
                {
                    var data = JsonUtility.FromJson<Data>(PlayerPrefs.GetString(PrefsKey, "{}"));
                    for(int i = 0; data != null && i < data.keys.Count && i < data.values.Count; i++) if(Defaults.ContainsKey(data.keys[i])) _values[data.keys[i]] = data.values[i];
                }
                catch(ArgumentException e) { Debug.LogWarning("Settings unreadable, using defaults: " + e.Message); }
                return _values;
            }
        }

        public static string Choice(string id) => Values.TryGetValue(id, out string v) ? v : "";
        public static bool On(string id) => Choice(id) == "on";
        public static int Range(string id) => int.TryParse(Choice(id), out int v) ? v : 0;
        public static bool FirstRunDone { get => Choice("firstRun") == "1"; set => Set("firstRun", value ? "1" : "0"); }
        public static string BindingOverrides { get => Choice("binds"); set => Set("binds", value ?? ""); }

        public static void Set(string id, string value)
        {
            if(Values.TryGetValue(id, out string old) && old == value) return;
            Values[id] = value;
            if(!_transient) { if(Ranges.ContainsKey(id)) _dirty = true; else Save(); }
            Changed?.Invoke();
            if(id == "lang") Loc.NotifyChanged();
        }
        public static void Cycle(string id, int direction)
        {
            var o = Options[id]; int i = Array.IndexOf(o, Choice(id));
            Set(id, o[((i < 0 ? 0 : i) + direction + o.Length) % o.Length]);
        }
        public static void SetRange(string id, int value) { var r = Ranges[id]; Set(id, Mathf.Clamp(value, r.x, r.y).ToString()); }

        /// <summary>QA runs: defaults in memory only, so automation never reads or overwrites the player's options.</summary>
        public static void UseTransientDefaults(bool transient, string language = null)
        {
            _transient = transient; _values = transient ? new Dictionary<string, string>(Defaults) { ["firstRun"] = "1" } : null;
            if(transient && language != null) _values["lang"] = language;
            Changed?.Invoke(); Loc.NotifyChanged();
        }

        static void Save()
        {
            var data = new Data(); foreach(var kv in _values) { data.keys.Add(kv.Key); data.values.Add(kv.Value); }
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(data)); PlayerPrefs.Save();
        }
    }
}
