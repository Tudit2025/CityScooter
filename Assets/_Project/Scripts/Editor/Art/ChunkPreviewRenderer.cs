using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityScooter.Track;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityScooter.EditorTools.Art
{
    /// <summary>
    /// CityScooter/Art/Render Chunk Previews: PNGs of every Street chunk (rider eye, aerial, top-down; each chunk
    /// tiled three times so the seams at z = 0 / 40 are visible) plus an A-B-C-A sequence. Spawn slots are drawn as
    /// spheres (white Any, red Obstacle, yellow Gift, magenta JumpObstacle), lane edges as cyan lines.
    /// Output defaults to &lt;project&gt;/Logs/ChunkPreviews (git-ignored). Needs a GPU: no -nographics.
    /// Batchmode: -executeMethod CityScooter.EditorTools.Art.ChunkPreviewRenderer.RenderBatch [-cityscooterPreviewOut DIR]
    /// </summary>
    public static class ChunkPreviewRenderer
    {
        public const string MenuPath = "CityScooter/Art/Render Chunk Previews";

        public static string DefaultOutDir => Path.GetFullPath("Logs/ChunkPreviews");

        [MenuItem(MenuPath, priority = 121)]
        static void RenderMenu()
        {
            string dir = RenderAll(DefaultOutDir);
            if (dir != null) EditorUtility.RevealInFinder(dir);
        }

        public static void RenderBatch()
        {
            string dir = ArtPreviewUtility.GetCommandLineValue("-cityscooterPreviewOut", DefaultOutDir);
            EditorApplication.Exit(RenderAll(dir) != null ? 0 : 1);
        }

        public static string RenderAll(string outDir)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Debug.LogError("[CityScooter] Chunk previews need a graphics device: run Unity without -nographics.");
                return null;
            }
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return null;

            var temp = new List<Object>();
            try
            {
                ArtPreviewUtility.NewPreviewScene();
                Material Unlit(Color c)
                {
                    var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    m.SetColor("_BaseColor", c);
                    temp.Add(m);
                    return m;
                }
                var slotMats = new Dictionary<SpawnKind, Material>
                {
                    { SpawnKind.Any, Unlit(Color.white) },
                    { SpawnKind.Obstacle, Unlit(new Color(1f, 0.25f, 0.25f)) },
                    { SpawnKind.Gift, Unlit(new Color(1f, 0.85f, 0.1f)) },
                    { SpawnKind.JumpObstacle, Unlit(new Color(1f, 0.3f, 1f)) },
                };
                Material laneMat = Unlit(new Color(0.1f, 0.9f, 1f));

                var prefabs = StreetChunkBuilder.ChunkNames
                    .Select(n => (name: n, go: AssetDatabase.LoadAssetAtPath<GameObject>(StreetChunkBuilder.ChunkPath(n))))
                    .Where(p => p.go != null).ToList();

                foreach ((string name, GameObject prefab) in prefabs)
                {
                    var placed = new List<GameObject>();
                    for (int i = -1; i <= 1; i++) placed.Add(Place(prefab, i * 40f));
                    placed.AddRange(Markers(placed[1], slotMats, laneMat));

                    Shoot(outDir, name + "_rider.png", 1280, 720, new Vector3(0f, 3.2f, -7f), new Vector3(0f, 1.2f, 28f), 60f);
                    Shoot(outDir, name + "_aerial.png", 1280, 720, new Vector3(10f, 55f, -38f), new Vector3(0f, 0f, 22f), 45f);
                    ShootTop(outDir, name + "_top.png", 1200, 840, new Vector3(0f, 0f, 20f), 30f, Vector3.right);
                    foreach (GameObject go in placed) Object.DestroyImmediate(go);
                }

                if (prefabs.Count > 0)
                {
                    var seq = new List<GameObject>();
                    for (int i = 0; i <= prefabs.Count; i++)
                    {
                        GameObject go = Place(prefabs[i % prefabs.Count].go, i * 40f);
                        seq.Add(go);
                        seq.AddRange(Markers(go, slotMats, laneMat));
                    }
                    float end = seq.Count > 0 ? prefabs.Count * 40f + 40f : 40f;
                    ShootTop(outDir, "Sequence_top.png", 1800, 560, new Vector3(0f, 0f, end * 0.5f), 40f, Vector3.forward);
                    Shoot(outDir, "Sequence_aerial.png", 1600, 720, new Vector3(-55f, 45f, end * 0.5f - 40f), new Vector3(0f, 0f, end * 0.5f), 50f);
                    foreach (GameObject go in seq) Object.DestroyImmediate(go);

                    RenderLineup(outDir, prefabs[0].go);
                }
                Debug.Log("[CityScooter] Chunk previews written to " + outDir);
                return outDir;
            }
            finally
            {
                foreach (Object o in temp) Object.DestroyImmediate(o);
                if (setup.Length > 0 && setup.All(s => !string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        /// <summary>Every obstacle and gift prefab on a chunk's road, to judge their size against the 8 m lane.</summary>
        static void RenderLineup(string outDir, GameObject chunkPrefab)
        {
            var placed = new List<GameObject> { Place(chunkPrefab, 0f) };
            foreach (string group in new[] { "Buildings", "Props" })
            {
                Transform t = placed[0].transform.Find(group);
                if (t != null) t.gameObject.SetActive(false); // unsaved scene instance only
            }
            string[] obstacles = AssetDatabase.FindAssets("t:Prefab", new[] { ArtPaths.ObstaclePrefabRoot })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();
            string[] gifts = AssetDatabase.FindAssets("t:Prefab", new[] { ArtPaths.PickupPrefabRoot })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();
            float z = 4f;
            int lane = 0;
            foreach (string path in obstacles)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                bool span = prefab.GetComponent<Obstacle>() != null && prefab.GetComponent<Obstacle>().spansRoad;
                GameObject go = Place(prefab, span ? 37f : z);
                if (!span)
                {
                    go.transform.position += new Vector3(lane++ % 2 == 0 ? -2.5f : 2.5f, 0f, 0f);
                    z += 5f;
                }
                placed.Add(go);
            }
            for (int i = 0; i < gifts.Length; i++)
            {
                GameObject go = Place(AssetDatabase.LoadAssetAtPath<GameObject>(gifts[i]), 6.5f + 5f * i);
                placed.Add(go);
            }
            Shoot(outDir, "Lineup_rider.png", 1280, 720, new Vector3(0f, 3.2f, -7f), new Vector3(0f, 1.2f, 28f), 60f);
            Shoot(outDir, "Lineup_side.png", 1600, 600, new Vector3(16f, 4f, 20f), new Vector3(0f, 0.6f, 20f), 70f);
            foreach (GameObject go in placed) Object.DestroyImmediate(go);
        }

        static GameObject Place(GameObject prefab, float z)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = new Vector3(0f, 0f, z);
            return go;
        }

        static IEnumerable<GameObject> Markers(GameObject chunkInstance, Dictionary<SpawnKind, Material> slotMats, Material laneMat)
        {
            var chunk = chunkInstance.GetComponent<RoadChunk>();
            foreach (SpawnSlot s in chunk.GetSlots())
            {
                GameObject m = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(m.GetComponent<Collider>());
                m.transform.position = s.transform.position + Vector3.up * 0.4f;
                m.transform.localScale = Vector3.one * 0.6f;
                m.GetComponent<Renderer>().sharedMaterial = slotMats[s.kind];
                yield return m;
            }
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(line.GetComponent<Collider>());
                line.transform.position = chunkInstance.transform.position + new Vector3(side * chunk.halfWidth, 0.02f, chunk.length * 0.5f);
                line.transform.localScale = new Vector3(0.08f, 0.02f, chunk.length);
                line.GetComponent<Renderer>().sharedMaterial = laneMat;
                yield return line;
            }
        }

        static void Shoot(string dir, string file, int w, int h, Vector3 pos, Vector3 lookAt, float fov)
        {
            Camera cam = ArtPreviewUtility.CreateCamera(pos, lookAt, fov, 600f);
            Save(cam, dir, file, w, h);
        }

        /// <summary>Orthographic top-down; <paramref name="screenRight"/> is the world axis pointing right in the image.</summary>
        static void ShootTop(string dir, string file, int w, int h, Vector3 center, float halfHeight, Vector3 screenRight)
        {
            Camera cam = ArtPreviewUtility.CreateCamera(center + Vector3.up * 150f, center, 30f, 600f);
            // Looking straight down: up vector = screenRight rotated -90 degrees around Y.
            cam.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.Cross(screenRight, Vector3.down) * -1f);
            cam.orthographic = true;
            cam.orthographicSize = halfHeight;
            Save(cam, dir, file, w, h);
        }

        static void Save(Camera cam, string dir, string file, int w, int h)
        {
            // Warm-up frame, discarded: in batchmode the first frame after placing objects sometimes samples
            // another kit's colormap (stale texture binding); the second frame is always correct.
            Object.DestroyImmediate(ArtPreviewUtility.Render(cam, w, h));
            Texture2D tex = ArtPreviewUtility.Render(cam, w, h);
            ArtPreviewUtility.SavePng(tex, Path.Combine(dir, file));
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(cam.gameObject);
        }
    }
}
