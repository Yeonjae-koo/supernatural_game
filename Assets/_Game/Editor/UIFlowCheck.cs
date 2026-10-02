using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BeforeJericho.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace BeforeJericho.Editor
{
    /// <summary>UI flow QA: the 14 prototype screens through real keyboard/mouse input (virtual devices), ko/en and every
    /// display option applied immediately, and text clipping at 16:9 resolutions. Start in SCN_GrantsNM_Slice Play Mode;
    /// the ritual hold check loads SCN_NewOrleans_Slice. Uses transient default options and qa-uiflow-* saves.</summary>
    public static class UIFlowCheck
    {
        public static string Result = "Not run", Stage = "Idle";
        static StringBuilder _log; static Keyboard _kb; static Mouse _mouse; static KeyboardState _keys; static bool _rmb, _lmb; static Vector2 _pointer;
        static HuntSession S => UnityEngine.Object.FindAnyObjectByType<HuntSession>();
        static HuntHUD Hud => S.hud;
        static void Check(bool ok, string label) { if(!ok) throw new Exception(label); _log.AppendLine("PASS: " + label); }
        static T F<T>(object o, string field) => (T)o.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(o);
        static object View(string field) => typeof(HuntHUD).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Hud);
        static Task Wait(int ms) => Task.Delay(ms);

        static void Tick()
        {
            if(InputState.currentUpdateType != InputUpdateType.Dynamic || _kb == null) return;
            InputSystem.QueueStateEvent(_kb, _keys);
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = _pointer }.WithButton(MouseButton.Right, _rmb).WithButton(MouseButton.Left, _lmb));
        }
        static async Task Press(Key key, int hold = 110) { _keys = new KeyboardState(key); await Wait(hold); _keys = new KeyboardState(); await Wait(160); }
        static async Task Click(Selectable b, string label)
        {
            Check(b && b.gameObject.activeInHierarchy && b.interactable, "Clickable: " + label);
            var rt = (RectTransform)b.transform; _pointer = RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center));
            await Wait(150); _lmb = true; await Wait(150); _lmb = false; await Wait(250);
        }
        static async Task Screen(HuntHUD.Screen expected, string label, int timeout = 3000)
        {
            for(int t = 0; t < timeout && Hud.Current != expected; t += 50) await Wait(50);
            Check(Hud.Current == expected, label + " → " + expected + " (actual " + Hud.Current + ")");
            await Wait(150); Clipping(label);
        }

        /// <summary>Every visible label stays inside the 16:9 canvas and no fixed box overflows.</summary>
        static void Clipping(string where)
        {
            var canvas = (RectTransform)Hud.transform; var corners = new Vector3[4]; int n = 0;
            foreach(var t in Hud.GetComponentsInChildren<TMP_Text>())
            {
                if(!t.isActiveAndEnabled || string.IsNullOrEmpty(t.text) || t.GetComponentInParent<DevOverlay>() || t.GetComponentInParent<CreditsView>()) continue; // the credits roll scrolls off-screen by design
                if(t.GetComponentInParent<CanvasGroup>() is CanvasGroup g && g.alpha <= 0) continue;
                t.ForceMeshUpdate(); if(t.textInfo.characterCount == 0) continue;
                var b = t.textBounds; Vector3 min = canvas.InverseTransformPoint(t.transform.TransformPoint(b.min)), max = canvas.InverseTransformPoint(t.transform.TransformPoint(b.max));
                var r = canvas.rect;
                bool inside = min.x >= r.xMin - 1 && max.x <= r.xMax + 1 && min.y >= r.yMin - 1 && max.y <= r.yMax + 1;
                bool fits = t.overflowMode == TextOverflowModes.Overflow || t.overflowMode == TextOverflowModes.Linked || !t.isTextOverflowing;
                if(!inside || !fits) throw new Exception("Clipped text at " + where + ": " + t.name + " \"" + t.text.Substring(0, Math.Min(40, t.text.Length)) + "\" " + min + ".." + max);
                n++;
            }
            _log.AppendLine("   no clipping (" + n + " labels) at " + where + " [" + Loc.Korean switch { true => "ko", _ => "en" } + "]");
        }

        public static string SweepResult = "Not run";
        /// <summary>All six chapters × ko/en: menu, card, HUD, pause, every journal tab, dead, complete — clipping + untranslated runtime strings.</summary>
        [MenuItem("Before Jericho/QA/UI Chapter Sweep")]
        public static async void Sweep()
        {
            if(SweepResult == "Running" || !EditorApplication.isPlaying) return;
            SweepResult = "Running"; _log = new StringBuilder(); string run = Guid.NewGuid().ToString("N");
            try
            {
                GameSettings.UseTransientDefaults(true, "ko");
                foreach(string scene in new[] { "SCN_NewOrleans_Slice", "SCN_Atchafalaya_Slice", "SCN_EastTexas_Slice", "SCN_GrantsNM_Slice", "SCN_Winslow_Slice", "SCN_Stanford_Slice" })
                {
                    Stage = scene; var previous = S; SceneManager.LoadScene(scene);
                    for(int t = 0; t < 100 && (SceneManager.GetActiveScene().name != scene || !S || S == previous || !S.hud || S.hud.Current == HuntHUD.Screen.None); t++) await Wait(100);
                    var s = S; s.saveSlot = "qa-sweep-" + run; var journal = (JournalView)View("_journal");
                    // Every runtime English string the chapter can show at start must have a String Table row.
                    foreach(var i in s.items) { if(!string.IsNullOrEmpty(i.label)) Check(Loc.KeyOf(i.label) != null, scene + " prompt row: " + i.label); if(!string.IsNullOrEmpty(i.description)) Check(Loc.KeyOf(i.description) != null || i.description.Split('\n').Length > 1, scene + " description row: " + i.kind); }
                    foreach(string lang in new[] { "ko", "en" })
                    {
                        GameSettings.Set("lang", lang); s.MainMenu(); await Screen(HuntHUD.Screen.Menu, scene + " menu");
                        Hud.StartNewGame(); string intro = s.Message; await Screen(HuntHUD.Screen.Play, scene + " card"); Check(Loc.KeyOf(s.Objective) != null, scene + " objective row: " + s.Objective);
                        Check(Loc.KeyOf(intro) != null, scene + " intro row: [" + intro + "]");
                        await Wait(5400); Clipping(scene + " HUD");
                        s.SetPaused(true); await Screen(HuntHUD.Screen.Pause, scene + " pause");
                        Hud.OpenJournal(false); await Screen(HuntHUD.Screen.Journal, scene + " journal");
                        for(int t = 0; t < 6; t++) { journal.Select((JournalView.Tab)t); await Wait(150); Clipping(scene + " journal " + (JournalView.Tab)t); }
                        s.Resume(); s.Damage(100); await Screen(HuntHUD.Screen.Dead, scene + " dead"); Check(Loc.Has("System/dead.hint." + Hud.ChapterId), scene + " dead hint row");
                        s.RestoreCheckpoint(); s.CompleteChapter(); await Screen(HuntHUD.Screen.Complete, scene + " complete");
                    }
                }
                SweepResult = "PASS\n" + _log;
            }
            catch(Exception e) { SweepResult = "FAIL at " + Stage + ": " + e.Message + "\n" + _log; Debug.LogError(SweepResult); }
            finally
            {
                GameSettings.UseTransientDefaults(false);
                foreach(var f in Directory.GetFiles(Application.persistentDataPath, "qa-sweep-" + run + "*")) File.Delete(f);
                File.WriteAllText("Logs/UIChapterSweep.txt", SweepResult);
            }
        }

        [MenuItem("Before Jericho/QA/UI Flow Check")]
        public static async void Run()
        {
            if(Result == "Running") return;
            if(!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "SCN_GrantsNM_Slice") { Result = "Open SCN_GrantsNM_Slice and enter Play Mode"; return; }
            Result = "Running"; _log = new StringBuilder(); var physical = new System.Collections.Generic.List<InputDevice>();
            bool oldRun = Application.runInBackground; var oldBg = InputSystem.settings.backgroundBehavior; var oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            string run = Guid.NewGuid().ToString("N");
            try
            {
                Application.runInBackground = true; InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                foreach(var d in InputSystem.devices) if((d is Keyboard || d is Mouse) && d.enabled) physical.Add(d);
                foreach(var d in physical) InputSystem.DisableDevice(d);
                _kb = InputSystem.AddDevice<Keyboard>(); _mouse = InputSystem.AddDevice<Mouse>(); _keys = new KeyboardState(); InputSystem.onBeforeUpdate += Tick;
                GameSettings.UseTransientDefaults(true, "ko");
                var s = S; s.saveSlot = "qa-uiflow-" + run; s.MainMenu();

                // 1 첫 실행 → 2 메뉴 (ko/en live)
                Stage = "Boot"; GameSettings.Set("firstRun", "0"); await Screen(HuntHUD.Screen.Boot, "First launch");
                var boot = View("_boot"); var bootRows = F<OptionRow[]>(boot, "_rows");
                string title = Hud.GetComponentInChildren<BootView>().GetComponentInChildren<TMP_Text>().text;
                await Click(F<Button>(bootRows[0], "_next"), "Boot language ▶"); Check(GameSettings.Choice("lang") == "en" && Hud.GetComponentInChildren<BootView>().GetComponentInChildren<TMP_Text>().text != title, "Boot: language switches ko→en immediately");
                await Click(F<Button>(bootRows[0], "_next"), "Boot language ▶"); Check(Loc.Korean, "Boot: back to ko");
                await Click(F<Button>(boot, "_confirm"), "Boot confirm"); await Screen(HuntHUD.Screen.Menu, "Boot confirm");
                var menu = (MenuView)View("_menu");
                string koNew = menu.NewGame.Label.text; GameSettings.Set("lang", "en"); await Wait(100);
                Check(menu.NewGame.Label.text == "NEW GAME" && koNew == "새 게임", "Menu labels re-render on ko→en (" + koNew + " → " + menu.NewGame.Label.text + ")"); Clipping("Menu");
                GameSettings.Set("lang", "ko"); await Wait(100);
                await Click(F<MenuButton>(menu, "_options").Button, "Menu Options"); await Screen(HuntHUD.Screen.Settings, "Menu › Options");
                await Press(Key.Escape); await Screen(HuntHUD.Screen.Menu, "Esc from Options");
                await Click(F<MenuButton>(menu, "_credits").Button, "Menu Credits"); await Screen(HuntHUD.Screen.Credits, "Menu › Credits");
                await Press(Key.Escape); await Screen(HuntHUD.Screen.Menu, "Esc from Credits");

                // 3 로케이션 카드 → 4 탐색 HUD
                Stage = "Card"; await Click(menu.NewGame.Button, "NEW GAME"); await Screen(HuntHUD.Screen.Play, "NEW GAME");
                Check(Hud.CardVisible, "Location card over play"); Clipping("Card");
                await Press(Key.Space); Check(!Hud.CardVisible, "Space skips the card");
                var hud = (HudView)View("_hud");
                Stage = "Explore"; var trunk = Array.Find(s.items, i => i.kind == HuntItem.ImpalaTrunk);
                Vector3 p = trunk.transform.position + (s.player.transform.position - trunk.transform.position).normalized * 1.1f; p.y = s.player.transform.position.y;
                s.player.Teleport(p); var dir = trunk.transform.position - p; s.player.view.Face(Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, 20); await Wait(400);
                var prompt = F<GameObject>(hud, "_prompt"); var promptKey = F<TMP_Text>(hud, "_promptKey");
                Check(prompt.activeSelf && promptKey.text == "E" && F<TMP_Text>(hud, "_promptText").text == Loc.Src(trunk.label), "Explore: [E] prompt near the trunk (" + F<TMP_Text>(hud, "_promptText").text + ")"); Clipping("Explore");
                GameSettings.Set("prompts", "off"); await Wait(100); Check(!prompt.activeSelf, "Option: interaction prompts off hides the prompt"); GameSettings.Set("prompts", "on"); await Wait(100);
                GameSettings.Set("contrast", "on"); await Wait(100); Check(F<Image>(hud, "_promptKeyBack").color == UIStyle.Bone, "Option: high-contrast key cap"); GameSettings.Set("contrast", "off");
                await Press(Key.E); Check(s.HasSpiritKit, "E interacts (Impala trunk → iron + salt)");

                // 5 전투: 1–4, 우클릭, Q, R, F, H
                Stage = "Combat"; var weapon = F<CanvasGroup>(hud, "_weapon"); var wName = F<TMP_Text>(hud, "_weaponName");
                string[] names = { "Items/weapon.1911", "Items/weapon.knife", "Items/weapon.iron", "Items/weapon.salt" };
                Key[] slots = { Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit1 };
                foreach(var k in slots) { await Press(k); int w = s.player.GetComponent<HunterCombat>().Weapon; Check(w == (k == Key.Digit1 ? 0 : (int)k - (int)Key.Digit1) && weapon.alpha > .9f && wName.text == Loc.T(names[w]), "Key " + k + " → " + wName.text + " (weapon HUD shown)"); }
                var dot = F<GameObject>(hud, "_dot"); var cross = F<GameObject>(hud, "_cross");
                _rmb = true; await Wait(300); Check(s.player.Input.Aim && dot.activeSelf && weapon.alpha > .9f, "Right mouse: aim shows dot reticle + weapon/ammo");
                GameSettings.Set("crosshair", "cross"); await Wait(100); Check(cross.activeSelf && !dot.activeSelf, "Option: reticle cross"); GameSettings.Set("crosshair", "off"); await Wait(100); Check(!cross.activeSelf && !dot.activeSelf, "Option: reticle off"); GameSettings.Set("crosshair", "dot");
                Clipping("Combat");
                _lmb = true; await Wait(120); _lmb = false; await Wait(200); var c = s.player.GetComponent<HunterCombat>(); Check(c.Ammo == 6, "Left mouse while aiming fires (ammo 6)");
                _rmb = false; await Wait(200);
                await Press(Key.R); Check(c.Reloading && F<TMP_Text>(hud, "_reloading").gameObject.activeSelf, "R reloads (Reloading label)");
                await Wait(1700); Check(!c.Reloading && c.Ammo == 7, "Reload completes");
                await Wait(4000); Check(weapon.alpha == 0, "HUD dynamic: weapon fades after aim/reload");
                GameSettings.Set("hud", "always"); await Wait(100); Check(weapon.alpha == 1 && F<CanvasGroup>(hud, "_health").alpha == 1, "Option: HUD always");
                GameSettings.Set("hudop", "40"); await Wait(100); Check(Mathf.Abs(F<CanvasGroup>(hud, "_layer").alpha - .4f) < .01f, "Option: HUD opacity 40%");
                GameSettings.Set("hud", "off"); _rmb = true; await Wait(250); Check(weapon.alpha == 0 && F<CanvasGroup>(hud, "_health").alpha == 0, "Option: HUD off hides weapon even while aiming"); _rmb = false;
                GameSettings.Set("hud", "dynamic"); GameSettings.Set("hudop", "100"); await Wait(200);
                await Press(Key.Q); Check(s.player.Input.Aim, "Q toggles aim on"); await Press(Key.Q); Check(!s.player.Input.Aim, "Q toggles aim off");
                GameSettings.Set("hold", "toggle"); _rmb = true; await Wait(150); _rmb = false; await Wait(150); Check(s.player.Input.Aim, "Option: aim input Toggle latches on RMB"); _rmb = true; await Wait(150); _rmb = false; await Wait(150); Check(!s.player.Input.Aim, "Toggle aim off"); GameSettings.Set("hold", "hold");
                await Press(Key.F); Check(c.LighterOn && F<GameObject>(hud, "_lighter").activeSelf, "F lighter on (HUD ✦ Lighter)"); await Press(Key.F); Check(!c.LighterOn, "F lighter off");
                await Press(Key.H); Check(F<Image>(hud, "_hurtFlash").enabled, "H (development) previews hit feedback");
                int hp = s.HP; s.Damage(10); await Wait(150); Check(F<CanvasGroup>(hud, "_health").alpha == 1 && s.HP == hp - 10, "Hit shows health (dynamic HUD)");
                GameSettings.Set("flash", "on"); s.Damage(1); await Wait(100); Check(!F<Image>(hud, "_hurtFlash").enabled, "Option: reduce flashing suppresses the hit flash"); GameSettings.Set("flash", "off");

                // 6 대사·자막 옵션 (즉시 반영)
                Stage = "Subtitles"; var subs = F<SubtitleView>(hud, "_subtitles");
                s.Notify("Mara: Sheriff says gators. Gators don't open bedroom windows.\nDean: Yeah. They usually skip the breaking-and-entering part.");
                typeof(HudView).GetMethod("Caption", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hud, new object[] { "Captions/door_slam", trunk.transform }); await Wait(200);
                var capText = F<TMP_Text>(subs, "_caption"); var speech = F<TMP_Text>(subs, "_speech");
                Check(speech.text.Contains("마라") && speech.text.Contains("#8aa3a6") && capText.text.Contains(Loc.T("Captions/door_slam")), "Dialogue subtitle: speaker tag + [caption] (ko)");
                var side = new GameObject("QA caption source").transform; var cam = s.player.view.lens.transform; side.position = cam.position + cam.right * 10;
                typeof(HudView).GetMethod("Caption", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hud, new object[] { "Captions/door_slam", side }); await Wait(200);
                Check(capText.text.Contains("▶"), "Off-screen caption to the right shows ▶");
                side.position = cam.position - cam.right * 10; await Wait(150); Check(capText.text.Contains("◀"), "Off-screen caption to the left shows ◀");
                GameSettings.Set("direction", "off"); await Wait(60); Check(!capText.text.Contains("◀") && !capText.text.Contains("▶"), "Option: direction off"); UnityEngine.Object.Destroy(side.gameObject);
                GameSettings.Set("subSize", "l"); await Wait(60); Check(Mathf.Approximately(speech.fontSize, 26.4f), "Option: subtitle size Large"); GameSettings.Set("subSize", "s"); await Wait(60); Check(Mathf.Approximately(speech.fontSize, 16.8f), "Option: subtitle size Small");
                GameSettings.Set("subBg", "off"); await Wait(60); Check(!F<Image>(subs, "_speechBack").enabled, "Option: subtitle background off");
                GameSettings.Set("speaker", "off"); await Wait(60); Check(!speech.text.Contains("#8aa3a6") && !speech.text.Contains("마라"), "Option: speaker names off");
                GameSettings.Set("captions", "off"); await Wait(60); Check(!capText.gameObject.transform.parent.gameObject.activeSelf, "Option: sound captions off");
                GameSettings.Set("lang", "en"); await Wait(60); Check(speech.text.Contains("Sheriff says gators"), "Subtitle re-renders in English immediately");
                GameSettings.Set("subs", "off"); await Wait(60); Check(!speech.transform.parent.gameObject.activeSelf, "Option: subtitles off");
                GameSettings.UseTransientDefaults(true, "ko"); await Wait(100);
                await Press(Key.Space); Check(!speech.transform.parent.gameObject.activeSelf, "Space dismisses the current subtitle");

                // 12 저널 / 13 지도 / 조사
                Stage = "Journal"; var journal = (JournalView)View("_journal");
                await Press(Key.Tab); await Screen(HuntHUD.Screen.Journal, "Tab");
                await Press(Key.E); Check(journal.Current == JournalView.Tab.Clues, "E → next journal tab (Clues)"); Clipping("Journal clues");
                await Press(Key.Q); Check(journal.Current == JournalView.Tab.Case, "Q → previous tab (Case)");
                for(int i = 0; i < 5; i++) { await Press(Key.E); Clipping("Journal tab " + journal.Current); }
                Check(journal.Current == JournalView.Tab.Map, "Tabs cycle to Map");
                await Press(Key.Tab); await Screen(HuntHUD.Screen.Play, "Tab closes the journal");
                await Press(Key.M); await Screen(HuntHUD.Screen.Journal, "M opens the map"); Check(journal.Current == JournalView.Tab.Map, "M → Map tab"); Clipping("Map");
                await Press(Key.M); await Screen(HuntHUD.Screen.Play, "M closes the map");
                var ledger = Array.Find(s.items, i => i.kind == HuntItem.GuestLedger); s.Interact(ledger); await Wait(200);
                Check(F<CanvasGroup>(hud, "_notify").alpha > 0, "Clue pickup: 'Added to journal' notification");
                await Press(Key.Tab); await Press(Key.E); await Wait(150);
                var entry = F<System.Collections.Generic.List<JournalEntry>>(journal, "_clues")[0];
                await Click(entry.Button, "Clue entry"); await Screen(HuntHUD.Screen.Evidence, "Clue entry → evidence inspect");
                var ev = (EvidenceView)View("_evidence");
                await Press(Key.R); Check(!F<GameObject>(ev, "_transcript").activeSelf, "R hides the transcript"); await Press(Key.R); Check(F<GameObject>(ev, "_transcript").activeSelf, "R shows the transcript");
                await Press(Key.Q); Check(F<GameObject>(ev, "_back").activeSelf, "Q turns the document over"); await Press(Key.Q);
                await Press(Key.UpArrow, 400); Check(F<RectTransform>(ev, "_paper").localScale.x > 1.01f, "↑ zooms the document");
                await Press(Key.Escape); await Screen(HuntHUD.Screen.Journal, "Esc from evidence → journal");
                await Press(Key.Tab); await Screen(HuntHUD.Screen.Play, "Tab closes the journal");

                // 11 일시정지 / 14 설정 (Esc)
                Stage = "Pause"; await Press(Key.Escape); await Screen(HuntHUD.Screen.Pause, "Esc");
                var pause = (PauseView)View("_pause");
                await Click(F<MenuButton>(pause, "_options").Button, "Pause Options"); await Screen(HuntHUD.Screen.Settings, "Pause › Options");
                var settings = (SettingsView)View("_settings");
                string head = settings.transform.Find("Options/Title").GetComponent<TMP_Text>().text;
                var rows = F<System.Collections.Generic.List<OptionRow>>(settings, "_rows");
                await Click(F<Button>(rows[0], "_next"), "Language ▶"); Check(!Loc.Korean && settings.transform.Find("Options/Title").GetComponent<TMP_Text>().text != head, "Options: language applies to the open screen at once");
                for(int t = 0; t < 7; t++) { settings.SelectTab(t); await Wait(120); Clipping("Options tab " + t + " (en)"); }
                GameSettings.Set("lang", "ko"); for(int t = 0; t < 7; t++) { settings.SelectTab(t); await Wait(120); Clipping("Options tab " + t + " (ko)"); }
                settings.SelectTab(0); await Press(Key.E); Check(F<int>(settings, "_tab") == 1, "E → next options tab");
                await Press(Key.Escape); await Screen(HuntHUD.Screen.Pause, "Esc from Options → Pause");
                await Click(F<MenuButton>(pause, "_journal").Button, "Pause Journal"); await Screen(HuntHUD.Screen.Journal, "Pause › Journal");
                await Press(Key.Escape); await Screen(HuntHUD.Screen.Pause, "Esc from journal returns to Pause");
                await Click(F<MenuButton>(pause, "_map").Button, "Pause Map"); await Screen(HuntHUD.Screen.Journal, "Pause › Map"); Check(journal.Current == JournalView.Tab.Map, "Pause › Map opens the map tab");
                await Press(Key.Escape); await Press(Key.Escape); await Screen(HuntHUD.Screen.Play, "Esc resumes");

                // 8 게임 오버 / 9 챕터 완료 / 10 크레딧
                Stage = "Dead"; s.Damage(100); await Screen(HuntHUD.Screen.Dead, "Death");
                var dead = (DeadView)View("_dead"); Check(dead.Hint.text == Loc.T("System/dead.hint.ch04"), "Game over: chapter hint");
                GameSettings.Set("lang", "en"); await Wait(80); Check(dead.Hint.text == Loc.T("System/dead.hint.ch04") && dead.Retry.Label.text == "Continue", "Game over re-renders in English"); Clipping("Dead (en)"); GameSettings.Set("lang", "ko");
                await Click(dead.Retry.Button, "Continue (retry)"); await Screen(HuntHUD.Screen.Play, "Retry");
                Stage = "Complete"; s.CompleteChapter(); await Screen(HuntHUD.Screen.Complete, "Chapter complete");
                var complete = (CompleteView)View("_complete"); Check(complete.Title.text == "JERICHO, CA." && complete.Next.text.Contains("05"), "Complete: English title + next chapter");
                GameSettings.Set("lang", "en"); await Wait(80); Check(complete.Label.text == "CHAPTER 04 COMPLETE" && complete.Title.text == "JERICHO, CA.", "Complete in English (title fixed)"); Clipping("Complete (en)"); GameSettings.Set("lang", "ko");

                // 해상도 (16:9) — every screen already scanned at the current size; repeat the densest ones at the extremes.
                Stage = "Resolutions";
                foreach(var (w, h) in new[] { (1280, 720), (2560, 1440), (3840, 2160), (1920, 1080) })
                {
                    UIQA.SetGameView(w, h); await Wait(500);
                    Check(Mathf.Approximately(((RectTransform)Hud.transform).rect.width, 1600) && Mathf.Approximately(((RectTransform)Hud.transform).rect.height, 900), w + "x" + h + ": canvas keeps the 1600×900 reference");
                    Clipping("Complete " + w + "x" + h);
                }

                // 7 의식 hold (Prologue anchor)
                Stage = "Ritual"; SceneManager.LoadScene("SCN_NewOrleans_Slice");
                for(int t = 0; t < 100 && (SceneManager.GetActiveScene().name != "SCN_NewOrleans_Slice" || !S || !S.hud || S.hud.Current == HuntHUD.Screen.None); t++) await Wait(100);
                s = S; s.saveSlot = "qa-uiflow-" + run + "-p"; Hud.StartNewGame(); await Wait(300); await Press(Key.Space); Check(!Hud.CardVisible, "Prologue card skipped");
                s.AddEvidence(127); var anchor = Array.Find(s.items, i => i.kind == HuntItem.Anchor); s.Interact(anchor);
                p = anchor.transform.position + new Vector3(0, 0, -1.1f); p.y = .1f; s.player.Teleport(p); s.player.view.Face(0, 25); await Wait(300);
                s.ghost.ResetGhost(false); // keep the final-hunt ghost from interrupting the hold measurement
                hud = (HudView)View("_hud"); var ritual = F<GameObject>(hud, "_ritual"); var ring = F<Image>(hud, "_ring");
                Check(ritual.activeSelf && F<TMP_Text>(hud, "_ritualSub").text.Contains(Loc.T("UI_Common/prompt.lighter_needed")), "Ritual: hold-E ring with 'light your lighter' line");
                await Press(Key.F); Check(F<TMP_Text>(hud, "_ritualSub").text == Loc.T("UI_Common/prompt.lighter_on"), "Ritual: lighter lit line");
                _keys = new KeyboardState(Key.E); await Wait(2200); float fill = ring.fillAmount; _keys = new KeyboardState(); await Wait(150);
                Check(fill > .1f && Mathf.Abs(fill - s.BurnProgress / s.definition.ignitionSeconds) < .05f, "Holding E fills the ring with ignition progress (" + fill.ToString("0.00") + ")");
                Clipping("Ritual");

                // Remapped key survives a scene load (code-built bindings get new ids each Awake); Pause › Retry resets the HUD.
                Stage = "Rebind"; var interact = s.player.Input.Action("Interact"); interact.ApplyBindingOverride(0, "<Keyboard>/g"); s.player.Input.SaveBindings(); Hud.BindingsChanged();
                var before = S; SceneManager.LoadScene("SCN_NewOrleans_Slice");
                for(int t = 0; t < 100 && (!S || S == before || !S.hud || S.hud.Current == HuntHUD.Screen.None); t++) await Wait(100);
                Check(S.hud.KeyName("Interact") == "G", "Remapped Interact (G) survives a scene reload (" + S.hud.KeyName("Interact") + ")");
                GameSettings.BindingOverrides = "";
                s = S; s.saveSlot = "qa-uiflow-" + run + "-r"; Hud.StartNewGame(); await Wait(300); s.SetPaused(true); await Screen(HuntHUD.Screen.Pause, "Pause before retry");
                await Click(F<MenuButton>((PauseView)View("_pause"), "_retry").Button, "Pause Retry"); await Screen(HuntHUD.Screen.Play, "Pause › Retry checkpoint");
                Result = "PASS\n" + _log; Stage = "Complete";
            }
            catch(Exception e) { Result = "FAIL at " + Stage + ": " + e.Message + "\n" + _log; Debug.LogError(Result); }
            finally
            {
                InputSystem.onBeforeUpdate -= Tick;
                if(_kb != null) InputSystem.RemoveDevice(_kb); if(_mouse != null) InputSystem.RemoveDevice(_mouse); _kb = null; _mouse = null;
                foreach(var d in physical) if(d.added) InputSystem.EnableDevice(d);
                Application.runInBackground = oldRun; InputSystem.settings.backgroundBehavior = oldBg; InputSystem.settings.editorInputBehaviorInPlayMode = oldEditor;
                GameSettings.UseTransientDefaults(false);
                foreach(var f in Directory.GetFiles(Application.persistentDataPath, "qa-uiflow-" + run + "*")) File.Delete(f);
                Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/UIFlowCheck.txt", Result);
            }
        }
    }
}
