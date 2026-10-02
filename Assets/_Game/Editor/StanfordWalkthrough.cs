using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace BeforeJericho.Editor
{
    /// <summary>Input-only CH06 route validation. Reads state for assertions; never teleports or applies combat/progression directly.</summary>
    public static class StanfordWalkthrough
    {
        public static string Result="Not run";
        public static string Stage="Idle";
        static Keyboard keyboard; static Mouse mouse; static HuntSession session; static HunterCombat combat;
        static StringBuilder report; static bool cancelled;
        static readonly System.Collections.Generic.List<InputDevice> physical=new(); static KeyboardState heldKeys; static Vector2 pendingLook; static bool heldAim, heldFire;
        static void Check(bool condition,string message) { if(!condition) throw new Exception(message); report.AppendLine("PASS: "+message); }
        static void Live() { if(cancelled || !EditorApplication.isPlaying || !session) throw new Exception("Play Mode ended or route cancelled"); if(!session.Playing) throw new Exception("Gameplay stopped at "+Stage+" HP="+session.HP+" "+session.Message); }
        static void Queue(KeyboardState keys,Vector2 look=default,bool aim=false,bool fire=false) { heldKeys=keys; pendingLook=look; heldAim=aim; heldFire=fire; }
        static void InputTick()
        {
            if(InputState.currentUpdateType!=InputUpdateType.Dynamic || keyboard==null || mouse==null) return;
            InputSystem.QueueStateEvent(keyboard,heldKeys);
            InputSystem.QueueStateEvent(mouse,new MouseState {delta=pendingLook}.WithButton(MouseButton.Right,heldAim).WithButton(MouseButton.Left,heldFire));
            pendingLook=Vector2.zero;
        }
        static async Task Press(Key key) { if(key!=Key.Tab) Live(); Queue(new KeyboardState(key)); await Task.Delay(110); Queue(new KeyboardState()); await Task.Delay(120); }
        static void OnMode(PlayModeStateChange mode) { if(mode==PlayModeStateChange.ExitingPlayMode) cancelled=true; }
        public static void Cancel() { cancelled=true; }
        static void Steer(ref KeyboardState keys,Vector3 direction)
        {
            Vector3 local=Quaternion.Euler(0,-session.player.view.Yaw,0)*direction.normalized;
            if(local.z>.38f)keys.Set(Key.W,true); if(local.z<-.38f)keys.Set(Key.S,true);
            if(local.x>.38f)keys.Set(Key.D,true); if(local.x<-.38f)keys.Set(Key.A,true);
        }
        static async Task Walk(float x,float z,float tolerance=.55f)
        {
            Stage="Walk "+x+","+z; float start=Time.realtimeSinceStartup; Vector3 previous=session.player.transform.position; float stuck=0;
            while(true)
            {
                Live();
                Vector3 delta=new Vector3(x,session.player.transform.position.y,z)-session.player.transform.position;
                if(delta.magnitude<tolerance) break;
                if(Time.realtimeSinceStartup-start>100) throw new Exception("Walk timeout "+Stage+" at "+session.player.transform.position);
                // Face the destination with mouse look so 8-way WASD steering cannot accumulate a heading error.
                float yawError=Mathf.DeltaAngle(session.player.view.Yaw,Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg);
                var keys=new KeyboardState(); Steer(ref keys,delta); Queue(keys,new Vector2(Mathf.Clamp(yawError*.5f,-25,25)/session.player.view.sensitivity,0));
                await Task.Delay(90);
                float moved=Vector3.Distance(previous,session.player.transform.position);
                stuck=moved<.004f?stuck+.09f:0; previous=session.player.transform.position;
                if(stuck>2.5f) throw new Exception("Blocked "+Stage+" at "+previous+" stance="+session.player.Stance);
            }
            Queue(new KeyboardState()); await Task.Delay(100);
            report.AppendLine("WALK: "+x+","+z+" actual="+session.player.transform.position+" stance="+session.player.Stance+" HP="+session.HP);
        }
        static async Task Use(HuntItem kind,bool collected=true)
        {
            Stage="Interact "+kind; Live(); Queue(new KeyboardState()); await Task.Delay(180);
            Check(combat.Target && combat.Target.kind==kind,"Interaction target is "+kind+" (actual "+(combat.Target?combat.Target.kind.ToString():"none")+")");
            await Press(Key.E);
            if(collected) Check(session.stanford.Used(kind),"E collected/activated "+kind);
        }
        [MenuItem("Before Jericho/QA/Input-only Stanford Walkthrough")]
        public static async void Run()
        {
            if(Result=="Running") return;
            session=UnityEngine.Object.FindAnyObjectByType<HuntSession>();
            if(!EditorApplication.isPlaying || !session || !session.stanford) { Result="Open CH06 and enter Play Mode first"; return; }
            Result="Running"; Stage="Setup"; report=new StringBuilder(); cancelled=false;
            string prior=session.saveSlot, slot="qa-stanford-walk-"+Guid.NewGuid().ToString("N");
            bool oldRun=Application.runInBackground; var oldBg=InputSystem.settings.backgroundBehavior; var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
            try
            {
                Application.runInBackground=true; InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                // Physical keyboard/mouse input would steer Dean during an unattended run.
                foreach(var device in InputSystem.devices) if((device is Keyboard || device is Mouse) && device.enabled) physical.Add(device);
                foreach(var device in physical) InputSystem.DisableDevice(device);
                keyboard=InputSystem.AddDevice<Keyboard>(); mouse=InputSystem.AddDevice<Mouse>(); EditorApplication.playModeStateChanged+=OnMode; InputSystem.onBeforeUpdate+=InputTick; Queue(new KeyboardState());
                session.saveSlot=slot; session.NewGame(); combat=session.player.GetComponent<HunterCombat>(); var ch=session.stanford; await Task.Delay(250);
                // Motel room: converge the evidence.
                await Walk(-2.5f,7.5f); await Use(HuntItem.JohnNotes);
                await Walk(-1,7.5f); await Use(HuntItem.JerichoClippings);
                await Walk(.5f,7.5f); await Use(HuntItem.RouteMap);
                await Walk(-1,3); await Walk(-2.8f,3); await Use(HuntItem.VoicemailTape);
                Check(ch.Converged,"Four inspected items converge on Jericho");
                await Press(Key.Tab); Check(session.Journal&&ch.JournalText.Contains("JERICHO, CALIFORNIA"),"Journal shows JERICHO / MISSING MEN / JOHN'S ROUTE"); await Press(Key.Tab);
                Queue(new KeyboardState(),default,true,true); await Task.Delay(150); Queue(new KeyboardState()); await Task.Delay(150);
                Check(combat.Ammo==7,"Attack input does nothing in the epilogue");
                await Walk(0,2.2f); await Walk(2.8f,1.6f); await Use(HuntItem.CallJohn);
                Check(session.Objective=="FIND SAM","Objective becomes FIND SAM");
                await Walk(0,1.6f); await Use(HuntItem.HitTheRoad,false);
                Stage="Montage"; float waited=0; while(ch.InMontage&&waited<15) { await Task.Delay(200); waited+=.2f; }
                Check(ch.Arrived&&session.player.transform.position.z>30,"Montage delivers Dean to Palo Alto");
                // Palo Alto: trunk, apartment, hallway.
                await Walk(-4.5f,36); await Use(HuntItem.StanfordTrunk);
                await Walk(3,50); await Walk(3,57); await Walk(6,57); await Walk(8,60);
                await Walk(6.4f,64); await Use(HuntItem.SamNeighbor,false);
                await Walk(8,66); await Walk(8,69.5f);
                Stage="Sam's door"; Queue(new KeyboardState(Key.W)); waited=0; while(!ch.GameComplete&&waited<4) { Live(); await Task.Delay(100); waited+=.1f; }
                Queue(new KeyboardState()); Check(ch.GameComplete,"Walking up to Sam's door triggers the final beat");
                Vector3 frozen=session.player.transform.position; Queue(new KeyboardState(Key.W)); await Task.Delay(500); Queue(new KeyboardState());
                Check(Vector3.Distance(frozen,session.player.transform.position)<.05f,"Dean stops at the door");
                waited=0; while(!session.Completed&&waited<5) { await Task.Delay(200); waited+=.2f; }
                Check(session.Completed&&session.hud.overlayTitle.text=="TO BE CONTINUED"&&session.hud.primaryLabel.text==BeforeJericho.UI.Loc.T("UI_Common/complete.continue"),"Fade to TO BE CONTINUED with the continue-to-credits button");
                Result="PASS\n"+report; Stage="Complete";
            }
            catch(Exception e) { Result="FAIL at "+Stage+": "+e+"\n"+report; Debug.LogError(Result); }
            finally
            {
                InputSystem.onBeforeUpdate-=InputTick; if(keyboard!=null) InputSystem.RemoveDevice(keyboard); if(mouse!=null) InputSystem.RemoveDevice(mouse); keyboard=null; mouse=null;
                foreach(var device in physical) if(device.added) InputSystem.EnableDevice(device); physical.Clear();
                EditorApplication.playModeStateChanged-=OnMode; Application.runInBackground=oldRun; InputSystem.settings.backgroundBehavior=oldBg; InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;
                if(session) session.saveSlot=prior;
                foreach(string suffix in new[]{"",".bak",".tmp"}) { string path=HuntSave.PathFor(slot)+suffix; if(File.Exists(path)) File.Delete(path); }
                Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/StanfordWalkthrough.txt",Result);
            }
        }
    }
}
