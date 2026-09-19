using UnityEngine;
namespace BeforeJericho
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class GhoulHunt : MonoBehaviour
    {
        [SerializeField] HuntSession _session;
        [SerializeField] GhoulEncounterDirector _director;
        [SerializeField] int _id;
        [SerializeField] bool _boss;
        CharacterController _controller; Vector3 _spawn; Quaternion _rotation;
        float _attack, _stagger, _windup; bool _lunging, _initialized;
        public int Id => _id;
        public bool Boss => _boss;
        public int MaxHP => _boss ? 240 : 80;
        public int HP { get; private set; }
        public bool Active { get; private set; }
        public bool Dead => HP <= 0;
        public bool Staggered => _stagger > 0;
        public void Configure(HuntSession session, GhoulEncounterDirector director, int stableId, bool boss)
        { _session = session; _director = director; _id = stableId; _boss = boss; }
        void Awake() { Initialize(); }
        void Initialize() { if(_initialized) return; _initialized = true; _controller = GetComponent<CharacterController>(); _spawn = transform.position; _rotation = transform.rotation; HP = MaxHP; }
        public void ResetEnemy() { Initialize(); Restore(new GhoulSnapshot { id = _id, hp = MaxHP, x = _spawn.x, y = _spawn.y, z = _spawn.z }); }
        public void Activate() { if(Dead) return; Active = true; gameObject.SetActive(true); }
        public GhoulSnapshot Capture() => new GhoulSnapshot { id = _id, hp = HP, active = Active, x = transform.position.x, y = transform.position.y, z = transform.position.z };
        public void Restore(GhoulSnapshot s)
        {
            Initialize();
            _controller.enabled = false; transform.SetPositionAndRotation(new Vector3(s.x,s.y,s.z), _rotation); _controller.enabled = true;
            HP = s.hp; Active = s.active && HP > 0; _attack = 1.5f; _windup = _stagger = 0; _lunging = false;
            gameObject.SetActive(Active);
        }
        void Update()
        {
            if(!Active || Dead || !_session.Playing) return;
            _attack -= Time.deltaTime;
            if(_stagger > 0) { _stagger -= Time.deltaTime; return; }
            Vector3 target = _session.player.transform.position; Vector3 delta = target - transform.position; delta.y = 0;
            float distance = delta.magnitude; Vector3 direction = distance > .01f ? delta / distance : transform.forward;
            bool clear = !Physics.Linecast(transform.position + Vector3.up, target + Vector3.up * _session.player.InteractionHeight, 1, QueryTriggerInteraction.Ignore);
            if(_windup > 0)
            {
                _windup -= Time.deltaTime;
                if(_lunging && clear) _controller.Move(direction * (6 * Time.deltaTime));
                if(_windup <= 0) { if(clear && distance < 2) _session.Damage(_boss ? 28 : 15); _attack = _boss ? 1.7f : 2.1f; _lunging = false; }
                return;
            }
            if(distance < (_boss ? 4.5f : 1.65f) && clear && _attack <= 0)
            { _windup = _boss ? .65f : .5f; _lunging = _boss; _session.Notify(_boss ? "Gideon lunges — move sideways!" : "Ghoul winds up — back away or stagger its head!"); return; }
            if(distance > 1.3f && clear) _controller.Move((direction * (_boss ? 2.25f : 1.7f) + Vector3.down * 3) * Time.deltaTime);
            if(delta.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 8);
        }
        public void Hit(bool head, bool knife)
        {
            if(!Active || Dead || !_session.Playing) return;
            bool finishing = knife && Staggered;
            int damage = finishing ? (_boss ? 130 : 100) : knife ? 24 : head ? 60 : 16;
            HP = Mathf.Max(0, HP - damage);
            if(head || finishing) { _stagger = 2.2f; _windup = 0; _lunging = false; }
            _session.Notify(finishing ? "Knife finish." : head ? "Head hit — staggered! Close in with the knife [2]." : "Body hit. Aim for the head to conserve ammunition.");
            if(HP == 0) { Active = false; gameObject.SetActive(false); _director.EnemyKilled(this); }
        }
    }
}
