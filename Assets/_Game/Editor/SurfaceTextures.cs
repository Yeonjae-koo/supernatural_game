using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
namespace BeforeJericho.Editor
{
    /// <summary>Puts CC0 Poly Haven PBR sets (Art/Textures/&lt;id&gt;/, licences in Art/Textures/POLYHAVEN_LICENSE.txt) on the greybox materials.
    /// World-space triplanar mapping, so scaled primitive cubes need no UVs. Each material keeps its authored average colour
    /// (base colour = original / texture mean), so the fixed-exposure lighting reads the same, only with surface detail. Re-runnable.</summary>
    public static class SurfaceTextures
    {
        const string Materials = "Assets/_Game/Art/Materials/", Textures = "Assets/_Game/Art/Textures/";

        // material → (Poly Haven id, metres per texture repeat, wet = glossy rain-soaked surface). Stanford "Steel" is mostly the sidewalk, hence concrete. Emissive, marker, paper, salt, water and actor-placeholder materials stay flat.
        static readonly (string mat, string id, float metres, bool wet)[] Map =
        {
            ("Rain dark asphalt", "asphalt_02", 4, true), ("Wet limestone", "cobblestone_floor_04", 2.5f, true), ("Aged stucco", "worn_cracked_plaster", 3, false),
            ("Tomb marble", "marble_01", 2, false), ("Walnut", "dark_wooden_planks", 2, false), ("Wrought iron", "rusty_metal_02", 2, false),
            ("Atchafalaya/CH02 Concrete", "concrete_floor_worn_001", 3, true), ("Atchafalaya/CH02 Cypress", "weathered_planks", 2, false),
            ("Atchafalaya/CH02 Iron", "rusty_metal_02", 2, false), ("Atchafalaya/CH02 Mud", "brown_mud_03", 3, true),
            ("EastTexas/CH03 Asphalt", "asphalt_02", 4, true), ("EastTexas/CH03 Brick", "brick_wall_10", 2.5f, false), ("EastTexas/CH03 Coffin wood", "dark_wooden_planks", 2, false),
            ("EastTexas/CH03 Cold steel", "rusty_metal_02", 2, false), ("EastTexas/CH03 Mortuary tile", "dirty_tiles", 1.5f, false), ("EastTexas/CH03 Wet grass", "leafy_grass", 3, false),
            ("GrantsNM/CH04 Asphalt", "asphalt_02", 4, false), ("GrantsNM/CH04 Desert sand", "dry_ground_01", 4, false), ("GrantsNM/CH04 Faded stucco", "clay_plaster", 3, false),
            ("GrantsNM/CH04 Laundry tile", "dirty_tiles", 1.5f, false), ("GrantsNM/CH04 Motel wood", "dark_wooden_planks", 2, false), ("GrantsNM/CH04 Utility steel", "rusty_metal_02", 2, false),
            ("Winslow/CH05 Desert", "dry_ground_01", 4, false), ("Winslow/CH05 Route 66 asphalt", "asphalt_02", 4, false), ("Winslow/CH05 Rusted wreck", "rusty_metal_02", 2, false),
            ("Winslow/CH05 Steel", "rusty_metal_02", 2, false), ("Winslow/CH05 Sunbaked stucco", "clay_plaster", 3, false), ("Winslow/CH05 Weathered wood", "weathered_planks", 2, false),
            ("Stanford/CH06 Apartment brick", "brick_wall_10", 2.5f, false), ("Stanford/CH06 Lawn", "leafy_grass", 3, false), ("Stanford/CH06 Motel stucco", "worn_cracked_plaster", 3, false),
            ("Stanford/CH06 Steel", "concrete_floor_worn_001", 3, false), ("Stanford/CH06 Street asphalt", "asphalt_02", 4, false), ("Stanford/CH06 Wood", "dark_wooden_planks", 2, false),
        };

        [MenuItem("Before Jericho/Art/Apply Surface Textures")]
        public static string Run()
        {
            var log = new System.Text.StringBuilder();
            var means = new System.Collections.Generic.Dictionary<string, Color>();
            foreach(var (mat, id, metres, wet) in Map)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(Materials + mat + ".mat");
                string dir = Textures + id + "/";
                if(!m || !File.Exists(dir + id + "_diff_2k.jpg")) { log.AppendLine("skip " + mat + (m ? " (no textures " + id + ")" : " (no material)")); continue; }
                if(!means.ContainsKey(id)) means[id] = Prepare(dir, id);
                // On a re-run the tint is already original / mean of the texture it was made for (possibly a since-swapped one),
                // so multiply back to the authored flat colour instead of dividing twice.
                Color mean = means[id], current = m.GetColor("_BaseColor").linear, flat = current;
                if(m.GetTexture("_BaseColorMap") is Texture old)
                {
                    string oldDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(old)) + "/", oldId = Path.GetFileName(oldDir.TrimEnd('/'));
                    if(!means.ContainsKey(oldId)) means[oldId] = Prepare(oldDir, oldId);
                    flat = current * means[oldId];
                }
                var tint = new Color(Mathf.Min(1, flat.r / mean.r), Mathf.Min(1, flat.g / mean.g), Mathf.Min(1, flat.b / mean.b)).gamma;
                m.SetColor("_BaseColor", tint);
                m.SetTexture("_BaseColorMap", AssetDatabase.LoadAssetAtPath<Texture2D>(dir + id + "_diff_2k.jpg"));
                m.SetTexture("_NormalMap", AssetDatabase.LoadAssetAtPath<Texture2D>(dir + id + "_nor_gl_2k.jpg")); m.SetFloat("_NormalScale", 1);
                m.SetTexture("_MaskMap", AssetDatabase.LoadAssetAtPath<Texture2D>(dir + id + "_mask_2k.png"));
                // Rain-soaked surfaces stay glossy enough for SSR; the rest use the texture's own roughness.
                m.SetFloat("_SmoothnessRemapMin", wet ? .45f : 0); m.SetFloat("_SmoothnessRemapMax", wet ? .95f : .8f);
                m.SetFloat("_MetallicRemapMax", 1); m.SetFloat("_AORemapMin", 0); m.SetFloat("_AORemapMax", 1);
                m.SetFloat("_UVBase", 5); m.SetFloat("_TexWorldScale", 1 / metres); // 5 = HDRP's internal UVBaseMapping.Triplanar
                HDMaterial.ValidateMaterial(m); EditorUtility.SetDirty(m);
                log.AppendLine(mat + " ← " + id + " tint " + tint);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Surface textures\n" + log);
            return log.ToString();
        }

        /// <summary>Import settings, an HDRP mask map packed from Poly Haven's ARM (R=metal, G=AO, A=1-rough), and the diffuse mean in linear space.</summary>
        static Color Prepare(string dir, string id)
        {
            Import(dir + id + "_diff_2k.jpg", TextureImporterType.Default, true);
            Import(dir + id + "_nor_gl_2k.jpg", TextureImporterType.NormalMap, false);
            string mask = dir + id + "_mask_2k.png";
            if(!File.Exists(mask))
            {
                var arm = Load(dir + id + "_arm_2k.jpg", true); var px = arm.GetPixels();
                for(int i = 0; i < px.Length; i++) { var a = px[i]; px[i] = new Color(a.b, a.r, 0, 1 - a.g); }
                var packed = new Texture2D(arm.width, arm.height, TextureFormat.RGBA32, false, true); packed.SetPixels(px);
                File.WriteAllBytes(mask, packed.EncodeToPNG()); Object.DestroyImmediate(arm); Object.DestroyImmediate(packed);
                AssetDatabase.ImportAsset(mask);
            }
            Import(mask, TextureImporterType.Default, false);
            var diff = Load(dir + id + "_diff_2k.jpg", false); var d = diff.GetPixels(); Color sum = Color.black;
            for(int i = 0; i < d.Length; i += 7) sum += d[i].linear; // every 7th texel is plenty for a mean
            Object.DestroyImmediate(diff);
            return sum / Mathf.Ceil(d.Length / 7f);
        }

        static Texture2D Load(string path, bool linear)
        { var t = new Texture2D(2, 2, TextureFormat.RGBA32, false, linear); t.LoadImage(File.ReadAllBytes(path)); return t; }

        static void Import(string path, TextureImporterType type, bool srgb)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if(importer.textureType == type && importer.sRGBTexture == srgb && importer.maxTextureSize == 2048) return;
            importer.textureType = type; importer.sRGBTexture = srgb; importer.maxTextureSize = 2048; importer.SaveAndReimport();
        }
    }
}
