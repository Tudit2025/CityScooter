using CityScooter.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CityScooter.EditorTools.LookDev
{
    /// <summary>
    /// Batchmode smoke test for QualityAutoTuner in real play mode: (1) it measures and logs a tier, (2) a forced Low
    /// tier swaps in a runtime copy of the URP asset with shadows off, render scale 0.7, MSAA off and VFX density 0.5,
    /// (3) stopping play mode puts the real asset back. Exit code 0 = pass.
    /// -executeMethod CityScooter.EditorTools.LookDev.QualityAutoTunerSmokeTest.RunBatch   (no -quit)
    /// </summary>
    [InitializeOnLoad]
    public static class QualityAutoTunerSmokeTest
    {
        const string StageKey = "CityScooter.TunerSmoke.Stage";
        const string TimeKey = "CityScooter.TunerSmoke.Time";
        const string FailKey = "CityScooter.TunerSmoke.Failed";
        const string OriginalKey = "CityScooter.TunerSmoke.Original";

        // Play mode reloads the domain; this re-hooks the update loop afterwards.
        static QualityAutoTunerSmokeTest()
        {
            if (SessionState.GetInt(StageKey, 0) != 0) EditorApplication.update += Tick;
        }

        public static void RunBatch()
        {
            EditorSceneManager.OpenScene(LookDevPaths.Scene, OpenSceneMode.Single);
            SessionState.SetString(OriginalKey, Describe(QualitySettings.renderPipeline));
            SessionState.SetInt(FailKey, 0);
            SessionState.SetInt(StageKey, 1);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        static void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            switch (SessionState.GetInt(StageKey, 0))
            {
                case 1: // entering play mode
                    if (!EditorApplication.isPlaying) return;
                    var measured = new GameObject("TunerUnderTest").AddComponent<QualityAutoTuner>();
                    measured.warmupSeconds = 0.1f;
                    measured.measureSeconds = 1f;
                    Next(2, now);
                    return;

                case 2: // waiting for the measured decision
                    if (!QualityAutoTuner.HasDecided)
                    {
                        if (now - SessionState.GetFloat(TimeKey, 0f) > 30f) Fail("no decision after 30 s");
                        else return;
                    }
                    Log("measured tier " + QualityAutoTuner.CurrentTier);
                    Object.Destroy(Object.FindFirstObjectByType<QualityAutoTuner>().gameObject);
                    var forced = new GameObject("TunerForcedLow").AddComponent<QualityAutoTuner>();
                    forced.forceTier = true;
                    forced.forcedTier = QualityAutoTuner.Tier.Low;
                    Next(3, now);
                    return;

                case 3: // a moment later the forced tier must be in place
                    if (now - SessionState.GetFloat(TimeKey, 0f) < 0.5f) return;
                    var copy = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
                    Check(copy != null && copy.name.Contains("auto-tuned"), "runtime copy assigned (" + Describe(copy) + ")");
                    if (copy != null)
                    {
                        Check(copy.shadowDistance == 0f, "shadow distance 0");
                        Check(Mathf.Approximately(copy.renderScale, 0.7f), "render scale 0.7");
                        Check(copy.msaaSampleCount == 1, "MSAA off");
                    }
                    Check(Mathf.Approximately(QualityAutoTuner.VfxDensity, 0.5f), "VFX density 0.5");
                    Check(!BlobShadow.RealShadowsActive(), "blob shadow sees real shadows off");
                    Next(4, now);
                    EditorApplication.ExitPlaymode();
                    return;

                case 4: // back in edit mode: the real asset must be restored
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    string original = SessionState.GetString(OriginalKey, "?");
                    Check(Describe(QualitySettings.renderPipeline) == original,
                        "original asset restored (" + Describe(QualitySettings.renderPipeline) + " == " + original + ")");
                    bool failed = SessionState.GetInt(FailKey, 0) != 0;
                    SessionState.EraseInt(StageKey);
                    EditorApplication.update -= Tick;
                    Debug.Log("[CityScooter] QualityAutoTuner smoke test " + (failed ? "FAILED" : "passed"));
                    EditorApplication.Exit(failed ? 1 : 0);
                    return;
            }
        }

        static void Next(int stage, double now)
        {
            SessionState.SetInt(StageKey, stage);
            SessionState.SetFloat(TimeKey, (float)now);
        }

        static void Check(bool ok, string what)
        {
            if (ok) Log("ok: " + what);
            else Fail(what);
        }

        static void Fail(string what)
        {
            SessionState.SetInt(FailKey, 1);
            Debug.LogError("[CityScooter] QualityAutoTuner smoke test FAILED: " + what);
        }

        static void Log(string message) => Debug.Log("[CityScooter] QualityAutoTuner smoke test: " + message);

        static string Describe(RenderPipelineAsset asset) => asset != null ? asset.name : "(none)";
    }
}
