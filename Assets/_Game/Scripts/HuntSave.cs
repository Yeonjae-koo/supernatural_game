using System;
using System.IO;
using UnityEngine;
namespace BeforeJericho
{
    [Serializable] public sealed class GhoulSnapshot { public int id, hp; public bool active; public float x,y,z; }
    [Serializable] public sealed class HuntSnapshot
    {
        public int version = 1, evidence, hp = 100, ammo = 7, reserve = 21;
        public GhoulSnapshot[] ghouls;
        public string chapterId = ""; public int chapterFlags;
        public float x, y, z; public bool resolved; public string checkpoint = "Shop entrance";
    }
    public static class HuntSave
    {
        public static string PathFor(string slot) => Path.Combine(Application.persistentDataPath, slot + ".json");
        public static bool Valid(HuntSnapshot s)
        {
            if(s == null || s.hp <= 0 || s.hp > 100 || s.ammo < 0 || s.ammo > 7 || s.reserve < 0 || float.IsNaN(s.x) || float.IsInfinity(s.x) || Mathf.Abs(s.x) >= 100 || !(s.y > -5 && s.y < 20)) return false;
            if(s.version == 1) return string.IsNullOrEmpty(s.chapterId) && s.evidence >= 0 && s.evidence < 128 && s.z > -5 && s.z < 100 && (!s.resolved || (s.evidence & 127) == 127);
            if(s.version == 6) return s.z > -5 && s.z < 200 && StanfordChapter.ValidSnapshot(s);
            if(s.version == 5) return s.z > -5 && s.z < 200 && WinslowChapter.ValidSnapshot(s);
            if(s.version == 4) return s.z > -5 && s.z < 200 && GrantsChapter.ValidSnapshot(s);
            if(s.version == 3) return s.z > -5 && s.z < 200 && EastTexasChapter.ValidSnapshot(s);
            return s.version == 2 && s.z > -5 && s.z < 200 && AtchafalayaChapter.ValidSnapshot(s);
        }
        public static bool TryLoad(string slot, out HuntSnapshot snapshot, string expectedChapterId = null)
        {
            snapshot = null;
            foreach(string path in new[]{PathFor(slot), PathFor(slot) + ".bak"})
            {
                try { if(File.Exists(path)) { var s = JsonUtility.FromJson<HuntSnapshot>(File.ReadAllText(path)); if(Valid(s) && (expectedChapterId == null || (s.chapterId ?? "") == expectedChapterId)) { snapshot = s; return true; } } }
                catch(Exception e) when(e is IOException || e is ArgumentException || e is UnauthorizedAccessException) { Debug.LogWarning("Save unreadable: " + e.Message); }
            }
            return false;
        }
        public static void Write(string slot, HuntSnapshot snapshot)
        {
            if(!Valid(snapshot)) throw new ArgumentException("Invalid hunt snapshot.");
            string path = PathFor(slot), temp = path + ".tmp";
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(temp, JsonUtility.ToJson(snapshot, true));
            if(File.Exists(path))
            {
                bool primaryValid = false;
                try { primaryValid = Valid(JsonUtility.FromJson<HuntSnapshot>(File.ReadAllText(path))); } catch(ArgumentException) { }
                if(primaryValid) File.Replace(temp, path, path + ".bak");
                else { File.Copy(temp, path, true); File.Delete(temp); } // Preserve a valid backup when primary was corrupt.
            }
            else File.Move(temp, path);
        }
    }
}
