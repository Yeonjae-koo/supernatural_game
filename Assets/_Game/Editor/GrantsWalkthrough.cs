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
    /// <summary>Input-only CH04 route validation. Reads state for assertions; never teleports or applies combat/progression directly.</summary>
    public static class GrantsWalkthrough
    {
        public static string Result="Not run";
        public static string Stage="Idle";
        static Keyboard keyboard; static Mouse mouse; static HuntSession session; static HunterCombat combat;
        static StringBuilder report; static bool cancelled; static float dodgeUntil; static int dodgeSide=1;
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
                var keys=new KeyboardState(); Steer(ref keys,delta); Queue(keys);
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
            foreach(var m in session.grants.Poltergeist.Manifestations)
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
            if(session.player.IsCrawling || !session.grants.HasKit) return;
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
            if(collected) Check(session.grants.Used(kind),"E collected/activated "+kind);
        }
        static async Task FaceYaw(float target)
        {
            for(int turn=0;turn<14 && Mathf.Abs(Mathf.DeltaAngle(session.player.view.Yaw,target))>2;turn++)
            { float angle=Mathf.DeltaAngle(session.player.view.Yaw,target); Queue(new KeyboardState(),new Vector2(Mathf.Clamp(angle*.7f,-35,35)/session.player.view.sensitivity,0)); await Task.Delay(70); }
            Queue(new KeyboardState()); await Task.Delay(80);
        }
        static async Task PourSaltSouth()
        {
            int before=session.grants.Poltergeist.SaltCharges;
            await FaceYaw(180); await Press(Key.Digit4);
            Queue(new KeyboardState(),default,false,true); await Task.Delay(120); Queue(new KeyboardState()); await Task.Delay(150);
            Check(session.grants.Poltergeist.SaltCharges==before-1,"LMB poured a salt line toward the utility room");
            await Press(Key.Digit3); await FaceYaw(0);
        }
        // Hold E beside the barrel with the lighter; step aside from telegraphed throws and swing iron at close shapes.
        static async Task BurnAnchor()
        {
            Stage="Burn anchor"; float start=Time.realtimeSinceStartup; var root=session.grants.Poltergeist.transform; bool[] seen=new bool[2]; float[] since=new float[2];
            Vector3 anchorSpot=new Vector3(-10,0,94.6f); int hits=0; float lastHp=session.HP;
            while(!session.Resolved)
            {
                Live();
                if(Time.realtimeSinceStartup-start>90) throw new Exception("Burn timeout progress="+session.BurnProgress+" HP="+session.HP);
                if(session.HP<lastHp) hits++; lastHp=session.HP;
                if(Threat(2.8f)) { await FightNear(); continue; }
                bool salted=false; for(int i=1;i<=3;i++) salted|=root.Find("Salt line "+i).gameObject.activeSelf;
                if(!salted && session.grants.Poltergeist.SaltCharges>0) { await PourSaltSouth(); continue; }
                if(!combat.LighterOn) { await Press(Key.F); continue; }
                for(int i=0;i<2;i++)
                {
                    bool active=root.Find("Thrown object "+(i+1)).gameObject.activeSelf;
                    if(active && !seen[i]) since[i]=Time.realtimeSinceStartup; seen[i]=active;
                    // Throws lock their aim at launch (0.9 s after lifting): sidestep only after that, never back into the aimed spot.
                    if(active && Time.realtimeSinceStartup-since[i]>.9f && Time.realtimeSinceStartup>dodgeUntil) { dodgeUntil=Time.realtimeSinceStartup+.7f; dodgeSide=-dodgeSide; }
                }
                var keys=new KeyboardState(Key.E);
                if(Time.realtimeSinceStartup<dodgeUntil) keys.Set(dodgeSide>0?Key.D:Key.A,true);
                else
                {
                    Vector3 back=anchorSpot-session.player.transform.position; back.y=0;
                    if(back.magnitude>.45f) Steer(ref keys,back);
                }
                Queue(keys); await Task.Delay(60);
            }
            Queue(new KeyboardState()); await Task.Delay(100);
            report.AppendLine("BURN: "+(Time.realtimeSinceStartup-start).ToString("F1")+"s, hits taken="+hits+", HP="+session.HP);
        }
        [MenuItem("Before Jericho/QA/Input-only Grants Walkthrough")]
        public static async void Run()
        {
            if(Result=="Running") return;
            session=UnityEngine.Object.FindAnyObjectByType<HuntSession>();
            if(!EditorApplication.isPlaying || !session || !session.grants) { Result="Open CH04 and enter Play Mode first"; return; }
            Result="Running"; Stage="Setup"; report=new StringBuilder(); cancelled=false;
            string prior=session.saveSlot, slot="qa-grants-walk-"+Guid.NewGuid().ToString("N");
            bool oldRun=Application.runInBackground; var oldBg=InputSystem.settings.backgroundBehavior; var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
            try
            {
                Application.runInBackground=true; InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                // Physical keyboard/mouse input would steer Dean during an unattended run.
                foreach(var device in InputSystem.devices) if((device is Keyboard || device is Mouse) && device.enabled) physical.Add(device);
                foreach(var device in physical) InputSystem.DisableDevice(device);
                keyboard=InputSystem.AddDevice<Keyboard>(); mouse=InputSystem.AddDevice<Mouse>(); EditorApplication.playModeStateChanged+=OnMode; InputSystem.onBeforeUpdate+=InputTick; Queue(new KeyboardState());
                session.saveSlot=slot; session.NewGame(); combat=session.player.GetComponent<HunterCombat>(); var ch=session.grants; await Task.Delay(250);
                // Parking lot: trunk kit.
                await Walk(-3,1); await Walk(-6,1); await Use(HuntItem.ImpalaTrunk);
                await Press(Key.Digit3); Check(combat.Weapon==2,"Key 3 equips the iron rod"); await Press(Key.Digit1);
                // Front office: ledger and blueprint.
                await Walk(-3,12); Check(ch.EventFired(0),"Arrival door slam fired on walking into the lot");
                await Walk(-8.5f,21); await Walk(-8.5f,27.3f); await Use(HuntItem.GuestLedger);
                await Walk(-11.5f,27.3f); await Use(HuntItem.RenovationBlueprint);
                await Walk(-13.6f,26.4f); await Use(HuntItem.LornaHayes,false);
                // Motel rooms: TV in 8, mirror and photo in 6, walkway lights.
                await Walk(-8,21); await Walk(-2,22); await Walk(4,34); await Walk(4,66.6f); await Walk(9,66.6f); Check(ch.EventFired(1),"Room 8 TV turned on as Dean entered");
                await Walk(4,66.6f); Check(ch.EventFired(2),"Walkway lights event fired on the walkway");
                await Walk(4,58.8f); await Walk(14.2f,58.8f); Check(ch.EventFired(3),"Room 6 mirror event fired"); await Use(HuntItem.OldMotelPhoto);
                // Impala radio, then the laundry note.
                await Walk(4,58.8f); await Walk(4,34); await Walk(-3,9); Check(ch.EventFired(4),"Impala radio activated after three clues");
                await Walk(-5,20); await Walk(-5,33); await Walk(-10,38); await Walk(-10,47.6f); await Walk(-13,47.6f); await Use(HuntItem.MaintenanceNote);
                Check(ch.Identified,"Four clues identify the poltergeist");
                await Press(Key.Tab); Check(session.Journal&&ch.JournalText.Contains("POLTERGEIST"),"Journal opens with the deduction"); await Press(Key.Tab);
                // Maintenance corridor: crouch, narrow squeeze, crawl.
                await Walk(-10,47.6f); await Walk(-10,54.3f); await Press(Key.C); Check(session.player.Stance==HunterStance.Crouching,"Crouch before the low duct");
                await Walk(-10,60); await Walk(-10,65.4f); Check(session.player.Stance==HunterStance.Crouching,"Crouched through the narrow squeeze");
                await Press(Key.Z); Check(session.player.IsCrawling,"Crawl before the wall cavity");
                await Walk(-10,73.4f); Check(session.player.IsCrawling,"Crawled through the wall cavity");
                await Press(Key.Z); Check(session.player.Stance==HunterStance.Standing,"Stood up beyond the crawl space");
                await Walk(-10,77); Check(ch.MidEncounter && ch.Poltergeist.ActiveCount==1,"First manifestation on entering the utility room");
                // First encounter: wait for the shape and scatter it with iron.
                float waited=0; while(!Threat(3.4f) && waited<12) { Live(); var m=ch.Poltergeist.Manifestations[0]; var keys=new KeyboardState(); if(!m.Dispersed){ Vector3 d=m.transform.position-session.player.transform.position; float yaw=Mathf.DeltaAngle(session.player.view.Yaw,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg); Queue(keys,new Vector2(Mathf.Clamp(yaw*.7f,-35,35)/session.player.view.sensitivity,0)); } await Task.Delay(100); waited+=.1f; }
                await FightNear(); Check(ch.Poltergeist.Manifestations[0].Dispersed,"First manifestation dispersed by iron input");
                // Wall puzzle: post 2.
                await Walk(-6,81.9f); await Use(HuntItem.WallCavity);
                Check(ch.Lockdown && ch.Poltergeist.ActiveCount==2,"Anchor taken; lockdown with two manifestations");
                await Walk(-10,90); await Walk(-10,94.6f); await FaceYaw(180);
                await PourSaltSouth();
                await BurnAnchor();
                Check(session.Resolved && ch.Poltergeist.ActiveCount==0,"Anchor burned by holding E with the lighter");
                // Return through the unlocked rear door to the office phone.
                await Walk(-10,93); await Walk(-18.5f,93); await Walk(-20.5f,88); await Walk(-20.5f,21); await Walk(-5,20); await Walk(-5,29.9f); await Use(HuntItem.JohnPhoneRecord);
                Check(ch.JournalText.Contains("JERICHO"),"Journal records Jericho");
                await Walk(-5,20); await Walk(-3.5f,6.2f); await Use(HuntItem.GrantsExit);
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
                Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/GrantsWalkthrough.txt",Result);
            }
        }
    }
}
