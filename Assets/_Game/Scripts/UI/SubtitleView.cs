using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace BeforeJericho.UI
{
    /// <summary>HTML .subs: optional [caption] line (with ◀ ▶ direction) above the speaker + dialogue line.
    /// Size, background, speaker names, captions and direction follow the options immediately.</summary>
    public sealed class SubtitleView : MonoBehaviour
    {
        [SerializeField] TMP_Text _caption, _speech;
        [SerializeField] Image _captionBack, _speechBack;
        [SerializeField] Material _plain, _shadow;
        [SerializeField] bool _preview; // settings/boot previews ignore the Subtitles on/off option
        [SerializeField] float _maxWidth = 963; // 62cqw minus the line padding
        static readonly Regex Speaker = new(@"^([^\s:\[][^:\n]{0,20}):\s+(.+)$");
        string _captionKey; int _direction; string _lines; bool _speakers;
        static readonly float[] Sizes = { 16.8f, 20.8f, 26.4f }; // 1.05 / 1.3 / 1.65 cqw at 1600

        void OnEnable() { GameSettings.Changed += Render; Loc.Changed += Render; Render(); }
        void OnDisable() { GameSettings.Changed -= Render; Loc.Changed -= Render; }

        /// <summary>captionKey: Captions/* row or null. direction: -1 left, 1 right, 0 none. lines: localized text;
        /// with speakers, "Speaker: text" lines render the name as a tag (Dialogue rows only — clue labels also use colons).</summary>
        public void Set(string captionKey, int direction, string lines, bool speakers)
        {
            if(_captionKey == captionKey && _direction == direction && _lines == lines && _speakers == speakers) return;
            _captionKey = captionKey; _direction = direction; _lines = lines; _speakers = speakers; Render();
        }
        public bool HasContent => !string.IsNullOrEmpty(_lines) || !string.IsNullOrEmpty(_captionKey);

        void Render()
        {
            bool subs = _preview || GameSettings.On("subs"), bg = GameSettings.On("subBg");
            int size = System.Array.IndexOf(GameSettings.Options["subSize"], GameSettings.Choice("subSize"));
            float px = Sizes[Mathf.Clamp(size, 0, 2)];
            bool showCaption = subs && GameSettings.On("captions") && !string.IsNullOrEmpty(_captionKey);
            string arrow = GameSettings.On("direction") && _direction != 0 ? "<color=#8aa3a6>" + (_direction < 0 ? "◀" : "▶") + "</color><space=0.5em>" : "";
            Line(_caption, _captionBack, showCaption, showCaption ? arrow + "[" + Loc.T(_captionKey) + "]" : "", px, bg);
            bool showSpeech = subs && !string.IsNullOrEmpty(_lines);
            Line(_speech, _speechBack, showSpeech, showSpeech ? (_speakers ? Format(_lines) : _lines) : "", px, bg);
        }

        void Line(TMP_Text text, Image back, bool on, string value, float px, bool bg)
        {
            back.gameObject.SetActive(on); if(!on) return;
            text.text = value; text.fontSize = px;
            // Box hugs the wrapped text (HTML inline line boxes) but never exceeds the safe width.
            var le = text.GetComponent<LayoutElement>(); if(le) le.preferredWidth = Mathf.Min(text.GetPreferredValues(value, _maxWidth, 0).x + 1, _maxWidth);
            back.enabled = bg; text.fontSharedMaterial = bg ? _plain : _shadow;
        }

        static string Format(string lines)
        {
            bool names = GameSettings.On("speaker");
            var sb = new StringBuilder();
            foreach(string line in lines.Split('\n'))
            {
                if(sb.Length > 0) sb.Append('\n');
                var m = Speaker.Match(line);
                if(!m.Success) { sb.Append(line); continue; }
                if(names) sb.Append("<color=#8aa3a6><b><cspace=0.04em>").Append(m.Groups[1].Value).Append("</cspace></b></color><space=0.7em>");
                sb.Append(m.Groups[2].Value);
            }
            return sb.ToString();
        }
    }
}
