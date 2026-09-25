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
            data.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
        }

        public static void Light(UnityEngine.Light light)
        {
            float intensity = light.intensity;
            if(!light.GetComponent<HDAdditionalLightData>()) light.gameObject.AddComponent<HDAdditionalLightData>();
            light.lightUnit = light.type == LightType.Directional ? LightUnit.Lux : LightUnit.Candela;
            light.intensity = intensity;
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

        /// <summary>Global volume carrying the scene's former RenderSettings ambient/fog plus neutral HDRP defaults.</summary>
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
            var f = Get<Fog>(profile); f.enabled.Override(fog); f.colorMode.Override(FogColorMode.ConstantColor); f.color.Override(fogColor / Mathf.PI);
            // URP fogged geometry only (exp², weak up close); keep the clear-colour background unfogged and thin the near fog.
            f.meanFreePath.Override(fogDensity > 0 ? 2f / fogDensity : 400); f.maximumHeight.Override(400); f.maxFogDistance.Override(1); f.enableVolumetricFog.Override(false);
            if(!tonemap) Get<Tonemapping>(profile).mode.Override(TonemappingMode.None);
            if(!tonemap) Get<Bloom>(profile).intensity.Override(0);
            Get<MotionBlur>(profile).intensity.Override(0);
            Get<ScreenSpaceAmbientOcclusion>(profile).intensity.Override(0);
            Get<ScreenSpaceReflection>(profile).enabled.Override(false);
            Get<ContactShadows>(profile).enable.Override(false);
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
