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
    public static class EastTexasSmoke
    {
        public static string Result="Not run";
        static StringBuilder report;
        static void Check(bool ok,string label) { if(!ok) throw new Exception(label); report.AppendLine("PASS: "+label); }
        static async Task Press(Keyboard k,Key key) { InputSystem.QueueStateEvent(k,new KeyboardState(key)); await Task.Delay(100); InputSystem.QueueStateEvent(k,new KeyboardState()); await Task.Delay(100); }
        public static async void Run()
        {
            if(!EditorApplication.isPlaying) { Result="Enter Play Mode"; return; }
            var s=UnityEngine.Object.FindAnyObjectByType<HuntSession>();
            if(!s || !s.eastTexas) { Result="Open East Texas first"; return; }
            if(Result=="Running") return;
            Result="Running"; report=new StringBuilder(); string prior=s.saveSlot,slot="qa-east-texas-"+Guid.NewGuid().ToString("N");
            var oldBg=InputSystem.settings.backgroundBehavior; var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode; bool oldRun=Application.runInBackground;
            Keyboard k=null; Mouse m=null;
            try
            {
                Application.runInBackground=true; InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                k=InputSystem.AddDevice<Keyboard>(); m=InputSystem.AddDevice<Mouse>(); s.saveSlot=slot; s.NewGame(); await Task.Delay(200);
                var c=s.player.GetComponent<HunterCombat>(); var ch=s.eastTexas; var d=ch.Director;
                HuntInteractable Item(HuntItem kind)=>Array.Find(s.items,x=>x.kind==kind);
                foreach(var kind in new[]{HuntItem.SheriffReddick,HuntItem.NoraBell,HuntItem.GideonVale,HuntItem.EmptyGrave,HuntItem.MissingBodyParts,HuntItem.OldFuneralPhoto,HuntItem.StolenPersonalItem,HuntItem.FeedingArea,HuntItem.JohnMap,HuntItem.TexasExit,HuntItem.TexasAmmo}) Check(Item(kind),"Scene binds "+kind);
                Check(d.Enemies.Length==6 && d.Enemies[4].Boss,"Five ordinary ghouls and one boss are bound");
                Check(HuntSave.TryLoad(slot,out var initial,EastTexasChapter.ChapterId)&&HuntSave.Valid(initial),"CH03 initial snapshot validates in its isolated chapter slot");
                Check(!s.Interact(Item(HuntItem.GideonVale))&&!ch.Revealed,"Gideon refuses reveal before evidence");
                Check(!s.Interact(Item(HuntItem.JohnMap))&&!s.Interact(Item(HuntItem.TexasExit)),"Map and exit refuse premature progression");
                var start=s.player.transform.position; InputSystem.QueueStateEvent(k,new KeyboardState(Key.W)); await Task.Delay(500); InputSystem.QueueStateEvent(k,new KeyboardState()); await Task.Delay(100);
                Check(Vector3.Distance(start,s.player.transform.position)>.7f,"WASD drives player movement");
                await Press(k,Key.Tab); start=s.player.transform.position; InputSystem.QueueStateEvent(k,new KeyboardState(Key.W)); await Task.Delay(250); InputSystem.QueueStateEvent(k,new KeyboardState());
                Check(s.Journal&&Vector3.Distance(start,s.player.transform.position)<.05f,"Journal suppresses movement"); await Press(k,Key.Tab);
                InputSystem.QueueStateEvent(m,new MouseState().WithButton(MouseButton.Right,true)); await Task.Delay(200);
                Check(s.player.view.lens.fieldOfView<60,"Aim narrows camera FOV"); InputSystem.QueueStateEvent(m,new MouseState().WithButton(MouseButton.Right,true).WithButton(MouseButton.Left,true)); await Task.Delay(100); InputSystem.QueueStateEvent(m,new MouseState()); await Task.Delay(100);
                Check(c.Ammo==6,"Fire consumes one round"); await Press(k,Key.R); await Task.Delay(1600); Check(c.Ammo==7&&c.Reserve==48,"Reload transfers reserve rounds");
                await Press(k,Key.Z); Check(s.player.IsCrawling,"Crawl input works in CH03"); await Press(k,Key.F); Check(c.LighterOn,"Crawl allows lighter"); await Press(k,Key.Z);
                foreach(var kind in new[]{HuntItem.EmptyGrave,HuntItem.MissingBodyParts,HuntItem.OldFuneralPhoto,HuntItem.StolenPersonalItem,HuntItem.FeedingArea}) { Check(s.Interact(Item(kind)),"Collect "+kind); int e=s.Evidence; Check(!s.Interact(Item(kind))&&s.Evidence==e,"Idempotent "+kind); }
                Check(ch.Identified&&ch.JournalText.Contains("100%")&&ch.JournalText.Contains("GHOUL"),"Five clues identify Ghoul in journal");
                Check(!s.Interact(Item(HuntItem.GideonVale)),"Living normal ghouls gate boss reveal");
                var g=d.Enemies[0]; g.Activate(); int hp=g.HP; g.Hit(false,false); Check(g.HP==hp-16&&!g.Staggered,"Body shot is effective but inefficient"); g.Hit(true,false); Check(g.HP==hp-76&&g.Staggered,"Head damage multiplier and stagger apply"); g.Hit(false,true); Check(g.Dead&&!g.Active,"Stagger permits knife finish");
                for(int i=1;i<4;i++) { g=d.Enemies[i]; g.Activate(); g.Hit(true,false); g.Hit(false,true); }
                Check(d.NormalsCleared,"All four investigation ghouls can be cleared");
                Check(s.Interact(Item(HuntItem.TexasAmmo)),"Supply pickup grants ammunition and healing"); int reserve=c.Reserve; Check(!s.Interact(Item(HuntItem.TexasAmmo))&&c.Reserve==reserve,"Supplies cannot duplicate");
                Check(s.Interact(Item(HuntItem.GideonVale))&&ch.Revealed&&d.ActiveCount==0,"Gideon reveal arms basement encounter without a distant attack");
                s.player.Teleport(new Vector3(0,.27f,101)); await Task.Delay(200);
                Check(d.ActiveCount==2,"Entering basement activates boss and one companion");
                s.SaveCheckpoint("Basement combat",s.player.transform.position);
                Check(HuntSave.TryLoad(slot,out var revealed,EastTexasChapter.ChapterId)&&revealed.ghouls[0].hp==0&&revealed.ghouls[4].active,"Checkpoint serializes dead and active enemies");
                d.Enemies[4].Hit(true,false); s.Damage(100); Check(s.HP==0&&!s.Playing,"Death stops input"); s.RestoreCheckpoint(); await Task.Delay(100);
                Check(s.HP==100&&d.Enemies[4].HP==240&&d.Enemies[4].Active&&d.Enemies[0].Dead,"Retry restores full boss snapshot without reviving cleared enemies");
                var boss=d.Enemies[4]; boss.Hit(true,false); boss.Hit(false,true); Check(boss.HP==50&&!s.Resolved,"Boss survives one headshot and knife finish"); boss.Hit(true,false); Check(boss.Dead&&s.Resolved,"Boss can be killed by final headshot");
                Check(!s.Interact(Item(HuntItem.JohnMap)),"Remaining companion gates map pickup"); g=d.Enemies[5]; g.Hit(true,false); g.Hit(false,true); Check(g.Dead&&d.ActiveCount==0,"Companion clears without summons");
                Check(s.Interact(Item(HuntItem.JohnMap))&&ch.BreadcrumbFound,"John's New Mexico map collected");
                s.ContinueGame(); Check(s.Resolved&&ch.BreadcrumbFound&&d.ActiveCount==0&&boss.Dead,"Durable reload keeps boss dead and breadcrumb");
                Check(s.Interact(Item(HuntItem.TexasExit))&&s.Completed,"Impala completes CH03 with New Mexico preview");
                s.ContinueGame(); Check(s.Completed&&ch.TransitionReady,"Completed checkpoint restores completion overlay");
                Check(HuntSave.TryLoad(slot,out var completed,EastTexasChapter.ChapterId),"Completed save validates"); completed.ghouls[4].hp=240; Check(!HuntSave.Valid(completed),"Rejects resolved snapshot with living boss");
                Check(!HuntSave.TryLoad(slot,out _,AtchafalayaChapter.ChapterId),"CH03 save cannot load as CH02");
                Result="PASS\n"+report;
            }
            catch(Exception e) { Result="FAIL: "+e+"\n"+report; Debug.LogError(Result); }
            finally
            {
                if(k!=null) InputSystem.RemoveDevice(k); if(m!=null) InputSystem.RemoveDevice(m);
                Application.runInBackground=oldRun; InputSystem.settings.backgroundBehavior=oldBg; InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor; s.saveSlot=prior;
                foreach(string suffix in new[]{"",".bak",".tmp"}) { string p=HuntSave.PathFor(slot)+suffix; if(File.Exists(p)) File.Delete(p); }
                Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/EastTexasSmoke.txt",Result);
            }
        }
    }
}
