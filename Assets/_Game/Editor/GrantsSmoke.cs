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
    public static class GrantsSmoke
    {
        public static string Result="Not run";
        static StringBuilder report;
        static void Check(bool ok,string label) { if(!ok) throw new Exception(label); report.AppendLine("PASS: "+label); }
        static async Task Press(Keyboard k,Key key) { InputSystem.QueueStateEvent(k,new KeyboardState(key)); await Task.Delay(100); InputSystem.QueueStateEvent(k,new KeyboardState()); await Task.Delay(100); }
        static async Task Click(Mouse m) { InputSystem.QueueStateEvent(m,new MouseState().WithButton(MouseButton.Left,true)); await Task.Delay(100); InputSystem.QueueStateEvent(m,new MouseState()); await Task.Delay(100); }
        public static async void Run()
        {
            if(!EditorApplication.isPlaying) { Result="Enter Play Mode"; return; }
            var s=UnityEngine.Object.FindAnyObjectByType<HuntSession>();
            if(!s || !s.grants) { Result="Open Grants New Mexico first"; return; }
            if(Result=="Running") return;
            Result="Running"; report=new StringBuilder(); string prior=s.saveSlot,slot="qa-grants-"+Guid.NewGuid().ToString("N");
            var oldBg=InputSystem.settings.backgroundBehavior; var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode; bool oldRun=Application.runInBackground;
            Keyboard k=null; Mouse m=null;
            try
            {
                Application.runInBackground=true; InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                k=InputSystem.AddDevice<Keyboard>(); m=InputSystem.AddDevice<Mouse>(); s.saveSlot=slot; s.NewGame(); await Task.Delay(200);
                var c=s.player.GetComponent<HunterCombat>(); var ch=s.grants; var p=ch.Poltergeist;
                HuntInteractable Item(HuntItem kind)=>Array.Find(s.items,x=>x.kind==kind);
                async Task Go(Vector3 point) { s.player.Teleport(point); await Task.Delay(150); }
                GameObject Scene(string name) { foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)) if(t.name==name) return t.gameObject; return null; }
                foreach(var kind in new[]{HuntItem.LornaHayes,HuntItem.MotelGuest,HuntItem.ImpalaTrunk,HuntItem.GuestLedger,HuntItem.RenovationBlueprint,HuntItem.OldMotelPhoto,HuntItem.MaintenanceNote,HuntItem.WallCavity,HuntItem.WallDecoy,HuntItem.Anchor,HuntItem.JohnPhoneRecord,HuntItem.GrantsExit}) Check(Item(kind),"Scene binds "+kind);
                Check(p.Manifestations.Length==2 && p.ActiveCount==0,"Two bounded manifestations start dormant");
                Check(HuntSave.TryLoad(slot,out var initial,GrantsChapter.ChapterId)&&HuntSave.Valid(initial),"CH04 initial snapshot validates in its isolated slot");
                Check(!s.Interact(Item(HuntItem.JohnPhoneRecord))&&!s.Interact(Item(HuntItem.GrantsExit))&&!s.Interact(Item(HuntItem.WallCavity)),"Phone, exit and wall cavity refuse premature progression");
                var start=s.player.transform.position; InputSystem.QueueStateEvent(k,new KeyboardState(Key.W)); await Task.Delay(500); InputSystem.QueueStateEvent(k,new KeyboardState()); await Task.Delay(100);
                Check(Vector3.Distance(start,s.player.transform.position)>.7f,"WASD drives player movement");
                await Press(k,Key.Tab); start=s.player.transform.position; InputSystem.QueueStateEvent(k,new KeyboardState(Key.W)); await Task.Delay(250); InputSystem.QueueStateEvent(k,new KeyboardState());
                Check(s.Journal&&Vector3.Distance(start,s.player.transform.position)<.05f,"Journal suppresses movement"); await Press(k,Key.Tab);
                InputSystem.QueueStateEvent(m,new MouseState().WithButton(MouseButton.Right,true)); await Task.Delay(200);
                Check(s.player.view.lens.fieldOfView<60,"Aim narrows camera FOV"); InputSystem.QueueStateEvent(m,new MouseState().WithButton(MouseButton.Right,true).WithButton(MouseButton.Left,true)); await Task.Delay(100); InputSystem.QueueStateEvent(m,new MouseState()); await Task.Delay(100);
                Check(c.Ammo==6,"Fire consumes one round"); await Press(k,Key.R); await Task.Delay(1600); Check(c.Ammo==7&&c.Reserve==27,"Reload transfers reserve rounds");
                await Press(k,Key.Digit3); await Press(k,Key.Digit4); Check(c.Weapon==0,"Iron and salt require the trunk kit");
                await Go(new Vector3(0,.25f,15)); Check(ch.EventFired(0)&&Scene("Room 3 door - slammed").activeSelf,"Arrival door-slam event fires once in the lot");
                Check(s.Interact(Item(HuntItem.ImpalaTrunk))&&ch.HasKit&&!s.Interact(Item(HuntItem.ImpalaTrunk)),"Trunk kit is collected once");
                await Press(k,Key.Digit3); Check(c.Weapon==2,"Kit enables iron [3]"); await Press(k,Key.Digit4); Check(c.Weapon==3,"Kit enables salt [4]"); await Press(k,Key.Digit1);
                Check(s.Interact(Item(HuntItem.LornaHayes))&&s.Interact(Item(HuntItem.MotelGuest)),"Lorna and the guest speak");
                var gate=Scene("Maintenance door - opens with blueprint and kit"); Check(gate.activeSelf,"Maintenance door starts locked");
                foreach(var kind in new[]{HuntItem.GuestLedger,HuntItem.RenovationBlueprint}) { Check(s.Interact(Item(kind)),"Collect "+kind); int e=s.Evidence; Check(!s.Interact(Item(kind))&&s.Evidence==e,"Idempotent "+kind); }
                Check(!gate.activeSelf&&ch.CorridorOpen,"Blueprint plus kit opens the maintenance corridor");
                await Go(new Vector3(11,.25f,66)); Check(ch.EventFired(1)&&Scene("Room 8 TV static").activeSelf,"Room 8 TV turns itself on");
                await Go(new Vector3(-10,.25f,78)); Check(ch.MidEncounter&&p.ActiveCount==1&&s.CheckpointName=="Utility room","Entering the utility room starts the first manifestation");
                Check(!s.Interact(Item(HuntItem.WallDecoy)),"Wrong wall post refuses with a hint");
                Check(!s.Interact(Item(HuntItem.WallCavity))&&!ch.AnchorSecured,"Wall cavity requires the full deduction");
                var mf=p.Manifestations[0]; float wait=0; while(mf.Dispersed&&wait<3) { await Task.Delay(100); wait+=.1f; }
                Check(!mf.Dispersed,"Manifestation forms after its entry delay");
                mf.Hit(false); Check(mf.Active&&!mf.Dispersed,"Bullets and knives cannot harm a manifestation");
                mf.Hit(true); Check(mf.Active&&mf.Dispersed,"Iron disperses it without a permanent kill");
                await Press(k,Key.Digit4); await Click(m); Check(p.SaltCharges==2&&Scene("Salt line 1").activeSelf,"Salt input pours a bounded line");
                Vector3 line=Scene("Salt line 1").transform.position; Check(p.SaltBlocks(line+Vector3.forward*3,line+Vector3.forward*1.2f)&&!p.SaltBlocks(line+Vector3.forward*1.2f,line+Vector3.forward*3),"Salt blocks approach but not retreat");
                await Press(k,Key.Digit1);
                s.Heal(); await Go(new Vector3(11,.25f,58)); Check(ch.EventFired(3),"Room 6 mirror event fires after evidence");
                foreach(var kind in new[]{HuntItem.OldMotelPhoto,HuntItem.MaintenanceNote}) Check(s.Interact(Item(kind)),"Collect "+kind);
                Check(ch.Identified&&ch.JournalText.Contains("POLTERGEIST"),"Four clues identify the poltergeist in the journal");
                await Go(new Vector3(4,.25f,70)); await Task.Delay(2800); Check(ch.EventFired(2)&&!Scene("Walkway light 1").GetComponent<Light>().enabled,"Walkway lights go out in sequence");
                await Go(new Vector3(-6,.25f,8)); Check(ch.EventFired(4)&&Scene("Impala radio glow").activeSelf,"Impala radio activates remotely");
                s.Heal(); await Go(new Vector3(-6,.25f,82)); Check(s.Interact(Item(HuntItem.WallCavity))&&ch.AnchorSecured&&ch.Lockdown,"Correct wall post yields the anchor");
                Check(Scene("Lockdown door - corridor exit").activeSelf&&Scene("Lockdown door - rear exterior").activeSelf&&p.ActiveCount==2,"Lockdown seals the block and raises two manifestations");
                wait=0; while(!Scene("Thrown object 1").activeSelf&&!Scene("Thrown object 2").activeSelf&&wait<5) { await Task.Delay(100); wait+=.1f; }
                Check(wait<5,"Lockdown throws telegraphed objects");
                s.Heal(); s.SaveCheckpoint("Lockdown fight",new Vector3(-10,.25f,77));
                s.Damage(100); Check(s.HP==0&&!s.Playing,"Death stops input"); s.RestoreCheckpoint(); await Task.Delay(100);
                Check(s.HP==100&&ch.Lockdown&&p.ActiveCount==2&&p.SaltCharges==3&&!Scene("Salt line 1").activeSelf,"Retry restores lockdown without stale salt or duplicate manifestations");
                foreach(var mm in p.Manifestations) { while(mm.Dispersed) await Task.Delay(100); mm.Hit(true); }
                await Go(new Vector3(-10,.25f,94.7f)); s.player.view.Face(0); await Press(k,Key.F);
                Check(c.Target&&c.Target.kind==HuntItem.Anchor&&c.LighterOn,"Burn barrel targetable with lighter on");
                float burned=0; InputSystem.QueueStateEvent(k,new KeyboardState(Key.E)); for(int i=0;i<12;i++) { await Task.Delay(100); burned=Mathf.Max(burned,s.BurnProgress); } InputSystem.QueueStateEvent(k,new KeyboardState()); await Task.Delay(100);
                Check(burned>.3f&&!s.Resolved,"Holding E with the lighter burns the anchor");
                for(int i=0;i<30&&!s.Resolved;i++) { s.Heal(); s.AdvanceBurn(true,.5f); await Task.Delay(120); }
                Check(s.Resolved&&p.ActiveCount==0&&!ch.Lockdown,"Anchor burn resolves the poltergeist");
                Check(!Scene("Lockdown door - corridor exit").activeSelf&&!Scene("Lockdown door - rear exterior").activeSelf&&Scene("Walkway light 1").GetComponent<Light>().enabled&&!Scene("Room 8 TV static").activeSelf,"Resolution unlocks doors, restores lights and silences the TV");
                Check(s.Interact(Item(HuntItem.JohnPhoneRecord))&&ch.PhoneFound&&ch.JournalText.Contains("JERICHO"),"Phone record records Jericho in the journal");
                s.ContinueGame(); Check(s.Resolved&&ch.PhoneFound&&p.ActiveCount==0,"Durable reload keeps the hunt resolved and the breadcrumb");
                Check(s.Interact(Item(HuntItem.GrantsExit))&&s.Completed,"Impala completes CH04");
                s.ContinueGame(); Check(s.Completed&&ch.TransitionReady,"Completed checkpoint restores the ending");
                Check(HuntSave.TryLoad(slot,out var done,GrantsChapter.ChapterId),"Completed save validates"); done.evidence=15; Check(!HuntSave.Valid(done),"Rejects resolved snapshot without the anchor");
                Check(!HuntSave.TryLoad(slot,out _,EastTexasChapter.ChapterId),"CH04 save cannot load as CH03");
                Result="PASS\n"+report;
            }
            catch(Exception e) { Result="FAIL: "+e+"\n"+report; Debug.LogError(Result); }
            finally
            {
                if(k!=null) InputSystem.RemoveDevice(k); if(m!=null) InputSystem.RemoveDevice(m);
                Application.runInBackground=oldRun; InputSystem.settings.backgroundBehavior=oldBg; InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor; s.saveSlot=prior;
                foreach(string suffix in new[]{"",".bak",".tmp"}) { string path=HuntSave.PathFor(slot)+suffix; if(File.Exists(path)) File.Delete(path); }
                Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/GrantsSmoke.txt",Result);
            }
        }
    }
}
