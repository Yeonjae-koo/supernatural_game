using UnityEngine;
namespace BeforeJericho
{
    // The building itself: manifestations, salt lines and lockdown throws. Fixed pools, no runtime spawning.
    public sealed class PoltergeistHunt : MonoBehaviour
    {
        const int MaxSalt = 3; const float SaltSeconds = 14, SaltHalfWidth = 1.2f, SaltBand = .35f;
        [SerializeField] HuntSession _session;
        [SerializeField] ManifestationHunt[] _manifestations;
        [SerializeField] Transform[] _saltLines, _throwSources, _projectiles;
        float[] _saltUntil, _projectileTimer; Vector3[] _projectileVelocity; int[] _projectileState;
        float _throwTimer; int _nextSource; bool _lockdown;
        public ManifestationHunt[] Manifestations => _manifestations;
        public int SaltCharges { get; private set; } = MaxSalt;
        public int ActiveCount { get { int n = 0; foreach(var m in _manifestations) if(m.Active) n++; return n; } }
        public bool Lockdown => _lockdown;
        public void Configure(HuntSession session, ManifestationHunt[] manifestations, Transform[] saltLines, Transform[] throwSources, Transform[] projectiles)
        { _session = session; _manifestations = manifestations; _saltLines = saltLines; _throwSources = throwSources; _projectiles = projectiles; }
        void Awake()
        {
            _saltUntil = new float[_saltLines.Length];
            _projectileTimer = new float[_projectiles.Length]; _projectileVelocity = new Vector3[_projectiles.Length]; _projectileState = new int[_projectiles.Length];
        }
        // Restores the whole haunt from chapter state; clears transient lines and throws.
        public void Apply(int manifestations, bool lockdown, int first = 0)
        {
            if(_saltUntil == null) Awake();
            for(int i = 0; i < _manifestations.Length; i++) { if(i >= first && i < first + manifestations) _manifestations[i].Activate(); else _manifestations[i].Deactivate(); }
            for(int i = 0; i < _saltLines.Length; i++) { _saltUntil[i] = 0; _saltLines[i].gameObject.SetActive(false); }
            for(int i = 0; i < _projectiles.Length; i++) { _projectileState[i] = 0; _projectiles[i].gameObject.SetActive(false); }
            SaltCharges = MaxSalt; _lockdown = lockdown; _throwTimer = 3; _nextSource = 0;
        }
        public void PourSalt(Vector3 feet, Vector3 forward)
        {
            if(SaltCharges == 0) { _session.Notify("Out of salt. Iron [3] still works."); return; }
            int slot = 0;
            for(int i = 1; i < _saltLines.Length; i++) if(_saltUntil[i] < _saltUntil[slot]) slot = i;
            forward.y = 0; if(forward.sqrMagnitude < .01f) forward = Vector3.forward;
            _saltLines[slot].SetPositionAndRotation(feet + forward.normalized * 1.1f + Vector3.up * .03f, Quaternion.LookRotation(forward));
            _saltLines[slot].gameObject.SetActive(true); _saltUntil[slot] = Time.time + SaltSeconds; SaltCharges--;
            _session.Notify("Salt line poured. It will hold for a little while. " + SaltCharges + " left.");
        }
        // A salt line is a 2.4 m segment: nothing may cross it or step into the thin band in front of it. Its ends can be walked around.
        public bool SaltBlocks(Vector3 from, Vector3 to)
        {
            for(int i = 0; i < _saltLines.Length; i++)
            {
                var line = _saltLines[i];
                if(!line.gameObject.activeSelf) continue;
                Quaternion inverse = Quaternion.Inverse(line.rotation);
                Vector3 a = inverse * (from - line.position), b = inverse * (to - line.position);
                if(Mathf.Abs(b.x) > SaltHalfWidth + SaltBand) continue;
                bool crosses = a.z * b.z < 0, entersBand = Mathf.Abs(b.z) < SaltBand && Mathf.Abs(b.z) < Mathf.Abs(a.z);
                if(crosses || entersBand) return true;
            }
            return false;
        }
        public void Melee(Vector3 origin, Vector3 forward, bool iron)
        {
            ManifestationHunt closest = null; float best = 3f;
            foreach(var m in _manifestations)
            {
                if(!m.Active || m.Dispersed) continue;
                Vector3 delta = m.transform.position - origin;
                if(delta.magnitude >= best || Vector3.Dot(forward, delta.normalized) < .15f || Physics.Linecast(origin, m.transform.position, 1, QueryTriggerInteraction.Ignore)) continue;
                closest = m; best = delta.magnitude;
            }
            if(closest) closest.Hit(iron);
        }
        void Update()
        {
            if(!_session.Playing) return;
            for(int i = 0; i < _saltLines.Length; i++) if(_saltLines[i].gameObject.activeSelf && Time.time > _saltUntil[i]) _saltLines[i].gameObject.SetActive(false);
            if(_lockdown && !_session.Resolved && (_throwTimer -= Time.deltaTime) <= 0) Throw();
            Vector3 dean = _session.player.transform.position + Vector3.up * _session.player.InteractionHeight;
            for(int i = 0; i < _projectiles.Length; i++)
            {
                if(_projectileState[i] == 0) continue;
                var p = _projectiles[i]; _projectileTimer[i] -= Time.deltaTime;
                if(_projectileState[i] == 1)
                {
                    // Telegraph: the object lifts and shakes before it flies.
                    p.position += Vector3.up * (.6f * Time.deltaTime); p.Rotate(0, 400 * Time.deltaTime, 0);
                    if(_projectileTimer[i] <= 0) { _projectileState[i] = 2; _projectileTimer[i] = 2; _projectileVelocity[i] = (dean - p.position).normalized * 11; }
                    continue;
                }
                p.position += _projectileVelocity[i] * Time.deltaTime; p.Rotate(720 * Time.deltaTime, 0, 0);
                bool hit = Vector3.Distance(p.position, dean) < .85f;
                if(hit) { _session.Damage(12); if(_session.sound) _session.sound.PlayCue(95, .15f, .2f); }
                if(hit || _projectileTimer[i] <= 0 || Physics.CheckSphere(p.position, .15f, 1, QueryTriggerInteraction.Ignore)) { _projectileState[i] = 0; p.gameObject.SetActive(false); }
            }
        }
        void Throw()
        {
            _throwTimer = 4f;
            for(int i = 0; i < _projectiles.Length; i++)
            {
                if(_projectileState[i] != 0) continue;
                var source = _throwSources[_nextSource++ % _throwSources.Length];
                _projectiles[i].SetPositionAndRotation(source.position, source.rotation); _projectiles[i].gameObject.SetActive(true);
                _projectileState[i] = 1; _projectileTimer[i] = .9f;
                _session.Notify("Something lifts off the shelf — move!");
                return;
            }
        }
    }
}
