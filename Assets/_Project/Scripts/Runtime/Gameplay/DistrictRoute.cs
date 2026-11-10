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

        // The lines by district id (v0.5). District.line is written from this by DistrictSetup.
        public static readonly string[] SouthLine =
        {
            "quay", "mountsbay", "canningbridge", "murdoch", "cockburn", "aubingrove", "kwinana", "wellard", "rockingham", "warnbro",
        };
        public static readonly string[] EastLine =
        {
            "mciver", "claisebrook", "eastperth", "mtlawley", "maylands", "meltham", "bayswater", "ashfield", "bassendean",
            "successhill", "guildford", "eastguildford", "woodbridge", "midland",
        };

        /// <summary>"south", "east" or "" (the trunk) for a district id.</summary>
        public static string LineOf(string id) =>
            System.Array.IndexOf(SouthLine, id) >= 0 ? "south" : System.Array.IndexOf(EastLine, id) >= 0 ? "east" : "";

        static string LineOf(District d) => d == null || d.line == null ? "" : d.line.Trim().ToLowerInvariant();

        /// <summary>Indices where the lines other than the trunk start (the first district of each line, in route order).
        /// Empty when the route has no lines: it is one loop.</summary>
        public static List<int> Branches(IReadOnlyList<District> route)
        {
            var starts = new List<int>();
            if (route == null) return starts;
            for (int i = 0; i < route.Count; i++)
            {
                string line = LineOf(route[i]);
                if (line.Length > 0 && (i == 0 || LineOf(route[i - 1]) != line)) starts.Add(i);
            }
            return starts;
        }

        /// <summary>
        /// The district after route[index] (v0.5: the route is a trunk with lines). Inside a line: the next one in
        /// order. At the end of the trunk (line ""): the start of one of the other lines, chosen by
        /// <paramref name="pick"/> (0..1, e.g. a random number from the ride's seed, so the same seed takes the same
        /// line). At the end of any other line: back to the first district of the route. A route without lines is the
        /// old loop.
        /// </summary>
        public static int Next(IReadOnlyList<District> route, int index, double pick)
        {
            if (route == null || route.Count == 0) return 0;
            int n = route.Count;
            index = ((index % n) + n) % n;
            string line = LineOf(route[index]);
            if (index + 1 < n && LineOf(route[index + 1]) == line) return index + 1;
            if (line.Length > 0) return 0; // a line ends: the trunk starts again
            List<int> starts = Branches(route);
            if (starts.Count == 0) return (index + 1) % n;
            int choice = (int)(System.Math.Min(System.Math.Max(pick, 0.0), 0.999999) * starts.Count);
            return starts[choice];
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
