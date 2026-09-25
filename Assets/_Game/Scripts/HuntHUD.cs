using System.Collections.Generic;
using BeforeJericho.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace BeforeJericho
{
    /// <summary>UI state owner for one scene: resolves the visible screen from HuntSession state plus UI-only sub-states
    /// (settings, credits, evidence), routes Esc / Tab / M / Q / E / R / Space, and renders through the screen views.
    ///
    /// Menu ─NEW GAME/CONTINUE→ Play (+ location card) ─Esc→ Pause ─→ Journal/Map ─Enter→ Evidence
    ///   │                        ├─Tab→ Journal ─M→ Map tab          └─→ Settings
    ///   └─Options→ Settings      ├─HP 0→ Dead ─Continue→ Play
    ///   └─Credits→ Credits       └─exit→ Complete ─Continue→ next scene (CH06: Credits → Menu)
    /// Boot (first launch only) precedes Menu.</summary>
    public sealed class HuntHUD : MonoBehaviour
    {
        public enum Screen { None, Boot, Menu, Play, Pause, Journal, Evidence, Settings, Dead, Complete, Credits }
        public HuntSession session; public HunterCombat combat;
        [SerializeField] TextAsset[] _stringTables;
        [SerializeField] BootView _boot; [SerializeField] MenuView _menu; [SerializeField] CardView _card; [SerializeField] HudView _hud;
        [SerializeField] EvidenceView _evidence; [SerializeField] DeadView _dead; [SerializeField] CompleteView _complete; [SerializeField] CreditsView _credits;
        [SerializeField] PauseView _pause; [SerializeField] JournalView _journal; [SerializeField] SettingsView _settings; [SerializeField] DevOverlay _dev;

        public Screen Current { get; private set; }
        public float PlaySeconds { get; private set; }
        public int Deaths { get; private set; }
        public bool CardVisible => _card.Visible;
        bool _settingsOpen, _creditsOpen, _evidenceOpen, _cardPending; Screen _base;
        readonly Dictionary<string, string> _keyNames = new();
        List<string> _clueKinds;
        UIView[] _views;

        public string ChapterId => session.stanford ? "ch06" : session.winslow ? "ch05" : session.grants ? "ch04" : session.eastTexas ? "ch03" : session.chapter ? "ch02" : "prologue";
        /// <summary>HTML screen id (development overlay): HUD contexts are explore / combat / ritual.</summary>
        public string ScreenId => Current switch
        {
            Screen.Play => CardVisible ? "card" : _hud.Context, Screen.Journal => _journal.Current == JournalView.Tab.Map ? "map" : "journal",
            Screen.None => "", _ => Current.ToString().ToLowerInvariant()
        };

        // Legacy contract used by the QA route runners: the primary/secondary actions of whichever screen is open.
        public Button primary => Current switch { Screen.Menu => _menu.NewGame.Button, Screen.Dead => _dead.Retry.Button, Screen.Complete => _complete.Continue.Button, Screen.Pause => _pause.Resume.Button, _ => null };
        public Button secondary => Current switch { Screen.Menu => _menu.Continue.Button, Screen.Dead => _dead.Main.Button, Screen.Complete => _complete.Main.Button, Screen.Pause => _pause.RetryButton.Button, _ => null };
        public Button tertiary => Current == Screen.Pause ? _pause.Main.Button : null;
        public TMP_Text primaryLabel => Label(primary);
        public TMP_Text secondaryLabel => Label(secondary);
        public TMP_Text tertiaryLabel => Label(tertiary);
        public TMP_Text subtitle => Current == Screen.Menu ? _menu.NewGame.Small : Current == Screen.Complete ? _complete.Label : null;
        public TMP_Text overlayTitle => Current switch { Screen.Menu => _menu.Logo, Screen.Dead => _dead.Title, Screen.Complete => _complete.Title, Screen.Credits => _credits.Logo, Screen.Pause => _pause.Title, _ => null };
        public TMP_Text overlayBody => Current switch { Screen.Dead => _dead.Hint, Screen.Complete => _complete.Body, _ => null };
        public TMP_Text completeNext => _complete.Next;
        public GameObject overlay => Current switch
        {
            Screen.Boot => _boot.gameObject, Screen.Menu => _menu.gameObject, Screen.Pause => _pause.gameObject, Screen.Journal => _journal.gameObject, Screen.Evidence => _evidence.gameObject,
            Screen.Settings => _settings.gameObject, Screen.Dead => _dead.gameObject, Screen.Complete => _complete.gameObject, Screen.Credits => _credits.gameObject, _ => _menu.gameObject
        };
        static TMP_Text Label(Button b) => b ? b.GetComponent<MenuButton>().Label : null;

        void Awake()
        {
            _views = new UIView[] { _boot, _menu, _card, _hud, _evidence, _dead, _complete, _credits, _pause, _journal, _settings };
            foreach(var v in _views) if(!v) { Debug.LogError("HuntHUD: a screen view reference is missing; rebuild PF_HuntUI (Before Jericho/UI/Build UI Prefabs).", this); enabled = false; return; }
            if(!session || !combat) { Debug.LogError("HuntHUD: session/combat references are missing.", this); enabled = false; return; }
            Loc.Load(_stringTables);
            foreach(var v in _views) { v.Bind(this); v.Show(false); }
            if(_dev) _dev.Bind(this);
        }

        void Start() => Switch(Resolve());

        void Update()
        {
            HandleInput();
            var next = Resolve();
            if(next != Current) Switch(next);
            if(session.Playing) PlaySeconds += Time.unscaledDeltaTime;
            if(Current == Screen.Play)
            {
                if(_cardPending) { _cardPending = false; _card.Show(true); }
                if(_card.Visible && _card.Done) { _card.Show(false); _hud.ObjectiveReady(); }
            }
            foreach(var v in _views) if(v.Visible) v.Tick();
        }

        Screen Resolve()
        {
            Screen b = session.Menu ? Screen.Menu : session.Completed ? Screen.Complete : session.HP == 0 ? Screen.Dead : session.Journal ? Screen.Journal : session.Paused ? Screen.Pause : Screen.Play;
            if(b != _base) { if(b != Screen.Journal) _evidenceOpen = false; if(b != Screen.Pause && b != Screen.Menu) _settingsOpen = false; if(b != Screen.Menu && b != Screen.Complete) _creditsOpen = false; _base = b; }
            return b switch
            {
                Screen.Menu => _creditsOpen ? Screen.Credits : _settingsOpen ? Screen.Settings : GameSettings.FirstRunDone ? Screen.Menu : Screen.Boot,
                Screen.Complete => _creditsOpen ? Screen.Credits : Screen.Complete,
                Screen.Journal => _evidenceOpen ? Screen.Evidence : Screen.Journal,
                Screen.Pause => _settingsOpen ? Screen.Settings : Screen.Pause,
                _ => b
            };
        }

        void Switch(Screen next)
        {
            var previous = Current; Current = next;
            if(next == Screen.Dead && previous != Screen.Dead) Deaths++;
            _boot.Show(next == Screen.Boot);
            _menu.Show(next == Screen.Menu || (next == Screen.Settings && session.Menu)); // options open over the menu, as in the prototype
            _hud.Show(next == Screen.Play); if(next != Screen.Play) _card.Show(false);
            _pause.Show(next == Screen.Pause); _journal.Show(next == Screen.Journal); _evidence.Show(next == Screen.Evidence);
            _settings.Show(next == Screen.Settings); _dead.Show(next == Screen.Dead); _complete.Show(next == Screen.Complete); _credits.Show(next == Screen.Credits);
            if(next == Screen.Menu || next == Screen.Settings && previous == Screen.Menu) _menu.Refresh();
        }

        void HandleInput()
        {
            var input = session.player ? session.player.Input : null;
            if(!input || _settings.Rebinding) return;
            switch(Current)
            {
                case Screen.Play:
                    if(input.Pause) session.SetPaused(true);
                    else if(input.Journal) OpenJournal(false);
                    else if(input.Map) OpenJournal(true);
                    else if(input.Skip) { if(_card.Visible) { _card.Show(false); _hud.ObjectiveReady(); } else if(_hud.SubtitleShowing) _hud.DismissSubtitle(); }
                    if(input.DebugHit && Debug.isDebugBuild) _hud.SimulateHit(); // prototype "H": development only
                    break;
                case Screen.Pause:
                    if(input.Pause) session.Resume(); else if(input.Journal) OpenJournal(false); else if(input.Map) OpenJournal(true);
                    break;
                case Screen.Journal:
                    if(input.Pause || input.Journal) CloseJournal();
                    else if(input.Map) { if(_journal.Current == JournalView.Tab.Map) CloseJournal(); else _journal.Select(JournalView.Tab.Map); }
                    else if(input.TabPrevious) _journal.Step(-1); else if(input.TabNext) _journal.Step(1);
                    break;
                case Screen.Evidence:
                    if(input.Pause) _evidenceOpen = false; else if(input.Journal) CloseJournal();
                    else if(input.Transcript) _evidence.ToggleTranscript(); else if(input.TabPrevious) _evidence.Flip();
                    else if(Mathf.Abs(input.Zoom) > .01f || Mathf.Abs(input.ZoomWheel) > .01f) _evidence.Zoom(input.Zoom * Time.unscaledDeltaTime * 2 + Mathf.Sign(input.ZoomWheel) * (Mathf.Abs(input.ZoomWheel) > .01f ? .1f : 0));
                    break;
                case Screen.Settings:
                    if(input.Pause) _settingsOpen = false; else if(input.TabPrevious) _settings.Step(-1); else if(input.TabNext) _settings.Step(1);
                    break;
                case Screen.Credits:
                    if(input.Pause) CloseCredits();
                    break;
            }
        }

        // ---- actions used by the views ----
        public void StartNewGame() { PlaySeconds = 0; Deaths = 0; session.NewGame(); _cardPending = true; _hud.Restart(true); }
        public void ContinueGame() { session.ContinueGame(); if(!session.Menu) { _cardPending = true; _hud.Restart(true); } }
        public void RetryCheckpoint() { session.RestoreCheckpoint(); _hud.Restart(false); }
        public void OpenSettings() => _settingsOpen = true;
        public void OpenCredits() => _creditsOpen = true;
        void CloseCredits() { _creditsOpen = false; if(session.Completed) session.MainMenu(); }
        public void OpenJournal(bool map)
        {
            if(map) _journal.Select(JournalView.Tab.Map); else if(_journal.Current == JournalView.Tab.Map) _journal.Select(JournalView.Tab.Case);
            session.SetJournal(true);
        }
        void CloseJournal() { _evidenceOpen = false; session.SetJournal(false); }
        public void OpenEvidence(HuntInteractable item) { _evidence.Open(item); _evidenceOpen = true; }
        public void ContinueFromComplete()
        {
            if(session.stanford) OpenCredits();
            else if(session.winslow) session.TravelToStanford();
            else if(session.grants) session.TravelToWinslow();
            else if(session.eastTexas) session.TravelToGrants();
            else if(session.chapter) session.TravelToEastTexas();
            else session.TravelToAtchafalaya();
        }

        // ---- shared queries ----
        public string KeyName(string action)
        {
            if(_keyNames.TryGetValue(action, out string name)) return name;
            var a = session.player.Input.Action(action);
            // Path names, not the keyboard layout's (a Korean layout would report "ㄷ" for E).
            name = a == null ? action : InputControlPath.ToHumanReadableString(a.bindings[0].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice)
                switch { "Left Button" => "LMB", "Right Button" => "RMB", "Escape" => "Esc", "Left Control" => "Ctrl", "Left Ctrl" => "Ctrl", "Space" => "Space", var s => s.Length == 1 ? s.ToUpperInvariant() : s };
            _keyNames[action] = name; return name;
        }
        public void BindingsChanged() { _keyNames.Clear(); Loc.NotifyChanged(); }

        public List<string> ClueKinds() => _clueKinds ??= new List<string>(Loc.Column("ClueKinds/" + ChapterId, false).Split(new[]{','}, System.StringSplitOptions.RemoveEmptyEntries));
        public (int found, int total) ClueProgress()
        {
            int found = 0; var kinds = ClueKinds();
            foreach(string k in kinds) { var item = Item(k); if(item && item.Used(session)) found++; }
            return (found, kinds.Count);
        }
        public HuntInteractable Item(string kind) { foreach(var i in session.items) if(i && i.kind.ToString() == kind) return i; return null; }
        public HuntInteractable Item(HuntItem kind) { foreach(var i in session.items) if(i && i.kind == kind) return i; return null; }
        public bool IsExit(HuntItem k) => k is HuntItem.Exit or HuntItem.ChapterExit or HuntItem.TexasExit or HuntItem.GrantsExit or HuntItem.WinslowExit or HuntItem.HitTheRoad;
        public bool IsAnchor(HuntItem k) => k is HuntItem.Anchor or HuntItem.WallCavity or HuntItem.SteeringWheel or HuntItem.VoltageSwitch;
        public IEnumerable<Vector3> ActiveThreats() => _hud.ActiveThreatPositions();
    }
}
