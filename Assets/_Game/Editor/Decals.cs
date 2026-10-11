using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
namespace BeforeJericho.Editor
{
    /// <summary>HDRP decal helpers for the kit passes. Source images are ambientCG (CC0) sets in Art/Textures/Decals/&lt;id&gt;/ (licence list: Art/Textures/Decals/AMBIENTCG_LICENSE.txt).
    /// <see cref="Get"/> packs each set into a base colour (RGB = Color, A = Opacity) and a linear mask map (R metal 0, G AO 1, B opacity, A smoothness) and builds an "HDRP/Decal" material under Art/Materials/Decals/;
    /// <see cref="Place"/> adds a DecalProjector whose box is centred on the surface point and projects along its local +Z.</summary>
    public static class Decals
    {
        const string Dir = "Assets/_Game/Art/Textures/Decals/", MatDir = "Assets/_Game/Art/Materials/Decals/";

        // Per-set look, shared by every scene that uses the set: smoothness scales the (1 - roughness) mask alpha, gain the opacity, tint is the material's _BaseColor (darkens the albedo the decal writes).
        // Night scenes need dark, dense stains to read; AsphaltDamage is used as a flat, satin dark wet blob, TireTracks as muddy ruts.
        static readonly System.Collections.Generic.Dictionary<string, (float smoothness, float gain, Color tint)> Look = new()
        {
            { "Leaking001", (.7f, 1.8f, new Color(.5f, .46f, .42f)) }, { "Leaking003", (.7f, 1.8f, new Color(.5f, .46f, .42f)) }, { "Leaking009", (.7f, 2.2f, new Color(.5f, .46f, .42f)) },
            { "Leaking006", (.6f, 1.6f, new Color(.55f, .5f, .45f)) },
            { "AsphaltDamage001", (.6f, 1, new Color(.22f, .22f, .22f)) }, { "TireTracks001", (.35f, 1, new Color(.35f, .3f, .25f)) }, { "ManholeCover008", (.4f, 1, Color.white) },
        };

        /// <summary>Ensures Art/Materials/Decals/&lt;id&gt;.mat exists and returns it. The packed PNGs are (re)made when one is missing or the set's <see cref="Look"/> changed (signature in the base colour importer's userData).</summary>
        public static Material Get(string id)
        {
            var (smoothness, gain, tint) = Look.TryGetValue(id, out var look) ? look : (.5f, 1f, Color.white);
            string dir = Dir + id + "/", bc = dir + id + "_Decal_BaseColor.png", mask = dir + id + "_Decal_Mask.png", matPath = MatDir + id + ".mat";
            string Src(string kind) => Directory.GetFiles(dir, id + "_*_" + kind + ".jpg").FirstOrDefault()?.Replace('\\', '/');
            string color = Src("Color"), normal = Src("NormalGL"), rough = Src("Roughness"), opacity = Src("Opacity");
            if(color == null || normal == null || rough == null) throw new FileNotFoundException("Decal set incomplete in " + dir);
            string sig = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}|{1}|v2", smoothness, gain);
            bool pack = !File.Exists(bc) || !File.Exists(mask) || AssetImporter.GetAtPath(bc)?.userData != sig;
            if(pack)
            {
                Color32[] Px(string p, out int w, out int h) { var t = new Texture2D(2, 2, TextureFormat.RGBA32, false, true); t.LoadImage(File.ReadAllBytes(p)); w = t.width; h = t.height; var px = t.GetPixels32(); Object.DestroyImmediate(t); return px; }
                var c = Px(color, out int W, out int H); var r = Px(rough, out int rw, out int rh); var o = opacity != null ? Px(opacity, out int ow, out int oh) : null;
                if(rw != W || rh != H) throw new System.InvalidOperationException(id + ": colour and roughness sizes differ");
                var cc = new Color32[c.Length]; var mm = new Color32[c.Length];
                for(int i = 0; i < c.Length; i++)
                {
                    float lum = (.299f * c[i].r + .587f * c[i].g + .114f * c[i].b) / 255f, u = (i % W + .5f) / W, v = (i / W + .5f) / H, sm = (1 - r[i].r / 255f) * smoothness, alpha;
                    // TireTracks has no opacity map and a flat colour (the ruts live in the normal map): a fixed band along the long (U) axis, faded at the ends and edges
                    if(o == null) alpha = .8f * gain * Mathf.SmoothStep(0, 1, Mathf.Min(u, 1 - u) * 8) * Mathf.SmoothStep(0, 1, Mathf.Min(v, 1 - v) * 5);
                    else alpha = o[i].r / 255f * gain;
                    // AsphaltDamage is a full square patch: break it into an irregular wet blob (radial falloff warped by its own mottling), uniformly glossy
                    if(id.StartsWith("AsphaltDamage")) { float rad = new Vector2(u - .5f, v - .5f).magnitude * 2; alpha *= Mathf.SmoothStep(1, 0, (rad - .3f + (lum - .5f) * .9f) / .45f); sm = smoothness; }
                    byte a = (byte)(Mathf.Clamp01(alpha) * 255f + .5f);
                    cc[i] = new Color32(c[i].r, c[i].g, c[i].b, a);
                    mm[i] = new Color32(0, 255, a, (byte)(Mathf.Clamp01(sm) * 255f + .5f));
                }
                void Save(string path, Color32[] px, bool srgb)
                {
                    var t = new Texture2D(W, H, TextureFormat.RGBA32, false, !srgb); t.SetPixels32(px); File.WriteAllBytes(path, t.EncodeToPNG()); Object.DestroyImmediate(t);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    var ti = (TextureImporter)AssetImporter.GetAtPath(path); ti.sRGBTexture = srgb; ti.alphaIsTransparency = srgb; ti.alphaSource = TextureImporterAlphaSource.FromInput; ti.userData = sig; ti.SaveAndReimport();
                }
                Save(bc, cc, true); Save(mask, mm, false);
            }
            if(AssetImporter.GetAtPath(normal) is TextureImporter ni && ni.textureType != TextureImporterType.NormalMap) { ni.textureType = TextureImporterType.NormalMap; ni.SaveAndReimport(); }
            var m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(!m || pack) Setup(ref m, id, matPath, bc, normal, mask);
            if(m.GetColor("_BaseColor") != tint) { m.SetColor("_BaseColor", tint); EditorUtility.SetDirty(m); AssetDatabase.SaveAssetIfDirty(m); }
            return m;
        }

        static void Setup(ref Material m, string id, string matPath, string bc, string normal, string mask)
        {
            Directory.CreateDirectory(MatDir);
            if(!m) { m = new Material(Shader.Find("HDRP/Decal")); AssetDatabase.CreateAsset(m, matPath); }
            m.SetTexture("_BaseColorMap", AssetDatabase.LoadAssetAtPath<Texture2D>(bc));
            m.SetTexture("_NormalMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            m.SetTexture("_MaskMap", AssetDatabase.LoadAssetAtPath<Texture2D>(mask));
            m.SetFloat("_AffectAlbedo", 1); m.SetFloat("_AffectNormal", id.StartsWith("AsphaltDamage") ? 0 : 1); // its broken-asphalt relief glitters on a wet stain m.SetFloat("_AffectSmoothness", 1); m.SetFloat("_AffectMetal", 0); m.SetFloat("_AffectAO", 0);
            m.SetFloat("_DecalBlend", 1); m.SetFloat("_NormalBlendSrc", 1); m.SetFloat("_MaskBlendSrc", 1); m.SetFloat("_DecalMaskMapBlueScale", 1);
            HDMaterial.ValidateMaterial(m); EditorUtility.SetDirty(m); AssetDatabase.SaveAssets();
        }

        /// <summary>Creates a DecalProjector under <paramref name="parent"/>: the box (size.x wide, size.y tall, <paramref name="depth"/> deep) is centred on <paramref name="surfacePoint"/> and projects along +Z, set to <paramref name="intoSurface"/> with <paramref name="up"/> as the image's up.</summary>
        public static DecalProjector Place(Material m, Transform parent, string name, Vector3 surfacePoint, Vector3 intoSurface, Vector3 up, Vector2 size, float depth)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false);
            g.transform.SetPositionAndRotation(surfacePoint, Quaternion.LookRotation(intoSurface, up));
            var d = g.AddComponent<DecalProjector>();
            d.material = m; d.size = new Vector3(size.x, size.y, depth); d.pivot = Vector3.zero; d.fadeFactor = 1; d.drawDistance = 40;
            return d;
        }
    }
}
