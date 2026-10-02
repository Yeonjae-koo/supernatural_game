using BeforeJericho.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static BeforeJericho.Editor.UIKit;
namespace BeforeJericho.Editor
{
    /// <summary>Builds the uGUI prefabs that port Before Jericho UI.html (widgets → 12 screen prefabs → PF_HuntUI) and places PF_HuntUI in the slice scenes.
    /// Layout values are the prototype's CSS in cqw/cqh (1cqw = 16, 1cqh = 9 canvas units at the 1600×900 reference).</summary>
    public static class UIPrefabBuilder
    {
        const string Root = "Assets/_Game/Prefabs/UI", Tables = "Assets/_Game/Data/UI";
        static GameObject _menuButton, _keyHint, _optionRow, _subtitles, _entry, _slot, _tab, _sideTab;
        static readonly string[] Scenes = { "SCN_NewOrleans_Slice", "SCN_Atchafalaya_Slice", "SCN_EastTexas_Slice", "SCN_GrantsNM_Slice", "SCN_Winslow_Slice", "SCN_Stanford_Slice" };

        [MenuItem("Before Jericho/UI/Build UI Prefabs")]
        public static void Build()
        {
            System.IO.Directory.CreateDirectory(Root);
            Fonts(); Sprites();
            Widgets();
            var screens = new GameObject[]
            {
                Hud(), Card(), Pause(), Journal(), Evidence(), Dead(), Complete(), Credits(), Menu(), Settings(), Boot(), Dev()
            };
            RootPrefab(screens);
            AssetDatabase.SaveAssets();
            Debug.Log("UI prefabs built under " + Root);
        }

        [MenuItem("Before Jericho/UI/Integrate UI Into Slice Scenes")]
        public static string IntegrateAll()
        {
            // Opening a slice under HDRP/TMP marks it dirty by itself; only protect unsaved work in other scenes.
            var active = EditorSceneManager.GetActiveScene();
            if(active.isDirty && System.Array.IndexOf(Scenes, active.name) < 0) throw new System.InvalidOperationException("Save the open scene first.");
            var log = new System.Text.StringBuilder();
            foreach(string name in Scenes)
            {
                var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/" + name + ".unity");
                var session = Object.FindAnyObjectByType<HuntSession>();
                // Replace only the old HUD canvas subtree; the rest of the hierarchy is untouched.
                foreach(var old in Object.FindObjectsByType<HuntHUD>(FindObjectsInactive.Include)) Object.DestroyImmediate(old.gameObject);
                var hud = PlaceInScene(session);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                log.AppendLine(name + ": " + hud.name + ", EventSystems " + Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include).Length);
            }
            return log.ToString();
        }

        /// <summary>Instantiate PF_HuntUI in the open scene, wire scene references, keep exactly one EventSystem.</summary>
        public static HuntHUD PlaceInScene(HuntSession session)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/PF_HuntUI.prefab");
            if(!prefab) throw new System.InvalidOperationException("Build the UI prefabs first (Before Jericho/UI/Build UI Prefabs).");
            var hud = Instance<HuntHUD>(prefab, null, "Hunt HUD");
            hud.session = session; hud.combat = session.player.GetComponent<HunterCombat>(); session.hud = hud;
            Set(hud.GetComponent<SettingsApplier>(), "_session", session);
            var systems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
            for(int i = 1; i < systems.Length; i++) Object.DestroyImmediate(systems[i].gameObject);
            if(systems.Length == 0) { var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<InputSystemUIInputModule>(); }
            EditorUtility.SetDirty(session); EditorUtility.SetDirty(hud);
            return hud;
        }

        // ================= widgets =================
        static void Widgets()
        {
            _keyHint = KeyHintWidget(); _menuButton = MenuButtonWidget(); _subtitles = SubtitleWidget(); _optionRow = OptionRowWidget();
            _entry = EntryWidget(); _slot = SlotWidget(); _tab = TabWidget(); _sideTab = SideTabWidget();
        }

        static RectTransform NewRoot(string name) { var go = new GameObject(name, typeof(RectTransform)) { layer = 5 }; return (RectTransform)go.transform; }

        /// <summary>HTML .key cap: 1.5px bone border, radius .3cqw, black .45 fill, bold label.</summary>
        static (Image fill, TextMeshProUGUI label) KeyCap(Transform parent, float minCqw, float fontCqw, float lineAlpha = 1)
        {
            var fill = Img(parent, "Key", KeyFill, Over(0, 0, 0, .45f), Image.Type.Sliced);
            var line = Img(fill.transform, "Border", KeyLine, UIStyle.Alpha(Bone, lineAlpha), Image.Type.Sliced); Stretch(line.rectTransform);
            var label = Text(fill.transform, "Label", Kr7, fontCqw, Bone, null, "E", TextAlignmentOptions.Center, 0, 0, false, false);
            Hz(fill, 0, Pad(.5f * W, .5f * W, 0, 0), TextAnchor.MiddleCenter);
            LE(fill, -1, minCqw * W, -1, minCqw * W);
            LE(line, -1, -1, -1, -1, true);
            return (fill, label);
        }

        static GameObject KeyHintWidget()
        {
            var r = NewRoot("PF_UI_KeyHint"); Hz(r, .5f * W); Fit(r);
            var (_, key) = KeyCap(r, 1.7f, .85f); key.text = "Esc";
            Text(r, "Label", Kr4, 1f, Ash, "UI_Common/hint.back", "", TextAlignmentOptions.MidlineLeft, 0, 0, false, false);
            return Save(r.gameObject, Root + "/PF_UI_KeyHint.prefab");
        }

        static GameObject MenuButtonWidget()
        {
            var r = NewRoot("PF_UI_MenuButton");
            var hit = r.gameObject.AddComponent<Image>(); hit.color = Clear; hit.raycastTarget = true;
            V(r, .2f * W, Pad(0, 0, .55f * W, .55f * W)); Fit(r, false, true);
            var label = Text(r, "Label", Kr5, 1.75f, Ash, "UI_Common/menu.new", "", TextAlignmentOptions.TopLeft, .04f, 0, false, false);
            var small = Text(r, "Small", Kr4, .95f, Dim, null, "", TextAlignmentOptions.TopLeft, 0, 0, false, false); small.gameObject.SetActive(false);
            var dash = Img(label.transform, "Dash", White, Accent); Place(dash.rectTransform, new Vector2(0, .5f), new Vector2(-2f * W, 0), new Vector2(1.1f * W, 1), new Vector2(0, .5f));
            var button = r.gameObject.AddComponent<Button>(); button.transition = Selectable.Transition.None; button.targetGraphic = hit;
            var nav = button.navigation; nav.mode = Navigation.Mode.Vertical; button.navigation = nav;
            var mb = r.gameObject.AddComponent<MenuButton>(); Set(mb, "_label", label); Set(mb, "_small", small); Set(mb, "_dash", dash);
            return Save(r.gameObject, Root + "/PF_UI_MenuButton.prefab");
        }

        static (RectTransform line, TextMeshProUGUI text, Image back) SubLine(Transform parent, string name, Color color)
        {
            var back = Img(parent, name, White, Over(0, 0, 0, .66f));
            Hz(back, 0, Pad(.9f * W, .9f * W, .25f * W, .25f * W), TextAnchor.MiddleCenter); Fit(back);
            var t = Text(back.transform, "Text", Kr4, 1.3f, color, null, "", TextAlignmentOptions.Center, 0, 1.45f);
            LE(t, 900);
            return (back.rectTransform, t, back);
        }

        static GameObject SubtitleWidget()
        {
            var r = NewRoot("PF_UI_Subtitles");
            Place(r, new Vector2(.5f, 0), new Vector2(0, 7 * H), new Vector2(62 * W, 20 * H), new Vector2(.5f, 0));
            V(r, .5f * W, null, TextAnchor.LowerCenter, false);
            var (_, cap, capBack) = SubLine(r, "Caption", UIStyle.Hex(0xbdb8ab));
            var (_, speech, speechBack) = SubLine(r, "Line", Bone);
            var view = r.gameObject.AddComponent<SubtitleView>();
            Set(view, "_caption", cap); Set(view, "_speech", speech); Set(view, "_captionBack", capBack); Set(view, "_speechBack", speechBack);
            Set(view, "_plain", Kr4.material); Set(view, "_shadow", Kr4Shadow);
            return Save(r.gameObject, Root + "/PF_UI_Subtitles.prefab");
        }

        static GameObject OptionRowWidget()
        {
            var r = NewRoot("PF_UI_OptionRow");
            var focus = r.gameObject.AddComponent<Image>(); focus.color = Over(1, 1, 1, .04f); focus.raycastTarget = true;
            Hz(r, 1f * W, Pad(1f * W, 1f * W, .7f * W, .7f * W)); LE(r, -1, 2.6f * W); Fill(r);
            var bottom = Img(r, "Rule", White, Over(1, 1, 1, .05f)); LE(bottom, -1, -1, -1, -1, true); Place(bottom.rectTransform, new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 1), new Vector2(.5f, 0)); bottom.rectTransform.anchorMin = new Vector2(0, 0); bottom.rectTransform.anchorMax = new Vector2(1, 0);
            var label = Text(r, "Label", Kr4, 1.2f, Bone, null, "Option", TextAlignmentOptions.MidlineLeft, 0, 0, false, false); LE(label, -1, -1, 1);
            var value = Node(r, "Value"); LE(value, 18 * W, 1.6f * W);
            // ◀ value ▶
            var cycle = Node(value, "Cycle"); Stretch(cycle); Hz(cycle, .5f * W, null, TextAnchor.MiddleCenter);
            Button Arrow(string n, string glyph)
            {
                var t = Text(cycle, n, Kr4, 1.1f, Ash, null, glyph, TextAlignmentOptions.Center, 0, 0, false, false); t.raycastTarget = true; LE(t, 1.6f * W);
                var b = t.gameObject.AddComponent<Button>(); b.targetGraphic = t; b.transition = Selectable.Transition.None;
                var nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav; return b;
            }
            var prev = Arrow("Prev", "◀");
            var val = Text(cycle, "Current", Kr5, 1.2f, Bone, null, "On", TextAlignmentOptions.Center, 0, 0, false, false); LE(val, -1, -1, 1);
            var next = Arrow("Next", "▶");
            // slider
            var sliderRoot = Node(value, "Slider"); Stretch(sliderRoot);
            var track = Img(sliderRoot, "Track", White, UIStyle.Hex(0x3a3d40)); Place(track.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(0, 4)); track.rectTransform.anchorMin = new Vector2(0, .5f); track.rectTransform.anchorMax = new Vector2(1, .5f);
            var fillArea = Node(sliderRoot, "Fill Area"); Stretch(fillArea, 0, 0, 0, 0); fillArea.anchorMin = new Vector2(0, .5f); fillArea.anchorMax = new Vector2(1, .5f); fillArea.sizeDelta = new Vector2(0, 4);
            var fill = Img(fillArea, "Fill", White, Accent); Stretch(fill.rectTransform);
            var handleArea = Node(sliderRoot, "Handle Area"); Stretch(handleArea, 8, 0, 8, 0);
            var handle = Img(handleArea, "Handle", Knob, Accent, Image.Type.Simple, true); handle.rectTransform.sizeDelta = new Vector2(16, 16);
            var slider = sliderRoot.gameObject.AddComponent<Slider>(); slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight; var sn = slider.navigation; sn.mode = Navigation.Mode.None; slider.navigation = sn;
            // fixed
            var fixedGroup = Node(value, "Fixed"); Stretch(fixedGroup);
            var fixedText = Text(fixedGroup, "Text", Kr5, 1.2f, Dim, null, "English", TextAlignmentOptions.Center, 0, 0, false, false); Stretch(fixedText.rectTransform);
            // remap key (.rk)
            var keyBack = Img(value, "Key", Frame, Dim, Image.Type.Sliced, true); Place(keyBack.rectTransform, new Vector2(1, .5f), Vector2.zero, new Vector2(6 * W, 1.9f * W));
            var key = Text(keyBack.transform, "Label", Ui6, 1.05f, Bone, null, "E", TextAlignmentOptions.Center, 0, 0, false, false); Stretch(key.rectTransform, .6f * W, 0, .6f * W, 0);
            var keyButton = keyBack.gameObject.AddComponent<Button>(); keyButton.targetGraphic = keyBack; keyButton.transition = Selectable.Transition.None; var kn = keyButton.navigation; kn.mode = Navigation.Mode.None; keyButton.navigation = kn;
            var row = r.gameObject.AddComponent<OptionRow>(); row.transition = Selectable.Transition.None; row.targetGraphic = focus;
            Set(row, "_label", label); Set(row, "_value", val); Set(row, "_key", key); Set(row, "_fixedText", fixedText);
            Set(row, "_prev", prev); Set(row, "_next", next); Set(row, "_keyButton", keyButton); Set(row, "_slider", slider);
            Set(row, "_cycleGroup", cycle.gameObject); Set(row, "_fixedGroup", fixedGroup.gameObject); Set(row, "_focus", focus);
            focus.enabled = false;
            return Save(r.gameObject, Root + "/PF_UI_OptionRow.prefab");
        }

        static GameObject EntryWidget()
        {
            var r = NewRoot("PF_UI_JournalEntry");
            var hit = r.gameObject.AddComponent<Image>(); hit.color = Clear; hit.raycastTarget = true;
            Hz(r, .6f * W, Pad(.8f * W, .8f * W, .8f * W, .8f * W), TextAnchor.UpperLeft); Fill(r);
            var fill = Img(r, "Selected", White, Over(1, 1, 1, .04f)); LE(fill, -1, -1, -1, -1, true); Stretch(fill.rectTransform);
            var edge = Img(r, "Edge", White, Accent); LE(edge, -1, -1, -1, -1, true); edge.rectTransform.anchorMin = Vector2.zero; edge.rectTransform.anchorMax = new Vector2(0, 1); edge.rectTransform.pivot = new Vector2(0, .5f); edge.rectTransform.sizeDelta = new Vector2(2, 0); edge.rectTransform.anchoredPosition = Vector2.zero;
            var icon = Text(r, "Icon", Kr4, 1.1f, Cold, null, "✓", TextAlignmentOptions.TopLeft, 0, 0, false, false); LE(icon, 2 * W);
            var col = Node(r, "Text"); V(col, 0); LE(col, -1, -1, 1);
            var name = Text(col, "Name", Kr5, 1.25f, Bone, null, "Name");
            var detail = Text(col, "Detail", Kr4, 1.05f, Ash, null, "Detail", TextAlignmentOptions.TopLeft, 0, 1.5f);
            var button = r.gameObject.AddComponent<Button>(); button.transition = Selectable.Transition.None; button.targetGraphic = hit;
            var nav = button.navigation; nav.mode = Navigation.Mode.Vertical; button.navigation = nav;
            var e = r.gameObject.AddComponent<JournalEntry>(); Set(e, "_icon", icon); Set(e, "_name", name); Set(e, "_detail", detail); Set(e, "_selectedFill", fill); Set(e, "_selectedEdge", edge);
            fill.enabled = edge.enabled = false;
            return Save(r.gameObject, Root + "/PF_UI_JournalEntry.prefab");
        }

        static GameObject SlotWidget()
        {
            var r = NewRoot("PF_UI_ItemSlot");
            var bg = r.gameObject.AddComponent<Image>(); bg.color = Over(1, 1, 1, .03f); bg.raycastTarget = true;
            r.gameObject.AddComponent<CanvasGroup>();
            var frame = Img(r, "Frame", Frame, Line, Image.Type.Sliced); Stretch(frame.rectTransform);
            var sel = Img(r, "Selected", Frame, Accent, Image.Type.Sliced); Stretch(sel.rectTransform);
            var key = Text(r, "Key", Kr4, .9f, Ash, null, "1"); Stretch(key.rectTransform, .6f * W, .6f * W, .6f * W, .6f * W);
            var glyph = Text(r, "Glyph", Kr4, 2f, Ash, null, "1911", TextAlignmentOptions.Center, 0, 0, false, false); Stretch(glyph.rectTransform);
            var star = Img(r, "Star", Star4, Ash); Place(star.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(2 * W, 2 * W));
            var count = Text(r, "Count", Ui4, .95f, Bone, null, "", TextAlignmentOptions.BottomRight); Stretch(count.rectTransform, .6f * W, .6f * W, .6f * W, .6f * W);
            var button = r.gameObject.AddComponent<Button>(); button.transition = Selectable.Transition.None; button.targetGraphic = bg;
            var e = r.gameObject.AddComponent<JournalEntry>(); Set(e, "_icon", glyph); Set(e, "_name", key); Set(e, "_count", count); Set(e, "_selectedEdge", sel); Set(e, "_iconImage", star);
            sel.enabled = false; star.enabled = false;
            return Save(r.gameObject, Root + "/PF_UI_ItemSlot.prefab");
        }

        static GameObject TabWidget()
        {
            var r = NewRoot("PF_UI_Tab"); var hit = r.gameObject.AddComponent<Image>(); hit.color = Clear; hit.raycastTarget = true;
            V(r, 0, Pad(0, 0, .3f * W, .6f * W)); Fit(r);
            var label = Text(r, "Label", Kr5, 1.25f, Ash, "Journal/tab.case", "", TextAlignmentOptions.Center, 0, 0, false, false);
            var line = Img(r, "Underline", White, Accent); LE(line, -1, -1, -1, -1, true); line.rectTransform.anchorMin = Vector2.zero; line.rectTransform.anchorMax = new Vector2(1, 0); line.rectTransform.pivot = new Vector2(.5f, 0); line.rectTransform.sizeDelta = new Vector2(0, 2); line.rectTransform.anchoredPosition = Vector2.zero;
            var b = r.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.targetGraphic = hit; var nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav;
            return Save(r.gameObject, Root + "/PF_UI_Tab.prefab");
        }

        static GameObject SideTabWidget()
        {
            var r = NewRoot("PF_UI_SideTab"); var bg = r.gameObject.AddComponent<Image>(); bg.color = Clear; bg.raycastTarget = true;
            Hz(r, 0, Pad(1f * W, 1f * W, .7f * W, .7f * W)); LE(r, -1, 2.6f * W); Fill(r);
            var mark = Img(r, "Selected", White, Over(1, 1, 1, .04f)); LE(mark, -1, -1, -1, -1, true); Stretch(mark.rectTransform);
            var edge = Img(mark.transform, "Edge", White, Accent); edge.rectTransform.anchorMin = Vector2.zero; edge.rectTransform.anchorMax = new Vector2(0, 1); edge.rectTransform.pivot = new Vector2(0, .5f); edge.rectTransform.sizeDelta = new Vector2(2, 0);
            Text(r, "Label", Kr5, 1.2f, Ash, "Settings/tab.gameplay", "", TextAlignmentOptions.MidlineLeft, 0, 0, false, false);
            var b = r.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.targetGraphic = bg; var nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav;
            return Save(r.gameObject, Root + "/PF_UI_SideTab.prefab");
        }

        // ---------- shared screen parts ----------
        static RectTransform Screen(string name) { var r = NewRoot(name); Stretch(r); return r; }
        static T View<T>(RectTransform r, Selectable first = null) where T : UIView { var v = r.gameObject.AddComponent<T>(); if(first) Set(v, "_first", first); return v; }
        static void Hints(Transform parent, params (string key, string label)[] items)
        {
            var bar = Node(parent, "Hints"); BottomRight(bar, 3, 4, 10, 10); bar.pivot = new Vector2(1, 0); Hz(bar, 1.6f * W, null, TextAnchor.MiddleRight); Fit(bar);
            foreach(var (key, label) in items)
            {
                var hint = Instance<RectTransform>(_keyHint, bar, "Hint " + key);
                hint.GetComponentInChildren<TextMeshProUGUI>().text = key; // key cap label (first TMP in hierarchy)
                Set(hint.Find("Label").GetComponent<LocText>(), "_key", label);
            }
        }
        static MenuButton MenuBtn(Transform parent, string name, string key) { var b = Instance<MenuButton>(_menuButton, parent, name); Set(b.transform.Find("Label").GetComponent<LocText>(), "_key", key); return b; }
        static RectTransform Column(Transform parent, string name, float leftCqw, float topCqh, float widthCqw, float spacing = 0)
        { var r = Node(parent, name); TopLeft(r, leftCqw, topCqh, widthCqw * W, 10); V(r, spacing); Fit(r, false, true); return r; }
        static void DimBackground(RectTransform r, float alpha = .86f) { var i = Img(r, "Dim", White, new Color(8 / 255f, 9 / 255f, 10 / 255f, alpha), Image.Type.Simple, true); Stretch(i.rectTransform); }
        static TextMeshProUGUI Label(Transform p, string name, string key, float cqw = .9f, float letter = .24f, Color? c = null) => Text(p, name, Ui6, cqw, c ?? Dim, key, "", TextAlignmentOptions.TopLeft, letter, 0, true);

        /// <summary>HTML .noteb: paper #cbbf9f, typewriter text, slight rotation, drop shadow.</summary>
        static (RectTransform paper, TextMeshProUGUI title, TextMeshProUGUI body, TextMeshProUGUI ded) NotePaper(Transform parent, string name, float fontCqw = 1.25f, bool krBody = false)
        {
            var holder = Node(parent, name);
            var shadow = Img(holder, "Shadow", SoftShadow, Over(0, 0, 0, .5f), Image.Type.Sliced); LE(shadow, -1, -1, -1, -1, true);
            var paper = Img(holder, "Paper", White, UIStyle.Hex(0xcbbf9f)); LE(paper, -1, -1, 1);
            V(holder, 0); Fit(holder, false, true);
            V(paper, 0, Pad(1.8f * W, 1.8f * W, 1.8f * W, 1.8f * W));
            var ink = UIStyle.Hex(0x2a2418);
            var title = Text(paper.transform, "Title", Type, 1.5f, ink, null, "", TextAlignmentOptions.TopLeft); title.fontStyle |= FontStyles.Underline;
            Spacer(paper.transform, .6f * W);
            var body = Text(paper.transform, "Body", krBody ? Kr4 : Type, fontCqw, ink, null, "", TextAlignmentOptions.TopLeft, 0, 1.7f);
            Spacer(paper.transform, 1f * W);
            var rule = Img(paper.transform, "Dashed", DashH, UIStyle.Hex(0x6b5f45), Image.Type.Tiled); LE(rule, -1, 1); Fill(rule);
            Spacer(paper.transform, .8f * W);
            var ded = Text(paper.transform, "Deduction", Type, fontCqw, ink, null, "", TextAlignmentOptions.TopLeft, 0, 1.7f);
            holder.localEulerAngles = new Vector3(0, 0, -.6f);
            Stretch(shadow.rectTransform, -1.2f * W, 0, -1.2f * W, -2.4f * W); shadow.rectTransform.anchoredPosition = new Vector2(0, -1f * W);
            return (holder, title, body, ded);
        }

        // ================= screens =================
        static GameObject Boot()
        {
            var r = Screen("PF_UI_Boot");
            var bg = Img(r, "Background", White, UIStyle.Hex(0x060707), Image.Type.Simple, true); Stretch(bg.rectTransform);
            var box = Node(r, "Box"); Place(box, new Vector2(.5f, .5f), Vector2.zero, new Vector2(52 * W, 10)); V(box, 0); Fit(box, false, true);
            Text(box, "Title", Ui8, 1.2f, Ash, "System/boot.title", "", TextAlignmentOptions.TopLeft, .34f); Spacer(box, .8f * W);
            Text(box, "Body", Kr4, 1.2f, Body, "System/boot.body", "", TextAlignmentOptions.TopLeft, 0, 1.6f); Spacer(box, 2f * W);
            var rows = new OptionRow[4]; for(int i = 0; i < 4; i++) rows[i] = Instance<OptionRow>(_optionRow, box, "Option " + i);
            Spacer(box, 2f * W);
            var pv = Img(box, "Preview", PreviewBg, Color.white); LE(pv, -1, 12 * H); Fill(pv); Stretch(Img(pv.transform, "Border", Frame, Line, Image.Type.Sliced).rectTransform);
            var sub = Instance<SubtitleView>(_subtitles, pv.transform, "Preview subtitles"); Place((RectTransform)sub.transform, new Vector2(.5f, 0), new Vector2(0, 1.5f * H), new Vector2(49 * W, 10 * H), new Vector2(.5f, 0));
            Set(sub, "_preview", true); Set(sub, "_maxWidth", 47f * W);
            Spacer(box, 1f * W);
            var goRow = Node(box, "Confirm row"); Hz(goRow, 0);
            var go = Img(goRow, "Confirm", White, Bone, Image.Type.Simple, true); Hz(go, 0, Pad(2 * W, 2 * W, .9f * W, .9f * W)); Fit(go);
            Text(go.transform, "Label", Kr7, 1.2f, UIStyle.Hex(0x111111), "System/boot.start", "", TextAlignmentOptions.Center, 0, 0, false, false);
            var btn = go.gameObject.AddComponent<Button>(); btn.targetGraphic = go;
            var v = View<BootView>(r); Set(v, "_rows", rows); Set(v, "_confirm", btn); Set(v, "_preview", sub);
            return Save(r.gameObject, Root + "/PF_UI_Boot.prefab");
        }

        static GameObject Menu()
        {
            var r = Screen("PF_UI_Menu");
            var bg = Img(r, "Background", MenuBg, Color.white, Image.Type.Simple, true); Stretch(bg.rectTransform);
            var logo = Column(r, "Logo", 7, 14, 60);
            var small = Text(logo, "Supernatural", Ui6, 1f, Ash, null, "SUPERNATURAL", TextAlignmentOptions.TopLeft, .5f); Spacer(logo, 1.2f * W);
            Text(logo, "Title", Ui8, 5.4f, Bone, null, "BEFORE\nJERICHO", TextAlignmentOptions.TopLeft, .14f, .95f);
            var list = Column(r, "Menu", 7, 49, 40); // prototype: 52cqh; the extra NEW GAME chapter line needs the room
            var cont = MenuBtn(list, "Continue", "UI_Common/menu.continue"); var newGame = MenuBtn(list, "New game", "UI_Common/menu.new");
            var load = MenuBtn(list, "Load", "UI_Common/menu.load"); var options = MenuBtn(list, "Options", "UI_Common/menu.options");
            var credits = MenuBtn(list, "Credits", "UI_Common/menu.credits"); var quit = MenuBtn(list, "Quit", "UI_Common/menu.quit");
            var disclaimer = Text(r, "Disclaimer", Kr4, 1f, Dim, "UI_Common/menu.disclaimer"); BottomLeft(disclaimer.rectTransform, 7, 4, 70 * W, 2 * W);
            disclaimer.alignment = TextAlignmentOptions.BottomLeft;
            Hints(r, ("Enter", "UI_Common/hint.select"), ("Esc", "UI_Common/hint.back"));
            var v = View<MenuView>(r, cont.Button);
            Set(v, "_logo", small); Set(v, "_continue", cont); Set(v, "_newGame", newGame); Set(v, "_load", load); Set(v, "_options", options); Set(v, "_credits", credits); Set(v, "_quit", quit);
            return Save(r.gameObject, Root + "/PF_UI_Menu.prefab");
        }

        static GameObject Card()
        {
            var r = Screen("PF_UI_Card"); var group = r.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false;
            var bg = Img(r, "Background", CardBg, Color.white); Stretch(bg.rectTransform);
            var card = Node(r, "Card"); BottomLeft(card, 6, 14, 70 * W, 10); card.pivot = new Vector2(0, 0); V(card, 0); Fit(card, false, true);
            var chapter = Text(card, "Chapter", Ui6, 1f, Cold, null, "", TextAlignmentOptions.TopLeft, .4f); Spacer(card, 1f * W);
            var place = Text(card, "Place", Ui8, 4.4f, Bone, null, "", TextAlignmentOptions.TopLeft, .08f, 1f);
            var placeSub = Text(card, "Place ko", Kr4, 1.35f, Ash, null, ""); Spacer(card, 1.6f * W);
            var hr = Img(card, "Rule", White, Accent); LE(hr, 5 * W, 1); Spacer(card, 2f * W);
            var date = Text(card, "Date", Ui6, 1.4f, Body, null, "", TextAlignmentOptions.TopLeft, .3f);
            var dateSub = Text(card, "Date ko", Kr4, 1.35f, Ash, null, "");
            Hints(r, ("Space", "UI_Common/hint.skip"));
            var v = View<CardView>(r); Set(v, "_group", group); Set(v, "_chapter", chapter); Set(v, "_place", place); Set(v, "_placeSub", placeSub); Set(v, "_date", date); Set(v, "_dateSub", dateSub);
            return Save(r.gameObject, Root + "/PF_UI_Card.prefab");
        }

        static GameObject Hud()
        {
            var r = Screen("PF_UI_HUD"); var layer = r.gameObject.AddComponent<CanvasGroup>(); layer.blocksRaycasts = false; layer.interactable = false;
            var hurtLow = Img(r, "Hurt low", Hurt, Color.white); Stretch(hurtLow.rectTransform);
            var hurtFlash = Img(r, "Hurt flash", HurtFlash, Color.white); Stretch(hurtFlash.rectTransform);
            // objective toast
            var obj = Column(r, "Objective toast", 3.2f, 5, 50); var objGroup = obj.gameObject.AddComponent<CanvasGroup>();
            var objLine = Img(obj, "Rule", White, Accent); LE(objLine, 3 * W, 1); Spacer(obj, .7f * W);
            Text(obj, "Label", Ui6, .95f, Accent, "Objectives/new", "", TextAlignmentOptions.TopLeft, .22f, 0, true); Spacer(obj, .4f * W);
            var objText = Text(obj, "Objective", Kr5, 1.75f, Bone, null, ""); objText.fontSharedMaterial = Kr5Shadow;
            // tutorial card
            var tut = Node(r, "Tutorial"); TopLeft(tut, 3.2f, 16, 28 * W, 10); var tutGroup = tut.gameObject.AddComponent<CanvasGroup>();
            var tutBg = tut.gameObject.AddComponent<Image>(); tutBg.color = Over(10 / 255f, 11 / 255f, 12 / 255f, .78f); tutBg.sprite = White;
            V(tut, 0, Pad(1.1f * W + 2, 1.1f * W, .9f * W, .9f * W)); Fit(tut, false, true);
            var tutEdge = Img(tut, "Edge", White, Accent); LE(tutEdge, -1, -1, -1, -1, true); tutEdge.rectTransform.anchorMin = Vector2.zero; tutEdge.rectTransform.anchorMax = new Vector2(0, 1); tutEdge.rectTransform.pivot = new Vector2(0, .5f); tutEdge.rectTransform.sizeDelta = new Vector2(2, 0);
            Text(tut, "Label", Ui6, .85f, Accent, "Tutorial/label", "", TextAlignmentOptions.TopLeft, .16f, 0, true); Spacer(tut, .3f * W);
            var tutTitle = Text(tut, "Title", Kr7, 1.3f, Bone, null, ""); Spacer(tut, .3f * W);
            var tutBody = Text(tut, "Body", Kr4, 1.1f, Body, null, "", TextAlignmentOptions.TopLeft, 0, 1.5f);
            // saving
            var save = Node(r, "Saving"); TopRight(save, 3, 5, 10, 10); save.pivot = new Vector2(1, 1); Hz(save, .6f * W, null, TextAnchor.MiddleRight); Fit(save); var saveGroup = save.gameObject.AddComponent<CanvasGroup>();
            var spin = Img(save, "Spinner", Spinner, Ash); LE(spin, 1.3f * W, 1.3f * W);
            Text(save, "Label", Ui4, .9f, Ash, "System/saving", "", TextAlignmentOptions.MidlineLeft, .16f, 0, true, false);
            // journal notify
            var note = Img(r, "Journal notify", NotifyBg, Color.white); var noteRt = note.rectTransform; TopRight(noteRt, 3, 40, 10, 10); noteRt.pivot = new Vector2(1, 1);
            Hz(note, 1f * W, Pad(4 * W, 1.2f * W, .8f * W, .8f * W)); Fit(note); var noteGroup = note.gameObject.AddComponent<CanvasGroup>();
            var icon = Img(note.transform, "Icon", Frame, Ash, Image.Type.Sliced); LE(icon, 3 * W, 3 * W);
            Stretch(Text(icon.transform, "Glyph", Kr4, 1.4f, Bone, null, "▤", TextAlignmentOptions.Center, 0, 0, false, false).rectTransform);
            var noteCol = Node(note.transform, "Text"); V(noteCol, 0);
            Text(noteCol, "Label", Ui6, .85f, Cold, "Journal/added", "", TextAlignmentOptions.TopLeft, .18f, 0, true, false);
            var noteName = Text(noteCol, "Name", Kr5, 1.3f, Bone, null, "", TextAlignmentOptions.TopLeft, 0, 0, false, false);
            // interaction prompt
            var prompt = Node(r, "Prompt"); Place(prompt, new Vector2(.5f, 1), new Vector2(0, -63 * H), new Vector2(10, 10), new Vector2(.5f, 1)); Hz(prompt, .8f * W, null, TextAnchor.MiddleCenter); Fit(prompt);
            var (pk, pkl) = KeyCap(prompt, 2.1f, 1.05f);
            var pText = Text(prompt, "Verb", Kr4, 1.35f, Bone, null, "", TextAlignmentOptions.MidlineLeft, 0, 0, false, false); pText.fontSharedMaterial = Kr4Shadow;
            // ritual hold ring
            var ritual = Node(r, "Ritual"); Place(ritual, new Vector2(.5f, 1), new Vector2(0, -58 * H), new Vector2(10, 10), new Vector2(.5f, 1)); Hz(ritual, .8f * W, null, TextAnchor.MiddleCenter); Fit(ritual);
            var ringBox = Node(ritual, "Ring"); LE(ringBox, 4.4f * W, 4.4f * W);
            var ringBg = Img(ringBox, "Track", Ring, Over(1, 1, 1, .18f)); Stretch(ringBg.rectTransform);
            var ring = Img(ringBox, "Progress", Ring, Ember, Image.Type.Filled); Stretch(ring.rectTransform); ring.fillMethod = Image.FillMethod.Radial360; ring.fillOrigin = (int)Image.Origin360.Top; ring.fillClockwise = true; ring.fillAmount = 0;
            var (rk, rkl) = KeyCap(ringBox, 2.1f, 1.05f); Place(rk.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(2.1f * W, 2.1f * W)); Object.DestroyImmediate(rk.GetComponent<HorizontalLayoutGroup>());
            var rCol = Node(ritual, "Text"); V(rCol, 0);
            var burn = Text(rCol, "Verb", Kr4, 1.35f, Bone, "UI_Common/prompt.hold_burn", "", TextAlignmentOptions.TopLeft, 0, 0, false, false); burn.fontSharedMaterial = Kr4Shadow;
            var rSub = Text(rCol, "Sub", Kr4, 1.05f, Ash, null, "", TextAlignmentOptions.TopLeft, 0, 0, false, false); rSub.fontSharedMaterial = Kr4Shadow;
            // weapon
            var wpn = Node(r, "Weapon"); BottomRight(wpn, 3, 5, 20 * W, 10); wpn.pivot = new Vector2(1, 0); V(wpn, 0, null, TextAnchor.UpperRight, false); Fit(wpn, false, true); var wGroup = wpn.gameObject.AddComponent<CanvasGroup>();
            var ammoRow = Node(wpn, "Ammo"); Hz(ammoRow, 0, null, TextAnchor.LowerRight); Fit(ammoRow);
            var ammo = Text(ammoRow, "Count", Ui8, 3.4f, Bone, null, "7", TextAlignmentOptions.BottomRight, .02f, 1f, false, false);
            var ammoSub = Text(ammoRow, "Reserve", Ui4, 1.8f, Ash, null, " / 21", TextAlignmentOptions.BottomLeft, 0, 1f, false, false);
            Spacer(wpn, .4f * W);
            var wName = Text(wpn, "Name", Ui4, 1f, Ash, null, "1911", TextAlignmentOptions.TopRight, .2f, 0, true, false);
            var reloading = Text(wpn, "Reloading", Ui4, 1f, Accent, "UI_Common/hud.reloading", "", TextAlignmentOptions.TopRight, .2f, 0, true, false);
            Spacer(wpn, .7f * W);
            var slots = Node(wpn, "Slots"); Hz(slots, .4f * W, null, TextAnchor.MiddleRight); Fit(slots);
            var frames = new Image[4]; var labels = new TMP_Text[4];
            for(int i = 0; i < 4; i++) { frames[i] = Img(slots, "Slot " + (i + 1), Frame, Dim, Image.Type.Sliced); LE(frames[i], 1.6f * W, 1.6f * W); labels[i] = Text(frames[i].transform, "N", Ui6, .8f, Dim, null, (i + 1).ToString(), TextAlignmentOptions.Center, 0, 0, false, false); Stretch(labels[i].rectTransform); }
            // lighter
            var lighter = Node(r, "Lighter"); BottomRight(lighter, 3, 22, 10, 10); lighter.pivot = new Vector2(1, 0); Hz(lighter, .4f * W, null, TextAnchor.MiddleRight); Fit(lighter);
            var ls = Img(lighter, "Star", Star4, Ember); LE(ls, .9f * W, .9f * W);
            Text(lighter, "Label", Ui4, .9f, Ember, "UI_Common/hud.lighter", "", TextAlignmentOptions.MidlineLeft, .2f, 0, true, false);
            // health
            var hp = Node(r, "Health"); BottomLeft(hp, 3, 5, 14 * W, 10); hp.pivot = new Vector2(0, 0); V(hp, 0); Fit(hp, false, true); var hpGroup = hp.gameObject.AddComponent<CanvasGroup>();
            Text(hp, "Label", Ui4, .85f, Ash, "UI_Common/hud.health", "", TextAlignmentOptions.TopLeft, .2f); Spacer(hp, .4f * W);
            var bar = Img(hp, "Bar", White, Over(1, 1, 1, .12f)); LE(bar, -1, .45f * W); Fill(bar);
            var hpFill = Img(bar.transform, "Fill", White, Bone, Image.Type.Filled); Stretch(hpFill.rectTransform); hpFill.fillMethod = Image.FillMethod.Horizontal;
            // crosshair
            var dot = Img(r, "Crosshair dot", Dot, Bone); Place(dot.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(.35f * W, .35f * W));
            var cross = Node(r, "Crosshair cross"); Place(cross, new Vector2(.5f, .5f), Vector2.zero, new Vector2(2.4f * W, 2.4f * W));
            Place(Img(cross, "H", White, Bone).rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(2.4f * W, 1));
            Place(Img(cross, "V", White, Bone).rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1, 2.4f * W));
            var subs = Instance<SubtitleView>(_subtitles, r, "Subtitles");
            var v = View<HudView>(r);
            Set(v, "_layer", layer); Set(v, "_objective", objGroup); Set(v, "_objectiveText", objText); Set(v, "_tutorial", tutGroup); Set(v, "_tutorialTitle", tutTitle); Set(v, "_tutorialBody", tutBody);
            Set(v, "_saving", saveGroup); Set(v, "_spinner", spin.rectTransform); Set(v, "_notify", noteGroup); Set(v, "_notifyName", noteName);
            Set(v, "_prompt", prompt.gameObject); Set(v, "_promptKeyBack", pk); Set(v, "_promptKey", pkl); Set(v, "_promptText", pText);
            Set(v, "_ritual", ritual.gameObject); Set(v, "_ring", ring); Set(v, "_ritualKeyBack", rk); Set(v, "_ritualKey", rkl); Set(v, "_ritualSub", rSub);
            Set(v, "_weapon", wGroup); Set(v, "_ammo", ammo); Set(v, "_ammoSub", ammoSub); Set(v, "_weaponName", wName); Set(v, "_reloading", reloading); Set(v, "_slotFrames", frames); Set(v, "_slotLabels", labels);
            Set(v, "_lighter", lighter.gameObject); Set(v, "_health", hpGroup); Set(v, "_healthFill", hpFill); Set(v, "_hurtFlash", hurtFlash); Set(v, "_hurtLow", hurtLow);
            Set(v, "_dot", dot.gameObject); Set(v, "_cross", cross.gameObject); Set(v, "_subtitles", subs);
            foreach(var g in new[]{ objGroup, tutGroup, saveGroup, noteGroup, wGroup, hpGroup }) g.alpha = 0;
            prompt.gameObject.SetActive(false); ritual.gameObject.SetActive(false); lighter.gameObject.SetActive(false); dot.gameObject.SetActive(false); cross.gameObject.SetActive(false);
            hurtFlash.enabled = hurtLow.enabled = false;
            return Save(r.gameObject, Root + "/PF_UI_HUD.prefab");
        }

        static GameObject Evidence()
        {
            var r = Screen("PF_UI_Evidence"); var bg = Img(r, "Dim", White, Over(5 / 255f, 5 / 255f, 5 / 255f, .82f), Image.Type.Simple, true); Stretch(bg.rectTransform);
            var holder = Node(r, "Paper"); TopLeft(holder, 8, 8, 40 * W, 82 * H); holder.localEulerAngles = new Vector3(0, 0, 1.6f);
            var shadow = Img(holder, "Shadow", SoftShadow, Over(0, 0, 0, .7f), Image.Type.Sliced); Stretch(shadow.rectTransform, -3 * W, -1 * W, -3 * W, -5 * W);
            var paper = Img(holder, "Sheet", White, UIStyle.Hex(0xd9d0b8)); Stretch(paper.rectTransform); paper.gameObject.AddComponent<RectMask2D>();
            var ink = UIStyle.Hex(0x1e1a14);
            var front = Node(paper.transform, "Front"); Stretch(front, 2.2f * W, 2 * W, 2.2f * W, 2 * W); V(front, 0);
            var mast = Fill(Text(front, "Masthead", Paper7, 2.2f, ink, null, "", TextAlignmentOptions.Top, .04f));
            Spacer(front, .4f * W); var d1 = Img(front, "Rule 1", White, ink); LE(d1, -1, 1); Fill(d1); Spacer(front, 1); var d2 = Img(front, "Rule 2", White, ink); LE(d2, -1, 1); Fill(d2);
            var dl = Node(front, "Dateline"); Hz(dl, 0, Pad(0, 0, .3f * W, .3f * W)); Fill(dl);
            var dA = Text(dl, "Left", Paper4, .85f, ink, null, "", TextAlignmentOptions.Left, 0, 0, false, false); LE(dA, -1, -1, 1);
            var dB = Text(dl, "Mid", Paper4, .85f, ink, null, "", TextAlignmentOptions.Center, 0, 0, false, false); LE(dB, -1, -1, 1);
            var dC = Text(dl, "Right", Paper4, .85f, ink, null, "", TextAlignmentOptions.Right, 0, 0, false, false); LE(dC, -1, -1, 1);
            var d3 = Img(front, "Rule 3", White, ink); LE(d3, -1, 1); Fill(d3); Spacer(front, 1.2f * W);
            var headline = Fill(Text(front, "Headline", Paper7, 2.1f, ink, null, "", TextAlignmentOptions.Top, 0, 1.1f)); Spacer(front, 1f * W);
            var photoBg = Img(front, "Photo", PhotoBg, Color.white); LE(photoBg, -1, 14 * H); Fill(photoBg);
            var photo = Text(photoBg.transform, "Caption", MonoFont, .8f, UIStyle.Hex(0xcfc6ad), null, "", TextAlignmentOptions.Center); Stretch(photo.rectTransform);
            Spacer(front, 1f * W);
            var cols = Node(front, "Columns"); Hz(cols, 1.4f * W, null, TextAnchor.UpperLeft); LE(cols, -1, 30 * H); Fill(cols);
            var colA = Text(cols, "Column A", Paper4, 1.05f, ink, null, "", TextAlignmentOptions.TopJustified, 0, 1.45f); LE(colA, -1, 30 * H, 1);
            var colB = Text(cols, "Column B", Paper4, 1.05f, ink, null, "", TextAlignmentOptions.TopJustified, 0, 1.45f); LE(colB, -1, 30 * H, 1);
            colA.overflowMode = TextOverflowModes.Linked; colA.linkedTextComponent = colB; colB.overflowMode = TextOverflowModes.Truncate;
            var back = Node(paper.transform, "Back"); Stretch(back, 3 * W, 3 * W, 3 * W, 3 * W);
            var backNote = Text(back, "Note", Type, 1.25f, UIStyle.Hex(0x6b5f45), null, "", TextAlignmentOptions.Center, .04f, 1.7f); Stretch(backNote.rectTransform);
            back.gameObject.SetActive(false);
            var tint = Img(paper.transform, "Tint", PaperTint, Color.white); Stretch(tint.rectTransform);
            var evName = Text(r, "Name", Ui4, 1f, Ash, null, "", TextAlignmentOptions.TopRight, .2f, 0, true); TopRight(evName.rectTransform, 5, 5, 38 * W, 2 * W);
            var tr = Img(r, "Transcript", White, Over(12 / 255f, 13 / 255f, 14 / 255f, .9f)); var trRt = tr.rectTransform; TopRight(trRt, 5, 12, 38 * W, 10);
            V(tr, 0, Pad(1.8f * W, 1.8f * W, 1.6f * W, 1.6f * W)); Fit(tr, false, true);
            var trTop = Img(tr.transform, "Edge", White, Accent); LE(trTop, -1, -1, -1, -1, true); trTop.rectTransform.anchorMin = new Vector2(0, 1); trTop.rectTransform.anchorMax = Vector2.one; trTop.rectTransform.pivot = new Vector2(.5f, 1); trTop.rectTransform.sizeDelta = new Vector2(0, 1);
            Text(tr.transform, "Label", Ui6, .9f, Cold, "UI_Common/inspect.translate", "", TextAlignmentOptions.TopLeft, .2f, 0, true); Spacer(tr.transform, .8f * W);
            var trTitle = Text(tr.transform, "Title", Kr7, 1.6f, Bone, null, "", TextAlignmentOptions.TopLeft, 0, 1.35f); Spacer(tr.transform, .8f * W);
            var trBody = Text(tr.transform, "Body", Kr4, 1.15f, Body, null, "", TextAlignmentOptions.TopLeft, 0, 1.7f); Spacer(tr.transform, 1f * W);
            var trRule = Img(tr.transform, "Rule", White, Line); LE(trRule, -1, 1); Fill(trRule); Spacer(tr.transform, 1f * W);
            var trNote = Text(tr.transform, "Note", Type, 1.15f, Cold, null, "", TextAlignmentOptions.TopLeft, 0, 1.5f);
            Hints(r, ("R", "UI_Common/inspect.translate"), ("Q", "UI_Common/inspect.flip"), ("⇅", "UI_Common/inspect.zoom"), ("Esc", "UI_Common/hint.close"));
            var v = View<EvidenceView>(r);
            Set(v, "_paper", holder); Set(v, "_front", front.gameObject); Set(v, "_back", back.gameObject); Set(v, "_transcript", tr.gameObject);
            Set(v, "_mast", mast); Set(v, "_dateLeft", dA); Set(v, "_dateMid", dB); Set(v, "_dateRight", dC); Set(v, "_headline", headline); Set(v, "_photo", photo); Set(v, "_original", colA); Set(v, "_backNote", backNote);
            Set(v, "_name", evName); Set(v, "_trTitle", trTitle); Set(v, "_trBody", trBody); Set(v, "_trNote", trNote);
            return Save(r.gameObject, Root + "/PF_UI_Evidence.prefab");
        }

        static GameObject Dead()
        {
            var r = Screen("PF_UI_Dead");
            var bg = Img(r, "Background", DeadBg, Color.white, Image.Type.Simple, true); Stretch(bg.rectTransform);
            var col = Node(r, "Content"); Place(col, new Vector2(0, .5f), new Vector2(9 * W, 0), new Vector2(60 * W, 10), new Vector2(0, .5f)); V(col, 0); Fit(col, false, true);
            var title = Text(col, "Title", Ui8, 4f, Bone, "System/dead.title", "", TextAlignmentOptions.TopLeft, .06f); Spacer(col, 1.6f * W);
            var hint = Text(col, "Hint", Kr4, 1.35f, Body, null, "", TextAlignmentOptions.TopLeft, 0, 1.7f); LE(hint, 48 * W); Spacer(col, .6f * W);
            var cp = Text(col, "Checkpoint", Ui4, 1f, Ash, null, "", TextAlignmentOptions.TopLeft, .2f, 0, true); Spacer(col, 3 * H);
            var buttons = Node(col, "Buttons"); V(buttons, 0);
            var retry = MenuBtn(buttons, "Continue", "UI_Common/dead.retry"); var main = MenuBtn(buttons, "Main menu", "UI_Common/dead.main");
            Hints(r, ("Enter", "UI_Common/hint.select"));
            var v = View<DeadView>(r, retry.Button); Set(v, "_title", title); Set(v, "_hint", hint); Set(v, "_checkpoint", cp); Set(v, "_retry", retry); Set(v, "_main", main);
            return Save(r.gameObject, Root + "/PF_UI_Dead.prefab");
        }

        static GameObject Complete()
        {
            var r = Screen("PF_UI_Complete");
            var bg = Img(r, "Background", CompleteBg, Color.white, Image.Type.Simple, true); Stretch(bg.rectTransform);
            var col = Column(r, "Content", 7, 12, 60);
            var label = Text(col, "Label", Ui6, 1f, Accent, null, "", TextAlignmentOptions.TopLeft, .34f); Spacer(col, .6f * W);
            var title = Text(col, "Title", Ui8, 5.4f, Bone, null, "", TextAlignmentOptions.TopLeft, .06f); Spacer(col, 1.4f * W);
            var body = Text(col, "Body", Kr4, 1.35f, Body, null, "", TextAlignmentOptions.TopLeft, 0, 1.7f); LE(body, 44 * W); Spacer(col, 3 * H);
            var stats = Node(col, "Stats"); Hz(stats, 4 * W, null, TextAnchor.UpperLeft);
            TextMeshProUGUI Stat(string key) { var s = Node(stats, key); V(s, 0); Label(s, "Label", key, .85f, .22f); var b = Text(s, "Value", Ui6, 2.2f, Bone, null, "0", TextAlignmentOptions.TopLeft, 0, 0, false, false); return b; }
            var clues = Stat("System/complete.clues"); var time = Stat("System/complete.time"); var deaths = Stat("System/complete.deaths");
            Spacer(col, 4 * H);
            var next = Text(col, "Next", Kr4, 1.05f, Ash, null, ""); Spacer(col, 1 * H);
            var buttons = Node(col, "Buttons"); V(buttons, 0);
            var cont = MenuBtn(buttons, "Continue", "UI_Common/complete.continue"); var main = MenuBtn(buttons, "Main menu", "UI_Common/dead.main");
            var v = View<CompleteView>(r, cont.Button);
            Set(v, "_label", label); Set(v, "_title", title); Set(v, "_body", body); Set(v, "_clues", clues); Set(v, "_time", time); Set(v, "_deaths", deaths); Set(v, "_next", next); Set(v, "_continue", cont); Set(v, "_main", main);
            return Save(r.gameObject, Root + "/PF_UI_Complete.prefab");
        }

        static GameObject Credits()
        {
            var r = Screen("PF_UI_Credits");
            var bg = Img(r, "Background", White, Over(0, 0, 0, .88f), Image.Type.Simple, true); Stretch(bg.rectTransform);
            var viewport = Node(r, "Viewport"); Stretch(viewport); viewport.gameObject.AddComponent<RectMask2D>();
            var roll = Node(viewport, "Roll"); Place(roll, new Vector2(.5f, 1), Vector2.zero, new Vector2(60 * W, 10), new Vector2(.5f, 1)); V(roll, 0, Pad(0, 0, 0, 10 * H), TextAnchor.UpperCenter); Fit(roll, false, true);
            var small = Text(roll, "Supernatural", Ui6, 1f, Ash, null, "SUPERNATURAL", TextAlignmentOptions.Top, .5f); Spacer(roll, 1.2f * W);
            Text(roll, "Logo", Ui8, 3.2f, Bone, null, "BEFORE JERICHO", TextAlignmentOptions.Top, .14f, .95f); Spacer(roll, 6 * H);
            void Credit(string role, string valueKey, string value)
            {
                Spacer(roll, 4 * H); Text(roll, role, Ui6, .95f, Dim, role, "", TextAlignmentOptions.Top, .3f); Spacer(roll, .6f * W);
                Text(roll, role + " value", Kr4, 1.3f, Bone, valueKey, value, TextAlignmentOptions.Top);
            }
            Credit("System/credits.design", null, "Before Jericho project");
            Credit("System/credits.audio", "System/credits.audio.v", "");
            Credit("System/credits.engine", null, "Unity 6 · HDRP");
            Spacer(roll, 8 * H); Text(roll, "Thanks", Kr7, 1.3f, Bone, "System/credits.thanks", "", TextAlignmentOptions.Top, .2f);
            Hints(r, ("Esc", "UI_Common/hint.back"));
            var v = View<CreditsView>(r); Set(v, "_roll", roll); Set(v, "_viewport", viewport); Set(v, "_logo", small);
            return Save(r.gameObject, Root + "/PF_UI_Credits.prefab");
        }

        static GameObject Pause()
        {
            var r = Screen("PF_UI_Pause"); DimBackground(r);
            var left = Column(r, "Menu", 7, 10, 34);
            var title = Text(left, "Title", Ui8, 1.2f, Ash, "UI_Common/pause.title", "", TextAlignmentOptions.TopLeft, .34f); Spacer(left, .4f * W);
            Text(left, "Flavor", Ui4, 2.6f, Bone, "UI_Common/pause.flavor", ""); Spacer(left, 3 * H);
            var resume = MenuBtn(left, "Resume", "UI_Common/pause.resume"); var journal = MenuBtn(left, "Journal", "UI_Common/pause.journal"); var map = MenuBtn(left, "Map", "UI_Common/pause.map");
            var options = MenuBtn(left, "Options", "UI_Common/pause.options"); var retry = MenuBtn(left, "Retry", "UI_Common/pause.retry"); var main = MenuBtn(left, "Main menu", "UI_Common/pause.main");
            var side = Node(r, "Side"); TopLeft(side, 45, 10, 40 * W, 10); V(side, 2.2f * H, Pad(3 * W, 0, 0, 0)); Fit(side, false, true);
            var rule = Img(side, "Rule", White, Line); LE(rule, -1, -1, -1, -1, true); rule.rectTransform.anchorMin = Vector2.zero; rule.rectTransform.anchorMax = new Vector2(0, 1); rule.rectTransform.pivot = new Vector2(0, .5f); rule.rectTransform.sizeDelta = new Vector2(1, 0);
            TextMeshProUGUI Section(string key, TMP_FontAsset font) { var s = Node(side, key); V(s, .3f * W); Label(s, "Label", key); return Text(s, "Value", font, 1.35f, Bone, null, "", TextAlignmentOptions.TopLeft, 0, 1.5f); }
            var objective = Section("Journal/case.current", Kr4); var checkpoint = Section("UI_Common/pause.checkpoint", Kr4); var playTime = Section("UI_Common/pause.playtime", Ui4);
            // ★ prefix (HTML .obj::before)
            var star = Img(objective.transform, "Star", Star, Accent); Place(star.rectTransform, new Vector2(0, 1), new Vector2(-1.5f * W, -.45f * W), new Vector2(.9f * W, .9f * W), new Vector2(0, 1));
            Hints(r, ("Enter", "UI_Common/hint.select"), ("Esc", "UI_Common/hint.back"));
            var v = View<PauseView>(r, resume.Button);
            Set(v, "_resume", resume); Set(v, "_journal", journal); Set(v, "_map", map); Set(v, "_options", options); Set(v, "_retry", retry); Set(v, "_main", main);
            Set(v, "_objective", objective); Set(v, "_checkpoint", checkpoint); Set(v, "_playTime", playTime); Set(v, "_title", title);
            return Save(r.gameObject, Root + "/PF_UI_Pause.prefab");
        }

        static GameObject Journal()
        {
            var r = Screen("PF_UI_Journal"); DimBackground(r);
            var frame = Node(r, "Journal"); Stretch(frame, 4 * W, 4 * H, 4 * W, 9 * H);
            var head = Node(frame, "Head"); Place(head, new Vector2(.5f, 1), Vector2.zero, new Vector2(0, 3.2f * W), new Vector2(.5f, 1)); head.anchorMin = new Vector2(0, 1); head.anchorMax = Vector2.one;
            var title = Text(head, "Title", Ui8, 1.4f, Bone, "Journal/title", "", TextAlignmentOptions.BottomLeft, .3f); Stretch(title.rectTransform, 0, 0, 0, 1f * W);
            var tabs = Node(head, "Tabs"); Place(tabs, new Vector2(1, 0), Vector2.zero, new Vector2(10, 10), new Vector2(1, 0)); Hz(tabs, 2.2f * W, null, TextAnchor.LowerRight); Fit(tabs);
            Text(tabs, "Q", MonoFont, .8f, Dim, null, "Q", TextAlignmentOptions.Center, 0, 0, false, false);
            string[] ids = { "case", "clues", "people", "files", "items", "map" };
            var tabButtons = new Button[6]; var lines = new Graphic[6]; var labels = new TMP_Text[6];
            for(int i = 0; i < 6; i++)
            {
                var t = Instance<RectTransform>(_tab, tabs, "Tab " + ids[i]); Set(t.Find("Label").GetComponent<LocText>(), "_key", "Journal/tab." + ids[i]);
                tabButtons[i] = t.GetComponent<Button>(); lines[i] = t.Find("Underline").GetComponent<Image>(); labels[i] = t.Find("Label").GetComponent<TMP_Text>();
            }
            Text(tabs, "E", MonoFont, .8f, Dim, null, "E", TextAlignmentOptions.Center, 0, 0, false, false);
            var rule = Img(frame, "Rule", White, Line); Place(rule.rectTransform, new Vector2(.5f, 1), new Vector2(0, -3.2f * W), new Vector2(0, 1), new Vector2(.5f, 1)); rule.rectTransform.anchorMin = new Vector2(0, 1); rule.rectTransform.anchorMax = Vector2.one;
            var body = Node(frame, "Body"); Stretch(body, 0, 3.2f * W + 2.4f * W, 0, 0);
            float leftW = (1472 - 48) * 1.1f / 2.1f, rightX = leftW + 48;
            RectTransform Left(RectTransform panel) { var l = Node(panel, "Left"); Place(l, new Vector2(0, 1), Vector2.zero, new Vector2(leftW, 10)); V(l, 0); Fit(l, false, true); return l; }
            RectTransform Right(RectTransform panel) { var rr = Node(panel, "Right"); Place(rr, new Vector2(0, 1), new Vector2(rightX, 0), new Vector2(1472 - rightX, 10)); V(rr, 0); Fit(rr, false, true); return rr; }
            RectTransform Panel(string n) { var p = Node(body, n); Stretch(p); return p; }
            // case
            var casePanel = Panel("Case"); var cl = Left(casePanel);
            var caseTitle = Label(cl, "Case title", null); Spacer(cl, 1.4f * W);
            TextMeshProUGUI Field(string key, bool big)
            {
                Label(cl, key, key); Spacer(cl, .4f * W);
                var t = Text(cl, key + " value", big ? Kr5 : Kr4, big ? 1.8f : 1.25f, big ? Bone : UIStyle.Hex(0xd6d1c3), null, "", TextAlignmentOptions.TopLeft, 0, big ? 1.35f : 1.6f);
                Spacer(cl, 1.6f * W); return t;
            }
            var objective = Field("Journal/case.current", true); var location = Field("Journal/case.location", false); var rules = Field("Journal/case.rules", false); var john = Field("Journal/case.john", false);
            var star = Img(objective.transform, "Star", Star, Accent); Place(star.rectTransform, new Vector2(0, 1), new Vector2(-1.6f * W, -.55f * W), new Vector2(1.1f * W, 1.1f * W), new Vector2(0, 1));
            var (cn, cnTitle, cnBody, cnDed) = NotePaper(Right(casePanel), "Dean's notes"); Set(cnTitle.gameObject.AddComponent<LocText>(), "_key", "Journal/note.title");
            // clues
            var cluesPanel = Panel("Clues"); var ql = Left(cluesPanel);
            var count = Label(ql, "Count", null); Spacer(ql, 1f * W);
            var list = Node(ql, "List"); V(list, .2f * W);
            var clueTemplate = Instance<JournalEntry>(_entry, list, "Entry");
            var (qn, qnTitle, qnBody, qnDed) = NotePaper(Right(cluesPanel), "Dean's notes"); Set(qnTitle.gameObject.AddComponent<LocText>(), "_key", "Journal/note.title");
            // people
            var peoplePanel = Panel("People"); var pl = Left(peoplePanel); var plist = Node(pl, "List"); V(plist, .2f * W);
            var personTemplate = Instance<JournalEntry>(_entry, plist, "Entry");
            var (pn, pnTitle, pnBody, pnDed) = NotePaper(Right(peoplePanel), "Quote", 1.15f); pnDed.transform.parent.Find("Dashed").gameObject.SetActive(false);
            // files
            var filesPanel = Panel("Files"); var fl = Left(filesPanel); var flist = Node(fl, "List"); V(flist, .2f * W);
            var fileTemplate = Instance<JournalEntry>(_entry, flist, "Entry");
            var (fn, fnTitle, fnBody, fnDed) = NotePaper(Right(filesPanel), "Transcript", 1.15f, true); fnDed.transform.parent.Find("Dashed").gameObject.SetActive(false);
            // items
            var itemsPanel = Panel("Items"); var il = Node(itemsPanel, "Grid"); Place(il, new Vector2(0, 1), Vector2.zero, new Vector2(leftW, 400));
            var grid = il.gameObject.AddComponent<GridLayoutGroup>(); float cell = (leftW - 3 * .8f * W) / 4; grid.cellSize = new Vector2(cell, cell); grid.spacing = new Vector2(.8f * W, .8f * W); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 4;
            var itemTemplate = Instance<JournalEntry>(_slot, il, "Slot");
            var ir = Right(itemsPanel); var itemName = Text(ir, "Name", Kr5, 1.8f, Bone, null, ""); Spacer(ir, .6f * W); var itemDesc = Text(ir, "Desc", Kr4, 1.2f, Body, null, "", TextAlignmentOptions.TopLeft, 0, 1.6f);
            // map
            var mapPanel = Panel("Map"); float legendW = 17 * W, mapW = 1472 - legendW - 2 * W;
            var mapArea = Node(mapPanel, "Plan area"); Place(mapArea, new Vector2(0, 1), new Vector2(0, -1.6f * W), new Vector2(mapW, 560));
            var mapTitle = Text(mapArea, "Title", Ui8, 22 * 1.3f / W, UIStyle.Hex(0xe6e1d3), null, "", TextAlignmentOptions.TopLeft, .18f, 0, false, false); TopLeft(mapTitle.rectTransform, 0, 0, mapW, 40);
            var mapSub = Text(mapArea, "Sub", Ui4, 14 * 1.3f / W, UIStyle.Hex(0x5f5d57), null, "", TextAlignmentOptions.TopLeft, .21f, 0, false, false); TopLeft(mapSub.rectTransform, 0, 4.3f, mapW, 30);
            // Wide-tracked caps: kerning pairs like "T␠" shrink word gaps to letter gaps ("SAINTMERCY").
            mapTitle.fontFeatures.Remove(UnityEngine.TextCore.OTL_FeatureTag.kern); mapSub.fontFeatures.Remove(UnityEngine.TextCore.OTL_FeatureTag.kern);
            var plan = Node(mapArea, "Plan").gameObject.AddComponent<RawImage>(); plan.raycastTarget = false; Place(plan.rectTransform, new Vector2(.5f, 0), Vector2.zero, new Vector2(480, 480), new Vector2(.5f, 0));
            var markers = Node(plan.transform, "Markers"); Stretch(markers);
            Image Marker(string n, Sprite s, Color c, float size) { var m = Img(markers, n, s, c); m.rectTransform.sizeDelta = new Vector2(size, size); m.gameObject.SetActive(false); return m; }
            var mHere = Marker("Here", Tri, Color.white, 22); var mObj = Marker("Objective", Star, Accent, 26); var mClue = Marker("Clue", Dot, UIStyle.Hex(0xe6e1d3), 14); var mDanger = Marker("Danger", Danger, Color.white, 30);
            var legend = Node(mapPanel, "Legend"); Place(legend, new Vector2(1, 1), new Vector2(0, -1.6f * W), new Vector2(legendW, 10), new Vector2(1, 1)); V(legend, .9f * W); Fit(legend, false, true);
            Label(legend, "Legend", "UI_Common/map.legend", .9f, .22f);
            void LegendRow(Sprite s, Color c, string key) { var row = Node(legend, key); Hz(row, .8f * W); var ic = Img(row, "Icon", s, c); LE(ic, 1.6f * W, 1.6f * W); Text(row, "Label", Kr4, 1.05f, Ash, key, "", TextAlignmentOptions.MidlineLeft, 0, 0, false, false); }
            LegendRow(Tri, Color.white, "UI_Common/map.here"); LegendRow(Star, Accent, "UI_Common/map.objective"); LegendRow(Dot, UIStyle.Hex(0xe6e1d3), "UI_Common/map.clue"); LegendRow(Danger, Color.white, "UI_Common/map.danger");
            Hints(r, ("Q/E", "UI_Common/hint.tab"), ("Tab", "UI_Common/hint.close"));
            var v = View<JournalView>(r);
            Set(v, "_tabs", tabButtons); Set(v, "_tabUnderlines", lines); Set(v, "_tabLabels", labels);
            Set(v, "_casePanel", casePanel.gameObject); Set(v, "_cluesPanel", cluesPanel.gameObject); Set(v, "_peoplePanel", peoplePanel.gameObject); Set(v, "_filesPanel", filesPanel.gameObject); Set(v, "_itemsPanel", itemsPanel.gameObject); Set(v, "_mapPanel", mapPanel.gameObject);
            Set(v, "_caseTitle", caseTitle); Set(v, "_objective", objective); Set(v, "_location", location); Set(v, "_rules", rules); Set(v, "_john", john); Set(v, "_noteTheory", cnBody); Set(v, "_noteBody", cnDed);
            Set(v, "_clueCount", count); Set(v, "_clueTemplate", clueTemplate); Set(v, "_personTemplate", personTemplate); Set(v, "_fileTemplate", fileTemplate); Set(v, "_itemTemplate", itemTemplate);
            Set(v, "_clueNote", qnBody); Set(v, "_clueDeduction", qnDed); Set(v, "_personNoteTitle", pnTitle); Set(v, "_personNote", pnBody); Set(v, "_fileNoteTitle", fnTitle); Set(v, "_fileNote", fnBody); Set(v, "_itemName", itemName); Set(v, "_itemDesc", itemDesc);
            Set(v, "_plan", plan); Set(v, "_markers", markers); Set(v, "_markerHere", mHere); Set(v, "_markerObjective", mObj); Set(v, "_markerClue", mClue); Set(v, "_markerDanger", mDanger); Set(v, "_mapTitle", mapTitle); Set(v, "_mapSub", mapSub);
            return Save(r.gameObject, Root + "/PF_UI_Journal.prefab");
        }

        static GameObject Settings()
        {
            var r = Screen("PF_UI_Settings"); DimBackground(r, .95f);
            var frame = Node(r, "Options"); Stretch(frame, 4 * W, 4 * H, 4 * W, 9 * H);
            var title = Text(frame, "Title", Ui8, 1.4f, Bone, "Settings/title", "", TextAlignmentOptions.BottomLeft, .3f); Place(title.rectTransform, new Vector2(0, 1), Vector2.zero, new Vector2(40 * W, 2.4f * W));
            var rule = Img(frame, "Rule", White, Line); Place(rule.rectTransform, new Vector2(.5f, 1), new Vector2(0, -3.4f * W), new Vector2(0, 1), new Vector2(.5f, 1)); rule.rectTransform.anchorMin = new Vector2(0, 1); rule.rectTransform.anchorMax = Vector2.one;
            float top = 3.4f * W + 2 * W, tabsW = 15 * W, descW = 24 * W, gap = 2.5f * W, optsW = 1472 - tabsW - descW - 2 * gap;
            var tabs = Node(frame, "Tabs"); Place(tabs, new Vector2(0, 1), new Vector2(0, -top), new Vector2(tabsW, 10)); V(tabs, .2f * W); Fit(tabs, false, true);
            string[] ids = { "gameplay", "controls", "camera", "display", "audio", "lang", "access" };
            var tabButtons = new Button[7]; var marks = new Graphic[7]; var labels = new TMP_Text[7];
            for(int i = 0; i < 7; i++)
            {
                var t = Instance<RectTransform>(_sideTab, tabs, "Tab " + ids[i]); Set(t.Find("Label").GetComponent<LocText>(), "_key", "Settings/tab." + ids[i]);
                tabButtons[i] = t.GetComponent<Button>(); marks[i] = t.Find("Selected").GetComponent<Image>(); labels[i] = t.Find("Label").GetComponent<TMP_Text>();
            }
            var opts = Node(frame, "Rows"); Place(opts, new Vector2(0, 1), new Vector2(tabsW + gap, -top), new Vector2(optsW, 10)); V(opts, .2f * W); Fit(opts, false, true);
            var template = Instance<OptionRow>(_optionRow, opts, "Row");
            var desc = Node(frame, "Description"); Place(desc, new Vector2(0, 1), new Vector2(tabsW + gap + optsW + gap, -top), new Vector2(descW, 10)); V(desc, 1.5f * W, Pad(2 * W, 0, 0, 0)); Fit(desc, false, true);
            var dRule = Img(desc, "Rule", White, Line); LE(dRule, -1, -1, -1, -1, true); dRule.rectTransform.anchorMin = Vector2.zero; dRule.rectTransform.anchorMax = new Vector2(0, 1); dRule.rectTransform.pivot = new Vector2(0, .5f); dRule.rectTransform.sizeDelta = new Vector2(1, 0);
            var dTitle = Label(desc, "Title", null, .9f, .22f); dTitle.fontStyle |= FontStyles.UpperCase;
            var dBody = Text(desc, "Body", Kr4, 1.15f, Body, null, "", TextAlignmentOptions.TopLeft, 0, 1.6f);
            var pv = Img(desc, "Subtitle preview", PreviewBg, Color.white); LE(pv, -1, 16 * H); Fill(pv); Stretch(Img(pv.transform, "Border", Frame, Line, Image.Type.Sliced).rectTransform);
            var sub = Instance<SubtitleView>(_subtitles, pv.transform, "Preview subtitles"); Place((RectTransform)sub.transform, new Vector2(.5f, 0), new Vector2(0, 1 * H), new Vector2(descW - 2 * W - 20, 14 * H), new Vector2(.5f, 0));
            Set(sub, "_preview", true); Set(sub, "_maxWidth", descW - 2 * W - 50);
            var brt = Img(desc, "Brightness", White, Color.black); LE(brt, -1, 12 * H); Fill(brt); Hz(brt, 1 * W, null, TextAnchor.MiddleCenter);
            var sw = new Image[3]; for(int i = 0; i < 3; i++) { sw[i] = Img(brt.transform, "Swatch " + i, White, Color.gray); LE(sw[i], 5 * W, 5 * W); }
            Hints(r, ("↑↓", "UI_Common/hint.select"), ("Esc", "UI_Common/hint.back"));
            var v = View<SettingsView>(r);
            Set(v, "_tabs", tabButtons); Set(v, "_tabMarks", marks); Set(v, "_tabLabels", labels); Set(v, "_template", template); Set(v, "_descTitle", dTitle); Set(v, "_descBody", dBody);
            Set(v, "_subtitlePreview", pv.gameObject); Set(v, "_brightnessPreview", brt.gameObject); Set(v, "_brightnessSwatches", sw); Set(v, "_preview", sub);
            return Save(r.gameObject, Root + "/PF_UI_Settings.prefab");
        }

        static GameObject Dev()
        {
            var r = Screen("PF_UI_DevOverlay");
            var hint = Text(r, "Hint", MonoFont, .75f, Over(127 / 255f, 208 / 255f, 192 / 255f, .6f), null, "DEV · F1 UI 영역 가이드 · F2 String Key 보기 · F3 UX 메모 · 문자열", TextAlignmentOptions.TopLeft, 0, 0, false, false);
            TopLeft(hint.rectTransform, .8f, 1, 60 * W, 20);
            var guides = Node(r, "Guides"); Stretch(guides);
            var g = Img(guides, "Guide", GuideFrame, Color.white, Image.Type.Sliced); Stretch(g.rectTransform);
            var gl = Img(g.transform, "Label", White, UIStyle.Hex(0xd99a4a)); Place(gl.rectTransform, new Vector2(0, 1), Vector2.zero, new Vector2(10, 10), new Vector2(0, 0)); Hz(gl, 0, Pad(.4f * W, .4f * W, .1f * W, .1f * W)); Fit(gl);
            Text(gl.transform, "Text", Kr5, .85f, UIStyle.Hex(0x111111), null, "zone", TextAlignmentOptions.MidlineLeft, 0, 0, false, false);
            var panel = Img(r, "Memo panel", White, Over(11 / 255f, 12 / 255f, 13 / 255f, .94f)); var pr = panel.rectTransform; pr.anchorMin = new Vector2(1, 0); pr.anchorMax = Vector2.one; pr.pivot = new Vector2(1, .5f); pr.sizeDelta = new Vector2(36 * W, 0); pr.anchoredPosition = Vector2.zero;
            panel.gameObject.AddComponent<RectMask2D>(); V(panel, 1 * W, Pad(1.4f * W, 1.4f * W, 1.4f * W, 1.4f * W));
            Text(panel.transform, "Heading", Ui6, .8f, Dim, null, "이 화면 · UX 메모", TextAlignmentOptions.TopLeft, .16f);
            var memo = Text(panel.transform, "Memo", Kr4, .95f, Body, null, "", TextAlignmentOptions.TopLeft, 0, 1.55f);
            var strings = Text(panel.transform, "Strings", Kr4, .75f, Ash, null, "", TextAlignmentOptions.TopLeft, 0, 1.4f);
            var dev = r.gameObject.AddComponent<DevOverlay>(); Set(dev, "_guides", guides); Set(dev, "_guideTemplate", g.rectTransform); Set(dev, "_panel", panel.gameObject); Set(dev, "_memo", memo); Set(dev, "_strings", strings); Set(dev, "_hint", hint);
            return Save(r.gameObject, Root + "/PF_UI_DevOverlay.prefab");
        }

        static void RootPrefab(GameObject[] screens)
        {
            var r = NewRoot("PF_HuntUI");
            var canvas = r.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var scaler = r.gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            r.gameObject.AddComponent<GraphicRaycaster>();
            var views = new Component[screens.Length];
            for(int i = 0; i < screens.Length; i++) { var inst = (GameObject)PrefabUtility.InstantiatePrefab(screens[i], r); views[i] = inst.GetComponent<UIView>() ?? (Component)inst.GetComponent<DevOverlay>(); inst.SetActive(inst.GetComponent<DevOverlay>() != null); }
            var hud = r.gameObject.AddComponent<HuntHUD>();
            Set(hud, "_stringTables", new Object[]{ AssetDatabase.LoadAssetAtPath<TextAsset>(Tables + "/StringTable_UI.txt"), AssetDatabase.LoadAssetAtPath<TextAsset>(Tables + "/StringTable_Game.txt") });
            string[] fields = { "_hud", "_card", "_pause", "_journal", "_evidence", "_dead", "_complete", "_credits", "_menu", "_settings", "_boot", "_dev" };
            for(int i = 0; i < fields.Length; i++) Set(hud, fields[i], views[i]);
            r.gameObject.AddComponent<SettingsApplier>();
            Save(r.gameObject, Root + "/PF_HuntUI.prefab");
        }

    }
}
