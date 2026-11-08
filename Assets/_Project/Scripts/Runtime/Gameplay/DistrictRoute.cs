using System.Collections.Generic;
using CityScooter.Track;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// Route order of the districts (v0.3): by <see cref="District.order"/>, then id, without empty entries. Used by the
    /// track generator at runtime and by DistrictSetup when it writes the theme, so both agree.
    /// </summary>
    public static class DistrictRoute
    {
        public static District[] Sort(IEnumerable<District> districts)
        {
            var list = new List<District>();
            if (districts != null)
                foreach (District d in districts)
                    if (d != null && !list.Contains(d)) list.Add(d);
            // Stable: equal orders keep their id order.
            list.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order) : string.CompareOrdinal(a.id ?? "", b.id ?? ""));
            return list.ToArray();
        }

        /// <summary>Index of the district with this id (case and spaces ignored); -1 when there is none.</summary>
        public static int IndexOf(IReadOnlyList<District> route, string id)
        {
            if (route == null || string.IsNullOrEmpty(id)) return -1;
            string want = id.Trim().ToLowerInvariant();
            for (int i = 0; i < route.Count; i++)
                if (route[i] != null && (route[i].id ?? "").Trim().ToLowerInvariant() == want) return i;
            return -1;
        }
    }
}
