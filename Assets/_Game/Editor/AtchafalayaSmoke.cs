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
    /// <summary>Input-driven CH02 integration smoke. Uses an isolated save slot and restores global input settings.</summary>
    public static class AtchafalayaSmoke
    {
        public static string Result = "Not run";
        static StringBuilder _report;

        static void Check(bool condition, string label)
        {
            if(!condition) throw new Exception(label);
            _report.AppendLine("PASS: " + label);
        }

        static void Keys(Keyboard keyboard, params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        }

        static void MouseButtons(Mouse mouse, bool aim, bool attack)
        {
            var state = new MouseState();
            state = state.WithButton(MouseButton.Right, aim).WithButton(MouseButton.Left, attack);
            InputSystem.QueueStateEvent(mouse, state);
        }

        static async Task Press(Keyboard keyboard, Key key)
        {
            Keys(keyboard, key); await Task.Delay(100);
            Keys(keyboard); await Task.Delay(100);
        }

        static async Task Move(Keyboard keyboard, int milliseconds, params Key[] keys)
        {
            Keys(keyboard, keys); await Task.Delay(milliseconds);
            Keys(keyboard); await Task.Delay(100);
        }

        static async Task InteractAt(HuntSession session, Keyboard keyboard, HuntInteractable item, Vector3 offset)
        {
            session.player.Teleport(item.transform.position + offset);
            session.player.view.Face(offset.z > 0 ? 180 : 0, 0);
            await Task.Delay(180);
            await Press(keyboard, Key.E);
        }

        static void RepositionPreservingStance(HunterMotor player, Vector3 point)
        {
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = point;
            controller.enabled = true;
        }

        static void DeleteQaSave(string slot)
        {
            foreach(string suffix in new[]{"", ".bak", ".tmp"})
            {
                string path = HuntSave.PathFor(slot) + suffix;
                try { if(File.Exists(path)) File.Delete(path); }
                catch(Exception e) { Debug.LogWarning("QA save cleanup failed: " + e.Message); }
            }
        }

        public static async void Run()
        {
            if(!EditorApplication.isPlaying) { Result = "Enter Play Mode first"; return; }

            var session = UnityEngine.Object.FindAnyObjectByType<HuntSession>();
            if(!session || !session.chapter) { Result = "Open SCN_Atchafalaya_Slice in Play Mode first"; return; }

            Result = "Running";
            _report = new StringBuilder();
            string previousSlot = session.saveSlot;
            string qaSlot = "qa-live-atchafalaya-" + Guid.NewGuid().ToString("N");
            bool oldRunInBackground = Application.runInBackground;
            var oldBackground = InputSystem.settings.backgroundBehavior;
            var oldEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            Keyboard keyboard = null;
            Mouse mouse = null;

            try
            {
                Application.runInBackground = true;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                keyboard = InputSystem.AddDevice<Keyboard>();
                mouse = InputSystem.AddDevice<Mouse>();
                session.saveSlot = qaSlot;
                session.NewGame();
                await Task.Delay(250);

                HunterCombat combat = session.player.GetComponent<HunterCombat>();
                AtchafalayaChapter chapter = session.chapter;
                RawheadHunt rawhead = chapter.Rawhead;
                HuntInteractable Item(HuntItem kind) => Array.Find(session.items, item => item.kind == kind);
                foreach(HuntItem kind in new[]{HuntItem.Mara, HuntItem.Eli, HuntItem.MudHandprints, HuntItem.HairTissue, HuntItem.MissingReport, HuntItem.ElectricalReport, HuntItem.CrawlTrace, HuntItem.IronDoor, HuntItem.FuelValve, HuntItem.MainBreaker, HuntItem.GeneratorStart, HuntItem.VoltageSwitch, HuntItem.JohnReceipt, HuntItem.ChapterExit})
                    Check(Item(kind), "Scene binds " + kind);

                Vector3 start = session.player.transform.position;
                await Move(keyboard, 500, Key.W);
                Check(Vector3.Distance(start, session.player.transform.position) > .7f, "Input System WASD moves the CharacterController");

                MouseButtons(mouse, true, false); await Task.Delay(180);
                Check(session.player.view.lens.fieldOfView < 60, "Aim input narrows the shoulder-camera field of view");
                MouseButtons(mouse, true, true); await Task.Delay(90);
                MouseButtons(mouse, true, false); await Task.Delay(100);
                Check(combat.Ammo == 6, "Aim and fire input spends exactly one round");
                await Press(keyboard, Key.R); await Task.Delay(1550);
                Check(combat.Ammo == 7 && combat.Reserve == 20, "Reload input transfers reserve ammunition");
                MouseButtons(mouse, false, false); await Task.Delay(100);

                Check(!chapter.TriggerFirstEncounter() && !chapter.ChaseStarted, "First encounter is gated until all critical evidence is found");
                await InteractAt(session, keyboard, Item(HuntItem.Mara), new Vector3(0, -.75f, -1.2f));
                Check(chapter.Phase == AtchafalayaPhase.Investigation, "Mara interaction starts the investigation");

                HuntInteractable firstEvidence = Item(HuntItem.MudHandprints);
                Check(session.Interact(firstEvidence), "Critical evidence can be collected");
                int afterFirstEvidence = session.Evidence;
                Check(!session.Interact(firstEvidence) && session.Evidence == afterFirstEvidence, "Evidence collection is idempotent");
                HuntInteractable hair = Item(HuntItem.HairTissue);
                Check(!session.Interact(hair), "Hair and tissue evidence requires the lighter");
                await Press(keyboard, Key.F);
                Check(combat.LighterOn && session.Interact(hair), "Lighter reveals the hair and tissue evidence");
                await Press(keyboard, Key.F);
                foreach(HuntItem kind in new[]{HuntItem.MissingReport, HuntItem.ElectricalReport})
                    Check(session.Interact(Item(kind)), "Collects " + kind);
                Check(session.Evidence == 15 && chapter.Identified, "Four critical leads identify Rawhead");

                session.player.Teleport(new Vector3(0, .1f, 57.8f));
                session.player.view.Face(0, 0);
                await Press(keyboard, Key.Z);
                Check(session.player.IsCrawling, "Z input enters crawl stance");
                await Move(keyboard, 2500, Key.W);
                Check(session.player.transform.position.z > 59.2f, "Actual crawl input enters the low pipe route");
                await Press(keyboard, Key.Z);
                Check(session.player.IsCrawling, "Real low ceiling blocks standing headroom");
                await Press(keyboard, Key.F);
                Check(combat.LighterOn, "Lighter input remains available while crawling");
                int crawlAmmo = combat.Ammo;
                MouseButtons(mouse, true, true); await Task.Delay(120);
                MouseButtons(mouse, false, false); await Task.Delay(120);
                Check(combat.Ammo == crawlAmmo && combat.LighterOn && session.player.view.lens.fieldOfView > 60, "Crawl blocks aim and attack without extinguishing the lighter");
                await Move(keyboard, 1800, Key.W);
                await Press(keyboard, Key.E);
                Check(chapter.ChaseStarted && rawhead.Active, "Crawl, lighter and actual interaction trigger the first encounter");

                rawhead.Hit(false);
                Check(!session.Resolved && rawhead.Active, "Ordinary damage staggers Rawhead without resolving the hunt");
                RepositionPreservingStance(session.player, new Vector3(0, .1f, 70.4f));
                Keys(keyboard, Key.W);
                for(int i = 0; i < 9; i++) { rawhead.Hit(false); await Task.Delay(300); }
                Keys(keyboard); await Task.Delay(100);
                Check(session.player.transform.position.z > 72.1f, "Actual movement crosses the iron-door threshold");
                await Move(keyboard, 250, Key.W, Key.LeftShift);
                Check(session.player.Stance == HunterStance.Standing, "Forward sprint leaves crawl when standing headroom is clear");
                await Press(keyboard, Key.E);
                Check(chapter.ChaseEscaped && !rawhead.Active, "Actual interaction closes the iron door from its safe side");

                Check(!session.Interact(Item(HuntItem.MainBreaker)) && chapter.PowerStep == 0, "Main breaker refuses out-of-order activation");
                Check(session.Interact(Item(HuntItem.FuelValve)) && chapter.PowerStep == 1, "Fuel valve is the first generator step");
                Check(!session.Interact(Item(HuntItem.GeneratorStart)) && chapter.PowerStep == 1, "Generator refuses to start before the breaker");
                Check(session.Interact(Item(HuntItem.MainBreaker)) && chapter.PowerStep == 2, "Main breaker is the second generator step");
                Check(session.Interact(Item(HuntItem.GeneratorStart)) && chapter.PowerStep == 3, "Generator start arms the water circuit");

                session.Damage(100);
                Check(session.HP == 0 && !session.Playing, "Fatal damage ends gameplay");
                session.RestoreCheckpoint(); await Task.Delay(150);
                Check(session.HP == 100 && chapter.PowerStep == 3 && session.Playing, "Checkpoint retry restores health and chapter progression");
                Check(rawhead.Active && !chapter.RawheadInWater, "Final Rawhead starts alive on dry ground");
                Check(!session.Interact(Item(HuntItem.VoltageSwitch)) && !session.Resolved, "Voltage switch cannot resolve Rawhead outside water");

                session.player.view.Face(180, 0);
                await Move(keyboard, 800, Key.W);
                float lureDeadline = Time.realtimeSinceStartup + 7;
                while(!chapter.RawheadInWater && Time.realtimeSinceStartup < lureDeadline) await Task.Delay(50);
                Check(chapter.RawheadInWater, "Player movement lures Rawhead into the marked water");
                Check(session.Interact(Item(HuntItem.VoltageSwitch)) && session.Resolved && rawhead.Resolved, "Powered water is the only successful Rawhead resolution");
                Check(HuntSave.TryLoad(qaSlot, out HuntSnapshot saved, AtchafalayaChapter.ChapterId) && saved.resolved, "Resolved chapter checkpoint is durably saved");
                session.RestoreCheckpoint();
                Check(session.Resolved && rawhead.Resolved && !rawhead.Active, "Durable resolution survives checkpoint restore");

                await InteractAt(session, keyboard, Item(HuntItem.JohnReceipt), new Vector3(0, -.9f, 1.2f));
                Check(chapter.BreadcrumbFound, "Actual interaction collects John's motel receipt");
                await InteractAt(session, keyboard, Item(HuntItem.ChapterExit), new Vector3(0, -.9f, 1.2f));
                Check(session.Completed && chapter.TransitionReady, "Actual exit interaction reaches the Texas transition");

                Result = _report.ToString();
            }
            catch(Exception e)
            {
                Result = _report + "FAIL: " + e;
                Debug.LogError("Atchafalaya smoke: " + e);
            }
            finally
            {
                if(keyboard != null)
                {
                    Keys(keyboard);
                    InputSystem.RemoveDevice(keyboard);
                }
                if(mouse != null)
                {
                    MouseButtons(mouse, false, false);
                    InputSystem.RemoveDevice(mouse);
                }
                InputSystem.settings.backgroundBehavior = oldBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInput;
                Application.runInBackground = oldRunInBackground;
                session.MainMenu();
                session.saveSlot = previousSlot;
                DeleteQaSave(qaSlot);
                try { File.WriteAllText(Path.Combine(Application.dataPath, "../Logs/AtchafalayaSmoke.txt"), Result); }
                catch(Exception e) { Debug.LogWarning("Atchafalaya smoke log write failed: " + e.Message); }
            }
        }
    }
}
