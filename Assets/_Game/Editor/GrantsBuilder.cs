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
    /// <summary>One-shot CH04 Sunset Star Motor Court greybox builder. It creates a new scene and never replaces an authored one.</summary>
    public static class GrantsBuilder
    {
        const string Root = "Assets/_Game";
        const string ScenePath = Root + "/Scenes/SCN_GrantsNM_Slice.unity";
        static readonly List<HuntInteractable> Items = new();
        static TMP_FontAsset Font;
        static HuntSession Session;
        static Material Stucco, Asphalt, Sand, Wood, Steel, Tile, Paper, Ghost, Gold, Neon, Salt, Screen;

        [MenuItem("Before Jericho/Create Grants New Mexico Slice")]
        public static void Create()
        {
            if(File.Exists(ScenePath)) throw new InvalidOperationException("CH04 scene already exists. Keep the authored scene, or remove its generated assets before rebuilding.");
            if(EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the active scene before generating CH04.");
            foreach(string folder in new[] { "Scenes", "Data/GrantsNM", "Art/Materials/GrantsNM" }) Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if(!Font) throw new InvalidOperationException("TMP Essential Resources are required.");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Items.Clear();
            Materials();
            var events = new MotelEvent[5];
            var neon = ParkingLot(events);
            FrontOffice();
            var walkwayLights = MotelRooms(events);
            Laundry();
            var maintenanceGate = MaintenanceCorridor(out var corridorExitGate);
            var utilityZone = UtilityRoom();
            var rearGate = RearStorage(out var throwSources);
            events[2] = new MotelEvent { line = "The walkway lights die one by one, marching toward you.", zone = Zone("Event - walkway lights", new Vector3(4, 1.5f, 70), new Vector3(4.5f, 3, 20)), minEvidence = 2, lightsOff = walkwayLights };

            Session = new GameObject("Hunt Session").AddComponent<HuntSession>();
            Session.definition = NewDefinition();
            Session.sound = Session.gameObject.AddComponent<HuntAudio>();
            Player();
            var poltergeist = Poltergeist(throwSources);
            var chapter = Session.gameObject.AddComponent<GrantsChapter>();
            chapter.Configure(Session, poltergeist, Point("Parking lot arrival", new Vector3(-3, .1f, 4)), Point("Front office checkpoint", new Vector3(-10, .25f, 23.5f)),
                Point("Laundry checkpoint", new Vector3(-10, .25f, 42)), Point("Utility room checkpoint", new Vector3(-10, .25f, 77)),
                maintenanceGate, corridorExitGate, rearGate, utilityZone, neon, events);
            Session.items = Items.ToArray();
            Hud();
            Atmosphere();
            HdrpAuthoring.FinishScene("SCN_GrantsNM_Slice", false);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            var build = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if(!build.Exists(scene => scene.path == ScenePath)) build.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = build.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("BEFORE JERICHO: Created CH04 Grants / Sunset Star Motor Court with 8 zones, 5 evidence items, 5 scripted events and 2 manifestations.");
        }

        static HuntDefinition NewDefinition()
        {
            var definition = ScriptableObject.CreateInstance<HuntDefinition>();
            definition.name = "SO_GrantsHunt"; definition.ghostSpeed = 2.2f; definition.ghostDamage = 15; definition.attackInterval = 2.2f; definition.ironDisperseSeconds = 7; definition.ignitionSeconds = 6;
            AssetDatabase.CreateAsset(definition, Root + "/Data/GrantsNM/SO_GrantsHunt.asset");
            return definition;
        }

        static void Materials()
        {
            Stucco = Mat("CH04 Faded stucco", new Color(.42f, .3f, .22f), .05f);
            Asphalt = Mat("CH04 Asphalt", new Color(.08f, .075f, .07f), .12f);
            Sand = Mat("CH04 Desert sand", new Color(.3f, .23f, .15f));
            Wood = Mat("CH04 Motel wood", new Color(.2f, .12f, .06f), .2f);
            Steel = Mat("CH04 Utility steel", new Color(.2f, .22f, .22f), .5f);
            Tile = Mat("CH04 Laundry tile", new Color(.32f, .34f, .3f), .2f);
            Paper = Mat("CH04 Paper", new Color(.6f, .54f, .38f));
            Ghost = Mat("CH04 Manifestation", new Color(.42f, .5f, .55f), .6f, new Color(.12f, .2f, .26f));
            Gold = Mat("CH04 Gold marker", new Color(.78f, .48f, .08f), .25f, new Color(.16f, .07f, .005f));
            Neon = Mat("CH04 Neon", new Color(.9f, .2f, .12f), .3f, new Color(1.4f, .25f, .1f));
            Salt = Mat("CH04 Salt", new Color(.92f, .92f, .88f), .1f, new Color(.12f, .12f, .11f));
            Screen = Mat("CH04 TV static", new Color(.6f, .7f, .75f), .5f, new Color(.7f, .85f, .95f));
        }

        // Zone 1: parking lot, Impala trunk kit, the guest and the flickering VACANCY sign.
        static Light ParkingLot(MotelEvent[] events)
        {
            Box("Grants ground", new Vector3(0, -.3f, 55), new Vector3(50, .55f, 120), Sand);
            Box("Parking lot asphalt", new Vector3(0, .01f, 10), new Vector3(40, .12f, 20), Asphalt);
            Boundary("West lot boundary", new Vector3(-25, 2, 55), new Vector3(.4f, 4, 120));
            Boundary("East lot boundary", new Vector3(25, 2, 55), new Vector3(.4f, 4, 120));
            Boundary("South Route 66 boundary", new Vector3(0, 2, -5), new Vector3(50, 4, .4f));
            Boundary("North desert boundary", new Vector3(0, 2, 115), new Vector3(50, 4, .4f));
            for(int x = -14; x <= 14; x += 4) Box("Parking stripe", new Vector3(x, .08f, 12), new Vector3(.15f, .02f, 4), Paper);
            Box("Neon sign post", new Vector3(0, 2, 19.5f), new Vector3(.3f, 4, .3f), Steel);
            Sign("SUNSET STAR MOTOR COURT", new Vector3(0, 5, 19.4f), 3, Gold);
            Sign("VACANCY", new Vector3(0, 4.1f, 19.3f), 2.4f, Neon);
            var neon = Light("VACANCY neon", new Vector3(0, 4, 18.6f), new Color(1, .3f, .15f), 3.2f, 14);
            Car(new Vector3(-6, .58f, 5));
            Interactable(HuntItem.ImpalaTrunk, "Open the Impala trunk", "IMPALA TRUNK: An iron rod and a canister of rock salt. [3] Iron disperses. [4] then LMB pours a salt line.", new Vector3(-6, .95f, 2.1f), Gold);
            Npc("Motel guest", HuntItem.MotelGuest, "Talk to the guest", "Guest: I checked into room eight. Woke up in six.\nDean: Rough night?\nGuest: I didn't drink. Not that night.", new Vector3(6, .9f, 16));
            var radio = Part(null, "Impala radio glow", new Vector3(-6, 1.35f, 5.9f), new Vector3(.35f, .12f, .1f), Neon); radio.SetActive(false);
            var door = Box("Room 3 door - slammed", new Vector3(6.1f, 1.2f, 46), new Vector3(.12f, 2.4f, 1.3f), Wood); door.GetComponent<Collider>().enabled = false; door.SetActive(false);
            events[0] = new MotelEvent { line = "Down the row, a room door slams shut on its own.\nDean: \"Friendly place.\"", zone = Zone("Event - arrival door slam", new Vector3(0, 1.5f, 15), new Vector3(40, 3, 10)), show = door };
            events[4] = new MotelEvent { line = "The Impala's radio turns itself on. Through the static: \"...room eight... room six...\"", zone = Zone("Event - Impala radio", new Vector3(-6, 1.5f, 6), new Vector3(10, 3, 10)), minEvidence = 3, show = radio };
            return neon;
        }

        // Zone 2: Lorna's front office holds the guest ledger, blueprint and, after the hunt, John's phone record.
        static void FrontOffice()
        {
            FloorRoom("Front Office", new Vector3(-10, .1f, 27), new Vector2(12, 10), Stucco, 3.5f);
            Sign("OFFICE", new Vector3(-10, 3.2f, 21.9f), 1.8f, Neon);
            Box("Office counter", new Vector3(-10, .6f, 28.5f), new Vector3(7, 1, .8f), Wood);
            Npc("Lorna Hayes", HuntItem.LornaHayes, "Talk to Lorna Hayes", "Lorna: Old motels make noise.\nDean: Usually not in complete sentences.", new Vector3(-14.8f, .9f, 27.5f));
            Interactable(HuntItem.GuestLedger, "Read the guest ledger", "GUEST LEDGER: Deaths in 1958 (room 8), 1971 (room 6), 1989 (room 4) and 2003 (room 2). Different numbers, same kind of death.", new Vector3(-8.5f, 1.25f, 28.5f), Paper);
            Interactable(HuntItem.RenovationBlueprint, "Study the renovation blueprint", "RENOVATION BLUEPRINT: Every remodel shifted the room numbers by two. Old 8, 6, 4 and 2 are the same physical unit — the one backing onto the utility room's east wall.", new Vector3(-11.5f, 1.25f, 28.5f), Paper);
            Box("Phone table", new Vector3(-6, .45f, 31), new Vector3(1.2f, .7f, .8f), Wood);
            Interactable(HuntItem.JohnPhoneRecord, "Check the phone records", "PHONE RECORD", new Vector3(-6, .95f, 31), Paper);
        }

        // Zone 3: rooms 1–12 along one walkway. Room 6 hides the photo; rooms 3, 6 and 8 carry scripted scares.
        static Light[] MotelRooms(MotelEvent[] events)
        {
            Box("Walkway concrete", new Vector3(4, .05f, 60), new Vector3(4, .1f, 48), Tile);
            Boundary("Motel wing back wall", new Vector3(16, 1.5f, 60), new Vector3(.35f, 3, 48));
            Box("Motel wing roof", new Vector3(10, 3.1f, 60), new Vector3(12.5f, .2f, 48.4f), Wood);
            for(int k = 0; k <= 12; k++) Boundary("Room partition " + k, new Vector3(11, 1.5f, 36 + k * 4), new Vector3(10, 3, .25f));
            for(int i = 1; i <= 12; i++)
            {
                float z = 34 + i * 4;
                Sign(i.ToString(), new Vector3(6.1f, 2.55f, z), 1.2f, Paper, 90);
                Box("Room " + i + " bed", new Vector3(12.5f, .35f, z - 1.1f), new Vector3(2.4f, .5f, 1.6f), Wood); // South side keeps a clear lane to the back wall.
            }
            Interactable(HuntItem.OldMotelPhoto, "Study the framed photo", "OLD PHOTO: 1952, before the motel. A mining crew poses at a timber support post — POST 2. The deaths line up with that same beam.", new Vector3(15.4f, 1.3f, 58.8f), Paper);
            Box("Room 6 mirror", new Vector3(15.75f, 1.5f, 57), new Vector3(.08f, 1.2f, .8f), Screen);
            var figure = Box("Mirror figure", new Vector3(15.7f, 1.4f, 57), new Vector3(.05f, 1f, .35f), Ghost); figure.GetComponent<Collider>().enabled = false; figure.SetActive(false);
            events[3] = new MotelEvent { line = "In room six's mirror someone stands right behind you. The room behind you is empty.", zone = Zone("Event - room 6 mirror", new Vector3(11, 1.5f, 58), new Vector3(9.5f, 3, 3.6f)), minEvidence = 1, flash = figure };
            Box("Room 8 television", new Vector3(15.2f, .9f, 66), new Vector3(.6f, .6f, .8f), Steel);
            var tv = Box("Room 8 TV static", new Vector3(14.88f, .95f, 66), new Vector3(.04f, .45f, .65f), Screen); tv.GetComponent<Collider>().enabled = false;
            var tvLight = new GameObject("TV glow").AddComponent<Light>(); tvLight.transform.SetParent(tv.transform, false); tvLight.transform.localPosition = new Vector3(-8, 0, 0); tvLight.type = LightType.Point; tvLight.color = new Color(.6f, .8f, 1); tvLight.intensity = 1.5f; tvLight.range = 5;
            tv.SetActive(false);
            events[1] = new MotelEvent { line = "Room eight's TV snaps on by itself. Static, then a 1958 weather report.", zone = Zone("Event - room 8 TV", new Vector3(11, 1.5f, 66), new Vector3(9.5f, 3, 3.6f)), show = tv };
            var lights = new Light[6];
            for(int i = 0; i < lights.Length; i++) lights[i] = Light("Walkway light " + (i + 1), new Vector3(4, 2.8f, 40 + i * 8), new Color(1, .8f, .5f), 1.6f, 8);
            return lights;
        }

        // Zone 4: the laundry holds the maintenance note and the locked access to the service corridor.
        static void Laundry()
        {
            FloorRoom("Laundry", new Vector3(-10, .1f, 46), new Vector2(12, 12), Tile, 3.5f);
            Sign("LAUNDRY", new Vector3(-10, 3.1f, 39.9f), 1.6f, Paper);
            for(int x = -15; x <= -12; x += 2) Box("Washer", new Vector3(x, .55f, 44), new Vector3(1.2f, 1, 1), Steel);
            Box("Folding table", new Vector3(-13, .5f, 49), new Vector3(2.4f, .8f, 1), Wood);
            Interactable(HuntItem.MaintenanceNote, "Read the maintenance note", "MAINTENANCE NOTE (1996): Rewiring the utility room. Found a canvas pouch inside the east wall. Put it back. Guests on that line keep complaining about knocking.", new Vector3(-13, 1.05f, 49), Paper);
        }

        // Zone 5: crouch, narrow squeeze, then crawl. Real ceilings enforce each stance.
        static GameObject MaintenanceCorridor(out GameObject exitGate)
        {
            Boundary("Laundry north wall west", new Vector3(-13.6f, 1.75f, 52), new Vector3(4.8f, 3.5f, .35f));
            Boundary("Laundry north wall east", new Vector3(-6.4f, 1.75f, 52), new Vector3(4.8f, 3.5f, .35f));
            var gate = Box("Maintenance door - opens with blueprint and kit", new Vector3(-10, 1.5f, 52.2f), new Vector3(2.4f, 3, .3f), Steel);
            Sign("MAINTENANCE", new Vector3(-10, 3.2f, 51.8f), 1.3f, Gold);
            Box("Corridor floor", new Vector3(-10, .1f, 63.5f), new Vector3(2.4f, .18f, 23), Tile);
            Boundary("Corridor west wall", new Vector3(-11.2f, 1.5f, 63.5f), new Vector3(.3f, 3, 23));
            Boundary("Corridor east wall", new Vector3(-8.8f, 1.5f, 63.5f), new Vector3(.3f, 3, 23));
            Box("Low duct ceiling - crouch", new Vector3(-10, 1.55f, 60.5f), new Vector3(2.4f, .2f, 11), Steel);
            Box("Narrow west studs", new Vector3(-10.85f, .8f, 63.5f), new Vector3(.4f, 1.4f, 5), Wood);
            Box("Narrow east studs", new Vector3(-9.15f, .8f, 63.5f), new Vector3(.4f, 1.4f, 5), Wood);
            Box("Wall cavity ceiling - crawl", new Vector3(-10, 1.1f, 69), new Vector3(2.4f, .2f, 6), Wood);
            for(int z = 67; z <= 71; z += 2) Box("Crawl space pipe", new Vector3(-10, .9f, z), new Vector3(2.2f, .12f, .12f), Steel).GetComponent<Collider>().enabled = false;
            Sign("LOW CLEARANCE", new Vector3(-10, 1.35f, 54.9f), .9f, Gold);
            exitGate = Box("Lockdown door - corridor exit", new Vector3(-10, 1.5f, 74.6f), new Vector3(2.4f, 3, .3f), Steel);
            return gate;
        }

        // Zone 6: utility room, wall cavity puzzle and first manifestation.
        static Collider UtilityRoom()
        {
            FloorRoom("Utility Room", new Vector3(-10, .1f, 81), new Vector2(12, 12), Stucco, 3.5f);
            Boundary("Utility south wall west", new Vector3(-13.6f, 1.75f, 75), new Vector3(4.8f, 3.5f, .35f));
            Boundary("Utility south wall east", new Vector3(-6.4f, 1.75f, 75), new Vector3(4.8f, 3.5f, .35f));
            Box("Water heater", new Vector3(-14.5f, 1.2f, 79), new Vector3(1.2f, 2.2f, 1.2f), Steel);
            Box("Breaker panel", new Vector3(-15.7f, 1.6f, 83), new Vector3(.2f, 1.2f, .9f), Steel);
            for(int post = 1; post <= 3; post++)
            {
                float z = 75 + post * 3;
                Box("Wall post " + post, new Vector3(-4.35f, 1.6f, z), new Vector3(.3f, 3.2f, .35f), Wood);
                Sign("POST " + post, new Vector3(-4.6f, 2.7f, z), .9f, Paper, 90);
                if(post == 2) Interactable(HuntItem.WallCavity, "Tear open the wall at post 2", "WALL CAVITY: A miner's brass tag and a scorched canvas pouch — personal effects of a 1952 mine accident victim. This is the anchor.", new Vector3(-4.75f, 1.1f, z + .9f), Gold);
                else Interactable(HuntItem.WallDecoy, "Open the wall at post " + post, "Hollow drywall at post " + post + ". Nothing inside. The deaths don't line up here.", new Vector3(-4.75f, 1.1f, z + .9f), Stucco);
            }
            Sign("UTILITY", new Vector3(-10, 3.2f, 75.3f), 1.4f, Paper);
            return Zone("Utility room zone", new Vector3(-10, 1.5f, 81), new Vector3(11.5f, 3, 11.5f));
        }

        // Zones 7 and 8: rear storage with the burn barrel, throw shelves and the exterior return door.
        static GameObject RearStorage(out Transform[] throwSources)
        {
            Box("Rear storage floor", new Vector3(-10, .1f, 93), new Vector3(12, .18f, 12), Wood);
            Boundary("Rear storage north", new Vector3(-10, 1.75f, 99), new Vector3(12, 3.5f, .35f));
            Boundary("Rear storage east", new Vector3(-4, 1.75f, 93), new Vector3(.35f, 3.5f, 12));
            Boundary("Rear storage west south", new Vector3(-16, 1.75f, 89.25f), new Vector3(.35f, 3.5f, 4.5f));
            Boundary("Rear storage west north", new Vector3(-16, 1.75f, 96.75f), new Vector3(.35f, 3.5f, 4.5f));
            var gate = Box("Lockdown door - rear exterior", new Vector3(-16, 1.5f, 93), new Vector3(.35f, 3, 3), Steel);
            Sign("REAR STORAGE", new Vector3(-10, 3.2f, 87.3f), 1.4f, Paper);
            var shelves = new[] { new Vector3(-14.8f, 1, 90), new Vector3(-14.8f, 1, 97), new Vector3(-5.2f, 1, 91), new Vector3(-5.2f, 1, 96) };
            throwSources = new Transform[shelves.Length];
            for(int i = 0; i < shelves.Length; i++)
            {
                Box("Storage shelf", shelves[i], new Vector3(1, 2, 2), Wood);
                throwSources[i] = Point("Throw source " + (i + 1), shelves[i] + new Vector3(shelves[i].x < -10 ? .9f : -.9f, 1.5f, 0));
            }
            var barrel = Box("Burn barrel", new Vector3(-10, .7f, 96), new Vector3(.9f, 1.2f, .9f), Steel);
            var item = barrel.AddComponent<HuntInteractable>(); item.kind = HuntItem.Anchor; item.label = "Burn the anchor"; item.description = "The burn barrel."; Marker(barrel, item);
            // Exterior return path to the lot.
            Box("Service path", new Vector3(-20.5f, .02f, 56), new Vector3(8, .1f, 76), Asphalt);
            return gate;
        }

        static PoltergeistHunt Poltergeist(Transform[] throwSources)
        {
            var root = new GameObject("Sunset Star poltergeist");
            var poltergeist = root.AddComponent<PoltergeistHunt>();
            // Both re-form in the utility room, well south of the burn barrel, so iron and salt buy real time.
            var homes = new[] { new Vector3(-7, 1.3f, 84), new Vector3(-15, 1.3f, 84.5f) };
            var manifestations = new ManifestationHunt[homes.Length];
            for(int i = 0; i < homes.Length; i++)
            {
                var actor = new GameObject("Manifestation " + (char)('A' + i)) { layer = 2 };
                actor.transform.position = homes[i];
                var visual = new GameObject("Manifestation shape") { layer = 8 };
                visual.transform.SetParent(actor.transform, false);
                Part(visual.transform, "Torso", new Vector3(0, -.2f, 0), new Vector3(.55f, 1.1f, .3f), Ghost);
                Part(visual.transform, "Head", new Vector3(.05f, .55f, 0), new Vector3(.34f, .36f, .3f), Ghost);
                Part(visual.transform, "Reaching arm", new Vector3(.3f, .1f, .35f), new Vector3(.14f, .14f, .8f), Ghost);
                var hitbox = visual.AddComponent<BoxCollider>(); hitbox.center = new Vector3(0, 0, 0); hitbox.size = new Vector3(.7f, 1.7f, .5f);
                manifestations[i] = actor.AddComponent<ManifestationHunt>();
                manifestations[i].Configure(Session, poltergeist, visual);
                actor.SetActive(false);
            }
            var salt = new Transform[3];
            for(int i = 0; i < salt.Length; i++) { var line = Part(root.transform, "Salt line " + (i + 1), Vector3.zero, new Vector3(2.4f, .05f, .35f), Salt); line.layer = 2; line.SetActive(false); salt[i] = line.transform; }
            var projectiles = new Transform[2];
            for(int i = 0; i < projectiles.Length; i++) { var p = Part(root.transform, "Thrown object " + (i + 1), Vector3.zero, new Vector3(.4f, .4f, .4f), Wood); p.layer = 2; p.SetActive(false); projectiles[i] = p.transform; }
            poltergeist.Configure(Session, manifestations, salt, throwSources, projectiles);
            return poltergeist;
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
            var moon = new GameObject("Desert night moon").AddComponent<Light>(); moon.type = LightType.Directional; moon.color = new Color(.55f, .6f, .8f); moon.intensity = .9f; moon.transform.rotation = Quaternion.Euler(40, 35, 0); moon.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.17f, .15f, .2f); RenderSettings.fog = true; RenderSettings.fogColor = new Color(.06f, .05f, .07f); RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .012f;
            Light("Office lamp", new Vector3(-10, 3, 27), new Color(1, .75f, .45f), 2, 10);
            Light("Laundry fluorescent", new Vector3(-10, 3, 46), new Color(.8f, .95f, .85f), 1.8f, 10);
            Light("Utility bulb", new Vector3(-10, 3, 81), new Color(1, .7f, .4f), 1.6f, 10);
            Light("Rear storage red bulb", new Vector3(-10, 3, 93), new Color(.8f, .15f, .08f), 2, 11);
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

        static void Car(Vector3 position)
        {
            var car = new GameObject("1967 Impala - return point");
            Part(car.transform, "Body", new Vector3(0, .55f, 0), new Vector3(2.05f, .65f, 4.8f), Steel);
            Part(car.transform, "Cabin", new Vector3(0, 1.16f, -.2f), new Vector3(1.7f, .62f, 2.25f), Wood);
            car.transform.position = position;
            Interactable(HuntItem.GrantsExit, "Drive west on Route 66", "Route 66 west. California.", position + new Vector3(1.4f, .4f, 1), Gold);
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
            AssetDatabase.CreateAsset(material, Root + "/Art/Materials/GrantsNM/" + name + ".mat");
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
