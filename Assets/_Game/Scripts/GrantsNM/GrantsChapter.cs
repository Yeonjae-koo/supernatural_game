using System;
using System.Text;
using UnityEngine;
namespace BeforeJericho
{
    // One authored environmental scare. Fires once when Dean enters its zone after enough evidence.
    [Serializable] public sealed class MotelEvent
    {
        public string line; public Collider zone; public int minEvidence;
        public GameObject show;      // stays on until the anchor burns (TV glow, slammed door)
        public GameObject flash;     // visible for a moment (mirror figure)
        public Light[] lightsOff;    // switched off one after another
    }
    public sealed class GrantsChapter : MonoBehaviour
    {
        public const string ChapterId = "CH04_GRANTS_NM";
        const int KitFlag = 1, MidFlag = 2, PhoneFlag = 4, ExitFlag = 8, EventShift = 4, EventCount = 5;
        const int AnchorBit = 16;
        [SerializeField] HuntSession _session;
        [SerializeField] PoltergeistHunt _poltergeist;
        [SerializeField] Transform _arrival, _officeCheckpoint, _laundryCheckpoint, _utilityCheckpoint;
        [SerializeField] GameObject _maintenanceGate, _lockdownGate, _rearDoor;
        [SerializeField] Collider _utilityZone;
        [SerializeField] Light _neon;
        [SerializeField] MotelEvent[] _events;
        int _flags, _playing = -1; float _eventStart;
        public PoltergeistHunt Poltergeist => _poltergeist;
        public Vector3 ArrivalPosition => _arrival.position;
        public bool HasKit => (_flags & KitFlag) != 0;
        public bool MidEncounter => (_flags & MidFlag) != 0;
        public bool PhoneFound => (_flags & PhoneFlag) != 0;
        public bool TransitionReady => (_flags & ExitFlag) != 0;
        public bool CorridorOpen => HasKit && (_session.Evidence & EvidenceBit(HuntItem.RenovationBlueprint)) != 0;
        public bool Identified => (_session.Evidence & 15) == 15;
        public bool AnchorSecured => (_session.Evidence & AnchorBit) != 0;
        public bool Lockdown => AnchorSecured && !_session.Resolved;
        public bool EventFired(int i) => (_flags & (1 << (EventShift + i))) != 0;
        public int EvidenceCount { get { int n = 0; for(int i = 0; i < 5; i++) if((_session.Evidence & (1 << i)) != 0) n++; return n; } }
        public string Objective => TransitionReady ? "JERICHO, CA / CHAPTER 05 PREVIEW" : PhoneFound ? "RETURN TO THE IMPALA / ROUTE 66 WEST" : _session.Resolved ? "FRONT OFFICE / CHECK THE PHONE RECORDS"
            : AnchorSecured ? "LOCKDOWN / HOLD THEM OFF, THEN BURN THE ANCHOR IN REAR STORAGE" : Identified ? "UTILITY ROOM / OPEN THE WALL ON THE DEATH AXIS"
            : !HasKit ? "PARKING LOT / TAKE IRON AND SALT FROM THE IMPALA TRUNK" : "SUNSET STAR / FOLLOW THE WALL, NOT THE ROOM NUMBER " + EvidenceCount + "/5";
        public string JournalText
        {
            get
            {
                var text = new StringBuilder("SUNSET STAR MOTOR COURT / REPEAT DEATHS\n\n");
                foreach(var item in _session.items) if(IsEvidence(item.kind) && Used(item.kind)) text.Append("• ").Append(item.description).Append("\n\n");
                text.Append("CANDIDATES: Vengeful spirit / Poltergeist\n");
                text.Append(Identified ? "DEDUCTION: POLTERGEIST. Rooms 8, 6 and 4 were renumbered over the years; every death sits on one physical wall line, the old mine support beam behind the utility room. Something inside that wall anchors it.\nIron disperses a manifestation. Salt holds a line. Fire ends it.\n" : "Room numbers changed. Compare the ledger, blueprint, photograph and maintenance note.\n");
                if(PhoneFound) text.Append("\nJOHN'S TRAIL: The front office phone log. John called a California number. In his handwriting: JERICHO.");
                return text.ToString();
            }
        }
        public void Configure(HuntSession session, PoltergeistHunt poltergeist, Transform arrival, Transform office, Transform laundry, Transform utility, GameObject maintenanceGate, GameObject lockdownGate, GameObject rearDoor, Collider utilityZone, Light neon, MotelEvent[] events)
        {
            _session = session; _poltergeist = poltergeist; _arrival = arrival; _officeCheckpoint = office; _laundryCheckpoint = laundry; _utilityCheckpoint = utility;
            _maintenanceGate = maintenanceGate; _lockdownGate = lockdownGate; _rearDoor = rearDoor; _utilityZone = utilityZone; _neon = neon; _events = events; session.grants = this;
        }
        void Start() { if(!_session || !_poltergeist || !_arrival || !_officeCheckpoint || !_laundryCheckpoint || !_utilityCheckpoint || !_maintenanceGate || !_lockdownGate || !_rearDoor || !_utilityZone || _events == null || _events.Length != EventCount) Debug.LogError("GrantsChapter requires all scene bindings.", this); }
        public static bool IsEvidence(HuntItem kind) => kind >= HuntItem.GuestLedger && kind <= HuntItem.WallCavity;
        public static int EvidenceBit(HuntItem kind) => IsEvidence(kind) ? 1 << ((int)kind - (int)HuntItem.GuestLedger) : 0;
        public bool Used(HuntItem kind)
        {
            if(IsEvidence(kind)) return (_session.Evidence & EvidenceBit(kind)) != 0;
            return kind == HuntItem.ImpalaTrunk ? HasKit : kind == HuntItem.Anchor ? _session.Resolved : kind == HuntItem.JohnPhoneRecord ? PhoneFound : kind == HuntItem.GrantsExit && TransitionReady;
        }
        public void NewGame() { _flags = 0; ApplyWorld(); }
        void ApplyWorld()
        {
            bool resolved = _session.Resolved;
            _maintenanceGate.SetActive(!CorridorOpen);
            _lockdownGate.SetActive(Lockdown);
            _rearDoor.SetActive(!resolved); // Exterior return route opens only after the hunt.
            _playing = -1;
            for(int i = 0; i < _events.Length; i++)
            {
                var e = _events[i];
                if(e.show) e.show.SetActive(EventFired(i) && !resolved);
                if(e.flash) e.flash.SetActive(false);
                if(e.lightsOff != null) foreach(var l in e.lightsOff) l.enabled = !EventFired(i) || resolved;
            }
            _poltergeist.Apply(resolved ? 0 : AnchorSecured ? 2 : MidEncounter ? 1 : 0, Lockdown);
        }
        void Update()
        {
            if(_neon) _neon.intensity = _session.Resolved ? 3.2f : Mathf.PerlinNoise(Time.unscaledTime * 7, 0) > .38f ? 3.2f : .25f;
            if(_playing >= 0) PlayEvent();
            if(!_session.Playing || _session.Resolved) return;
            Vector3 dean = _session.player.transform.position + Vector3.up;
            for(int i = 0; i < _events.Length; i++)
                if(!EventFired(i) && EvidenceCount >= _events[i].minEvidence && _events[i].zone.bounds.Contains(dean)) FireEvent(i);
            if(!MidEncounter && CorridorOpen && _utilityZone.bounds.Contains(dean))
            {
                _flags |= MidFlag; _poltergeist.Apply(1, false);
                _session.SaveCheckpoint("Utility room", _utilityCheckpoint.position);
                _session.Notify("The wall exhales. A half-formed shape pulls free of it.\nBullets pass through. Iron [3 + LMB] scatters it.");
            }
        }
        void FireEvent(int i)
        {
            if(_playing >= 0) FinishEvent();
            _flags |= 1 << (EventShift + i); _playing = i; _eventStart = Time.time;
            var e = _events[i]; if(e.show) e.show.SetActive(true); if(e.flash) e.flash.SetActive(true);
            _session.Notify(e.line); if(_session.sound) _session.sound.PlayCue(70 + i * 25, .25f, .2f);
        }
        void PlayEvent()
        {
            var e = _events[_playing]; float t = Time.time - _eventStart; int lights = e.lightsOff?.Length ?? 0;
            for(int j = 0; j < lights; j++) if(t > j * .4f) e.lightsOff[j].enabled = false;
            if(t > Mathf.Max(1.2f, lights * .4f)) FinishEvent();
        }
        void FinishEvent()
        {
            var e = _events[_playing]; if(e.flash) e.flash.SetActive(false);
            if(e.lightsOff != null) foreach(var l in e.lightsOff) l.enabled = false;
            _playing = -1;
        }
        bool Refuse(string text) { _session.Notify(text); return false; }
        public bool Interact(HuntInteractable item)
        {
            if(IsEvidence(item.kind))
            {
                if(item.kind == HuntItem.WallCavity)
                {
                    if(!Identified) return Refuse("Plaster and old studs. Line up the ledger, blueprint, photograph and maintenance note before tearing into the wall.");
                    if(!HasKit) return Refuse("Not without iron and salt. Get the kit from the Impala trunk first.");
                    _flags |= MidFlag; _session.AddEvidence(AnchorBit); _session.Heal(); ApplyWorld();
                    _session.SaveCheckpoint("Lockdown", _utilityCheckpoint.position);
                    _session.Notify(item.description + "\nEvery door in the block slams and locks.");
                    return true;
                }
                _session.AddEvidence(EvidenceBit(item.kind)); ApplyWorld(); _session.Notify(item.description);
                _session.SaveCheckpoint("Sunset Star evidence " + EvidenceCount + "/5", item.kind == HuntItem.MaintenanceNote ? _laundryCheckpoint.position : _officeCheckpoint.position);
                return true;
            }
            switch(item.kind)
            {
                case HuntItem.LornaHayes: case HuntItem.MotelGuest: _session.Notify(item.description); return true;
                case HuntItem.WallDecoy: return Refuse(item.description);
                case HuntItem.ImpalaTrunk:
                    _flags |= KitFlag; ApplyWorld(); _session.SaveCheckpoint("Impala trunk", _arrival.position);
                    _session.Notify(item.description); return true;
                case HuntItem.Anchor:
                    return Refuse(AnchorSecured ? "Lighter on [F], then hold E to burn the anchor." : "An old burn barrel. Nothing worth burning yet.");
                case HuntItem.JohnPhoneRecord:
                    if(!_session.Resolved) return Refuse("Lorna: That log's private.\nFinish the job before digging through the office.");
                    _flags |= PhoneFlag; _session.SaveCheckpoint("John's phone record", _officeCheckpoint.position);
                    _session.Notify("PHONE RECORD: John called a California number from this desk. Beside it, in his hand: JERICHO, CA.\nDean: \"Jericho...\""); return true;
                case HuntItem.GrantsExit:
                    if(!PhoneFound) return Refuse(_session.Resolved ? "Check the front office phone records first." : "Not yet. The motel is still hunting its guests.");
                    _flags |= ExitFlag; _session.SaveCheckpoint("Sunset Star complete / Jericho", _arrival.position); _session.CompleteChapter(); return true;
            }
            return false;
        }
        public void AnchorBurned()
        {
            _session.SetChapterResolved(); ApplyWorld();
            _session.SaveCheckpoint("Anchor burned", _utilityCheckpoint.position);
            _session.Notify("The miner's tag blackens. Every bulb overloads — then silence.\nThe VACANCY sign burns steady. Check the office.");
        }
        public void Capture(HuntSnapshot s) { s.version = 4; s.chapterId = ChapterId; s.chapterFlags = _flags; }
        public void Restore(HuntSnapshot s) { _flags = s.chapterFlags; ApplyWorld(); }
        public static bool ValidSnapshot(HuntSnapshot s)
        {
            if(s.chapterId != ChapterId || s.evidence < 0 || s.evidence > 31 || s.chapterFlags < 0 || s.chapterFlags >= 1 << (EventShift + EventCount)) return false;
            bool kit = (s.chapterFlags & KitFlag) != 0, anchor = (s.evidence & AnchorBit) != 0;
            if((s.chapterFlags & MidFlag) != 0 && (!kit || (s.evidence & 2) == 0)) return false;
            if(anchor && (s.evidence != 31 || (s.chapterFlags & MidFlag) == 0)) return false;
            if(s.resolved && !anchor) return false;
            if((s.chapterFlags & PhoneFlag) != 0 && !s.resolved) return false;
            return (s.chapterFlags & ExitFlag) == 0 || (s.chapterFlags & PhoneFlag) != 0;
        }
    }
}
