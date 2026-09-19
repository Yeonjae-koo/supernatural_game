using UnityEngine;
namespace BeforeJericho
{
    public sealed class GhoulEncounterDirector : MonoBehaviour
    {
        [SerializeField] HuntSession _session;
        [SerializeField] EastTexasChapter _chapter;
        [SerializeField] GhoulHunt[] _enemies;
        [SerializeField] Collider _basementArena;
        public void ConfigureArena(Collider arena) { _basementArena=arena; }
        public GhoulHunt[] Enemies => _enemies;
        public GhoulHunt Boss => _enemies[4];
        public int DeadMask { get { int mask=0; foreach(var enemy in _enemies) if(enemy.Dead) mask |= 1 << enemy.Id; return mask; } }
        public int ActiveCount { get { int n=0; foreach(var enemy in _enemies) if(enemy.Active && !enemy.Dead) n++; return n; } }
        public bool NormalsCleared { get { for(int i=0;i<4;i++) if(!_enemies[i].Dead) return false; return true; } }
        public void Configure(HuntSession session, EastTexasChapter chapter, GhoulHunt[] enemies) { _session=session; _chapter=chapter; _enemies=enemies; }
        public void ResetEncounters() { foreach(var enemy in _enemies) enemy.ResetEnemy(); }
        void Update()
        {
            if(!_session.Playing) return;
            if(_chapter.Revealed) { RevealBoss(); return; }
            for(int i=0;i<4;i++)
            {
                var enemy=_enemies[i];
                if(!enemy.Dead && !enemy.Active && _chapter.EvidenceCount >= (i<2 ? 1 : 3) && ActiveCount < 2 && Vector3.Distance(_session.player.transform.position, enemy.transform.position) < 14) enemy.Activate();
            }
        }
        public void RevealBoss()
        {
            if(!_basementArena || !_basementArena.bounds.Contains(_session.player.transform.position+Vector3.up)) return;
            _enemies[4].Activate(); _enemies[5].Activate();
        }
        public void EnemyKilled(GhoulHunt enemy) { if(enemy.Boss) _chapter.BossKilled(); else _session.Notify("Ghoul down. Check your ammunition and continue investigating."); }
        public GhoulSnapshot[] Capture() { var states=new GhoulSnapshot[_enemies.Length]; for(int i=0;i<states.Length;i++) states[i]=_enemies[i].Capture(); return states; }
        public void Restore(GhoulSnapshot[] states) { for(int i=0;i<_enemies.Length;i++) _enemies[i].Restore(states[i]); }
        public void KnifeAttack(Vector3 origin, Vector3 forward)
        {
            GhoulHunt closest=null; float best=2.8f;
            foreach(var enemy in _enemies)
            {
                if(!enemy.Active || enemy.Dead) continue;
                Vector3 delta=enemy.transform.position+Vector3.up-origin;
                if(delta.magnitude>=best || Vector3.Dot(forward,delta.normalized)<.15f || Physics.Linecast(origin, enemy.transform.position+Vector3.up,1,QueryTriggerInteraction.Ignore)) continue;
                closest=enemy; best=delta.magnitude;
            }
            if(closest) closest.Hit(false,true);
        }
    }
}
