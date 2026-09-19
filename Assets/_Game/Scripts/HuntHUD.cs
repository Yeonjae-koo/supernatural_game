using System.Text;
using TMPro;
using UnityEngine;
namespace BeforeJericho
{
    public sealed class HuntHUD : MonoBehaviour
    {
        public HuntSession session; public HunterCombat combat;
        public TMP_Text objective, stats, prompt, message, overlayTitle, overlayBody, subtitle;
        public GameObject overlay; public UnityEngine.UI.Button primary, secondary, tertiary;
        public TMP_Text primaryLabel, secondaryLabel, tertiaryLabel;
        public UnityEngine.UI.Image healthFill, burnFill;
        string _mode;
        void Start() { primary.onClick.AddListener(Primary); secondary.onClick.AddListener(Secondary); tertiary.onClick.AddListener(session.MainMenu); }
        void Update()
        {
            objective.text = session.Objective;
            stats.text = (combat.Weapon == 0 ? "1911  /  " + combat.Ammo + " : " + combat.Reserve : combat.Weapon == 1 ? "HUNTING KNIFE" : combat.Weapon == 3 ? "SALT  /  " + session.grants.Poltergeist.SaltCharges + " LINES" : session.grants ? "IRON ROD" : "IRON POKER") + (combat.Reloading ? "  / RELOADING" : "") + "\n" + (combat.LighterOn ? "LIGHTER  ON" : "LIGHTER  OFF");
            prompt.text = combat.Prompt; message.text = session.Message;
            healthFill.fillAmount = session.HP / 100f; burnFill.fillAmount = session.grants ? (session.Resolved ? 1 : session.grants.AnchorSecured ? session.BurnProgress / session.definition.ignitionSeconds : session.grants.EvidenceCount / 5f) : session.eastTexas ? (session.Resolved ? 1 : session.eastTexas.EvidenceCount / 5f) : session.chapter ? (session.Resolved ? 1 : session.chapter.PowerStep / 3f) : session.BurnProgress / session.definition.ignitionSeconds;
            string mode = session.Menu ? "menu" : session.Completed ? "end" : session.HP == 0 ? "dead" : session.Journal ? "journal" : session.Paused ? "pause" : "";
            overlay.SetActive(mode != "");
            if(_mode == mode) return; _mode = mode;
            primary.gameObject.SetActive(true); secondary.gameObject.SetActive(mode != "journal"); tertiary.gameObject.SetActive(mode == "pause" || mode == "dead");
            if(mode == "menu")
            {
                overlayTitle.text = "SUPERNATURAL\n<size=36>BEFORE JERICHO</size>";
                subtitle.text = session.grants ? "CHAPTER 04 / GRANTS, NEW MEXICO" : session.eastTexas ? "CHAPTER 03 / EAST TEXAS" : session.chapter ? "CHAPTER 02  /  ATCHAFALAYA" : "PROLOGUE  /  THE LAST JOB";
                overlayBody.text = "New Orleans, 2005.\nA dead antique dealer. A necklace with no history.\nFinish the hunt. Then call Dad.\n\nWASD  Move    MOUSE  Look    SHIFT  Sprint\nE  Investigate    F  Lighter    TAB  Journal\nRMB  Aim    LMB  Attack    R  Reload\n1  Handgun    2  Knife    3  Iron poker\n\nPersonal fan-game prototype / Original placeholder art & audio";
                if(session.chapter) overlayBody.text = "Belle Rive, Louisiana.\nChildren vanish. Mud on bedroom windows.\nFind the creature. Learn what can kill it.\n\nWASD  Move    MOUSE  Look    SHIFT  Sprint\nCTRL / C  Crouch    Z  Crawl\nE  Investigate    F  Lighter    TAB  Journal\nRMB  Aim    LMB  Attack    R  Reload\n1  Handgun    2  Knife\n\nPersonal fan-game prototype / Placeholder art & audio";
                if(session.eastTexas) overlayBody.text = "Mercy Creek, East Texas.\nOpened graves. Missing remains. An unchanged face.\nInvestigate the funeral home and identify the ghouls.\n\nWASD Move / MOUSE Look / SHIFT Sprint\nC Crouch / Z Crawl / F Lighter / E Investigate\nRMB Aim / LMB Attack / R Reload / 1 Gun / 2 Knife\nTAB Journal / ESC Pause\nHead hits stagger. Follow up with a knife finish.";
                if(session.grants) overlayBody.text = "Route 66. Grants, New Mexico.\nThe Sunset Star Motor Court keeps losing guests in the same place.\nFind what the building is protecting and burn it.\n\nWASD Move / MOUSE Look / SHIFT Sprint\nC Crouch / Z Crawl / F Lighter / E Investigate\nRMB Aim / LMB Attack / 1 Gun / 2 Knife / 3 Iron / 4 Salt\nTAB Journal / ESC Pause\nIron disperses. Salt holds a line. Nothing here dies to bullets.";
                primaryLabel.text = "NEW HUNT"; secondaryLabel.text = "CONTINUE"; secondary.interactable = session.CanContinue;
            }
            else if(mode == "journal")
            {
                overlayTitle.text = "JOHN'S JOURNAL"; subtitle.text = "CASE 01  /  SAINT MERCY";
                var text = new StringBuilder();
                foreach(var item in session.items) if(item.kind <= HuntItem.Salt && session.Has(item.kind)) text.Append("• ").Append(item.description).Append("\n\n");
                if(text.Length == 0) text.Append("No evidence yet. Begin at the antique shop.");
                if(session.Has(HuntItem.Record)) text.Append("\nDEDUCTION: Vengeful spirit. The necklace is its anchor.\nSALT + FIRE ends the hunt. IRON buys time.");
                overlayBody.text = session.grants ? session.grants.JournalText : session.eastTexas ? session.eastTexas.JournalText : session.chapter ? session.chapter.JournalText : text.ToString();
                if(session.chapter) subtitle.text = "CASE 02  /  BELLE RIVE";
                if(session.eastTexas) subtitle.text = "CASE 03 / MERCY CREEK";
                if(session.grants) subtitle.text = "CASE 04 / SUNSET STAR MOTOR COURT";
                primaryLabel.text = "CLOSE JOURNAL";
            }
            else if(mode == "dead")
            { overlayTitle.text = "THE HUNT ENDS HERE."; subtitle.text = "DEAN WINCHESTER"; overlayBody.text = "Ordinary weapons cannot kill a spirit.\nUse the iron poker to disperse it.\n\nCheckpoint: " + session.CheckpointName; if(session.chapter) overlayBody.text = "Rawhead is physical, but ordinary wounds close.\nUse guns and knives to buy time.\nRestore power, lure it into water, then throw the voltage switch.\n\nCheckpoint: " + session.CheckpointName; if(session.eastTexas) overlayBody.text = "Head hits stagger ghouls. Finish with a knife.\nBack away from attacks; sidestep Gideon's lunge.\n\nCheckpoint: " + session.CheckpointName; if(session.grants) overlayBody.text = "The manifestation cannot be killed.\nIron [3] disperses it. Salt [4] holds it behind a line.\nWatch for lifting objects and keep moving.\n\nCheckpoint: " + session.CheckpointName; primaryLabel.text = "RETRY CHECKPOINT"; secondaryLabel.text = "MAIN MENU"; secondary.interactable = true; tertiary.gameObject.SetActive(false); }
            else if(mode == "end")
            { overlayTitle.text = "NO RESPONSE."; subtitle.text = "MOTEL  /  2:17 AM"; overlayBody.text = "Dean calls John. Voicemail. Again.\n\n\"Job's done. Call me back, Dad.\"\n\nOne outgoing call. No response.\nBeyond New Orleans, the road waits.\n\nPROLOGUE COMPLETE\nContinue to Atchafalaya and follow John's trail."; if(session.chapter) { overlayTitle.text = "WESTBOUND."; subtitle.text = "TEXAS  /  NEXT CHAPTER PREVIEW"; overlayBody.text = "The children are safe. Belle Rive can sleep.\n\nEli's motel receipt points toward Texas.\nJohn was here. He kept moving.\n\nCHAPTER 02 COMPLETE\nContinue to Mercy Creek, East Texas."; }
                if(session.eastTexas) { overlayTitle.text = "YOU WERE HERE."; subtitle.text = "MOTEL DESK / NEW MEXICO"; overlayBody.text = "John's folded map lies beneath the motel lamp.\nThe camera moves toward his handwriting: NEW MEXICO.\nBeyond the window, desert highway heat shimmers.\n\nCHAPTER 03 COMPLETE\nJohn is following a larger pattern. He remains missing.\nCHAPTER 04 / NEW MEXICO is the next destination."; }
                if(session.grants) { overlayTitle.text = "JERICHO, CA."; subtitle.text = "FRONT OFFICE / GRANTS, NEW MEXICO"; overlayBody.text = "The phone log sits under the desk lamp.\nJohn called a California number. One word in his hand: JERICHO.\nDean: \"Jericho...\"\n\nThe Impala's tail lights fade west down Route 66.\n\nCHAPTER 04 COMPLETE\nJohn remains missing. The trail points to California."; }
                primaryLabel.text = session.grants ? "PLAY AGAIN" : session.eastTexas ? "CONTINUE TO NEW MEXICO" : session.chapter ? "CONTINUE TO EAST TEXAS" : "CONTINUE TO ATCHAFALAYA"; secondaryLabel.text = "MAIN MENU"; secondary.interactable = true; }
            else if(mode == "pause")
            { overlayTitle.text = "THE ROAD CAN WAIT."; subtitle.text = "PAUSED"; overlayBody.text = "Checkpoint: " + session.CheckpointName + "\n\nE  Investigate / Hold to ignite\nF  Lighter    TAB  Journal\n3 + LMB  Disperse with iron\n\nIgnition progress is retained between attempts.\nA hit interrupts the ritual and removes one second."; if(session.chapter) overlayBody.text = "Checkpoint: " + session.CheckpointName + "\n\nCTRL / C  Crouch    Z  Crawl\nE  Investigate    F  Lighter    TAB  Journal\n\nFuel valve → breaker → generator.\nLure Rawhead into water, then activate the voltage switch."; if(session.grants) overlayBody.text = "Checkpoint: " + session.CheckpointName + "\n\nDeaths follow the wall, not the room number.\n3 Iron disperses / 4 Salt + LMB pours a line\nC Crouch / Z Crawl / E Investigate / F Lighter\nBurn the anchor: lighter on, hold E."; if(session.eastTexas) overlayBody.text = "Checkpoint: " + session.CheckpointName + "\n\nFind five clues. Aim for heads and use knife finishes.\nC Crouch / Z Crawl / E Investigate / F Lighter\nShow Gideon the photograph, then find John's map."; primaryLabel.text = "RESUME"; secondaryLabel.text = "RETRY CHECKPOINT"; tertiaryLabel.text = "MAIN MENU"; secondary.interactable = true; }
        }
        void Primary() { if(_mode == "end" && session.eastTexas) session.TravelToGrants(); else if(_mode == "end" && session.chapter) session.TravelToEastTexas(); else if(_mode == "end" && !session.grants && !session.eastTexas && !session.chapter) session.TravelToAtchafalaya(); else if(_mode == "menu" || _mode == "end") session.NewGame(); else if(_mode == "dead") session.RestoreCheckpoint(); else session.Resume(); }
        void Secondary() { if(_mode == "menu") session.ContinueGame(); else if(_mode == "pause") session.RestoreCheckpoint(); else session.MainMenu(); }
    }
}
