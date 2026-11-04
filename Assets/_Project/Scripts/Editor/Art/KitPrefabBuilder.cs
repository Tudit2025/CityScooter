using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityScooter.Track;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CityScooter.EditorTools.Art
{
    /// <summary>
    /// CityScooter/Art/Rebuild Kit Prefabs:
    /// 1. one shared URP Simple Lit material per kit (created once, never overwritten, so look-dev can tune it);
    /// 2. every kit model remapped to that material and re-imported if its settings drifted from <see cref="KitRules"/>;
    /// 3. Prefabs/Kit/&lt;Kit&gt;/&lt;model&gt;.prefab: root (BoxCollider [+ Obstacle]) + "Model" child normalised to
    ///    project axes (+Z forward, road asphalt at y = 0);
    /// 4. Prefabs/Pickups/Gift_*.prefab (Gift + trigger SphereCollider);
    /// 5. Prefabs/Obstacles/Obstacle_*.prefab (curated lane obstacles + the road-spanning railing).
    /// Batchmode: -executeMethod CityScooter.EditorTools.Art.KitPrefabBuilder.RebuildBatch
    /// </summary>
    public static class KitPrefabBuilder
    {
        public const string MenuPath = "CityScooter/Art/Rebuild Kit Prefabs";
        public const string KitShader = "Universal Render Pipeline/Simple Lit";
        /// <summary>Props/foliage: collider footprint taken from geometry below this height (metres).</summary>
        public const float PropBaseHeight = 2.5f;

        sealed class ObstacleDef
        {
            public string Name;
            public string Kit;
            public string Model;
            /// <summary>Extra yaw on top of the kit-normalised orientation (kit prefab faces +Z).</summary>
            public float Yaw;
            public bool CanJumpOver;
            public float Weight = 1f;
            /// <summary>Lateral offsets of repeated copies (road-spanning railing); null = one copy at x = 0.</summary>
            public float[] Copies;
            public bool SpansRoad;
        }

        // Single-lane obstacles fit inside one spawn-slot spacing (<= 2.3 m wide); the railing covers the whole road.
        static readonly ObstacleDef[] Obstacles =
        {
            new ObstacleDef { Name = "Obstacle_Cone", Kit = "CityKitRoads", Model = "construction-cone", CanJumpOver = true, Weight = 1.5f },
            new ObstacleDef { Name = "Obstacle_Barrier", Kit = "CityKitRoads", Model = "construction-barrier", Yaw = 90f, CanJumpOver = true },
            new ObstacleDef { Name = "Obstacle_Bin", Kit = "CityKitRoads", Model = "dumpster", CanJumpOver = false, Weight = 0.8f },
            new ObstacleDef { Name = "Obstacle_Bench", Kit = "HolidayKit", Model = "bench", Yaw = 180f, CanJumpOver = true, Weight = 0.6f },
            new ObstacleDef { Name = "Obstacle_Box", Kit = "CarKit", Model = "box", Yaw = 20f, CanJumpOver = true },
            new ObstacleDef { Name = "Obstacle_Car", Kit = "CarKit", Model = "sedan", CanJumpOver = false, Weight = 0.5f },
            new ObstacleDef
            {
                Name = "Obstacle_RailingSpan", Kit = "CityKitRoads", Model = "construction-fence", Yaw = 90f,
                CanJumpOver = true, SpansRoad = true, Copies = new[] { -3.375f, -1.125f, 1.125f, 3.375f },
            },
        };

        [MenuItem(MenuPath, priority = 100)]
        static void RebuildMenu() => Rebuild();

        public static void RebuildBatch() => EditorApplication.Exit(Rebuild() ? 0 : 1);

        public struct KitModel
        {
            public string Path;
            public ResolvedRule Rule;
        }

        /// <summary>All model files in ThirdParty/&lt;Vendor&gt;/&lt;Kit&gt;/Models, sorted by path.</summary>
        public static List<KitModel> FindKitModels()
        {
            var list = new List<KitModel>();
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { ArtPaths.ThirdPartyRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("/Models/")) continue;
                list.Add(new KitModel { Path = path, Rule = KitRules.Resolve(path) });
            }
            list.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
            return list;
        }

        public static string KitPrefabPath(ResolvedRule rule) => ArtPaths.KitPrefabFolder(rule.Kit) + "/" + rule.Name + ".prefab";
        public static string GiftPrefabPath(ResolvedRule rule) => ArtPaths.PickupPrefabRoot + "/Gift_" + PrefabBuildUtility.ToPascal(rule.Name) + ".prefab";
        public static string ModelPath(string vendor, string kit, string model) => ArtPaths.KitModelsFolder(vendor, kit) + "/" + model + ".fbx";

        public static bool Rebuild()
        {
            List<KitModel> models = FindKitModels();
            if (models.Count == 0)
            {
                Debug.LogError("[CityScooter] No models found under " + ArtPaths.ThirdPartyRoot);
                return false;
            }

            Dictionary<string, Material> materials = EnsureKitMaterials(models);
            SyncImporters(models, materials);

            int kitPrefabs = 0, gifts = 0;
            var expected = new HashSet<string>();
            foreach (KitModel m in models)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(m.Path);
                if (m.Rule.Category == KitCategory.Pickup)
                {
                    BuildGiftPrefab(m.Rule, asset);
                    gifts++;
                }
                else
                {
                    string path = KitPrefabPath(m.Rule);
                    expected.Add(path);
                    BuildKitPrefab(m.Rule, asset, path);
                    kitPrefabs++;
                }
            }
            DeleteOrphanKitPrefabs(expected);

            int obstacles = 0;
            foreach (ObstacleDef def in Obstacles)
                if (BuildObstaclePrefab(def)) obstacles++;

            AssetDatabase.SaveAssets();
            Debug.Log("[CityScooter] Rebuild Kit Prefabs: " + models.Count + " models, " + materials.Count + " kit materials, "
                      + kitPrefabs + " kit prefabs, " + gifts + " gift prefabs, " + obstacles + " obstacle prefabs.");
            return obstacles == Obstacles.Length;
        }

        // ---------------------------------------------------------------- materials + importers

        static Dictionary<string, Material> EnsureKitMaterials(List<KitModel> models)
        {
            var result = new Dictionary<string, Material>();
            foreach (KitModel m in models)
            {
                string key = m.Rule.Vendor + "/" + m.Rule.Kit;
                if (m.Rule.Vendor == null || result.ContainsKey(key)) continue;
                result[key] = EnsureKitMaterial(m.Rule.Vendor, m.Rule.Kit);
            }
            return result;
        }

        /// <summary>Creates the kit material if missing. An existing material is left alone (look-dev owns tuning).</summary>
        public static Material EnsureKitMaterial(string vendor, string kit)
        {
            string path = ArtPaths.KitMaterialPath(vendor, kit);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtPaths.KitColormapPath(vendor, kit));
            if (tex == null) Debug.LogWarning("[CityScooter] " + vendor + "/" + kit + " has no Textures/colormap.png");

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
            {
                if (tex != null && mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") == null)
                {
                    mat.SetTexture("_BaseMap", tex);
                    EditorUtility.SetDirty(mat);
                }
                return mat;
            }

            Shader shader = Shader.Find(KitShader);
            if (shader == null)
            {
                Debug.LogError("[CityScooter] Shader '" + KitShader + "' not found; is URP installed?");
                return null;
            }
            mat = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color", Color.white);
            // Matte, opaque, no keywords: cheapest Simple Lit variant and identical for every kit, so the
            // SRP Batcher batches all kits together. _SpecularHighlights holds SimpleLitGUI.SpecularSource:
            // 0 = SpecularTextureAndColor, 1 = NoSpecular. With 0, URP's material validation turns on
            // _SPECULAR_COLOR and the broad smoothness-0 highlight washes the palette out towards grey.
            mat.SetFloat("_SpecularHighlights", 1f);
            mat.SetFloat("_Smoothness", 0f);
            mat.SetFloat("_ReceiveShadows", 1f);
            mat.SetFloat("_Surface", 0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetFloat("_Cull", (float)CullMode.Back);
            mat.DisableKeyword("_SPECULAR_COLOR");
            mat.DisableKeyword("_SPECGLOSSMAP");
            mat.enableInstancing = false;
            PrefabBuildUtility.EnsureFolder(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(mat, path);
            Debug.Log("[CityScooter] Created kit material " + path);
            return mat;
        }

        /// <summary>Remaps every source material of each model to its kit material and re-imports drifted models.</summary>
        static void SyncImporters(List<KitModel> models, Dictionary<string, Material> materials)
        {
            var dirty = new List<ModelImporter>();
            foreach (KitModel m in models)
            {
                if (!(AssetImporter.GetAtPath(m.Path) is ModelImporter mi)) continue;
                bool changed = !ThirdPartyArtPostprocessor.ModelSettingsMatch(mi, m.Rule);
                if (changed) ThirdPartyArtPostprocessor.ApplyModelSettings(mi, m.Rule);

                materials.TryGetValue(m.Rule.Vendor + "/" + m.Rule.Kit, out Material kitMat);
                if (kitMat != null)
                {
                    Dictionary<AssetImporter.SourceAssetIdentifier, Object> map = mi.GetExternalObjectMap();
                    var names = new HashSet<string>();
                    foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(m.Path))
                        if (o is Material embedded) names.Add(embedded.name);
                    foreach (AssetImporter.SourceAssetIdentifier id in map.Keys)
                        if (id.type == typeof(Material)) names.Add(id.name);
                    foreach (string n in names)
                    {
                        var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), n);
                        if (map.TryGetValue(id, out Object current) && current == kitMat) continue;
                        mi.AddRemap(id, kitMat);
                        changed = true;
                    }
                }
                if (changed) dirty.Add(mi);
            }

            if (dirty.Count == 0) return;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (ModelImporter mi in dirty) mi.SaveAndReimport();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            Debug.Log("[CityScooter] Re-imported " + dirty.Count + " model(s) with kit material remaps / rule settings.");
        }

        // ---------------------------------------------------------------- kit prefabs

        static void BuildKitPrefab(ResolvedRule rule, GameObject model, string prefabPath)
        {
            GameObject root = PrefabBuildUtility.OpenPrefab(prefabPath);
            try
            {
                var children = new ChildSync(root.transform);
                GameObject inst = children.Instance("Model", model);
                PrefabBuildUtility.SetTransform(inst.transform, new Vector3(0f, rule.OffsetY, 0f), rule.Yaw, Vector3.one);
                children.Finish();

                bool flat = rule.Category == KitCategory.Road || rule.Category == KitCategory.Ground;
                PrefabBuildUtility.SetShadowCasting(inst, flat ? ShadowCastingMode.Off : ShadowCastingMode.On);

                Bounds b = PrefabBuildUtility.LocalMeshBounds(root.transform);
                var box = PrefabBuildUtility.KeepSingle<BoxCollider>(root) ?? root.AddComponent<BoxCollider>();
                box.isTrigger = false;
                if (rule.Category == KitCategory.Prop || rule.Category == KitCategory.Foliage)
                {
                    // Lamp arms, sign boards and canopies overhang; only the pole/trunk footprint is solid.
                    Bounds foot = PrefabBuildUtility.LocalFootprintBounds(root.transform, PropBaseHeight);
                    box.center = foot.center;
                    box.size = foot.size;
                }
                else if (rule.Category == KitCategory.Road)
                {
                    // Drivable slab: top exactly at the asphalt (y = 0); curbs are outside the lane anyway.
                    box.center = new Vector3(b.center.x, -KitRules.RoadAsphaltHeight * 0.5f, b.center.z);
                    box.size = new Vector3(b.size.x, KitRules.RoadAsphaltHeight, b.size.z);
                }
                else
                {
                    box.center = b.center;
                    box.size = b.size;
                }
                PrefabBuildUtility.SyncComponent<SphereCollider>(root, false);

                bool obstacle = rule.Category == KitCategory.Obstacle || rule.Category == KitCategory.Vehicle;
                PrefabBuildUtility.SyncComponent<Obstacle>(root, obstacle, o =>
                {
                    o.canJumpOver = rule.CanJumpOver ?? (rule.Category != KitCategory.Vehicle && b.max.y < KitRules.JumpableHeight);
                    o.spansRoad = b.size.x >= 8f;
                    o.spawnWeight = rule.SpawnWeight;
                });
                PrefabBuildUtility.SyncComponent<Gift>(root, false);
            }
            catch
            {
                PrefabUtility.UnloadPrefabContents(root);
                throw;
            }
            PrefabBuildUtility.SaveAndClose(root, prefabPath);
        }

        static void DeleteOrphanKitPrefabs(HashSet<string> expected)
        {
            if (!AssetDatabase.IsValidFolder(ArtPaths.KitPrefabRoot)) return;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { ArtPaths.KitPrefabRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (expected.Contains(path)) continue;
                Debug.LogWarning("[CityScooter] Deleting kit prefab without a source model: " + path);
                AssetDatabase.DeleteAsset(path);
            }
        }

        // ---------------------------------------------------------------- pickups

        static void BuildGiftPrefab(ResolvedRule rule, GameObject model)
        {
            string path = GiftPrefabPath(rule);
            GameObject root = PrefabBuildUtility.OpenPrefab(path);
            try
            {
                var children = new ChildSync(root.transform);
                GameObject inst = children.Instance("Model", model);
                PrefabBuildUtility.SetTransform(inst.transform, new Vector3(0f, rule.OffsetY, 0f), rule.Yaw, Vector3.one);
                children.Finish();
                PrefabBuildUtility.SetShadowCasting(inst, ShadowCastingMode.On);

                // Only a trigger: gifts must never physically block the scooter.
                PrefabBuildUtility.SyncComponent<BoxCollider>(root, false);
                Bounds b = PrefabBuildUtility.LocalMeshBounds(root.transform);
                var sphere = PrefabBuildUtility.KeepSingle<SphereCollider>(root) ?? root.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.center = b.center;
                // Generous radius (~0.75 m) so children steering with a tilt sensor still collect it.
                sphere.radius = Mathf.Max(0.75f, b.extents.magnitude);

                Gift gift = PrefabBuildUtility.GetOrAdd<Gift>(root);
                gift.points = 10;
                PrefabBuildUtility.SyncComponent<Obstacle>(root, false);
            }
            catch
            {
                PrefabUtility.UnloadPrefabContents(root);
                throw;
            }
            PrefabBuildUtility.SaveAndClose(root, path);
        }

        // ---------------------------------------------------------------- obstacles

        static bool BuildObstaclePrefab(ObstacleDef def)
        {
            string modelPath = ModelPath("Kenney", def.Kit, def.Model);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                Debug.LogError("[CityScooter] " + def.Name + ": model not found at " + modelPath);
                return false;
            }
            ResolvedRule rule = KitRules.Resolve(modelPath);
            string path = ArtPaths.ObstaclePrefabRoot + "/" + def.Name + ".prefab";
            GameObject root = PrefabBuildUtility.OpenPrefab(path);
            try
            {
                var children = new ChildSync(root.transform);
                float[] copies = def.Copies ?? new[] { 0f };
                for (int i = 0; i < copies.Length; i++)
                {
                    GameObject inst = children.Instance(copies.Length == 1 ? "Model" : "Model_" + i, model);
                    PrefabBuildUtility.SetTransform(inst.transform, new Vector3(copies[i], rule.OffsetY, 0f), rule.Yaw + def.Yaw, Vector3.one);
                    PrefabBuildUtility.SetShadowCasting(inst, ShadowCastingMode.On);
                }
                children.Finish();

                // One non-trigger box around everything (Obstacle contract), pivot stays on the road at the slot.
                Bounds b = PrefabBuildUtility.LocalMeshBounds(root.transform);
                PrefabBuildUtility.SyncComponent<SphereCollider>(root, false);
                var box = PrefabBuildUtility.KeepSingle<BoxCollider>(root) ?? root.AddComponent<BoxCollider>();
                box.isTrigger = false;
                box.center = b.center;
                box.size = b.size;

                Obstacle o = PrefabBuildUtility.GetOrAdd<Obstacle>(root);
                o.canJumpOver = def.CanJumpOver;
                o.spansRoad = def.SpansRoad;
                o.spawnWeight = def.Weight;
                PrefabBuildUtility.SyncComponent<Gift>(root, false);

                if (def.CanJumpOver && b.max.y >= KitRules.JumpableHeight)
                    Debug.LogWarning("[CityScooter] " + def.Name + " is marked jumpable but is " + b.max.y.ToString("F2") + " m high.");
            }
            catch
            {
                PrefabUtility.UnloadPrefabContents(root);
                throw;
            }
            PrefabBuildUtility.SaveAndClose(root, path);
            return true;
        }
    }
}
