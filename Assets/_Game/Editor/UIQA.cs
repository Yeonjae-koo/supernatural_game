using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace BeforeJericho.Editor
{
    /// <summary>QA helpers for UI captures: pin the Game view to an exact resolution (16:9 checks).</summary>
    public static class UIQA
    {
        static readonly Assembly EditorAssembly = typeof(UnityEditor.Editor).Assembly;

        public static string SetGameView(int width, int height)
        {
            var sizesType = EditorAssembly.GetType("UnityEditor.GameViewSizes");
            var instance = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            var group = sizesType.GetMethod("GetGroup").Invoke(instance, new object[] { (int)GameViewSizeGroupType.Standalone });
            var groupType = group.GetType();
            int count = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null), index = -1;
            string label = "QA " + width + "x" + height;
            for(int i = 0; i < count; i++)
            {
                var size = groupType.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                if((string)size.GetType().GetProperty("baseText").GetValue(size) == label) { index = i; break; }
            }
            if(index < 0)
            {
                var sizeType = EditorAssembly.GetType("UnityEditor.GameViewSize"); var kind = EditorAssembly.GetType("UnityEditor.GameViewSizeType");
                var size = Activator.CreateInstance(sizeType, Enum.ToObject(kind, 1), width, height, label);
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                index = count;
            }
            var viewType = EditorAssembly.GetType("UnityEditor.GameView");
            var view = EditorWindow.GetWindow(viewType, false, null, false);
            viewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(view, index);
            view.Repaint();
            return label + " @" + index;
        }

        static HuntSession S => UnityEngine.Object.FindAnyObjectByType<HuntSession>();
        static HuntHUD Hud => S.hud;
        static HuntInteractable Item(string kind) => Array.Find(S.items, i => i && i.kind.ToString() == kind);

        /// <summary>Put the running slice into one of the prototype's 14 screens (captures / manual review).</summary>
        public static string Step(string step)
        {
            var s = S; var hud = Hud; var bf = BindingFlags.Instance | BindingFlags.NonPublic;
            switch(step)
            {
                case "qa": Application.runInBackground = true; UI.GameSettings.UseTransientDefaults(true, "ko"); s.saveSlot = "qa-ui-" + s.gameObject.scene.name; break;
                case "ko": UI.GameSettings.Set("lang", "ko"); break;
                case "en": UI.GameSettings.Set("lang", "en"); break;
                case "boot": s.MainMenu(); UI.GameSettings.Set("firstRun", "0"); break;
                case "menu": UI.GameSettings.FirstRunDone = true; s.MainMenu(); break;
                case "new": hud.StartNewGame(); break;
                case "explore":
                {
                    var target = Array.Find(s.items, i => i && !i.Used(s) && i.kind != HuntItem.Anchor && !hud.IsExit(i.kind) && i.Available(s));
                    Vector3 p = target.transform.position + (s.player.transform.position - target.transform.position).normalized * 1.2f; p.y = s.player.transform.position.y;
                    s.player.Teleport(p); var d = target.transform.position - p; s.player.view.Face(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 20);
                    return "near " + target.kind;
                }
                case "clue": { var kinds = hud.ClueKinds(); foreach(var k in kinds) { var i = Item(k); if(i && !i.Used(s)) { s.Interact(i); return "collected " + k; } } break; }
                case "hit": s.Damage(18); break;
                case "pause": s.SetPaused(true); break;
                case "resume": s.Resume(); break;
                case "journal": hud.OpenJournal(false); break;
                case "map": hud.OpenJournal(true); break;
                case "tab": typeof(HuntHUD).GetField("_journal", bf).GetValue(hud).GetType().GetMethod("Step").Invoke(typeof(HuntHUD).GetField("_journal", bf).GetValue(hud), new object[] { 1 }); break;
                case "evidence": { foreach(var k in hud.ClueKinds()) { var i = Item(k); if(i && i.Used(s)) { hud.OpenEvidence(i); return "inspect " + k; } } return "no clue found yet"; }
                case "settings": hud.OpenSettings(); break;
                case "dead": s.Damage(100); break;
                case "retry": s.RestoreCheckpoint(); break;
                case "complete": s.CompleteChapter(); break;
                case "credits": hud.OpenCredits(); break;
                case "cleanup": foreach(var f in System.IO.Directory.GetFiles(Application.persistentDataPath, "qa-ui-*")) System.IO.File.Delete(f); UI.GameSettings.UseTransientDefaults(false); break;
                default: return "unknown step " + step;
            }
            return step + " → " + hud.Current;
        }
    }
}
