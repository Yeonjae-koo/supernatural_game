using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace BeforeJericho.Editor
{
    /// <summary>Prologue → CH06 → credits → main menu through the real menu/end-card buttons and scene loads.
    /// Each chapter's middle is completed by its existing route runner; saves go to isolated QA slots.</summary>
    public static class FullGameIntegration
    {
        public static string Result="Not run", Stage="Idle";
        static StringBuilder report; static Keyboard keyboard; static readonly string run=Guid.NewGuid().ToString("N");
        static void Check(bool ok,string label) { if(!ok) throw new Exception(label); report.AppendLine("PASS: "+label); }
        static HuntSession Session() => UnityEngine.Object.FindAnyObjectByType<HuntSession>();
        static async Task Frames(int ms) { await Task.Delay(ms); }
        // Real pointer click on a uGUI button through InputSystemUIInputModule.
        static async Task Click(Button button,string label)
        {
            Check(button && button.gameObject.activeInHierarchy && button.interactable,"Button available: "+label);
            var rt=(RectTransform)button.transform; Vector2 p=RectTransformUtility.WorldToScreenPoint(null,rt.TransformPoint(rt.rect.center));
            // A fresh pointer per click: other route runners add/remove their own mice between clicks.
            var pointer=InputSystem.AddDevice<Mouse>();
            InputSystem.QueueStateEvent(pointer,new MouseState{position=p}); await Frames(200);
            InputSystem.QueueStateEvent(pointer,new MouseState{position=p}.WithButton(MouseButton.Left,true)); await Frames(200);
            InputSystem.QueueStateEvent(pointer,new MouseState{position=p}); await Frames(300);
            InputSystem.RemoveDevice(pointer);
            report.AppendLine("CLICK: "+label+" at "+p);
        }
        static async Task<HuntSession> Load(string scene)
        {
            Stage="Load "+scene; float t=0;
            while(t<20) { var s=Session(); if(SceneManager.GetActiveScene().name==scene && s && s.hud && s.hud.primary) { await Frames(400); return Session(); } await Frames(200); t+=.2f; }
            throw new Exception("Scene did not load: "+scene+" (active "+SceneManager.GetActiveScene().name+")");
        }
        static async Task StartChapter(HuntSession s,string subtitle)
        {
            s.saveSlot="qa-integration-"+run+"-"+SceneManager.GetActiveScene().name;
            Check(s.Menu&&s.hud.subtitle.text.Contains(subtitle),"Main menu shows "+subtitle);
            await Click(s.hud.primary,"NEW HUNT"); Check(!s.Menu&&s.Playing,"NEW HUNT starts gameplay in "+subtitle);
        }
        static async Task RunRoute(string name,Action start,Func<string> result)
        {
            Stage="Route "+name; start(); float t=0;
            await Frames(300);
            while(result()=="Running" && t<600) { await Frames(500); t+=.5f; }
            string r=result(); File.WriteAllText("Logs/FullGameIntegration-"+name+".txt",r);
            // PrologueSmoke reports "PASS: …" lines before a trailing "FAIL:", so a prefix check alone is not enough.
            Check(r.StartsWith("PASS")&&!r.Contains("FAIL"),name+" route completed ("+r.Split('\n')[0]+")");
        }
        // Prologue/CH02 route runners return to the main menu when done; resume their last checkpoint and use the real exit with E.
        static async Task ExitWithE(HuntSession s,HuntItem exit,string label)
        {
            Stage="Exit "+label; s.RestoreCheckpoint(); await Frames(300);
            report.AppendLine(label+" resumed at checkpoint: "+s.CheckpointName+" resolved="+s.Resolved);
            if(s.Completed) return;
            var item=Array.Find(s.items,x=>x.kind==exit); Vector3 d=s.player.transform.position-item.transform.position; d.y=0; d=d.sqrMagnitude>.01f?d.normalized:Vector3.back;
            Vector3 p=item.transform.position+d*1.2f; p.y=Physics.Raycast(p+Vector3.up*3,Vector3.down,out var hit,8,1,QueryTriggerInteraction.Ignore)?hit.point.y+.1f:s.player.transform.position.y;
            s.player.Teleport(p); await Frames(300);
            var c=s.player.GetComponent<HunterCombat>(); Check(c.Target&&c.Target.kind==exit,label+" exit is targeted (actual "+(c.Target?c.Target.kind.ToString():"none")+")");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E)); await Frames(150); InputSystem.QueueStateEvent(keyboard,new KeyboardState()); await Frames(300);
            Check(s.Completed,label+" exit with E completes the chapter ("+s.Message.Split('\n')[0]+")");
        }
        // Chapter-complete screen: Continue travels on; the "Next —" line names the destination.
        static async Task Advance(HuntSession s,string next,string label)
        {
            Stage="End card "+label; await Frames(300);
            Check(s.Completed&&s.hud.overlay.activeSelf,label+" end card shown");
            Check(s.hud.primaryLabel.text==UI.Loc.T("UI_Common/complete.continue")&&s.hud.completeNext.text.Contains(next),label+" end card offers "+next+" (actual "+s.hud.completeNext.text+")");
            await Click(s.hud.primary,"CONTINUE → "+next);
        }
        [MenuItem("Before Jericho/QA/Full Game Integration")]
        public static async void Run()
        {
            if(Result=="Running") return;
            if(!EditorApplication.isPlaying || SceneManager.GetActiveScene().name!="SCN_NewOrleans_Slice") { Result="Open SCN_NewOrleans_Slice and enter Play Mode"; return; }
            Result="Running"; report=new StringBuilder(); var physical=new System.Collections.Generic.List<InputDevice>();
            bool oldRun=Application.runInBackground; var oldBg=InputSystem.settings.backgroundBehavior; var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
            try
            {
                UI.GameSettings.UseTransientDefaults(true,"en"); // default options, English strings; the player's options are untouched
                Application.runInBackground=true; InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                foreach(var d in InputSystem.devices) if((d is Keyboard || d is Mouse) && d.enabled) physical.Add(d);
                foreach(var d in physical) InputSystem.DisableDevice(d);
                keyboard=InputSystem.AddDevice<Keyboard>();
                // Prologue.
                var s=await Load("SCN_NewOrleans_Slice"); await StartChapter(s,"PROLOGUE");
                await RunRoute("Prologue",PrologueSmoke.Run,()=>PrologueSmoke.Result);
                await ExitWithE(s,HuntItem.Exit,"Prologue");
                await Advance(s,"ATCHAFALAYA","Prologue");
                // CH02.
                s=await Load("SCN_Atchafalaya_Slice"); await StartChapter(s,"CHAPTER 02");
                await RunRoute("CH02",AtchafalayaSmoke.Run,()=>AtchafalayaSmoke.Result);
                await ExitWithE(s,HuntItem.ChapterExit,"CH02");
                await Advance(s,"EAST TEXAS","CH02");
                // CH03: weapon hotkeys through the real input path first.
                s=await Load("SCN_EastTexas_Slice"); await StartChapter(s,"CHAPTER 03");
                var c=s.player.GetComponent<HunterCombat>(); string keys="";
                foreach(var key in new[]{Key.Digit2,Key.Digit1,Key.Digit3,Key.Digit4})
                { InputSystem.QueueStateEvent(keyboard,new KeyboardState(key)); await Frames(150); InputSystem.QueueStateEvent(keyboard,new KeyboardState()); await Frames(150); keys+=key+"->"+c.Weapon+" \""+s.Message.Split('\n')[0]+"\"; "; if(key==Key.Digit3) Check(!s.Message.Contains("antique shop"),"CH03 key 3 does not show the Prologue iron-poker hint"); }
                report.AppendLine("CH03 HOTKEYS: "+keys);
                Check(keys.Contains("Digit2->1")&&keys.Contains("Digit1->0"),"CH03 keys 1/2 switch gun/knife");
                Check(keys.Contains("Digit3->0")&&keys.Contains("Digit4->0"),"CH03 keys 3/4 are refused (no iron or salt in this chapter)");
                await RunRoute("CH03",EastTexasWalkthrough.Run,()=>EastTexasWalkthrough.Result);
                await Advance(s,"NEW MEXICO","CH03");
                // CH04: death and Retry through the dead-screen button.
                s=await Load("SCN_GrantsNM_Slice"); await StartChapter(s,"CHAPTER 04");
                Stage="CH04 death/retry"; s.Damage(100); await Frames(300); Check(s.HP==0&&s.hud.primaryLabel.text==UI.Loc.T("UI_Common/dead.retry"),"Death shows the game-over screen with Continue (retry)");
                await Click(s.hud.primary,"RETRY CHECKPOINT"); Check(s.Playing&&s.HP==100,"Retry restores a playable checkpoint");
                await RunRoute("CH04",GrantsWalkthrough.Run,()=>GrantsWalkthrough.Result);
                await Advance(s,"ARIZONA","CH04");
                // CH05.
                s=await Load("SCN_Winslow_Slice"); await StartChapter(s,"CHAPTER 05");
                await RunRoute("CH05",WinslowWalkthrough.Run,()=>WinslowWalkthrough.Result);
                await Advance(s,"STANFORD","CH05");
                // CH06, credits, main menu, re-entry.
                s=await Load("SCN_Stanford_Slice"); await StartChapter(s,"CHAPTER 06");
                await RunRoute("CH06",StanfordWalkthrough.Run,()=>StanfordWalkthrough.Result);
                Stage="Credits"; Check(s.Completed&&s.hud.overlayTitle.text=="TO BE CONTINUED"&&s.hud.completeNext.text.Contains(UI.Loc.T("System/complete.ch06.next")),"TO BE CONTINUED card leads to the credits");
                await Click(s.hud.primary,"CONTINUE → CREDITS"); await Frames(300); Check(s.hud.Current==HuntHUD.Screen.Credits,"Credits roll");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape)); await Frames(150); InputSystem.QueueStateEvent(keyboard,new KeyboardState()); await Frames(300);
                Check(s.Menu&&s.hud.overlayTitle.text.Contains("SUPERNATURAL"),"Esc on the credits returns to the main menu");
                Check(s.CanContinue,"Main menu offers CONTINUE for the saved epilogue");
                await Click(s.hud.secondary,"CONTINUE"); Check(!s.Menu&&(s.Playing||s.Completed),"CONTINUE re-enters from the saved checkpoint ("+s.CheckpointName+")");
                Result="PASS\n"+report; Stage="Complete";
            }
            catch(Exception e) { Result="FAIL at "+Stage+": "+e.Message+"\n"+report; Debug.LogError(Result); }
            finally
            {
                if(keyboard!=null) InputSystem.RemoveDevice(keyboard); keyboard=null;
                foreach(var d in physical) if(d.added) InputSystem.EnableDevice(d);
                UI.GameSettings.UseTransientDefaults(false);
                Application.runInBackground=oldRun; InputSystem.settings.backgroundBehavior=oldBg; InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;
                foreach(var f in Directory.GetFiles(Application.persistentDataPath,"qa-integration-"+run+"*")) File.Delete(f);
                Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/FullGameIntegration.txt",Result);
            }
        }
    }
}
