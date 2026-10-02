using TMPro;
using UnityEngine;
namespace BeforeJericho.UI
{
    /// <summary>Game over: chapter hint + last checkpoint; Continue restores it (RE4 wording).</summary>
    public sealed class DeadView : UIView
    {
        [SerializeField] TMP_Text _title, _hint, _checkpoint;
        [SerializeField] MenuButton _retry, _main;
        public MenuButton Retry => _retry;
        public MenuButton Main => _main;
        public TMP_Text Title => _title;
        public TMP_Text Hint => _hint;
        void Awake() { _retry.Button.onClick.AddListener(() => Hud.RetryCheckpoint()); _main.Button.onClick.AddListener(() => Hud.session.MainMenu()); }
        public override void Refresh()
        {
            if(!Hud) return;
            _hint.text = Loc.T("System/dead.hint." + Hud.ChapterId);
            _checkpoint.text = Loc.T("UI_Common/pause.checkpoint") + " · " + Loc.Src(Hud.session.CheckpointName);
        }
    }
}
