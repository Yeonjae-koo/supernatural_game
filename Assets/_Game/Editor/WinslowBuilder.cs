using System;
using System.Collections.Generic;
using System.IO;
using BeforeJericho;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace BeforeJericho.Editor
{
    /// <summary>One-shot CH05 Coyote Mile greybox builder. It creates a new scene and never replaces an authored one.</summary>
    public static class WinslowBuilder
    {
        const string Root = "Assets/_Game";
        const string ScenePath = Root + "/Scenes/SCN_Winslow_Slice.unity";
        static readonly List<HuntInteractable> Items = new();
        static TMP_FontAsset Font;
        static HuntSession Session;
        static Material Stucco, Asphalt, Sand, Wood, Steel, Rust, Paper, Ghost, Gold, Neon, Salt;

        [MenuItem("Before Jericho/Create Winslow Arizona Slice")]
        public static void Create()
        {
            if(File.Exists(ScenePath)) throw new InvalidOperationException("CH05 scene already exists. Keep the authored scene, or remove its generated assets before rebuilding.");
            if(EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the active scene before generating CH05.");
            foreach(string folder in new[] { "Scenes", "Data/Winslow", "Art/Materials/Winslow" }) Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if(!Font) throw new InvalidOperationException("TMP Essential Resources are required.");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Items.Clear();
            Materials();
            var events = new MotelEvent[3];
            var arrivalCar = RoadAndCrashSite(events);
            Diner(); Garage();
            var motelZone = ClosedMotel();
            var motelCar = MotelRoom();
            var salvageCar = SalvageYard(out var headlights);

            Session = new GameObject("Hunt Session").AddComponent<HuntSession>();
            Session.definition = NewDefinition();
            Session.sound = Session.gameObject.AddComponent<HuntAudio>();
            Player();
            var spirit = RoadSpirit();
            var chapter = Session.gameObject.AddComponent<WinslowChapter>();
            chapter.Configure(Session, spirit, Point("Coyote Mile arrival", new Vector3(-5, .1f, 2)), Point("Diner checkpoint", new Vector3(-12, .25f, 46.5f)),
                Point("Motel checkpoint", new Vector3(9, .25f, 68)), Point("Salvage yard checkpoint", new Vector3(0, .25f, 104)), Point("Motel room 7", new Vector3(-12, .25f, 71)),
                arrivalCar, salvageCar, motelCar, motelZone, headlights, events);
            Session.items = Items.ToArray();
            Hud();
            Atmosphere();
            HdrpAuthoring.FinishScene("SCN_Winslow_Slice", false);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            var build = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if(!build.Exists(scene => scene.path == ScenePath)) build.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = build.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("BEFORE JERICHO: Created CH05 Winslow / Coyote Mile with road, diner, garage, motel, salvage yard, 6 evidence items and a road spirit.");
        }

        static HuntDefinition NewDefinition()
        {
            var definition = ScriptableObject.CreateInstance<HuntDefinition>();
            definition.name = "SO_WinslowHunt"; definition.ghostSpeed = 2.4f; definition.ghostDamage = 16; definition.attackInterval = 2.2f; definition.ironDisperseSeconds = 7; definition.ignitionSeconds = 6;
            AssetDatabase.CreateAsset(definition, Root + "/Data/Winslow/SO_WinslowHunt.asset");
            return definition;
        }

        static void Materials()
        {
            Stucco = Mat("CH05 Sunbaked stucco", new Color(.45f, .32f, .22f), .05f);
            Asphalt = Mat("CH05 Route 66 asphalt", new Color(.07f, .07f, .07f), .12f);
            Sand = Mat("CH05 Desert", new Color(.33f, .24f, .15f));
            Wood = Mat("CH05 Weathered wood", new Color(.22f, .14f, .07f), .15f);
            Steel = Mat("CH05 Steel", new Color(.2f, .22f, .23f), .5f);
            Rust = Mat("CH05 Rusted wreck", new Color(.3f, .13f, .06f), .2f);
            Paper = Mat("CH05 Paper", new Color(.6f, .54f, .38f));
            Ghost = Mat("CH05 Road spirit", new Color(.5f, .48f, .42f), .5f, new Color(.25f, .22f, .15f));
            Gold = Mat("CH05 Gold marker", new Color(.78f, .48f, .08f), .25f, new Color(.16f, .07f, .005f));
            Neon = Mat("CH05 Neon", new Color(.2f, .6f, .9f), .3f, new Color(.2f, .8f, 1.3f));
            Salt = Mat("CH05 Salt", new Color(.92f, .92f, .88f), .1f, new Color(.12f, .12f, .11f));
        }

        // Route 66 through Coyote Mile: arrival Impala, crash site, road silhouette and rear-view mirror scares.
        static GameObject RoadAndCrashSite(MotelEvent[] events)
        {
            Box("Coyote Mile desert", new Vector3(0, -.3f, 65), new Vector3(50, .55f, 140), Sand);
            Box("Route 66", new Vector3(0, .01f, 50), new Vector3(8, .12f, 100), Asphalt);
            Boundary("West desert boundary", new Vector3(-25, 2, 65), new Vector3(.4f, 4, 140));
            Boundary("East desert boundary", new Vector3(25, 2, 65), new Vector3(.4f, 4, 140));
            Boundary("South boundary", new Vector3(0, 2, -5), new Vector3(50, 4, .4f));
            Boundary("North boundary", new Vector3(0, 2, 135), new Vector3(50, 4, .4f));
            Boundary("Road closed barrier", new Vector3(0, 1.5f, 100), new Vector3(50, 3, .4f));
            Sign("ROAD CLOSED / SALVAGE AHEAD", new Vector3(0, 3.4f, 99.7f), 2, Gold);
            Sign("WINSLOW, ARIZONA / COYOTE MILE", new Vector3(5.5f, 3, 12), 2.2f, Gold);
            for(int z = 2; z < 98; z += 6) Box("Road center stripe", new Vector3(0, .09f, z), new Vector3(.2f, .02f, 2.5f), Gold);
            var car = Car("1967 Impala - Coyote Mile", new Vector3(-7, .58f, 6), HuntItem.WinslowDrive, "Drive to the salvage yard", "The Impala. The salvage yard is down the closed road.");
            var silhouette = Box("Road silhouette", new Vector3(0, 1, 24), new Vector3(.5f, 1.8f, .3f), Ghost); silhouette.GetComponent<Collider>().enabled = false; silhouette.SetActive(false);
            events[0] = new MotelEvent { line = "Someone stands in the middle of the road ahead. Dean blinks, and the road is empty.", zone = Zone("Event - road silhouette", new Vector3(0, 1.5f, 17), new Vector3(10, 3, 6)), flash = silhouette };
            for(int i = 0; i < 4; i++) Box("Skid mark", new Vector3(-1.5f + i, .08f, 31 + i * .5f), new Vector3(.25f, .02f, 5), Rust);
            Box("Broken guardrail", new Vector3(5, .5f, 33), new Vector3(.2f, .6f, 6), Steel);
            Interactable(HuntItem.CrashMarks, "Inspect the skid marks", "CRASH MARKS: Fresh skids over old ones, all swerving at the same guardrail. Somebody keeps braking for something in the road.", new Vector3(2.5f, .3f, 32), Rust);
            var wreckFlash = Box("Crash hallucination wreck", new Vector3(6, .7f, 34), new Vector3(2, 1.1f, 4.4f), Rust); wreckFlash.GetComponent<Collider>().enabled = false; wreckFlash.SetActive(false);
            events[1] = new MotelEvent { line = "For a heartbeat there is a burning car against the guardrail. Then only the scorched post.", zone = Zone("Event - crash hallucination", new Vector3(0, 1.5f, 32), new Vector3(10, 3, 8)), minEvidence = 1, flash = wreckFlash };
            var backSeat = Box("Rear-view mirror figure", new Vector3(-7, 1.35f, 4.8f), new Vector3(.45f, .8f, .3f), Ghost); backSeat.GetComponent<Collider>().enabled = false; backSeat.SetActive(false);
            events[2] = new MotelEvent { line = "In the Impala's rear-view mirror a burned man sits in the back seat. Dean turns. Empty.", zone = Zone("Event - rear-view mirror", new Vector3(-6, 1.5f, 6), new Vector3(8, 3, 10)), minEvidence = 2, flash = backSeat };
            return car;
        }

        // Mae's diner: John's almost-there beat, the police report and the cremation notice.
        static void Diner()
        {
            FloorRoom("Mae's Diner", new Vector3(-12, .1f, 50), new Vector2(12, 10), Stucco, 3.5f);
            Sign("MAE'S DINER", new Vector3(-12, 3.2f, 44.9f), 1.8f, Neon);
            Box("Diner counter", new Vector3(-12, .6f, 52.5f), new Vector3(7, 1, .8f), Wood);
            Npc("Mae Torres", HuntItem.MaeTorres, "Talk to Mae Torres", "Mae Torres", new Vector3(-17, .9f, 50));
            Interactable(HuntItem.PoliceReport, "Read the old police report", "POLICE REPORT (1979): Ray Colton, 31, lost control on Coyote Mile and burned in his Chevy. First death on this stretch.", new Vector3(-14, 1.25f, 52.5f), Paper);
            Interactable(HuntItem.CremationRecord, "Read the obituary clipping", "CREMATION RECORD: Ray Colton was cremated in Flagstaff. No grave, no bones to salt. Something else is holding him here.", new Vector3(-10, 1.25f, 52.5f), Paper);
        }

        static void Garage()
        {
            FloorRoom("Doyle Garage", new Vector3(12, .1f, 50), new Vector2(12, 10), Stucco, 3.5f);
            Sign("DOYLE GARAGE", new Vector3(12, 3.2f, 44.9f), 1.8f, Paper);
            Box("Hydraulic lift", new Vector3(10, .3f, 49), new Vector3(2.5f, .4f, 4.5f), Steel);
            Npc("Hank Doyle", HuntItem.HankDoyle, "Talk to Hank Doyle", "Hank: Car was totaled. Driver wasn't much better.\nHank: Hauled the wreck to the salvage yard. Nobody ever claimed it.", new Vector3(8, .9f, 53.5f));
            Box("Work bench", new Vector3(15, .5f, 52), new Vector3(2, .8f, 1), Wood);
            Interactable(HuntItem.GarageRecord, "Check the work record", "GARAGE WORK RECORD: 1979 tow, Chevy Bel Air, VIN matches the Colton crash. Towed to Coyote Salvage and never scrapped.", new Vector3(15, 1.05f, 52), Paper);
        }

        static Collider ClosedMotel()
        {
            FloorRoom("Desert Rose Motel", new Vector3(12, .1f, 72), new Vector2(12, 10), Stucco, 3.5f);
            Sign("DESERT ROSE MOTEL / CLOSED", new Vector3(12, 3.2f, 66.9f), 1.8f, Neon);
            Box("Front desk", new Vector3(15, .5f, 74), new Vector3(2, .8f, 1), Wood);
            Interactable(HuntItem.DriverIdentity, "Read the guest register", "DRIVER IDENTITY: Ray Colton checked in the night he died, room 4. The last three crash victims all stayed here too.", new Vector3(15, 1.05f, 74), Paper);
            return Zone("Closed motel zone", new Vector3(12, 1.5f, 72), new Vector3(11.5f, 3, 9.5f));
        }

        // Dean's own room across the road: voicemail and the exit Impala after the drive back.
        static GameObject MotelRoom()
        {
            FloorRoom("Room 7", new Vector3(-12, .1f, 72), new Vector2(10, 8), Wood, 3);
            Sign("ROOM 7", new Vector3(-12, 2.8f, 67.9f), 1.2f, Paper);
            Box("Motel desk", new Vector3(-14, .5f, 74), new Vector3(1.6f, .8f, 1), Wood);
            Interactable(HuntItem.JohnVoicemail, "Play the voicemail", "JOHN'S VOICEMAIL", new Vector3(-14, 1.05f, 74), Gold);
            return Car("1967 Impala - motel", new Vector3(-12, .58f, 62), HuntItem.WinslowExit, "Drive to Palo Alto", "Stanford. Sam.");
        }

        static GameObject SalvageYard(out Light[] headlights)
        {
            Box("Salvage yard dirt", new Vector3(0, .02f, 117), new Vector3(48, .1f, 34), Sand);
            Sign("COYOTE SALVAGE", new Vector3(0, 3.5f, 101), 2.2f, Gold);
            var wrecks = new[] { new Vector3(-14, .6f, 112), new Vector3(-6, .6f, 126), new Vector3(12, .6f, 110), new Vector3(15, .6f, 125), new Vector3(-17, .6f, 122) };
            foreach(var w in wrecks) Box("Junked car", w, new Vector3(2, 1, 4.4f), Rust);
            Box("Colton's Bel Air wreck", new Vector3(6, .6f, 122), new Vector3(2, 1, 4.4f), Rust);
            Interactable(HuntItem.SteeringWheel, "Pry out the steering wheel", "STEERING WHEEL FRAGMENT: Half-melted, with dried blood and a wedding band fused to the rim. Ray never let go. This is the anchor.", new Vector3(4.4f, 1, 122), Gold);
            var barrel = Box("Burn barrel", new Vector3(0, .7f, 124), new Vector3(.9f, 1.2f, .9f), Steel);
            var item = barrel.AddComponent<HuntInteractable>(); item.kind = HuntItem.Anchor; item.label = "Burn the steering wheel"; item.description = "The burn barrel."; Marker(barrel, item);
            var car = Car("1967 Impala - salvage", new Vector3(-8, .58f, 105), HuntItem.WinslowReturn, "Drive back to the motel", "Back to the motel.");
            headlights = new[] { Light("Impala headlight L", new Vector3(-8.6f, .8f, 107.6f), new Color(1, .95f, .8f), 2, 12), Light("Impala headlight R", new Vector3(-7.4f, .8f, 107.6f), new Color(1, .95f, .8f), 2, 12) };
            foreach(var l in headlights) l.transform.SetParent(car.transform, true);
            return car;
        }

        static PoltergeistHunt RoadSpirit()
        {
            var root = new GameObject("Road spirit");
            var hunt = root.AddComponent<PoltergeistHunt>();
            // Index 0 appears in the closed motel, index 1 at the salvage yard: one spirit, two haunts.
            var homes = new[] { new Vector3(16, 1.3f, 76), new Vector3(-12, 1.3f, 129) };
            var shapes = new ManifestationHunt[homes.Length];
            for(int i = 0; i < homes.Length; i++)
            {
                var actor = new GameObject("Road spirit " + (i == 0 ? "motel" : "salvage")) { layer = 2 };
                actor.transform.position = homes[i];
                var visual = new GameObject("Road spirit shape") { layer = 8 };
                visual.transform.SetParent(actor.transform, false);
                Part(visual.transform, "Torso", new Vector3(0, -.2f, 0), new Vector3(.55f, 1.1f, .32f), Ghost);
                Part(visual.transform, "Head", new Vector3(0, .55f, 0), new Vector3(.34f, .36f, .3f), Ghost);
                Part(visual.transform, "Headlight eye L", new Vector3(-.08f, .58f, .16f), new Vector3(.07f, .05f, .02f), Neon);
                Part(visual.transform, "Headlight eye R", new Vector3(.08f, .58f, .16f), new Vector3(.07f, .05f, .02f), Neon);
                var hitbox = visual.AddComponent<BoxCollider>(); hitbox.size = new Vector3(.7f, 1.7f, .5f);
                shapes[i] = actor.AddComponent<ManifestationHunt>();
                shapes[i].Configure(Session, hunt, visual);
                actor.SetActive(false);
            }
            var salt = new Transform[3];
            for(int i = 0; i < salt.Length; i++) { var line = Part(root.transform, "Salt line " + (i + 1), Vector3.zero, new Vector3(2.4f, .05f, .35f), Salt); line.layer = 2; line.SetActive(false); salt[i] = line.transform; }
            hunt.Configure(Session, shapes, salt, new Transform[0], new Transform[0]);
            return hunt;
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
            var shoulder = camera.AddComponent<ShoulderCamera>(); shoulder.player = motor; shoulder.session = Session; shoulder.lens = lens;
            motor.view = shoulder; combat.view = shoulder;
        }

        static void Hud()
        { UIPrefabBuilder.PlaceInScene(Session); } // PF_HuntUI: the HTML-prototype UI (screens, HUD, options); keeps one EventSystem


        static void Atmosphere()
        {
            var moon = new GameObject("Arizona night moon").AddComponent<Light>(); moon.type = LightType.Directional; moon.color = new Color(.6f, .6f, .75f); moon.intensity = .9f; moon.transform.rotation = Quaternion.Euler(38, -25, 0); moon.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.18f, .16f, .18f); RenderSettings.fog = true; RenderSettings.fogColor = new Color(.08f, .07f, .07f); RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .014f;
            Light("Diner light", new Vector3(-12, 3, 50), new Color(1, .8f, .5f), 2, 11);
            Light("Garage light", new Vector3(12, 3, 50), new Color(.85f, .9f, 1), 1.8f, 11);
            Light("Motel hall bulb", new Vector3(12, 3, 72), new Color(1, .6f, .35f), 1.4f, 10);
            Light("Room 7 lamp", new Vector3(-12, 2.6f, 72), new Color(1, .75f, .45f), 1.6f, 9);
            Light("Salvage yard floodlight", new Vector3(0, 5, 115), new Color(.9f, .85f, .7f), 2.2f, 22);
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
            var material = HdrpAuthoring.Lit(name, color, smoothness, emission);
            AssetDatabase.CreateAsset(material, Root + "/Art/Materials/Winslow/" + name + ".mat");
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
    }
}
