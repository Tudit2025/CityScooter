using System;

namespace CityScooter.EditorTools.Art
{
    /// <summary>
    /// Folder conventions for third-party art and generated prefabs (see Assets/_Project/Art/README.md).
    /// Third-party models live in ThirdParty/&lt;Vendor&gt;/&lt;Kit&gt;/Models, their palette texture in .../Textures,
    /// the one shared material per kit in .../Materials.
    /// </summary>
    public static class ArtPaths
    {
        public const string ThirdPartyRoot = "Assets/_Project/Art/ThirdParty";
        public const string KenneyRoot = ThirdPartyRoot + "/Kenney";

        public const string PrefabRoot = "Assets/_Project/Prefabs";
        public const string KitPrefabRoot = PrefabRoot + "/Kit";
        public const string ObstaclePrefabRoot = PrefabRoot + "/Obstacles";
        public const string PickupPrefabRoot = PrefabRoot + "/Pickups";
        public const string RoadPrefabRoot = PrefabRoot + "/Road";

        public static string Normalize(string assetPath) => assetPath.Replace('\\', '/');

        public static bool IsThirdParty(string assetPath) =>
            Normalize(assetPath).StartsWith(ThirdPartyRoot + "/", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Splits "Assets/_Project/Art/ThirdParty/&lt;Vendor&gt;/&lt;Kit&gt;/..." into vendor and kit folder names.
        /// </summary>
        public static bool TryGetKit(string assetPath, out string vendor, out string kit)
        {
            vendor = kit = null;
            string p = Normalize(assetPath);
            if (!IsThirdParty(p)) return false;
            string[] parts = p.Substring(ThirdPartyRoot.Length + 1).Split('/');
            if (parts.Length < 3) return false;
            vendor = parts[0];
            kit = parts[1];
            return true;
        }

        public static string KitFolder(string vendor, string kit) => ThirdPartyRoot + "/" + vendor + "/" + kit;
        public static string KitModelsFolder(string vendor, string kit) => KitFolder(vendor, kit) + "/Models";
        public static string KitColormapPath(string vendor, string kit) => KitFolder(vendor, kit) + "/Textures/colormap.png";
        public static string KitMaterialPath(string vendor, string kit) => KitFolder(vendor, kit) + "/Materials/" + kit + "_Colormap.mat";
        public static string KitPrefabFolder(string kit) => KitPrefabRoot + "/" + kit;
    }
}
