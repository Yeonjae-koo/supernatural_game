using UnityEngine;
using UnityEngine.InputSystem;
namespace BeforeJericho
{
    public sealed class HunterInput : MonoBehaviour
    {
        InputActionMap _game, _ui;
        InputAction _move, _look, _sprint, _aim, _fire, _interact, _reload, _lighter, _one, _two, _three, _four, _pause, _journal, _crouch, _crawl;
        InputAction _aimToggle, _debugHit, _map, _tabPrev, _tabNext, _transcript, _skip, _confirm, _zoom, _scroll;
        bool _aimLatched;
        public Vector2 Move => _move.ReadValue<Vector2>();
        public Vector2 Look => _look.ReadValue<Vector2>();
        public bool Crouch => _crouch.WasPressedThisFrame();
        public bool Crawl => _crawl.WasPressedThisFrame();
        public bool Sprint => _sprint.IsPressed();
        // Hold (default) or Toggle per options; Q toggles in either mode.
        public bool Aim => _aimLatched || (!AimToggleMode && _aim.IsPressed());
        public bool Fire => _fire.WasPressedThisFrame();
        public bool Interact => _interact.WasPressedThisFrame();
        public bool HoldInteract => _interact.IsPressed();
        public bool Reload => _reload.WasPressedThisFrame();
        public bool Lighter => _lighter.WasPressedThisFrame();
        public int Equip => _one.WasPressedThisFrame() ? 0 : _two.WasPressedThisFrame() ? 1 : _three.WasPressedThisFrame() ? 2 : _four.WasPressedThisFrame() ? 3 : -1;
        public bool Pause => _pause.WasPressedThisFrame();
        public bool Journal => _journal.WasPressedThisFrame();
        // UI map (always enabled; consumed by HuntHUD according to the open screen).
        public bool Map => _map.WasPressedThisFrame();
        public bool TabPrevious => _tabPrev.WasPressedThisFrame();
        public bool TabNext => _tabNext.WasPressedThisFrame();
        public bool Transcript => _transcript.WasPressedThisFrame();
        public bool Skip => _skip.WasPressedThisFrame();
        public bool Confirm => _confirm.WasPressedThisFrame();
        public float Zoom => _zoom.ReadValue<float>();
        public float ZoomWheel => _scroll.ReadValue<float>();
        /// <summary>Development-only hit preview (HTML "H"): HuntHUD ignores it in shipping builds.</summary>
        public bool DebugHit => _debugHit.WasPressedThisFrame();
        static bool AimToggleMode => UI.GameSettings.Choice("hold") == "toggle";
        void Awake()
        {
            _game = new InputActionMap("Gameplay"); _ui = new InputActionMap("UI");
            _move = _game.AddAction("Move", InputActionType.Value);
            _move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            _look = _game.AddAction("Look", InputActionType.Value, "<Mouse>/delta");
            _crouch = Button("Crouch", "<Keyboard>/leftCtrl"); _crouch.AddBinding("<Keyboard>/c");
            _crawl = Button("Crawl", "<Keyboard>/z");
            _sprint = Button("Sprint", "<Keyboard>/leftShift"); _aim = Button("Aim", "<Mouse>/rightButton");
            _fire = Button("Attack", "<Mouse>/leftButton"); _interact = Button("Interact", "<Keyboard>/e");
            _reload = Button("Reload", "<Keyboard>/r"); _lighter = Button("Lighter", "<Keyboard>/f");
            _one = Button("Handgun", "<Keyboard>/1"); _two = Button("Knife", "<Keyboard>/2"); _three = Button("Iron", "<Keyboard>/3"); _four = Button("Salt", "<Keyboard>/4");
            _aimToggle = Button("AimToggle", "<Keyboard>/q"); _debugHit = Button("DebugHit", "<Keyboard>/h");
            _pause = _ui.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            _journal = _ui.AddAction("Journal", InputActionType.Button, "<Keyboard>/tab");
            _map = _ui.AddAction("Map", InputActionType.Button, "<Keyboard>/m");
            _tabPrev = _ui.AddAction("TabPrevious", InputActionType.Button, "<Keyboard>/q");
            _tabNext = _ui.AddAction("TabNext", InputActionType.Button, "<Keyboard>/e");
            _transcript = _ui.AddAction("Transcript", InputActionType.Button, "<Keyboard>/r");
            _skip = _ui.AddAction("Skip", InputActionType.Button, "<Keyboard>/space");
            _confirm = _ui.AddAction("Confirm", InputActionType.Button, "<Keyboard>/enter"); _confirm.AddBinding("<Keyboard>/numpadEnter");
            _zoom = _ui.AddAction("Zoom", InputActionType.Value);
            _zoom.AddCompositeBinding("1DAxis").With("Positive", "<Keyboard>/upArrow").With("Negative", "<Keyboard>/downArrow");
            _scroll = _ui.AddAction("ZoomWheel", InputActionType.Value, "<Mouse>/scroll/y");
            LoadBindings();
        }
        InputAction Button(string id, string binding) => _game.AddAction(id, InputActionType.Button, binding);
        void OnEnable() { _game.Enable(); _ui.Enable(); }
        void OnDisable() { _game.Disable(); _ui.Disable(); }
        void Update()
        {
            if(!_game.enabled) { _aimLatched = false; return; }
            if(_aimToggle.WasPressedThisFrame() || (AimToggleMode && _aim.WasPressedThisFrame())) _aimLatched = !_aimLatched;
            if(!AimToggleMode && _aim.WasReleasedThisFrame()) _aimLatched = false;
        }
        public void SetGameplay(bool enabled) { if(enabled) _game.Enable(); else { _game.Disable(); _aimLatched = false; } }
        public InputAction Action(string name) => _game.FindAction(name) ?? _ui.FindAction(name);

        // Remapped keys persist as "Action#index=path" lines. Bindings are built in code, so their GUIDs change every
        // Awake and the Input System's id-based override JSON would never match again.
        void LoadBindings()
        {
            string saved = UI.GameSettings.BindingOverrides;
            if(string.IsNullOrEmpty(saved)) return;
            foreach(string line in saved.Split('\n'))
            {
                int hash = line.IndexOf('#'), eq = line.IndexOf('=');
                if(hash <= 0 || eq <= hash || !int.TryParse(line.Substring(hash + 1, eq - hash - 1), out int index)) continue;
                var action = Action(line.Substring(0, hash));
                if(action != null && index < action.bindings.Count) action.ApplyBindingOverride(index, line.Substring(eq + 1));
            }
        }
        public void SaveBindings()
        {
            var sb = new System.Text.StringBuilder();
            foreach(var map in new[] { _game, _ui })
                foreach(var action in map.actions)
                    for(int i = 0; i < action.bindings.Count; i++)
                        if(!string.IsNullOrEmpty(action.bindings[i].overridePath)) sb.Append(action.name).Append('#').Append(i).Append('=').Append(action.bindings[i].overridePath).Append('\n');
            UI.GameSettings.BindingOverrides = sb.ToString();
        }
        void OnDestroy() { _game.Dispose(); _ui.Dispose(); }
    }
}
