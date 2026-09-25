using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace BeforeJericho.UI
{
    /// <summary>HTML .opt row: label | ◀ value ▶, slider, fixed value, or remap key. ←/→ change the focused row.</summary>
    public sealed class OptionRow : Selectable, IMoveHandler, ISubmitHandler
    {
        public enum Kind { Cycle, Range, Fixed, Rebind }
        [SerializeField] TMP_Text _label, _value, _key, _fixedText;
        [SerializeField] Button _prev, _next, _keyButton;
        [SerializeField] Slider _slider;
        [SerializeField] GameObject _cycleGroup, _fixedGroup;
        [SerializeField] Graphic _focus;
        public Kind Type { get; private set; }
        public string Id { get; private set; }
        public System.Action<OptionRow> Focused, Remap;
        bool _wired;

        public void Bind(Kind kind, string id)
        {
            Type = kind; Id = id;
            if(!_wired)
            {
                _wired = true;
                _prev.onClick.AddListener(() => Step(-1)); _next.onClick.AddListener(() => Step(1));
                _slider.onValueChanged.AddListener(v => { if(Type == Kind.Range) GameSettings.SetRange(Id, Mathf.RoundToInt(v)); Focus(); });
                _keyButton.onClick.AddListener(() => Remap?.Invoke(this));
            }
            _cycleGroup.SetActive(kind == Kind.Cycle); _slider.gameObject.SetActive(kind == Kind.Range);
            _fixedGroup.SetActive(kind == Kind.Fixed); _keyButton.gameObject.SetActive(kind == Kind.Rebind);
            if(kind == Kind.Range) { var r = GameSettings.Ranges[id]; _slider.minValue = r.x; _slider.maxValue = r.y; _slider.wholeNumbers = true; }
            Render(null);
        }

        /// <summary>keyText: current binding for Rebind rows (or the "press key" prompt).</summary>
        public void Render(string keyText)
        {
            _label.text = Type == Kind.Rebind ? Loc.T("Settings/act." + Id) : Loc.T("Settings/" + Id);
            if(Type == Kind.Cycle) _value.text = Loc.T("Settings/v." + GameSettings.Choice(Id));
            else if(Type == Kind.Range) _slider.SetValueWithoutNotify(GameSettings.Range(Id));
            else if(Type == Kind.Fixed) _fixedText.text = Id == "move" ? "WASD" : Loc.T("Settings/v.en_fixed");
            else if(keyText != null) _key.text = keyText;
        }
        public void Waiting(bool on) { var c = on ? UIStyle.Accent : UIStyle.Bone; _key.color = c; _keyButton.targetGraphic.color = on ? UIStyle.Accent : UIStyle.Dim; }

        void Step(int d) { if(Type == Kind.Cycle) GameSettings.Cycle(Id, d); else if(Type == Kind.Range) GameSettings.SetRange(Id, GameSettings.Range(Id) + d * 5); Focus(); }
        void Focus() { if(EventSystem.current && EventSystem.current.currentSelectedGameObject != gameObject) EventSystem.current.SetSelectedGameObject(gameObject); }

        public override void OnPointerEnter(PointerEventData e) { base.OnPointerEnter(e); Focus(); }
        public override void OnSelect(BaseEventData e) { base.OnSelect(e); if(_focus) _focus.enabled = true; Focused?.Invoke(this); }
        public override void OnDeselect(BaseEventData e) { base.OnDeselect(e); if(_focus) _focus.enabled = false; }
        public override void OnMove(AxisEventData e)
        {
            if(e.moveDir == MoveDirection.Left || e.moveDir == MoveDirection.Right) { Step(e.moveDir == MoveDirection.Left ? -1 : 1); e.Use(); return; }
            base.OnMove(e);
        }
        public void OnSubmit(BaseEventData e) { if(Type == Kind.Cycle) Step(1); else if(Type == Kind.Rebind) Remap?.Invoke(this); }
    }
}
