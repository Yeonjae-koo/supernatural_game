using TMPro;
using UnityEngine;
namespace BeforeJericho.UI
{
    /// <summary>Evidence inspect (HTML evidence, Dead Space principle): the in-world English document on paper, the translation
    /// as an overlay. R transcript, Q turn over, ↑↓ / wheel zoom, Esc back to the journal.</summary>
    public sealed class EvidenceView : UIView
    {
        [SerializeField] RectTransform _paper;
        [SerializeField] GameObject _front, _back, _transcript;
        [SerializeField] TMP_Text _mast, _dateLeft, _dateMid, _dateRight, _headline, _photo, _original, _backNote;
        [SerializeField] TMP_Text _name, _trTitle, _trBody, _trNote;
        HuntInteractable _item; bool _showTranscript = true, _flipped; float _zoom = 1;

        public void Open(HuntInteractable item) { _item = item; _flipped = false; _zoom = 1; Refresh(); }
        public void ToggleTranscript() { _showTranscript = !_showTranscript; Refresh(); }
        public void Flip() { _flipped = !_flipped; Refresh(); }
        public void Zoom(float delta) { _zoom = Mathf.Clamp(_zoom + delta, 1, 1.6f); _paper.localScale = Vector3.one * _zoom; }

        public override void Refresh()
        {
            if(!Hud || !_item) return;
            string ch = Hud.ChapterId, kind = _item.kind.ToString();
            string nameKey = Loc.Has("ClueNames/" + kind) ? "ClueNames/" + kind : "Files/" + kind;
            string english = Loc.English(nameKey).ToUpperInvariant();
            // Paper = world object, never localized.
            _mast.text = Loc.English("Journal/" + ch + ".map.title");
            _dateLeft.text = Loc.English("Chapter/" + ch + ".card.place"); _dateMid.text = Loc.English("Chapter/" + ch + ".card.date"); _dateRight.text = "EVIDENCE";
            _headline.text = english; _photo.text = "[ PHOTO · " + english + " ]";
            _original.text = _item.description; _backNote.text = Loc.English("Chapter/" + ch + ".short") + "\n" + english;
            _front.SetActive(!_flipped); _back.SetActive(_flipped);
            // Overlay = localized reading of the same document.
            _name.text = Loc.T(nameKey);
            _transcript.SetActive(_showTranscript);
            _trTitle.text = _name.text; _trBody.text = Loc.Src(_item.description);
            string note = "Journal/" + ch + ".case.theory.v";
            _trNote.gameObject.SetActive(Loc.Has(note)); _trNote.text = "— " + Loc.T(note);
            _paper.localScale = Vector3.one * _zoom;
        }
    }
}
