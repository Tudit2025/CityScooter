using System.Collections.Generic;
using System.Linq;
using System.Text;
using CityScooter.Track;
using UnityEditor;
using UnityEngine;

namespace CityScooter.EditorTools.Art
{
    /// <summary>
    /// CityScooter/Art/Validate Budget: checks art against the Web-build budgets and the Track data contract.
    /// Every finding is a Debug.Log line starting with "[CityScooter][Budget]"; the last line is a summary.
    /// Batchmode: -executeMethod CityScooter.EditorTools.Art.ArtBudgetValidator.ValidateBatch (exit code 1 on errors).
    /// </summary>
    public static class ArtBudgetValidator
    {
        public const string MenuPath = "CityScooter/Art/Validate Budget";
        public const int MaxTextureSize = 2048;
        public const int PropTriangleBudget = 5000;
        public const int BuildingTriangleBudget = 15000;
        // Suggested, not given by the brief: three chunks are on screen at once on lab PCs.
        public const int ChunkTriangleBudget = 60000;
        public const int ChunkRendererBudget = 80;
        public const int MaterialBudget = 8;
        // Lane volume that must stay empty in a chunk: rider plus jump height above the asphalt.
        public const float LaneClearFrom = 0.15f;
        public const float LaneClearTo = 3f;

        public sealed class Report
        {
            public int Errors, Warnings;
            public readonly List<string> Lines = new List<string>();

            public void Error(string msg) { Errors++; Add("ERROR " + msg, LogType.Error); }
            public void Warn(string msg) { Warnings++; Add("WARN  " + msg, LogType.Warning); }
            public void Info(string msg) { Add("INFO  " + msg, LogType.Log); }

            void Add(string line, LogType type)
            {
                line = "[CityScooter][Budget] " + line;
                Lines.Add(line);
                // Errors/warnings are logged as plain logs too so batch logs stay greppable in one place.
                if (type == LogType.Error) Debug.LogError(line);
                else if (type == LogType.Warning) Debug.LogWarning(line);
                else Debug.Log(line);
            }
        }

        [MenuItem(MenuPath, priority = 120)]
        static void ValidateMenu() => Validate();

        public static void ValidateBatch() => EditorApplication.Exit(Validate().Errors > 0 ? 1 : 0);

        public static Report Validate()
        {
            var r = new Report();
            CheckTextures(r);
            CheckReadWrite(r);
            var materials = new Dictionary<Material, int>();
            CheckPrefabs(r, materials);
            CheckChunks(r);
            ReportMaterials(r, materials);
            string summary = "Summary: " + r.Errors + " error(s), " + r.Warnings + " warning(s).";
            r.Lines.Add("[CityScooter][Budget] " + summary);
            Debug.Log("[CityScooter][Budget] " + summary);
            return r;
        }

        // ------------------------------------------------------------------ textures / meshes

        static void CheckTextures(Report r)
        {
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;
                count++;
                ti.GetSourceTextureWidthAndHeight(out int w, out int h);
                int source = Mathf.Max(w, h);
                int max = ti.maxTextureSize;
                TextureImporterPlatformSettings web = ti.GetPlatformTextureSettings("WebGL");
                if (web.overridden) max = web.maxTextureSize;
                int imported = Mathf.Min(source, max);
                if (imported > MaxTextureSize)
                    r.Error("Texture over " + MaxTextureSize + " px: " + path + " (" + w + "x" + h + ", max size " + max + ")");
                else if (source > MaxTextureSize)
                    r.Info("Texture source is " + w + "x" + h + " but imported at <= " + max + ": " + path);
            }
            r.Info("Textures checked: " + count + " (limit " + MaxTextureSize + " px).");
        }

        static void CheckReadWrite(Report r)
        {
            int models = 0, meshes = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is ModelImporter mi)) continue;
                models++;
                if (mi.isReadable) r.Error("Mesh Read/Write enabled: " + path);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".asset")) continue;
                foreach (Mesh m in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>())
                {
                    meshes++;
                    if (m.isReadable) r.Error("Mesh Read/Write enabled: " + path + " (" + m.name + ")");
                }
            }
            r.Info("Read/Write checked: " + models + " model files, " + meshes + " mesh assets.");
        }

        // ------------------------------------------------------------------ prefabs

        static void CheckPrefabs(Report r, Dictionary<Material, int> materials)
        {
            if (!AssetDatabase.IsValidFolder(ArtPaths.PrefabRoot)) return;
            int count = 0, obstacles = 0, gifts = 0;
            (string path, int tris) maxProp = (null, 0), maxBuilding = (null, 0);
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { ArtPaths.PrefabRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;
                count++;

                foreach (Renderer rend in go.GetComponentsInChildren<Renderer>(true))
                    foreach (Material m in rend.sharedMaterials)
                    {
                        if (m == null) { r.Warn("Missing material on " + path + " / " + rend.name); continue; }
                        materials.TryGetValue(m, out int n);
                        materials[m] = n + 1;
                    }

                bool isChunk = go.GetComponent<RoadChunk>() != null;
                if (!isChunk)
                {
                    int tris = PrefabBuildUtility.CountTriangles(go);
                    bool building = IsBuilding(path);
                    int budget = building ? BuildingTriangleBudget : PropTriangleBudget;
                    if (tris > budget)
                        r.Error("Over triangle budget: " + path + " has " + tris + " tris (" + (building ? "building" : "prop") + " budget " + budget + ")");
                    if (building && tris > maxBuilding.tris) maxBuilding = (path, tris);
                    if (!building && tris > maxProp.tris) maxProp = (path, tris);
                }

                foreach (Obstacle o in go.GetComponentsInChildren<Obstacle>(true))
                {
                    obstacles++;
                    CheckObstacle(r, path, o);
                }
                foreach (Gift g in go.GetComponentsInChildren<Gift>(true))
                {
                    gifts++;
                    CheckGift(r, path, g);
                }
            }
            r.Info("Prefabs checked: " + count + " (" + obstacles + " Obstacle components, " + gifts + " Gift components).");
            r.Info("Largest prop: " + maxProp.path + " (" + maxProp.tris + " tris, budget " + PropTriangleBudget + "); largest building: "
                   + maxBuilding.path + " (" + maxBuilding.tris + " tris, budget " + BuildingTriangleBudget + ").");
        }

        static bool IsBuilding(string prefabPath)
        {
            string p = ArtPaths.Normalize(prefabPath);
            if (!p.StartsWith(ArtPaths.KitPrefabRoot + "/")) return false;
            string[] parts = p.Substring(ArtPaths.KitPrefabRoot.Length + 1).Split('/');
            if (parts.Length < 2) return false;
            KitRule kit = KitRules.Kits.FirstOrDefault(k => k.Kit == parts[0]);
            if (kit == null) return false;
            string model = KitPrefabBuilder.ModelPath(kit.Vendor, kit.Kit, System.IO.Path.GetFileNameWithoutExtension(parts[1]));
            return KitRules.Resolve(model).Category == KitCategory.Building;
        }

        static void CheckObstacle(Report r, string path, Obstacle o)
        {
            string where = path + (o.transform.parent == null ? "" : " / " + o.name);
            Collider[] cols = o.GetComponentsInChildren<Collider>(true);
            if (!cols.Any(c => !c.isTrigger))
            {
                r.Error("Obstacle without a non-trigger collider: " + where);
                return;
            }
            Bounds b = ColliderBounds(o.transform, cols.Where(c => !c.isTrigger));
            if (o.canJumpOver && b.max.y >= KitRules.JumpableHeight)
                r.Warn("Obstacle marked canJumpOver but " + b.max.y.ToString("F2") + " m high (>= " + KitRules.JumpableHeight + "): " + where);
            if (o.spansRoad && b.size.x < 2f * StreetChunkBuilder.HalfWidth)
                r.Warn("Obstacle marked spansRoad but only " + b.size.x.ToString("F2") + " m wide: " + where);
            if (o.spansRoad && !o.canJumpOver)
                r.Error("Obstacle spans the road but cannot be jumped (unavoidable crash): " + where);
        }

        static void CheckGift(Report r, string path, Gift g)
        {
            string where = path + (g.transform.parent == null ? "" : " / " + g.name);
            Collider[] cols = g.GetComponentsInChildren<Collider>(true);
            if (!cols.Any(c => c.isTrigger)) r.Error("Gift without a trigger collider: " + where);
            if (cols.Any(c => !c.isTrigger)) r.Warn("Gift has a non-trigger collider (scooter would bump into it): " + where);
            if (g.GetComponent<SphereCollider>() == null) r.Warn("Gift root has no SphereCollider (contract asks for one): " + where);
        }

        /// <summary>Bounds of BoxColliders in the space of <paramref name="space"/> (other collider types use renderer-free estimates).</summary>
        static Bounds ColliderBounds(Transform space, IEnumerable<Collider> cols)
        {
            bool any = false;
            var b = new Bounds();
            Matrix4x4 toSpace = space.worldToLocalMatrix;
            foreach (Collider c in cols)
            {
                Vector3 center, size;
                if (c is BoxCollider box) { center = box.center; size = box.size; }
                else if (c is SphereCollider s) { center = s.center; size = Vector3.one * s.radius * 2f; }
                else if (c is CapsuleCollider cap) { center = cap.center; size = new Vector3(cap.radius * 2f, cap.height, cap.radius * 2f); }
                else continue;
                Matrix4x4 m = toSpace * c.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var corner = center + Vector3.Scale(size * 0.5f, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 p = m.MultiplyPoint3x4(corner);
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
                    else b.Encapsulate(p);
                }
            }
            return b;
        }

        // ------------------------------------------------------------------ road chunks

        static void CheckChunks(Report r)
        {
            if (!AssetDatabase.IsValidFolder(ArtPaths.PrefabRoot)) return;
            var signatures = new List<(string path, RoadType type, string start, string end)>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { ArtPaths.PrefabRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                RoadChunk chunk = go != null ? go.GetComponent<RoadChunk>() : null;
                if (chunk == null) continue;
                CheckChunk(r, path, chunk, out string start, out string end);
                signatures.Add((path, chunk.roadType, start, end));
            }

            // Seam check: any chunk may follow any other chunk of the same road type.
            foreach (IGrouping<RoadType, (string path, RoadType type, string start, string end)> group in signatures.GroupBy(s => s.type))
            {
                var list = group.ToList();
                bool ok = true;
                foreach (var a in list)
                    foreach (var b in list)
                        if (a.end != b.start)
                        {
                            ok = false;
                            r.Error("Seam mismatch (" + group.Key + "): end of " + a.path + " [" + a.end + "] != start of " + b.path + " [" + b.start + "]");
                        }
                if (ok && list.Count > 0)
                    r.Info("Seams OK for " + list.Count + " " + group.Key + " chunk(s): cross-section " + list[0].start);
            }
        }

        static void CheckChunk(Report r, string path, RoadChunk chunk, out string startSig, out string endSig)
        {
            Transform root = chunk.transform;
            float len = chunk.length, hw = chunk.halfWidth;
            if (len <= 0f || hw <= 0f) r.Error("RoadChunk with length " + len + " / halfWidth " + hw + ": " + path);

            // Spawn slots
            SpawnSlot[] slots = chunk.GetSlots();
            if (slots.Length == 0) r.Error("RoadChunk without SpawnSlots: " + path);
            var kinds = new Dictionary<SpawnKind, int>();
            foreach (SpawnSlot s in slots)
            {
                Vector3 p = root.InverseTransformPoint(s.transform.position);
                kinds.TryGetValue(s.kind, out int n);
                kinds[s.kind] = n + 1;
                if (Mathf.Abs(p.x) > hw || p.z < 0f || p.z > len)
                    r.Error("SpawnSlot outside the lane: " + path + " / " + s.name + " at " + p.ToString("F2"));
                else if (Mathf.Abs(p.x) > hw - 0.5f && s.kind != SpawnKind.Gift)
                    r.Warn("SpawnSlot within 0.5 m of the lane edge (obstacle would overhang the curb): " + path + " / " + s.name);
                if (Mathf.Abs(p.y) > 0.05f) r.Warn("SpawnSlot not on the road surface (y = " + p.y.ToString("F2") + "): " + path + " / " + s.name);
                if (s.kind == SpawnKind.JumpObstacle && Mathf.Abs(p.x) > 0.5f)
                    r.Warn("JumpObstacle slot off the road centre (road-wide prefabs are centred): " + path + " / " + s.name);
            }

            // Lane must stay clear of authored geometry (everything except the road tiles).
            var lane = new Bounds(new Vector3(0f, (LaneClearFrom + LaneClearTo) * 0.5f, len * 0.5f),
                new Vector3(hw * 2f, LaneClearTo - LaneClearFrom, len));
            Transform roadGroup = root.Find("Road");
            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (roadGroup != null && mf.transform.IsChildOf(roadGroup)) continue;
                Bounds b = PrefabBuildUtility.LocalMeshBounds(root, t => t == mf.transform);
                if (!Shrink(b, 0.01f).Intersects(lane)) continue;
                // Coarse box hit (e.g. a lamp arm high above the lane): confirm per triangle.
                if (PrefabBuildUtility.MeshIntersects(mf, root, Shrink(lane, 0.01f)))
                    r.Error("Geometry inside the lane: " + path + " / " + PathOf(mf.transform, root) + " bounds " + b.min.ToString("F1") + ".." + b.max.ToString("F1"));
            }
            foreach (Collider c in root.GetComponentsInChildren<Collider>(true))
            {
                if (roadGroup != null && c.transform.IsChildOf(roadGroup)) continue;
                Bounds b = ColliderBounds(root, new[] { c });
                if (Shrink(b, 0.01f).Intersects(lane))
                    r.Error("Collider inside the lane: " + path + " / " + PathOf(c.transform, root));
            }
            foreach (Obstacle o in root.GetComponentsInChildren<Obstacle>(true))
                r.Info("Authored Obstacle inside chunk (gameplay will treat it as real): " + path + " / " + PathOf(o.transform, root));

            // Road must cover the lane end to end.
            Bounds road = roadGroup != null ? PrefabBuildUtility.LocalMeshBounds(roadGroup.parent, t => t.IsChildOf(roadGroup)) : new Bounds();
            if (roadGroup == null || road.min.z > 0.01f || road.max.z < len - 0.01f || road.min.x > -hw || road.max.x < hw)
                r.Error("Road tiles do not cover x = +-" + hw + ", z = 0.." + len + ": " + path);

            // Cross-section signatures at both ends (road + ground pieces touching z = 0 / z = length).
            startSig = Signature(root, len, atStart: true);
            endSig = Signature(root, len, atStart: false);

            int tris = PrefabBuildUtility.CountTriangles(chunk.gameObject);
            Renderer[] rends = chunk.GetComponentsInChildren<Renderer>(true);
            int mats = rends.SelectMany(x => x.sharedMaterials).Where(m => m != null).Distinct().Count();
            string kindText = string.Join(", ", kinds.OrderBy(k => k.Key).Select(k => k.Key + " " + k.Value));
            r.Info("Chunk " + path + ": " + tris + " tris, " + rends.Length + " renderers, " + mats + " materials, slots " + slots.Length + " (" + kindText + ")");
            if (tris > ChunkTriangleBudget) r.Warn("Chunk over " + ChunkTriangleBudget + " tris: " + path);
            if (rends.Length > ChunkRendererBudget) r.Warn("Chunk over " + ChunkRendererBudget + " renderers: " + path);
        }

        static string Signature(Transform root, float len, bool atStart)
        {
            var parts = new List<string>();
            foreach (string group in new[] { "Road", "Ground" })
            {
                Transform g = root.Find(group);
                if (g == null) continue;
                foreach (MeshFilter mf in g.GetComponentsInChildren<MeshFilter>(true))
                {
                    Bounds b = PrefabBuildUtility.LocalMeshBounds(root, t => t == mf.transform);
                    bool touches = atStart ? b.min.z <= 0.01f : b.max.z >= len - 0.01f;
                    if (!touches) continue;
                    // Only the strip within the standard cross-section matters for the seam (lots may differ).
                    if (b.min.x >= StreetChunkBuilder.FootpathOuter - 0.01f || b.max.x <= -StreetChunkBuilder.FootpathOuter + 0.01f) continue;
                    parts.Add(b.min.x.ToString("F2") + ".." + b.max.x.ToString("F2") + "@" + b.max.y.ToString("F2"));
                }
            }
            parts.Sort(string.CompareOrdinal);
            return string.Join(" ", parts);
        }

        static Bounds Shrink(Bounds b, float by)
        {
            b.Expand(-by * 2f);
            return b;
        }

        static string PathOf(Transform t, Transform root)
        {
            var sb = new StringBuilder(t.name);
            for (Transform p = t.parent; p != null && p != root; p = p.parent) sb.Insert(0, p.name + "/");
            return sb.ToString();
        }

        // ------------------------------------------------------------------ materials

        static void ReportMaterials(Report r, Dictionary<Material, int> materials)
        {
            foreach (KeyValuePair<Material, int> kv in materials.OrderBy(k => k.Key.name))
            {
                string path = AssetDatabase.GetAssetPath(kv.Key);
                bool embedded = path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase);
                string shader = kv.Key.shader != null ? kv.Key.shader.name : "<none>";
                string keywords = kv.Key.shaderKeywords.Length == 0 ? "no keywords" : "keywords " + string.Join(" ", kv.Key.shaderKeywords);
                r.Info("Material " + kv.Key.name + " (" + shader + ", " + keywords + ") used by " + kv.Value + " renderer slot(s): " + path);
                if (embedded) r.Warn("Embedded model material in use (run " + KitPrefabBuilder.MenuPath + " to remap): " + path);
                if (shader.Contains("Hidden/InternalErrorShader") || shader == "<none>") r.Error("Broken shader on material " + path);
            }
            int matFiles = AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project" }).Length;
            string line = "Total materials referenced by prefabs: " + materials.Count + " (budget " + MaterialBudget + "); .mat assets under Assets/_Project: " + matFiles;
            if (materials.Count > MaterialBudget) r.Warn(line);
            else r.Info(line);
        }
    }
}
