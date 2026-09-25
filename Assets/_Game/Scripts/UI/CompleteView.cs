using TMPro;
using UnityEngine;
namespace BeforeJericho.UI
{
    /// <summary>Chapter complete: in-world English title, summary stats, next destination. Continue travels on (CH06: credits).</summary>
    public sealed class CompleteView : UIView
    {
        [SerializeField] TMP_Text _label, _title, _body, _clues, _time, _deaths, _next;
        [SerializeField] MenuButton _continue, _main;
        public MenuButton Continue => _continue;
        public MenuButton Main => _main;
        public TMP_Text Label => _label;
        public TMP_Text Title => _title;
        public TMP_Text Body => _body;
        public TMP_Text Next => _next;
        void Awake() { _continue.Button.onClick.AddListener(() => Hud.ContinueFromComplete()); _main.Button.onClick.AddListener(() => Hud.session.MainMenu()); }
        public override void Refresh()
        {
            if(!Hud) return;
            string id = "System/complete." + Hud.ChapterId + ".";
            _label.text = Loc.T(id + "label"); _title.text = Loc.T(id + "title"); _body.text = Loc.T(id + "body"); _next.text = Loc.T(id + "next");
            var (found, total) = Hud.ClueProgress();
            _clues.text = found + " / " + total;
            int t = Mathf.FloorToInt(Hud.PlaySeconds);
            _time.text = t >= 3600 ? $"{t / 3600}:{t / 60 % 60:00}:{t % 60:00}" : $"{t / 60:00}:{t % 60:00}";
            _deaths.text = Hud.Deaths.ToString();
        }
    }
}
