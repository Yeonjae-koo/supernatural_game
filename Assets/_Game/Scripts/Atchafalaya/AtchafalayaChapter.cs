using System.Text;
using UnityEngine;
namespace BeforeJericho
{
    public enum AtchafalayaPhase { Arrival, Investigation, FirstEncounter, Preparation, FinalHunt, Resolution, Breadcrumb, Transition }

    /// <summary>CH02 objective, evidence and durable progression rules. Scene owns spatial references.</summary>
    public sealed class AtchafalayaChapter : MonoBehaviour
    {
        public const string ChapterId = "CH02_ATCHAFALAYA";
        const int ArrivalFlag = 1, ChaseFlag = 2, EscapeFlag = 4, FuelFlag = 8, BreakerFlag = 16, GeneratorFlag = 32, BreadcrumbFlag = 64, TransitionFlag = 128;
        [SerializeField] HuntSession _session;
        [SerializeField] RawheadHunt _rawhead;
        [SerializeField] Transform _arrival, _chaseCheckpoint, _preparationCheckpoint, _finalCheckpoint, _chaseSpawn, _finalSpawn;
        [SerializeField] Collider _waterZone;
        [SerializeField] GameObject _ironDoor;
        [SerializeField] Transform _escapeBoundary;
        [SerializeField] Light _emergencyLight;
        int _flags;
        float _resolutionEffect;
        public RawheadHunt Rawhead => _rawhead;
        public int EvidenceCount { get { int n = 0; for(int i = 0; i < 4; i++) if((_session.Evidence & (1 << i)) != 0) n++; return n; } }
        public bool Identified => (_session.Evidence & 15) == 15;
        public bool ChaseStarted => (_flags & ChaseFlag) != 0;
        public bool ChaseEscaped => (_flags & EscapeFlag) != 0;
        public int PowerStep => (_flags & GeneratorFlag) != 0 ? 3 : (_flags & BreakerFlag) != 0 ? 2 : (_flags & FuelFlag) != 0 ? 1 : 0;
        public bool BreadcrumbFound => (_flags & BreadcrumbFlag) != 0;
        public bool TransitionReady => (_flags & TransitionFlag) != 0;
        public bool RawheadInWater
        {
            get
            {
                if(!_rawhead || !_rawhead.Active || !_waterZone) return false;
                Vector3 probe = _rawhead.transform.position; probe.y = _waterZone.bounds.center.y;
                return (_waterZone.ClosestPoint(probe) - probe).sqrMagnitude < .01f;
            }
        }
        public AtchafalayaPhase Phase => TransitionReady ? AtchafalayaPhase.Transition : BreadcrumbFound ? AtchafalayaPhase.Breadcrumb : _session.Resolved ? AtchafalayaPhase.Resolution : PowerStep == 3 ? AtchafalayaPhase.FinalHunt : ChaseEscaped ? AtchafalayaPhase.Preparation : ChaseStarted ? AtchafalayaPhase.FirstEncounter : (_flags & ArrivalFlag) != 0 ? AtchafalayaPhase.Investigation : AtchafalayaPhase.Arrival;
        public string Objective => TransitionReady ? "TEXAS / NEXT CHAPTER PREVIEW" : BreadcrumbFound ? "RETURN TO THE IMPALA / FOLLOW JOHN'S TRAIL" : _session.Resolved ? "ASK ELI ABOUT JOHN / TAKE THE MOTEL RECEIPT" : PowerStep == 3 ? (RawheadInWater ? "RAWHEAD IN WATER / THROW THE VOLTAGE SWITCH" : "LURE RAWHEAD INTO THE WATER / USE THE SWITCH") : ChaseEscaped ? "RESTORE POWER / " + (PowerStep == 0 ? "1. OPEN THE FUEL VALVE" : PowerStep == 1 ? "2. RESET THE MAIN BREAKER" : "3. START GENERATOR + REROUTE") : ChaseStarted ? "ESCAPE THE SUBFLOOR / CLOSE THE IRON DOOR" : Identified ? "SEARCH THE PUMP SUBFLOOR / CRAWL + LIGHTER" : (_flags & ArrivalFlag) == 0 ? "ASK MARA ABOUT THE MISSING KIDS" : "INVESTIGATE BELLE RIVE / EVIDENCE " + EvidenceCount + "/4";
        public string JournalText
        {
            get
            {
                var text = new StringBuilder("BELLE RIVE / MISSING CHILDREN\n\n");
                foreach(var item in _session.items) if(IsEvidence(item.kind) && Used(item.kind)) text.Append("• ").Append(item.description).Append("\n\n");
                if(EvidenceCount == 0) text.Append("Mara at the diner knows about the disappearances. Gold markers identify leads.\n");
                if(Identified) text.Append("DEDUCTION: RAWHEAD. A physical creature hiding in low, dark spaces. The disappearances stopped after an electrical accident. Guns and knives only stagger it.\n\n");
                if(ChaseEscaped) text.Append("PLAN: Fuel valve → main breaker → generator + chamber reroute. Bait it into shallow water, then use the isolated voltage switch.\n\n");
                if(BreadcrumbFound) text.Append("JOHN'S TRAIL: Eli saw an older military type. His motel receipt points west toward Texas. John is still missing.");
                return text.ToString();
            }
        }
        public void Configure(HuntSession session, RawheadHunt rawhead, Transform arrival, Transform chaseCheckpoint, Transform preparationCheckpoint, Transform finalCheckpoint, Transform chaseSpawn, Transform finalSpawn, Collider waterZone, GameObject ironDoor, Light emergencyLight)
        {
            _session = session; _rawhead = rawhead; _arrival = arrival; _chaseCheckpoint = chaseCheckpoint; _preparationCheckpoint = preparationCheckpoint; _finalCheckpoint = finalCheckpoint;
            _chaseSpawn = chaseSpawn; _finalSpawn = finalSpawn; _waterZone = waterZone; _ironDoor = ironDoor; _emergencyLight = emergencyLight;
            session.chapter = this;
        }
        public void SetEscapeBoundary(Transform boundary) { _escapeBoundary = boundary; }
        void Start()
        {
            if(!_session || !_rawhead || !_arrival || !_chaseCheckpoint || !_preparationCheckpoint || !_finalCheckpoint || !_chaseSpawn || !_finalSpawn || !_waterZone || !_ironDoor)
                Debug.LogError("AtchafalayaChapter is missing required scene bindings. Rebuild/wire CH02 before playing.", this);
        }
        void Update()
        {
            if(!_session.Playing) return;
            if(PowerStep == 3 && !_session.Resolved && !_rawhead.Active && Vector3.Distance(_session.player.transform.position, _finalSpawn.position) < 12)
            { _rawhead.Activate(_finalSpawn.position, false); _session.Notify("Rawhead is coming. Draw it into the marked water, then reach the voltage switch."); }
            if(_resolutionEffect <= 0) return;
            _resolutionEffect -= Time.deltaTime;
            if(_emergencyLight)
            {
                _emergencyLight.color = _resolutionEffect > 1.65f ? Color.white : Color.red;
                _emergencyLight.intensity = _resolutionEffect > 1.65f ? 12 : _resolutionEffect > 1.1f ? 0 : 3;
            }
        }
        public Vector3 ArrivalPosition => _arrival.position;
        public void NewGame() { _flags = 0; ApplyWorld(); }
        public static bool IsEvidence(HuntItem kind) => kind >= HuntItem.MudHandprints && kind <= HuntItem.ElectricalReport;
        public static int EvidenceBit(HuntItem kind) => IsEvidence(kind) ? 1 << ((int)kind - (int)HuntItem.MudHandprints) : 0;
        public bool Used(HuntItem kind)
        {
            if(IsEvidence(kind)) return (_session.Evidence & EvidenceBit(kind)) != 0;
            switch(kind)
            {
                case HuntItem.Mara: return (_flags & ArrivalFlag) != 0;
                case HuntItem.CrawlTrace: return ChaseStarted;
                case HuntItem.IronDoor: return ChaseEscaped;
                case HuntItem.FuelValve: return PowerStep >= 1;
                case HuntItem.MainBreaker: return PowerStep >= 2;
                case HuntItem.GeneratorStart: return PowerStep >= 3;
                case HuntItem.VoltageSwitch: return _session.Resolved;
                case HuntItem.JohnReceipt: return BreadcrumbFound;
                case HuntItem.ChapterExit: return TransitionReady;
                default: return false;
            }
        }
        public bool Interact(HuntInteractable item)
        {
            if(IsEvidence(item.kind))
            {
                _flags |= ArrivalFlag; _session.AddEvidence(EvidenceBit(item.kind)); _session.Notify(item.description);
                if(Identified) { _session.SaveCheckpoint("Rawhead identified", _chaseCheckpoint.position); _session.Notify("RAWHEAD. The electrical accident stopped the disappearances. Search the low pump-station pipes."); }
                return true;
            }
            switch(item.kind)
            {
                case HuntItem.Mara: _flags |= ArrivalFlag; _session.Notify("Mara: Sheriff says gators. Gators don't open bedroom windows.\nDean: Yeah. They usually skip the breaking-and-entering part."); return true;
                case HuntItem.Eli: _session.Notify(_session.Resolved ? "Eli: Another guy asked about the station. Older. Military type. Didn't smile much. Take the receipt." : "Eli: Power surged six years ago. Fried half the station. After that, the disappearances stopped."); return true;
                case HuntItem.CrawlTrace: return TriggerFirstEncounter();
                case HuntItem.IronDoor:
                    if(!ChaseStarted) return Refuse("Investigate the subfloor traces first. Crawl under the pipes with your lighter.");
                    Vector3 boundaryPoint = _escapeBoundary ? _escapeBoundary.position : _ironDoor.transform.position;
                    Vector3 escapeDirection = _escapeBoundary ? _escapeBoundary.forward : _preparationCheckpoint.position - boundaryPoint; escapeDirection.y = 0;
                    Vector3 playerSide = _session.player.transform.position - boundaryPoint; playerSide.y = 0;
                    if(Vector3.Dot(playerSide, escapeDirection.normalized) < .5f) return Refuse("Get through the iron doorway before closing it behind you.");
                    _flags |= EscapeFlag; _rawhead.ResetCreature(false); _ironDoor.SetActive(true); _session.Heal();
                    _session.SaveCheckpoint("Iron door escape", _preparationCheckpoint.position); _session.Notify("Okay. Bullets are officially not the answer.\nThe iron door holds. Restore power to the water chamber."); return true;
                case HuntItem.FuelValve:
                    if(!ChaseEscaped) return Refuse("Search the subfloor and escape through the iron door first.");
                    _flags |= FuelFlag; _session.Notify("Fuel valve open. Next: the main breaker."); SavePreparation(); return true;
                case HuntItem.MainBreaker:
                    if(PowerStep != 1) return Refuse("Open the generator's fuel valve first.");
                    _flags |= BreakerFlag; _session.Notify("Main breaker reset. Start the generator and reroute the chamber circuit."); SavePreparation(); return true;
                case HuntItem.GeneratorStart:
                    if(PowerStep != 2) return Refuse("Restore fuel, then reset the main breaker.");
                    _flags |= GeneratorFlag; _session.Heal();
                    _session.SaveCheckpoint("Water chamber hunt", _finalCheckpoint.position); _session.Notify("Generator online. The shallow-water circuit is armed. Lure Rawhead into the water, then throw the switch."); return true;
                case HuntItem.VoltageSwitch:
                    if(PowerStep != 3) return Refuse("No power. Restore fuel, breaker and generator first.");
                    if(!RawheadInWater) return Refuse("Wait. Rawhead must be standing in the marked water zone.");
                    _rawhead.ResolveByElectricity(); _session.SetChapterResolved(); _ironDoor.SetActive(false); _resolutionEffect = 2;
                    if(_session.sound) _session.sound.PlayCue(120, .7f, .25f);
                    _session.SaveCheckpoint("Rawhead resolved", _finalCheckpoint.position); _session.Notify("White light. A convulsion. Silence.\nEmergency power returns. Ask Eli about John's trail."); return true;
                case HuntItem.JohnReceipt:
                    if(!_session.Resolved) return Refuse("Finish the hunt before following John's trail.");
                    _flags |= BreadcrumbFlag; _session.SaveCheckpoint("John's motel receipt", _preparationCheckpoint.position); _session.Notify("Eli: Older guy. Military type. Didn't smile much.\nJohn's motel receipt points west. Texas. Return to the Impala."); return true;
                case HuntItem.ChapterExit:
                    if(!BreadcrumbFound) return Refuse("Finish the hunt and collect John's motel receipt from Eli.");
                    _flags |= TransitionFlag; _session.SaveCheckpoint("Leaving Belle Rive / Texas trail", _arrival.position); _session.CompleteChapter(); return true;
            }
            return false;
        }
        public bool TriggerFirstEncounter()
        {
            if(ChaseStarted) return false;
            if(!Identified) return Refuse("Find all four leads before entering the pump subfloor.");
            if(!_session.player.IsCrawling || !_session.player.GetComponent<HunterCombat>().LighterOn) return Refuse("Crawl [Z] beneath the pipes and use your lighter [F] to read the traces.");
            _flags |= ChaseFlag; _rawhead.Activate(_chaseSpawn.position, true);
            _session.SaveCheckpoint("Subfloor chase", _chaseCheckpoint.position);
            _session.Notify("Something is under the pipes. Crawl out, then sprint to the iron door and close it [E]!"); return true;
        }
        bool Refuse(string text) { _session.Notify(text); return false; }
        void SavePreparation() { _session.SaveCheckpoint("Generator preparation " + PowerStep + "/3", _preparationCheckpoint.position); }
        public void Capture(HuntSnapshot snapshot) { snapshot.version = 2; snapshot.chapterId = ChapterId; snapshot.chapterFlags = _flags; }
        public void Restore(HuntSnapshot snapshot) { _flags = snapshot.chapterFlags; ApplyWorld(); }
        void ApplyWorld()
        {
            _resolutionEffect = 0; _ironDoor.SetActive(ChaseEscaped && !_session.Resolved); _rawhead.ResetCreature(_session.Resolved);
            if(_emergencyLight) { _emergencyLight.color = Color.red; _emergencyLight.intensity = _session.Resolved ? 3 : .3f; }
            if(ChaseStarted && !ChaseEscaped) _rawhead.Activate(_chaseSpawn.position, true);
        }
        public static bool ValidSnapshot(HuntSnapshot s)
        {
            if(s.chapterId != ChapterId || s.evidence < 0 || s.evidence > 15 || s.chapterFlags < 0 || s.chapterFlags > 255) return false;
            int f = s.chapterFlags;
            if((f & ChaseFlag) != 0 && (s.evidence != 15 || (f & ArrivalFlag) == 0)) return false;
            if((f & EscapeFlag) != 0 && (f & ChaseFlag) == 0) return false;
            if((f & FuelFlag) != 0 && (f & EscapeFlag) == 0) return false;
            if((f & BreakerFlag) != 0 && (f & FuelFlag) == 0) return false;
            if((f & GeneratorFlag) != 0 && (f & BreakerFlag) == 0) return false;
            if(s.resolved && (f & GeneratorFlag) == 0) return false;
            if((f & BreadcrumbFlag) != 0 && !s.resolved) return false;
            return (f & TransitionFlag) == 0 || (f & BreadcrumbFlag) != 0;
        }
    }
}
