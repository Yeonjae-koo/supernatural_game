using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
namespace BeforeJericho.Editor
{
    /// <summary>Builds the CH03 Vale Funeral Home from the Blender kit (Art/Models/EastTexas/) over the greybox lobby: a 24.4 x 16 m clapboard shell (open entrance, open rear),
    /// cornice, 35 degree shingle roof with gables and a portico, laid out in Blender-layout coordinates under one parent and converted (Blender x,y,z -> local -x,z,-y; rot t -> yaw -t).
    /// The parent sits at world (0,0,frontZ) with yaw 180, so the facade faces the cemetery (-Z) and the depth runs north. Greybox lobby walls keep their colliders and lose their renderers;
    /// extra collider boxes cover the front wall, the kit side walls and the portico columns; an interior ceiling hides the roof underside; the sign moves onto the portico entablature.
    /// All under one "Kit Art" root. Re-runnable. Also builds "Main Street": a diner on the west side and a block service building on the east side, from the phase-3 CMU/diner pieces.</summary>
    public static class EastTexasKit
    {
        const string Models = "Assets/_Game/Art/Models/EastTexas/", ScenePath = "Assets/_Game/Scenes/SCN_EastTexas_Slice.unity";

        const string MatDir = "Assets/_Game/Art/Materials/EastTexas/", TexDir = "Assets/_Game/Art/Textures/Generated/";

        /// <summary>Phase-3 materials (matched by name in KitModelImport): painted CMU block (world-space triplanar, 4.8 m per texture repeat, V clamped so grime stays low) and plain red enamel / stainless / safety yellow. Re-runnable.</summary>
        public static void EnsureMaterials()
        {
            void Tex(string n, TextureImporterType type, bool srgb)
            {
                var im = (TextureImporter)AssetImporter.GetAtPath(TexDir + n);
                if(im == null) throw new System.InvalidOperationException("Missing " + TexDir + n + " (run Tools/gen_block_texture.py)");
                im.textureType = type; im.sRGBTexture = srgb; im.wrapModeU = TextureWrapMode.Repeat; im.wrapModeV = TextureWrapMode.Clamp;
                im.npotScale = TextureImporterNPOTScale.None; im.anisoLevel = 9; im.maxTextureSize = 2048; im.SaveAndReimport();
            }
            Tex("T_BlockPaint.png", TextureImporterType.Default, true); Tex("T_BlockPaint_Mask.png", TextureImporterType.Default, false); Tex("T_BlockPaint_Normal.png", TextureImporterType.NormalMap, false);
            Material Get(string n)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(MatDir + n + ".mat");
                if(!m) { m = new Material(Shader.Find("HDRP/Lit")) { name = n }; AssetDatabase.CreateAsset(m, MatDir + n + ".mat"); }
                return m;
            }
            void Plain(string n, Color c, float metal, float smooth)
            {
                var m = Get(n); m.SetColor("_BaseColor", c); m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", smooth); HDMaterial.ValidateMaterial(m); EditorUtility.SetDirty(m);
            }
            var b = Get("CH03 Block");
            b.SetColor("_BaseColor", Color.white);
            b.SetTexture("_BaseColorMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "T_BlockPaint.png"));
            b.SetTexture("_MaskMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "T_BlockPaint_Mask.png"));
            b.SetTexture("_NormalMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "T_BlockPaint_Normal.png")); b.SetFloat("_NormalScale", 1);
            b.SetFloat("_SmoothnessRemapMin", 0); b.SetFloat("_SmoothnessRemapMax", .6f); b.SetFloat("_AORemapMin", 0); b.SetFloat("_AORemapMax", 1);
            b.SetFloat("_UVBase", 5); b.SetFloat("_TexWorldScale", 1 / 4.8f); // 5 = triplanar, world space: 1 repeat = 4.8 m (12 blocks x 24 courses)
            HDMaterial.ValidateMaterial(b); EditorUtility.SetDirty(b);
            Plain("CH03 Diner red", new Color(.60f, .09f, .08f), 0, .45f);
            Plain("CH03 Stainless", new Color(.80f, .81f, .83f), 1, .55f);
            Plain("CH03 Safety yellow", new Color(.92f, .72f, .04f), 0, .4f);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Main Street (Blender-layout coordinates under a per-building parent: world X = R.x -/+ y, Z = R.z +/- x; see the Diner / Service Building roots).
        /// West diner: front plane world x=-9 facing +X, z 0..16, depth 10 (x -19..-9). East service building: front plane x=+9 facing -X, z -4..20, depth 8 (x 9..17).
        /// Colliders cover each footprint (same layer as the town boundaries); flat dark roof slabs without colliders close the tops.</summary>
        static string MainStreet(Transform root, int layer)
        {
            string[] names = { "Wall_Block_4x4", "Wall_Block_Door_4x4", "Wall_Block_Utility_4x4", "Wall_RollUpDoor_4x4", "Block_Pilaster", "Block_Pilaster_Parapet", "Parapet_Band_4m", "Diner_Front_4m", "Diner_Corner" };
            var m = names.ToDictionary(n => n, n => AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SM_ET_" + n + ".fbx"));
            var missing = m.Where(kv => !kv.Value).Select(kv => kv.Key).ToList();
            var roofMat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "CH03 Asphalt.mat");
            if(missing.Count > 0 || !roofMat) throw new System.InvalidOperationException("Main Street models/materials missing: " + string.Join(", ", missing) + (roofMat ? "" : " CH03 Asphalt.mat"));
            var street = new GameObject("Main Street").transform; street.SetParent(root, false);
            int pieces = 0;
            Transform Building(string n, Vector3 pos, float yaw)
            { var t = new GameObject(n).transform; t.SetParent(street, false); t.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0)); return t; }
            // Blender-layout placement under the building parent: (x,y,z) -> local (-x,z,-y), rotation about Z -> -yaw.
            void P(Transform b, string n, float x, float y, float z, float rot = 0)
            {
                var g = KitSwap.Place(m[n], Vector3.zero, Quaternion.identity, "Kit - " + n, b);
                g.transform.localPosition = new Vector3(-x, z, -y); g.transform.localRotation = Quaternion.Euler(0, -rot, 0); pieces++;
            }
            void Wall(Transform b, string kind, float x, float y, float rot) { P(b, kind, x, y, 0, rot); P(b, "Parapet_Band_4m", x, y, 4, rot); }
            void Pilaster(Transform b, float x, float y, float rot) { P(b, "Block_Pilaster", x, y, 0, rot); P(b, "Block_Pilaster_Parapet", x, y, 4, rot); }
            void Solid(string n, Vector3 centre, Vector3 size)
            {
                var g = new GameObject(n); g.layer = layer; g.transform.SetParent(street, false); g.transform.position = centre; g.AddComponent<BoxCollider>().size = size;
            }
            void Roof(string n, Vector3 centre, Vector3 size, PrimitiveType shape = PrimitiveType.Cube)
            {
                var r = GameObject.CreatePrimitive(shape); r.name = n; Object.DestroyImmediate(r.GetComponent<Collider>()); r.GetComponent<MeshRenderer>().sharedMaterial = roofMat;
                r.transform.SetParent(street, false); r.transform.position = centre; r.transform.localScale = size;
            }
            // Diner: layout front along x 0..16 (world z 0..16), depth y 0..10 (world x -9..-19); parent yaw +90.
            var d = Building("Diner", new Vector3(-9, 0, 0), 90);
            for(int i = 0; i < 3; i++) P(d, "Diner_Front_4m", 4 + 4 * i, 0, 0);
            P(d, "Diner_Corner", 16, 0, 0, 0); P(d, "Diner_Corner", 0, 0, 0, 270);
            foreach(var (x, rot) in new[] { (16f, 90f), (0f, 270f) }) { Wall(d, "Wall_Block_4x4", x, 4, rot); Wall(d, "Wall_Block_Utility_4x4", x, 8, rot); }
            string[] rear = { "Wall_Block_4x4", "Wall_Block_Door_4x4", "Wall_Block_4x4", "Wall_Block_4x4" };
            for(int i = 0; i < 4; i++) Wall(d, rear[i], 2 + 4 * i, 10, 180);
            Pilaster(d, 0, 10, 180); Pilaster(d, 16, 10, 90);
            Solid("Kit - Diner collider", new Vector3(-13.875f, 2.3f, 8), new Vector3(10.25f, 4.6f, 16));
            // Roof slabs stay inside the shell (inset past the 20 cm walls, no coplanar faces): the rounded corners (arc radius 1.75 inside) get cylinders instead of square corners.
            Roof("Kit - Diner roof (rear)", new Vector3(-14.75f, 3.93f, 8), new Vector3(7.9f, .14f, 15.4f));
            Roof("Kit - Diner roof (front)", new Vector3(-10.15f, 3.93f, 8), new Vector3(1.7f, .14f, 12));
            foreach(var z in new[] { 2f, 14f }) Roof("Kit - Diner roof (corner)", new Vector3(-11, 3.93f, z), new Vector3(3.4f, .07f, 3.4f), PrimitiveType.Cylinder);
            // Service building: layout front along x 0..24 runs north->south (world z 20..-4), depth y 0..8 (world x 9..17); parent yaw -90.
            var e = Building("Service Building", new Vector3(9, 0, 20), -90);
            string[] front = { "Wall_Block_4x4", "Wall_Block_Door_4x4", "Wall_RollUpDoor_4x4", "Wall_RollUpDoor_4x4", "Wall_Block_Utility_4x4", "Wall_Block_4x4" };
            for(int i = 0; i < 6; i++) { Wall(e, front[i], 2 + 4 * i, 0, 0); Wall(e, "Wall_Block_4x4", 2 + 4 * i, 8, 180); }
            foreach(var (x, rot) in new[] { (24f, 90f), (0f, 270f) }) { Wall(e, "Wall_Block_4x4", x, 2, rot); Wall(e, "Wall_Block_4x4", x, 6, rot); }
            Pilaster(e, 24, 0, 0); Pilaster(e, 0, 0, 270); Pilaster(e, 24, 8, 90); Pilaster(e, 0, 8, 180);
            Solid("Kit - Service building collider", new Vector3(12.9f, 2.3f, 8), new Vector3(8.7f, 4.6f, 24.6f));
            Roof("Kit - Service building roof", new Vector3(13, 3.93f, 8), new Vector3(7.4f, .14f, 23.4f));
            return "Main Street " + pieces + " pieces";
        }

        [MenuItem("Before Jericho/Art/Swap East Texas Kit")]
        public static string Run()
        {
            EnsureMaterials();
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
            string street = MainStreet(root, layer);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            string log = "East Texas kit: " + street + "; " + pieces + " pieces under Funeral Home (frontZ " + frontZ + ", rear " + rearZ + "), " + boxes + " colliders, 1 ceiling, " + hidden + " renderers disabled, sign scale x" + fit.ToString("0.###") + " (text " + (b.size.x * .09f).ToString("0.##") + " x " + (b.size.y * .09f).ToString("0.##") + " m)";
            Debug.Log(log);
            return log;
        }
    }
}
