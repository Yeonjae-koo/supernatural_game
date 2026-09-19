using System.Text;
using UnityEngine;
namespace BeforeJericho
{
    public sealed class EastTexasChapter : MonoBehaviour
    {
        public const string ChapterId="CH03_EAST_TEXAS";
        const int RevealFlag=1, MapFlag=2, ExitFlag=4, AmmoFlag=8;
        [SerializeField] HuntSession _session;
        [SerializeField] GhoulEncounterDirector _director;
        [SerializeField] Transform _arrival, _cemeteryCheckpoint, _basementCheckpoint;
        [SerializeField] GameObject _basementGate, _disguiseVisual;
        int _flags;
        public GhoulEncounterDirector Director => _director;
        public Vector3 ArrivalPosition => _arrival.position;
        public bool Revealed => (_flags&RevealFlag)!=0;
        public bool BreadcrumbFound => (_flags&MapFlag)!=0;
        public bool TransitionReady => (_flags&ExitFlag)!=0;
        public int EvidenceCount { get { int n=0; for(int i=0;i<5;i++) if((_session.Evidence&(1<<i))!=0) n++; return n; } }
        public bool Identified => _session.Evidence==31;
        public string Objective => TransitionReady ? "NEW MEXICO / CHAPTER 04 PREVIEW" : BreadcrumbFound ? "RETURN TO THE IMPALA / FOLLOW JOHN WEST" : _session.Resolved ? "BASEMENT / COLLECT JOHN'S FOLDED MAP" : Revealed ? "GIDEON VALE / AIM FOR THE HEAD, THEN FINISH" : Identified ? (_director.NormalsCleared ? "SHOW GIDEON THE OLD FUNERAL PHOTO" : "CLEAR THE GHOULS BEFORE CONFRONTING GIDEON") : "MERCY CREEK / INVESTIGATE THE FUNERAL HOME " + EvidenceCount + "/5";
        public string JournalText
        {
            get
            {
                var text=new StringBuilder("MERCY CREEK / DISTURBED GRAVES\n\n");
                foreach(var item in _session.items) if(IsEvidence(item.kind)&&Used(item.kind)) text.Append("• ").Append(item.description).Append("\n\n");
                text.Append("CANDIDATES: Shapeshifter / Revenant / Ghoul\nGhoul confidence: ").Append(EvidenceCount*20).Append("%\n");
                text.Append(Identified ? "DEDUCTION: GHOUL. Corpse feeding, stolen identities and the unchanged director connect the evidence. Body shots hurt, but head hits stagger. A knife can finish a staggered ghoul.\n" : "No failed deduction ends the hunt. Collect the remaining leads.\n");
                if(BreadcrumbFound) text.Append("\nJOHN'S TRAIL: A folded map, his handwriting, a New Mexico marking and a newspaper clipping. He was following a larger pattern. John remains missing.");
                return text.ToString();
            }
        }
        public void Configure(HuntSession session, GhoulEncounterDirector director, Transform arrival, Transform cemeteryCheckpoint, Transform basementCheckpoint, GameObject basementGate)
        { _session=session; _director=director; _arrival=arrival; _cemeteryCheckpoint=cemeteryCheckpoint; _basementCheckpoint=basementCheckpoint; _basementGate=basementGate; session.eastTexas=this; }
        public void ConfigureDisguise(GameObject disguiseVisual) { _disguiseVisual=disguiseVisual; }
        void Start() { if(!_session || !_director || !_arrival || !_cemeteryCheckpoint || !_basementCheckpoint || !_basementGate) Debug.LogError("EastTexasChapter requires all scene bindings.",this); }
        public static bool IsEvidence(HuntItem kind) => kind>=HuntItem.EmptyGrave && kind<=HuntItem.FeedingArea;
        public static int EvidenceBit(HuntItem kind) => IsEvidence(kind) ? 1<<((int)kind-(int)HuntItem.EmptyGrave) : 0;
        public bool Used(HuntItem kind)
        {
            if(IsEvidence(kind)) return (_session.Evidence&EvidenceBit(kind))!=0;
            return kind==HuntItem.GideonVale ? Revealed : kind==HuntItem.JohnMap ? BreadcrumbFound : kind==HuntItem.TexasExit ? TransitionReady : kind==HuntItem.TexasAmmo && (_flags&AmmoFlag)!=0;
        }
        public void NewGame() { _flags=0; _director.ResetEncounters(); ApplyWorld(); }
        void ApplyWorld() { _basementGate.SetActive(!Revealed); if(_disguiseVisual) _disguiseVisual.SetActive(!Revealed); }
        bool Refuse(string text) { _session.Notify(text); return false; }
        public bool Interact(HuntInteractable item)
        {
            if(IsEvidence(item.kind))
            {
                _session.AddEvidence(EvidenceBit(item.kind)); _session.Notify(item.description);
                _session.SaveCheckpoint(Identified ? "Ghoul identified" : "Mercy Creek evidence "+EvidenceCount+"/5", _cemeteryCheckpoint.position);
                return true;
            }
            switch(item.kind)
            {
                case HuntItem.SheriffReddick: _session.Notify("Reddick: Coyotes dig. People panic.\nDean: Coyotes usually take the whole meal."); return true;
                case HuntItem.NoraBell: _session.Notify("Nora: Mr. Vale has run this place longer than I've been alive.\nNora is human. Check the old photograph and staff records."); return true;
                case HuntItem.GideonVale:
                    if(!Identified) return Refuse("Gideon: Death makes people imaginative.\nGather five leads before showing him the photograph.");
                    if(!_director.NormalsCleared) return Refuse("Clear the four ghouls hunting around Mercy Creek first.");
                    _flags|=RevealFlag; ApplyWorld(); _director.RevealBoss(); _session.Heal();
                    var combat=_session.player.GetComponent<HunterCombat>(); if(combat.Ammo+combat.Reserve<28) combat.AddAmmo(28-combat.Ammo-combat.Reserve);
                    _session.SaveCheckpoint("Gideon revealed",_basementCheckpoint.position);
                    _session.Notify("Dean: You take a hell of a passport photo.\nGideon's eyes darken. The human disguise falls. Follow him into the basement."); return true;
                case HuntItem.TexasAmmo: _flags|=AmmoFlag; _session.player.GetComponent<HunterCombat>().AddAmmo(35); _session.Heal(); _session.Notify("Spare .45 rounds and first aid. Aim for heads; save ammunition with knife finishes."); _session.SaveCheckpoint("Mercy Creek supplies",_cemeteryCheckpoint.position); return true;
                case HuntItem.JohnMap:
                    if(!_session.Resolved) return Refuse("Gideon is still hunting. Finish the fight first.");
                    if(_director.ActiveCount>0) return Refuse("Clear the remaining ghoul before examining John's map.");
                    _flags|=MapFlag; _session.SaveCheckpoint("John's New Mexico map",_basementCheckpoint.position); _session.Notify("Dean: You were here.\nJohn's handwriting. NEW MEXICO. A clipping links these hunts to a larger pattern."); return true;
                case HuntItem.TexasExit:
                    if(!BreadcrumbFound) return Refuse("Finish the hunt and collect John's map in the basement.");
                    _flags|=ExitFlag; _session.SaveCheckpoint("Mercy Creek complete / New Mexico",_arrival.position); _session.CompleteChapter(); return true;
            }
            return false;
        }
        public void BossKilled() { _session.SetChapterResolved(); _session.Notify("Gideon collapses. Clear his accomplice, then inspect John's folded map."); }
        public void Capture(HuntSnapshot s) { s.version=3; s.chapterId=ChapterId; s.chapterFlags=_flags; s.ghouls=_director.Capture(); }
        public void Restore(HuntSnapshot s) { _flags=s.chapterFlags; _director.Restore(s.ghouls); ApplyWorld(); }
        public static bool ValidSnapshot(HuntSnapshot s)
        {
            if(s.chapterId!=ChapterId || s.evidence<0 || s.evidence>31 || s.chapterFlags<0 || s.chapterFlags>15 || s.ghouls==null || s.ghouls.Length!=6) return false;
            bool revealed=(s.chapterFlags&RevealFlag)!=0;
            for(int i=0;i<6;i++)
            {
                var g=s.ghouls[i]; if(g==null || g.id!=i || g.hp<0 || g.hp>(i==4?240:80) || (g.hp==0&&g.active) || float.IsNaN(g.x) || float.IsInfinity(g.x) || Mathf.Abs(g.x)>=100 || !(g.y>-5&&g.y<20) || !(g.z>-5&&g.z<200)) return false;
                if(i>=4 && !revealed && (g.active||g.hp!=(i==4?240:80))) return false;
                if(i<4 && revealed && g.hp!=0) return false;
            }
            if(revealed && s.evidence!=31) return false;
            if(s.resolved != (s.ghouls[4].hp==0)) return false;
            if((s.chapterFlags&MapFlag)!=0 && (!s.resolved || s.ghouls[5].hp!=0)) return false;
            return (s.chapterFlags&ExitFlag)==0 || (s.chapterFlags&MapFlag)!=0;
        }
    }
}
