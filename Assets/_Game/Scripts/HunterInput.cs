using UnityEngine;
using UnityEngine.InputSystem;
namespace BeforeJericho
{
    public sealed class HunterInput : MonoBehaviour
    {
        InputActionMap _game, _ui;
        InputAction _move, _look, _sprint, _aim, _fire, _interact, _reload, _lighter, _one, _two, _three, _four, _pause, _journal, _crouch, _crawl;
        public Vector2 Move => _move.ReadValue<Vector2>();
        public Vector2 Look => _look.ReadValue<Vector2>();
        public bool Crouch => _crouch.WasPressedThisFrame();
        public bool Crawl => _crawl.WasPressedThisFrame();
        public bool Sprint => _sprint.IsPressed();
        public bool Aim => _aim.IsPressed();
        public bool Fire => _fire.WasPressedThisFrame();
        public bool Interact => _interact.WasPressedThisFrame();
        public bool HoldInteract => _interact.IsPressed();
        public bool Reload => _reload.WasPressedThisFrame();
        public bool Lighter => _lighter.WasPressedThisFrame();
        public int Equip => _one.WasPressedThisFrame() ? 0 : _two.WasPressedThisFrame() ? 1 : _three.WasPressedThisFrame() ? 2 : _four.WasPressedThisFrame() ? 3 : -1;
        public bool Pause => _pause.WasPressedThisFrame();
        public bool Journal => _journal.WasPressedThisFrame();
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
            _pause = _ui.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            _journal = _ui.AddAction("Journal", InputActionType.Button, "<Keyboard>/tab");
        }
        InputAction Button(string id, string binding) => _game.AddAction(id, InputActionType.Button, binding);
        void OnEnable() { _game.Enable(); _ui.Enable(); }
        void OnDisable() { _game.Disable(); _ui.Disable(); }
        public void SetGameplay(bool enabled) { if(enabled) _game.Enable(); else _game.Disable(); }
        void OnDestroy() { _game.Dispose(); _ui.Dispose(); }
    }
}
