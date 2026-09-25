using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace BeforeJericho.UI
{
    /// <summary>One UI screen prefab. HuntHUD owns which view is visible; the view renders its own content.</summary>
    public abstract class UIView : MonoBehaviour
    {
        [SerializeField] protected Selectable _first;
        protected HuntHUD Hud { get; private set; }
        public void Bind(HuntHUD hud) => Hud = hud;
        public bool Visible => gameObject.activeSelf;
        public void Show(bool on)
        {
            if(gameObject.activeSelf == on) return;
            gameObject.SetActive(on);
            if(on) Opened();
        }
        protected virtual void Opened()
        {
            Refresh();
            if(_first && EventSystem.current) EventSystem.current.SetSelectedGameObject(_first.gameObject);
        }
        /// <summary>Re-render text/settings-dependent content; also called on language and option changes.</summary>
        public virtual void Refresh() { }
        /// <summary>Per-frame update while visible.</summary>
        public virtual void Tick() { }
        protected virtual void OnEnable() { Loc.Changed += Refresh; GameSettings.Changed += Refresh; }
        protected virtual void OnDisable() { Loc.Changed -= Refresh; GameSettings.Changed -= Refresh; }
    }
}
