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
    /// <summary>Input-only route validation. Reads state for assertions; never teleports or applies combat/progression directly.</summary>
    public static class EastTexasWalkthrough
    {
        public static string Result="Not run";
        public static string Stage="Idle";
        static Keyboard keyboard; static Mouse mouse; static HuntSession session; static HunterCombat combat;
        static StringBuilder report; static bool cancelled;
        static KeyboardState heldKeys; static Vector2 pendingLook; static bool heldAim, heldFire;
        static void Check(bool condition,string message) { if(!condition) throw new Exception(message); report.AppendLine("PASS: "+message); }
        static void Live() { if(cancelled || !EditorApplication.isPlaying || !session) throw new Exception("Play Mode ended or route cancelled"); if(!session.Playing) throw new Exception("Gameplay stopped at "+Stage+" HP="+session.HP+" "+session.Message); }
        static void Queue(KeyboardState keys,Vector2 look=default,bool aim=false,bool fire=false)
        { heldKeys=keys; pendingLook=look; heldAim=aim; heldFire=fire; }
        static void InputTick()
        {
            if(InputState.currentUpdateType!=InputUpdateType.Dynamic || keyboard==null || mouse==null) return;
            InputSystem.QueueStateEvent(keyboard,heldKeys);
            InputSystem.QueueStateEvent(mouse,new MouseState {delta=pendingLook}.WithButton(MouseButton.Right,heldAim).WithButton(MouseButton.Left,heldFire));
            pendingLook=Vector2.zero;
        }
        static async Task Press(Key key)
        { if(key!=Key.Tab && key!=Key.Escape) Live(); Queue(new KeyboardState(key)); await Task.Delay(110); Queue(new KeyboardState()); await Task.Delay(120); }
        static void OnMode(PlayModeStateChange mode) { if(mode==PlayModeStateChange.ExitingPlayMode) cancelled=true; }
        public static void Cancel() { cancelled=true; }
        static async Task Walk(float x,float z,float tolerance=.55f)
        {
            Stage="Walk "+x+","+z; float start=Time.realtimeSinceStartup; Vector3 previous=session.player.transform.position; float stuck=0;
            while(true)
            {
                Live(); await FightVisible();
                Vector3 delta=new Vector3(x,session.player.transform.position.y,z)-session.player.transform.position;
                if(delta.magnitude<tolerance) break;
                if(Time.realtimeSinceStartup-start>100) throw new Exception("Walk timeout "+Stage+" at "+session.player.transform.position);
                Vector3 local=Quaternion.Euler(0,-session.player.view.Yaw,0)*delta.normalized;
                var keys=new KeyboardState();
                if(local.z>.38f)keys.Set(Key.W,true); if(local.z<-.38f)keys.Set(Key.S,true);
                if(local.x>.38f)keys.Set(Key.D,true); if(local.x<-.38f)keys.Set(Key.A,true);
                Queue(keys);
                await Task.Delay(90);
                float moved=Vector3.Distance(previous,session.player.transform.position);
                stuck=moved<.004f?stuck+.09f:0; previous=session.player.transform.position;
                if(stuck>2.5f) throw new Exception("Blocked "+Stage+" at "+previous);
            }
            Queue(new KeyboardState()); await Task.Delay(100);
            report.AppendLine("WALK: "+x+","+z+" actual="+session.player.transform.position+" HP="+session.HP);
        }
        static GhoulHunt VisibleEnemy()
        {
            GhoulHunt best=null; float distance=45;
            foreach(var enemy in session.eastTexas.Director.Enemies)
            {
                if(!enemy.Active||enemy.Dead) continue;
                var head=Head(enemy); float d=Vector3.Distance(session.player.transform.position,enemy.transform.position);
                if(d>=distance || Physics.Linecast(session.player.transform.position+Vector3.up*1.55f,head.position,1,QueryTriggerInteraction.Ignore)) continue;
                best=enemy; distance=d;
            }
            return best;
        }
        static Transform Head(GhoulHunt enemy)
        { foreach(var hitbox in enemy.GetComponentsInChildren<GhoulHitbox>()) if(hitbox.head) return hitbox.transform; throw new Exception("No head hitbox: "+enemy.name); }
        static KeyboardState CombatMovement(GhoulHunt target)
        {
            Vector3 position=session.player.transform.position;
            Vector3 away=position-target.transform.position; away.y=0;
            float distance=away.magnitude;
            if(distance>7.5f) return new KeyboardState();
            away.Normalize();
            Vector3 side=Vector3.Cross(Vector3.up,away);
            // Avoid running into the arena partitions while circling a lunging enemy.
            Vector3 desired=away+side*.8f;
            foreach(Vector3 candidate in new[]{desired,away-side*.8f,side,-side,away})
            {
                Vector3 direction=candidate.normalized;
                if(Physics.SphereCast(position+Vector3.up*.8f,.38f,direction,out _,1.4f,1,QueryTriggerInteraction.Ignore)) continue;
                Vector3 local=Quaternion.Euler(0,-session.player.view.Yaw,0)*direction;
                var keys=new KeyboardState();
                if(local.z>.35f) keys.Set(Key.W,true); if(local.z<-.35f) keys.Set(Key.S,true);
                if(local.x>.35f) keys.Set(Key.D,true); if(local.x<-.35f) keys.Set(Key.A,true);
                return keys;
            }
            return new KeyboardState();
        }
        static async Task FightVisible()
        {
            if(session.player.IsCrawling) return;
            var enemy=VisibleEnemy(); if(!enemy) return;
            string previousStage=Stage; Stage="Combat "+enemy.name;
            float start=Time.realtimeSinceStartup, lastShot=-10, lastTrace=-10;
            Queue(new KeyboardState(),default,true); await Task.Delay(100);
            while(enemy && !enemy.Dead)
            {
                Live();
                if(Time.realtimeSinceStartup-start>55) throw new Exception("Combat timeout "+enemy.name+" HP="+enemy.HP+" ammo="+combat.Ammo+" player="+session.player.transform.position);
                // React to the closest threat; don't tunnel-vision the boss while its companion attacks.
                var nearest=VisibleEnemy();
                if(nearest && nearest!=enemy && Vector3.Distance(session.player.transform.position,nearest.transform.position)+1.5f<Vector3.Distance(session.player.transform.position,enemy.transform.position)) enemy=nearest;
                Stage="Combat "+enemy.name;
                float distance=Vector3.Distance(session.player.transform.position,enemy.transform.position);
                Vector3 direction=Head(enemy).position-session.player.view.lens.transform.position;
                Quaternion desired=Quaternion.LookRotation(direction);
                Vector3 current=session.player.view.lens.transform.eulerAngles;
                float yaw=Mathf.DeltaAngle(current.y,desired.eulerAngles.y),pitch=Mathf.DeltaAngle(current.x,desired.eulerAngles.x);
                Vector2 correction=new Vector2(Mathf.Clamp(yaw*.65f,-30,30),Mathf.Clamp(-pitch*.65f,-20,20))/(session.player.view.sensitivity*.65f);
                var keys=CombatMovement(enemy);
                bool knife=enemy.Staggered && distance<2.5f && Mathf.Abs(yaw)<20;
                if(knife) keys.Set(Key.Digit2,true); else if(combat.Weapon!=0) keys.Set(Key.Digit1,true);
                if(!knife && combat.Ammo==0 && !combat.Reloading)
                {
                    if(combat.Reserve==0) throw new Exception("Out of ammunition during input-only route");
                    keys.Set(Key.R,true);
                }
                // Mouse correction is applied once by InputTick; the next observation measures actual camera response.
                Queue(keys,correction,true); await Task.Delay(60);
                Ray ray=session.player.view.lens.ViewportPointToRay(new Vector3(.5f,.5f));
                bool hitActor=Physics.Raycast(ray,out var hit,70,~4,QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<GhoulHunt>()==enemy;
                bool hitHead=hitActor && hit.collider.GetComponent<GhoulHitbox>()?.head==true;
                bool fire=knife ? combat.Weapon==1 : combat.Weapon==0 && !combat.Reloading && combat.Ammo>0 && (hitHead || (hitActor && distance<5));
                float cooldown=knife ? .7f : .34f;
                if(fire && Time.realtimeSinceStartup-lastShot>cooldown)
                {
                    int before=enemy.HP; lastShot=Time.realtimeSinceStartup;
                    Queue(CombatMovement(enemy),default,true,true); await Task.Delay(65);
                    Queue(CombatMovement(enemy),default,true); await Task.Delay(45);
                    report.AppendLine((knife?"KNIFE: ":"SHOT: ")+enemy.Id+" hp "+before+" -> "+enemy.HP+" ammo="+combat.Ammo+" playerHP="+session.HP);
                }
                if(Time.realtimeSinceStartup-lastTrace>2)
                {
                    lastTrace=Time.realtimeSinceStartup;
                    report.AppendLine("TRACK: "+enemy.Id+" distance="+distance.ToString("F1")+" yawError="+yaw.ToString("F1")+" pitchError="+pitch.ToString("F1")+" player="+session.player.transform.position+" playerHP="+session.HP+" hit="+(hit.collider?hit.collider.name:"none"));
                }
            }
            Check(enemy.Dead,"Actual combat input killed "+enemy.name);
            Queue(new KeyboardState(Key.Digit1)); await Task.Delay(100); Queue(new KeyboardState()); Stage=previousStage;
            if(VisibleEnemy()) await FightVisible();
        }
        static async Task Use(HuntItem kind,bool collected=true)
        {
            Stage="Interact "+kind; Live(); Queue(new KeyboardState()); await Task.Delay(180);
            Check(combat.Target && combat.Target.kind==kind,"Interaction target is "+kind+" (actual "+(combat.Target?combat.Target.kind.ToString():"none")+")");
            await Press(Key.E);
            if(collected) Check(session.eastTexas.Used(kind),"E collected/activated "+kind);
        }
        [MenuItem("Before Jericho/QA/Input-only East Texas Walkthrough")]
        public static async void Run()
        {
            if(Result=="Running") return;
            session=UnityEngine.Object.FindAnyObjectByType<HuntSession>();
            if(!EditorApplication.isPlaying || !session || !session.eastTexas) { Result="Open CH03 and enter Play Mode first"; return; }
            Result="Running"; Stage="Setup"; report=new StringBuilder(); cancelled=false;
            string prior=session.saveSlot, slot="qa-east-texas-walk-"+Guid.NewGuid().ToString("N");
            bool oldRun=Application.runInBackground; var oldBg=InputSystem.settings.backgroundBehavior; var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
            try
            {
                Application.runInBackground=true; InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                keyboard=InputSystem.AddDevice<Keyboard>(); mouse=InputSystem.AddDevice<Mouse>(); EditorApplication.playModeStateChanged+=OnMode; InputSystem.onBeforeUpdate+=InputTick; Queue(new KeyboardState());
                session.saveSlot=slot; session.NewGame(); combat=session.player.GetComponent<HunterCombat>(); await Task.Delay(250);
                await Walk(-5,20); await Walk(-10,22); await Walk(-10,26.5f); await Use(HuntItem.MissingBodyParts);
                await Walk(-5.5f,26.5f); await Use(HuntItem.OldFuneralPhoto);
                await Walk(-5.5f,22); await Walk(0,22); await Walk(0,41.5f); await Use(HuntItem.EmptyGrave);
                await Walk(0,51); await FightVisible();
                await Walk(0,69); await Walk(-11,69); await Walk(-11,80.5f); await Use(HuntItem.StolenPersonalItem);
                await Walk(-11,69); await Walk(1,69); await Walk(1,77); await Walk(5,77); await Walk(5,88.5f);
                if(!combat.LighterOn) await Press(Key.F); await Use(HuntItem.FeedingArea);
                Check(session.eastTexas.Identified,"Five evidence clues identify Ghoul");
                await Press(Key.Tab); Check(session.Journal,"Journal opens from keyboard input"); await Press(Key.Tab);
                await Walk(5,77); await Walk(1,77); await Walk(1,69); await Walk(6,64.5f);
                Check(session.eastTexas.Director.NormalsCleared,"Four normal ghouls cleared through actual combat input"); await Use(HuntItem.GideonVale);
                Check(session.eastTexas.Revealed,"Gideon reveal through interaction input");
                await Walk(1,69); await Walk(1,95);
                if(combat.Ammo<7 && combat.Reserve>0) { await Press(Key.R); await Task.Delay(1600); }
                for(int turn=0;turn<12 && Mathf.Abs(Mathf.DeltaAngle(session.player.view.Yaw,0))>2;turn++)
                { float angle=Mathf.DeltaAngle(session.player.view.Yaw,0); Queue(new KeyboardState(),new Vector2(Mathf.Clamp(angle*.65f,-30,30)/session.player.view.sensitivity,0)); await Task.Delay(70); }
                Queue(new KeyboardState()); await Walk(0,101); await FightVisible();
                await Walk(9,111.5f); await FightVisible(); Check(session.Resolved,"Boss kill resolved the hunt"); await Use(HuntItem.JohnMap);
                await Walk(0,119.5f); await Press(Key.Z); Check(session.player.IsCrawling,"Crawl toggled before low service tunnel");
                await Walk(0,125); Check(session.player.IsCrawling,"Actual crawl movement traverses low tunnel roof");
                await Walk(0,131); await Press(Key.Z); Check(!session.player.IsCrawling,"Standing restored beyond tunnel roof");
                await Walk(0,131); await Use(HuntItem.TexasExit);
                Check(session.Completed&&session.eastTexas.TransitionReady,"Start-to-finish input-only chapter completed");
                Result="PASS\n"+report; Stage="Complete";
            }
            catch(Exception e) { Result="FAIL at "+Stage+": "+e+"\n"+report; Debug.LogError(Result); }
            finally
            {
                InputSystem.onBeforeUpdate-=InputTick; if(keyboard!=null) InputSystem.RemoveDevice(keyboard); if(mouse!=null) InputSystem.RemoveDevice(mouse); keyboard=null; mouse=null;
                EditorApplication.playModeStateChanged-=OnMode; Application.runInBackground=oldRun; InputSystem.settings.backgroundBehavior=oldBg; InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;
                if(session) session.saveSlot=prior;
                foreach(string suffix in new[]{"",".bak",".tmp"}) { string path=HuntSave.PathFor(slot)+suffix; if(File.Exists(path)) File.Delete(path); }
                Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/EastTexasWalkthrough.txt",Result);
            }
        }
    }
}
