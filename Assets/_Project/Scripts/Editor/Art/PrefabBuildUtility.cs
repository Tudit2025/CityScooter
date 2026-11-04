using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CityScooter.EditorTools.Art
{
    /// <summary>
    /// Helpers for generated prefabs. Generated prefabs are edited in place (LoadPrefabContents) and children are
    /// matched by name, so re-running a builder keeps GUIDs and fileIDs stable: references from scenes/scripts
    /// survive, and an unchanged rebuild produces no git diff.
    /// </summary>
    public static class PrefabBuildUtility
    {
        public static void EnsureFolder(string assetFolder)
        {
            assetFolder = ArtPaths.Normalize(assetFolder).TrimEnd('/');
            if (AssetDatabase.IsValidFolder(assetFolder)) return;
            string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(assetFolder));
        }

        /// <summary>Opens the prefab for editing, creating an empty one first if it does not exist yet.</summary>
        public static GameObject OpenPrefab(string prefabPath)
        {
            EnsureFolder(Path.GetDirectoryName(prefabPath));
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                var temp = new GameObject(Path.GetFileNameWithoutExtension(prefabPath));
                try { PrefabUtility.SaveAsPrefabAsset(temp, prefabPath); }
                finally { Object.DestroyImmediate(temp); }
            }
            return PrefabUtility.LoadPrefabContents(prefabPath);
        }

        public static void SaveAndClose(GameObject contentsRoot, string prefabPath)
        {
            try
            {
                PrefabUtility.SaveAsPrefabAsset(contentsRoot, prefabPath, out bool ok);
                if (!ok) Debug.LogError("[CityScooter] Failed to save prefab " + prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contentsRoot);
            }
        }

        public static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        /// <summary>Adds/configures the component when wanted, removes it otherwise.</summary>
        public static void SyncComponent<T>(GameObject go, bool wanted, Action<T> configure = null) where T : Component
        {
            T c = go.GetComponent<T>();
            if (!wanted)
            {
                if (c != null) Object.DestroyImmediate(c, true);
                return;
            }
            if (c == null) c = go.AddComponent<T>();
            configure?.Invoke(c);
        }

        /// <summary>Removes all components of type T except the first one; returns the kept one (or null).</summary>
        public static T KeepSingle<T>(GameObject go) where T : Component
        {
            T[] all = go.GetComponents<T>();
            for (int i = 1; i < all.Length; i++) Object.DestroyImmediate(all[i], true);
            return all.Length > 0 ? all[0] : null;
        }

        public static void SetTransform(Transform t, Vector3 localPosition, float yaw, Vector3 localScale)
        {
            t.localPosition = localPosition;
            t.localRotation = Quaternion.Euler(0f, yaw, 0f);
            t.localScale = localScale;
        }

        /// <summary>Exact bounds of all MeshFilter meshes under <paramref name="root"/>, in root-local space.</summary>
        public static Bounds LocalMeshBounds(Transform root, Func<Transform, bool> include = null)
        {
            bool any = false;
            var b = new Bounds();
            Matrix4x4 toRoot = root.worldToLocalMatrix;
            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                if (include != null && !include(mf.transform)) continue;
                Matrix4x4 m = toRoot * mf.transform.localToWorldMatrix;
                Bounds mb = mf.sharedMesh.bounds;
                Vector3 c = mb.center, e = mb.extents;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        c.x + ((i & 1) == 0 ? -e.x : e.x),
                        c.y + ((i & 2) == 0 ? -e.y : e.y),
                        c.z + ((i & 4) == 0 ? -e.z : e.z));
                    Vector3 p = m.MultiplyPoint3x4(corner);
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
                    else b.Encapsulate(p);
                }
            }
            return b;
        }

        /// <summary>
        /// Footprint bounds for colliders of props with overhangs (lamp arms, tree canopies, sign boards):
        /// XZ from the vertices below <paramref name="baseHeight"/>, Y from the full mesh. Falls back to full bounds.
        /// Mesh data is readable in Edit Mode even with Read/Write off.
        /// </summary>
        public static Bounds LocalFootprintBounds(Transform root, float baseHeight)
        {
            Bounds full = LocalMeshBounds(root);
            bool any = false;
            var foot = new Bounds();
            Matrix4x4 toRoot = root.worldToLocalMatrix;
            try
            {
                foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh == null) continue;
                    Matrix4x4 m = toRoot * mf.transform.localToWorldMatrix;
                    foreach (Vector3 v in mf.sharedMesh.vertices)
                    {
                        Vector3 p = m.MultiplyPoint3x4(v);
                        if (p.y > baseHeight) continue;
                        if (!any) { foot = new Bounds(p, Vector3.zero); any = true; }
                        else foot.Encapsulate(p);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[CityScooter] Could not read mesh vertices under " + root.name + ": " + e.Message);
                return full;
            }
            if (!any) return full;
            var min = new Vector3(foot.min.x, full.min.y, foot.min.z);
            var max = new Vector3(foot.max.x, full.max.y, foot.max.z);
            return new Bounds((min + max) * 0.5f, max - min);
        }

        /// <summary>True when any triangle's bounds (in <paramref name="space"/>) intersects <paramref name="box"/>.</summary>
        public static bool MeshIntersects(MeshFilter mf, Transform space, Bounds box)
        {
            Mesh mesh = mf.sharedMesh;
            if (mesh == null) return false;
            Matrix4x4 m = space.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            Vector3[] v = mesh.vertices;
            int[] tri = mesh.triangles;
            for (int i = 0; i + 2 < tri.Length; i += 3)
            {
                var b = new Bounds(m.MultiplyPoint3x4(v[tri[i]]), Vector3.zero);
                b.Encapsulate(m.MultiplyPoint3x4(v[tri[i + 1]]));
                b.Encapsulate(m.MultiplyPoint3x4(v[tri[i + 2]]));
                if (b.Intersects(box)) return true;
            }
            return false;
        }

        public static int CountTriangles(GameObject root)
        {
            long tris = 0;
            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = mf.sharedMesh;
                if (mesh == null) continue;
                for (int s = 0; s < mesh.subMeshCount; s++)
                    if (mesh.GetTopology(s) == MeshTopology.Triangles) tris += mesh.GetIndexCount(s) / 3;
            }
            foreach (SkinnedMeshRenderer smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh mesh = smr.sharedMesh;
                if (mesh == null) continue;
                for (int s = 0; s < mesh.subMeshCount; s++)
                    if (mesh.GetTopology(s) == MeshTopology.Triangles) tris += mesh.GetIndexCount(s) / 3;
            }
            return (int)tris;
        }

        public static void SetShadowCasting(GameObject root, ShadowCastingMode mode)
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
                if (r.shadowCastingMode != mode) r.shadowCastingMode = mode;
        }

        public static string ToPascal(string kebab)
        {
            var sb = new System.Text.StringBuilder();
            foreach (string part in kebab.Split('-', '_', ' '))
                if (part.Length > 0) sb.Append(char.ToUpperInvariant(part[0])).Append(part.Substring(1));
            return sb.ToString();
        }
    }

    /// <summary>
    /// Reuses existing direct children by name (in call order), creates missing ones and deletes leftovers
    /// on <see cref="Finish"/>. Child names must be unique under one parent.
    /// </summary>
    public sealed class ChildSync
    {
        readonly Transform _parent;
        readonly HashSet<Transform> _used = new HashSet<Transform>();
        int _index;

        public ChildSync(Transform parent) { _parent = parent; }

        Transform FindUnused(string name)
        {
            for (int i = 0; i < _parent.childCount; i++)
            {
                Transform c = _parent.GetChild(i);
                if (c.name == name && !_used.Contains(c)) return c;
            }
            return null;
        }

        Transform Claim(Transform t)
        {
            _used.Add(t);
            t.SetSiblingIndex(_index++);
            return t;
        }

        /// <summary>A nested instance of <paramref name="source"/> (prefab or model asset).</summary>
        public GameObject Instance(string name, GameObject source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source), "Missing source asset for child " + name);
            string sourcePath = AssetDatabase.GetAssetPath(source);
            Transform t = FindUnused(name);
            if (t != null && !(PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)
                               && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == sourcePath))
            {
                Object.DestroyImmediate(t.gameObject);
                t = null;
            }
            if (t == null)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(source, _parent);
                go.name = name;
                t = go.transform;
            }
            return Claim(t).gameObject;
        }

        /// <summary>A plain GameObject (not a prefab instance).</summary>
        public GameObject Plain(string name)
        {
            Transform t = FindUnused(name);
            if (t != null && PrefabUtility.IsPartOfPrefabInstance(t.gameObject))
            {
                Object.DestroyImmediate(t.gameObject);
                t = null;
            }
            if (t == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(_parent, false);
                t = go.transform;
            }
            return Claim(t).gameObject;
        }

        public void Finish()
        {
            for (int i = _parent.childCount - 1; i >= 0; i--)
            {
                Transform c = _parent.GetChild(i);
                if (!_used.Contains(c)) Object.DestroyImmediate(c.gameObject);
            }
        }
    }
}
