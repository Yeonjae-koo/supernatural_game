using System;
using System.Collections.Generic;
using System.IO;
using BeforeJericho;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace BeforeJericho.Editor
{
    /// <summary>One-shot CH06 Stanford epilogue greybox builder. It creates a new scene and never replaces an authored one.</summary>
    public static class StanfordBuilder
    {
        const string Root = "Assets/_Game";
        const string ScenePath = Root + "/Scenes/SCN_Stanford_Slice.unity";
        static readonly List<HuntInteractable> Items = new();
        static TMP_FontAsset Font;
        static HuntSession Session;
        static Material Stucco, Asphalt, Grass, Wood, Steel, Paper, Gold, Neon, Window, Brick;

        [MenuItem("Before Jericho/Create Stanford Epilogue Slice")]
        public static void Create()
        {
            if(File.Exists(ScenePath)) throw new InvalidOperationException("CH06 scene already exists. Keep the authored scene, or remove its generated assets before rebuilding.");
            if(EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the active scene before generating CH06.");
            foreach(string folder in new[] { "Scenes", "Data/Stanford", "Art/Materials/Stanford" }) Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if(!Font) throw new InvalidOperationException("TMP Essential Resources are required.");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Items.Clear();
            Materials();
            MotelRoom();
            var door = PaloAlto(out var samsDoor);
            Session = new GameObject("Hunt Session").AddComponent<HuntSession>();
            Session.definition = NewDefinition();
            Session.sound = Session.gameObject.AddComponent<HuntAudio>();
            Player();
            var chapter = Session.gameObject.AddComponent<StanfordChapter>();
            chapter.Configure(Session, Point("Motel room arrival", new Vector3(0, .25f, 4)), Point("Palo Alto arrival", new Vector3(-2, .25f, 34)), door, samsDoor);
            Session.items = Items.ToArray();
            Hud();
            Atmosphere();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            var build = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if(!build.Exists(scene => scene.path == ScenePath)) build.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = build.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("BEFORE JERICHO: Created CH06 Stanford epilogue: motel room, Palo Alto street, apartment hallway and Sam's door.");
        }

        static HuntDefinition NewDefinition()
        {
            var definition = ScriptableObject.CreateInstance<HuntDefinition>();
            definition.name = "SO_StanfordEpilogue";
            AssetDatabase.CreateAsset(definition, Root + "/Data/Stanford/SO_StanfordEpilogue.asset");
            return definition;
        }

        static void Materials()
        {
            Stucco = Mat("CH06 Motel stucco", new Color(.4f, .3f, .22f), .05f);
            Asphalt = Mat("CH06 Street asphalt", new Color(.09f, .09f, .1f), .2f);
            Grass = Mat("CH06 Lawn", new Color(.1f, .18f, .09f));
            Wood = Mat("CH06 Wood", new Color(.25f, .16f, .09f), .2f);
            Steel = Mat("CH06 Steel", new Color(.2f, .22f, .23f), .5f);
            Paper = Mat("CH06 Paper", new Color(.62f, .56f, .4f));
            Gold = Mat("CH06 Gold marker", new Color(.78f, .48f, .08f), .25f, new Color(.16f, .07f, .005f));
            Neon = Mat("CH06 Accent", new Color(.85f, .55f, .25f), .3f, new Color(.4f, .25f, .08f));
            Window = Mat("CH06 Warm window", new Color(.95f, .75f, .4f), .3f, new Color(1.2f, .8f, .35f));
            Brick = Mat("CH06 Apartment brick", new Color(.42f, .26f, .2f), .05f);
        }

        // Motel room: four pieces of John's trail, the phone and the door out.
        static void MotelRoom()
        {
            Box("Motel floor", new Vector3(0, .1f, 5), new Vector3(10, .18f, 10), Wood);
            Boundary("Motel west", new Vector3(-5, 1.5f, 5), new Vector3(.35f, 3, 10));
            Boundary("Motel east", new Vector3(5, 1.5f, 5), new Vector3(.35f, 3, 10));
            Boundary("Motel south", new Vector3(0, 1.5f, 0), new Vector3(10, 3, .35f));
            Boundary("Motel north", new Vector3(0, 1.5f, 10), new Vector3(10, 3, .35f));
            Box("Motel bed", new Vector3(3, .35f, 5.5f), new Vector3(2.2f, .5f, 3), Wood);
            Box("Desk", new Vector3(-1, .5f, 8.8f), new Vector3(4.4f, .8f, 1), Wood);
            Interactable(HuntItem.JohnNotes, "Read John's notes", "JOHN'S NOTES: Dates, highway mile markers, and one name underlined three times: JERICHO.", new Vector3(-2.5f, 1.05f, 8.8f), Paper);
            Interactable(HuntItem.JerichoClippings, "Study the clippings", "CLIPPINGS: Men vanishing on the same stretch outside Jericho, California. One every few months, going back years.", new Vector3(-1, 1.05f, 8.8f), Paper);
            Interactable(HuntItem.RouteMap, "Trace John's map", "JOHN'S ROUTE: New Orleans, Belle Rive, Mercy Creek, Grants, Winslow. Every line points west to Jericho.", new Vector3(.5f, 1.05f, 8.8f), Paper);
            Box("Nightstand", new Vector3(-3.8f, .35f, 3), new Vector3(.8f, .5f, .8f), Wood);
            Interactable(HuntItem.VoicemailTape, "Replay John's voicemail", "VOICEMAIL: Dean plays it again. \"...don't follow me...\" And underneath, the whisper: \"...Jericho...\"", new Vector3(-3.8f, .75f, 3), Gold);
            Box("Room phone table", new Vector3(3.8f, .35f, 1.5f), new Vector3(.8f, .5f, .8f), Wood);
            Interactable(HuntItem.CallJohn, "Call John", "The motel phone.", new Vector3(3.8f, .75f, 1.5f), Gold);
            Interactable(HuntItem.HitTheRoad, "Hit the road", "Palo Alto.", new Vector3(0, 1.1f, .5f), Gold);
            Sign("ROOM 7 / WINSLOW", new Vector3(0, 2.5f, 9.7f), 1.2f, Paper);
        }

        // Palo Alto: a tidy street, the Impala trunk beat, one apartment hallway and Sam's door.
        static GameObject PaloAlto(out Collider samsDoor)
        {
            Box("Palo Alto lawn", new Vector3(0, -.3f, 58), new Vector3(40, .55f, 60), Grass);
            Box("University Ave", new Vector3(-2, .01f, 52), new Vector3(8, .12f, 44), Asphalt);
            Box("Sidewalk", new Vector3(3, .05f, 52), new Vector3(2, .1f, 44), Steel);
            Boundary("Street west", new Vector3(-20, 2, 58), new Vector3(.4f, 4, 60));
            Boundary("Street east", new Vector3(20, 2, 58), new Vector3(.4f, 4, 60));
            Boundary("Street south", new Vector3(0, 2, 28), new Vector3(40, 4, .4f));
            Boundary("Street north", new Vector3(0, 2, 88), new Vector3(40, 4, .4f));
            Sign("PALO ALTO", new Vector3(-2, 3, 31), 2, Neon);
            for(int z = 36; z <= 72; z += 9)
            {
                Box("Street tree", new Vector3(-7.5f, 1.5f, z), new Vector3(.3f, 3, .3f), Wood);
                Box("Tree canopy", new Vector3(-7.5f, 3.4f, z), new Vector3(2.2f, 1.6f, 2.2f), Grass);
                Light("Street lamp", new Vector3(1.6f, 3.5f, z + 4), new Color(1, .82f, .55f), 1.6f, 10);
            }
            for(int i = 0; i < 4; i++) { Box("House across the street", new Vector3(-13, 2, 38 + i * 11), new Vector3(8, 4, 8), Stucco); Box("Warm window", new Vector3(-8.95f, 2, 38 + i * 11), new Vector3(.1f, 1.2f, 1.6f), Window); }
            var car = Car("1967 Impala - Palo Alto", new Vector3(-4.5f, .58f, 40), HuntItem.StanfordTrunk, "Open the trunk", "The trunk.");
            car.transform.Find("Interact - StanfordTrunk").position = new Vector3(-4.5f, .95f, 37.2f);
            // Apartment: west door onto the sidewalk, one hallway, Sam's door at the north end.
            Box("Apartment floor", new Vector3(8, .1f, 62), new Vector3(8, .18f, 24), Wood);
            Boundary("Apartment south", new Vector3(8, 2, 50), new Vector3(8, 4, .35f));
            Boundary("Apartment north", new Vector3(8, 2, 74), new Vector3(8, 4, .35f));
            Boundary("Apartment east", new Vector3(12, 2, 62), new Vector3(.35f, 4, 24));
            Boundary("Apartment west south", new Vector3(4, 2, 52.75f), new Vector3(.35f, 4, 5.5f));
            Boundary("Apartment west north", new Vector3(4, 2, 66.25f), new Vector3(.35f, 4, 15.5f));
            var door = Box("Apartment entrance door - opens after the trunk", new Vector3(4, 1.5f, 57), new Vector3(.3f, 3, 3), Wood);
            Sign("RIVERSIDE APARTMENTS", new Vector3(3.7f, 3.5f, 57), 1.4f, Neon, 90);
            for(int z = 54; z <= 62; z += 1) Box("Stairwell step", new Vector3(10.8f, .15f + (z - 54) * .1f, z), new Vector3(1.6f, .2f + (z - 54) * .2f, .9f), Wood).GetComponent<Collider>().enabled = false;
            for(int z = 60; z <= 70; z += 5) { Box("Apartment door", new Vector3(11.8f, 1.2f, z), new Vector3(.1f, 2.4f, 1.1f), Wood); Box("Door light", new Vector3(11.75f, 2.6f, z), new Vector3(.05f, .15f, .4f), Window); }
            Npc("Neighbor", HuntItem.SamNeighbor, "Talk to the neighbor", "Neighbor (with a laundry basket): Looking for Sam? End of the hall. He's got finals, so... good luck.", new Vector3(5.2f, .9f, 64));
            Box("Sam's door", new Vector3(8, 1.2f, 73.75f), new Vector3(1.2f, 2.4f, .12f), Wood);
            Sign("SAM", new Vector3(8, 2.7f, 73.6f), 1, Paper);
            Light("Hallway light", new Vector3(8, 3.4f, 62), new Color(1, .85f, .6f), 1.8f, 14);
            samsDoor = Zone("Sam's door zone", new Vector3(8, 1.5f, 72), new Vector3(4, 3, 3));
            return door;
        }

        static GameObject Car(string name, Vector3 position, HuntItem kind, string label, string description)
        {
            var car = new GameObject(name);
            Part(car.transform, "Body", new Vector3(0, .55f, 0), new Vector3(2.05f, .65f, 4.8f), Steel);
            Part(car.transform, "Cabin", new Vector3(0, 1.16f, -.2f), new Vector3(1.7f, .62f, 2.25f), Wood);
            car.transform.position = position;
            var root = Box("Interact - " + kind, position + new Vector3(1.4f, .4f, 1), new Vector3(.58f, .28f, .46f), Gold);
            root.transform.SetParent(car.transform, true);
            var item = root.AddComponent<HuntInteractable>(); item.kind = kind; item.label = label; item.description = description; Marker(root, item);
            return car;
        }

        static void Player()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Actors/PF_Dean.prefab");
            if(!prefab) throw new InvalidOperationException("PF_Dean.prefab is required; create the Prologue slice first.");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.name = "Dean Winchester"; player.transform.position = new Vector3(-3, .1f, 4); player.layer = 2;
            var motor = player.GetComponent<HunterMotor>(); var combat = player.GetComponent<HunterCombat>();
            motor.session = Session; combat.session = Session; combat.motor = motor; Session.player = motor;
            var camera = new GameObject("OTS Camera"); camera.tag = "MainCamera";
            var lens = camera.AddComponent<Camera>(); camera.AddComponent<AudioListener>(); lens.nearClipPlane = .05f; lens.farClipPlane = 170; lens.clearFlags = CameraClearFlags.SolidColor; lens.backgroundColor = new Color(.03f, .03f, .05f);
            camera.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            var shoulder = camera.AddComponent<ShoulderCamera>(); shoulder.player = motor; shoulder.session = Session; shoulder.lens = lens;
            motor.view = shoulder; combat.view = shoulder;
        }

        static void Hud()
        {
            var root = new GameObject("Hunt HUD", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            root.AddComponent<GraphicRaycaster>();
            var eventSystem = new GameObject("EventSystem"); eventSystem.AddComponent<EventSystem>(); eventSystem.AddComponent<InputSystemUIInputModule>();
            var hud = root.AddComponent<HuntHUD>(); hud.session = Session; hud.combat = Session.player.GetComponent<HunterCombat>(); Session.hud = hud;
            Panel(root.transform, "Objective shade", new Vector2(.025f, .83f), new Vector2(.5f, .97f), new Color(.02f, .015f, .02f, .86f));
            Text(root.transform, "Chapter", "06 / STANFORD", new Vector2(.04f, .93f), new Vector2(.48f, .96f), 17);
            hud.objective = Text(root.transform, "Objective", "", new Vector2(.04f, .845f), new Vector2(.48f, .915f), 23);
            hud.stats = Text(root.transform, "Equipment", "", new Vector2(.73f, .055f), new Vector2(.97f, .145f), 22, TextAlignmentOptions.BottomRight);
            hud.prompt = Text(root.transform, "Interaction", "", new Vector2(.2f, .16f), new Vector2(.8f, .21f), 23, TextAlignmentOptions.Center);
            hud.message = Text(root.transform, "Subtitle", "", new Vector2(.15f, .23f), new Vector2(.85f, .32f), 23, TextAlignmentOptions.Center);
            Text(root.transform, "Crosshair", "·", new Vector2(.49f, .485f), new Vector2(.51f, .515f), 30, TextAlignmentOptions.Center);
            Text(root.transform, "Controls", "WASD MOVE   SHIFT SPRINT   E INSPECT   F LIGHTER   TAB JOURNAL", new Vector2(.04f, .02f), new Vector2(.75f, .055f), 16);
            Panel(root.transform, "Health back", new Vector2(.04f, .09f), new Vector2(.19f, .102f), new Color(.15f, .08f, .07f));
            hud.healthFill = Panel(root.transform, "Health", new Vector2(.04f, .09f), new Vector2(.19f, .102f), new Color(.66f, .31f, .23f)); hud.healthFill.type = Image.Type.Filled; hud.healthFill.fillMethod = Image.FillMethod.Horizontal;
            hud.burnFill = Panel(root.transform, "Case progress", new Vector2(.35f, .14f), new Vector2(.65f, .15f), Gold.color); hud.burnFill.type = Image.Type.Filled; hud.burnFill.fillMethod = Image.FillMethod.Horizontal;
            var overlay = Panel(root.transform, "Menu overlay", Vector2.zero, Vector2.one, new Color(.015f, .012f, .02f, .97f)); overlay.raycastTarget = true; hud.overlay = overlay.gameObject;
            Panel(overlay.transform, "Neon accent", new Vector2(.095f, .18f), new Vector2(.099f, .88f), Neon.color);
            hud.subtitle = Text(overlay.transform, "Kicker", "", new Vector2(.13f, .87f), new Vector2(.9f, .92f), 20);
            hud.overlayTitle = Text(overlay.transform, "Title", "", new Vector2(.13f, .65f), new Vector2(.93f, .85f), 55);
            hud.overlayBody = Text(overlay.transform, "Body", "", new Vector2(.13f, .19f), new Vector2(.9f, .64f), 23); hud.overlayBody.enableAutoSizing = true; hud.overlayBody.fontSizeMin = 17; hud.overlayBody.fontSizeMax = 23;
            hud.primary = Button(overlay.transform, "Primary", .13f, out hud.primaryLabel); hud.secondary = Button(overlay.transform, "Secondary", .395f, out hud.secondaryLabel); hud.tertiary = Button(overlay.transform, "Tertiary", .66f, out hud.tertiaryLabel);
        }


        static void Atmosphere()
        {
            var moon = new GameObject("California evening").AddComponent<Light>(); moon.type = LightType.Directional; moon.color = new Color(.7f, .7f, .85f); moon.intensity = 1f; moon.transform.rotation = Quaternion.Euler(35, -30, 0); moon.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.22f, .21f, .24f); RenderSettings.fog = true; RenderSettings.fogColor = new Color(.1f, .1f, .13f); RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .01f;
            Light("Motel lamp", new Vector3(0, 2.6f, 6), new Color(1, .75f, .45f), 1.8f, 10);
        }

        static void FloorRoom(string name, Vector3 center, Vector2 size, Material material, float height)
        {
            Box(name + " floor", center, new Vector3(size.x, .18f, size.y), material);
            Boundary(name + " west", center + new Vector3(-size.x * .5f, height * .5f, 0), new Vector3(.35f, height, size.y));
            Boundary(name + " east", center + new Vector3(size.x * .5f, height * .5f, 0), new Vector3(.35f, height, size.y));
        }

        static Collider Zone(string name, Vector3 center, Vector3 size)
        {
            var zone = Box(name, center, size, Stucco); zone.GetComponent<Renderer>().enabled = false;
            var collider = zone.GetComponent<BoxCollider>(); collider.isTrigger = true; zone.layer = 2; return collider;
        }

        static GameObject Npc(string name, HuntItem kind, string label, string description, Vector3 position)
        {
            var actor = Box(name, position, new Vector3(.62f, 1.7f, .42f), Wood);
            var item = actor.AddComponent<HuntInteractable>(); item.kind = kind; item.label = label; item.description = description; Marker(actor, item);
            return actor;
        }

        static void Interactable(HuntItem kind, string label, string description, Vector3 position, Material material, bool needsLight = false)
        {
            var itemRoot = Box("Interact - " + kind, position, new Vector3(.58f, .28f, .46f), material);
            var item = itemRoot.AddComponent<HuntInteractable>(); item.kind = kind; item.label = label; item.description = description; item.needsLight = needsLight; Marker(itemRoot, item);
        }

        static void Marker(GameObject owner, HuntInteractable item)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere); marker.name = "Evidence marker"; marker.transform.SetParent(owner.transform); marker.transform.localPosition = Vector3.up * .65f; marker.transform.localScale = new Vector3(.18f, .4f, .22f); marker.GetComponent<Renderer>().sharedMaterial = Gold; UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>()); item.marker = marker.transform; Items.Add(item);
        }

        static Material Mat(string name, Color color, float smoothness = 0, Color? emission = null)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", smoothness);
            if(emission.HasValue) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", emission.Value); }
            AssetDatabase.CreateAsset(material, Root + "/Art/Materials/Stanford/" + name + ".mat");
            return material;
        }

        static GameObject Box(string name, Vector3 position, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube); box.name = name; box.transform.position = position; box.transform.localScale = scale; box.GetComponent<Renderer>().sharedMaterial = material; return box;
        }

        static void Boundary(string name, Vector3 position, Vector3 scale) => Box(name, position, scale, Stucco);
        static Transform Point(string name, Vector3 position) { var point = new GameObject(name).transform; point.position = position; return point; }
        static Light Light(string name, Vector3 position, Color color, float intensity, float range) { var light = new GameObject(name).AddComponent<Light>(); light.transform.position = position; light.type = LightType.Point; light.color = color; light.intensity = intensity; light.range = range; return light; }
        static GameObject Part(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material) { var part = Box(name, Vector3.zero, scale, material); if(parent) part.transform.SetParent(parent, false); part.transform.localPosition = localPosition; UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>()); part.layer = 8; return part; }
        static void Sign(string text, Vector3 position, float size, Material material, float yaw = 0) { var sign = new GameObject("Sign - " + text); sign.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0)); sign.transform.localScale = Vector3.one * .09f; var label = sign.AddComponent<TextMeshPro>(); label.font = Font; label.text = text; label.fontSize = size * 10; label.alignment = TextAlignmentOptions.Center; label.color = material.color; label.textWrappingMode = TextWrappingModes.NoWrap; label.rectTransform.sizeDelta = new Vector2(size * 3, 2); }
        static TMP_Text Text(Transform parent, string name, string value, Vector2 min, Vector2 max, int size, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft) { var textRoot = new GameObject(name, typeof(RectTransform)); textRoot.transform.SetParent(parent, false); var text = textRoot.AddComponent<TextMeshProUGUI>(); text.font = Font; text.text = value; text.fontSize = size; text.color = new Color(.92f, .86f, .76f); text.alignment = alignment; text.raycastTarget = false; var rect = text.rectTransform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; return text; }
        static Image Panel(Transform parent, string name, Vector2 min, Vector2 max, Color color) { var panelRoot = new GameObject(name, typeof(RectTransform)); panelRoot.transform.SetParent(parent, false); var image = panelRoot.AddComponent<Image>(); image.color = color; image.raycastTarget = false; var rect = image.rectTransform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; return image; }
        static Button Button(Transform parent, string name, float x, out TMP_Text label) { var image = Panel(parent, name, new Vector2(x, .08f), new Vector2(x + .23f, .155f), new Color(.16f, .1f, .07f, .96f)); image.raycastTarget = true; var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; label = Text(image.transform, "Label", name, Vector2.zero, Vector2.one, 20, TextAlignmentOptions.Center); return button; }
    }
}
