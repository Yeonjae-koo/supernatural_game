using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace BeforeJericho.Editor
{
    /// <summary>Swaps the Prologue greybox gas lamps and cemetery tombs for the Blender kit models (Art/Models/Prologue/) and covers the street face of each
    /// "Townhouse" box with 4x4 m wall/window modules (the box keeps its renderer for sides and top; greybox shutters on that face are hidden).
    /// Storeys above the ground use the plinth-less "Upper" modules, the first upper storey gets an iron balcony per module (greybox balconies and railings
    /// on that face are hidden), and a deterministic ~40% of the windows swap their pane for the warm "Window glow" material.
    /// The antique shop gets a fascia/column storefront and sign board, the cemetery an iron fence run with piers and a gate.
    /// The greybox objects keep their colliders and lose only their MeshRenderers; the kit instances live under one "Kit Art" root. Re-runnable.</summary>
    public static class KitSwap
    {
        const string Models = "Assets/_Game/Art/Models/Prologue/", ScenePath = "Assets/_Game/Scenes/SCN_NewOrleans_Slice.unity";

        [MenuItem("Before Jericho/Art/Swap Prologue Kit")]
        public static string Run()
        {
            var scene = SceneManager.GetActiveScene();
            if(scene.path != ScenePath) throw new System.InvalidOperationException("KitSwap needs " + ScenePath + ", open scene is '" + scene.path + "'");
            var old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Kit Art");
            if(old) Object.DestroyImmediate(old);
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToList();
            List<Transform> Named(string n) => all.Where(t => t.name == n).ToList();
            var root = new GameObject("Kit Art").transform;
            var hidden = new HashSet<Renderer>();
            // Hides the MeshRenderer of the greybox object `t` and of every `parts` object within 0.5 m (XZ) of it; colliders stay.
            void Hide(Transform t, params string[] parts)
            {
                foreach(var o in all.Where(o => o == t || (parts.Contains(o.name) && XZ(o, t) < .5f)))
                    if(o.GetComponent<MeshRenderer>() is MeshRenderer r) { r.enabled = false; hidden.Add(r); }
            }
            int lamps = 0, tombs = 0;
            var lamp = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_NO_GasLamp.fbx");
            var tomb = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_NO_Tomb_Raised.fbx");
            if(!lamp || !tomb) throw new System.InvalidOperationException("Kit models missing in " + Models);
            foreach(var p in Named("Lamp post"))
            {
                var g = Place(lamp, p.position + Vector3.down * 1.8f, Quaternion.identity, "Kit - Gas lamp", root);
                // the Gas lamp light sits inside the lantern, so a shadow-casting lantern would block it
                foreach(var r in g.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
                Hide(p, "Lantern"); lamps++;
            }
            foreach(var p in Named("Raised tomb"))
            {
                Place(tomb, p.position + Vector3.down * .8f, Quaternion.Euler(0, 180, 0), "Kit - Raised tomb", root);
                Hide(p, "Tomb cornice", "Cross", "Cross arms"); tombs++;
            }
            int facades = 0, mods = 0, balconies = 0, lit = 0, b = 0;
            var wall = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_NO_Wall_4x4.fbx");
            var win = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_NO_Wall_Window_4x4.fbx");
            var upWall = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_NO_Wall_Upper_4x4.fbx");
            var upWin = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_NO_Wall_Window_Upper_4x4.fbx");
            var balcony = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_NO_Balcony_4m.fbx");
            var glow = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Window glow.mat");
            if(!wall || !win || !upWall || !upWin || !balcony || !glow) throw new System.InvalidOperationException("Facade models missing in " + Models);
            foreach(var t in Named("Townhouse"))
            {
                Vector3 p = t.position, sc = t.localScale; float depth = sc.z, sgn = Mathf.Sign(p.x), faceX = p.x - sgn * sc.x / 2;
                // the kit windows are recessed into the wall, so the box face must sit behind the kit's back face: swap the box renderer for a collider-free shell 0.32 m shallower on the street side
                if(t.GetComponent<MeshRenderer>() is MeshRenderer tr)
                {
                    tr.enabled = false; hidden.Add(tr);
                    var shell = GameObject.CreatePrimitive(PrimitiveType.Cube); shell.name = "Kit - Townhouse shell";
                    Object.DestroyImmediate(shell.GetComponent<Collider>());
                    shell.GetComponent<MeshRenderer>().sharedMaterial = tr.sharedMaterial;
                    shell.transform.SetParent(root, false);
                    shell.transform.localScale = new Vector3(sc.x - .32f, sc.y, sc.z); shell.transform.position = p + new Vector3(sgn * .16f, 0, 0);
                }
                int n = Mathf.Max(1, Mathf.RoundToInt(depth / 4)), floors = Mathf.RoundToInt(sc.y / 4); float w = depth / n, bottom = p.y - sc.y / 2;
                for(int s = 0; s < floors; s++)
                    for(int i = 0; i < n; i++)
                    {
                        bool window = s > 0 || i % 2 == 1;
                        var pos = new Vector3(faceX - sgn * .02f, bottom + 4 * s, p.z - depth / 2 + (i + .5f) * w); var rot = Quaternion.Euler(0, p.x < 0 ? 90 : -90, 0);
                        var g = Place(window ? (s > 0 ? upWin : win) : (s > 0 ? upWall : wall), pos, rot, window ? "Kit - Facade window" : "Kit - Facade wall", root);
                        g.transform.localScale = new Vector3(w / 4, 1, 1); mods++;
                        if(window && (i * 7 + s * 3 + b * 5) % 5 < 2)
                        {
                            foreach(var r in g.GetComponentsInChildren<Renderer>())
                            {
                                var ms = r.sharedMaterials;
                                for(int k = 0; k < ms.Length; k++) if(ms[k] && ms[k].name == "Window pane") ms[k] = glow;
                                r.sharedMaterials = ms;
                            }
                            g.name = "Kit - Facade window (lit)"; lit++;
                        }
                        if(s == 1) { Place(balcony, pos, rot, "Kit - Balcony", root).transform.localScale = new Vector3(w / 4, 1, 1); balconies++; }
                    }
                foreach(var o in Named("Balcony").Concat(Named("Iron railing")).Concat(Named("Shutter window")).Where(o => Mathf.Abs(o.position.x - faceX) < (o.name == "Shutter window" ? .5f : 1.5f) && Mathf.Abs(o.position.z - p.z) < depth / 2 + .5f))
                    if(o.GetComponent<MeshRenderer>() is MeshRenderer r) { r.enabled = false; hidden.Add(r); }
                facades++; b++;
            }
            // Shop: fascia + iron columns over the open storefront (x = -7, z 14..30); the header box keeps its collider, the sign is flipped to face the street
            var fascia = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_NO_ShopFascia_4m.fbx");
            var column = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_NO_ShopColumn.fbx");
            var signBoard = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_NO_SignBoard.fbx");
            var fence = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_NO_IronFence_4m.fbx");
            var pier = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_NO_FencePier.fbx");
            var gate = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_NO_CemeteryGate.fbx");
            if(!fascia || !column || !signBoard || !fence || !pier || !gate) throw new System.InvalidOperationException("Shop/cemetery models missing in " + Models);
            int fascias = 0, columns = 0, segs = 0, piers = 0, gates = 0;
            foreach(var h in Named("Shop header")) if(h.GetComponent<MeshRenderer>() is MeshRenderer hr) { hr.enabled = false; hidden.Add(hr); }
            foreach(var zc in new[] { 16f, 20f, 24f, 28f }) { Place(fascia, new Vector3(-6.8f, 0, zc), Quaternion.Euler(0, 90, 0), "Kit - Shop fascia", root); fascias++; }
            foreach(var z in new[] { 14.35f, 18f, 26f, 29.65f }) { Place(column, new Vector3(-7f, 0, z), Quaternion.identity, "Kit - Shop column", root); columns++; }
            // TMP text reads correctly from its -forward side; Euler(0,-90,0) makes that the street (+X)
            var shopSign = all.FirstOrDefault(t => t.name.StartsWith("Sign - MERCER") && t.name.Contains("ANTIQUES"));
            if(shopSign)
            {
                shopSign.rotation = Quaternion.Euler(0, -90, 0);
                Place(signBoard, new Vector3(shopSign.position.x - .04f, shopSign.position.y, shopSign.position.z), Quaternion.Euler(0, 90, 0), "Kit - Shop sign board", root);
            }
            // Cemetery: gate (pier boxes only, the opening stays passable), fence runs with a collider per segment/pier, piers at run vertices except at the gate
            var gg = Place(gate, new Vector3(10.3f, 0, 35), Quaternion.Euler(0, 180, 0), "Kit - Cemetery gate", root); gates++;
            var gm = gg.GetComponentInChildren<MeshFilter>().gameObject;
            foreach(var sx in new[] { -1f, 1f }) { var bc = gm.AddComponent<BoxCollider>(); bc.center = new Vector3(sx * 1.85f, 1.5f, 0); bc.size = new Vector3(.7f, 3f, .7f); }
            var pierPos = new List<Vector3>();
            foreach(var (a, c) in new[] { (new Vector3(8, 0, 35), new Vector3(8, 0, 53)), (new Vector3(8, 0, 53), new Vector3(21.5f, 0, 53)), (new Vector3(21.5f, 0, 53), new Vector3(21.5f, 0, 35)), (new Vector3(21.5f, 0, 35), new Vector3(12.5f, 0, 35)) })
            {
                var d = c - a; float len = d.magnitude; int n = Mathf.Max(1, Mathf.RoundToInt(len / 4));
                for(int i = 0; i < n; i++)
                {
                    var g = Place(fence, a + (i + .5f) / n * d, Quaternion.Euler(0, Mathf.Atan2(-d.z, d.x) * Mathf.Rad2Deg, 0), "Kit - Cemetery fence", root);
                    g.transform.localScale = new Vector3(len / n / 4, 1, 1); g.GetComponentInChildren<MeshFilter>().gameObject.AddComponent<BoxCollider>(); segs++;
                }
                for(int k = 0; k <= n; k++)
                {
                    var q = a + (float)k / n * d;
                    if(pierPos.Any(o => (o - q).magnitude < .1f)) continue;
                    // distance to the gate opening segment x 8.05..12.55 on z = 35
                    if(Vector2.Distance(new Vector2(q.x, q.z), new Vector2(Mathf.Clamp(q.x, 8.05f, 12.55f), 35)) < .6f) continue;
                    pierPos.Add(q);
                    Place(pier, q, Quaternion.identity, "Kit - Fence pier", root).GetComponentInChildren<MeshFilter>().gameObject.AddComponent<BoxCollider>(); piers++;
                }
            }
            var cemSign = all.FirstOrDefault(t => t.name.StartsWith("Sign - ST. MARAIS"));
            if(cemSign)
            {
                // A root RectTransform saves anchoredPosition, so set that (Transform.position does not persist); z 34.55 clears the gate piers' faces.
                var rt = (RectTransform)cemSign; rt.anchoredPosition3D = new Vector3(10.3f, 3.42f, 34.55f); rt.rotation = Quaternion.identity; EditorUtility.SetDirty(rt);
                if(cemSign.GetComponent<TMPro.TMP_Text>() is TMPro.TMP_Text tmp) tmp.fontSize = 4;
            }
            // Boundary walls and cellar: brick modules on the inner face of the greybox wall boxes (the boxes keep their colliders), brick piers at the cellar entrance, stone floor
            var brick6 = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_NO_BrickWall_4x6.fbx");
            var brick4 = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_NO_BrickWall_4x4.fbx");
            var brickPier = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_NO_BrickPier.fbx");
            var cellarStone = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Cellar stone.mat");
            if(!brick6 || !brick4 || !brickPier || !cellarStone) throw new System.InvalidOperationException("Brick wall/pier models or Cellar stone material missing");
            int boundary = 0, cellar = 0, cellarPiers = 0;
            foreach(var n in new[] { "West boundary", "East boundary", "South boundary", "North boundary" })
                foreach(var t in Named(n)) boundary += Line(t, new Vector2(0, 34), brick6, brick4, "Kit - Boundary wall", root, hidden);
            foreach(var n in new[] { "Cellar left", "Cellar right", "Cellar back" })
                foreach(var t in Named(n)) cellar += Line(t, new Vector2(0, 64), brick6, brick4, "Kit - Cellar wall", root, hidden);
            foreach(var x in new[] { -6.5f, 6.5f })
            {
                Place(brickPier, new Vector3(x, 0, 56), Quaternion.identity, "Kit - Cellar pier", root).GetComponentInChildren<MeshFilter>().gameObject.AddComponent<BoxCollider>(); cellarPiers++;
            }
            foreach(var f in Named("Cellar floor")) if(f.GetComponent<MeshRenderer>() is MeshRenderer fr) fr.sharedMaterial = cellarStone;
            // Cellar props: arched wall niches with a candle fill light, hanging chains, floor candelabras and old crates (all inside the cellar, clear of the ritual zone and entrance corridor)
            var niche = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_NO_WallNiche.fbx");
            var chain = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_NO_Chain.fbx");
            var candelabra = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_NO_Candelabra.fbx");
            var crate = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_NO_Crate.fbx");
            var candleCluster = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_NO_CandleCluster.fbx");
            var chainIron = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Chain iron.mat");
            if(!niche || !chain || !candelabra || !crate || !candleCluster || !chainIron) throw new System.InvalidOperationException("Cellar prop models missing in " + Models);
            int niches = 0, chains = 0, candelabras = 0, crates = 0, crateCandles = 0, lights = 0;
            // BoxCollider fitted to the mesh bounds of the MeshFilter object
            void Box(GameObject g) { var mf = g.GetComponentInChildren<MeshFilter>(); var bc = mf.gameObject.AddComponent<BoxCollider>(); bc.center = mf.sharedMesh.bounds.center; bc.size = mf.sharedMesh.bounds.size; }
            Light Candle(GameObject parent, string name, Vector3 local, float intensity, float range)
            {
                var l = new GameObject(name).AddComponent<Light>(); l.transform.SetParent(parent.transform, false); l.transform.localPosition = local;
                l.type = LightType.Point; l.color = new Color(1f, .55f, .25f); l.intensity = intensity; l.range = range; HdrpAuthoring.Light(l); lights++; return l;
            }
            foreach(var (x, z, y) in new[] { (-6.22f, 62f, 90f), (-6.22f, 70f, 90f), (6.22f, 62f, -90f), (6.22f, 66f, -90f), (-4.2f, 71.77f, 180f), (4.2f, 71.77f, 180f) })
            {
                var g = Place(niche, new Vector3(x, .6f, z), Quaternion.Euler(0, y, 0), "Kit - Cellar niche", root);
                Candle(g, "Niche candle", new Vector3(0, .35f, .25f), .8f, 3.5f).shadows = LightShadows.None; niches++;
            }
            // chains hang next to the back-wall niches so their candles light them; the brighter metal reads against the brick
            foreach(var x in new[] { -2.9f, 2.9f }) { Place(chain, new Vector3(x, 2.6f, 71.77f), Quaternion.Euler(0, 180, 0), "Kit - Cellar chain", root).GetComponentInChildren<MeshFilter>().GetComponent<MeshRenderer>().sharedMaterial = chainIron; chains++; }
            foreach(var (x, z) in new[] { (-4.5f, 61.5f), (4.5f, 61.5f), (-4.5f, 69.5f), (4.5f, 69.5f) })
            {
                var g = Place(candelabra, new Vector3(x, .095f, z), Quaternion.identity, "Kit - Candelabra", root);
                Box(g); Candle(g, "Candelabra candle", new Vector3(0, 1.45f, 0), 1.5f, 5f); candelabras++;
            }
            foreach(var (x, y, z, r) in new[] { (-5.5f, .095f, 57.3f, 10f), (-5.5f, .795f, 57.3f, 35f), (-5.4f, .095f, 58.3f, -8f), (5.5f, .095f, 71f, 5f), (5.5f, .795f, 71f, -20f), (4.6f, .095f, 71.1f, 15f) })
            { Box(Place(crate, new Vector3(x, y, z), Quaternion.Euler(0, r, 0), "Kit - Cellar crate", root)); crates++; }
            // candle on the top crate of the entrance stack (crate top = 0.795 + 0.72)
            Candle(Place(candleCluster, new Vector3(-5.5f, .795f + .72f, 57.3f), Quaternion.Euler(0, 0, 0), "Kit - Crate candle", root), "Crate candle light", new Vector3(0, .3f, 0), .9f, 3.5f); crateCandles++;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            string log = "Kit swap: " + lamps + " lamps, " + tombs + " tombs, " + facades + " facades (" + mods + " modules, " + balconies + " balconies, " + lit + " lit windows) placed, " + hidden.Count + " renderers disabled; shop " + fascias + " fascia + " + columns + " columns; cemetery " + segs + " fence segments + " + piers + " piers + " + gates + " gate; " + boundary + " boundary + " + cellar + " cellar wall modules, " + cellarPiers + " cellar piers; props " + niches + " niches, " + chains + " chains, " + candelabras + " candelabras, " + crates + " crates, " + crateCandles + " crate candle, " + lights + " lights";
            Debug.Log(log);
            return log;
        }

        /// <summary>Covers the `inside` side of the greybox wall box `box` with brick modules (module6 for boxes 5 m or taller, else module4), 0.02 m off the face, fronts toward `inside`; returns the module count.</summary>
        static int Line(Transform box, Vector2 inside, GameObject module6, GameObject module4, string name, Transform root, HashSet<Renderer> hidden)
        {
            Vector3 p = box.position, sc = box.lossyScale;
            bool thinX = sc.x < sc.z; float len = thinX ? sc.z : sc.x, bottom = p.y - sc.y / 2;
            float sgn = Mathf.Sign(thinX ? inside.x - p.x : inside.y - p.z);
            var module = sc.y >= 5 ? module6 : module4;
            int n = Mathf.Max(1, Mathf.RoundToInt(len / 4)); float w = len / n;
            var rot = Quaternion.Euler(0, thinX ? sgn * 90 : (sgn > 0 ? 0 : 180), 0);
            for(int i = 0; i < n; i++)
            {
                float along = (i + .5f) * w - len / 2;
                var pos = thinX ? new Vector3(p.x + sgn * (sc.x / 2 + .02f), bottom, p.z + along) : new Vector3(p.x + along, bottom, p.z + sgn * (sc.z / 2 + .02f));
                Place(module, pos, rot, name, root).transform.localScale = new Vector3(w / 4, 1, 1);
            }
            if(box.GetComponent<MeshRenderer>() is MeshRenderer r) { r.enabled = false; hidden.Add(r); }
            return n;
        }

        static float XZ(Transform a, Transform b) => Vector2.Distance(new Vector2(a.position.x, a.position.z), new Vector2(b.position.x, b.position.z));

        static GameObject Place(GameObject prefab, Vector3 pos, Quaternion rot, string name, Transform parent)
        {
            var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            g.name = name; g.transform.SetParent(parent, false); g.transform.SetPositionAndRotation(pos, rot);
            return g;
        }
    }
}
