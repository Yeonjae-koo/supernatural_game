using TMPro;
using UnityEngine;
namespace BeforeJericho.UI
{
    /// <summary>Credits roll (HTML .cred): centred column scrolling bottom → top over 26 s, looping. Esc returns.</summary>
    public sealed class CreditsView : UIView
    {
        [SerializeField] RectTransform _roll, _viewport;
        [SerializeField] TMP_Text _logo;
        public TMP_Text Logo => _logo;
        float _startedAt;
        protected override void Opened() { _startedAt = Time.unscaledTime; base.Opened(); }
        public override void Tick()
        {
            float h = _viewport.rect.height, total = h + _roll.rect.height;
            float u = (Time.unscaledTime - _startedAt) % 26f / 26f;
            _roll.anchoredPosition = new Vector2(0, -h + u * total);
        }
    }
}
