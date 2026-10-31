using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace BeforeJericho.Editor
{
    /// <summary>Builds the CH03 Vale Funeral Home from the Blender kit (Art/Models/EastTexas/) over the greybox lobby: a 24.4 x 16 m clapboard shell (open entrance, open rear),
    /// cornice, 35 degree shingle roof with gables and a portico, laid out in Blender-layout coordinates under one parent and converted (Blender x,y,z -> local -x,z,-y; rot t -> yaw -t).
    /// The parent sits at world (0,0,frontZ) with yaw 180, so the facade faces the cemetery (-Z) and the depth runs north. Greybox lobby walls keep their colliders and lose their renderers;
    /// extra collider boxes cover the front wall, the kit side walls and the portico columns; an interior ceiling hides the roof underside; the sign moves onto the portico entablature.
    /// All under one "Kit Art" root. Re-runnable.</summary>
    public static class EastTexasKit
    {
        const string Models = "Assets/_Game/Art/Models/EastTexas/", ScenePath = "Assets/_Game/Scenes/SCN_EastTexas_Slice.unity";

        [MenuItem("Before Jericho/Art/Swap East Texas Kit")]
        public static string Run()
        {
            var scene = SceneManager.GetActiveScene();
            if(scene.path != ScenePath) throw new System.InvalidOperationException("EastTexasKit needs " + ScenePath + ", open scene is '" + scene.path + "'");
            var old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Kit Art");
            if(old) Object.DestroyImmediate(old);
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToList();
            Transform Need(string n) => all.FirstOrDefault(t => t.name == n) ?? throw new System.InvalidOperationException("EastTexasKit: scene object '" + n + "' missing");
            Transform floor = Need("Funeral Home Lobby floor"), west = Need("Funeral Home Lobby west"), east = Need("Funeral Home Lobby east"), sign = Need("Sign - VALE FUNERAL HOME");
            float frontZ = floor.position.z - floor.lossyScale.z / 2 - .2f, W = 24.4f, D = 16, hw = W / 2, rearZ = frontZ + D, sx = 1.22f;
            var hidden = 0;
            foreach(var t in new[] { west, east }) if(t.GetComponent<MeshRenderer>() is MeshRenderer r) { r.enabled = false; hidden++; }
            GameObject Load(string n) => AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_ET_" + n + ".fbx");
            string[] names = { "Wall_Clapboard_4x4", "Wall_Clapboard_Upper_4x4", "Wall_Clapboard_Window_4x4", "Wall_Clapboard_Window_Upper_4x4", "Wall_Clapboard_Door_4x4", "Corner_Clapboard", "Corner_Clapboard_Upper", "Cornice_4m", "Cornice_Corner", "Roof_Shingle_Span16_4m", "Roof_Shingle_Span16_GableEnd_4m", "Gable_Clapboard_16m", "Portico" };
            var m = names.ToDictionary(n => n, Load);
            var missing = m.Where(kv => !kv.Value).Select(kv => kv.Key).ToList();
            var stucco = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Aged stucco.mat");
            if(missing.Count > 0 || !stucco) throw new System.InvalidOperationException("East Texas kit models/materials missing: " + string.Join(", ", missing) + (stucco ? "" : " Aged stucco.mat"));
            var root = new GameObject("Kit Art").transform;
            var fh = new GameObject("Funeral Home").transform; fh.SetParent(root, false); fh.SetPositionAndRotation(new Vector3(0, 0, frontZ), Quaternion.Euler(0, 180, 0));
            int pieces = 0;
            // Blender-layout placement: (x,y,z) -> local (-x,z,-y), rotation about Z -> -yaw, X scale on the instance.
            void P(string n, float x, float y, float z, float rot = 0, float scale = 1)
            {
                var g = KitSwap.Place(m[n], Vector3.zero, Quaternion.identity, "Kit - " + n, fh);
                g.transform.localPosition = new Vector3(-x, z, -y); g.transform.localRotation = Quaternion.Euler(0, -rot, 0); g.transform.localScale = new Vector3(scale, 1, 1); pieces++;
            }
            float[] xs = { -2 * 4.88f, -4.88f, 0, 4.88f, 2 * 4.88f };
            string[] lowerFront = { "Wall_Clapboard_Window_4x4", "Wall_Clapboard_Window_4x4", "Wall_Clapboard_Door_4x4", "Wall_Clapboard_Window_4x4", "Wall_Clapboard_Window_4x4" };
            string[] upperRear = { "Wall_Clapboard_Upper_4x4", "Wall_Clapboard_Window_Upper_4x4", "Wall_Clapboard_Upper_4x4", "Wall_Clapboard_Window_Upper_4x4", "Wall_Clapboard_Upper_4x4" };
            for(int i = 0; i < 5; i++)
            {
                P(lowerFront[i], xs[i], 0, 0, 0, sx); P("Wall_Clapboard_Window_Upper_4x4", xs[i], 0, 4, 0, sx);
                P(upperRear[i], xs[i], D, 4, 180, sx);
                P("Cornice_4m", xs[i], 0, 8, 0, sx); P("Cornice_4m", xs[i], D, 8, 180, sx);
            }
            foreach(var (side, rot) in new[] { (hw, 90f), (-hw, -90f) })
            {
                for(int i = 0; i < 4; i++)
                {
                    bool win = i % 2 == 0; float y = 2 + 4 * i;
                    P(win ? "Wall_Clapboard_Window_4x4" : "Wall_Clapboard_4x4", side, y, 0, rot);
                    P(win ? "Wall_Clapboard_Window_Upper_4x4" : "Wall_Clapboard_Upper_4x4", side, y, 4, rot);
                }
                P("Gable_Clapboard_16m", side, D / 2, 8, rot);
            }
            foreach(var (x, y, r) in new[] { (hw, 0f, 0f), (hw, D, 90f), (-hw, D, 180f), (-hw, 0f, 270f) })
            {
                if(y == 0) P("Corner_Clapboard", x, y, 0, r);
                P("Corner_Clapboard_Upper", x, y, 4, r); P("Cornice_Corner", x, y, 8, r);
            }
            P("Roof_Shingle_Span16_GableEnd_4m", xs[0], D / 2, 8, 0, sx); P("Roof_Shingle_Span16_GableEnd_4m", xs[4], D / 2, 8, 180, sx);
            for(int i = 1; i < 4; i++) P("Roof_Shingle_Span16_4m", xs[i], D / 2, 8, 0, sx);
            P("Portico", 0, 0, 0);
            // Colliders (same layer as the greybox boundary boxes): front wall split around the door gap, kit side walls, portico columns.
            int layer = west.gameObject.layer, boxes = 0;
            void Col(string n, Vector3 centre, Vector3 size)
            {
                var g = new GameObject(n); g.layer = layer; g.transform.SetParent(root, false); g.transform.position = centre; g.AddComponent<BoxCollider>().size = size; boxes++;
            }
            float fw = hw - .61f, fx = .61f + fw / 2;
            foreach(var s in new[] { -1f, 1f })
            {
                Col("Kit - Front wall collider", new Vector3(s * fx, 2, frontZ + .1f), new Vector3(fw, 4, .2f));
                Col("Kit - Side wall collider", new Vector3(s * (hw - .1f), 2, frontZ + D / 2), new Vector3(.2f, 4, D));
                Col("Kit - Portico column collider", new Vector3(s * 2, 1.525f, frontZ - 2.4f), new Vector3(.4f, 3.05f, .4f));
            }
            // Interior ceiling (no collider): top at y 4.0, hides the roof underside from inside.
            float cz0 = floor.position.z - floor.lossyScale.z / 2, cz1 = rearZ;
            var ceil = GameObject.CreatePrimitive(PrimitiveType.Cube); ceil.name = "Kit - Lobby ceiling";
            Object.DestroyImmediate(ceil.GetComponent<Collider>()); ceil.GetComponent<MeshRenderer>().sharedMaterial = stucco;
            ceil.transform.SetParent(root, false); ceil.transform.position = new Vector3(0, 3.975f, (cz0 + cz1) / 2); ceil.transform.localScale = new Vector3(24, .05f, cz1 - cz0);
            // Sign on the front face of the portico entablature (blender y -2.6, z 3.05..3.40, x +-2.2), proud by 1 cm; scale recomputed from the base each run to fit 4.2 x 0.3 m.
            var tmp = sign.GetComponent<TMPro.TMP_Text>(); var rt = (RectTransform)sign;
            sign.localScale = Vector3.one * .09f; rt.rotation = Quaternion.identity; tmp.ForceMeshUpdate();
            var b = tmp.textBounds; float fit = Mathf.Min(1, 4.2f / (b.size.x * .09f), .3f / (b.size.y * .09f));
            sign.localScale = Vector3.one * .09f * fit; rt.anchoredPosition3D = new Vector3(0, 3.225f, frontZ - 2.61f); EditorUtility.SetDirty(rt);
            // The builder's porch light sat inside the portico ceiling slab (3.06..3.30); hang it just under the ceiling, mid-portico.
            var porch = Need("Funeral porch light"); porch.position = new Vector3(0, 2.85f, frontZ - 1.3f); EditorUtility.SetDirty(porch);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            string log = "East Texas kit: " + pieces + " pieces under Funeral Home (frontZ " + frontZ + ", rear " + rearZ + "), " + boxes + " colliders, 1 ceiling, " + hidden + " renderers disabled, sign scale x" + fit.ToString("0.###") + " (text " + (b.size.x * .09f).ToString("0.##") + " x " + (b.size.y * .09f).ToString("0.##") + " m)";
            Debug.Log(log);
            return log;
        }
    }
}
