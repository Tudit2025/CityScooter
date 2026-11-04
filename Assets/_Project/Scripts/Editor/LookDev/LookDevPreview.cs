using System;
using System.Collections.Generic;
using System.IO;
using CityScooter.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace CityScooter.EditorTools.LookDev
{
    /// <summary>
    /// Renders LookDev.unity to PNGs with the Web quality level (Mobile_RPAsset), plus PC and auto-tuner tier
    /// comparisons and skyline close-ups. Needs a GPU (do not pass -nographics).
    /// Batchmode: -executeMethod CityScooter.EditorTools.LookDev.LookDevPreview.RenderBatch -lookdevOut "C:/some/folder"
    /// The scene is never saved; quality level and render pipeline are restored afterwards.
    /// </summary>
    public static class LookDevPreview
    {
        const int Width = 1280;
        const int Height = 720;

        [MenuItem("CityScooter/Look Dev/Render Preview PNGs (Temp/LookDevPreviews)", priority = 20)]
        static void RenderMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string folder = Path.Combine(LookDevUtil.ProjectRoot, "Temp", "LookDevPreviews");
            RenderAll(folder);
            EditorUtility.RevealInFinder(folder);
        }

        public static void RenderBatch()
        {
            int code = 0;
            try
            {
                RenderAll(LookDevUtil.GetArg("-lookdevOut") ?? Path.Combine(LookDevUtil.ProjectRoot, "Temp", "LookDevPreviews"));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        public static void RenderAll(string outDir)
        {
            Directory.CreateDirectory(outDir);
            bool asyncWas = ShaderUtil.allowAsyncCompilation;
            int qualityWas = QualitySettings.GetQualityLevel();
            var spawned = new List<Object>();
            ShaderUtil.allowAsyncCompilation = false; // otherwise first renders show the cyan placeholder shader

            try
            {
                EditorSceneManager.OpenScene(LookDevPaths.Scene, OpenSceneMode.Single);
                var apply = Object.FindFirstObjectByType<ApplySceneLook>();
                var rig = Object.FindFirstObjectByType<SkylineParallax>();
                var blob = Object.FindFirstObjectByType<BlobShadow>();
                var lines = Object.FindFirstObjectByType<SpeedLines>();
                Camera cam = Camera.main;
                if (apply == null || rig == null || cam == null) throw new InvalidOperationException("LookDev scene is missing its look, rig or camera");
                var perth = AssetDatabase.LoadAssetAtPath<SceneLook>(LookDevPaths.LookPerth);
                var rockingham = AssetDatabase.LoadAssetAtPath<SceneLook>(LookDevPaths.LookRockingham);

                int web = Array.IndexOf(QualitySettings.names, "Mobile");
                int pc = Array.IndexOf(QualitySettings.names, "PC");
                SetQuality(web < 0 ? 0 : web);

                SetLook(apply, perth);
                SpawnVfx(spawned, lines);
                Transform camT = cam.transform;
                Vector3 homePos = camT.localPosition;
                Quaternion homeRot = camT.localRotation;
                float homeFov = cam.fieldOfView;

                Render(cam, rig, blob, Path.Combine(outDir, "01_gameplay_perth_web.png"), 1f); // warm-up + first view
                Render(cam, rig, blob, Path.Combine(outDir, "01_gameplay_perth_web.png"), 1f);

                camT.localPosition = new Vector3(0f, 9f, -16f);
                camT.localRotation = Quaternion.LookRotation(new Vector3(0f, 4f, 80f) - camT.localPosition);
                Render(cam, rig, blob, Path.Combine(outDir, "02_wide_perth_web.png"), 1f);

                // Close-up on the skyline (halo / compression check).
                camT.localPosition = homePos;
                camT.localRotation = Quaternion.Euler(-4f, 0f, 0f);
                cam.fieldOfView = 16f;
                Render(cam, rig, blob, Path.Combine(outDir, "03_skyline_zoom_perth_web.png"), 1f);
                cam.fieldOfView = homeFov;

                // Looking towards the sun: disc, halo and warm glow.
                Quaternion sunRotation = perth != null ? perth.SunRotation : Quaternion.identity;
                Vector3 toSun = -(sunRotation * Vector3.forward);
                Vector3 flat = new Vector3(toSun.x, 0f, toSun.z).normalized;
                camT.localRotation = Quaternion.LookRotation(Vector3.Slerp(flat, toSun, 0.6f));
                cam.fieldOfView = 70f;
                Render(cam, rig, blob, Path.Combine(outDir, "04_sun_perth_web.png"), 1f);
                cam.fieldOfView = homeFov;
                camT.localPosition = homePos;
                camT.localRotation = homeRot;

                // Auto-tuner "Reduced" tier: shadows off (blob shadow shows), render scale 0.85.
                RenderWithRuntimeCopy(cam, rig, blob, Path.Combine(outDir, "05_gameplay_perth_tier_reduced.png"), 0.85f);

                if (pc >= 0)
                {
                    SetQuality(pc);
                    Render(cam, rig, blob, Path.Combine(outDir, "06_gameplay_perth_pc.png"), 1f);
                    SetQuality(web < 0 ? 0 : web);
                }

                SetLook(apply, rockingham);
                Render(cam, rig, blob, Path.Combine(outDir, "07_gameplay_rockingham_web.png"), 1f);
                camT.localRotation = Quaternion.Euler(-4f, 0f, 0f);
                cam.fieldOfView = 16f;
                Render(cam, rig, blob, Path.Combine(outDir, "08_skyline_zoom_rockingham_web.png"), 1f);
                cam.fieldOfView = homeFov;
                camT.localRotation = homeRot;
                SetLook(apply, perth);

                LookDevUtil.Log("previews written to " + outDir);
            }
            finally
            {
                foreach (Object o in spawned)
                {
                    if (o != null) Object.DestroyImmediate(o);
                }
                QualitySettings.SetQualityLevel(qualityWas, true);
                ShaderUtil.allowAsyncCompilation = asyncWas;
            }
        }

        static void SetQuality(int level)
        {
            QualitySettings.SetQualityLevel(level, true);
            RenderPipelineAsset rp = GraphicsSettings.currentRenderPipeline;
            LookDevUtil.Log("quality level '" + QualitySettings.names[level] + "' -> " + (rp != null ? rp.name : "built-in"));
        }

        static void SetLook(ApplySceneLook apply, SceneLook look)
        {
            if (look == null) return;
            apply.look = look;
            apply.Apply();
            SphericalHarmonicsL2 sh = RenderSettings.ambientProbe;
            LookDevUtil.Log("look " + look.name + ": ambient probe DC = (" + sh[0, 0].ToString("F3") + ", " + sh[1, 0].ToString("F3")
                + ", " + sh[2, 0].ToString("F3") + "), fog " + ColorUtility.ToHtmlStringRGB(RenderSettings.fogColor)
                + " " + RenderSettings.fogStartDistance + "-" + RenderSettings.fogEndDistance + " m");
        }

        static void SpawnVfx(List<Object> spawned, SpeedLines lines)
        {
            foreach (VfxPreviewSpawner spawner in Object.FindObjectsByType<VfxPreviewSpawner>(FindObjectsSortMode.None))
            {
                if (spawner.prefab == null) continue;
                var go = (GameObject)Object.Instantiate(spawner.prefab, spawner.transform.position, spawner.transform.rotation);
                spawned.Add(go);
                bool crash = spawner.prefab.name.Contains("Crash");
                foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>())
                {
                    if (ps.transform.parent == null || ps.transform.parent.GetComponent<ParticleSystem>() == null)
                        ps.Simulate(crash ? 0.3f : 0.15f, true, true, true);
                }
            }
            if (lines != null)
            {
                lines.ApplyEmission(1f);
                lines.GetComponent<ParticleSystem>().Simulate(1.2f, true, true, true);
            }
        }

        static void RenderWithRuntimeCopy(Camera cam, SkylineParallax rig, BlobShadow blob, string path, float renderScale)
        {
            RenderPipelineAsset original = QualitySettings.renderPipeline;
            var source = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (source == null) return;
            UniversalRenderPipelineAsset copy = Object.Instantiate(source);
            copy.hideFlags = HideFlags.DontSave;
            copy.shadowDistance = 0f; // what QualityAutoTuner does
            try
            {
                QualitySettings.renderPipeline = copy;
                Render(cam, rig, blob, path, renderScale);
            }
            finally
            {
                QualitySettings.renderPipeline = original;
                Object.DestroyImmediate(copy);
            }
        }

        /// <summary>Renders the camera into an sRGB target (MSAA as the active URP asset) and saves a PNG.</summary>
        static void Render(Camera cam, SkylineParallax rig, BlobShadow blob, string path, float renderScale)
        {
            rig.Refresh();
            if (blob != null) blob.UpdateBlob();

            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            int msaa = urp != null ? Mathf.Max(1, urp.msaaSampleCount) : 1;
            int w = Mathf.RoundToInt(Width * renderScale);
            int h = Mathf.RoundToInt(Height * renderScale);
            var target = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = msaa };
            var output = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            target.Create();
            output.Create();
            RenderTexture previous = RenderTexture.active;
            try
            {
                var request = new RenderPipeline.StandardRequest { destination = target };
                if (RenderPipeline.SupportsRenderRequest(cam, request))
                {
                    RenderPipeline.SubmitRenderRequest(cam, request);
                }
                else
                {
                    cam.targetTexture = target;
                    cam.Render();
                    cam.targetTexture = null;
                }

                Graphics.Blit(target, output); // bilinear upscale when renderScale < 1 (like the URP final blit)
                RenderTexture.active = output;
                var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false, false);
                tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            finally
            {
                RenderTexture.active = previous;
                cam.targetTexture = null;
                target.Release();
                output.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(output);
            }
            LookDevUtil.Log("rendered " + Path.GetFileName(path) + " (" + (urp != null ? urp.name : "?") + ", MSAA " + msaa
                + ", scale " + renderScale + ", real shadows " + BlobShadow.RealShadowsActive() + ")");
        }
    }
}
