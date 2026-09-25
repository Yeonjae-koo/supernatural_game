using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BeforeJericho;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace BeforeJericho.Editor
{
    public static class PrologueSmoke
    {
        public static string Result = "Not run";
        static StringBuilder _report;
        static void Check(bool condition, string label) { if(!condition) throw new Exception(label); _report.AppendLine("PASS: " + label); }
        static void Keys(Keyboard keyboard, params Key[] keys) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); }
        static void MouseButtons(Mouse mouse, bool aim, bool attack)
        {
            var state = new MouseState(); state = state.WithButton(MouseButton.Right, aim).WithButton(MouseButton.Left, attack); InputSystem.QueueStateEvent(mouse, state);
        }
        public static async void Run()
        {
            if(!EditorApplication.isPlaying) { Result="Enter Play Mode first"; return; }
            Result="Running"; _report=new StringBuilder();
            var session=UnityEngine.Object.FindAnyObjectByType<HuntSession>();
            string previousSlot=session.saveSlot;
            Keyboard keyboard=null; Mouse mouse=null;
            var oldBackground=InputSystem.settings.backgroundBehavior;
            var oldEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            try
            {
                Application.runInBackground=true;
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                keyboard=InputSystem.AddDevice<Keyboard>(); mouse=InputSystem.AddDevice<Mouse>();
                session.saveSlot="qa-live-"+Guid.NewGuid().ToString("N");session.NewGame();
                var combat=session.player.GetComponent<HunterCombat>();
                await Task.Delay(250);
                var start=session.player.transform.position;
                Keys(keyboard,Key.W);await Task.Delay(500);Keys(keyboard);await Task.Delay(80);
                Check(Vector3.Distance(start,session.player.transform.position)>.7f,"Input System WASD moves CharacterController in Play Mode");
                MouseButtons(mouse,true,false);await Task.Delay(180);
                Check(session.player.view.lens.fieldOfView < 60,"Aim narrows OTS field of view");
                MouseButtons(mouse,true,true);await Task.Delay(90);MouseButtons(mouse,true,false);await Task.Delay(100);
                Check(combat.Ammo==6,"Mouse aim/fire spends exactly one round");
                Keys(keyboard,Key.R);await Task.Delay(80);Keys(keyboard);await Task.Delay(1750);
                Check(combat.Ammo==7 && combat.Reserve==20,"Reload transfers reserve ammunition");
                MouseButtons(mouse,false,false);await Task.Delay(80);
                Keys(keyboard,Key.Tab);await Task.Delay(100);Keys(keyboard);await Task.Delay(80);
                Check(session.Journal && !session.Playing,"Tab opens journal and disables gameplay");
                var paused=session.player.transform.position;Keys(keyboard,Key.W);await Task.Delay(150);Keys(keyboard);
                Check(Vector3.Distance(paused,session.player.transform.position)<.01f,"Journal prevents movement");session.Resume();
                HuntInteractable Item(HuntItem kind)=>Array.Find(session.items,i=>i.kind==kind);
                foreach(var kind in new[]{HuntItem.Mirror,HuntItem.Photos,HuntItem.Ledger,HuntItem.Scratches,HuntItem.Iron})
                {
                    session.player.Teleport(Item(kind).transform.position+new Vector3(0,-1,-1.2f));
                    combat.SetLighter(true);session.Interact(Item(kind));
                }
                Check(session.ghost.Active,"Shop investigation manifests the ghost");
                session.ghost.Hit(false);Check(!session.Resolved,"Ordinary weapon cannot resolve spirit");
                session.Interact(Item(HuntItem.Record));session.Interact(Item(HuntItem.Salt));
                session.Damage(100);Check(session.HP==0,"Death ends gameplay");session.RestoreCheckpoint();
                Check(session.HP==100 && session.Has(HuntItem.Record),"Retry restores investigation snapshot");
                session.player.Teleport(new Vector3(0,.1f,63.3f));session.player.view.Face(0,10);
                await Task.Delay(850);combat.Equip(2);combat.SetLighter(true);
                session.ghost.Hit(true);
                Keys(keyboard,Key.E);
                float deadline=Time.realtimeSinceStartup+45;
                while(!session.Resolved && Time.realtimeSinceStartup<deadline)
                {
                    await Task.Delay(300);
                    Keys(keyboard,Key.E); // same held state again: survives an Editor-side virtual device reset, never a new press
                    if(session.ghost.Stagger<1) session.ghost.Hit(true);
                }
                Keys(keyboard);await Task.Delay(100);
                Check(session.Resolved,"Actual hold-E input salts and ignites the anchor across iron work windows");
                Check(session.ghost.Active==false && session.fire.activeSelf,"Anchor resolution removes ghost and enables aftermath");
                Check(HuntSave.TryLoad(session.saveSlot,out var saved) && saved.resolved,"Resolved hunt is durably saved");
                session.RestoreCheckpoint();Check(session.Resolved && !session.ghost.Active,"Resolved state survives checkpoint reload");
                session.player.Teleport(new Vector3(1,.1f,2));await Task.Delay(100);Keys(keyboard,Key.E);await Task.Delay(150);Keys(keyboard);
                Check(session.Completed,"Exit interaction reaches John voicemail epilogue card");
                Result=_report.ToString();
            }
            catch(Exception e) { Result=_report+"FAIL: "+e;Debug.LogError("Prologue smoke: "+e); }
            finally
            {
                if(keyboard!=null) InputSystem.RemoveDevice(keyboard);if(mouse!=null) InputSystem.RemoveDevice(mouse);
                InputSystem.settings.backgroundBehavior=oldBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;
                session.MainMenu();session.saveSlot=previousSlot;
                File.WriteAllText(Path.Combine(Application.dataPath,"../Logs/PrologueSmoke.txt"),Result);
            }
        }
    }
}
