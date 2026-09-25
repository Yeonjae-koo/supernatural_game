using TMPro;
using UnityEngine;
namespace BeforeJericho.UI
{
    /// <summary>Static label bound to a String Table key; re-renders immediately when the language (or key view) changes.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocText : MonoBehaviour
    {
        [SerializeField] string _key;
        [SerializeField] string _prefix, _suffix;
        TMP_Text _text;
        public string Key { get => _key; set { _key = value; Render(); } }
        void OnEnable() { Loc.Changed += Render; Render(); }
        void OnDisable() => Loc.Changed -= Render;
        public void Render()
        {
            if(!_text) _text = GetComponent<TMP_Text>();
            if(!string.IsNullOrEmpty(_key) && Loc.Loaded) _text.text = _prefix + Loc.T(_key) + _suffix;
        }
    }
}
