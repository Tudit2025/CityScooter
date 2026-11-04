using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace CityScooter.EditorTools.LookDev
{
    /// <summary>
    /// The Web render settings from docs/art/urp-web-settings.md, as code: "Apply" writes them, "Check" reports any
    /// drift (run it before every Web build). Touches Mobile_RPAsset / Mobile_Renderer (Web), PC_RPAsset / PC_Renderer
    /// (editor + Windows), GraphicsSettings fog stripping and the PC quality level's platform list.
    /// Batchmode: -executeMethod CityScooter.EditorTools.LookDev.WebRenderSettings.ApplyBatch (or CheckBatch).
    /// </summary>
    public static class WebRenderSettings
    {
        const string MobileAssetPath = "Assets/Settings/Mobile_RPAsset.asset";
        const string MobileRendererPath = "Assets/Settings/Mobile_Renderer.asset";
        const string PcAssetPath = "Assets/Settings/PC_RPAsset.asset";
        const string PcRendererPath = "Assets/Settings/PC_Renderer.asset";
        const string GraphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";
        const string QualitySettingsPath = "ProjectSettings/QualitySettings.asset";

        // Serialized field -> wanted value. bool / int (enums by value) / float.
        static readonly (string field, object value)[] MobileAsset =
        {
            ("m_SupportsHDR", false),
            ("m_MSAA", 2),                       // MsaaQuality._2x
            ("m_RenderScale", 1f),
            ("m_UpscalingFilter", 1),            // Bilinear (only used when the auto-tuner lowers render scale)
            ("m_RequireDepthTexture", false),
            ("m_RequireOpaqueTexture", false),
            ("m_MainLightRenderingMode", 1),     // per pixel
            ("m_MainLightShadowsSupported", true),
            ("m_MainLightShadowmapResolution", 1024),
            ("m_ShadowDistance", 40f),
            ("m_ShadowCascadeCount", 1),
            ("m_SoftShadowsSupported", false),
            ("m_AdditionalLightsRenderingMode", 2), // per vertex
            ("m_AdditionalLightsPerObjectLimit", 2),
            ("m_AdditionalLightShadowsSupported", false),
            ("m_UseSRPBatcher", true),
            ("m_SupportsDynamicBatching", false),
            ("m_SupportsTerrainHoles", false),
            ("m_EnableLODCrossFade", false),
            ("m_MixedLightingSupported", false),
            ("m_SupportsLightCookies", false),
            ("m_SupportsLightLayers", false),
            ("m_ReflectionProbeBlending", false),
            ("m_ReflectionProbeBoxProjection", false),
            ("m_SupportDataDrivenLensFlare", false),
            ("m_SupportScreenSpaceLensFlare", false),
            ("m_UseAdaptivePerformance", false),
            ("m_ShEvalMode", 1),                 // per vertex
        };

        static readonly (string field, object value)[] MobileRenderer =
        {
            ("m_RenderingMode", 0),              // Forward (per-vertex additional lights need it; no Forward+ clustering)
            ("m_DepthPrimingMode", 0),
            ("m_IntermediateTextureMode", 0),    // Auto
        };

        static readonly (string field, object value)[] PcAsset =
        {
            ("m_SupportsHDR", false),            // same colour pipeline as the Web build
            ("m_MSAA", 4),
            ("m_RenderScale", 1f),
            ("m_RequireDepthTexture", false),
            ("m_RequireOpaqueTexture", false),
            ("m_MainLightShadowsSupported", true),
            ("m_MainLightShadowmapResolution", 2048),
            ("m_ShadowDistance", 60f),
            ("m_ShadowCascadeCount", 2),
            ("m_Cascade2Split", 0.3f),
            ("m_SoftShadowsSupported", true),
            ("m_SoftShadowQuality", 2),          // medium
            ("m_AdditionalLightsRenderingMode", 1), // per pixel
            ("m_AdditionalLightShadowsSupported", false),
            ("m_UseSRPBatcher", true),
        };

        static readonly (string field, object value)[] GraphicsFog =
        {
            ("m_FogStripping", 1),               // custom: keep exactly the modes below
            ("m_FogKeepLinear", true),           // ApplySceneLook turns linear fog on at runtime
            ("m_FogKeepExp", false),
            ("m_FogKeepExp2", false),
        };

        [MenuItem("CityScooter/Look Dev/Check Web URP Settings", priority = 40)]
        static void CheckMenu() => Check();

        [MenuItem("CityScooter/Look Dev/Apply Web URP Settings", priority = 41)]
        static void ApplyMenu()
        {
            if (!EditorUtility.DisplayDialog("Apply Web URP settings",
                    "Writes the values from docs/art/urp-web-settings.md into the URP assets, GraphicsSettings and QualitySettings.",
                    "Apply", "Cancel")) return;
            Apply();
            Check();
        }

        public static void ApplyBatch()
        {
            int code = 0;
            try
            {
                Apply();
                code = Check() == 0 ? 0 : 2;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        public static void CheckBatch()
        {
            int code;
            try
            {
                code = Check() == 0 ? 0 : 2;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        public static void Apply()
        {
            Write(Load(MobileAssetPath), MobileAsset);
            Write(Load(MobileRendererPath), MobileRenderer);
            Write(Load(PcAssetPath), PcAsset);

            // PC renderer: keep the SSAO feature but switch it off (Web has none; the editor should look the same).
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(PcRendererPath))
            {
                if (o is ScriptableRendererFeature feature && feature.isActive)
                {
                    feature.SetActive(false);
                    EditorUtility.SetDirty(feature);
                    LookDevUtil.Log("PC_Renderer: disabled feature " + feature.name);
                }
            }

            Write(Load(GraphicsSettingsPath), GraphicsFog);
            ExcludePcQualityFromWeb(true);
            AssetDatabase.SaveAssets();
            LookDevUtil.Log("Web URP settings applied");
        }

        /// <summary>Logs every deviation; returns how many there are.</summary>
        public static int Check()
        {
            var issues = new List<string>();
            Compare(Load(MobileAssetPath), MobileAsset, "Mobile_RPAsset (Web)", issues);
            Compare(Load(MobileRendererPath), MobileRenderer, "Mobile_Renderer (Web)", issues);
            Compare(Load(PcAssetPath), PcAsset, "PC_RPAsset", issues);
            Compare(Load(GraphicsSettingsPath), GraphicsFog, "GraphicsSettings", issues);

            var mobileRenderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(MobileRendererPath);
            if (mobileRenderer != null && mobileRenderer.rendererFeatures.Count > 0)
                issues.Add("Mobile_Renderer (Web): has " + mobileRenderer.rendererFeatures.Count + " renderer features (each one is a full-screen cost)");
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(PcRendererPath))
            {
                if (o is ScriptableRendererFeature feature && feature.isActive)
                    issues.Add("PC_Renderer: feature " + feature.name + " is active (editor would not match the Web look)");
            }
            if (!ExcludePcQualityFromWeb(false))
                issues.Add("QualitySettings: level 'PC' is not excluded for WebGL (its URP asset's shader variants go into the Web build)");

            var report = new StringBuilder("[CityScooter] Web URP settings check: ");
            if (issues.Count == 0) report.Append("OK");
            else report.Append(issues.Count).Append(" issue(s)\n  - ").Append(string.Join("\n  - ", issues));
            if (issues.Count == 0) Debug.Log(report.ToString());
            else Debug.LogWarning(report.ToString());
            return issues.Count;
        }

        static Object Load(string path)
        {
            Object asset = path.StartsWith("ProjectSettings/", StringComparison.Ordinal)
                ? AssetDatabase.LoadAllAssetsAtPath(path)[0]
                : AssetDatabase.LoadAssetAtPath<Object>(path);
            if (asset == null) throw new InvalidOperationException("Missing " + path);
            return asset;
        }

        static void Write(Object target, (string field, object value)[] values)
        {
            var so = new SerializedObject(target);
            foreach ((string field, object value) in values)
            {
                SerializedProperty p = so.FindProperty(field);
                if (p == null)
                {
                    LookDevUtil.Warn(target.name + ": no field " + field);
                    continue;
                }
                switch (value)
                {
                    case bool b: p.boolValue = b; break;
                    case int i: p.intValue = i; break;
                    case float f: p.floatValue = f; break;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        static void Compare(Object target, (string field, object value)[] values, string label, List<string> issues)
        {
            var so = new SerializedObject(target);
            foreach ((string field, object value) in values)
            {
                SerializedProperty p = so.FindProperty(field);
                if (p == null)
                {
                    issues.Add(label + ": field " + field + " not found");
                    continue;
                }
                object actual;
                bool same;
                switch (value)
                {
                    case bool b: actual = p.boolValue; same = p.boolValue == b; break;
                    case int i: actual = p.intValue; same = p.intValue == i; break;
                    case float f: actual = p.floatValue; same = Mathf.Abs(p.floatValue - f) < 1e-4f; break;
                    default: actual = "?"; same = false; break;
                }
                if (!same) issues.Add(label + ": " + field + " = " + actual + " (want " + value + ")");
            }
        }

        /// <summary>Makes sure the "PC" quality level is excluded for WebGL; returns whether it is (after the call).</summary>
        static bool ExcludePcQualityFromWeb(bool fix)
        {
            var so = new SerializedObject(Load(QualitySettingsPath));
            SerializedProperty levels = so.FindProperty("m_QualitySettings");
            for (int i = 0; i < levels.arraySize; i++)
            {
                SerializedProperty level = levels.GetArrayElementAtIndex(i);
                if (level.FindPropertyRelative("name").stringValue != "PC") continue;
                SerializedProperty excluded = level.FindPropertyRelative("excludedTargetPlatforms");
                for (int j = 0; j < excluded.arraySize; j++)
                {
                    if (excluded.GetArrayElementAtIndex(j).stringValue == "WebGL") return true;
                }
                if (!fix) return false;
                excluded.InsertArrayElementAtIndex(excluded.arraySize);
                excluded.GetArrayElementAtIndex(excluded.arraySize - 1).stringValue = "WebGL";
                so.ApplyModifiedPropertiesWithoutUndo();
                LookDevUtil.Log("QualitySettings: 'PC' level excluded for WebGL");
                return true;
            }
            LookDevUtil.Warn("QualitySettings: no level named 'PC'");
            return true;
        }
    }
}
