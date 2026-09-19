using System.Text;
using UnityEngine;
namespace BeforeJericho
{
    // CH06 epilogue: evidence convergence, the Sam decision, a text montage, and the walk to Sam's door. No combat.
    public sealed class StanfordChapter : MonoBehaviour
    {
        public const string ChapterId = "CH06_STANFORD";
        const int CallFlag = 1, ArrivedFlag = 2, TrunkFlag = 4, GameCompleteFlag = 8;
        static readonly string[] Montage =
        {
            "Interstate 40, westbound. The Impala eats the dark.",
            "A gas station at 3 AM. Coffee that tastes like the pot.",
            "WELCOME TO CALIFORNIA.",
            "John's folded map on the passenger seat. JERICHO circled twice.",
            "Evening. Palo Alto. Porch lights, bicycles, somebody laughing through an open window."
        };
        [SerializeField] HuntSession _session;
        [SerializeField] Transform _arrival, _paloAlto;
        [SerializeField] GameObject _apartmentDoor;
        [SerializeField] Collider _samsDoorZone;
        int _flags, _montageStep = -1; float _stepAt, _finalAt = -1;
        public Vector3 ArrivalPosition => _arrival.position;
        public bool Converged => _session.Evidence == 15;
        public bool Called => (_flags & CallFlag) != 0;
        public bool Arrived => (_flags & ArrivedFlag) != 0;
        public bool TrunkDone => (_flags & TrunkFlag) != 0;
        public bool GameComplete => (_flags & GameCompleteFlag) != 0;
        public bool TransitionReady => GameComplete;
        public bool InMontage => _montageStep >= 0;
        public int EvidenceCount { get { int n = 0; for(int i = 0; i < 4; i++) if((_session.Evidence & (1 << i)) != 0) n++; return n; } }
        public string Objective => GameComplete ? "TO BE CONTINUED" : Called ? "FIND SAM" : Converged ? "CALL JOHN" : "MOTEL ROOM / CONNECT JOHN'S TRAIL " + EvidenceCount + "/4";
        public string JournalText
        {
            get
            {
                var text = new StringBuilder("JOHN'S TRAIL / CONVERGENCE\n\n");
                foreach(var item in _session.items) if(IsEvidence(item.kind) && Used(item.kind)) text.Append("• ").Append(item.description).Append("\n\n");
                if(Converged) text.Append("CONCLUSION:\nJERICHO, CALIFORNIA\nMISSING MEN\nJOHN'S ROUTE\n\n");
                if(Called) text.Append("Called Dad again. No answer. He's not busy. He's in trouble.\nFIND SAM.");
                return text.ToString();
            }
        }
        public void Configure(HuntSession session, Transform arrival, Transform paloAlto, GameObject apartmentDoor, Collider samsDoorZone)
        { _session = session; _arrival = arrival; _paloAlto = paloAlto; _apartmentDoor = apartmentDoor; _samsDoorZone = samsDoorZone; session.stanford = this; }
        void Start() { if(!_session || !_arrival || !_paloAlto || !_apartmentDoor || !_samsDoorZone) Debug.LogError("StanfordChapter requires all scene bindings.", this); }
        public static bool IsEvidence(HuntItem kind) => kind >= HuntItem.JohnNotes && kind <= HuntItem.VoicemailTape;
        public static int EvidenceBit(HuntItem kind) => IsEvidence(kind) ? 1 << ((int)kind - (int)HuntItem.JohnNotes) : 0;
        public bool Used(HuntItem kind)
        {
            if(IsEvidence(kind)) return (_session.Evidence & EvidenceBit(kind)) != 0;
            return kind == HuntItem.CallJohn ? Called : kind == HuntItem.HitTheRoad ? Arrived : kind == HuntItem.StanfordTrunk && TrunkDone;
        }
        public void NewGame() { _flags = 0; ApplyWorld(); }
        void ApplyWorld() { _apartmentDoor.SetActive(!TrunkDone); _montageStep = -1; _finalAt = -1; }
        void Update()
        {
            if(InMontage && _session.Playing && Time.time >= _stepAt)
            {
                if(_montageStep < Montage.Length) { _session.Notify(Montage[_montageStep++]); _stepAt = Time.time + 1.6f; if(_session.sound) _session.sound.PlayCue(110 + _montageStep * 20, .2f, .12f); }
                else
                {
                    _montageStep = -1; _flags |= ArrivedFlag; _session.player.Teleport(_paloAlto.position); _session.player.view.Face(0); _session.player.Input.SetGameplay(true);
                    _session.SaveCheckpoint("Palo Alto", _paloAlto.position);
                    _session.Notify("Palo Alto. Clean streets, warm windows. Dean feels like the only thing here with rust on it.");
                }
            }
            if(_finalAt >= 0 && Time.time >= _finalAt) { _finalAt = -1; _session.CompleteChapter(); }
            if(!_session.Playing || GameComplete || !TrunkDone) return;
            if(_samsDoorZone.bounds.Contains(_session.player.transform.position + Vector3.up))
            {
                // Final beat: stop at the door, no confrontation, fade to the ending card.
                _flags |= GameCompleteFlag; _session.player.Input.SetGameplay(false);
                _session.SaveCheckpoint("Game complete", _paloAlto.position);
                _session.Notify("Dean stops. The hallway goes quiet. His hand finds the lock.\nDean: \"All right, Sammy.\"");
                _finalAt = Time.time + 2.5f;
            }
        }
        bool Refuse(string text) { _session.Notify(text); return false; }
        public bool Interact(HuntInteractable item)
        {
            if(InMontage) return false;
            if(IsEvidence(item.kind))
            {
                _session.AddEvidence(EvidenceBit(item.kind)); _session.Notify(item.description);
                _session.SaveCheckpoint("Motel room " + EvidenceCount + "/4", _arrival.position);
                if(Converged) _session.Notify(item.description + "\n\nJERICHO, CALIFORNIA / MISSING MEN / JOHN'S ROUTE");
                return true;
            }
            switch(item.kind)
            {
                case HuntItem.CallJohn:
                    if(!Converged) return Refuse("Not yet. Lay it all out first.");
                    _flags |= CallFlag; _session.SaveCheckpoint("Find Sam", _arrival.position);
                    _session.Notify("Ringing. Ringing. Voicemail. Again.\nDean: \"Fine.\" ... \"Time to call in the pain in my ass.\"\nOBJECTIVE: FIND SAM"); return true;
                case HuntItem.HitTheRoad:
                    if(!Called) return Refuse("Call Dad one more time before leaving.");
                    _montageStep = 0; _stepAt = Time.time; _session.player.Input.SetGameplay(false); return true;
                case HuntItem.StanfordTrunk:
                    _flags |= TrunkFlag; ApplyWorld(); _session.SaveCheckpoint("Impala trunk / Palo Alto", _paloAlto.position);
                    _session.Notify("The trunk: handgun, knife, salt, iron, lighter, Dad's files.\nDean leaves the gun. \"Family visit. Keep it classy.\""); return true;
                case HuntItem.SamNeighbor: _session.Notify(item.description); return true;
            }
            return false;
        }
        public void Capture(HuntSnapshot s) { s.version = 6; s.chapterId = ChapterId; s.chapterFlags = _flags; }
        public void Restore(HuntSnapshot s) { _flags = s.chapterFlags; ApplyWorld(); }
        public static bool ValidSnapshot(HuntSnapshot s)
        {
            if(s.chapterId != ChapterId || s.evidence < 0 || s.evidence > 15 || s.chapterFlags < 0 || s.chapterFlags > 15) return false;
            int f = s.chapterFlags;
            if((f & CallFlag) != 0 && s.evidence != 15) return false;
            if((f & ArrivedFlag) != 0 && (f & CallFlag) == 0) return false;
            if((f & TrunkFlag) != 0 && (f & ArrivedFlag) == 0) return false;
            return (f & GameCompleteFlag) == 0 || (f & TrunkFlag) != 0;
        }
    }
}
