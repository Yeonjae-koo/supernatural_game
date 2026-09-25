using TMPro;
using UnityEngine;
namespace BeforeJericho.UI
{
    /// <summary>Location card over live play (HTML .card): chapter, English place/date, ko-only sub lines. Space skips.</summary>
    public sealed class CardView : UIView
    {
        [SerializeField] CanvasGroup _group;
        [SerializeField] TMP_Text _chapter, _place, _placeSub, _date, _dateSub;
        float _shownAt;
        public const float Seconds = 5;
        protected override void Opened() { _shownAt = Time.unscaledTime; base.Opened(); }
        public bool Done => Time.unscaledTime - _shownAt >= Seconds;
        public override void Refresh()
        {
            if(!Hud) return;
            string id = "Chapter/" + Hud.ChapterId + ".card.";
            _chapter.text = Loc.T(id + "chapter"); _place.text = Loc.T(id + "place"); _date.text = Loc.T(id + "date");
            // ko-only rows: English column is empty, so the sub lines disappear in English (HTML behaviour).
            bool sub = Loc.Korean || Loc.ShowKeys;
            _placeSub.gameObject.SetActive(sub); _dateSub.gameObject.SetActive(sub);
            if(sub) { _placeSub.text = Loc.T(id + "place_sub"); _dateSub.text = Loc.T(id + "date_sub"); }
        }
        public override void Tick() { float t = Time.unscaledTime - _shownAt; _group.alpha = Mathf.Clamp01(t / 1.4f) * Mathf.Clamp01((Seconds - t) / .5f); }
    }
}
