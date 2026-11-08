using System;
using System.Collections.Generic;
using System.Linq;
using CityScooter.EditorTools.Art;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using static CityScooter.EditorTools.Districts.ParkRiverKit;

namespace CityScooter.EditorTools.Districts
{
    /// <summary>
    /// Props atlases of Kings Park and Mounts Bay Rd in the style of art-src/comic-city/previews/03-props.png (grid of
    /// orthographic three-quarter views, plain light-blue background, ComicPalette + ComicInk outline): the trees and plants
    /// baked into the chunks, plus the district prefabs (obstacles, animals, people, ramps).
    ///   Batch (needs graphics, no -nographics): -executeMethod CityScooter.EditorTools.Districts.ParkRiverAtlas.RenderBatch
    /// Writes art-src/districts/kingspark/props-atlas.png and art-src/districts/mountsbay/props-atlas.png.
    /// </summary>
    public static class ParkRiverAtlas
    {
        [MenuItem("CityScooter/Districts/Render Kings Park and Mounts Bay Rd props atlases")]
        public static void Render()
        {
            Init();
            var kp = new List<(string, ComicMesh)>();
            ComicMesh M(Action<ComicMesh> build) { var m = new ComicMesh(); build(m); return m; }
            kp.Add(("Gum", M(m => Gum(m, Vector3.zero, 18f, 11))));
            kp.Add(("GumB", M(m => Gum(m, Vector3.zero, 16f, 29))));
            kp.Add(("GumFar", M(m => Gum(m, Vector3.zero, 16f, 5, true))));
            kp.Add(("Balga", M(m => Balga(m, Vector3.zero, 1.2f, 7))));
            kp.Add(("Banksia", M(m => Banksia(m, Vector3.zero, 1.1f, 9))));
            kp.Add(("KangarooPaw", M(m => KangarooPaw(m, Vector3.zero, 1.3f, 3))));
            kp.Add(("Shrub", M(m => Shrub(m, Vector3.zero, 1.2f, Color.Lerp(LeafGreen, Lawn, .3f)))));
            kp.Add(("DaisyBed", M(m => DaisyBed(m, -1.2f, 1.2f, -1.6f, 1.6f, 0f, Rng(4), 12))));
            kp.Add(("Peppermint", M(m => RoundTree(m, Vector3.zero, 9f, Color.Lerp(Lawn, LeafGreen, .5f), 40))));
            kp.Add(("Binoculars", M(m => KingsParkBuilder.Viewer(m, Vector3.zero))));
            kp.Add(("DnaTower", M(m => KingsParkBuilder.DnaTower(m, Vector3.zero))));
            kp.Add(("NameWall", M(m => NameBlock(m, Vector3.zero, "KINGS PARK", .17f, Limestone, new Color(.24f, .36f, .2f)))));
            Sheet("kingspark", kp, new[]
            {
                KingsParkBuilder.PrefabFolder + "/OBS_KingsPark_GumBranch.prefab", KingsParkBuilder.PrefabFolder + "/PED_KingsPark_Magpie.prefab",
                KingsParkBuilder.PrefabFolder + "/RMP_KingsPark_Boardwalk.prefab",
            });
            var mb = new List<(string, ComicMesh)>();
            mb.Add(("Palm", M(m => Palm(m, Vector3.zero, 10f, 3))));
            mb.Add(("PalmB", M(m => Palm(m, Vector3.zero, 8.5f, 8))));
            mb.Add(("RoundTree", M(m => RoundTree(m, Vector3.zero, 9f, Color.Lerp(Lawn, LeafGreen, .5f), 51))));
            mb.Add(("Balga", M(m => Balga(m, Vector3.zero, 1f, 12))));
            mb.Add(("Shrub", M(m => Shrub(m, Vector3.zero, 1f, Color.Lerp(BanksiaLeaf, BushFloor, .3f)))));
            mb.Add(("Boulders", M(m => { var r = Rng(5); for (int i = 0; i < 4; i++) Boulder(m, V(i % 2 * 1.1f, .4f, i / 2 * 1.1f), 1f, r); })));
            mb.Add(("Lamp", M(m => MountsBayBuilder.Lamp(m, Vector3.zero, 1))));
            mb.Add(("Yacht", M(m => MountsBayBuilder.Yacht(m, Vector3.zero, 1f, R.red))));
            mb.Add(("Climber", M(m => MountsBayBuilder.Climber(m, Vector3.zero, R.yellow))));
            mb.Add(("NameWall", M(m => NameBlock(m, Vector3.zero, "MOUNTS BAY RD", .12f, WhiteWall, Water))));
            Sheet("mountsbay", mb, new[]
            {
                MountsBayBuilder.PrefabFolder + "/OBS_MountsBay_WheelieBin.prefab", MountsBayBuilder.PrefabFolder + "/PED_MountsBay_Pelican.prefab",
                MountsBayBuilder.PrefabFolder + "/PED_MountsBay_Cyclist.prefab", MountsBayBuilder.PrefabFolder + "/RMP_MountsBay_RiverWall.prefab",
            });
        }

        public static void RenderBatch()
        {
            try { Render(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static GameObject Specimen(string name, ComicMesh data)
        {
            var go = new GameObject(name);
            Mesh mesh = data.Bake(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = Palette;
            var ink = new GameObject("InkOutline");
            ink.transform.SetParent(go.transform, false);
            ink.AddComponent<MeshFilter>().sharedMesh = mesh;
            ink.AddComponent<MeshRenderer>().sharedMaterial = InkMaterial;
            return go;
        }

        // Exact bounds from the vertices (the saved meshes' bounds are stretched upwards for the GPU city curl).
        static Bounds VertexBounds(GameObject go)
        {
            bool any = false;
            var b = new Bounds();
            foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                foreach (Vector3 v in mf.sharedMesh.vertices)
                {
                    Vector3 p = mf.transform.TransformPoint(v);
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                }
            }
            return b;
        }

        static void Sheet(string id, List<(string name, ComicMesh mesh)> specimens, string[] prefabs)
        {
            ArtPreviewUtility.NewPreviewScene();
            var items = new List<GameObject>();
            foreach (var s in specimens) items.Add(Specimen(s.name, s.mesh));
            foreach (string path in prefabs)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset != null) items.Add((GameObject)PrefabUtility.InstantiatePrefab(asset));
            }
            const int tile = 300, cols = 6;
            int rows = Mathf.CeilToInt(items.Count / (float)cols);
            var sheet = new Texture2D(cols * tile, rows * tile, TextureFormat.RGB24, false);
            sheet.SetPixels(Enumerable.Repeat(new Color(.82f, .87f, .9f), sheet.width * sheet.height).ToArray());
            for (int i = 0; i < items.Count; i++)
            {
                foreach (GameObject other in items) other.SetActive(other == items[i]);
                Bounds b = VertexBounds(items[i]);
                float extent = Mathf.Max(b.size.x, b.size.y, b.size.z);
                var cam = ArtPreviewUtility.CreateCamera(b.center + new Vector3(1f, .65f, 1.4f).normalized * extent * 2f, b.center, 32f, 400f);
                cam.orthographic = true; cam.orthographicSize = Mathf.Max(.6f, extent * .62f);
                Object.DestroyImmediate(ArtPreviewUtility.Render(cam, tile, tile));
                Texture2D tex = ArtPreviewUtility.Render(cam, tile, tile);
                sheet.SetPixels(i % cols * tile, (rows - 1 - i / cols) * tile, tile, tile, tex.GetPixels());
                Object.DestroyImmediate(tex); Object.DestroyImmediate(cam.gameObject);
            }
            foreach (GameObject go in items) Object.DestroyImmediate(go);
            sheet.Apply();
            ArtPreviewUtility.SavePng(sheet, "art-src/districts/" + id + "/props-atlas.png");
            Object.DestroyImmediate(sheet);
            Debug.Log("[ParkRiverAtlas] " + id + ": " + items.Count + " items");
        }
    }
}
