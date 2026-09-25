using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace BeforeJericho.UI
{
    /// <summary>Options (HTML settings): 7 tabs, rows in the middle, description + live preview right. Every change applies at once.</summary>
    public sealed class SettingsView : UIView
    {
        [SerializeField] Button[] _tabs; [SerializeField] Graphic[] _tabMarks; [SerializeField] TMP_Text[] _tabLabels;
        [SerializeField] OptionRow _template;
        [SerializeField] TMP_Text _descTitle, _descBody;
        [SerializeField] GameObject _subtitlePreview, _brightnessPreview;
        [SerializeField] SubtitleView _preview;
        [SerializeField] Image[] _brightnessSwatches;
        static readonly string[] Tabs = { "gameplay", "controls", "camera", "display", "audio", "lang", "access" };
        static readonly Dictionary<string, string[]> TabOptions = new()
        {
            ["gameplay"] = new[]{"hud","hudop","prompts","tutorial","crosshair"}, ["camera"] = new[]{"sens","aimsens","invert","shake","blur","fov"},
            ["display"] = new[]{"mode","bright"}, ["audio"] = new[]{"master","dialogue","music","sfx","ambient"},
            ["lang"] = new[]{"lang","voice","subs","subSize","subBg","speaker","captions","direction"}, ["access"] = new[]{"hold","aimassist","flash","contrast","subSize","captions"},
        };
        // Remappable actions (HTML binds). Move is a WASD composite and stays fixed.
        public static readonly string[] Actions = { "move", "interact", "lighter", "aim", "attack", "reload", "crouch", "crawl", "journal", "map", "pause" };
        readonly List<OptionRow> _rows = new();
        int _tab = 5; string _focus; InputActionRebindingExtensions.RebindingOperation _rebind; OptionRow _waiting;

        void Awake()
        {
            _template.gameObject.SetActive(false);
            for(int i = 0; i < _tabs.Length; i++) { int t = i; _tabs[i].onClick.AddListener(() => SelectTab(t)); }
        }
        protected override void OnDisable() { base.OnDisable(); CancelRebind(); GameSettings.Flush(); }
        public void SelectTab(int tab) { CancelRebind(); _tab = tab; _focus = null; Build(); }
        public void Step(int d) => SelectTab((_tab + d + Tabs.Length) % Tabs.Length);
        protected override void Opened() { Build(); base.Opened(); if(_rows.Count > 0 && UnityEngine.EventSystems.EventSystem.current) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(_rows[0].gameObject); }
        public bool Rebinding => _rebind != null;

        void Build()
        {
            string tab = Tabs[_tab];
            string[] ids = tab == "controls" ? Actions : TabOptions[tab];
            while(_rows.Count < ids.Length) { var r = Instantiate(_template, _template.transform.parent); r.gameObject.SetActive(true); r.Focused = Focused; r.Remap = StartRebind; _rows.Add(r); }
            for(int i = 0; i < _rows.Count; i++)
            {
                bool on = i < ids.Length; _rows[i].gameObject.SetActive(on); if(!on) continue;
                string id = ids[i];
                var kind = tab == "controls" ? (id == "move" ? OptionRow.Kind.Fixed : OptionRow.Kind.Rebind) : id == "voice" ? OptionRow.Kind.Fixed : GameSettings.Ranges.ContainsKey(id) ? OptionRow.Kind.Range : OptionRow.Kind.Cycle;
                _rows[i].Bind(kind, id);
            }
            // Explicit vertical navigation through the visible rows.
            for(int i = 0; i < ids.Length; i++) { var n = _rows[i].navigation; n.mode = Navigation.Mode.Explicit; n.selectOnUp = _rows[(i - 1 + ids.Length) % ids.Length]; n.selectOnDown = _rows[(i + 1) % ids.Length]; _rows[i].navigation = n; }
            Refresh();
        }

        public override void Refresh()
        {
            if(!Hud || _rows.Count == 0) return;
            string tab = Tabs[_tab];
            for(int i = 0; i < _tabs.Length; i++) { bool on = i == _tab; _tabMarks[i].enabled = on; _tabLabels[i].color = on ? UIStyle.Bone : UIStyle.Ash; }
            foreach(var r in _rows) if(r.gameObject.activeSelf) r.Render(r.Type == OptionRow.Kind.Rebind ? (r == _waiting ? Loc.T("Settings/remap.press") : Hud.KeyName(ActionName(r.Id))) : null);
            _descTitle.text = _focus != null ? (tab == "controls" ? Loc.T("Settings/act." + _focus) : Loc.T("Settings/" + _focus)) : Loc.T("Settings/tab." + tab);
            _descBody.text = tab == "controls" ? Loc.T("Settings/desc.controls") : _focus != null && Loc.Has("Settings/desc." + _focus) ? Loc.T("Settings/desc." + _focus) : Loc.T("Settings/desc.default");
            _subtitlePreview.SetActive(tab == "lang" || tab == "gameplay");
            _preview.Set("Captions/scratch", -1, Loc.T("Dialogue/speaker.dean") + ": " + Loc.T("Dialogue_Sample/dean.wall"), true);
            _brightnessPreview.SetActive(tab == "display");
            float b = GameSettings.Range("bright") / 50f;
            float[] baseValues = { 0x0b / 255f, 0x1c / 255f, 0x55 / 255f };
            for(int i = 0; i < _brightnessSwatches.Length; i++) { float v = Mathf.Clamp01(baseValues[i] * b); _brightnessSwatches[i].color = new Color(v, v, v); }
        }

        void Focused(OptionRow row) { if(_focus == row.Id) return; _focus = row.Id; Refresh(); }

        static string ActionName(string id) => id switch { "interact" => "Interact", "lighter" => "Lighter", "aim" => "Aim", "attack" => "Attack", "reload" => "Reload", "crouch" => "Crouch", "crawl" => "Crawl", "journal" => "Journal", "map" => "Map", "pause" => "Pause", _ => "" };

        void StartRebind(OptionRow row)
        {
            CancelRebind();
            var action = Hud.session.player.Input.Action(ActionName(row.Id)); if(action == null) return;
            bool wasEnabled = action.enabled; action.Disable();
            _waiting = row; row.Waiting(true); Refresh();
            _rebind = action.PerformInteractiveRebinding(0).WithControlsExcluding("<Mouse>/position").WithControlsExcluding("<Mouse>/delta").WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(.05f)
                .OnComplete(op => Finish(action, wasEnabled, true)).OnCancel(op => Finish(action, wasEnabled, false)).Start();
        }

        void Finish(InputAction action, bool wasEnabled, bool applied)
        {
            _rebind?.Dispose(); _rebind = null;
            if(wasEnabled) action.Enable();
            if(applied) { Hud.session.player.Input.SaveBindings(); Hud.BindingsChanged(); }
            if(_waiting) _waiting.Waiting(false); _waiting = null; Refresh();
        }
        void CancelRebind() { if(_rebind != null) _rebind.Cancel(); }
    }

}
