using UnityEngine;
namespace BeforeJericho
{
    public sealed class HunterCombat : MonoBehaviour
    {
        public HuntSession session; public HunterMotor motor; public ShoulderCamera view;
        public Light lighter, muzzleFlash; public Transform muzzle; public GameObject handgun, knife, iron;
        public int Weapon { get; private set; }
        public int Ammo { get; private set; } = 7;
        public int Reserve { get; private set; } = 21;
        public bool LighterOn => lighter && lighter.enabled;
        public bool Reloading => _reload > 0;
        float _cooldown, _reload, _flash;
        HuntInteractable _target;
        readonly Collider[] _near = new Collider[24];
        public string Prompt { get; private set; }
        public HuntInteractable Target => _target;
        void Update()
        {
            _flash -= Time.deltaTime; if(muzzleFlash) muzzleFlash.enabled = _flash > 0;
            if(!session.Playing) return;
            _cooldown = Mathf.Max(0, _cooldown - Time.deltaTime);
            if(_reload > 0) { _reload -= Time.deltaTime; if(_reload <= 0) { int n = Mathf.Min(session.definition.magazineSize - Ammo, Reserve); Ammo += n; Reserve -= n; } }
            if(motor.Input.Equip >= 0) Equip(motor.Input.Equip);
            if(motor.Input.Lighter) SetLighter(!LighterOn);
            if(motor.CanAim && motor.Input.Aim) SetLighter(false);
            if(motor.Input.Reload) Reload();
            if(motor.Input.Fire) Attack();
            FindTarget();
            if(_target && motor.Input.Interact) session.Interact(_target);
            if(_target && _target.kind == HuntItem.Anchor) session.AdvanceBurn(motor.Input.HoldInteract && LighterOn, Time.deltaTime);
            else session.AdvanceBurn(false, Time.deltaTime);
        }
        void FindTarget()
        {
            _target = null; Prompt = ""; float best = 2.25f;
            Vector3 origin = transform.position + Vector3.up * motor.InteractionHeight;
            int count = Physics.OverlapSphereNonAlloc(origin, 2.3f, _near, ~0, QueryTriggerInteraction.Collide);
            for(int i = 0; i < count; i++)
            {
                var item = _near[i].GetComponentInParent<HuntInteractable>();
                if(!item || item.Used(session)) continue;
                Vector3 point = _near[i].ClosestPoint(origin);
                Vector3 direction = point - origin;
                float d = direction.magnitude;
                if(d >= best) continue;
                if(d > .001f && Physics.Raycast(origin, direction / d, out RaycastHit hit, d + .02f, 1, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<HuntInteractable>() != item) continue;
                best = d; _target = item;
            }
            if(_target) Prompt = !_target.Available(session) ? "[F] LIGHTER  /  Reveal the residue" : _target.kind == HuntItem.Anchor ? "[HOLD E] IGNITE ANCHOR  /  Lighter required" : "[E] " + _target.label;
        }
        public void SetLighter(bool on) { if(lighter) lighter.enabled = on && !(motor.CanAim && motor.Input.Aim); }
        public void Equip(int index)
        {
            bool kit = session.HasSpiritKit;
            if(index == 2 && !session.Has(HuntItem.Iron) && !kit) { if(session.Haunt) session.Notify("Take the iron rod from the Impala trunk."); else if(session.ghost) session.Notify("Find the iron poker in the antique shop."); return; }
            if(index == 3 && !kit) { if(session.Haunt) session.Notify("Take the salt from the Impala trunk."); return; }
            Weapon = index; _reload = 0;
            if(handgun) handgun.SetActive(index == 0); if(knife) knife.SetActive(index == 1); if(iron) iron.SetActive(index == 2);
        }
        public void Reload() { if(Weapon == 0 && !Reloading && Ammo < session.definition.magazineSize && Reserve > 0) { _reload = 1.5f; session.Notify("Reloading..."); } }
        public void Attack()
        {
            if(!session.Playing || motor.IsCrawling || _cooldown > 0 || Reloading) return;
            if(session.stanford) { session.Notify("No hunting here. Not tonight."); return; }
            if(Weapon == 3) { _cooldown = .8f; session.Haunt.PourSalt(transform.position, Quaternion.Euler(0, view.Yaw, 0) * Vector3.forward); return; }
            if(Weapon == 0)
            {
                if(!motor.Input.Aim) { session.Notify("Hold RMB to aim."); return; }
                if(Ammo == 0) { session.Notify("Empty. [R] Reload."); return; }
                Ammo--; _cooldown = .3f; _flash = .06f; if(session.sound) session.sound.PlayCue(180, .1f, .22f);
                Ray ray = view.lens.ViewportPointToRay(new Vector3(.5f,.5f));
                Vector3 target = ray.GetPoint(70);
                if(Physics.Raycast(ray, out RaycastHit aimHit, 70, ~4, QueryTriggerInteraction.Ignore)) target = aimHit.point;
                Vector3 path = target - muzzle.position;
                if(Physics.Raycast(muzzle.position, path.normalized, out RaycastHit hit, path.magnitude + .1f, ~4, QueryTriggerInteraction.Ignore))
                { var ghoul = hit.collider.GetComponentInParent<GhoulHunt>(); var hitbox = hit.collider.GetComponent<GhoulHitbox>(); if(ghoul) ghoul.Hit(hitbox && hitbox.head, false); var ghost = hit.collider.GetComponentInParent<GhostHunt>(); if(ghost) ghost.Hit(false); var rawhead = hit.collider.GetComponentInParent<RawheadHunt>(); if(rawhead) rawhead.Hit(false); var manifestation = hit.collider.GetComponentInParent<ManifestationHunt>(); if(manifestation) manifestation.Hit(false); }
            }
            else
            {
                _cooldown = .65f; if(session.sound) session.sound.PlayCue(85, .12f, .12f);
                if(session.Haunt) { session.Haunt.Melee(transform.position + Vector3.up * motor.InteractionHeight, transform.forward, Weapon == 2); return; }
                if(session.eastTexas) { session.eastTexas.Director.KnifeAttack(transform.position + Vector3.up * motor.InteractionHeight, transform.forward); return; }
                if(session.chapter && session.chapter.Rawhead)
                {
                    var enemy = session.chapter.Rawhead;
                    Vector3 toEnemy = enemy.transform.position + Vector3.up - (transform.position + Vector3.up * motor.InteractionHeight);
                    if(toEnemy.magnitude < 3f && Vector3.Dot(transform.forward, toEnemy.normalized) > .15f && !Physics.Linecast(transform.position + Vector3.up * motor.InteractionHeight, enemy.transform.position + Vector3.up, 1, QueryTriggerInteraction.Ignore)) enemy.Hit(Weapon == 2);
                    return;
                }
                if(!session.ghost) return;
                Vector3 delta = session.ghost.transform.position - (transform.position + Vector3.up * motor.InteractionHeight);
                if(delta.magnitude < 3f && Vector3.Dot(transform.forward, delta.normalized) > .15f && !Physics.Linecast(transform.position + Vector3.up * motor.InteractionHeight, session.ghost.transform.position, 1, QueryTriggerInteraction.Ignore))
                    session.ghost.Hit(Weapon == 2);
            }
        }
        public void AddAmmo(int amount) { Reserve += Mathf.Max(0, amount); }
        public void Restore(int ammo, int reserve) { Ammo = Mathf.Clamp(ammo, 0, session.definition.magazineSize); Reserve = Mathf.Max(0, reserve); _reload = _cooldown = _flash = 0; Equip(0); SetLighter(false); }
    }
}
