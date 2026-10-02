using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace BeforeJericho.UI
{
    /// <summary>HTML .mbtn: ash text; bone + accent dash on the one selected (hover selects, like the prototype); dim when disabled.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class MenuButton : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] TMP_Text _label, _small;
        [SerializeField] Graphic _dash;
        Button _button; bool _selected;
        public Button Button => _button ? _button : _button = GetComponent<Button>();
        public TMP_Text Label => _label;
        public TMP_Text Small => _small;
        void OnEnable() { _selected = EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject; Paint(); }
        public void OnPointerEnter(PointerEventData e) { if(Button.interactable && EventSystem.current) EventSystem.current.SetSelectedGameObject(gameObject); }
        public void OnSelect(BaseEventData e) { _selected = true; Paint(); }
        public void OnDeselect(BaseEventData e) { _selected = false; Paint(); }
        public void SetInteractable(bool value) { Button.interactable = value; Paint(); }
        public void SetSmall(string text) { if(!_small) return; _small.text = text; _small.gameObject.SetActive(!string.IsNullOrEmpty(text)); }
        void Paint()
        {
            bool on = Button.interactable && _selected;
            _label.color = !Button.interactable ? UIStyle.Dim : on ? UIStyle.Bone : UIStyle.Ash;
            if(_dash) _dash.enabled = on;
        }
    }
}
