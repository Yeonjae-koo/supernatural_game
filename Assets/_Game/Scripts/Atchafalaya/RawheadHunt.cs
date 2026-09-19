using UnityEngine;
namespace BeforeJericho
{
    /// <summary>Physical pursuer: ordinary hits buy time; only the chapter's live water circuit resolves it.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class RawheadHunt : MonoBehaviour
    {
        [SerializeField] HuntSession _session;
        [SerializeField] Transform _visual;
        [SerializeField] CharacterController _controller;
        [SerializeField] float _chaseSpeed = 2.6f, _huntSpeed = 2.3f, _attackInterval = 1.8f;
        [SerializeField] int _damage = 25;
        [SerializeField] Transform[] _chaseRoute = new Transform[0];
        float _stagger, _attack, _vertical; int _routeIndex; bool _chase;
        Vector3 _visualScale; Quaternion _visualRotation;
        public bool Active { get; private set; }
        public bool Resolved { get; private set; }
        public float Stagger => _stagger;
        public void Configure(HuntSession session, Transform visual, CharacterController controller)
        { _session = session; _visual = visual; _controller = controller; }
        public void SetChaseRoute(Transform[] route) { _chaseRoute = route ?? new Transform[0]; }
        void Awake()
        {
            if(!_controller) _controller = GetComponent<CharacterController>();
            if(!_session || !_visual) { Debug.LogError("Rawhead requires a HuntSession and visual child.", this); enabled = false; return; }
            _visualScale = _visual.localScale; _visualRotation = _visual.localRotation;
            ResetCreature(false);
        }
        public void Activate(Vector3 point, bool chase)
        {
            _controller.enabled = false;
            _controller.radius = chase ? .3f : .42f; _controller.height = chase ? .65f : 1.75f;
            _controller.center = Vector3.up * (_controller.height * .5f); _controller.stepOffset = chase ? .1f : .3f;
            transform.position = point; _controller.enabled = true;
            Active = true; Resolved = false; _chase = chase; _routeIndex = 0;
            _stagger = 2.5f; _attack = 2; _vertical = 0;
            _visual.gameObject.SetActive(true); _visual.localScale = Vector3.Scale(_visualScale, new Vector3(1, chase ? .35f : 1, 1)); _visual.localRotation = _visualRotation;
        }
        public void ResetCreature(bool resolved)
        {
            Active = false; Resolved = resolved; _stagger = _attack = _vertical = 0;
            if(_controller) _controller.enabled = false;
            if(_visual) { _visual.gameObject.SetActive(resolved); _visual.localScale = _visualScale; _visual.localRotation = resolved ? Quaternion.Euler(0, 0, 82) : _visualRotation; }
        }
        void Update()
        {
            if(!_session.Playing || !Active || Resolved) return;
            float dt = Time.deltaTime; _stagger = Mathf.Max(0, _stagger - dt); _attack -= dt;
            Vector3 destination = _session.player.transform.position;
            if(_chase && _routeIndex < _chaseRoute.Length)
            {
                if(!_chaseRoute[_routeIndex]) { _routeIndex++; return; }
                destination = _chaseRoute[_routeIndex].position;
                Vector3 routeDelta = destination - transform.position; routeDelta.y = 0;
                if(routeDelta.sqrMagnitude < .65f * .65f) _routeIndex++;
            }
            Vector3 delta = destination - transform.position; delta.y = 0;
            _vertical = _controller.isGrounded ? -2 : Mathf.Max(-20, _vertical - 20 * dt);
            Vector3 velocity = _stagger > 0 ? Vector3.zero : delta.normalized * (_chase ? _chaseSpeed : _huntSpeed);
            _controller.Move((velocity + Vector3.up * _vertical) * dt);
            if(delta.sqrMagnitude > .02f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta), dt * 8);
            if(_stagger > 0) return;
            Vector3 toPlayer = _session.player.transform.position - transform.position;
            if(toPlayer.sqrMagnitude < 1.7f * 1.7f && _attack <= 0 && !Physics.Linecast(transform.position + Vector3.up * .6f, _session.player.transform.position + Vector3.up * .6f, 1, QueryTriggerInteraction.Ignore))
            { _attack = _attackInterval; _session.Damage(_damage); if(_session.sound) _session.sound.PlayCue(48, .25f, .18f); }
        }
        public void Hit(bool iron)
        {
            if(!Active || Resolved || !_session.Playing) return;
            _stagger = Mathf.Max(_stagger, iron ? .65f : .8f);
            _session.Notify("It staggers, but the wound closes. Electricity is the only way to end this hunt.");
        }
        internal void ResolveByElectricity() { ResetCreature(true); }
    }
}
