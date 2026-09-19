using UnityEngine;
namespace BeforeJericho
{
    public sealed class GhostHunt : MonoBehaviour
    {
        public HuntSession session; public Transform apparition; public Light glow;
        Vector3 _home; float _stagger, _attack;
        public bool Active { get; private set; }
        public int IronHits { get; private set; }
        public float Stagger => _stagger;
        void Awake() { _home = transform.position; }
        public void Activate(Vector3 point) { Active = true; apparition.gameObject.SetActive(true); transform.position = point; _stagger = 2; _attack = 2; gameObject.SetActive(true); }
        public void ResetGhost(bool resolved) { Active = false; IronHits = 0; _stagger = 0; transform.position = _home; apparition.gameObject.SetActive(false); glow.enabled = false; }
        void Update()
        {
            if(!session.Playing || !Active || session.Resolved) return;
            _stagger -= Time.deltaTime; _attack -= Time.deltaTime;
            apparition.localScale = Vector3.one * (_stagger > 0 ? .22f : 1);
            glow.enabled = _stagger <= 0;
            if(_stagger > 0) return;
            Vector3 destination = session.player.transform.position + Vector3.up * 1.2f;
            transform.position = Vector3.MoveTowards(transform.position, destination, session.definition.ghostSpeed * Time.deltaTime);
            apparition.localPosition = Vector3.up * Mathf.Sin(Time.time * 2.8f) * .12f;
            if(Vector3.Distance(transform.position, destination) < 1.6f && _attack <= 0)
            { _attack = session.definition.attackInterval; session.Damage(session.definition.ghostDamage); session.sound.PlayCue(55, .3f, .2f); }
        }
        public void Hit(bool iron)
        {
            if(!Active || session.Resolved) return;
            if(iron) { IronHits++; _stagger = session.definition.ironDisperseSeconds; session.Notify("IRON DISPERSES SPIRITS. Burn the anchor while it is gone."); }
            else { _stagger = Mathf.Max(_stagger, .5f); session.Notify("No wound. Ordinary weapons cannot end this hunt."); }
        }
        public void Resolve() { Active = false; apparition.gameObject.SetActive(false); glow.enabled = false; }
    }
}
