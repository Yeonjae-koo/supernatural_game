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
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            string log = "Kit swap: " + lamps + " lamps, " + tombs + " tombs, " + facades + " facades (" + mods + " modules, " + balconies + " balconies, " + lit + " lit windows) placed, " + hidden.Count + " renderers disabled";
            Debug.Log(log);
            return log;
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
