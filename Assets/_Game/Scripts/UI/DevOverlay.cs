using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace BeforeJericho.UI
{
    /// <summary>Prototype tooling (HTML "UI 영역 가이드", "String Key 보기", UX memo + string list) for the Editor and Development builds.
    /// F1 zone guides, F2 key view, F3 memo/strings. Destroyed at start in release (non-development) players.</summary>
    public sealed class DevOverlay : MonoBehaviour
    {
        [SerializeField] RectTransform _guides; [SerializeField] RectTransform _guideTemplate;
        [SerializeField] GameObject _panel; [SerializeField] TMP_Text _memo, _strings, _hint;
        HuntHUD _hud; bool _showGuides, _showPanel; string _context, _last, _listed;
        readonly StringBuilder _sb = new();
        // Editor and Development builds only; a release player removes the overlay on start.
        public void Bind(HuntHUD hud) { if(!Debug.isDebugBuild) { Destroy(gameObject); return; } _hud = hud; _guideTemplate.gameObject.SetActive(false); _panel.SetActive(false); }
        void Update()
        {
            var k = Keyboard.current;
            if(k != null)
            {
                if(k.f1Key.wasPressedThisFrame) _showGuides = !_showGuides;
                if(k.f2Key.wasPressedThisFrame) { Loc.ShowKeys = !Loc.ShowKeys; Loc.NotifyChanged(); }
                if(k.f3Key.wasPressedThisFrame) { _showPanel = !_showPanel; _panel.SetActive(_showPanel); _last = null; }
            }
            if(!_hud) return;
            _context = _hud.ScreenId;
            // New screen: restart the used-key list and re-render so static labels register again.
            if(_showPanel && _context != _listed) { _listed = _context; Loc.Used.Clear(); Loc.NotifyChanged(); }
            Guides();
            if(_showPanel && Time.frameCount % 15 == 0) Panel();
        }

        void Guides()
        {
            bool on = _showGuides && DevMemo.Guides.ContainsKey(_context);
            _guides.gameObject.SetActive(on); if(!on || _last == _context + "g") return;
            _last = _context + "g";
            for(int i = _guides.childCount - 1; i >= 0; i--) if(_guides.GetChild(i) != _guideTemplate) Destroy(_guides.GetChild(i).gameObject);
            foreach(string id in DevMemo.Guides[_context])
            {
                var (x, y, w, h, label) = DevMemo.Zones[id];
                var g = Instantiate(_guideTemplate, _guides); g.gameObject.SetActive(true);
                g.anchorMin = new Vector2(x / 100, 1 - (y + h) / 100); g.anchorMax = new Vector2((x + w) / 100, 1 - y / 100); g.offsetMin = g.offsetMax = Vector2.zero;
                g.GetComponentInChildren<TMP_Text>().text = label;
            }
        }

        void Panel()
        {
            if(!DevMemo.Screens.TryGetValue(_context, out var spec)) return;
            _sb.Clear().Append("<b>").Append(spec.title).Append("</b>\n").Append(spec.lead).Append("\n\n");
            foreach(var z in spec.zones) _sb.Append("<color=#d9d8d2>").Append(z[0]).Append("</color>  ").Append(z[1]).Append('\n');
            _memo.text = _sb.ToString();
            _sb.Clear().Append("<color=#595d5d>이 화면에서 쓰는 String Table 항목 (").Append(Loc.Used.Count).Append(")</color>\n");
            foreach(string key in Loc.Used)
            {
                if(!Loc.Has(key)) continue;
                string ko = Loc.Column(key, true), en = Loc.Column(key, false);
                _sb.Append("<color=#b8433a>").Append(key).Append("</color>  ").Append(string.IsNullOrEmpty(ko) ? "(ko 전용)" : ko).Append("  <color=#8e918e>").Append(string.IsNullOrEmpty(en) ? "— 숨김" : en).Append("</color>\n");
            }
            _strings.text = _sb.ToString();
        }

    }
}
