using UnityEngine;
namespace BeforeJericho
{
    public enum HuntItem { Mirror, Photos, Ledger, Scratches, Iron, Record, Salt, Anchor, Exit, Mara, Eli, MudHandprints, HairTissue, MissingReport, ElectricalReport, CrawlTrace, IronDoor, FuelValve, MainBreaker, GeneratorStart, VoltageSwitch, JohnReceipt, ChapterExit, SheriffReddick, NoraBell, GideonVale, EmptyGrave, MissingBodyParts, OldFuneralPhoto, StolenPersonalItem, FeedingArea, JohnMap, TexasExit, TexasAmmo, LornaHayes, MotelGuest, ImpalaTrunk, GuestLedger, RenovationBlueprint, OldMotelPhoto, MaintenanceNote, WallCavity, WallDecoy, JohnPhoneRecord, GrantsExit, MaeTorres, HankDoyle, CrashMarks, PoliceReport, GarageRecord, DriverIdentity, CremationRecord, SteeringWheel, WinslowDrive, WinslowReturn, JohnVoicemail, WinslowExit }
    public sealed class HuntInteractable : MonoBehaviour
    {
        public HuntItem kind; public string label; [TextArea] public string description;
        public bool needsLight; public Transform marker;
        public bool Used(HuntSession s) => s.winslow ? s.winslow.Used(kind) : s.grants ? s.grants.Used(kind) : s.eastTexas ? s.eastTexas.Used(kind) : s.chapter ? s.chapter.Used(kind) : kind == HuntItem.Anchor ? s.Resolved : kind == HuntItem.Exit ? s.Completed : s.Has(kind);
        public bool Available(HuntSession s) => !Used(s) && (!needsLight || s.player.GetComponent<HunterCombat>().LighterOn);
    }
}
