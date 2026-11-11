using System.Collections;
using System.Collections.Generic;
using CityScooter.Gameplay;
using CityScooter.Track;
using NUnit.Framework;
using UnityEngine;

namespace CityScooter.Tests
{
    /// <summary>
    /// v0.6 polish, south line B (Kwinana, Wellard, Rockingham, Warnbro): the "ride it first" pictures. Rides one
    /// district on the real route from the chase camera and renders a frame every 20 m, from 60 m before its entry
    /// boundary to 20 m after its exit boundary, then packs the frames into contact sheets (3 x 3 frames of 640 x 360).
    ///   Sheets: art-src/districts/&lt;id&gt;/previews/polish/ride-&lt;tag&gt;-&lt;n&gt;.jpg
    ///   Single frames (1280 x 720) when -cityscooterRideOut &lt;folder&gt; is given: &lt;folder&gt;/&lt;id&gt;-&lt;tag&gt;-&lt;metres&gt;.png
    /// -cityscooterRideTag before|round1|round2 names the pass (default "ride").
    /// The callers load Main and hand over a runtime copy of the tuning (see DistrictSouth3Screenshots).
    /// </summary>
    public static class SouthBRideThrough
    {
        const int TileW = 640, TileH = 360, Columns = 3, Rows = 3;

        static string Arg(string name, string fallback)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, name);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : fallback;
        }

        public static IEnumerator Ride(GameController game, GameTuning tuning, string districtId)
        {
            TrackTheme theme = game.track.theme;
            Assert.IsTrue(theme.HasDistricts, "run DistrictSetup.ApplyBatch first");
            int index = System.Array.FindIndex(theme.districts, d => d != null && d.id == districtId);
            Assert.GreaterOrEqual(index, 0, districtId + " is on the route");
            District before = theme.districts[(index + theme.districts.Length - 1) % theme.districts.Length];
            string tag = Arg("-cityscooterRideTag", "ride"), outFolder = Arg("-cityscooterRideOut", "");
            tuning.fakeTurns = false;
            tuning.boulevardSections = false;
            tuning.hazardChanceStart = tuning.hazardChanceMax = 0f;
            tuning.obstacleChanceStart = tuning.obstacleChanceMax = .2f;
            tuning.startDistrict = before.id;
            game.EnterReady();
            yield return null;
            yield return null;

            // the entry boundary, then the exit boundary
            float z = game.rider.transform.position.z, entry = float.PositiveInfinity;
            for (int step = 0; step < 60 && float.IsInfinity(entry); step++)
            {
                entry = game.track.NextBoundaryAfter(game.rider.transform.position.z);
                if (!float.IsInfinity(entry)) break;
                z += 60f;
                yield return RiderAt(game, z);
            }
            Assert.IsFalse(float.IsInfinity(entry), "found the boundary into " + districtId);
            while (z < entry - 150f) { z += 60f; yield return RiderAt(game, z); }

            var frames = new List<Texture2D>();
            var labels = new List<string>();
            float exit = float.PositiveInfinity;
            for (float at = entry - 60f; at <= (float.IsInfinity(exit) ? entry + 700f : exit + 20f); at += 20f)
            {
                yield return RiderAt(game, at);
                for (int f = 0; f < 5; f++) yield return null;
                if (float.IsInfinity(exit) && at > entry + 20f)
                {
                    float next = game.track.NextBoundaryAfter(at);
                    if (!float.IsInfinity(next) && next > entry + 100f) exit = next;
                }
                int metres = Mathf.RoundToInt(at - entry);
                Texture2D frame = Render(Camera.main, 1280, 720);
                if (outFolder.Length > 0)
                {
                    System.IO.Directory.CreateDirectory(outFolder);
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outFolder, districtId + "-" + tag + "-" + (metres < 0 ? "m" : "p") + Mathf.Abs(metres).ToString("000") + ".png"), frame.EncodeToPNG());
                }
                frames.Add(Shrink(frame, TileW, TileH));
                labels.Add(metres.ToString());
                Object.Destroy(frame);
            }
            Debug.Log("[RideThrough] " + districtId + " (" + tag + "): entry at " + entry + ", exit at " + exit + ", frames at " + string.Join(",", labels));

            string folder = System.IO.Path.Combine(Application.dataPath, "../art-src/districts/" + districtId + "/previews/polish");
            System.IO.Directory.CreateDirectory(folder);
            int perSheet = Columns * Rows, sheet = 0;
            for (int first = 0; first < frames.Count; first += perSheet, sheet++)
            {
                var page = new Texture2D(TileW * Columns, TileH * Rows, TextureFormat.RGB24, false);
                page.SetPixels32(new Color32[TileW * Columns * TileH * Rows]);
                for (int i = first; i < Mathf.Min(frames.Count, first + perSheet); i++)
                {
                    int col = (i - first) % Columns, row = (i - first) / Columns;
                    page.SetPixels32(col * TileW, (Rows - 1 - row) * TileH, TileW, TileH, frames[i].GetPixels32());
                }
                page.Apply();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, "ride-" + tag + "-" + (sheet + 1) + ".jpg"), page.EncodeToJPG(88));
                Object.Destroy(page);
            }
            foreach (Texture2D t in frames) Object.Destroy(t);
            Assert.Greater(frames.Count, 10, "rode through " + districtId);
        }

        static IEnumerator RiderAt(GameController game, float z)
        {
            game.rider.ResetRider(new Vector3(0f, game.track.SurfaceHeightAt(z), z));
            Camera.main.GetComponent<FollowCamera>()?.SnapToTarget();
            for (int i = 0; i < 4; i++) yield return null;
            Camera.main.GetComponent<FollowCamera>()?.SnapToTarget();
        }

        static Texture2D Render(Camera camera, int w, int h)
        {
            var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 4);
            RenderTexture previous = RenderTexture.active, oldTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = rt;
                // camera.Render, not SubmitRenderRequest: only this runs beginCameraRendering (sky, far scenery, curl).
                for (int pass = 0; pass < 2; pass++) camera.Render();
                RenderTexture.active = rt;
                var image = new Texture2D(w, h, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                image.Apply();
                return image;
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = oldTarget;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        static Texture2D Shrink(Texture2D source, int w, int h)
        {
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            try
            {
                source.filterMode = FilterMode.Bilinear;
                Graphics.Blit(source, rt);
                RenderTexture.active = rt;
                var small = new Texture2D(w, h, TextureFormat.RGB24, false);
                small.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                small.Apply();
                return small;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}
