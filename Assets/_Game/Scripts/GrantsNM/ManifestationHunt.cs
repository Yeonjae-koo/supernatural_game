using UnityEngine;
namespace BeforeJericho
{
    // Poltergeist-made shape. It has no HP: iron scatters it, salt stops it, only the anchor burn ends it.
    public sealed class ManifestationHunt : MonoBehaviour
    {
        [SerializeField] HuntSession _session;
        [SerializeField] PoltergeistHunt _poltergeist;
        [SerializeField] GameObject _visual;
        Vector3 _home; float _dispersed, _attack; bool _initialized;
        public bool Active { get; private set; }
        public bool Dispersed => _dispersed > 0;
        public bool Blocked { get; private set; }
        public void Configure(HuntSession session, PoltergeistHunt poltergeist, GameObject visual) { _session = session; _poltergeist = poltergeist; _visual = visual; }
        void Awake() { Initialize(); }
        void Initialize() { if(_initialized) return; _initialized = true; _home = transform.position; }
        public void Activate()
        {
            Initialize(); Active = true; transform.position = _home; Blocked = false;
            _dispersed = 1.2f; _attack = 2; _visual.SetActive(false); gameObject.SetActive(true);
        }
        public void Deactivate() { Initialize(); Active = false; _dispersed = 0; Blocked = false; transform.position = _home; gameObject.SetActive(false); }
        void Update()
        {
            if(!Active || !_session.Playing) return;
            _attack -= Time.deltaTime;
            if(_dispersed > 0)
            {
                _dispersed -= Time.deltaTime;
                if(_dispersed <= 0) { transform.position = _home; _visual.SetActive(true); }
                return;
            }
            Vector3 destination = _session.player.transform.position + Vector3.up * 1.1f;
            Vector3 next = Vector3.MoveTowards(transform.position, destination, _session.definition.ghostSpeed * Time.deltaTime);
            Blocked = _poltergeist.SaltBlocks(transform.position, next);
            if(!Blocked) transform.position = next;
            _visual.transform.localPosition = Vector3.up * Mathf.Sin(Time.time * 3.1f) * .1f;
            if(!Blocked && Vector3.Distance(transform.position, destination) < 1.6f && _attack <= 0)
            { _attack = _session.definition.attackInterval; _session.Damage(_session.definition.ghostDamage); if(_session.sound) _session.sound.PlayCue(55, .3f, .2f); }
        }
        public void Hit(bool iron)
        {
            if(!Active || Dispersed || !_session.Playing) return;
            if(!iron) { _session.Notify("It flickers around the hit. Nothing there to wound."); return; }
            _dispersed = _session.definition.ironDisperseSeconds; _visual.SetActive(false);
            _session.Notify("Iron scatters it. It will re-form. Move.");
        }
    }
}
