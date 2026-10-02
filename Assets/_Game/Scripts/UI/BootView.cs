using UnityEngine;
using UnityEngine.UI;
namespace BeforeJericho.UI
{
    /// <summary>First-launch setup (HTML boot): language / subtitles / size / speaker names with a live preview. Shown once.</summary>
    public sealed class BootView : UIView
    {
        [SerializeField] OptionRow[] _rows;
        [SerializeField] Button _confirm;
        [SerializeField] SubtitleView _preview;
        static readonly string[] Ids = { "lang", "subs", "subSize", "speaker" };
        void Awake() => _confirm.onClick.AddListener(() => GameSettings.FirstRunDone = true);
        protected override void Opened()
        {
            for(int i = 0; i < _rows.Length; i++) _rows[i].Bind(OptionRow.Kind.Cycle, Ids[i]);
            _first = _rows[0];
            base.Opened();
        }
        public override void Refresh()
        {
            foreach(var r in _rows) if(r.Id != null) r.Render(null);
            _preview.Set("Captions/tv", 0, Loc.T("Dialogue/speaker.lorna") + ": " + Loc.T("Dialogue_Sample/lorna.wall"), true);
        }
    }
}
