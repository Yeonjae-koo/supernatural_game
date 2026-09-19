using System;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace BeforeJericho
{
    public sealed class HuntSession : MonoBehaviour
    {
        public AtchafalayaChapter chapter;
        public EastTexasChapter eastTexas;
        public GrantsChapter grants;
        public WinslowChapter winslow;
        public StanfordChapter stanford;
        public PoltergeistHunt Haunt => grants ? grants.Poltergeist : winslow ? winslow.Spirit : null;
        public bool HasSpiritKit => grants ? grants.HasKit : winslow;
        string ActiveChapterId => stanford ? StanfordChapter.ChapterId : winslow ? WinslowChapter.ChapterId : grants ? GrantsChapter.ChapterId : eastTexas ? EastTexasChapter.ChapterId : chapter ? AtchafalayaChapter.ChapterId : "";
        public HuntDefinition definition; public HunterMotor player; public GhostHunt ghost; public HuntAudio sound; public HuntHUD hud;
        public HuntInteractable[] items; public GameObject saltCircle, fire;
        public string saveSlot = "before-jericho-prologue-v1";
        public bool Playing => !Menu && !Paused && !Journal && HP > 0 && !Completed;
        public bool Menu { get; private set; } = true;
        public bool Paused { get; private set; }
        public bool Journal { get; private set; }
        public bool Resolved { get; private set; }
        public bool Completed { get; private set; }
        public int HP { get; private set; } = 100;
        public int Evidence { get; private set; }
        public float BurnProgress { get; private set; }
        public string Message { get; private set; } = "";
        public string CheckpointName => _checkpoint?.checkpoint ?? "Shop entrance";
        public bool CanContinue => HuntSave.TryLoad(saveSlot, out _, ActiveChapterId);
        float _messageUntil, _lastDamage = -10; bool _firstGhost, _finalGhost; HuntSnapshot _checkpoint;
        HunterCombat _combat;
        public string Objective => stanford ? stanford.Objective : winslow ? winslow.Objective : grants ? grants.Objective : eastTexas ? eastTexas.Objective : chapter ? chapter.Objective : Resolved ? "RETURN TO THE IMPALA" : Has(HuntItem.Record) && Has(HuntItem.Salt) ? "RETURN TO THE CELLAR / SALT AND BURN THE NECKLACE" : (Evidence & 15) == 15 ? "ST. MARAIS / FIND THE BURIAL RECORD AND SALT" : "CHECK THE ANTIQUE SHOP / FIND FOUR CLUES";
        void Start()
        {
            _combat = player.GetComponent<HunterCombat>();
            if(chapter && saveSlot == "before-jericho-prologue-v1") saveSlot = "before-jericho-ch02-atchafalaya-v1";
            if(eastTexas && saveSlot == "before-jericho-prologue-v1") saveSlot = "before-jericho-ch03-east-texas-v1";
            if(grants && saveSlot == "before-jericho-prologue-v1") saveSlot = "before-jericho-ch04-grants-v1";
            if(winslow && saveSlot == "before-jericho-prologue-v1") saveSlot = "before-jericho-ch05-winslow-v1";
            if(stanford && saveSlot == "before-jericho-prologue-v1") saveSlot = "before-jericho-ch06-stanford-v1";
            ApplyInput();
        }
        void Update()
        {
            if(Time.unscaledTime > _messageUntil) Message = "";
            if(!Menu && !Completed && HP > 0)
            {
                if(player.Input.Pause) { if(Journal) Journal = false; else Paused = !Paused; ApplyInput(); }
                else if(player.Input.Journal && !Paused) { Journal = !Journal; ApplyInput(); }
            }
            if(Playing && player.transform.position.y < -4) Damage(100);
            if(!stanford && !winslow && !grants && !eastTexas && !chapter && Playing && !_finalGhost && !Resolved && Has(HuntItem.Record) && Has(HuntItem.Salt) && player.transform.position.z > 54)
            {
                _finalGhost = true; SaveCheckpoint("Final hunt", new Vector3(0, .1f, 53));
                ghost.Activate(new Vector3(2,1.3f,64)); Notify("Salt the necklace. Use iron to buy time, then hold E with the lighter.");
            }
            foreach(var item in items) if(item.marker) item.marker.gameObject.SetActive(!item.Used(this));
        }
        void ApplyInput()
        {
            bool play = Playing; if(player.Input) player.Input.SetGameplay(play);
            Cursor.lockState = play ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !play;
            Time.timeScale = play ? 1 : 0;
        }
        public void NewGame()
        {
            Evidence = 0; HP = 100; Resolved = Completed = Paused = Journal = false; Menu = false;
            BurnProgress = 0; _firstGhost = _finalGhost = false; if(stanford) stanford.NewGame(); else if(winslow) winslow.NewGame(); else if(grants) grants.NewGame(); else if(eastTexas) eastTexas.NewGame(); else if(chapter) chapter.NewGame(); else { ghost.ResetGhost(false); saltCircle.SetActive(false); fire.SetActive(false); }
            player.Teleport(stanford ? stanford.ArrivalPosition : winslow ? winslow.ArrivalPosition : grants ? grants.ArrivalPosition : eastTexas ? eastTexas.ArrivalPosition : chapter ? chapter.ArrivalPosition : new Vector3(0,.1f,3)); player.view.Face(0); _combat.Restore(7,winslow || grants ? 28 : eastTexas ? 49 : 21);
            SaveCheckpoint(stanford ? "Motel room" : winslow ? "Coyote Mile arrival" : grants ? "Sunset Star parking lot" : eastTexas ? "Mercy Creek arrival" : chapter ? "Belle Rive arrival" : "Shop entrance", player.transform.position); ApplyInput(); Notify(stanford ? "MOTEL ROOM / WINSLOW, ARIZONA\nThe phone, Dad's notes, the clippings, the map. Lay it out.\nNo hunting tonight." : winslow ? "WINSLOW, ARIZONA / COYOTE MILE\nHeadlights in the haze. A man standing in the middle of the road — Dean brakes hard. Nobody there.\nThree crashes on this mile this year. Start at the skid marks." : grants ? "GRANTS, NEW MEXICO / ROUTE 66\nSunset Star Motor Court. Guests keep dying in the same place.\nTake the kit from the trunk and talk to the manager." : eastTexas ? "EAST TEXAS / MERCY CREEK\nRadio: Cemetery vandalism reported.\nDean: That's subtle. Investigate the graves and funeral home." : chapter ? "ATCHAFALAYA BASIN / BELLE RIVE\nMissing children. Mud on the windowsills. Ask Mara at the diner." : "SAINT MERCY DISTRICT / 11:17 PM\nJohn can wait. Finish the job first.");
        }
        public void ContinueGame() { if(HuntSave.TryLoad(saveSlot, out var s, ActiveChapterId)) { _checkpoint = s; RestoreCheckpoint(); } else Notify("No valid checkpoint found."); }
        public void Resume() { Paused = Journal = false; ApplyInput(); }
        public void MainMenu() { Menu = true; Paused = Journal = false; ApplyInput(); }
        public void SetJournal(bool value) { Journal = value; ApplyInput(); }
        public bool Has(HuntItem kind) => !stanford && !winslow && !grants && !eastTexas && !chapter && kind >= HuntItem.Mirror && kind <= HuntItem.Salt && (Evidence & (1 << (int)kind)) != 0;
        public void Notify(string message) { Message = message; _messageUntil = Time.unscaledTime + 5; }
        public bool Interact(HuntInteractable item)
        {
            if(!Playing || !item || !item.Available(this)) return false;
            if(stanford) return stanford.Interact(item);
            if(winslow) return winslow.Interact(item);
            if(grants) return grants.Interact(item);
            if(eastTexas) return eastTexas.Interact(item);
            if(chapter) return chapter.Interact(item);
            if(item.kind == HuntItem.Exit)
            {
                if(!Resolved) { Notify("Not yet. There is still a hunt to finish."); return false; }
                Completed = true; ApplyInput(); return true;
            }
            if(item.kind == HuntItem.Anchor)
            {
                if(!Has(HuntItem.Record) || !Has(HuntItem.Salt)) { Notify("Find the burial record and salt at St. Marais first."); return false; }
                saltCircle.SetActive(true); return true;
            }
            if((item.kind == HuntItem.Record || item.kind == HuntItem.Salt) && (Evidence & 15) != 15)
            { Notify("The antique shop still holds four clues. Start there."); return false; }
            Evidence |= 1 << (int)item.kind; Notify(item.description); sound.PlayCue(420,.1f,.12f);
            if(item.kind == HuntItem.Iron) _combat.Equip(2);
            if((Evidence & 15) == 15 && !_firstGhost)
            { _firstGhost = true; ghost.Activate(new Vector3(-9,1.3f,24)); Notify("Behind you. Bullets cannot kill it. Find the iron poker."); }
            if(item.kind == HuntItem.Record || item.kind == HuntItem.Salt)
            {
                ghost.ResetGhost(false); HP = 100; SaveCheckpoint("St. Marais cemetery", new Vector3(4,.1f,42));
            }
            return true;
        }
        public void AdvanceBurn(bool holding, float dt)
        {
            if(grants || winslow)
            {
                if(!Playing || Resolved || !holding || !(grants ? grants.AnchorSecured : winslow.AnchorSecured) || Time.time - _lastDamage < .7f) return;
                BurnProgress = Mathf.Min(definition.ignitionSeconds, BurnProgress + dt);
                if(BurnProgress >= definition.ignitionSeconds) { if(grants) grants.AnchorBurned(); else winslow.AnchorBurned(); }
                return;
            }
            if(eastTexas || chapter || !Playing || Resolved || !holding || !saltCircle.activeSelf || !Has(HuntItem.Iron) || !Has(HuntItem.Record) || !Has(HuntItem.Salt) || !_combat.LighterOn || Time.time - _lastDamage < .7f) return;
            BurnProgress = Mathf.Min(definition.ignitionSeconds, BurnProgress + dt);
            if(BurnProgress >= definition.ignitionSeconds)
            {
                Resolved = true; ghost.Resolve(); fire.SetActive(true); HP = 100;
                SaveCheckpoint("Hunt resolved", new Vector3(0,.1f,53)); Notify("The necklace cracks. Silence. Time to call Dad.");
            }
        }
        public void Damage(int amount)
        {
            if(!Playing) return; HP = Mathf.Max(0, HP - amount); _lastDamage = Time.time;
            BurnProgress = Mathf.Max(0, BurnProgress - 1); Notify(winslow || grants ? "Iron [3 + LMB] disperses it. Salt [4 + LMB] holds a line. Keep moving." : eastTexas ? "Keep moving. Head hits stagger ghouls; knives finish staggered targets." : chapter ? "Keep moving. Guns and knives only stagger Rawhead." : "It is too close. Disperse it with iron [3 + LMB].");
            if(HP == 0) ApplyInput();
        }
        public void SaveCheckpoint(string name, Vector3 point)
        {
            _checkpoint = new HuntSnapshot { evidence = Evidence, hp = HP, ammo = _combat.Ammo, reserve = _combat.Reserve, x = point.x, y = point.y, z = point.z, resolved = Resolved, checkpoint = name };
            if(stanford) stanford.Capture(_checkpoint); else if(winslow) winslow.Capture(_checkpoint); else if(grants) grants.Capture(_checkpoint); else if(eastTexas) eastTexas.Capture(_checkpoint); else if(chapter) chapter.Capture(_checkpoint);
            try { HuntSave.Write(saveSlot, _checkpoint); } catch(Exception e) { Notify("Checkpoint kept in memory; disk save failed."); Debug.LogWarning("Checkpoint save failed: " + e.Message); }
        }
        public void RestoreCheckpoint()
        {
            if(_checkpoint == null) { ContinueGame(); return; }
            Evidence = _checkpoint.evidence; HP = _checkpoint.hp; Resolved = _checkpoint.resolved;
            Menu = Paused = Journal = Completed = false; BurnProgress = 0;
            _firstGhost = (Evidence & 15) == 15; _finalGhost = false;
            if(stanford) { stanford.Restore(_checkpoint); Completed = stanford.TransitionReady; }
            else if(winslow) { winslow.Restore(_checkpoint); Completed = winslow.TransitionReady; }
            else if(grants) { grants.Restore(_checkpoint); Completed = grants.TransitionReady; }
            else if(eastTexas) { eastTexas.Restore(_checkpoint); Completed = eastTexas.TransitionReady; }
            else if(chapter) { chapter.Restore(_checkpoint); Completed = chapter.TransitionReady; }
            else { ghost.ResetGhost(Resolved); saltCircle.SetActive(Resolved); fire.SetActive(Resolved); }
            player.Teleport(new Vector3(_checkpoint.x,_checkpoint.y,_checkpoint.z)); player.view.Face(0); _combat.Restore(_checkpoint.ammo,_checkpoint.reserve);
            ApplyInput(); Notify("Checkpoint restored / " + CheckpointName);
        }
        public void AddEvidence(int bit) { Evidence |= bit; if(sound) sound.PlayCue(420, .1f, .12f); }
        public void Heal() { HP = 100; }
        public void SetChapterResolved() { Resolved = true; HP = 100; }
        public void CompleteChapter() { Completed = true; ApplyInput(); }
        public void TravelToAtchafalaya()
        {
            if(eastTexas || chapter || !Completed) return;
            const string scene = "SCN_Atchafalaya_Slice";
            if(!Application.CanStreamedLevelBeLoaded(scene)) { Notify("CH02 scene is missing from Build Profiles. Add SCN_Atchafalaya_Slice to continue."); return; }
            Time.timeScale = 1; SceneManager.LoadScene(scene);
        }
        public void TravelToEastTexas()
        {
            if(!chapter || !Completed) return;
            const string scene = "SCN_EastTexas_Slice";
            if(!Application.CanStreamedLevelBeLoaded(scene)) { Notify("CH03 scene is missing from Build Profiles."); return; }
            Time.timeScale = 1; SceneManager.LoadScene(scene);
        }
        public void TravelToGrants()
        {
            if(!eastTexas || !Completed) return;
            const string scene = "SCN_GrantsNM_Slice";
            if(!Application.CanStreamedLevelBeLoaded(scene)) { Notify("CH04 scene is missing from Build Profiles."); return; }
            Time.timeScale = 1; SceneManager.LoadScene(scene);
        }
        public void TravelToWinslow()
        {
            if(!grants || !Completed) return;
            const string scene = "SCN_Winslow_Slice";
            if(!Application.CanStreamedLevelBeLoaded(scene)) { Notify("CH05 scene is missing from Build Profiles."); return; }
            Time.timeScale = 1; SceneManager.LoadScene(scene);
        }
        public void TravelToStanford()
        {
            if(!winslow || !Completed) return;
            const string scene = "SCN_Stanford_Slice";
            if(!Application.CanStreamedLevelBeLoaded(scene)) { Notify("CH06 scene is missing from Build Profiles."); return; }
            Time.timeScale = 1; SceneManager.LoadScene(scene);
        }
        void OnDestroy() { Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}
