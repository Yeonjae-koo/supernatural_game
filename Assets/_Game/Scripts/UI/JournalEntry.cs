using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace BeforeJericho.UI
{
    /// <summary>HTML .clist li / .slot: icon, name, description; selected = faint fill + accent left edge.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class JournalEntry : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        [SerializeField] TMP_Text _icon, _name, _detail, _count;
        [SerializeField] Graphic _selectedFill, _selectedEdge;
        [SerializeField] Image _iconImage; // ✦ is missing from the bundled fonts: drawn as a sprite
        Button _button;
        public Button Button => _button ? _button : _button = GetComponent<Button>();
        public System.Action<JournalEntry> Selected;
        [System.NonSerialized] public object Data;
        public void OnPointerEnter(PointerEventData e) { if(EventSystem.current) EventSystem.current.SetSelectedGameObject(gameObject); }
        public void OnSelect(BaseEventData e) => Selected?.Invoke(this);
        public void Set(string icon, string name, string detail, bool found, string count = null)
        {
            bool star = icon == "✦";
            if(_iconImage) _iconImage.enabled = star;
            if(_icon) { _icon.text = star ? "" : icon; _icon.color = found ? UIStyle.Cold : UIStyle.Dim; }
            if(_name) { _name.text = name; _name.color = found ? UIStyle.Bone : UIStyle.Dim; }
            if(_detail) { _detail.text = detail; _detail.gameObject.SetActive(!string.IsNullOrEmpty(detail)); }
            if(_count) _count.text = count ?? "";
        }
        public void Highlight(bool on) { if(_selectedFill) _selectedFill.enabled = on; if(_selectedEdge) _selectedEdge.enabled = on; }
    }
}
