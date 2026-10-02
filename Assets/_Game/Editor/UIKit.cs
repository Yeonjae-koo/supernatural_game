using System.Collections.Generic;
using System.IO;
using BeforeJericho.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
namespace BeforeJericho.Editor
{
    /// <summary>Authoring helpers for the UI prefabs: HTML prototype units (cqw/cqh at 1600×900), fonts, generated sprites.</summary>
    static class UIKit
    {
        public const float W = 16f, H = 9f; // 1cqw, 1cqh in canvas units
        public const string ArtRoot = "Assets/_Game/Art/UI", FontRoot = "Assets/_Game/Art/Fonts";
        public static TMP_FontAsset Ui4, Ui6, Ui8, Kr4, Kr5, Kr7, MonoFont, Type, Paper4, Paper7;
        public static Material Kr4Shadow, Kr5Shadow;
        public static Sprite White, MenuBg, CompleteBg, CardBg, NotifyBg, DeadBg, Hurt, HurtFlash, Ring, Spinner, Dot, Tri, Star, Star4, Danger,
            Frame, KeyFill, KeyLine, SoftShadow, DashH, PaperTint, PhotoBg, PreviewBg, Knob, GuideFrame;
        public static readonly Color Ink = UIStyle.Ink, Line = UIStyle.Line, Bone = UIStyle.Bone, Ash = UIStyle.Ash, Dim = UIStyle.Dim, Body = UIStyle.Body,
            Accent = UIStyle.Accent, Cold = UIStyle.Cold, Ember = UIStyle.Ember, Clear = new(0, 0, 0, 0);

        // ---------- fonts ----------
        public static void Fonts()
        {
            var kr4 = Font("NotoSansKR-Regular", null); var kr5 = Font("NotoSansKR-Medium", null); var kr7 = Font("NotoSansKR-Bold", null);
            var myeongjo = Font("NanumMyeongjo-Regular", new[]{ kr4 });
            Kr4 = kr4; Kr5 = kr5; Kr7 = kr7;
            Ui4 = Font("Overpass-Regular", new[]{ kr4 }); Ui6 = Font("Overpass-SemiBold", new[]{ kr5 }); Ui8 = Font("Overpass-ExtraBold", new[]{ kr7 });
            MonoFont = Font("IBMPlexMono-Regular", new[]{ kr4 }); Type = Font("SpecialElite-Regular", new[]{ myeongjo, kr4 });
            Paper4 = Font("OldStandardTT-Regular", new[]{ myeongjo }); Paper7 = Font("OldStandardTT-Bold", new[]{ myeongjo });
            Kr4Shadow = ShadowPreset(Kr4); Kr5Shadow = ShadowPreset(Kr5);
        }

        static TMP_FontAsset Font(string file, TMP_FontAsset[] fallbacks)
        {
            string path = FontRoot + "/" + file + " SDF.asset";
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if(!asset)
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>(FontRoot + "/" + file + ".ttf");
                if(!font) throw new FileNotFoundException("Font missing: " + file);
                // Dynamic + multi-atlas: Hangul glyphs are rasterized on demand, the ASCII set is baked up front.
                asset = TMP_FontAsset.CreateFontAsset(font, 72, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                asset.name = file + " SDF";
                AssetDatabase.CreateAsset(asset, path);
                asset.material.name = file + " Material"; AssetDatabase.AddObjectToAsset(asset.material, asset);
                asset.atlasTextures[0].name = file + " Atlas"; AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
                asset.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~·—–…“”‘’★◀▶●◌▤✓┃╱", out _);
            }
            if(fallbacks != null) { asset.fallbackFontAssetTable = new List<TMP_FontAsset>(fallbacks); EditorUtility.SetDirty(asset); }
            return asset;
        }

        static Material ShadowPreset(TMP_FontAsset font)
        {
            string path = FontRoot + "/" + font.name + " Shadow.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m) { m = new Material(font.material) { name = font.name + " Shadow" }; AssetDatabase.CreateAsset(m, path); }
            // HTML text-shadow: 0 0 .8cqw #000 on HUD text without a box.
            m.EnableKeyword("UNDERLAY_ON"); m.SetColor("_UnderlayColor", new Color(0, 0, 0, .85f));
            m.SetFloat("_UnderlaySoftness", .6f); m.SetFloat("_UnderlayDilate", .3f); m.SetFloat("_UnderlayOffsetX", 0); m.SetFloat("_UnderlayOffsetY", 0);
            EditorUtility.SetDirty(m); return m;
        }

        /// <summary>HDRP composites UI in linear space; the prototype blends in sRGB. Re-derive alpha so a translucent
        /// dark layer (or a faint light one) reads like the CSS value over a dark scene.</summary>
        public static float DarkA(float a) => 1 - Mathf.Pow(1 - a, 2.2f);
        public static float LightA(float a) => Mathf.Pow(a, 2.2f);
        public static Color Over(float r, float g, float b, float a) => new(r, g, b, (r + g + b) / 3 < .5f ? DarkA(a) : LightA(a));

        // ---------- generated sprites (baked HTML gradients / shapes) ----------
        public static void Sprites()
        {
            Directory.CreateDirectory(ArtRoot);
            White = Tex("white", 4, 4, (x, y) => Color.white);
            MenuBg = Tex("menu_bg", 256, 4, (x, y) => new Color(5 / 255f, 6 / 255f, 7 / 255f, DarkA(Stop(x, .35f, 1, .94f, .25f))));
            CompleteBg = Tex("complete_bg", 256, 4, (x, y) => new Color(0, 0, 0, DarkA(Stop(x, .30f, 1, .9f, .2f))));
            CardBg = Tex("card_bg", 4, 256, (x, y) => new Color(0, 0, 0, DarkA(Stop(y, 0, .6f, .8f, 0))));
            NotifyBg = Tex("notify_bg", 256, 4, (x, y) => new Color(10 / 255f, 11 / 255f, 12 / 255f, DarkA(Stop(x, 0, .25f, 0, .85f))));
            DeadBg = Tex("dead_bg", 256, 144, (x, y) => { float d = Ellipse(x, y); var c = Color.Lerp(new Color(40 / 255f, 6 / 255f, 4 / 255f, .75f), new Color(0, 0, 0, .95f), d); c.a = DarkA(c.a); return c; });
            Hurt = Tex("hurt", 256, 144, (x, y) => new Color(140 / 255f, 20 / 255f, 15 / 255f, .55f * Mathf.Pow(Mathf.Clamp01((Edge(x, y) - .55f) / .45f), 1.6f)));
            HurtFlash = Tex("hurt_flash", 256, 144, (x, y) => new Color(170 / 255f, 25 / 255f, 15 / 255f, Mathf.Pow(Mathf.Clamp01((Edge(x, y) - .35f) / .65f), 1.4f)));
            Ring = Tex("ring", 128, 128, (x, y) => Circle(x, y, .44f, .05f));
            Spinner = Tex("spinner", 64, 64, (x, y) => { var c = Circle(x, y, .42f, .08f); return Mathf.Atan2(y - .5f, x - .5f) is float a && a > Mathf.PI * .25f && a < Mathf.PI * .75f ? Clear : c; });
            Dot = Tex("dot", 64, 64, (x, y) => new Color(1, 1, 1, Mathf.Clamp01((.48f - Vector2.Distance(new(x, y), new(.5f, .5f))) * 64)));
            Tri = Tex("triangle", 64, 64, (x, y) => new Color(1, 1, 1, InTri(x, y) ? 1 : 0));
            Star = Tex("star", 64, 64, (x, y) => new Color(1, 1, 1, InStar(x, y, 5, .48f, .2f) ? 1 : 0));
            Star4 = Tex("star4", 64, 64, (x, y) => new Color(1, 1, 1, InStar(x, y, 4, .48f, .12f) ? 1 : 0));
            Danger = Tex("danger", 64, 64, (x, y) => { float d = Vector2.Distance(new(x, y), new(.5f, .5f)); float a = Mathf.Atan2(y - .5f, x - .5f);
                bool ring = Mathf.Abs(d - .4f) < .04f && Mathf.Repeat(a * 6 / Mathf.PI, 1) < .6f; return ring ? UIStyle.Blood : d < .4f ? new Color(125 / 255f, 36 / 255f, 30 / 255f, .25f) : Clear; });
            Frame = Tex("frame", 16, 16, (x, y) => new Color(1, 1, 1, x < 1 / 16f || y < 1 / 16f || x > 15 / 16f || y > 15 / 16f ? 1 : 0), new Vector4(2, 2, 2, 2));
            KeyFill = Tex("key_fill", 32, 32, (x, y) => new Color(1, 1, 1, RoundRect(x, y, .15f, 1)), new Vector4(8, 8, 8, 8));
            KeyLine = Tex("key_line", 32, 32, (x, y) => new Color(1, 1, 1, RoundRect(x, y, .15f, 1) * (1 - RoundRect(x, y, .15f, 1, 1.5f / 32))), new Vector4(8, 8, 8, 8));
            SoftShadow = Tex("soft_shadow", 64, 64, (x, y) => new Color(0, 0, 0, DarkA(Mathf.Clamp01(Mathf.Min(Mathf.Min(x, 1 - x), Mathf.Min(y, 1 - y)) * 4))), new Vector4(24, 24, 24, 24));
            DashH = Tex("dash", 12, 2, (x, y) => new Color(1, 1, 1, x < .5f ? 1 : 0));
            PaperTint = Tex("paper_tint", 128, 128, (x, y) => { float d = Vector2.Distance(new(x * 1.2f, y), new(.84f, .8f)); return new Color(90 / 255f, 60 / 255f, 20 / 255f, .25f * Mathf.Clamp01((d - .4f) / .6f)); });
            PhotoBg = Tex("photo_bg", 4, 64, (x, y) => Color.Lerp(UIStyle.Hex(0x3a3527), UIStyle.Hex(0x6b6452), y));
            PreviewBg = Tex("preview_bg", 4, 64, (x, y) => Color.Lerp(UIStyle.Hex(0x0b0c0d), UIStyle.Hex(0x1a1d20), y));
            Knob = Dot;
            GuideFrame = Tex("guide_frame", 16, 16, (x, y) => { bool edge = x < 1 / 16f || y < 1 / 16f || x > 15 / 16f || y > 15 / 16f; bool dash = ((int)((x + y) * 16) % 4) < 2;
                return edge && dash ? new Color(217 / 255f, 154 / 255f, 74 / 255f, .8f) : new Color(217 / 255f, 154 / 255f, 74 / 255f, LightA(.06f)); }, new Vector4(3, 3, 3, 3));
        }

        static float Stop(float t, float a, float b, float va, float vb) => t <= a ? va : t >= b ? vb : Mathf.Lerp(va, vb, (t - a) / (b - a));
        static float Ellipse(float x, float y) => Mathf.Clamp01(new Vector2((x - .5f) * 2, (y - .5f) * 2).magnitude / 1.414f);
        static float Edge(float x, float y) => Mathf.Clamp01(Mathf.Max(Mathf.Abs(x - .5f) * 2, Mathf.Abs(y - .5f) * 2) * .7f + new Vector2((x - .5f) * 2, (y - .5f) * 2).magnitude * .3f);
        static Color Circle(float x, float y, float r, float t) { float d = Mathf.Abs(Vector2.Distance(new(x, y), new(.5f, .5f)) - r); return new Color(1, 1, 1, Mathf.Clamp01((t / 2 - d) * 128)); }
        static bool InTri(float x, float y) => y > .12f && y < .9f && Mathf.Abs(x - .5f) < (.9f - y) * .5f;
        static bool InStar(float x, float y, int points, float outer, float inner)
        {
            var p = new Vector2(x - .5f, y - .5f); float a = Mathf.Atan2(p.x, p.y), seg = Mathf.PI / points;
            float k = Mathf.Repeat(a, seg * 2) / seg; float r = k < 1 ? Mathf.Lerp(outer, inner, k) : Mathf.Lerp(inner, outer, k - 1);
            return p.magnitude < r * (points == 4 ? 1 : .98f);
        }
        static float RoundRect(float x, float y, float radius, float soft, float inset = 0)
        {
            var q = new Vector2(Mathf.Abs(x - .5f), Mathf.Abs(y - .5f)) - new Vector2(.5f - radius - inset, .5f - radius - inset);
            float d = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - radius;
            return Mathf.Clamp01(-d * 32 * soft + .5f);
        }

        static Sprite Tex(string name, int w, int h, System.Func<float, float, Color> f, Vector4 border = default)
        {
            string path = ArtRoot + "/" + name + ".png";
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for(int y = 0; y < h; y++) for(int x = 0; x < w; x++) t.SetPixel(x, y, f((x + .5f) / w, (y + .5f) / h));
            File.WriteAllBytes(path, t.EncodeToPNG()); Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single; imp.alphaIsTransparency = true; imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp; imp.filterMode = FilterMode.Bilinear; imp.spriteBorder = border; imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---------- node helpers ----------
        public static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false); return (RectTransform)go.transform;
        }
        public static RectTransform Stretch(RectTransform r, float left = 0, float top = 0, float right = 0, float bottom = 0)
        { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.pivot = new Vector2(.5f, .5f); r.offsetMin = new Vector2(left, bottom); r.offsetMax = new Vector2(-right, -top); return r; }
        /// <summary>Anchor + pivot at the same normalized point; pos/size in canvas units.</summary>
        public static RectTransform Place(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        { r.anchorMin = r.anchorMax = anchor; r.pivot = pivot ?? anchor; r.anchoredPosition = pos; r.sizeDelta = size; return r; }
        public static RectTransform TopLeft(RectTransform r, float leftCqw, float topCqh, float w, float h) => Place(r, new Vector2(0, 1), new Vector2(leftCqw * W, -topCqh * H), new Vector2(w, h));
        public static RectTransform TopRight(RectTransform r, float rightCqw, float topCqh, float w, float h) => Place(r, new Vector2(1, 1), new Vector2(-rightCqw * W, -topCqh * H), new Vector2(w, h));
        public static RectTransform BottomLeft(RectTransform r, float leftCqw, float bottomCqh, float w, float h) => Place(r, new Vector2(0, 0), new Vector2(leftCqw * W, bottomCqh * H), new Vector2(w, h));
        public static RectTransform BottomRight(RectTransform r, float rightCqw, float bottomCqh, float w, float h) => Place(r, new Vector2(1, 0), new Vector2(-rightCqw * W, bottomCqh * H), new Vector2(w, h));

        public static TextMeshProUGUI Text(Transform parent, string name, TMP_FontAsset font, float cqw, Color color, string key = null, string text = "",
            TextAlignmentOptions align = TextAlignmentOptions.TopLeft, float letterEm = 0, float lineHeight = 0, bool upper = false, bool wrap = true)
        {
            var r = Node(parent, name); var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font; t.fontSize = cqw * W; t.color = color; t.alignment = align; t.characterSpacing = letterEm * 100; t.raycastTarget = false;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
            if(upper) t.fontStyle |= FontStyles.UpperCase;
            if(lineHeight > 0) t.lineSpacing = (lineHeight - NaturalLine(font)) * 100; // CSS line-height → TMP extra spacing (em/100)
            t.text = text;
            if(key != null) { var lt = r.gameObject.AddComponent<LocText>(); Set(lt, "_key", key); }
            return t;
        }
        static float NaturalLine(TMP_FontAsset f) { var fi = f.faceInfo; return fi.pointSize > 0 ? fi.lineHeight / fi.pointSize : 1.2f; }

        public static Image Img(Transform parent, string name, Sprite sprite, Color color, Image.Type type = Image.Type.Simple, bool raycast = false)
        {
            var r = Node(parent, name); var i = r.gameObject.AddComponent<Image>(); i.sprite = sprite; i.color = color; i.type = type; i.raycastTarget = raycast; return i;
        }
        public static VerticalLayoutGroup V(Component c, float spacing = 0, RectOffset pad = null, TextAnchor align = TextAnchor.UpperLeft, bool controlW = true)
        {
            var g = c.gameObject.AddComponent<VerticalLayoutGroup>(); g.spacing = spacing; g.padding = pad ?? new RectOffset(); g.childAlignment = align;
            g.childControlWidth = controlW; g.childControlHeight = true; g.childForceExpandWidth = false; g.childForceExpandHeight = false; return g;
        }
        public static HorizontalLayoutGroup Hz(Component c, float spacing = 0, RectOffset pad = null, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var g = c.gameObject.AddComponent<HorizontalLayoutGroup>(); g.spacing = spacing; g.padding = pad ?? new RectOffset(); g.childAlignment = align;
            g.childControlWidth = true; g.childControlHeight = true; g.childForceExpandWidth = false; g.childForceExpandHeight = false; return g;
        }
        public static ContentSizeFitter Fit(Component c, bool w = true, bool h = true)
        {
            var f = c.gameObject.AddComponent<ContentSizeFitter>();
            f.horizontalFit = w ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            f.verticalFit = h ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained; return f;
        }
        public static LayoutElement LE(Component c, float w = -1, float h = -1, float flexW = -1, float minW = -1, bool ignore = false)
        { var e = c.gameObject.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>(); e.preferredWidth = w; e.preferredHeight = h; e.flexibleWidth = flexW; e.minWidth = minW; e.ignoreLayout = ignore; return e; }
        /// <summary>CSS block box: take the full column width (LayoutElement.flexibleWidth = 1).</summary>
        public static T Fill<T>(T c) where T : Component { var e = c.gameObject.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>(); e.flexibleWidth = 1; return c; }
        public static RectOffset Pad(float l, float r, float t, float b) => new(Mathf.RoundToInt(l), Mathf.RoundToInt(r), Mathf.RoundToInt(t), Mathf.RoundToInt(b));
        public static RectTransform Spacer(Transform parent, float h) { var r = Node(parent, "Space"); LE(r, -1, h); return r; }

        public static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target); var p = so.FindProperty(field);
            if(p == null) throw new System.ArgumentException(target.GetType().Name + " has no serialized field " + field);
            switch(value)
            {
                case Object o: p.objectReferenceValue = o; break;
                case string s: p.stringValue = s; break;
                case bool b: p.boolValue = b; break;
                case float f: p.floatValue = f; break;
                case int i: p.intValue = i; break;
                case System.Collections.IList list:
                    p.arraySize = list.Count; for(int i = 0; i < list.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = (Object)list[i]; break;
                default: throw new System.ArgumentException("Unsupported value for " + field);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static GameObject Save(GameObject go, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go); return prefab;
        }
        public static T Instance<T>(GameObject prefab, Transform parent, string name = null) where T : Component
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if(name != null) go.name = name; return go.GetComponent<T>();
        }
    }
}
