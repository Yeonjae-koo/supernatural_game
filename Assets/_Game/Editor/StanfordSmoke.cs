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
    public static class StanfordSmoke
    {
        public static string Result="Not run";
        static StringBuilder report;
        static void Check(bool ok,string label) { if(!ok) throw new Exception(label); report.AppendLine("PASS: "+label); }
        public static async void Run()
        {
            if(!EditorApplication.isPlaying) { Result="Enter Play Mode"; return; }
            var s=UnityEngine.Object.FindAnyObjectByType<HuntSession>();
            if(!s || !s.stanford) { Result="Open Stanford first"; return; }
            if(Result=="Running") return;
            Result="Running"; report=new StringBuilder(); string prior=s.saveSlot,slot="qa-stanford-"+Guid.NewGuid().ToString("N");
            var oldBg=InputSystem.settings.backgroundBehavior; var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode; bool oldRun=Application.runInBackground;
            Keyboard k=null; Mouse m=null;
            try
            {
                Application.runInBackground=true; InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                k=InputSystem.AddDevice<Keyboard>(); m=InputSystem.AddDevice<Mouse>(); s.saveSlot=slot; s.NewGame(); await Task.Delay(200);
                var c=s.player.GetComponent<HunterCombat>(); var ch=s.stanford;
                HuntInteractable Item(HuntItem kind)=>Array.Find(s.items,x=>x.kind==kind);
                GameObject Scene(string name) { foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)) if(t.name==name) return t.gameObject; return null; }
                foreach(var kind in new[]{HuntItem.JohnNotes,HuntItem.JerichoClippings,HuntItem.RouteMap,HuntItem.VoicemailTape,HuntItem.CallJohn,HuntItem.HitTheRoad,HuntItem.StanfordTrunk,HuntItem.SamNeighbor}) Check(Item(kind),"Scene binds "+kind);
                Check(HuntSave.TryLoad(slot,out var initial,StanfordChapter.ChapterId)&&HuntSave.Valid(initial),"CH06 initial snapshot validates in its isolated slot");
                var start=s.player.transform.position; InputSystem.QueueStateEvent(k,new KeyboardState(Key.S)); await Task.Delay(400); InputSystem.QueueStateEvent(k,new KeyboardState()); await Task.Delay(100);
                Check(Vector3.Distance(start,s.player.transform.position)>.5f,"WASD drives player movement");
                InputSystem.QueueStateEvent(m,new MouseState().WithButton(MouseButton.Right,true)); await Task.Delay(200); InputSystem.QueueStateEvent(m,new MouseState().WithButton(MouseButton.Right,true).WithButton(MouseButton.Left,true)); await Task.Delay(100); InputSystem.QueueStateEvent(m,new MouseState()); await Task.Delay(100);
                Check(c.Ammo==7&&s.Message.Contains("No hunting"),"Combat is disabled in the epilogue");
                Check(!s.Interact(Item(HuntItem.CallJohn))&&!s.Interact(Item(HuntItem.HitTheRoad)),"Call and departure wait for the evidence");
                foreach(var kind in new[]{HuntItem.JohnNotes,HuntItem.JerichoClippings,HuntItem.RouteMap,HuntItem.VoicemailTape}) { Check(s.Interact(Item(kind)),"Inspect "+kind); int e=s.Evidence; Check(!s.Interact(Item(kind))&&s.Evidence==e,"Idempotent "+kind); }
                Check(ch.Converged&&ch.JournalText.Contains("JERICHO, CALIFORNIA")&&ch.JournalText.Contains("MISSING MEN")&&ch.JournalText.Contains("JOHN'S ROUTE"),"Evidence converges on Jericho");
                Check(s.Interact(Item(HuntItem.CallJohn))&&ch.Called&&s.Objective=="FIND SAM","No answer from John; objective FIND SAM");
                Check(s.Interact(Item(HuntItem.HitTheRoad))&&ch.InMontage,"Travel montage starts");
                start=s.player.transform.position; InputSystem.QueueStateEvent(k,new KeyboardState(Key.W)); await Task.Delay(400); InputSystem.QueueStateEvent(k,new KeyboardState());
                Check(Vector3.Distance(start,s.player.transform.position)<.05f,"Montage suppresses movement");
                float wait=0; while(ch.InMontage&&wait<15) { await Task.Delay(200); wait+=.2f; }
                Check(ch.Arrived&&s.player.transform.position.z>30&&s.CheckpointName=="Palo Alto","Montage ends with the Palo Alto arrival checkpoint");
                Check(Scene("Apartment entrance door - opens after the trunk").activeSelf,"Apartment door waits for the trunk beat");
                Check(s.Interact(Item(HuntItem.StanfordTrunk))&&!Scene("Apartment entrance door - opens after the trunk").activeSelf,"Trunk beat leaves the gun and opens the approach");
                Check(s.Interact(Item(HuntItem.SamNeighbor)),"Hallway neighbor speaks");
                s.Damage(100); Check(s.HP==0,"Death state still works"); s.RestoreCheckpoint(); await Task.Delay(100);
                Check(s.HP==100&&ch.TrunkDone&&ch.Arrived&&!s.Completed,"Retry restores the Palo Alto checkpoint");
                s.player.Teleport(new Vector3(8,.25f,71)); await Task.Delay(300);
                Check(ch.GameComplete&&s.Message.Contains("All right, Sammy"),"Sam's door triggers the final beat");
                wait=0; while(!s.Completed&&wait<5) { await Task.Delay(200); wait+=.2f; }
                Check(s.Completed,"Fade to the TO BE CONTINUED ending");
                Check(HuntSave.TryLoad(slot,out var done,StanfordChapter.ChapterId)&&(done.chapterFlags&8)!=0,"GameComplete flag saved");
                s.MainMenu(); Check(s.Menu,"Credits return to the main menu");
                s.ContinueGame(); Check(s.Completed&&ch.GameComplete,"Continue restores the completed ending");
                done.evidence=3; Check(!HuntSave.Valid(done),"Rejects a completed snapshot without the evidence");
                Check(!HuntSave.TryLoad(slot,out _,WinslowChapter.ChapterId),"CH06 save cannot load as CH05");
                Result="PASS\n"+report;
            }
            catch(Exception e) { Result="FAIL: "+e+"\n"+report; Debug.LogError(Result); }
            finally
            {
                if(k!=null) InputSystem.RemoveDevice(k); if(m!=null) InputSystem.RemoveDevice(m);
                Application.runInBackground=oldRun; InputSystem.settings.backgroundBehavior=oldBg; InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor; s.saveSlot=prior;
                foreach(string suffix in new[]{"",".bak",".tmp"}) { string path=HuntSave.PathFor(slot)+suffix; if(File.Exists(path)) File.Delete(path); }
                Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/StanfordSmoke.txt",Result);
            }
        }
    }
}
