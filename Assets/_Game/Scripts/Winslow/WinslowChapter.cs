using System.Text;
using UnityEngine;
namespace BeforeJericho
{
    // CH05 Coyote Mile: Road Spirit investigation, scripted Impala drive, anchor burn and John's voicemail.
    public sealed class WinslowChapter : MonoBehaviour
    {
        public const string ChapterId = "CH05_WINSLOW_AZ";
        const int MaeFlag = 1, MidFlag = 2, DriveFlag = 4, ReturnFlag = 8, VoicemailFlag = 16, ExitFlag = 32, EventShift = 6, EventCount = 3;
        const int AnchorBit = 32;
        [SerializeField] HuntSession _session;
        [SerializeField] PoltergeistHunt _spirit;
        [SerializeField] Transform _arrival, _dinerCheckpoint, _motelCheckpoint, _salvageCheckpoint, _motelRoom;
        [SerializeField] GameObject _arrivalCar, _salvageCar, _motelCar;
        [SerializeField] Collider _motelZone;
        [SerializeField] Light[] _headlights;
        [SerializeField] MotelEvent[] _events;
        int _flags, _playing = -1; float _eventStart, _resolvedAt = -10;
        public PoltergeistHunt Spirit => _spirit;
        public Vector3 ArrivalPosition => _arrival.position;
        public bool MaeTalked => (_flags & MaeFlag) != 0;
        public bool MidEncounter => (_flags & MidFlag) != 0;
        public bool Driven => (_flags & DriveFlag) != 0;
        public bool Returned => (_flags & ReturnFlag) != 0;
        public bool VoicemailHeard => (_flags & VoicemailFlag) != 0;
        public bool TransitionReady => (_flags & ExitFlag) != 0;
        public bool Identified => (_session.Evidence & 31) == 31;
        public bool AnchorSecured => (_session.Evidence & AnchorBit) != 0;
        public bool EventFired(int i) => (_flags & (1 << (EventShift + i))) != 0;
        public int EvidenceCount { get { int n = 0; for(int i = 0; i < 6; i++) if((_session.Evidence & (1 << i)) != 0) n++; return n; } }
        public string Objective => TransitionReady ? "PALO ALTO / CHAPTER 06 PREVIEW" : VoicemailHeard ? "THE IMPALA / GO GET SAM" : Returned ? "MOTEL ROOM / PLAY JOHN'S VOICEMAIL"
            : _session.Resolved ? "SALVAGE YARD / DRIVE BACK TO THE MOTEL" : AnchorSecured ? "BURN THE STEERING WHEEL / SALT AND IRON BUY TIME"
            : Driven ? "SALVAGE YARD / FIND THE CRASH CAR'S STEERING WHEEL" : Identified && MaeTalked ? "THE IMPALA / DRIVE TO THE SALVAGE YARD"
            : Identified ? "DINER / TALK TO MAE" : "COYOTE MILE / INVESTIGATE THE CRASHES " + EvidenceCount + "/5";
        public string JournalText
        {
            get
            {
                var text = new StringBuilder("COYOTE MILE / REPEAT NIGHT CRASHES\n\n");
                foreach(var item in _session.items) if(IsEvidence(item.kind) && Used(item.kind)) text.Append("• ").Append(item.description).Append("\n\n");
                if(MaeTalked) text.Append("• MAE: An older man asked the same questions last night. Dad. One day behind him.\n\n");
                text.Append(Identified ? "DEDUCTION: ROAD SPIRIT. Ray Colton died on this mile in 1979 and was cremated, so no bones to burn. Something of him stayed with the wreck: the steering wheel he died holding. Salt slows it, iron repels it, fire ends it.\n" : "Find what keeps the dead driver on this road.\n");
                if(VoicemailHeard) text.Append("\nJOHN'S VOICEMAIL: He's hunting something dangerous and warns me to stay back. Under the static, a second voice: \"...Jericho...\"\nHe isn't just busy. He's missing. I need Sam.");
                return text.ToString();
            }
        }
        public void Configure(HuntSession session, PoltergeistHunt spirit, Transform arrival, Transform diner, Transform motel, Transform salvage, Transform motelRoom, GameObject arrivalCar, GameObject salvageCar, GameObject motelCar, Collider motelZone, Light[] headlights, MotelEvent[] events)
        {
            _session = session; _spirit = spirit; _arrival = arrival; _dinerCheckpoint = diner; _motelCheckpoint = motel; _salvageCheckpoint = salvage; _motelRoom = motelRoom;
            _arrivalCar = arrivalCar; _salvageCar = salvageCar; _motelCar = motelCar; _motelZone = motelZone; _headlights = headlights; _events = events; session.winslow = this;
        }
        void Start() { if(!_session || !_spirit || !_arrival || !_dinerCheckpoint || !_motelCheckpoint || !_salvageCheckpoint || !_motelRoom || !_arrivalCar || !_salvageCar || !_motelCar || !_motelZone || _events == null || _events.Length != EventCount) Debug.LogError("WinslowChapter requires all scene bindings.", this); }
        public static bool IsEvidence(HuntItem kind) => kind >= HuntItem.CrashMarks && kind <= HuntItem.SteeringWheel;
        public static int EvidenceBit(HuntItem kind) => IsEvidence(kind) ? 1 << ((int)kind - (int)HuntItem.CrashMarks) : 0;
        public bool Used(HuntItem kind)
        {
            if(IsEvidence(kind)) return (_session.Evidence & EvidenceBit(kind)) != 0;
            return kind == HuntItem.MaeTorres ? MaeTalked : kind == HuntItem.WinslowDrive ? Driven : kind == HuntItem.Anchor ? _session.Resolved
                : kind == HuntItem.WinslowReturn ? Returned : kind == HuntItem.JohnVoicemail ? VoicemailHeard : kind == HuntItem.WinslowExit && TransitionReady;
        }
        public void NewGame() { _flags = 0; ApplyWorld(); }
        void ApplyWorld()
        {
            bool resolved = _session.Resolved;
            _arrivalCar.SetActive(!Driven); _salvageCar.SetActive(Driven && !Returned); _motelCar.SetActive(Returned);
            _playing = -1;
            for(int i = 0; i < _events.Length; i++) { if(_events[i].show) _events[i].show.SetActive(EventFired(i) && !resolved); if(_events[i].flash) _events[i].flash.SetActive(false); }
            // Index 0 haunts the motel; index 1 guards the wreck at the salvage yard.
            if(resolved) _spirit.Apply(0, false); else if(Driven) _spirit.Apply(1, false, 1); else _spirit.Apply(MidEncounter ? 1 : 0, false);
        }
        void Update()
        {
            if(_headlights != null && _headlights.Length > 0)
            {
                bool flicker = Time.time - _resolvedAt < 2.5f && Mathf.PerlinNoise(Time.time * 14, 0) < .45f;
                foreach(var l in _headlights) if(l) l.enabled = !flicker;
            }
            if(_playing >= 0)
            {
                var e = _events[_playing];
                if(Time.time - _eventStart > 1.4f) { if(e.flash) e.flash.SetActive(false); _playing = -1; }
            }
            if(!_session.Playing || _session.Resolved) return;
            Vector3 dean = _session.player.transform.position + Vector3.up;
            for(int i = 0; i < _events.Length; i++)
                if(!EventFired(i) && EvidenceCount >= _events[i].minEvidence && _events[i].zone.bounds.Contains(dean)) FireEvent(i);
            if(!MidEncounter && !Driven && EvidenceCount >= 2 && _motelZone.bounds.Contains(dean))
            {
                _flags |= MidFlag; _spirit.Apply(1, false);
                _session.SaveCheckpoint("Closed motel", _motelCheckpoint.position);
                _session.Notify("A figure in a crash-torn jacket stands in the motel hallway, headlights where its eyes should be.\nIron [3 + LMB] drives it back. Salt [4 + LMB] holds it.");
            }
        }
        void FireEvent(int i)
        {
            if(_playing >= 0 && _events[_playing].flash) _events[_playing].flash.SetActive(false);
            _flags |= 1 << (EventShift + i); _playing = i; _eventStart = Time.time;
            var e = _events[i]; if(e.show) e.show.SetActive(true); if(e.flash) e.flash.SetActive(true);
            _session.Notify(e.line); if(_session.sound) _session.sound.PlayCue(65 + i * 30, .3f, .22f);
        }
        bool Refuse(string text) { _session.Notify(text); return false; }
        public bool Interact(HuntInteractable item)
        {
            if(IsEvidence(item.kind))
            {
                if(item.kind == HuntItem.SteeringWheel)
                {
                    if(!Identified) return Refuse("Just another wreck. Figure out whose car this was first.");
                    _session.AddEvidence(AnchorBit); _session.Heal();
                    _session.SaveCheckpoint("Steering wheel secured", _salvageCheckpoint.position);
                    _session.Notify(item.description + "\nThe yard lights die. It is coming for its car. Salt it, then burn it in the barrel.");
                    return true;
                }
                _session.AddEvidence(EvidenceBit(item.kind)); _session.Notify(item.description);
                _session.SaveCheckpoint("Coyote Mile evidence " + EvidenceCount + "/5", MidEncounter ? _motelCheckpoint.position : _dinerCheckpoint.position);
                return true;
            }
            switch(item.kind)
            {
                case HuntItem.MaeTorres:
                    _flags |= MaeFlag; _session.SaveCheckpoint("Mae's diner", _dinerCheckpoint.position);
                    _session.Notify("Mae: Older guy came through last night. Same questions.\nDean: Tall? Grumpy? Looks like sleep is a personal insult?\nMae: That's the one.\nDean: (One day. I missed him by one day.)"); return true;
                case HuntItem.HankDoyle: _session.Notify(item.description); return true;
                case HuntItem.WinslowDrive:
                    if(!Identified) return Refuse("Not done here. The crash, the report, the car and the driver — then the body.");
                    if(!MaeTalked) return Refuse("Talk to Mae at the diner before leaving town.");
                    _flags |= DriveFlag; ApplyWorld(); _session.player.Teleport(_salvageCheckpoint.position); _session.player.view.Face(0);
                    _session.SaveCheckpoint("Salvage yard", _salvageCheckpoint.position);
                    _session.Notify("Route 66, midnight. The phone buzzes; Dean misses it. In the rear-view mirror, a burned man sits in the back seat.\nDean stands on the brake — the seat is empty. The salvage yard gate is right there."); return true;
                case HuntItem.Anchor:
                    return Refuse(AnchorSecured ? "Lighter on [F], then hold E to burn the steering wheel." : "An oil drum. Find the anchor first.");
                case HuntItem.WinslowReturn:
                    if(!_session.Resolved) return Refuse("Not yet. The wheel still has to burn.");
                    _flags |= ReturnFlag; ApplyWorld(); _session.player.Teleport(_motelRoom.position); _session.player.view.Face(0);
                    _session.SaveCheckpoint("Motel room", _motelRoom.position);
                    _session.Notify("Back at the motel. Dean drops the duffel. One new voicemail."); return true;
                case HuntItem.JohnVoicemail:
                    if(!Returned) return Refuse("Later. Finish the job.");
                    _flags |= VoicemailFlag; _session.SaveCheckpoint("John's voicemail", _motelRoom.position);
                    _session.Notify("JOHN (voicemail): \"Dean. I'm onto something bad. Don't follow me. Watch yourself.\"\nDean replays it. Under the static, a woman's whisper: \"...Jericho...\"\nDean: I need another set of eyes. ... Sammy."); return true;
                case HuntItem.WinslowExit:
                    if(!VoicemailHeard) return Refuse(Returned ? "Play the voicemail first." : "Not leaving yet.");
                    _flags |= ExitFlag; _session.SaveCheckpoint("Coyote Mile complete / Palo Alto", _motelRoom.position); _session.CompleteChapter(); return true;
            }
            return false;
        }
        public void AnchorBurned()
        {
            _session.SetChapterResolved(); ApplyWorld(); _resolvedAt = Time.time;
            _session.SaveCheckpoint("Road spirit at rest", _salvageCheckpoint.position);
            _session.Notify("Headlights flicker across the yard. A distant horn. The spirit's rage breaks; for a second it looks calm.\nA gust of wind, and it is gone.");
        }
        public void Capture(HuntSnapshot s) { s.version = 5; s.chapterId = ChapterId; s.chapterFlags = _flags; }
        public void Restore(HuntSnapshot s) { _flags = s.chapterFlags; ApplyWorld(); }
        public static bool ValidSnapshot(HuntSnapshot s)
        {
            if(s.chapterId != ChapterId || s.evidence < 0 || s.evidence > 63 || s.chapterFlags < 0 || s.chapterFlags >= 1 << (EventShift + EventCount)) return false;
            int f = s.chapterFlags;
            if((f & DriveFlag) != 0 && ((s.evidence & 31) != 31 || (f & MaeFlag) == 0)) return false;
            if((s.evidence & AnchorBit) != 0 && (s.evidence != 63 || (f & DriveFlag) == 0)) return false;
            if(s.resolved && (s.evidence & AnchorBit) == 0) return false;
            if((f & ReturnFlag) != 0 && !s.resolved) return false;
            if((f & VoicemailFlag) != 0 && (f & ReturnFlag) == 0) return false;
            return (f & ExitFlag) == 0 || (f & VoicemailFlag) != 0;
        }
    }
}
