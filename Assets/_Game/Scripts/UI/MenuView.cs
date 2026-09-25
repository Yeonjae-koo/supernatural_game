using System;
using System.IO;
using TMPro;
using UnityEngine;
namespace BeforeJericho.UI
{
    /// <summary>Main menu (HTML menu): logo stays English, CONTINUE shows the saved checkpoint, NEW GAME starts this scene's chapter.</summary>
    public sealed class MenuView : UIView
    {
        [SerializeField] TMP_Text _logo;
        [SerializeField] MenuButton _continue, _newGame, _load, _options, _credits, _quit;
        public MenuButton Continue => _continue;
        public MenuButton NewGame => _newGame;
        public TMP_Text Logo => _logo;

        void Awake()
        {
            _continue.Button.onClick.AddListener(() => Hud.ContinueGame());
            _newGame.Button.onClick.AddListener(() => Hud.StartNewGame());
            _options.Button.onClick.AddListener(() => Hud.OpenSettings());
            _credits.Button.onClick.AddListener(() => Hud.OpenCredits());
            _quit.Button.onClick.AddListener(Application.Quit);
            _load.SetInteractable(false); // one checkpoint per chapter: nothing else to load
        }

        // Options never change menu content except the language, so skip GameSettings.Changed (slider drags would re-read the save).
        protected override void OnEnable() => Loc.Changed += Refresh;
        protected override void OnDisable() => Loc.Changed -= Refresh;

        protected override void Opened()
        {
            _first = Hud.session.CanContinue ? _continue.Button : _newGame.Button;
            base.Opened();
        }

        public override void Refresh()
        {
            if(!Hud) return;
            var s = Hud.session;
            bool can = s.CanContinue;
            _continue.SetInteractable(can);
            _continue.SetSmall(can ? Loc.T("Chapter/" + Hud.ChapterId + ".short") + " · " + Loc.Src(SavedCheckpoint(s)) + " · " + Ago(s.saveSlot) : Loc.T("UI_Common/menu.no_save"));
            _newGame.SetSmall(Loc.T("Chapter/" + Hud.ChapterId + ".menu"));
        }

        static string SavedCheckpoint(HuntSession s) => HuntSave.TryLoad(s.saveSlot, out var snapshot) ? snapshot.checkpoint : s.CheckpointName;

        static string Ago(string slot)
        {
            string path = HuntSave.PathFor(slot);
            if(!File.Exists(path)) return "";
            var span = DateTime.Now - File.GetLastWriteTime(path);
            return span.TotalMinutes < 1 ? Loc.T("UI_Common/time.now") : span.TotalHours < 1 ? Loc.Format("UI_Common/time.min", ((int)span.TotalMinutes).ToString())
                 : span.TotalDays < 1 ? Loc.Format("UI_Common/time.hour", ((int)span.TotalHours).ToString()) : Loc.Format("UI_Common/time.day", ((int)span.TotalDays).ToString());
        }
    }
}
