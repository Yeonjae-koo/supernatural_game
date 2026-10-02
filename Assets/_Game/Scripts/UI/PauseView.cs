using TMPro;
using UnityEngine;
namespace BeforeJericho.UI
{
    /// <summary>Pause: menu left; objective, checkpoint and play time right (the HUD no longer shows the objective permanently).</summary>
    public sealed class PauseView : UIView
    {
        [SerializeField] MenuButton _resume, _journal, _map, _options, _retry, _main;
        [SerializeField] TMP_Text _objective, _checkpoint, _playTime, _title;
        public MenuButton Resume => _resume;
        public MenuButton RetryButton => _retry;
        public MenuButton Main => _main;
        public TMP_Text Title => _title;
        void Awake()
        {
            _resume.Button.onClick.AddListener(() => Hud.session.Resume());
            _journal.Button.onClick.AddListener(() => Hud.OpenJournal(false));
            _map.Button.onClick.AddListener(() => Hud.OpenJournal(true));
            _options.Button.onClick.AddListener(() => Hud.OpenSettings());
            _retry.Button.onClick.AddListener(() => Hud.RetryCheckpoint());
            _main.Button.onClick.AddListener(() => Hud.session.MainMenu());
        }
        public override void Refresh()
        {
            if(!Hud) return;
            _objective.text = Loc.Src(Hud.session.Objective);
            _checkpoint.text = Loc.Src(Hud.session.CheckpointName);
        }
        public override void Tick()
        {
            int t = Mathf.FloorToInt(Hud.PlaySeconds);
            _playTime.SetText("{0:00}:{1:00}:{2:00}", t / 3600, t / 60 % 60, t % 60);
        }
    }
}
