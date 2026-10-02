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
    /// <summary>Input-only CH05 route validation. Reads state for assertions; never teleports or applies combat/progression directly.</summary>
    public static class WinslowWalkthrough
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
                Live(); await FightNear();
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
        static ManifestationHunt Threat(float range)
        {
            ManifestationHunt best=null; float distance=range;
            foreach(var m in session.winslow.Spirit.Manifestations)
            {
                if(!m.Active||m.Dispersed) continue;
                Vector3 d=m.transform.position-session.player.transform.position; d.y=0;
                if(d.magnitude<distance) { best=m; distance=d.magnitude; }
            }
            return best;
        }
        // Iron only: turn toward the shape with mouse input, hold aim so Dean faces it, swing when in reach.
        static async Task FightNear()
        {
            if(session.player.IsCrawling || !session.winslow) return;
            var target=Threat(3.4f); if(!target) return;
            string previousStage=Stage; Stage="Iron vs "+target.name; float start=Time.realtimeSinceStartup, lastSwing=-10;
            while(target.Active && !target.Dispersed)
            {
                Live();
                if(Time.realtimeSinceStartup-start>20) throw new Exception("Iron fight timeout "+target.name+" player="+session.player.transform.position);
                Vector3 d=target.transform.position-session.player.transform.position; d.y=0;
                if(d.magnitude>5) break;
                float yaw=Mathf.DeltaAngle(session.player.view.Yaw,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg);
                var keys=new KeyboardState(); if(combat.Weapon!=2) keys.Set(Key.Digit3,true);
                bool swing=combat.Weapon==2 && d.magnitude<2.6f && Mathf.Abs(yaw)<20 && Time.realtimeSinceStartup-lastSwing>.75f;
                Queue(keys,new Vector2(Mathf.Clamp(yaw*.7f,-35,35)/(session.player.view.sensitivity*.65f),0),true,swing);
                if(swing) { lastSwing=Time.realtimeSinceStartup; report.AppendLine("IRON: "+target.name+" distance="+d.magnitude.ToString("F1")+" playerHP="+session.HP); }
                await Task.Delay(swing?120:60);
                Queue(new KeyboardState(),default,true); await Task.Delay(40);
            }
            Check(target.Dispersed,"Actual iron input dispersed "+target.name);
            Check(target.Active,"Dispersed manifestation was not permanently killed");
            Queue(new KeyboardState()); await Task.Delay(100); Stage=previousStage;
        }
        static async Task Use(HuntItem kind,bool collected=true)
        {
            Stage="Interact "+kind; Live(); Queue(new KeyboardState()); await Task.Delay(180);
            Check(combat.Target && combat.Target.kind==kind,"Interaction target is "+kind+" (actual "+(combat.Target?combat.Target.kind.ToString():"none")+")");
            await Press(Key.E);
            if(collected) Check(session.winslow.Used(kind),"E collected/activated "+kind);
        }
        static async Task FaceYaw(float target)
        {
            for(int turn=0;turn<14 && Mathf.Abs(Mathf.DeltaAngle(session.player.view.Yaw,target))>2;turn++)
            { float angle=Mathf.DeltaAngle(session.player.view.Yaw,target); Queue(new KeyboardState(),new Vector2(Mathf.Clamp(angle*.7f,-35,35)/session.player.view.sensitivity,0)); await Task.Delay(70); }
            Queue(new KeyboardState()); await Task.Delay(80);
        }
        static async Task PourSaltToward(ManifestationHunt target)
        {
            int before=session.winslow.Spirit.SaltCharges;
            Vector3 d=target.transform.position-session.player.transform.position;
            await FaceYaw(Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg); await Press(Key.Digit4);
            Queue(new KeyboardState(),default,false,true); await Task.Delay(120); Queue(new KeyboardState()); await Task.Delay(150);
            Check(session.winslow.Spirit.SaltCharges==before-1,"LMB poured a salt line toward the spirit");
            await Press(Key.Digit3); await FaceYaw(0);
        }
        // Hold E at the barrel with the lighter; salt the spirit's approach and swing iron when it gets close.
        static async Task BurnAnchor()
        {
            Stage="Burn anchor"; float start=Time.realtimeSinceStartup; var sp=session.winslow.Spirit; Vector3 spot=new Vector3(0,0,122.6f); int hits=0; float lastHp=session.HP;
            while(!session.Resolved)
            {
                Live();
                if(Time.realtimeSinceStartup-start>90) throw new Exception("Burn timeout progress="+session.BurnProgress+" HP="+session.HP);
                if(session.HP<lastHp) hits++; lastHp=session.HP;
                if(Threat(2.8f)) { await FightNear(); continue; }
                bool salted=false; for(int i=1;i<=3;i++) salted|=sp.transform.Find("Salt line "+i).gameObject.activeSelf;
                var approaching=Threat(12);
                if(!salted && approaching && sp.SaltCharges>0) { await PourSaltToward(approaching); continue; }
                if(!combat.LighterOn) { await Press(Key.F); continue; }
                var keys=new KeyboardState(Key.E); Vector3 back=spot-session.player.transform.position; back.y=0;
                if(back.magnitude>.45f) Steer(ref keys,back);
                Queue(keys); await Task.Delay(60);
            }
            Queue(new KeyboardState()); await Task.Delay(100);
            report.AppendLine("BURN: "+(Time.realtimeSinceStartup-start).ToString("F1")+"s, hits taken="+hits+", HP="+session.HP);
        }
        [MenuItem("Before Jericho/QA/Input-only Winslow Walkthrough")]
        public static async void Run()
        {
            if(Result=="Running") return;
            session=UnityEngine.Object.FindAnyObjectByType<HuntSession>();
            if(!EditorApplication.isPlaying || !session || !session.winslow) { Result="Open CH05 and enter Play Mode first"; return; }
            Result="Running"; Stage="Setup"; report=new StringBuilder(); cancelled=false;
            string prior=session.saveSlot, slot="qa-winslow-walk-"+Guid.NewGuid().ToString("N");
            bool oldRun=Application.runInBackground; var oldBg=InputSystem.settings.backgroundBehavior; var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
            try
            {
                Application.runInBackground=true; InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                // Physical keyboard/mouse input would steer Dean during an unattended run.
                foreach(var device in InputSystem.devices) if((device is Keyboard || device is Mouse) && device.enabled) physical.Add(device);
                foreach(var device in physical) InputSystem.DisableDevice(device);
                keyboard=InputSystem.AddDevice<Keyboard>(); mouse=InputSystem.AddDevice<Mouse>(); EditorApplication.playModeStateChanged+=OnMode; InputSystem.onBeforeUpdate+=InputTick; Queue(new KeyboardState());
                session.saveSlot=slot; session.NewGame(); combat=session.player.GetComponent<HunterCombat>(); var ch=session.winslow; await Task.Delay(250);
                // Road: silhouette and crash site.
                await Walk(-3,10); await Walk(0,18); Check(ch.EventFired(0),"Road silhouette appears on Route 66");
                await Walk(2.5f,30.9f); await Use(HuntItem.CrashMarks); await Task.Delay(200); Check(ch.EventFired(1),"Crash hallucination at the guardrail");
                // Diner: report, cremation notice, Mae.
                await Walk(-3,40); await Walk(-12,44); await Walk(-14,51.3f); await Use(HuntItem.PoliceReport);
                await Walk(-10,51.3f); await Use(HuntItem.CremationRecord);
                await Walk(-16.3f,48.8f); await Use(HuntItem.MaeTorres); Check(ch.JournalText.Contains("last night"),"Mae: John passed through the day before");
                // Impala mirror, then garage and closed motel.
                await Walk(-12,44); await Walk(-4,40); await Walk(-4,9); Check(ch.EventFired(2),"Rear-view mirror apparition at the Impala");
                await Walk(3,20); await Walk(3,43); await Walk(15,43); await Walk(15,50.8f); await Use(HuntItem.GarageRecord);
                await Walk(12.5f,52.4f); await Walk(8,52.4f); await Use(HuntItem.HankDoyle,false);
                await Walk(12,56); await Walk(12,66); await Walk(12,69); Check(ch.MidEncounter&&ch.Spirit.ActiveCount==1,"Road spirit manifests in the closed motel");
                float waited=0; while(!Threat(3.4f) && waited<12) { Live(); var m=ch.Spirit.Manifestations[0]; if(!m.Dispersed){ Vector3 d=m.transform.position-session.player.transform.position; float yaw=Mathf.DeltaAngle(session.player.view.Yaw,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg); Queue(new KeyboardState(),new Vector2(Mathf.Clamp(yaw*.7f,-35,35)/session.player.view.sensitivity,0)); } await Task.Delay(100); waited+=.1f; }
                await FightNear(); Check(ch.Spirit.Manifestations[0].Dispersed,"Iron input repels the road spirit");
                await Walk(15,72.8f); await Use(HuntItem.DriverIdentity);
                Check(ch.Identified,"Five clues identify the road spirit");
                await Press(Key.Tab); Check(session.Journal&&ch.JournalText.Contains("steering wheel"),"Journal concludes an object anchor"); await Press(Key.Tab);
                // Scripted drive to the salvage yard.
                await Walk(12,66); await Walk(0,58); await Walk(-4,12); await Walk(-4.4f,7); await Use(HuntItem.WinslowDrive);
                Check(session.player.transform.position.z>100,"Impala interior scare ends at the salvage yard");
                await Walk(3.3f,121); await Walk(3.3f,122); await Use(HuntItem.SteeringWheel);
                Check(ch.AnchorSecured&&ch.Spirit.Manifestations[1].Active,"Anchor secured; the spirit comes for it");
                await Walk(0,122.6f); await FaceYaw(0);
                await BurnAnchor();
                Check(session.Resolved&&ch.Spirit.ActiveCount==0,"Steering wheel burned by holding E with the lighter");
                // Motel: voicemail, decision, exit.
                await Walk(-3,110); await Walk(-5.4f,106); await Use(HuntItem.WinslowReturn);
                await Walk(-14,72.8f); await Use(HuntItem.JohnVoicemail);
                Check(ch.JournalText.Contains("Jericho")&&ch.JournalText.Contains("Sam"),"Voicemail EVP links Jericho; Dean decides on Sam");
                await Walk(-12,68); await Walk(-9.4f,63.2f); await Use(HuntItem.WinslowExit);
                Check(session.Completed&&ch.TransitionReady,"Start-to-finish input-only chapter completed");
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
                Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/WinslowWalkthrough.txt",Result);
            }
        }
    }
}
