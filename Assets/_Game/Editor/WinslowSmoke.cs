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
    public static class WinslowSmoke
    {
        public static string Result="Not run";
        static StringBuilder report;
        static void Check(bool ok,string label) { if(!ok) throw new Exception(label); report.AppendLine("PASS: "+label); }
        static async Task Press(Keyboard k,Key key) { InputSystem.QueueStateEvent(k,new KeyboardState(key)); await Task.Delay(100); InputSystem.QueueStateEvent(k,new KeyboardState()); await Task.Delay(100); }
        public static async void Run()
        {
            if(!EditorApplication.isPlaying) { Result="Enter Play Mode"; return; }
            var s=UnityEngine.Object.FindAnyObjectByType<HuntSession>();
            if(!s || !s.winslow) { Result="Open Winslow first"; return; }
            if(Result=="Running") return;
            Result="Running"; report=new StringBuilder(); string prior=s.saveSlot,slot="qa-winslow-"+Guid.NewGuid().ToString("N");
            var oldBg=InputSystem.settings.backgroundBehavior; var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode; bool oldRun=Application.runInBackground;
            Keyboard k=null; Mouse m=null;
            try
            {
                Application.runInBackground=true; InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                k=InputSystem.AddDevice<Keyboard>(); m=InputSystem.AddDevice<Mouse>(); s.saveSlot=slot; s.NewGame(); await Task.Delay(200);
                var c=s.player.GetComponent<HunterCombat>(); var ch=s.winslow; var sp=ch.Spirit;
                HuntInteractable Item(HuntItem kind)=>Array.Find(s.items,x=>x.kind==kind);
                async Task Go(Vector3 point) { s.player.Teleport(point); await Task.Delay(150); }
                GameObject Scene(string name) { foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)) if(t.name==name) return t.gameObject; return null; }
                foreach(var kind in new[]{HuntItem.MaeTorres,HuntItem.HankDoyle,HuntItem.CrashMarks,HuntItem.PoliceReport,HuntItem.GarageRecord,HuntItem.DriverIdentity,HuntItem.CremationRecord,HuntItem.SteeringWheel,HuntItem.WinslowDrive,HuntItem.Anchor,HuntItem.WinslowReturn,HuntItem.JohnVoicemail,HuntItem.WinslowExit}) Check(Item(kind),"Scene binds "+kind);
                Check(sp.Manifestations.Length==2&&sp.ActiveCount==0,"One road spirit with two haunt points starts dormant");
                Check(HuntSave.TryLoad(slot,out var initial,WinslowChapter.ChapterId)&&HuntSave.Valid(initial),"CH05 initial snapshot validates in its isolated slot");
                Check(!s.Interact(Item(HuntItem.WinslowDrive))&&!s.Interact(Item(HuntItem.SteeringWheel))&&!s.Interact(Item(HuntItem.JohnVoicemail)),"Drive, anchor and voicemail refuse premature progression");
                var start=s.player.transform.position; InputSystem.QueueStateEvent(k,new KeyboardState(Key.W)); await Task.Delay(500); InputSystem.QueueStateEvent(k,new KeyboardState()); await Task.Delay(100);
                Check(Vector3.Distance(start,s.player.transform.position)>.7f,"WASD drives player movement");
                await Press(k,Key.Digit3); Check(c.Weapon==2,"Iron is available from the start"); await Press(k,Key.Digit4); Check(c.Weapon==3,"Salt is available from the start"); await Press(k,Key.Digit1);
                await Go(new Vector3(0,.25f,17)); Check(ch.EventFired(0),"Road silhouette event fires");
                Check(s.Interact(Item(HuntItem.CrashMarks)),"Collect CrashMarks"); int e=s.Evidence; Check(!s.Interact(Item(HuntItem.CrashMarks))&&s.Evidence==e,"Evidence is idempotent");
                await Go(new Vector3(0,.25f,32)); Check(ch.EventFired(1),"Crash hallucination fires after first clue");
                foreach(var kind in new[]{HuntItem.PoliceReport,HuntItem.CremationRecord}) Check(s.Interact(Item(kind)),"Collect "+kind);
                Check(s.Interact(Item(HuntItem.MaeTorres))&&ch.MaeTalked&&ch.JournalText.Contains("last night"),"Mae places John here the day before");
                await Go(new Vector3(-6,.25f,6)); Check(ch.EventFired(2),"Rear-view mirror apparition fires near the Impala");
                await Go(new Vector3(9,.25f,70)); Check(ch.MidEncounter&&sp.ActiveCount==1&&s.CheckpointName=="Closed motel","Closed motel manifests the road spirit");
                var spirit=sp.Manifestations[0]; float wait=0; while(spirit.Dispersed&&wait<3) { await Task.Delay(100); wait+=.1f; }
                spirit.Hit(false); Check(spirit.Active&&!spirit.Dispersed,"Bullets cannot harm the road spirit");
                spirit.Hit(true); Check(spirit.Dispersed&&spirit.Active,"Iron repels it without a permanent kill");
                foreach(var kind in new[]{HuntItem.GarageRecord,HuntItem.DriverIdentity}) Check(s.Interact(Item(kind)),"Collect "+kind);
                Check(ch.Identified&&ch.JournalText.Contains("ROAD SPIRIT"),"Five clues identify the road spirit and object anchor");
                s.Heal(); Check(s.Interact(Item(HuntItem.WinslowDrive))&&ch.Driven&&s.player.transform.position.z>100,"Scripted drive reaches the salvage yard");
                Check(!Scene("1967 Impala - Coyote Mile").activeSelf&&Scene("1967 Impala - salvage").activeSelf,"Impala follows Dean to the salvage yard");
                Check(sp.Manifestations[1].Active&&!sp.Manifestations[0].Active,"Only the salvage haunt is active after the drive");
                Check(!s.Interact(Item(HuntItem.WinslowReturn)),"Cannot leave before the burn");
                Check(s.Interact(Item(HuntItem.SteeringWheel))&&ch.AnchorSecured,"Steering wheel fragment secured as the anchor");
                s.Damage(100); Check(s.HP==0&&!s.Playing,"Death stops input"); s.RestoreCheckpoint(); await Task.Delay(100);
                Check(s.HP==100&&ch.AnchorSecured&&sp.ActiveCount==1&&sp.Manifestations[1].Active,"Retry restores the final hunt without duplicate spirits");
                foreach(var mm in sp.Manifestations) if(mm.Active) { while(mm.Dispersed) await Task.Delay(100); mm.Hit(true); }
                await Go(new Vector3(0,.25f,122.6f)); s.player.view.Face(0); await Press(k,Key.F);
                Check(c.Target&&c.Target.kind==HuntItem.Anchor&&c.LighterOn,"Burn barrel targetable with the lighter");
                float burned=0; InputSystem.QueueStateEvent(k,new KeyboardState(Key.E)); for(int i=0;i<12;i++) { await Task.Delay(100); burned=Mathf.Max(burned,s.BurnProgress); } InputSystem.QueueStateEvent(k,new KeyboardState()); await Task.Delay(100);
                Check(burned>.3f,"Holding E with the lighter burns the anchor");
                for(int i=0;i<30&&!s.Resolved;i++) { s.Heal(); s.AdvanceBurn(true,.5f); await Task.Delay(120); }
                Check(s.Resolved&&sp.ActiveCount==0,"Anchor burn lays the road spirit to rest");
                Check(!s.Interact(Item(HuntItem.JohnVoicemail)),"Voicemail waits for the motel");
                Check(s.Interact(Item(HuntItem.WinslowReturn))&&ch.Returned&&Scene("1967 Impala - motel").activeSelf,"Drive back to the motel");
                Check(s.Interact(Item(HuntItem.JohnVoicemail))&&ch.VoicemailHeard&&ch.JournalText.Contains("Jericho")&&ch.JournalText.Contains("Sam"),"Voicemail plays with EVP; Jericho and Sam decision recorded");
                s.ContinueGame(); Check(s.Resolved&&ch.VoicemailHeard&&sp.ActiveCount==0,"Durable reload keeps resolution and voicemail");
                Check(s.Interact(Item(HuntItem.WinslowExit))&&s.Completed,"Impala completes CH05");
                s.ContinueGame(); Check(s.Completed&&ch.TransitionReady,"Completed checkpoint restores the ending");
                Check(HuntSave.TryLoad(slot,out var done,WinslowChapter.ChapterId),"Completed save validates"); done.evidence=31; Check(!HuntSave.Valid(done),"Rejects resolved snapshot without the anchor");
                Check(!HuntSave.TryLoad(slot,out _,GrantsChapter.ChapterId),"CH05 save cannot load as CH04");
                Result="PASS\n"+report;
            }
            catch(Exception e) { Result="FAIL: "+e+"\n"+report; Debug.LogError(Result); }
            finally
            {
                if(k!=null) InputSystem.RemoveDevice(k); if(m!=null) InputSystem.RemoveDevice(m);
                Application.runInBackground=oldRun; InputSystem.settings.backgroundBehavior=oldBg; InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor; s.saveSlot=prior;
                foreach(string suffix in new[]{"",".bak",".tmp"}) { string path=HuntSave.PathFor(slot)+suffix; if(File.Exists(path)) File.Delete(path); }
                Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/WinslowSmoke.txt",Result);
            }
        }
    }
}
