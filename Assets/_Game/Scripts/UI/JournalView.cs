using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace BeforeJericho.UI
{
    /// <summary>Journal (HTML journal + map): Case / Clues / People / Files / Items / Map. Q/E switch tabs, Tab closes, M toggles the map.
    /// Reads session/chapter state only; progression stays in the chapter scripts.</summary>
    public sealed class JournalView : UIView
    {
        public enum Tab { Case, Clues, People, Files, Items, Map }
        [SerializeField] Button[] _tabs; [SerializeField] Graphic[] _tabUnderlines; [SerializeField] TMP_Text[] _tabLabels;
        [SerializeField] GameObject _casePanel, _cluesPanel, _peoplePanel, _filesPanel, _itemsPanel, _mapPanel;
        [Header("Case")] [SerializeField] TMP_Text _caseTitle, _objective, _location, _rules, _john, _noteTheory, _noteBody;
        [Header("Lists")] [SerializeField] TMP_Text _clueCount; [SerializeField] JournalEntry _clueTemplate, _personTemplate, _fileTemplate, _itemTemplate;
        [SerializeField] TMP_Text _clueNote, _clueDeduction, _personNoteTitle, _personNote, _fileNoteTitle, _fileNote, _itemName, _itemDesc;
        [Header("Map")] [SerializeField] RawImage _plan; [SerializeField] RectTransform _markers; [SerializeField] Image _markerHere, _markerObjective, _markerClue, _markerDanger;
        [SerializeField] TMP_Text _mapTitle, _mapSub;

        public Tab Current { get; private set; }
        readonly List<JournalEntry> _clues = new(), _people = new(), _files = new(), _items = new();
        readonly List<Image> _pool = new();
        Texture2D _planTex; Rect _world;
        static readonly HuntItem[] Breadcrumbs = { HuntItem.JohnReceipt, HuntItem.JohnMap, HuntItem.JohnPhoneRecord, HuntItem.JohnVoicemail };
        static readonly string[] Order = { "prologue", "ch02", "ch03", "ch04", "ch05", "ch06" };

        void Awake()
        {
            for(int i = 0; i < _tabs.Length; i++) { var t = (Tab)i; _tabs[i].onClick.AddListener(() => Select(t)); }
            foreach(var template in new[]{ _clueTemplate, _personTemplate, _fileTemplate, _itemTemplate }) template.gameObject.SetActive(false);
        }
        void OnDestroy() { if(_planTex) Destroy(_planTex); }

        public void Select(Tab tab) { Current = tab; Refresh(); }
        public void Step(int direction) => Select((Tab)(((int)Current + direction + 6) % 6));

        public override void Refresh()
        {
            if(!Hud) return;
            for(int i = 0; i < _tabs.Length; i++) { bool on = i == (int)Current; _tabUnderlines[i].enabled = on; _tabLabels[i].color = on ? UIStyle.Bone : UIStyle.Ash; }
            _casePanel.SetActive(Current == Tab.Case); _cluesPanel.SetActive(Current == Tab.Clues); _peoplePanel.SetActive(Current == Tab.People);
            _filesPanel.SetActive(Current == Tab.Files); _itemsPanel.SetActive(Current == Tab.Items); _mapPanel.SetActive(Current == Tab.Map);
            string ch = Hud.ChapterId, j = "Journal/" + ch + ".";
            switch(Current)
            {
                case Tab.Case:
                    _caseTitle.text = Loc.T(j + "case.title"); _objective.text = Loc.Src(Hud.session.Objective);
                    _location.text = Loc.T(j + "case.location.v"); _rules.text = Loc.T(j + "case.rules.v"); _john.text = Loc.T(j + "case.john.v");
                    _noteTheory.text = Loc.T(j + "case.theory.v"); _noteBody.text = Loc.T(j + "note.body"); break;
                case Tab.Clues: Clues(j); break;
                case Tab.People: People(); break;
                case Tab.Files: Files(ch); break;
                case Tab.Items: Items(); break;
                case Tab.Map: Map(); break;
            }
        }

        void Clues(string j)
        {
            var (found, total) = Hud.ClueProgress();
            _clueCount.text = found + " / " + total;
            var kinds = Hud.ClueKinds();
            Fill(_clues, _clueTemplate, kinds.Count);
            for(int i = 0; i < kinds.Count; i++)
            {
                var item = Hud.Item(kinds[i]); bool has = item && item.Used(Hud.session);
                _clues[i].Data = has ? item : null;
                _clues[i].Set(has ? "✓" : "—", Loc.T(has ? "ClueNames/" + kinds[i] : "Journal/clue.unknown"), has ? Loc.Src(item.description) : Loc.T("Journal/clue.unknown.d"), has);
                _clues[i].Button.onClick.RemoveAllListeners(); var captured = _clues[i]; _clues[i].Button.onClick.AddListener(() => Inspect(captured));
            }
            _clueNote.text = Loc.T(j + "note.body");
            _clueDeduction.text = found == total && total > 0 ? Loc.T(j + "note.deduction") : Loc.T("Journal/deduction.locked");
            Highlight(_clues);
        }

        void People()
        {
            var met = new List<HuntInteractable>();
            foreach(var item in Hud.session.items) if(item && Loc.Has("PeopleNames/" + item.kind) && item.Used(Hud.session) && !met.Exists(m => m.kind == item.kind)) met.Add(item);
            Fill(_people, _personTemplate, met.Count + 1);
            for(int i = 0; i < met.Count; i++) { _people[i].Data = met[i]; _people[i].Set("●", Loc.T("PeopleNames/" + met[i].kind), Loc.T("PeopleNames/" + met[i].kind + ".d"), true); }
            _people[met.Count].Data = null; _people[met.Count].Set("●", Loc.T("PeopleNames/John"), Loc.T("PeopleNames/John.d"), true);
            Highlight(_people);
        }

        void Files(string ch)
        {
            int chapter = System.Array.IndexOf(Order, ch);
            var files = new List<(HuntItem kind, HuntInteractable item)>();
            for(int i = 0; i < Breadcrumbs.Length; i++)
            {
                var item = Hud.Item(Breadcrumbs[i]);
                if(i + 1 < chapter || (item && item.Used(Hud.session))) files.Add((Breadcrumbs[i], item));
            }
            Fill(_files, _fileTemplate, files.Count);
            for(int i = 0; i < files.Count; i++) { _files[i].Data = files[i].item; _files[i].Set("▤", Loc.T("Files/" + files[i].kind), Loc.T("Files/" + files[i].kind + ".d"), true); _files[i].name = files[i].kind.ToString(); }
            if(files.Count == 0) { _fileNoteTitle.text = ""; _fileNote.text = Loc.T("Journal/files.none"); }
            Highlight(_files);
        }

        void Items()
        {
            var s = Hud.session; var c = Hud.combat;
            var list = new List<(string glyph, string name, string count, string desc, string slot)>();
            if(!s.stanford)
            {
                list.Add(("1911", "Items/weapon.1911", c.Ammo + "/" + c.Reserve, "Items/1911.desc", "1"));
                list.Add(("╱", "Items/weapon.knife", "", "Items/knife.desc", "2"));
                if(s.HasSpiritKit) list.Add(("┃", "Items/weapon.iron", "", "Items/iron.desc", "3"));
                else if(s.ghost && s.Has(HuntItem.Iron)) list.Add(("┃", "Items/weapon.poker", "", "Items/poker.desc", "3"));
                if(s.HasSpiritKit && s.Haunt) list.Add(("◌", "Items/weapon.salt", "×" + s.Haunt.SaltCharges, "Items/salt.desc", "4"));
            }
            list.Add(("✦", "Items/lighter", "", "Items/lighter.desc", "F"));
            Fill(_items, _itemTemplate, 8);
            for(int i = 0; i < 8; i++)
            {
                bool has = i < list.Count;
                _items[i].Data = has ? list[i].name + "|" + list[i].desc : null;
                _items[i].Set(has ? list[i].glyph : "", has ? list[i].slot : "", "", true, has ? list[i].count : "");
                _items[i].GetComponent<CanvasGroup>().alpha = has ? 1 : .35f; _items[i].Button.interactable = has;
            }
            Highlight(_items);
        }

        void Map()
        {
            var s = Hud.session;
            if(!_planTex) { _planTex = MapPlan.Build(s.player.transform.position, out _world); _plan.texture = _planTex; }
            string j = "Journal/" + Hud.ChapterId + ".";
            _mapTitle.text = Loc.English(j + "map.title"); _mapSub.text = Loc.English(j + "map.sub");
            foreach(var m in _pool) m.gameObject.SetActive(false);
            int used = 0;
            Place(ref used, _markerHere, s.player.transform.position, -s.player.view.Yaw);
            var (found, total) = Hud.ClueProgress();
            foreach(var item in s.items)
            {
                if(!item || item.Used(s)) continue;
                bool clue = Hud.ClueKinds().Contains(item.kind.ToString());
                bool goal = (s.Resolved && Hud.IsExit(item.kind)) || (!s.Resolved && found == total && Hud.IsAnchor(item.kind));
                if(goal) Place(ref used, _markerObjective, item.transform.position, 0);
                else if(clue) Place(ref used, _markerClue, item.transform.position, 0);
            }
            foreach(var threat in Hud.ActiveThreats()) Place(ref used, _markerDanger, threat, 0);
        }

        void Place(ref int used, Image template, Vector3 world, float rotation)
        {
            if(used >= _pool.Count) _pool.Add(Instantiate(template, _markers));
            var marker = _pool[used++]; marker.sprite = template.sprite; marker.color = template.color;
            marker.rectTransform.sizeDelta = template.rectTransform.sizeDelta; marker.gameObject.SetActive(true);
            Vector2 uv = MapPlan.ToMap(world, _world), size = _plan.rectTransform.rect.size;
            marker.rectTransform.anchorMin = marker.rectTransform.anchorMax = new Vector2(.5f, .5f);
            marker.rectTransform.anchoredPosition = (uv - new Vector2(.5f, .5f)) * size;
            marker.rectTransform.localEulerAngles = new Vector3(0, 0, rotation);
        }

        static void Fill(List<JournalEntry> list, JournalEntry template, int count)
        {
            while(list.Count < count) { var e = Instantiate(template, template.transform.parent); e.gameObject.SetActive(true); list.Add(e); }
            for(int i = 0; i < list.Count; i++) list[i].gameObject.SetActive(i < count);
        }

        void Highlight(List<JournalEntry> list)
        {
            var selected = UnityEngine.EventSystems.EventSystem.current ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject : null;
            JournalEntry current = null;
            foreach(var e in list) if(e.gameObject.activeSelf && e.gameObject == selected) current = e;
            if(!current) foreach(var e in list) if(e.gameObject.activeSelf) { current = e; break; }
            foreach(var e in list) { e.Highlight(e == current); e.Selected = Choose; }
            if(current) Describe(current);
        }

        void Choose(JournalEntry entry)
        {
            foreach(var list in new[]{ _clues, _people, _files, _items }) foreach(var e in list) if(list.Contains(entry)) e.Highlight(e == entry);
            Describe(entry);
        }

        void Describe(JournalEntry e)
        {
            if(Current == Tab.People)
            {
                var item = e.Data as HuntInteractable;
                _personNoteTitle.text = item ? Loc.T("PeopleNames/" + item.kind) : Loc.T("PeopleNames/John");
                _personNote.text = item ? "“" + Loc.Src(item.description) + "”" : Loc.T("Journal/" + Hud.ChapterId + ".case.john.v");
            }
            else if(Current == Tab.Files)
            {
                var item = e.Data as HuntInteractable; string kind = e.name;
                _fileNoteTitle.text = Loc.T("Files/" + kind);
                _fileNote.text = item && item.Used(Hud.session) ? Loc.Src(item.description) : Loc.T("Files/" + kind + ".d");
            }
            else if(Current == Tab.Items && e.Data is string d)
            {
                var parts = d.Split('|'); _itemName.text = Loc.T(parts[0]); _itemDesc.text = Loc.T(parts[1]);
            }
        }

        void Inspect(JournalEntry e) { if(e.Data is HuntInteractable item) Hud.OpenEvidence(item); }
    }
}
