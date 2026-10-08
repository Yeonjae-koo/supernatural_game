using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace BeforeJericho.Editor
{
    /// <summary>Swaps the CH02 Atchafalaya greybox for the Blender kit models (Art/Models/Atchafalaya/): siding/concrete 4x4 m wall modules on the faces of the
    /// greybox wall boxes, deck tiles on the dock and house platform, and a cypress tree line behind the (now invisible) boundary colliders over mud ground cubes.
    /// The greybox objects keep their colliders and lose only their MeshRenderers; the kit instances live under one "Kit Art" root. Re-runnable.</summary>
    public static class AtchafalayaKit
    {
        const string Models = "Assets/_Game/Art/Models/Atchafalaya/", ScenePath = "Assets/_Game/Scenes/SCN_Atchafalaya_Slice.unity";

        [MenuItem("Before Jericho/Art/Swap Atchafalaya Kit")]
        public static string Run()
        {
            var scene = SceneManager.GetActiveScene();
            if(scene.path != ScenePath) throw new System.InvalidOperationException("AtchafalayaKit needs " + ScenePath + ", open scene is '" + scene.path + "'");
            var old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Kit Art");
            if(old) Object.DestroyImmediate(old);
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToList();
            List<Transform> Named(string n) => all.Where(t => t.name == n).ToList();
            var root = new GameObject("Kit Art").transform;
            var hidden = new HashSet<Renderer>();
            void Hide(Transform t) { if(t.GetComponent<MeshRenderer>() is MeshRenderer r) { r.enabled = false; hidden.Add(r); } }
            GameObject Load(string n) => AssetDatabase.LoadAssetAtPath<GameObject>(Models + n + ".fbx");
            GameObject siding = Load("SM_AT_Wall_Siding_4x4"), concrete = Load("SM_AT_Wall_Concrete_4x4"), deck = Load("SM_AT_Deck_4x2"), cypress = Load("SM_AT_Cypress");
            var mud = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Atchafalaya/CH02 Mud.mat");
            if(!siding || !concrete || !deck || !cypress || !mud) throw new System.InvalidOperationException("Atchafalaya kit models/materials missing in " + Models);
            // Covers the faces of the greybox wall box with 4x4 m modules, 0.02 m off the face (both faces facing outward, or only the face toward `inside` facing it); returns the module count.
            int Faces(Transform box, GameObject module, bool bothSides, Vector2 inside, string name)
            {
                Vector3 p = box.position, sc = box.lossyScale;
                bool thinX = sc.x < sc.z; float len = thinX ? sc.z : sc.x, H = sc.y, bottom = p.y - H / 2, thin = thinX ? sc.x : sc.z;
                int n = Mathf.Max(1, Mathf.RoundToInt(len / 4)), count = 0; float w = len / n;
                var sides = bothSides ? new[] { -1f, 1f } : new[] { Mathf.Sign(thinX ? inside.x - p.x : inside.y - p.z) };
                foreach(var sgn in sides)
                    for(int i = 0; i < n; i++)
                    {
                        float along = (i + .5f) * w - len / 2, off = sgn * (thin / 2 + .02f);
                        var pos = thinX ? new Vector3(p.x + off, bottom, p.z + along) : new Vector3(p.x + along, bottom, p.z + off);
                        KitSwap.Place(module, pos, Quaternion.Euler(0, thinX ? sgn * 90 : (sgn > 0 ? 0 : 180), 0), name, root).transform.localScale = new Vector3(w / 4, H / 4, 1); count++;
                    }
                Hide(box);
                return count;
            }
            int sidingMods = 0, concreteMods = 0, genMods = 0, deckTiles = 0;
            foreach(var n in new[] { "Diner west", "Diner north", "Diner south", "House north", "House south", "House rear" })
                foreach(var t in Named(n)) sidingMods += Faces(t, siding, true, default, "Kit - Siding wall");
            foreach(var n in new[] { "Pump west shell", "Pump east shell", "Pump back west partition", "Pump back east partition", "Crawl entry west partition", "Crawl entry east partition", "Iron door partition west", "Iron door partition east", "Crawl left", "Crawl right" })
                foreach(var t in Named(n)) concreteMods += Faces(t, concrete, true, default, "Kit - Concrete wall");
            foreach(var n in new[] { "Generator west", "Generator east", "Generator north" })
                foreach(var t in Named(n)) genMods += Faces(t, concrete, false, new Vector2(0, 94), "Kit - Concrete wall");
            // Deck tiles (rotated 90 so the 4 m side runs along world Z) tiling the top of the box.
            int Deck(Transform box, string name)
            {
                Vector3 p = box.position, sc = box.lossyScale; float top = p.y + sc.y / 2;
                int nx = Mathf.Max(1, Mathf.RoundToInt(sc.x / 2)), nz = Mathf.Max(1, Mathf.RoundToInt(sc.z / 4)), c = 0;
                for(int i = 0; i < nx; i++)
                    for(int j = 0; j < nz; j++)
                    {
                        var pos = new Vector3(p.x - sc.x / 2 + (i + .5f) * sc.x / nx, top, p.z - sc.z / 2 + (j + .5f) * sc.z / nz);
                        KitSwap.Place(deck, pos, Quaternion.Euler(0, 90, 0), name, root).transform.localScale = new Vector3(sc.z / nz / 4, 1, sc.x / nx / 2); c++;
                    }
                Hide(box);
                return c;
            }
            foreach(var n in new[] { "Dock / Swamp Edge", "Raised house platform" })
                foreach(var t in Named(n)) deckTiles += Deck(t, "Kit - Deck");
            // Swamp tree line: boundaries stay as invisible colliders behind the cypresses; mud cubes give the ground outside the play area.
            foreach(var n in new[] { "West boundary", "East boundary", "North boundary", "South boundary" })
                foreach(var t in Named(n)) Hide(t);
            int ground = 0;
            foreach(var (c, s) in new[] { (new Vector3(-21, -.3f, 55), new Vector3(12, .5f, 140)), (new Vector3(21, -.3f, 55), new Vector3(12, .5f, 140)), (new Vector3(0, -.3f, 121), new Vector3(54, .5f, 8)), (new Vector3(0, -.3f, -11), new Vector3(54, .5f, 8)) })
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = "Kit - Swamp ground";
                Object.DestroyImmediate(g.GetComponent<Collider>()); g.GetComponent<MeshRenderer>().sharedMaterial = mud;
                g.transform.SetParent(root, false); g.transform.position = c; g.transform.localScale = s; ground++;
            }
            var rng = new System.Random(2);
            int trees = 0; long treeVerts = 0;
            void Tree(float x, float z)
            {
                // jitter is drawn by the caller; yaw then scale here
                var g = KitSwap.Place(cypress, new Vector3(x, -.05f, z), Quaternion.Euler(0, (float)(360 * rng.NextDouble()), 0), "Kit - Cypress", root);
                g.transform.localScale = Vector3.one * (float)(.85 + .3 * rng.NextDouble());
                foreach(var col in g.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
                foreach(var mf in g.GetComponentsInChildren<MeshFilter>()) treeVerts += mf.sharedMesh ? mf.sharedMesh.vertexCount : 0;
                trees++;
            }
            foreach(var s in new[] { -1f, 1f })
            {
                for(float z = -10; z <= 120; z += 5) Tree(s * (float)(16.5 + 1.5 * rng.NextDouble()), z);
                for(float z = -6; z <= 118; z += 8) Tree(s * (float)(21 + 1.5 * rng.NextDouble()), z);
            }
            for(float x = -20; x <= 20; x += 5) Tree(x, (float)(118.5 + rng.NextDouble()));
            for(float x = -18; x <= 18; x += 8) Tree(x, (float)(123 + rng.NextDouble()));
            for(float x = -20; x <= 20; x += 5) Tree(x, (float)(-8.5 - rng.NextDouble()));
            // Concrete floors take a darker copy of CH02 Concrete: under the raised ambient the shared material reads white on the ground.
            var floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Atchafalaya/CH02 Concrete floor.mat");
            foreach(var n in new[] { "Gas station slab", "Roadside", "Pump Station Exterior", "Pipe Crawl floor", "Generator Chamber floor" }) foreach(var t in Named(n)) t.GetComponent<MeshRenderer>().sharedMaterial = floorMat;
            // Props (no colliders): tin roofs over the diner/house (ridge along world Z), caged work lamps with a warm point light, pipe runs on the generator/pump walls, vertical valve pipes.
            GameObject roof = Load("SM_AT_TinRoof_Gable"), lamp = Load("SM_AT_CageLamp"), pipeRun = Load("SM_AT_PipeRun_4m"), valve = Load("SM_AT_PipeValve");
            if(!roof || !lamp || !pipeRun || !valve) throw new System.InvalidOperationException("Atchafalaya prop models missing in " + Models);
            int roofs = 0, lamps = 0, lampLights = 0, pipeRuns = 0, valves = 0;
            foreach(var (pos, ridge, span) in new[] { (new Vector3(-7.1f, 4.0f, 15), 13.4f, 12.2f), (new Vector3(-8.05f, 4.2f, 29), 10.2f, 9.1f) })
            { KitSwap.Place(roof, pos, Quaternion.Euler(0, 90, 0), "Kit - Tin roof", root).transform.localScale = new Vector3(ridge / 4, 1, span / 4); roofs++; }
            void Lamp(float x, float y, float z, float yaw)
            {
                var g = KitSwap.Place(lamp, new Vector3(x, y, z), Quaternion.Euler(0, yaw, 0), "Kit - Cage lamp", root); lamps++;
                // The light sits inside the cage, so a shadow-casting lamp would print its bars across the wall.
                foreach(var r in g.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var l = new GameObject("Cage lamp light").AddComponent<Light>(); l.transform.SetParent(g.transform, false); l.transform.localPosition = new Vector3(0, -.1f, .55f);
                l.type = LightType.Point; l.color = new Color(1f, .72f, .38f); l.intensity = 2f; l.range = 7f; HdrpAuthoring.Light(l); lampLights++;
            }
            foreach(var z in new[] { 47f, 51f }) { Lamp(-8.73f, 3.2f, z, 90); Lamp(8.73f, 3.2f, z, -90); }
            foreach(var z in new[] { 80f, 94f, 108f }) { Lamp(-11.73f, 3.1f, z, 90); Lamp(11.73f, 3.1f, z, -90); }
            void Pipe(float x, float y, float z, float yaw, float len)
            { KitSwap.Place(pipeRun, new Vector3(x, y, z), Quaternion.Euler(0, yaw, 0), "Kit - Pipe run", root).transform.localScale = new Vector3(len / 4, 1, 1); pipeRuns++; }
            for(int i = 0; i < 10; i++) { float z = 73 + (i + .5f) * 4.2f; Pipe(-11.73f, 4.0f, z, 90, 4.2f); Pipe(11.73f, 4.0f, z, -90, 4.2f); }
            foreach(var cx in new[] { -5.55f, 5.55f }) foreach(var dx in new[] { -1.725f, 1.725f }) Pipe(cx + dx, 2.8f, 52.53f, 180, 3.45f);
            foreach(var z in new[] { 86f, 102f }) { KitSwap.Place(valve, new Vector3(-11.73f, .095f, z), Quaternion.Euler(0, 90, 0), "Kit - Pipe valve", root); KitSwap.Place(valve, new Vector3(11.73f, .095f, z), Quaternion.Euler(0, -90, 0), "Kit - Pipe valve", root); valves += 2; }
            KitSwap.Place(valve, new Vector3(-8.73f, .225f, 49), Quaternion.Euler(0, 90, 0), "Kit - Pipe valve", root); KitSwap.Place(valve, new Vector3(8.73f, .225f, 49), Quaternion.Euler(0, -90, 0), "Kit - Pipe valve", root); valves += 2;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            string log = "Atchafalaya kit: " + sidingMods + " siding + " + concreteMods + " concrete + " + genMods + " generator concrete wall modules, " + deckTiles + " deck tiles, " + trees + " cypress (" + treeVerts + " verts), " + ground + " swamp ground cubes, " + roofs + " tin roofs, " + lamps + " cage lamps (" + lampLights + " lights), " + pipeRuns + " pipe runs, " + valves + " pipe valves, " + hidden.Count + " renderers disabled";
            Debug.Log(log);
            return log;
        }
    }
}
