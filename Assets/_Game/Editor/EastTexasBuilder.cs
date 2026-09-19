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
    /// <summary>One-shot CH03 Mercy Creek greybox builder. It creates a new scene and never replaces an authored one.</summary>
    public static class EastTexasBuilder
    {
        const string Root = "Assets/_Game";
        const string ScenePath = Root + "/Scenes/SCN_EastTexas_Slice.unity";
        static readonly List<HuntInteractable> Items = new();
        static TMP_FontAsset Font;
        static HuntSession Session;
        static GameObject GideonDisguise;
        static Material Brick, Asphalt, Grass, Wood, Steel, Tile, Paper, Skin, Gold, Blood;

        [MenuItem("Before Jericho/Create East Texas Slice")]
        public static void Create()
        {
            if(File.Exists(ScenePath)) throw new InvalidOperationException("CH03 scene already exists. Keep the authored scene, or remove its generated assets before rebuilding.");
            if(EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the active scene before generating CH03.");

            foreach(string folder in new[] { "Scenes", "Data/EastTexas", "Art/Materials/EastTexas" }) Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if(!Font) throw new InvalidOperationException("TMP Essential Resources are required.");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Items.Clear();
            GideonDisguise = null;
            Materials();
            GroundAndMainStreet();
            RecordsAndCemetery();
            FuneralHome();
            var points = Points();
            var basementGate = BasementAndTunnel();
            Session = new GameObject("Hunt Session").AddComponent<HuntSession>();
            Session.definition = NewDefinition();
            Session.sound = Session.gameObject.AddComponent<HuntAudio>();
            Player();
            var director = new GameObject("Mercy Creek encounter director").AddComponent<GhoulEncounterDirector>();
            var chapter = Session.gameObject.AddComponent<EastTexasChapter>();
            var ghouls = Ghouls(director);
            chapter.Configure(Session, director, points.arrival, points.cemeteryCheckpoint, points.basementCheckpoint, basementGate);
            chapter.ConfigureDisguise(GideonDisguise);
            director.Configure(Session, chapter, ghouls);
            var arena = Box("Basement encounter trigger", new Vector3(0, 1.5f, 109), new Vector3(28, 3, 20), Brick);
            arena.GetComponent<Renderer>().enabled = false;
            arena.GetComponent<BoxCollider>().isTrigger = true;
            director.ConfigureArena(arena.GetComponent<BoxCollider>());
            Session.items = Items.ToArray();
            Hud();
            Atmosphere();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            var build = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if(!build.Exists(scene => scene.path == ScenePath)) build.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = build.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("BEFORE JERICHO: Created CH03 East Texas / Mercy Creek with 8 zones, 5 evidence items and 6 bounded ghouls.");
        }

        static HuntDefinition NewDefinition()
        {
            var definition = ScriptableObject.CreateInstance<HuntDefinition>();
            definition.name = "SO_EastTexasHunt";
            AssetDatabase.CreateAsset(definition, Root + "/Data/EastTexas/SO_EastTexasHunt.asset");
            return definition;
        }

        static void Materials()
        {
            Brick = Mat("CH03 Brick", new Color(.24f, .095f, .06f), .1f);
            Asphalt = Mat("CH03 Asphalt", new Color(.075f, .08f, .075f), .15f);
            Grass = Mat("CH03 Wet grass", new Color(.065f, .12f, .06f));
            Wood = Mat("CH03 Coffin wood", new Color(.17f, .085f, .035f), .25f);
            Steel = Mat("CH03 Cold steel", new Color(.18f, .22f, .22f), .55f);
            Tile = Mat("CH03 Mortuary tile", new Color(.28f, .3f, .28f), .15f);
            Paper = Mat("CH03 Paper", new Color(.58f, .51f, .34f));
            Skin = Mat("CH03 Ghoul skin", new Color(.34f, .25f, .16f));
            Gold = Mat("CH03 Gold marker", new Color(.78f, .48f, .08f), .25f, new Color(.16f, .07f, .005f));
            Blood = Mat("CH03 Dried blood", new Color(.19f, .018f, .012f), .05f);
        }

        // Zone 1: Main Street, Impala arrival and the first Sheriff conversation.
        static void GroundAndMainStreet()
        {
            Box("Mercy Creek ground", new Vector3(0, -.3f, 65), new Vector3(42, .55f, 145), Grass);
            Box("Main Street asphalt", new Vector3(0, .01f, 11), new Vector3(14, .12f, 25), Asphalt);
            Boundary("West town boundary", new Vector3(-21, 2, 65), new Vector3(.4f, 4, 145));
            Boundary("East town boundary", new Vector3(21, 2, 65), new Vector3(.4f, 4, 145));
            Boundary("South town boundary", new Vector3(0, 2, -7), new Vector3(42, 4, .4f));
            Boundary("North town boundary", new Vector3(0, 2, 137), new Vector3(42, 4, .4f));
            Sign("MERCY CREEK / TEXAS", new Vector3(0, 3, 2), 4, Gold);
            Sign("MAIN STREET", new Vector3(0, 2.5f, 18), 2, Paper);
            for(int z = 2; z < 23; z += 5) Box("Road center stripe", new Vector3(0, .09f, z), new Vector3(.3f, .02f, 2.2f), Paper);
            Car(new Vector3(-5, .58f, 4));
            Npc("Sheriff Tom Reddick", HuntItem.SheriffReddick, "Talk to Sheriff Reddick", "Sheriff Reddick: Coyotes dig. People panic.\nDean: Coyotes usually take the whole meal.", new Vector3(5, .9f, 12));
            Box("Sheriff cruiser placeholder", new Vector3(7, .55f, 7), new Vector3(1.8f, .8f, 4.2f), Steel);
            Box("Mercy Creek police tape", new Vector3(4, 1.05f, 16), new Vector3(8, .07f, .06f), Gold);
        }

        // Zones 2 and 3: the county records counter leads directly into the disturbed cemetery.
        static void RecordsAndCemetery()
        {
            FloorRoom("Records Counter", new Vector3(-8, .1f, 29), new Vector3(14, 10), Brick, 3);
            Sign("MERCY CREEK RECORDS", new Vector3(-8, 3.25f, 24), 2.2f, Paper);
            Box("Records counter", new Vector3(-8, 1.05f, 29), new Vector3(9, 2, .8f), Wood);
            Interactable(HuntItem.MissingBodyParts, "Compare body inventory", "MISSING BODY PARTS: The funeral record and the body inventory do not match. Someone has been feeding before burial.", new Vector3(-10, 1.25f, 27.8f), Paper);
            Interactable(HuntItem.OldFuneralPhoto, "Study old funeral home photo", "OLD FUNERAL HOME PHOTO: Gideon Vale is in a forty-year-old picture and has not aged a day.", new Vector3(-5.5f, 1.25f, 28), Paper);
            Box("Cemetery path", new Vector3(0, .06f, 43), new Vector3(12, .14f, 21), Asphalt);
            Sign("MERCY CREEK CEMETERY", new Vector3(0, 2.5f, 35), 2.4f, Paper);
            for(int x = -14; x <= 14; x += 4)
                for(int z = 39; z <= 51; z += 4)
                    if(!(x == 0 && z == 43)) Grave(x, z);
            Box("Empty grave dirt", new Vector3(0, .03f, 44), new Vector3(2.7f, .1f, 4.3f), Blood);
            Interactable(HuntItem.EmptyGrave, "Inspect empty grave", "EMPTY GRAVE: The earth was opened carefully, then packed back down. This was not an animal attack.", new Vector3(0, .45f, 43), Gold);
        }

        // Zones 4–6: lobby, preparation room and cold storage form one readable funeral-home route.
        static void FuneralHome()
        {
            FloorRoom("Funeral Home Lobby", new Vector3(0, .1f, 61), new Vector3(24, 15), Wood, 4);
            Sign("VALE FUNERAL HOME", new Vector3(0, 4.1f, 53.2f), 3, Paper);
            Box("Lobby reception desk", new Vector3(-6, 1.05f, 60), new Vector3(7, 2, .8f), Wood);
            Npc("Nora Bell", HuntItem.NoraBell, "Talk to Nora Bell", "Nora Bell: Mr. Vale has run this place longer than I've been alive.\nDean: That makes two of us wondering how.", new Vector3(-4, .9f, 62));
            GideonDisguise = Npc("Gideon Vale", HuntItem.GideonVale, "Question Gideon Vale", "Gideon Vale: Death makes people imaginative.\nDean: Funny. Usually makes my job pretty straightforward.", new Vector3(6, .9f, 63));
            FloorRoom("Preparation Room", new Vector3(-7, .1f, 78), new Vector3(14, 15), Tile, 3);
            Box("Preparation table", new Vector3(-7, 1.1f, 78), new Vector3(3, 1.8f, 6), Steel);
            Interactable(HuntItem.StolenPersonalItem, "Recover stolen pendant", "STOLEN PERSONAL ITEM: A dead woman's pendant is hidden in the employee room. The thief works close to the bodies.", new Vector3(-11, 1.05f, 82), Gold);
            Box("Prep room blood drain", new Vector3(-3, .13f, 75), new Vector3(1.2f, .04f, 1.2f), Blood);
            FloorRoom("Cold Storage", new Vector3(8, .1f, 87), new Vector3(12, 15), Tile, 3);
            for(int z = 82; z <= 92; z += 4) Box("Cold storage rack", new Vector3(10, 1.3f, z), new Vector3(2.2f, 2.6f, .55f), Steel);
            Interactable(HuntItem.FeedingArea, "Inspect feeding area", "FEEDING AREA: Bone scraps and tagged remains fill the disposal bay. This is a ghoul nest.", new Vector3(5, .65f, 90), Blood, true);
            Sign("COLD STORAGE", new Vector3(8, 3.15f, 80), 2, Paper);
        }

        // Zones 7 and 8: a gated basement arena and a physically low maintenance escape route.
        static GameObject BasementAndTunnel()
        {
            FloorRoom("Service Basement", new Vector3(0, .1f, 108), new Vector3(30, 26), Brick, 4);
            Box("Basement embalming drain", new Vector3(0, .1f, 110), new Vector3(7, .08f, 8), Blood);
            Box("Basement cold locker", new Vector3(-9, 1.35f, 116), new Vector3(3, 2.7f, 4), Steel);
            Boundary("Basement entry west partition", new Vector3(-12.25f, 2, 96), new Vector3(17.5f, 4, .35f));
            Boundary("Basement entry east partition", new Vector3(12.25f, 2, 96), new Vector3(17.5f, 4, .35f));
            Boundary("Tunnel entry west partition", new Vector3(-11.55f, 2, 122), new Vector3(18.9f, 4, .35f));
            Boundary("Tunnel entry east partition", new Vector3(11.55f, 2, 122), new Vector3(18.9f, 4, .35f));
            var gate = Box("Basement security gate - opens on Gideon reveal", new Vector3(0, 1.7f, 96), new Vector3(7, 3.4f, .35f), Steel);
            Interactable(HuntItem.JohnMap, "Take John's folded map", "JOHN'S MAP: New Mexico is circled in John's handwriting. He was following the same pattern west.", new Vector3(9, 1, 113), Paper);
            Box("Service tunnel floor", new Vector3(0, .05f, 128), new Vector3(4.2f, .12f, 16), Tile);
            Boundary("Tunnel west wall", new Vector3(-2.1f, .9f, 128), new Vector3(.22f, 1.8f, 16));
            Boundary("Tunnel east wall", new Vector3(2.1f, .9f, 128), new Vector3(.22f, 1.8f, 16));
            Box("Low tunnel roof - crawl required", new Vector3(0, 1.13f, 126), new Vector3(4.2f, .25f, 7.5f), Steel);
            for(int z = 123; z <= 129; z += 2) Box("Tunnel pipe", new Vector3(0, .78f, z), new Vector3(3.65f, .23f, .23f), Steel).GetComponent<Collider>().enabled = false;
            Sign("SERVICE TUNNEL / LOW CLEARANCE", new Vector3(0, 1.9f, 121), 1.5f, Gold);
            Interactable(HuntItem.TexasAmmo, "Open maintenance ammo tin", "AMMO: A clean box of .45 rounds. Enough to keep moving.", new Vector3(-1.2f, .45f, 126), Gold, true);
            Car(new Vector3(0, .58f, 134));
            Interactable(HuntItem.TexasExit, "Return to the Impala", "The motel map points west. New Mexico is next.", new Vector3(0, 1, 132), Gold);
            return gate;
        }

        static GhoulHunt[] Ghouls(GhoulEncounterDirector director)
        {
            var setups = new[]
            {
                ("Cemetery ghoul A", new Vector3(-8, .05f, 45), false),
                ("Cemetery ghoul B", new Vector3(8, .05f, 48), false),
                ("Mortuary ghoul A", new Vector3(-10, .05f, 78), false),
                ("Cold storage ghoul", new Vector3(7, .05f, 89), false),
                ("Gideon Vale / Ghoul", new Vector3(0, .05f, 110), true),
                ("Basement ghoul companion", new Vector3(7, .05f, 116), false)
            };
            var results = new GhoulHunt[setups.Length];
            for(int i = 0; i < setups.Length; i++)
            {
                // The controller must not catch combat rays before its child head/body hitboxes.
                var root = new GameObject(setups[i].Item1) { layer = 2 };
                root.transform.position = setups[i].Item2;
                var controller = root.AddComponent<CharacterController>();
                controller.radius = .4f; controller.height = 1.8f; controller.center = Vector3.up * .9f;
                var ghoul = root.AddComponent<GhoulHunt>();
                BuildGhoulVisual(root.transform, setups[i].Item3);
                ghoul.Configure(Session, director, i, setups[i].Item3);
                results[i] = ghoul;
            }
            return results;
        }

        static void BuildGhoulVisual(Transform parent, bool boss)
        {
            float scale = boss ? 1.25f : 1f;
            Part(parent, "Ghoul torso", new Vector3(0, .92f * scale, 0), new Vector3(.68f, 1.15f, .4f) * scale, Skin);
            var head = Part(parent, "Ghoul head weak point", new Vector3(0, 1.65f * scale, .02f), new Vector3(.42f, .42f, .4f) * scale, Skin);
            var hitbox = head.AddComponent<GhoulHitbox>();
            hitbox.ghoul = parent.GetComponent<GhoulHunt>(); hitbox.head = true;
            var headCollider = head.AddComponent<BoxCollider>();
            headCollider.isTrigger = false;
            Part(parent, "Ghoul left arm", new Vector3(-.46f * scale, 1.05f * scale, 0), new Vector3(.18f, 1.1f, .2f) * scale, Skin);
            Part(parent, "Ghoul right arm", new Vector3(.46f * scale, 1.05f * scale, 0), new Vector3(.18f, 1.1f, .2f) * scale, Skin);
            // Keep the body target below the head target: lower-head shots must not resolve as body hits.
            var body = Part(parent, "Ghoul body hitbox", new Vector3(0, .8f * scale, .02f), new Vector3(.78f, 1.1f, .5f) * scale, Skin);
            body.AddComponent<GhoulHitbox>().ghoul = parent.GetComponent<GhoulHunt>();
            var bodyCollider = body.AddComponent<BoxCollider>();
            bodyCollider.isTrigger = false;
            if(boss)
            {
                Part(parent, "Gideon funeral coat", new Vector3(0, .95f * scale, -.04f), new Vector3(.82f, 1.3f, .48f) * scale, Wood);
                Part(parent, "Gideon left eye", new Vector3(-.12f * scale, 1.68f * scale, -.2f), new Vector3(.08f, .08f, .04f) * scale, Gold);
                Part(parent, "Gideon right eye", new Vector3(.12f * scale, 1.68f * scale, -.2f), new Vector3(.08f, .08f, .04f) * scale, Gold);
            }
        }

        static (Transform arrival, Transform cemeteryCheckpoint, Transform basementCheckpoint) Points()
        {
            return (Point("Mercy Creek arrival", new Vector3(-5, .1f, 4)), Point("Cemetery checkpoint", new Vector3(0, .1f, 37)), Point("Basement checkpoint", new Vector3(0, .1f, 101)));
        }

        static void Player()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Actors/PF_Dean.prefab");
            if(!prefab) throw new InvalidOperationException("PF_Dean.prefab is required; create the Prologue slice first.");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.name = "Dean Winchester"; player.transform.position = new Vector3(-5, .1f, 4); player.layer = 2;
            var motor = player.GetComponent<HunterMotor>(); var combat = player.GetComponent<HunterCombat>();
            motor.session = Session; combat.session = Session; combat.motor = motor; Session.player = motor;
            var camera = new GameObject("OTS Camera"); camera.tag = "MainCamera";
            var lens = camera.AddComponent<Camera>(); camera.AddComponent<AudioListener>(); lens.nearClipPlane = .05f; lens.farClipPlane = 170; lens.clearFlags = CameraClearFlags.SolidColor; lens.backgroundColor = new Color(.035f, .045f, .04f);
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
            Panel(root.transform, "Objective shade", new Vector2(.025f, .83f), new Vector2(.5f, .97f), new Color(.015f, .018f, .012f, .86f));
            Text(root.transform, "Chapter", "03 / EAST TEXAS", new Vector2(.04f, .93f), new Vector2(.48f, .96f), 17);
            hud.objective = Text(root.transform, "Objective", "", new Vector2(.04f, .845f), new Vector2(.48f, .915f), 23);
            hud.stats = Text(root.transform, "Equipment", "", new Vector2(.73f, .055f), new Vector2(.97f, .145f), 22, TextAlignmentOptions.BottomRight);
            hud.prompt = Text(root.transform, "Interaction", "", new Vector2(.2f, .16f), new Vector2(.8f, .21f), 23, TextAlignmentOptions.Center);
            hud.message = Text(root.transform, "Subtitle", "", new Vector2(.15f, .23f), new Vector2(.85f, .32f), 23, TextAlignmentOptions.Center);
            Text(root.transform, "Crosshair", "·", new Vector2(.49f, .485f), new Vector2(.51f, .515f), 30, TextAlignmentOptions.Center);
            Text(root.transform, "Controls", "WASD MOVE    E INVESTIGATE    F LIGHTER    Z CRAWL    TAB JOURNAL", new Vector2(.04f, .02f), new Vector2(.7f, .055f), 16);
            Panel(root.transform, "Health back", new Vector2(.04f, .09f), new Vector2(.19f, .102f), new Color(.15f, .08f, .07f));
            hud.healthFill = Panel(root.transform, "Health", new Vector2(.04f, .09f), new Vector2(.19f, .102f), new Color(.66f, .31f, .23f)); hud.healthFill.type = Image.Type.Filled; hud.healthFill.fillMethod = Image.FillMethod.Horizontal;
            hud.burnFill = Panel(root.transform, "Case progress", new Vector2(.35f, .14f), new Vector2(.65f, .15f), Gold.color); hud.burnFill.type = Image.Type.Filled; hud.burnFill.fillMethod = Image.FillMethod.Horizontal;
            var overlay = Panel(root.transform, "Menu overlay", Vector2.zero, Vector2.one, new Color(.012f, .015f, .01f, .97f)); overlay.raycastTarget = true; hud.overlay = overlay.gameObject;
            Panel(overlay.transform, "Brass accent", new Vector2(.095f, .18f), new Vector2(.099f, .88f), Gold.color);
            hud.subtitle = Text(overlay.transform, "Kicker", "", new Vector2(.13f, .87f), new Vector2(.9f, .92f), 20);
            hud.overlayTitle = Text(overlay.transform, "Title", "", new Vector2(.13f, .65f), new Vector2(.93f, .85f), 55);
            hud.overlayBody = Text(overlay.transform, "Body", "", new Vector2(.13f, .19f), new Vector2(.9f, .64f), 23); hud.overlayBody.enableAutoSizing = true; hud.overlayBody.fontSizeMin = 17; hud.overlayBody.fontSizeMax = 23;
            hud.primary = Button(overlay.transform, "Primary", .13f, out hud.primaryLabel); hud.secondary = Button(overlay.transform, "Secondary", .395f, out hud.secondaryLabel); hud.tertiary = Button(overlay.transform, "Tertiary", .66f, out hud.tertiaryLabel);
        }

        static void Atmosphere()
        {
            var moon = new GameObject("Texas storm moon").AddComponent<Light>(); moon.type = LightType.Directional; moon.color = new Color(.65f, .72f, .62f); moon.intensity = 1.05f; moon.transform.rotation = Quaternion.Euler(48, -30, 0); moon.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.18f, .21f, .16f); RenderSettings.fog = true; RenderSettings.fogColor = new Color(.055f, .065f, .045f); RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .017f;
            Light("Funeral porch light", new Vector3(0, 3.3f, 53), new Color(1f, .66f, .34f), 3, 14);
            Light("Cold storage light", new Vector3(8, 3.3f, 87), new Color(.55f, .76f, .7f), 2, 12);
            Light("Basement red light", new Vector3(0, 3.4f, 108), new Color(.75f, .08f, .03f), 2.4f, 16);
        }

        static void FloorRoom(string name, Vector3 center, Vector2 size, Material material, float height)
        {
            Box(name + " floor", center, new Vector3(size.x, .18f, size.y), material);
            Boundary(name + " west", center + new Vector3(-size.x * .5f, height * .5f, 0), new Vector3(.35f, height, size.y));
            Boundary(name + " east", center + new Vector3(size.x * .5f, height * .5f, 0), new Vector3(.35f, height, size.y));
            // Rooms deliberately open north/south so the compact investigation route has no invisible detours.
        }

        static void Grave(float x, float z)
        {
            Box("Grave mound", new Vector3(x, .08f, z), new Vector3(2.1f, .16f, 2.8f), Grass);
            Box("Headstone", new Vector3(x, .75f, z - 1), new Vector3(.8f, 1.45f, .2f), Tile);
        }

        static void Car(Vector3 position)
        {
            var car = new GameObject("1967 Impala - return point");
            Part(car.transform, "Body", new Vector3(0, .55f, 0), new Vector3(2.05f, .65f, 4.8f), Steel);
            Part(car.transform, "Cabin", new Vector3(0, 1.16f, -.2f), new Vector3(1.7f, .62f, 2.25f), Wood);
            car.transform.position = position;
        }

        static GameObject Npc(string name, HuntItem kind, string label, string description, Vector3 position)
        {
            var actor = Box(name, position, new Vector3(.62f, 1.7f, .42f), Skin);
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
            AssetDatabase.CreateAsset(material, Root + "/Art/Materials/EastTexas/" + name + ".mat");
            return material;
        }

        static GameObject Box(string name, Vector3 position, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube); box.name = name; box.transform.position = position; box.transform.localScale = scale; box.GetComponent<Renderer>().sharedMaterial = material; return box;
        }

        static void Boundary(string name, Vector3 position, Vector3 scale) => Box(name, position, scale, Brick);
        static Transform Point(string name, Vector3 position) { var point = new GameObject(name).transform; point.position = position; return point; }
        static Light Light(string name, Vector3 position, Color color, float intensity, float range) { var light = new GameObject(name).AddComponent<Light>(); light.transform.position = position; light.type = LightType.Point; light.color = color; light.intensity = intensity; light.range = range; return light; }
        static GameObject Part(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material) { var part = Box(name, Vector3.zero, scale, material); part.transform.SetParent(parent, false); part.transform.localPosition = localPosition; UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>()); part.layer = 8; return part; }
        static void Sign(string text, Vector3 position, float size, Material material) { var sign = new GameObject("Sign - " + text); sign.transform.position = position; sign.transform.localScale = Vector3.one * .09f; var label = sign.AddComponent<TextMeshPro>(); label.font = Font; label.text = text; label.fontSize = size * 10; label.alignment = TextAlignmentOptions.Center; label.color = material.color; label.textWrappingMode = TextWrappingModes.NoWrap; label.rectTransform.sizeDelta = new Vector2(size * 3, 2); }
        static TMP_Text Text(Transform parent, string name, string value, Vector2 min, Vector2 max, int size, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft) { var textRoot = new GameObject(name, typeof(RectTransform)); textRoot.transform.SetParent(parent, false); var text = textRoot.AddComponent<TextMeshProUGUI>(); text.font = Font; text.text = value; text.fontSize = size; text.color = new Color(.9f, .88f, .76f); text.alignment = alignment; text.raycastTarget = false; var rect = text.rectTransform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; return text; }
        static Image Panel(Transform parent, string name, Vector2 min, Vector2 max, Color color) { var panelRoot = new GameObject(name, typeof(RectTransform)); panelRoot.transform.SetParent(parent, false); var image = panelRoot.AddComponent<Image>(); image.color = color; image.raycastTarget = false; var rect = image.rectTransform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; return image; }
        static Button Button(Transform parent, string name, float x, out TMP_Text label) { var image = Panel(parent, name, new Vector2(x, .08f), new Vector2(x + .23f, .155f), new Color(.16f, .12f, .06f, .96f)); image.raycastTarget = true; var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; label = Text(image.transform, "Label", name, Vector2.zero, Vector2.one, 20, TextAlignmentOptions.Center); return button; }
    }
}
