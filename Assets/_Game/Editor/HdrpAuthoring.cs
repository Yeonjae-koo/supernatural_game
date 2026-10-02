using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
namespace BeforeJericho.Editor
{
    /// <summary>HDRP equivalents of the URP authoring the slice builders used, so migrated and newly built scenes match.
    /// URP intensities are kept numerically: a fixed exposure whose multiplier is π cancels HDRP's Lambert 1/π,
    /// so lights/ambient/fog read like the URP greybox. Sky, fog and background colours are divided by π for the same reason.</summary>
    public static class HdrpAuthoring
    {
        public const float FixedEV100 = -1.914f; // 1 / (1.2 * 2^EV) = π
        const string DataRoot = "Assets/_Game/Data/HDRP";

        public static Material Lit(string name, Color color, float smoothness = 0, Color? emission = null)
        {
            var m = new Material(Shader.Find("HDRP/Lit")) { name = name };
            ApplyLit(m, color, smoothness, 0, emission ?? Color.black);
            return m;
        }

        public static void ApplyLit(Material m, Color color, float smoothness, float metallic, Color emission)
        {
            if(m.shader.name != "HDRP/Lit") m.shader = Shader.Find("HDRP/Lit");
            m.SetColor("_BaseColor", color); m.SetFloat("_Smoothness", smoothness); m.SetFloat("_Metallic", metallic);
            // Emission is displayed at its authored value, independent of exposure.
            m.SetFloat("_UseEmissiveIntensity", 0); m.SetColor("_EmissiveColor", emission); m.SetFloat("_EmissiveExposureWeight", 0);
            HDMaterial.ValidateMaterial(m);
        }

        public static void Camera(GameObject go, Color background)
        {
            var data = go.GetComponent<HDAdditionalCameraData>() ?? go.AddComponent<HDAdditionalCameraData>();
            data.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            var bg = background / Mathf.PI; bg.a = 1; data.backgroundColorHDR = bg;
            data.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        }

        public static void Light(UnityEngine.Light light)
        {
            float intensity = light.intensity;
            var data = light.GetComponent<HDAdditionalLightData>() ?? light.gameObject.AddComponent<HDAdditionalLightData>();
            light.lightUnit = light.type == LightType.Directional ? LightUnit.Lux : LightUnit.Candela;
            light.intensity = intensity;
            // Lamps, lighter and flames cast soft low-res shadows near the player; a muzzle flash is too brief to need one.
            if(light.type != LightType.Directional && light.name != "Muzzle flash")
            { light.shadows = LightShadows.Soft; data.SetShadowResolutionLevel(0); data.shadowFadeDistance = 30; data.volumetricDimmer = 3; }
        }

        /// <summary>HDRP camera/light data for everything in the open scene plus its environment volume (from the scene's RenderSettings).</summary>
        public static void FinishScene(string sceneName, bool tonemap)
        {
            foreach(var cam in Object.FindObjectsByType<UnityEngine.Camera>(FindObjectsInactive.Include))
            {
                foreach(var c in cam.GetComponents<Component>()) if(c && c.GetType().Name == "UniversalAdditionalCameraData") Object.DestroyImmediate(c);
                Camera(cam.gameObject, cam.backgroundColor);
            }
            foreach(var light in Object.FindObjectsByType<UnityEngine.Light>(FindObjectsInactive.Include))
            {
                foreach(var c in light.GetComponents<Component>()) if(c && c.GetType().Name == "UniversalAdditionalLightData") Object.DestroyImmediate(c);
                Light(light);
            }
            Environment(sceneName, RenderSettings.ambientLight, RenderSettings.fog, RenderSettings.fogColor, RenderSettings.fogDensity, tonemap);
        }

        /// <summary>Global volume carrying the scene's former RenderSettings ambient/fog plus the shared night look (volumetric fog, SSAO, SSR, contact shadows).</summary>
        public static Volume Environment(string sceneName, Color ambient, bool fog, Color fogColor, float fogDensity, bool tonemap)
        {
            System.IO.Directory.CreateDirectory(DataRoot);
            string path = DataRoot + "/" + sceneName + "_Environment.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if(!profile) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, path); }
            var exposure = Get<Exposure>(profile); exposure.mode.Override(ExposureMode.Fixed); exposure.fixedExposure.Override(FixedEV100);
            var visual = Get<VisualEnvironment>(profile); visual.skyType.Override((int)SkyType.Gradient); visual.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
            var sky = Get<GradientSky>(profile); Color a = ambient / Mathf.PI;
            sky.top.Override(a); sky.middle.Override(a); sky.bottom.Override(a * .8f);
            if(Hdri.TryGetValue(sceneName, out string hdriId) && NightSky(hdriId, out var cube, out float irradiance))
            {
                // Night HDRI for ambient light and reflections (the camera still clears to its colour), scaled to the gradient's
                // upper-hemisphere irradiance (uniform radiance a/π → π·a) so the authored ambient level holds under the fixed exposure.
                visual.skyType.Override((int)SkyType.HDRI);
                var hdri = Get<HDRISky>(profile); hdri.hdriSky.Override(cube); hdri.skyIntensityMode.Override(SkyIntensityMode.Multiplier);
                hdri.multiplier.Override(Mathf.PI * Luminance(a) / irradiance); // sky colour parameters are used as linear values
            }
            var f = Get<Fog>(profile); f.enabled.Override(fog); f.colorMode.Override(FogColorMode.ConstantColor); f.color.Override(fogColor / Mathf.PI);
            // URP fogged geometry only (exp², weak up close); keep the clear-colour background unfogged and thin the near fog.
            // Volumetric fog: denser than the URP exp² (~25 m at the Prologue's .018) so lamps, the lighter and spirit glows scatter into halos;
            // albedo .5 keeps the fog near surface brightness (higher washes brighter-ambient chapters grey), lights scatter ×3 to compensate.
            f.meanFreePath.Override(fogDensity > 0 ? .45f / fogDensity : 400); f.maximumHeight.Override(400); f.maxFogDistance.Override(1);
            f.enableVolumetricFog.Override(true); f.albedo.Override(new Color(.43f, .44f, .45f)); f.depthExtent.Override(48); f.anisotropy.Override(.55f);
            if(!tonemap) Get<Tonemapping>(profile).mode.Override(TonemappingMode.None);
            if(!tonemap) Get<Bloom>(profile).intensity.Override(0);
            Get<MotionBlur>(profile).intensity.Override(0);
            var ao = Get<ScreenSpaceAmbientOcclusion>(profile); ao.intensity.Override(1.2f); ao.radius.Override(1.5f);
            Get<ScreenSpaceReflection>(profile).enabled.Override(true);
            var contact = Get<ContactShadows>(profile); contact.enable.Override(true); contact.length.Override(.2f);
            Get<HDShadowSettings>(profile).maxShadowDistance.Override(80);
            EditorUtility.SetDirty(profile);
            var go = GameObject.Find("HDRP Environment") ?? new GameObject("HDRP Environment");
            var volume = go.GetComponent<Volume>() ?? go.AddComponent<Volume>();
            volume.isGlobal = true; volume.priority = 0; volume.sharedProfile = profile;
            return volume;
        }

        /// <summary>The Prologue's shared post look (ACES, vignette, bloom) as HDRP overrides.</summary>
        public static void Atmosphere(VolumeProfile profile)
        {
            Get<Tonemapping>(profile).mode.Override(TonemappingMode.ACES);
            Get<Vignette>(profile).intensity.Override(.28f);
            var bloom = Get<Bloom>(profile); bloom.intensity.Override(.25f); bloom.threshold.Override(1.1f);
            EditorUtility.SetDirty(profile);
        }

        // Poly Haven CC0 night HDRIs in Art/HDRI (licences in Art/Textures/POLYHAVEN_LICENSE.txt). Cool-toned on purpose: warmth comes from the lamps. Moon-dominated ones (narrow_moonlit_road, rogland) suit open ground only: matching their irradiance leaves walls dark.
        static readonly System.Collections.Generic.Dictionary<string, string> Hdri = new()
        {
            { "SCN_NewOrleans_Slice", "kloppenheim_07" }, { "SCN_Atchafalaya_Slice", "kloppenheim_04" }, { "SCN_EastTexas_Slice", "kloppenheim_07" },
            { "SCN_GrantsNM_Slice", "rogland_moonlit_night" }, { "SCN_Winslow_Slice", "narrow_moonlit_road" }, { "SCN_Stanford_Slice", "rogland_moonlit_night" },
        };

        /// <summary>The HDRI as a cubemap plus its upper-hemisphere luminance irradiance, measured once from the lat-long image and cached in the importer's userData.</summary>
        static bool NightSky(string id, out Cubemap cube, out float irradiance)
        {
            string path = "Assets/_Game/Art/HDRI/" + id + "_2k.hdr";
            cube = null; irradiance = 0;
            if(AssetImporter.GetAtPath(path) is not TextureImporter importer) return false;
            if(!float.TryParse(importer.userData, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out irradiance))
            {
                importer.textureShape = TextureImporterShape.Texture2D; importer.isReadable = true; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
                var latLong = AssetDatabase.LoadAssetAtPath<Texture2D>(path); var px = latLong.GetPixels(); int w = latLong.width, h = latLong.height;
                double e = 0; // rows run bottom→top; ∫ L cosθ dω over the upper half, dω = cos(elevation) dElevation dAzimuth
                for(int y = h / 2; y < h; y++)
                {
                    float elevation = ((y + .5f) / h - .5f) * Mathf.PI, weight = Mathf.Sin(elevation) * Mathf.Cos(elevation) * (Mathf.PI / h) * (2 * Mathf.PI / w);
                    for(int x = 0; x < w; x++) e += Luminance(px[y * w + x]) * weight;
                }
                irradiance = (float)e;
                importer.userData = irradiance.ToString(System.Globalization.CultureInfo.InvariantCulture);
                importer.isReadable = false; importer.textureCompression = TextureImporterCompression.Compressed;
            }
            if(importer.textureShape != TextureImporterShape.TextureCube) { importer.textureShape = TextureImporterShape.TextureCube; importer.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap; importer.SaveAndReimport(); }
            cube = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            return cube && irradiance > 0;
        }

        static float Luminance(Color c) => .2126f * c.r + .7152f * c.g + .0722f * c.b;

        static T Get<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if(profile.TryGet(out T component)) return component;
            component = profile.Add<T>();
            component.name = typeof(T).Name; component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }
    }
}
