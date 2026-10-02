using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
namespace BeforeJericho.Editor
{
    /// <summary>One-shot, re-runnable URP → HDRP conversion of the pipeline settings, materials, Atmosphere profile and the six slice scenes.
    /// Reads URP data by name only, so it keeps compiling after the URP package is removed.</summary>
    public static class HdrpMigration
    {
        const string AssetPath = "Assets/Settings/HDRP/HDRenderPipelineAsset.asset";
        static readonly string[] Scenes = { "SCN_NewOrleans_Slice", "SCN_Atchafalaya_Slice", "SCN_EastTexas_Slice", "SCN_GrantsNM_Slice", "SCN_Winslow_Slice", "SCN_Stanford_Slice" };

        [MenuItem("Before Jericho/Rendering/Migrate Project To HDRP")]
        public static string Run()
        {
            if(EditorSceneManager.GetActiveScene().isDirty) throw new System.InvalidOperationException("Save the open scene first.");
            var log = new System.Text.StringBuilder();
            Pipeline(log); Materials(log); Atmosphere(log);
            foreach(string scene in Scenes) Scene(scene, log);
            AssetDatabase.SaveAssets();
            Debug.Log("HDRP migration\n" + log);
            return log.ToString();
        }

        static void Pipeline(System.Text.StringBuilder log)
        {
            System.IO.Directory.CreateDirectory("Assets/Settings/HDRP");
            var asset = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(AssetPath);
            if(!asset) { asset = ScriptableObject.CreateInstance<HDRenderPipelineAsset>(); AssetDatabase.CreateAsset(asset, AssetPath); }
            {
                // Features the slices do not use are off: fewer shader variants, shorter builds. Volumetrics + SSR carry the night look (lamp halos, wet streets).
                var so = new SerializedObject(asset);
                void Set(string name, bool value) { var p = so.FindProperty("m_RenderPipelineSettings." + name); if(p != null) p.boolValue = value; else log.AppendLine("missing setting " + name); }
                Set("supportSSR", true); Set("supportSSGI", false); Set("supportVolumetrics", true); Set("supportDecals", false);
                Set("supportSubsurfaceScattering", false); Set("supportRayTracing", false); Set("supportWater", false); Set("supportTransparentBackface", false);
                var mode = so.FindProperty("m_RenderPipelineSettings.supportedLitShaderMode"); if(mode != null) mode.intValue = (int)RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            GraphicsSettings.defaultRenderPipeline = asset;
            int current = QualitySettings.GetQualityLevel();
            for(int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = asset; }
            QualitySettings.SetQualityLevel(current, false);
            log.AppendLine("Pipeline: " + AssetPath + " on " + QualitySettings.names.Length + " quality levels");
        }

        static void Materials(System.Text.StringBuilder log)
        {
            int n = 0;
            foreach(string guid in AssetDatabase.FindAssets("t:Material", new[]{"Assets/_Game"}))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                // Only former URP materials (missing shader once URP is removed); TMP font and other materials keep their shaders.
                if(!m || !(m.shader.name.StartsWith("Universal Render Pipeline/") || m.shader.name == "Hidden/InternalErrorShader")) continue;
                Color color = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color;
                float smooth = m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness") : 0, metal = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0;
                Color emission = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;
                HdrpAuthoring.ApplyLit(m, color, smooth, metal, emission); EditorUtility.SetDirty(m); n++;
            }
            log.AppendLine("Materials converted: " + n);
        }

        static void Atmosphere(System.Text.StringBuilder log)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Game/Data/Atmosphere.asset");
            foreach(var c in profile.components.Where(c => !c || c.GetType().Namespace == "UnityEngine.Rendering.Universal").ToArray())
            { profile.components.Remove(c); if(c) AssetDatabase.RemoveObjectFromAsset(c); Object.DestroyImmediate(c, true); }
            HdrpAuthoring.Atmosphere(profile);
            log.AppendLine("Atmosphere.asset: " + string.Join(", ", profile.components.Select(c => c.GetType().Name)));
        }

        static void Scene(string name, System.Text.StringBuilder log)
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/" + name + ".unity");
            bool prologue = name == "SCN_NewOrleans_Slice";
            HdrpAuthoring.FinishScene(name, prologue);
            var atmosphere = GameObject.Find("Atmosphere"); if(atmosphere && atmosphere.TryGetComponent(out Volume v)) v.priority = 1;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            log.AppendLine(name + ": ambient " + RenderSettings.ambientLight + ", fog " + RenderSettings.fogDensity);
        }
    }
}
