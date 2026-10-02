using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace BeforeJericho.UI
{
    /// <summary>In-play HUD (HTML explore / combat / ritual). Mostly empty while exploring; weapon and health appear
    /// on aim, hit or reload and fade (HUD: dynamic), stay (always) or never show (off).</summary>
    public sealed class HudView : UIView
    {
        [Header("Layer")] [SerializeField] CanvasGroup _layer;
        [Header("Objective toast")] [SerializeField] CanvasGroup _objective; [SerializeField] TMP_Text _objectiveText;
        [Header("Tutorial")] [SerializeField] CanvasGroup _tutorial; [SerializeField] TMP_Text _tutorialTitle, _tutorialBody;
        [Header("Saving")] [SerializeField] CanvasGroup _saving; [SerializeField] RectTransform _spinner;
        [Header("Journal notify")] [SerializeField] CanvasGroup _notify; [SerializeField] TMP_Text _notifyName;
        [Header("Prompt")] [SerializeField] GameObject _prompt; [SerializeField] Image _promptKeyBack; [SerializeField] TMP_Text _promptKey, _promptText;
        [Header("Ritual")] [SerializeField] GameObject _ritual; [SerializeField] Image _ring, _ritualKeyBack; [SerializeField] TMP_Text _ritualKey, _ritualSub;
        [Header("Weapon")] [SerializeField] CanvasGroup _weapon; [SerializeField] TMP_Text _ammo, _ammoSub, _weaponName, _reloading; [SerializeField] Image[] _slotFrames; [SerializeField] TMP_Text[] _slotLabels;
        [Header("Status")] [SerializeField] GameObject _lighter; [SerializeField] CanvasGroup _health; [SerializeField] Image _healthFill;
        [SerializeField] Image _hurtFlash, _hurtLow; [SerializeField] GameObject _dot, _cross;
        [Header("Subtitles")] [SerializeField] SubtitleView _subtitles;

        HuntSession S => Hud.session;
        HunterCombat C => Hud.combat;
        float _objectiveAt = -99, _tutorialAt = -99, _tutorialFor, _saveAt = -99, _notifyAt = -99, _aimOffAt = -99, _hurtAt = -99, _weaponAt = -99, _captionAt = -99;
        string _lastObjective, _lastCheckpoint, _lastMessage, _captionKey; int _lastHp = 100, _lastWeapon, _captionDir; bool _dismissed, _wasAiming, _wasReloading, _wasResolved, _firstThreatSeen, _pendingObjective = true;
        Transform _captionSource;
        readonly List<(Component c, bool active)> _threats = new();
        readonly HashSet<HuntInteractable> _usedItems = new();
        static float Now => Time.unscaledTime;

        protected override void Opened()
        {
            base.Opened();
            if(_threats.Count == 0) FindThreats();
            _lastHp = S.HP; _lastCheckpoint = S.CheckpointName; _lastMessage = null; _lastWeapon = C.Weapon;
            SnapshotItems();
        }

        /// <summary>A new chapter start or checkpoint restore: re-show the objective, forget one-off toasts.</summary>
        public void Restart(bool newSession)
        {
            _pendingObjective = true; _lastObjective = null; _dismissed = false; _captionKey = null;
            if(newSession) _firstThreatSeen = false;
            _lastHp = S.HP; _wasResolved = S.Resolved; _lastCheckpoint = S.CheckpointName; SnapshotItems();
            for(int i = 0; i < _threats.Count; i++) _threats[i] = (_threats[i].c, ThreatActive(_threats[i].c));
        }

        public void ObjectiveReady() { _pendingObjective = true; }
        public void DismissSubtitle() { _dismissed = true; }
        public bool SubtitleShowing => _subtitles.HasContent && !_dismissed;
        public void SimulateHit() { _hurtAt = Now; }

        public override void Tick()
        {
            bool dynamic = GameSettings.Choice("hud") == "dynamic", off = GameSettings.Choice("hud") == "off";
            _layer.alpha = GameSettings.Range("hudop") / 100f;
            bool aiming = S.Playing && S.player.CanAim && S.player.Input.Aim;
            if(_wasAiming && !aiming) _aimOffAt = Now; _wasAiming = aiming;
            if(_wasReloading && !C.Reloading) _aimOffAt = Now; _wasReloading = C.Reloading;
            if(C.Weapon != _lastWeapon) { _lastWeapon = C.Weapon; _weaponAt = Now; }
            if(S.HP < _lastHp) _hurtAt = Now; _lastHp = S.HP;

            Objective(); Saving(); Notify(); Threats(); Messages();
            Prompt(off); Weapon(aiming, dynamic, off); Status(dynamic, off, aiming);
        }

        void Objective()
        {
            string objective = S.Objective;
            if(objective != _lastObjective && !Hud.CardVisible) { if(_lastObjective != null || _pendingObjective) _objectiveAt = Now; _lastObjective = objective; _pendingObjective = false; _objectiveText.text = Loc.Src(objective); }
            float t = Now - _objectiveAt;
            _objective.alpha = Toast(t, 4.2f);
        }

        void Saving()
        {
            if(S.CheckpointName != _lastCheckpoint) { _lastCheckpoint = S.CheckpointName; _saveAt = Now; }
            float t = Now - _saveAt;
            _saving.alpha = t < 3 ? (t < 2.4f ? 1 : 1 - (t - 2.4f) / .6f) : 0;
            if(_saving.alpha > 0) _spinner.localEulerAngles = new Vector3(0, 0, -Now * 360);
        }

        void Notify()
        {
            foreach(var item in S.items)
            {
                if(!item || _usedItems.Contains(item) || !item.Used(S)) continue;
                _usedItems.Add(item);
                string name = Loc.Has("ClueNames/" + item.kind) ? "ClueNames/" + item.kind : Loc.Has("Files/" + item.kind) ? "Files/" + item.kind : null;
                if(name != null) { _notifyName.text = Loc.T(name); _notifyAt = Now; }
            }
            _notify.alpha = Toast(Now - _notifyAt, 4f);
        }

        void SnapshotItems() { _usedItems.Clear(); foreach(var item in S.items) if(item && item.Used(S)) _usedItems.Add(item); }

        void FindThreats()
        {
            foreach(var c in FindObjectsByType<GhostHunt>(FindObjectsInactive.Include)) _threats.Add((c, false));
            foreach(var c in FindObjectsByType<RawheadHunt>(FindObjectsInactive.Include)) _threats.Add((c, false));
            foreach(var c in FindObjectsByType<GhoulHunt>(FindObjectsInactive.Include)) _threats.Add((c, false));
            foreach(var c in FindObjectsByType<ManifestationHunt>(FindObjectsInactive.Include)) _threats.Add((c, false));
            for(int i = 0; i < _threats.Count; i++) _threats[i] = (_threats[i].c, ThreatActive(_threats[i].c));
        }

        static bool ThreatActive(Component c) => c && c.gameObject.activeInHierarchy && c switch
        {
            GhostHunt g => g.Active, RawheadHunt r => r.Active, GhoulHunt h => h.Active && !h.Dead,
            ManifestationHunt m => m.Active && !m.Dispersed, _ => false
        };

        /// <summary>HTML screen this HUD state corresponds to (development overlay).</summary>
        public string Context => _ritual.activeSelf ? "ritual" : ThreatNear(22) || _wasAiming ? "combat" : "explore";
        public IEnumerable<Vector3> ActiveThreatPositions() { foreach(var t in _threats) if(t.active && t.c) yield return t.c.transform.position; }

        public bool ThreatNear(float range)
        {
            Vector3 p = S.player.transform.position;
            foreach(var t in _threats) if(t.active && (t.c.transform.position - p).sqrMagnitude < range * range) return true;
            return false;
        }

        void Threats()
        {
            for(int i = 0; i < _threats.Count; i++)
            {
                var (c, was) = _threats[i]; bool now = ThreatActive(c);
                if(now == was) continue;
                _threats[i] = (c, now);
                if(!now) continue;
                Caption(c switch { RawheadHunt => "Captions/growl", GhoulHunt => "Captions/snarl", ManifestationHunt when S.grants => "Captions/scratch", _ => "Captions/whisper" }, c.transform);
                if(!_firstThreatSeen) { _firstThreatSeen = true; string key = "Tutorial/" + Hud.ChapterId + ".first"; if(Loc.Has(key + ".title")) Tutorial(Loc.T(key + ".title"), Loc.T(key + ".body"), 8); }
            }
            if(S.Resolved && !_wasResolved && (S.ghost || S.grants || S.winslow)) Caption("Captions/fire", null);
            _wasResolved = S.Resolved;
        }

        void Caption(string key, Transform source) { _captionKey = key; _captionSource = source; _captionAt = Now; _dismissed = false; }

        void Tutorial(string title, string body, float seconds) { _tutorialTitle.text = title; _tutorialTitle.gameObject.SetActive(!string.IsNullOrEmpty(title)); _tutorialBody.text = body; _tutorialAt = Now; _tutorialFor = seconds; }

        string _msgTable = "", _msgLines = "";
        bool _ritualLit; string _linesLabel = "";
        public override void Refresh() { if(!Hud) return; Localize(); _linesLabel = " " + Loc.T("UI_Common/hud.lines"); _ritualSub.text = ""; }

        void Localize()
        {
            string message = S.Message;
            _msgTable = KeyTable(message);
            _msgLines = string.IsNullOrEmpty(message) || _msgTable == "Tutorial" ? "" : Loc.Src(message);
            if(_msgTable == "Intro") { int cut = _msgLines.IndexOf('\n'); _msgLines = cut >= 0 ? _msgLines.Substring(cut + 1) : ""; }
            if(_lastObjective != null) _objectiveText.text = Loc.Src(_lastObjective);
        }

        void Messages()
        {
            string message = S.Message;
            if(message != _lastMessage)
            {
                _lastMessage = message; _dismissed = false; Localize();
                if(_msgTable == "Tutorial") Tutorial("", Loc.Src(message), 6);
            }
            _tutorial.alpha = GameSettings.On("tutorial") ? Toast(Now - _tutorialAt, _tutorialFor, .25f) : 0;
            string caption = Now - _captionAt < 3.5f ? _captionKey : null;
            int dir = caption != null && _captionSource ? Direction(_captionSource.position) : 0;
            if(_dismissed) _subtitles.Set(null, 0, "", false);
            else _subtitles.Set(caption, dir, _msgLines, _msgTable == "Dialogue" || _msgTable == "Intro");
        }

        static string KeyTable(string message) { string key = Loc.KeyOf(message); int slash = key?.IndexOf('/') ?? -1; return slash > 0 ? key.Substring(0, slash) : ""; }

        int Direction(Vector3 world)
        {
            var cam = S.player.view.lens.transform; Vector3 d = world - cam.position; d.y = 0;
            Vector3 f = cam.forward; f.y = 0;
            if(d.sqrMagnitude < .01f || Vector3.Angle(f, d) < 35) return 0; // on screen: no arrow
            return Vector3.Dot(cam.right, d) < 0 ? -1 : 1;
        }

        void Prompt(bool off)
        {
            var target = C.Target; bool show = S.Playing && target && !(S.player.CanAim && S.player.Input.Aim) && !Hud.CardVisible;
            bool ritual = show && target.kind == HuntItem.Anchor && target.Available(S);
            bool contrast = GameSettings.On("contrast");
            _prompt.SetActive(show && !ritual && GameSettings.On("prompts"));
            _ritual.SetActive(ritual && S.BurnProgress < S.definition.ignitionSeconds);
            if(_prompt.activeSelf)
            {
                bool needsLight = !target.Available(S);
                _promptKey.text = Hud.KeyName(needsLight ? "Lighter" : "Interact");
                _promptText.text = needsLight ? Loc.T("UI_Common/prompt.lighter_needed") : Loc.Src(target.label);
                Contrast(_promptKeyBack, _promptKey, contrast);
            }
            if(_ritual.activeSelf)
            {
                _ring.fillAmount = S.BurnProgress / S.definition.ignitionSeconds;
                _ritualKey.text = Hud.KeyName("Interact");
                if(_ritualLit != C.LighterOn || _ritualSub.text.Length == 0) { _ritualLit = C.LighterOn; _ritualSub.text = C.LighterOn ? Loc.T("UI_Common/prompt.lighter_on") : Loc.T("UI_Common/prompt.lighter_needed") + " [" + Hud.KeyName("Lighter") + "]"; }
                Contrast(_ritualKeyBack, _ritualKey, contrast);
            }
        }

        // Key-cap fill: CSS rgba(0,0,0,.45), alpha corrected for linear-space UI blending.
        static void Contrast(Image back, TMP_Text key, bool on) { back.color = on ? UIStyle.Bone : new Color(0, 0, 0, .73f); key.color = on ? Color.black : UIStyle.Bone; }

        void Weapon(bool aiming, bool dynamic, bool off)
        {
            bool active = aiming || C.Reloading || Now - _weaponAt < .1f;
            float alpha = off ? 0 : !dynamic || active ? 1 : Fade(Now - Mathf.Max(_aimOffAt, _weaponAt), 3.6f);
            _weapon.alpha = alpha; if(alpha <= 0) return;
            int w = C.Weapon;
            if(S.stanford) { _ammo.text = "—"; _ammoSub.text = ""; _weaponName.text = Loc.T("Items/weapon.none"); }
            else
            {
                if(w == 0) { _ammo.SetText("{0}", C.Ammo); _ammoSub.SetText(" / {0}", C.Reserve); }
                else if(w == 3 && S.Haunt) { _ammo.SetText("{0}", S.Haunt.SaltCharges); _ammoSub.text = _linesLabel; }
                else { _ammo.text = "—"; _ammoSub.text = ""; }
                _weaponName.text = Loc.T(w == 0 ? "Items/weapon.1911" : w == 1 ? "Items/weapon.knife" : w == 2 ? (S.Haunt ? "Items/weapon.iron" : "Items/weapon.poker") : "Items/weapon.salt");
            }
            _reloading.gameObject.SetActive(C.Reloading);
            for(int i = 0; i < _slotFrames.Length; i++) { Color c = i == w ? UIStyle.Bone : UIStyle.Dim; _slotFrames[i].color = c; _slotLabels[i].color = c; }
        }

        void Status(bool dynamic, bool off, bool aiming)
        {
            _lighter.SetActive(!off && C.LighterOn);
            bool combat = ThreatNear(22) || aiming;
            bool recent = Now - _hurtAt < 4;
            _health.alpha = off ? 0 : !dynamic || combat || recent ? 1 : 0;
            _healthFill.fillAmount = S.HP / 100f; _healthFill.color = S.HP < 40 ? UIStyle.Blood : UIStyle.Bone;
            float flash = Now - _hurtAt;
            _hurtFlash.enabled = flash < .7f && !GameSettings.On("flash");
            if(_hurtFlash.enabled) _hurtFlash.color = new Color(1, 1, 1, .9f * (1 - flash / .7f));
            _hurtLow.enabled = S.HP > 0 && S.HP < 40;
            string cross = GameSettings.Choice("crosshair");
            _dot.SetActive(aiming && cross == "dot"); _cross.SetActive(aiming && cross == "cross");
        }

        /// <summary>HTML @keyframes toast: fade in over 8%, hold, fade out from 82%.</summary>
        static float Toast(float t, float duration, float edge = .08f)
        {
            if(t < 0 || t >= duration) return 0; float u = t / duration;
            return u < edge ? u / edge : u > .82f ? 1 - (u - .82f) / .18f : 1;
        }
        static float Fade(float t, float duration) { if(t >= duration) return 0; float u = t / duration; return u < .8f ? 1 : 1 - (u - .8f) / .2f; }
    }
}
