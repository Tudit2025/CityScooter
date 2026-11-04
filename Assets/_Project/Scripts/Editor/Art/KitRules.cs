using System;
using System.IO;

namespace CityScooter.EditorTools.Art
{
    public enum KitCategory
    {
        /// <summary>Drivable road tile. Kit prefab: runs along +Z, asphalt at y = 0, no shadows.</summary>
        Road,
        /// <summary>Flat ground / sidewalk slab. Kit prefab: top at sidewalk height, no shadows.</summary>
        Ground,
        /// <summary>House / shop / tower. Simple BoxCollider; front faces +Z in the kit prefab.</summary>
        Building,
        /// <summary>Street furniture that is only ever set dressing (lamps, signs, parasols). BoxCollider.</summary>
        Prop,
        /// <summary>Trees and plants. BoxCollider around the trunk area only.</summary>
        Foliage,
        /// <summary>Things gameplay can spawn in the lane. BoxCollider + Obstacle.</summary>
        Obstacle,
        /// <summary>Cars. BoxCollider + Obstacle (cannot be jumped).</summary>
        Vehicle,
        /// <summary>Presents. Go to Prefabs/Pickups with Gift + trigger SphereCollider instead of Prefabs/Kit.</summary>
        Pickup,
    }

    /// <summary>Import/prefab rule for one model, or for a family of models when the pattern ends with '*'.</summary>
    public sealed class ModelRule
    {
        public string Pattern;
        /// <summary>Kenney units to metres (ModelImporter.globalScale). 0 = kit default.</summary>
        public float Scale;
        public KitCategory Category;
        /// <summary>null = automatic: true when the prefab is lower than <see cref="KitRules.JumpableHeight"/>.</summary>
        public bool? CanJumpOver;
        /// <summary>Degrees around Y applied to the Model child so the kit prefab follows project axes.</summary>
        public float Yaw;
        /// <summary>Metres applied to the Model child (e.g. sink road tiles so the asphalt is at y = 0).</summary>
        public float OffsetY;
        public float SpawnWeight = 1f;
    }

    public sealed class KitRule
    {
        public string Vendor;
        public string Kit;
        public float DefaultScale;
        public KitCategory DefaultCategory;
        /// <summary>Yaw applied to every model of the kit unless the model rule overrides it (non-zero).</summary>
        public float DefaultYaw;
        public ModelRule[] Models = Array.Empty<ModelRule>();
    }

    /// <summary>The fully resolved rule for one model file.</summary>
    public struct ResolvedRule
    {
        public bool Known;
        public string Vendor;
        public string Kit;
        public string Name;
        public float Scale;
        public KitCategory Category;
        public bool? CanJumpOver;
        public float Yaw;
        public float OffsetY;
        public float SpawnWeight;
    }

    /// <summary>
    /// Single source of truth for how third-party kits are scaled and classified.
    /// Edit this table (not the importer inspector) to change a scale or category, then run
    /// CityScooter/Art/Rebuild Kit Prefabs. Kenney city kits share a 1-unit grid; at x10 a road tile is
    /// 10 m long with exactly 8 m of asphalt (RoadChunk.halfWidth = 4).
    /// </summary>
    public static class KitRules
    {
        /// <summary>Obstacles lower than this can be jumped (Obstacle.canJumpOver) unless the rule says otherwise.</summary>
        public const float JumpableHeight = 1.2f;

        /// <summary>Kenney road tiles: asphalt top is at 0.01 Kenney units (0.1 m at x10), curbs/sidewalk at 0.02.</summary>
        public const float RoadAsphaltHeight = 0.1f;
        public const float CurbHeight = 0.1f; // sidewalk top above asphalt

        public static readonly KitRule[] Kits =
        {
            new KitRule
            {
                Vendor = "Kenney", Kit = "CityKitRoads", DefaultScale = 10f, DefaultCategory = KitCategory.Prop,
                Models = new[]
                {
                    new ModelRule { Pattern = "road-*", Category = KitCategory.Road, Yaw = 90f, OffsetY = -RoadAsphaltHeight },
                    new ModelRule { Pattern = "tile-*", Category = KitCategory.Ground, OffsetY = -RoadAsphaltHeight },
                    new ModelRule { Pattern = "light-*", Category = KitCategory.Prop },
                    new ModelRule { Pattern = "traffic-light", Category = KitCategory.Prop },
                    new ModelRule { Pattern = "road-sign-*", Category = KitCategory.Prop, Scale = 6.5f },
                    new ModelRule { Pattern = "construction-cone", Category = KitCategory.Obstacle, Scale = 8f, CanJumpOver = true },
                    new ModelRule { Pattern = "construction-barrier", Category = KitCategory.Obstacle, Scale = 8f, CanJumpOver = true },
                    new ModelRule { Pattern = "construction-fence", Category = KitCategory.Obstacle, Scale = 6f, CanJumpOver = true },
                    new ModelRule { Pattern = "construction-light", Category = KitCategory.Prop, Scale = 8f },
                    new ModelRule { Pattern = "dumpster", Category = KitCategory.Obstacle, Scale = 6f, CanJumpOver = false },
                },
            },
            new KitRule
            {
                Vendor = "Kenney", Kit = "CityKitCommercial", DefaultScale = 10f, DefaultCategory = KitCategory.Building,
                Models = new[]
                {
                    new ModelRule { Pattern = "detail-*", Category = KitCategory.Prop, Scale = 6f },
                },
            },
            new KitRule
            {
                Vendor = "Kenney", Kit = "CityKitSuburban", DefaultScale = 8f, DefaultCategory = KitCategory.Building,
                Models = new[]
                {
                    new ModelRule { Pattern = "tree-*", Category = KitCategory.Foliage },
                    new ModelRule { Pattern = "planter", Category = KitCategory.Prop, Scale = 6f },
                    new ModelRule { Pattern = "fence*", Category = KitCategory.Prop, Scale = 7f },
                },
            },
            new KitRule
            {
                // Chunky stylised cars: x1.3 gives ~2.0 m wide, 1.7 m high, 3.3-4.0 m long. Imported noses point -Z.
                Vendor = "Kenney", Kit = "CarKit", DefaultScale = 1.3f, DefaultCategory = KitCategory.Vehicle, DefaultYaw = 180f,
                Models = new[]
                {
                    new ModelRule { Pattern = "box", Category = KitCategory.Obstacle, Scale = 1.1f, CanJumpOver = true },
                },
            },
            new KitRule
            {
                Vendor = "Kenney", Kit = "HolidayKit", DefaultScale = 1.3f, DefaultCategory = KitCategory.Prop,
                Models = new[]
                {
                    new ModelRule { Pattern = "present-*", Category = KitCategory.Pickup },
                    // Imported seat faces -Z.
                    new ModelRule { Pattern = "bench*", Category = KitCategory.Obstacle, CanJumpOver = true, Yaw = 180f },
                },
            },
        };

        public static KitRule FindKit(string vendor, string kit)
        {
            foreach (KitRule k in Kits)
                if (string.Equals(k.Vendor, vendor, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(k.Kit, kit, StringComparison.OrdinalIgnoreCase))
                    return k;
            return null;
        }

        /// <summary>Rule for a model asset path; unknown kits get scale 1 and category Prop (Known = false).</summary>
        public static ResolvedRule Resolve(string modelAssetPath)
        {
            var r = new ResolvedRule { Scale = 1f, Category = KitCategory.Prop, SpawnWeight = 1f };
            r.Name = Path.GetFileNameWithoutExtension(modelAssetPath);
            if (!ArtPaths.TryGetKit(modelAssetPath, out r.Vendor, out r.Kit)) return r;
            KitRule kit = FindKit(r.Vendor, r.Kit);
            if (kit == null) return r;

            r.Known = true;
            r.Scale = kit.DefaultScale;
            r.Category = kit.DefaultCategory;
            r.Yaw = kit.DefaultYaw;

            ModelRule best = null;
            int bestScore = -1;
            foreach (ModelRule m in kit.Models)
            {
                int score = Match(m.Pattern, r.Name);
                if (score > bestScore) { best = m; bestScore = score; }
            }
            if (best != null)
            {
                if (best.Scale > 0f) r.Scale = best.Scale;
                r.Category = best.Category;
                r.CanJumpOver = best.CanJumpOver;
                if (best.Yaw != 0f) r.Yaw = best.Yaw;
                r.OffsetY = best.OffsetY;
                r.SpawnWeight = best.SpawnWeight;
            }
            return r;
        }

        /// <summary>-1 = no match. Exact names beat prefixes; longer prefixes beat shorter ones.</summary>
        static int Match(string pattern, string name)
        {
            if (pattern.EndsWith("*", StringComparison.Ordinal))
            {
                string prefix = pattern.Substring(0, pattern.Length - 1);
                return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? prefix.Length : -1;
            }
            return string.Equals(pattern, name, StringComparison.OrdinalIgnoreCase) ? 10000 : -1;
        }
    }
}
