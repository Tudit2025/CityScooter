using System.Collections.Generic;
using CityScooter.Track;
using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// Lays road chunks end to end along +Z ahead of the rider, recycles them behind, and fills their
    /// SpawnSlots with obstacles and gifts according to difficulty. Uses the TrackTheme's prefabs, or
    /// grey-box placeholders when the theme is empty. Every row keeps at least one free lane unless it
    /// holds a road-wide jump obstacle.
    /// </summary>
    [DefaultExecutionOrder(-40)] // moving cars move before the rider sweeps for collisions
    public sealed class TrackGenerator : MonoBehaviour
    {
        public TrackTheme theme;
        public GameTuning tuning;
        public Transform rider;
        [Tooltip("Metres of track kept built ahead of the rider.")]
        public float buildAhead = 240f;
        [Tooltip("Metres kept behind the rider before recycling (camera looks back a little).")]
        public float keepBehind = 30f;
        [Tooltip("0 = different track every run.")]
        public int seed;
        [Tooltip("Ordered themes: also build the previous chunk behind the start so the chase camera never sees a cut edge.")]
        public bool buildLeadIn;

        sealed class Placed
        {
            public object Key;
            public GameObject Instance;
        }

        sealed class Active
        {
            public object Key;
            public RoadChunk Chunk;
            public float StartZ;
            public readonly List<Placed> Spawned = new List<Placed>();
            public District District; // ---- Districts (v0.3): the district this chunk belongs to (null when districts are off)
        }

        // Hazard patterns: static entries wait until the chunk under them exists; moving ones until the rider is near.
        sealed class PendingEntry
        {
            public object Key;
            public float X, Z, Yaw, MeetZ;
            public Mover Mover;
            public int Pattern;
        }

        sealed class Moving
        {
            public object Key;
            public GameObject Instance;
            public float Speed, X;
            public int Pattern;
        }

        // What each scheduled pattern has put on the road, so a program's event can replace one nobody has reached yet.
        struct PatternItem
        {
            public int Pattern;
            public Active Chunk;
            public GameObject Instance;
        }
        readonly List<PatternItem> _patternItems = new List<PatternItem>();
        int _patternSerial, _lastPattern;

        // Boulevard sections: In -> full-width chunks -> Out, planned at a street cycle boundary and laid in order.
        readonly Queue<RoadChunk> _section = new Queue<RoadChunk>();
        System.Random _sectionRng = new System.Random(1);
        float _wideFrom = float.MinValue, _wideUntil = float.MinValue, _nextSectionZ = float.MinValue;
        int _streetIndex, _sectionStreetIndex = int.MinValue;

        /// <summary>Boulevard sections planned since the last reset (tests).</summary>
        public int BoulevardSections { get; private set; }
        /// <summary>World z where the full-width part of the latest boulevard starts and ends (tests).</summary>
        public Vector2 LastBoulevardRange => new Vector2(_wideFrom, _wideUntil);
        /// <summary>A hazard pattern was scheduled at this world z (tests, debugging).</summary>
        public event System.Action<HazardPattern, float> PatternScheduled;

        readonly List<PendingEntry> _pending = new List<PendingEntry>();
        readonly List<PendingEntry> _pendingMovers = new List<PendingEntry>();
        readonly List<Moving> _moving = new List<Moving>();
        float _reservedFrom = float.MinValue, _reservedUntil = float.MinValue, _nextHazardZ;
        RiderController _riderController;

        readonly List<Active> _active = new List<Active>();
        readonly Dictionary<object, Stack<GameObject>> _pool = new Dictionary<object, Stack<GameObject>>();
        readonly List<SpawnSlot> _rowScratch = new List<SpawnSlot>();
        readonly SortedDictionary<int, List<SpawnSlot>> _rows = new SortedDictionary<int, List<SpawnSlot>>();
        System.Random _rng = new System.Random(1);
        Transform _poolRoot;
        float _nextZ;
        float _nextY;
        float _runStartZ;
        int _chunkCounter;
        int _runSeed;
        object _lastChunkKey;
        bool _collisionTransformsDirty;

        GameTuning T => tuning != null ? tuning : GameTuning.Default;

        public int ActiveChunkCount => _active.Count;
        public int MovingCount => _moving.Count;
        /// <summary>Start and end (world z) of the latest hazard pattern, for tests.</summary>
        public Vector2 LastHazardRange => new Vector2(_reservedFrom, _reservedUntil);

        /// <summary>Clears everything and builds fresh track starting at <paramref name="startZ"/>.</summary>
        public void ResetTrack(float startZ)
        {
            for (int i = _active.Count - 1; i >= 0; i--) Recycle(i);
            foreach (Moving m in _moving) Release(m.Key, m.Instance);
            _moving.Clear();
            _pending.Clear();
            _pendingMovers.Clear();
            _patternItems.Clear();
            _lastPattern = 0;
            _reservedFrom = _reservedUntil = float.MinValue;
            _nextHazardZ = startZ + T.hazardStartDistance;
            _runSeed = seed != 0 ? seed : System.Environment.TickCount;
            _rng = new System.Random(_runSeed);
            _nextZ = startZ;
            _nextY = 0f;
            _runStartZ = startZ;
            _chunkCounter = 0;
            _lastChunkKey = null;
            _section.Clear();
            _sectionRng = new System.Random(_runSeed ^ 0x5bd1e995); // own stream: the street content stays as it was
            _wideFrom = _wideUntil = _nextSectionZ = float.MinValue;
            _sectionStreetIndex = int.MinValue;
            BoulevardSections = 0;
            ResetDistricts(startZ); // ---- Districts (v0.3)
            if (_route != null)
            {
                // ---- Districts (v0.3): the chunk behind the start comes from the start district.
                RoadChunk[] own = _route[_dIndex].chunks;
                RoadChunk previous = buildLeadIn ? own[own.Length - 1] : null;
                if (previous != null)
                {
                    _forcedChunk = previous;
                    _nextZ -= Mathf.Max(1f, previous.length);
                    _nextY = -previous.endHeight;
                    _chunkCounter = -1;
                }
            }
            else if (buildLeadIn && theme != null && theme.orderedChunks && theme.HasChunks)
            {
                RoadChunk previous = theme.chunks[theme.chunks.Length - 1];
                if (previous != null)
                {
                    _nextZ -= Mathf.Max(1f, previous.length);
                    _nextY = -previous.endHeight;
                    _chunkCounter = -1;
                }
            }
            _streetIndex = _route != null ? 0 : _chunkCounter;
            ResetRamps(startZ); // ---- Ramps and air (v0.3)
            BuildAhead(startZ);
            SyncCollisionTransforms();
        }

        void Update()
        {
            if (rider == null) return;
            float z = rider.position.z;
            TrimDistrictSpans(z); // ---- Districts (v0.3)
            BuildAhead(z);
            while (_active.Count > 1 && _active[0].StartZ + _active[0].Chunk.length < z - keepBehind) Recycle(0);
            UpdateHazards(z, Time.deltaTime);
            SyncCollisionTransforms();
        }

        void SyncCollisionTransforms()
        {
            if(Mover.MovedThisFrame){_collisionTransformsDirty=true;Mover.MovedThisFrame=false;}
            if(!_collisionTransformsDirty)return;
            Physics.SyncTransforms();_collisionTransformsDirty=false;
        }

        void BuildAhead(float riderZ)
        {
            int guard = 0;
            while (_nextZ < riderZ + buildAhead && guard++ < 64) SpawnChunk();
        }

        // The chunk under world z; the first and last chunks extend past the built track (chase camera, cars far ahead).
        Active ActiveAt(float z)
        {
            if(_active.Count==0)return null;
            for(int i=0;i<_active.Count;i++)if(z<_active[i].StartZ+_active[i].Chunk.length)return _active[i];
            return _active[_active.Count-1];
        }

        /// <summary>The built chunk under world z and its start z (tests, tools).</summary>
        public RoadChunk ChunkAt(float z, out float startZ)
        {
            Active a = ActiveAt(z);
            startZ = a != null ? a.StartZ : 0f;
            return a?.Chunk;
        }

        /// <summary>Half width of the road, excluding sidewalks (boulevard transitions change it along z).</summary>
        public float HalfWidthAt(float z)
        {
            Active a = ActiveAt(z);
            return a != null ? a.Chunk.HalfWidthAt(z - a.StartZ) : PlaceholderTrack.HalfWidth;
        }
        public float RideableHalfWidthAt(float z)
        {
            Active a = ActiveAt(z);
            return a != null ? a.Chunk.RideableHalfWidthAt(z - a.StartZ) : PlaceholderTrack.HalfWidth;
        }
        public float SidewalkOffsetAt(float x,float z)
        {
            Active a = ActiveAt(z);
            return a != null ? a.Chunk.SidewalkOffsetAt(x, z - a.StartZ) : 0f;
        }
        public float SurfaceHeightAt(float x,float z)=>SurfaceHeightAt(z)+SidewalkOffsetAt(x,z);

        // Extrapolate the end chunks for the chase camera just behind / ahead of built track.
        public float SurfaceHeightAt(float z)
        {
            if (_active.Count == 0) return 0f;
            Active a = _active[_active.Count - 1];
            for (int i = 0; i < _active.Count; i++)
                if (z < _active[i].StartZ + _active[i].Chunk.length) { a = _active[i]; break; }
            return a.Chunk.transform.position.y + a.Chunk.HeightAt(z - a.StartZ);
        }

        public float SurfacePitchAt(float z) => -Mathf.Atan2(SurfaceHeightAt(z + 0.1f) - SurfaceHeightAt(z), 0.1f) * Mathf.Rad2Deg;

        void SpawnChunk()
        {
            RoadChunk planned = NextSectionChunk();
            if (planned == null && _route != null) planned = NextDistrictChunk(); // ---- Districts (v0.3): lead-in, entry, exit
            object key = planned != null ? planned.gameObject : PickChunk();
            if (planned == null) _streetIndex++; // boulevard chunks do not advance the street rhythm
            GameObject go = Get(key, () => CreateChunk(key));
            var chunk = go.GetComponent<RoadChunk>();
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(new Vector3(0f, _nextY, _nextZ), Quaternion.identity);
            go.GetComponent<StreetDressing>()?.ApplyLayout(_runSeed, _chunkCounter);
            _collisionTransformsDirty=true;
            var active = new Active { Key = key, Chunk = chunk, StartZ = _nextZ, District = _route != null ? _route[_dIndex] : null };
            _active.Add(active);
            AuthoredOn(chunk); // ---- Districts (v0.3): the chunk's own ramps and gaps, before anything is placed on it
            PlaceGateway(active); // ---- Districts (v0.3)
            Populate(active);
            PlacePending(active);
            PlaceStreetLife(active);
            _nextZ += Mathf.Max(1f, chunk.length);
            _nextY += chunk.endHeight;
            _chunkCounter++;
            _lastChunkKey = key;
            TryPlaceRamp(); // ---- Ramps and air (v0.3): once its landing zone is built
            _lastEndHalfWidth = chunk.HalfWidthAt(chunk.length); // ---- Districts (v0.3)
        }

        object PickChunk()
        {
            if (_route == null && (theme == null || !theme.HasChunks))
            {
                int variant = _chunkCounter % 3 == 2 ? 2 : _chunkCounter % 2;
                return "placeholder-chunk-" + variant;
            }
            // ---- Districts (v0.3): the current district's own chunks, else the theme's
            RoadChunk[] chunks = _route != null ? _route[_dIndex].chunks : theme.chunks;
            bool ordered = _route != null ? _route[_dIndex].orderedChunks : theme.orderedChunks;
            int level = T.DifficultyLevel(_nextZ - _runStartZ);
            if (_route != null) return PickDistrictChunk(chunks, ordered, level); // ---- Districts (v0.3): also matching road widths
            if (ordered)
            {
                for (int i = 0; i < chunks.Length; i++)
                {
                    RoadChunk candidate = chunks[((_streetIndex + i) % chunks.Length + chunks.Length) % chunks.Length];
                    if (candidate != null && candidate.minDifficulty <= level) return candidate.gameObject;
                }
                return FirstChunk(chunks);
            }
            float total = 0f;
            foreach (RoadChunk c in chunks)
                if (c != null && c.minDifficulty <= level && (object)c.gameObject != _lastChunkKey) total += Mathf.Max(0f, c.weight);
            if (total <= 0f) return FirstChunk(chunks);
            double pick = _rng.NextDouble() * total;
            foreach (RoadChunk c in chunks)
            {
                if (c == null || c.minDifficulty > level || (object)c.gameObject == _lastChunkKey) continue;
                pick -= Mathf.Max(0f, c.weight);
                if (pick <= 0) return c.gameObject;
            }
            return FirstChunk(chunks);
        }

        // ---- Districts (v0.3): like the theme's picking, but a chunk must start as wide as the last one ended (districts mix
        // 4-lane and street chunks: CBD Widen / Terrace / Narrow / Mall). Ordered: the first fitting chunk of the cycle from
        // the current position (the cycle continues after it). If none fits, as before.
        object PickDistrictChunk(RoadChunk[] chunks, bool ordered, int level)
        {
            int n = chunks.Length;
            if (ordered)
            {
                for (int pass = 0; pass < 2; pass++)
                    for (int i = 0; i < n; i++)
                    {
                        RoadChunk candidate = chunks[((_streetIndex + i) % n + n) % n];
                        if (candidate == null || candidate.minDifficulty > level || (pass == 0 && !WidthFits(candidate))) continue;
                        if (pass == 0) _streetIndex += i;
                        return candidate.gameObject;
                    }
                return FirstChunk(chunks);
            }
            for (int pass = 0; pass < 2; pass++)
            {
                float total = 0f;
                foreach (RoadChunk c in chunks)
                    if (c != null && c.minDifficulty <= level && (object)c.gameObject != _lastChunkKey && (pass == 1 || WidthFits(c))) total += Mathf.Max(0f, c.weight);
                if (total <= 0f) continue;
                double pick = _rng.NextDouble() * total;
                foreach (RoadChunk c in chunks)
                {
                    if (c == null || c.minDifficulty > level || (object)c.gameObject == _lastChunkKey || (pass == 0 && !WidthFits(c))) continue;
                    pick -= Mathf.Max(0f, c.weight);
                    if (pick <= 0) return c.gameObject;
                }
            }
            return FirstChunk(chunks);
        }

        float _lastEndHalfWidth = float.NaN; // ---- Districts (v0.3): road half width where the last laid chunk ends
        bool WidthFits(RoadChunk c) => float.IsNaN(_lastEndHalfWidth) || Mathf.Abs(c.HalfWidthAt(0f) - _lastEndHalfWidth) < 0.01f;

        static object FirstChunk(RoadChunk[] chunks)
        {
            foreach (RoadChunk c in chunks)
                if (c != null) return c.gameObject;
            return "placeholder-chunk-0";
        }

        // ---------------- boulevard sections: the street widens to 4 lanes for a while, then narrows back

        RoadChunk NextSectionChunk()
        {
            if (_section.Count == 0) TryPlanSection();
            return _section.Count > 0 ? _section.Dequeue() : null;
        }

        void TryPlanSection()
        {
            if (theme == null || !theme.HasChunks || !theme.HasBoulevard || !T.boulevardSections) return;
            // ---- Districts (v0.3): only in districts that allow it, and the whole section stays inside the district.
            int cycle = theme.chunks.Length;
            float room = float.MaxValue;
            if (_route != null && !DistrictSectionRoom(out cycle, out room)) return;
            // Only where the ordered street cycle starts again, so its near / open rhythm stays whole; never two
            // sections back to back (after the Out chunk the street index is still on the same boundary).
            if (_streetIndex <= 0 || _streetIndex % cycle != 0 || _streetIndex == _sectionStreetIndex) return;
            if (_nextZ - _runStartZ < T.boulevardMinDistance || _nextZ < _nextSectionZ) return;
            // A hazard pattern authored for the street still runs into this stretch: try again next cycle.
            if (_reservedUntil >= _nextZ) return;
            if (_sectionRng.NextDouble() >= T.boulevardChance) return;
            int min = Mathf.Max(1, T.boulevardChunks.x), count = _sectionRng.Next(min, Mathf.Max(min, T.boulevardChunks.y) + 1);
            if (room < float.MaxValue) // ---- Districts (v0.3): fewer wide chunks so that In, wide and Out fit before the district ends
            {
                float wideLength = 1f;
                foreach (RoadChunk c in theme.boulevardChunks) wideLength = Mathf.Max(wideLength, c.length);
                int fit = Mathf.FloorToInt((room - Mathf.Max(1f, theme.boulevardIn.length) - Mathf.Max(1f, theme.boulevardOut.length)) / wideLength);
                if (fit < min) return;
                count = Mathf.Min(count, fit);
            }
            _sectionStreetIndex = _streetIndex;
            int first = _sectionRng.Next(theme.boulevardChunks.Length);
            _section.Enqueue(theme.boulevardIn);
            float z = _nextZ + Mathf.Max(1f, theme.boulevardIn.length);
            _wideFrom = z;
            for (int i = 0; i < count; i++)
            {
                RoadChunk c = theme.boulevardChunks[(first + i) % theme.boulevardChunks.Length];
                _section.Enqueue(c);
                z += Mathf.Max(1f, c.length);
            }
            _wideUntil = z;
            _section.Enqueue(theme.boulevardOut);
            _nextSectionZ = z + Mathf.Max(1f, theme.boulevardOut.length) + T.boulevardGap;
            BoulevardSections++;
        }

        bool OnBoulevard(float z) => z >= _wideFrom - 0.01f && z < _wideUntil - 0.01f;

        GameObject CreateChunk(object key)
        {
            if (key is GameObject prefab) return Instantiate(prefab);
            string s = (string)key;
            int variant = s[s.Length - 1] - '0';
            return PlaceholderTrack.CreateChunk(variant, _rng.Next(1, 9999)).gameObject;
        }

        void Populate(Active a)
        {
            float chunkDistance = a.StartZ - _runStartZ;
            float d01 = T.Difficulty01(chunkDistance);
            int level = T.DifficultyLevel(chunkDistance);
            float obstacleChance = Mathf.Lerp(T.obstacleChanceStart, T.obstacleChanceMax, d01);
            _filling = a.District; // ---- Districts (v0.3): obstacles, gifts and patterns from this chunk's district
            TryScheduleHazard(a, level, d01);

            _rows.Clear();
            foreach (SpawnSlot slot in a.Chunk.GetSlots())
            {
                int row = Mathf.RoundToInt(slot.transform.localPosition.z * 2f);
                if (!_rows.TryGetValue(row, out List<SpawnSlot> list)) _rows[row] = list = new List<SpawnSlot>();
                list.Add(slot);
            }

            foreach (KeyValuePair<int, List<SpawnSlot>> row in _rows)
            {
                float rowDistance = chunkDistance + row.Key * 0.5f;
                float rowZ = a.StartZ + row.Key * 0.5f;
                if (rowZ >= _reservedFrom && rowZ <= _reservedUntil) continue; // a hazard pattern owns this stretch
                bool safe = rowDistance < T.safeStartDistance;
                List<SpawnSlot> slots = row.Value;

                SpawnSlot jumpSlot = slots.Find(s => s.kind == SpawnKind.JumpObstacle);
                if (jumpSlot != null && !safe && level >= T.jumpObstacleMinLevel && _rng.NextDouble() < T.jumpObstacleChance)
                {
                    Place(a, JumpObstacleKey(), jumpSlot.transform.localPosition);
                    continue; // a road-wide railing owns the whole row
                }

                _rowScratch.Clear();
                foreach (SpawnSlot s in slots)
                    if (s.kind != SpawnKind.JumpObstacle && !Blocked(a.Chunk, s.transform.localPosition.x, s.transform.localPosition.z)) _rowScratch.Add(s); // ---- Districts (v0.3): not on an authored ramp / gap
                Shuffle(_rowScratch);
                int maxObstacles = Mathf.Max(0, _rowScratch.Count - 1);
                int placedObstacles = 0;
                foreach (SpawnSlot s in _rowScratch)
                {
                    Vector3 p = s.transform.localPosition;
                    bool canObstacle = s.kind == SpawnKind.Obstacle || s.kind == SpawnKind.Any;
                    if (!safe && canObstacle && placedObstacles < maxObstacles && _rng.NextDouble() < obstacleChance)
                    {
                        Place(a, ObstacleKey(), p);
                        placedObstacles++;
                    }
                    else if ((s.kind == SpawnKind.Gift || s.kind == SpawnKind.Any) && _rng.NextDouble() < T.giftChance * (s.kind == SpawnKind.Gift ? 1.6f : 0.6f))
                    {
                        Place(a, GiftKey(), p);
                    }
                }
            }
        }

        object ObstacleKey()
        {
            Obstacle[] obstacles = Own(_filling != null ? _filling.obstacles : null, theme != null ? theme.obstacles : null); // ---- Districts (v0.3)
            if (obstacles != null && obstacles.Length > 0)
            {
                Obstacle o = PickWeighted(obstacles);
                if (o != null) return o.gameObject;
            }
            return _rng.NextDouble() < 0.7 ? "placeholder-obstacle-0" : "placeholder-obstacle-1";
        }

        object JumpObstacleKey()
        {
            Obstacle[] jumps = Own(_filling != null ? _filling.jumpObstacles : null, theme != null ? theme.jumpObstacles : null); // ---- Districts (v0.3)
            if (jumps != null && jumps.Length > 0)
            {
                Obstacle o = PickWeighted(jumps);
                if (o != null) return o.gameObject;
            }
            return "placeholder-railing";
        }

        object GiftKey()
        {
            Gift[] gifts = Own(_filling != null ? _filling.gifts : null, theme != null ? theme.gifts : null); // ---- Districts (v0.3)
            if (gifts != null && gifts.Length > 0)
            {
                Gift g = gifts[_rng.Next(gifts.Length)];
                if (g != null) return g.gameObject;
            }
            return "placeholder-gift";
        }

        Obstacle PickWeighted(Obstacle[] items)
        {
            float total = 0f;
            foreach (Obstacle o in items) if (o != null) total += Mathf.Max(0f, o.spawnWeight);
            double pick = _rng.NextDouble() * total;
            foreach (Obstacle o in items)
            {
                if (o == null) continue;
                pick -= Mathf.Max(0f, o.spawnWeight);
                if (pick <= 0) return o;
            }
            return null;
        }

        void Place(Active a, object key, Vector3 localPos, float yaw = 0f, bool followSlope = false)
        {
            GameObject go = Get(key, () => CreateSpawnable(key));
            go.transform.SetParent(a.Chunk.transform, false);
            go.transform.localPosition = localPos;
            float pitch = followSlope ? -Mathf.Atan2(a.Chunk.endHeight, Mathf.Max(1f, a.Chunk.length)) * Mathf.Rad2Deg : 0f;
            go.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
            go.SetActive(true);
            a.Spawned.Add(new Placed { Key = key, Instance = go });
        }

        // ---------------- street life: people strolling or standing on the sidewalks

        readonly Collider[] _overlap = new Collider[4];

        void PlaceStreetLife(Active a)
        {
            RoadChunk c = a.Chunk;
            GameObject[] people = Own(a.District != null ? a.District.pedestrians : null, theme != null ? theme.pedestrians : null); // ---- Districts (v0.3)
            // Not on boulevard transitions: the sidewalk moves sideways under people walking along it.
            if (theme == null || people == null || people.Length == 0 || c.sidewalkWidth <= 1f || c.ChangesWidth) return;
            int count = _rng.Next(Mathf.RoundToInt(T.sidewalkPeople.x), Mathf.RoundToInt(T.sidewalkPeople.y) + 1);
            count = Mathf.Min(count, T.maxPeople - Pedestrian.Active.Count);
            if (count <= 0) return;
            Physics.SyncTransforms(); // this chunk's sidewalk props were just laid out
            _collisionTransformsDirty = false;
            for (int i = 0, tries = 0; i < count && tries < count * 4; tries++)
            {
                float side = _rng.NextDouble() < 0.5 ? -1f : 1f;
                float x = side * Mathf.Lerp(c.halfWidth + 0.8f, c.halfWidth + c.sidewalkWidth - 0.6f, (float)_rng.NextDouble());
                float z = Mathf.Lerp(2f, Mathf.Max(3f, c.length - 2f), (float)_rng.NextDouble());
                if (Blocked(c, x, z)) continue; // ---- Districts (v0.3): nobody stands over water, a drop or on a ramp
                Vector3 local = new Vector3(x, c.HeightAt(z) + c.SidewalkOffsetAt(x), z);
                Vector3 world = c.transform.TransformPoint(local);
                // Not inside a bench, a bin, a bus shelter or another person.
                if (Physics.OverlapBoxNonAlloc(world + Vector3.up * 0.95f, new Vector3(0.5f, 0.8f, 0.6f), _overlap, Quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Ignore) > 0) continue;
                GameObject prefab = people[_rng.Next(people.Length)];
                if (prefab == null) continue;
                Place(a, prefab, local, 0f, true);
                GameObject go = a.Spawned[a.Spawned.Count - 1].Instance;
                float walk = _rng.NextDouble() < T.sidewalkPeopleWalking ? (_rng.NextDouble() < 0.5 ? -1f : 1f) * Mathf.Lerp(0.7f, 1.5f, (float)_rng.NextDouble()) : 0f;
                walk = AnimalPace(prefab, walk); // ---- Districts (v0.3)
                go.GetComponent<Pedestrian>()?.Configure(walk);
                Physics.SyncTransforms();
                i++;
            }
        }

        // ---------------- hazard patterns

        void TryScheduleHazard(Active a, int level, float d01)
        {
            if (theme == null) return;
            // Patterns are authored for one road width: never on a boulevard transition, and on a boulevard only
            // when the whole pattern fits inside its full-width part.
            if (a.Chunk.ChangesWidth) return;
            if (AuthoredOn(a.Chunk).Any) return; // ---- Districts (v0.3): never on a chunk with its own ramp or road gap
            bool wide = OnBoulevard(a.StartZ);
            // ---- Districts (v0.3): a chunk wider than the street (Mounts Bay's divided road) also uses the 4-lane rules;
            // planned boulevards keep the boulevard list, other chunks use their district's own patterns first.
            bool wideChunk = wide || a.Chunk.halfWidth > StreetHalfWidth + 0.01f;
            HazardPattern[] own = a.District != null ? a.District.hazards : null;
            bool hasBoulevardList = theme.boulevardHazards != null && theme.boulevardHazards.Length > 0;
            HazardPattern[] list = wide && hasBoulevardList ? theme.boulevardHazards
                : own != null && own.Length > 0 ? own
                : wideChunk && hasBoulevardList ? theme.boulevardHazards : theme.hazards;
            if (list == null || list.Length == 0) return;
            if (a.StartZ < _nextHazardZ) return;
            if (a.StartZ - 1f < _rampZone.y + T.rampMinGap * 0.5f) return; // ---- Ramps and air (v0.3): not in a ramp's landing zone
            if (_rng.NextDouble() >= Mathf.Lerp(T.hazardChanceStart, T.hazardChanceMax, d01))
            {
                _nextHazardZ = a.StartZ + Mathf.Max(1f, a.Chunk.length);
                return;
            }
            float start = a.StartZ + 5f;
            float room = wide ? _wideUntil - 6f - start : float.MaxValue;
            if (!DistrictPatternRoom(a, start, ref room)) return; // ---- Districts (v0.3): not across a district boundary
            HazardPattern p = PickPattern(list, level, a.Chunk.halfWidth, room, IsMall(a.Chunk));
            if (p == null) return;
            SchedulePattern(p, start, p.allowMirror && _rng.NextDouble() < 0.5, Mathf.Max(0f, a.Chunk.halfWidth - p.roadHalfWidth));
        }

        /// <summary>
        /// Programs ("do:car", "do:roadworks", ...): place a pattern at least <paramref name="ahead"/> metres in front of
        /// the rider, after any pattern already there, inside the built street and never across a boulevard transition.
        /// False when there is no room right now.
        /// </summary>
        public bool TryPlacePatternAhead(HazardPattern p, float ahead)
        {
            if (p == null || rider == null) return false;
            float riderZ = rider.position.z, want = riderZ + ahead;
            // The road ahead is often already booked by a random pattern. If nobody has reached it yet, the program's
            // event goes first: that pattern is taken off the road. Otherwise the event comes right after it.
            if (_lastPattern != 0 && _reservedUntil + 8f > want && _reservedFrom > riderZ + ahead * 0.8f)
            {
                CancelPattern(_lastPattern);
                _reservedFrom = _reservedUntil = float.MinValue;
            }
            float start = Mathf.Max(want, _reservedUntil + 8f);
            for (int tries = 0; tries < 6; tries++)
            {
                float end = start + p.length;
                if (start > riderZ + buildAhead + 150f) return false; // too far to count as "now"
                if (RampZoneOverlap(start - 6f, end + 6f, out float afterRamp)) { start = afterRamp + 8f; continue; } // ---- Ramps and air (v0.3)
                bool moved = false;
                // Built chunks can be checked; chunks laid later never start a boulevard section inside a reserved pattern.
                for (float z = start - 6f; z <= end + 6f && z < _nextZ; z += 5f)
                {
                    RoadChunk c = ChunkAt(z, out float chunkStart);
                    if (c == null || !(c.ChangesWidth || (IsMall(c) && HasMovers(p)))) continue; // ---- Districts (v0.3): cars not in a mall
                    start = chunkStart + c.length + 8f;
                    moved = true;
                    break;
                }
                if (moved) continue;
                PlacePattern(p, start, p.allowMirror && _rng.NextDouble() < 0.5);
                return true;
            }
            return false;
        }

        // Take a scheduled pattern off the road: its waiting entries and cars, and what it already placed.
        void CancelPattern(int id)
        {
            _pending.RemoveAll(e => e.Pattern == id);
            _pendingMovers.RemoveAll(e => e.Pattern == id);
            for (int i = _patternItems.Count - 1; i >= 0; i--)
            {
                PatternItem item = _patternItems[i];
                if (item.Pattern != id) continue;
                _patternItems.RemoveAt(i);
                int at = item.Chunk.Spawned.FindIndex(s => s.Instance == item.Instance);
                if (at < 0) continue;
                Release(item.Chunk.Spawned[at].Key, item.Instance);
                item.Chunk.Spawned.RemoveAt(at);
            }
            for (int i = _moving.Count - 1; i >= 0; i--)
                if (_moving[i].Pattern == id) { Release(_moving[i].Key, _moving[i].Instance); _moving.RemoveAt(i); }
            _collisionTransformsDirty = true;
        }

        /// <summary>Places a whole hazard pattern starting at world z (tests, scripted moments). Built chunks get
        /// their entries now; moving cars appear when the rider gets near, as in a normal run.</summary>
        public void PlacePattern(HazardPattern p, float startZ, bool mirror = false)
        {
            // Chunks already built may hold random obstacles and gifts in this stretch: the pattern owns it now.
            foreach (Active a in _active)
                for (int i = a.Spawned.Count - 1; i >= 0; i--)
                {
                    Placed s = a.Spawned[i];
                    float z = s.Instance != null ? s.Instance.transform.position.z : float.MinValue;
                    if (z < startZ - 6f || z > startZ + p.length + 6f) continue;
                    if (a.District != null && a.District.gateway != null && ReferenceEquals(s.Key, a.District.gateway)) continue; // ---- Districts (v0.3): the gateway stays
                    Release(s.Key, s.Instance);
                    a.Spawned.RemoveAt(i);
                }
            SchedulePattern(p, startZ, mirror && p.allowMirror, Mathf.Max(0f, HalfWidthAt(startZ) - p.roadHalfWidth));
            foreach (Active a in _active) PlacePending(a);
            Physics.SyncTransforms();
        }

        // sidewalkShift: the road is this much wider than the pattern was authored for (boulevard); entries beyond the
        // authored kerb (signs, people on the sidewalk) move out with the kerb, entries on the road stay in their lanes.
        void SchedulePattern(HazardPattern p, float start, bool mirror, float sidewalkShift = 0f)
        {
            int id = _lastPattern = ++_patternSerial;
            _reservedFrom = start - 6f;
            _reservedUntil = start + p.length + 6f;
            _nextHazardZ = _reservedUntil + T.hazardGap;
            foreach (HazardPattern.Entry e in p.entries)
            {
                GameObject prefab = mirror && e.mirrorPrefab != null ? e.mirrorPrefab : e.prefab;
                if (prefab == null) continue;
                float x = mirror ? -e.x : e.x;
                if (sidewalkShift > 0f && Mathf.Abs(x) > p.roadHalfWidth + 0.01f) x += Mathf.Sign(x) * sidewalkShift;
                var pending = new PendingEntry
                {
                    Key = prefab, X = x, Z = start + e.z, Yaw = mirror ? -e.yaw : e.yaw,
                    MeetZ = start + p.meetAtZ, Mover = prefab.GetComponent<Mover>(), Pattern = id,
                };
                (pending.Mover != null ? _pendingMovers : _pending).Add(pending);
            }
            PatternScheduled?.Invoke(p, start);
            if (p.turnDirection != 0)
            {
                // The turn is fixed in the world now, so it shows up in the distance like the rest of the pattern.
                if (_riderController == null && rider != null) _riderController = rider.GetComponent<RiderController>();
                float speed = _riderController != null ? Mathf.Max(T.startSpeed, _riderController.Speed) : T.startSpeed;
                FakeTurnDirector.Instance?.RequestTurn(start + p.turnAtZ, mirror ? -p.turnDirection : p.turnDirection, p.turnStrength, Mathf.Max(T.turnMinLength, speed * p.turnSeconds));
            }
        }

        // Patterns for this road width (never wider than the road) that fit in the room left (boulevard).
        HazardPattern PickPattern(HazardPattern[] list, int level, float roadHalfWidth, float room, bool noTraffic = false)
        {
            bool Fits(HazardPattern h) => h != null && h.minLevel <= level && h.roadHalfWidth <= roadHalfWidth + 0.01f && h.length <= room
                && !(noTraffic && HasMovers(h)); // ---- Districts (v0.3): no cars in a pedestrian mall
            float total = 0f;
            foreach (HazardPattern h in list) if (Fits(h)) total += Mathf.Max(0f, h.weight);
            if (total <= 0f) return null;
            double pick = _rng.NextDouble() * total;
            foreach (HazardPattern h in list)
            {
                if (!Fits(h)) continue;
                pick -= Mathf.Max(0f, h.weight);
                if (pick <= 0) return h;
            }
            return null;
        }

        void PlacePending(Active a)
        {
            float end = a.StartZ + Mathf.Max(1f, a.Chunk.length);
            for (int i = 0; i < _pending.Count; i++)
            {
                PendingEntry e = _pending[i];
                if (e.Z < a.StartZ || e.Z >= end) continue;
                float local = e.Z - a.StartZ;
                if (Blocked(a.Chunk, e.X, local)) { _pending.RemoveAt(i--); continue; } // ---- Districts (v0.3): not on a ramp or over a gap
                Place(a, e.Key, new Vector3(e.X, a.Chunk.HeightAt(local) + a.Chunk.SidewalkOffsetAt(e.X, local), local), e.Yaw, true);
                _patternItems.Add(new PatternItem { Pattern = e.Pattern, Chunk = a, Instance = a.Spawned[a.Spawned.Count - 1].Instance });
                _pending.RemoveAt(i--);
            }
        }

        // Cars keep moverKerbGap from the kerb: one in an outer boulevard lane merges in where the road narrows.
        // Cars authored inside that gap (all street traffic) keep their x.
        float MoverX(float x, float z)
        {
            float limit = Mathf.Max(0f, HalfWidthAt(z) - T.moverKerbGap);
            return Mathf.Abs(x) > limit + 0.05f ? Mathf.Sign(x) * limit : x;
        }

        void UpdateHazards(float riderZ, float dt)
        {
            if (_riderController == null && rider != null) _riderController = rider.GetComponent<RiderController>();
            float riderSpeed = _riderController != null ? Mathf.Max(T.startSpeed, _riderController.Speed) : T.startSpeed;

            // Moving cars appear a few seconds before they meet the rider, placed so that they meet near MeetZ.
            float lead = Mathf.Max(160f, riderSpeed * T.moverLeadSeconds);
            for (int i = 0; i < _pendingMovers.Count; i++)
            {
                PendingEntry e = _pendingMovers[i];
                if (riderZ < e.MeetZ - lead) continue;
                _pendingMovers.RemoveAt(i--);
                if (MallAt(e.MeetZ)) continue; // ---- Districts (v0.3): no traffic in a pedestrian mall
                float speed = e.Mover.fractionOfRiderSpeed > 0f ? riderSpeed * e.Mover.fractionOfRiderSpeed : e.Mover.speed;
                float t = Mathf.Max(0.5f, (e.MeetZ - riderZ) / riderSpeed);
                // Keep it inside the stretch where cars live (a slow rider makes the meeting time long), and never on top of the rider.
                float z = Mathf.Clamp(e.Z - speed * t, riderZ + 40f, riderZ + buildAhead + 100f);
                if (MallAt(z)) continue; // ---- Districts (v0.3)
                GameObject go = Get(e.Key, () => CreateSpawnable(e.Key));
                go.transform.SetParent(transform, true);
                float x = MoverX(e.X, z);
                go.transform.SetPositionAndRotation(new Vector3(x, SurfaceHeightAt(x, z), z), Quaternion.Euler(SurfacePitchAt(z), e.Yaw, 0f));
                go.SetActive(true);
                _moving.Add(new Moving { Key = e.Key, Instance = go, Speed = speed, X = e.X, Pattern = e.Pattern });
                _collisionTransformsDirty = true;
            }

            for (int i = 0; i < _moving.Count; i++)
            {
                Moving m = _moving[i];
                if (m.Instance == null) { _moving.RemoveAt(i--); continue; }
                Vector3 p = m.Instance.transform.position;
                p.z += m.Speed * dt;
                p.x = MoverX(m.X, p.z);
                p.y = SurfaceHeightAt(p.x, p.z);
                if (p.z < riderZ - keepBehind - 20f || p.z > riderZ + buildAhead + 120f)
                {
                    Release(m.Key, m.Instance);
                    _moving.RemoveAt(i--);
                    continue;
                }
                m.Instance.transform.position = p;
                _collisionTransformsDirty = true;
            }

        }

        /// <summary>Puts an obstacle at a world position on the current track (tests, scripted moments).</summary>
        public GameObject PlaceAt(object key, Vector3 worldPos)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                Active a = _active[i];
                if (worldPos.z >= a.StartZ && worldPos.z < a.StartZ + a.Chunk.length)
                {
                    Place(a, key, a.Chunk.transform.InverseTransformPoint(worldPos));
                    Physics.SyncTransforms();
                    return a.Spawned[a.Spawned.Count - 1].Instance;
                }
            }
            return null;
        }

        static GameObject CreateSpawnable(object key)
        {
            if (key is GameObject prefab) return Instantiate(prefab);
            switch ((string)key)
            {
                case "placeholder-obstacle-0": return PlaceholderTrack.CreateObstacle(0).gameObject;
                case "placeholder-obstacle-1": return PlaceholderTrack.CreateObstacle(1).gameObject;
                case "placeholder-railing": return PlaceholderTrack.CreateRailing().gameObject;
                default: return PlaceholderTrack.CreateGift().gameObject;
            }
        }

        void Recycle(int index)
        {
            Active a = _active[index];
            _active.RemoveAt(index);
            _patternItems.RemoveAll(it => it.Chunk == a);
            foreach (Placed p in a.Spawned) Release(p.Key, p.Instance);
            a.Spawned.Clear();
            Release(a.Key, a.Chunk.gameObject);
        }

        GameObject Get(object key, System.Func<GameObject> create)
        {
            if (_pool.TryGetValue(key, out Stack<GameObject> stack))
            {
                while (stack.Count > 0)
                {
                    GameObject go = stack.Pop();
                    if (go == null) continue;
                    go.SetActive(true);
                    return go;
                }
            }
            return create();
        }

        void Release(object key, GameObject go)
        {
            if (go == null) return;
            if (_poolRoot == null)
            {
                _poolRoot = new GameObject("Pool").transform;
                _poolRoot.SetParent(transform, false);
            }
            go.SetActive(false);
            go.transform.SetParent(_poolRoot, false);
            if (!_pool.TryGetValue(key, out Stack<GameObject> stack)) _pool[key] = stack = new Stack<GameObject>();
            stack.Push(go);
        }

        // ---------------- Ramps and air (v0.3, system B): ramps on the road, a cleared landing zone, gifts in the air
        // A ramp is placed once the road under its whole flight is built (the run-up, the predicted landing at the
        // expected speed, plus rampLandingMargin): never on a boulevard transition, never in a hazard pattern, at
        // least rampMinGap from other ramps and road gaps. Random obstacles on the ramp and anywhere on the road in the
        // landing zone are taken away; gifts hang along the predicted flight, high enough that only a flying rider
        // reaches them. The generator never places road gaps (only authored chunks have them).

        /// <summary>Ramp prefabs (root has Ramp) the generator may place on the road at world z: the district's own
        /// (District.ramps) when it has any, else the theme's generic ones.</summary>
        Ramp[] RampChoices(float z)
        {
            District d = RampDistrict(z);
            if (d != null && d.ramps != null && d.ramps.Length > 0 && System.Array.TrueForAll(d.ramps, r => r != null)) return d.ramps;
            return theme != null ? theme.ramps : null;
        }

        /// <summary>How many ramps at world z: the district's rampFrequency (1 normal, 0 none, 2 twice as many; divides
        /// rampSpacing). 1 without districts.</summary>
        float RampFrequency(float z)
        {
            District d = RampDistrict(z);
            return d != null ? Mathf.Max(0f, d.rampFrequency) : 1f;
        }

        // The built chunk's district, else the planned one (null when districts are off).
        District RampDistrict(float z) => DistrictForChunkAt(z) ?? DistrictAt(z);

        const float RampLaneX = 2.6f;
        System.Random _rampRng = new System.Random(3);
        float _nextRampZ;
        Vector2 _rampZone = new Vector2(float.MinValue, float.MinValue);
        readonly List<Vector2> _rampZones = new List<Vector2>();

        /// <summary>Ramps placed by the generator (and do:ramp) since the last reset (tests).</summary>
        public int RampsPlaced { get; private set; }
        /// <summary>World z from the start of each placed ramp's run-up to the end of its landing zone (tests).</summary>
        public IReadOnlyList<Vector2> RampZones => _rampZones;
        /// <summary>A ramp was placed: the ramp and its zone (start of the run-up, end of the landing zone).</summary>
        public event System.Action<Ramp, Vector2> RampPlaced;

        // Every hazard pattern's reserved stretch (the generator itself only remembers the latest one).
        readonly List<Vector2> _patternZones = new List<Vector2>();
        bool _rampsHooked;

        void ResetRamps(float startZ)
        {
            if (!_rampsHooked)
            {
                _rampsHooked = true;
                PatternScheduled += (p, s) =>
                {
                    _patternZones.Add(new Vector2(s - 6f, s + p.length + 6f));
                    if (_patternZones.Count > 24) _patternZones.RemoveAt(0);
                };
            }
            _patternZones.Clear();
            _rampRng = new System.Random(_runSeed ^ 0x2545f491); // own stream: the street content stays as it was
            _nextRampZ = startZ + T.rampFirstDistance;
            _rampZone = new Vector2(float.MinValue, float.MinValue);
            _rampZones.Clear();
            RampsPlaced = 0;
        }

        void TryPlaceRamp()
        {
            if (_active.Count == 0 || T.rampChance <= 0f) return;
            // Close to the far end of the built road (out of sight), after the previous ramp's spacing.
            float start = Mathf.Max(_nextRampZ, _active[0].StartZ + 4f, _nextZ - 140f);
            Ramp[] choices = RampChoices(start);
            float freq = RampFrequency(start);
            if (choices == null || choices.Length == 0) return;
            if (freq <= 0f) { _nextRampZ = start + 40f; return; } // no ramps in this district: look again further on
            Ramp prefab = choices[_rampRng.Next(choices.Length)];
            if (prefab == null) return;
            float speed = ExpectedRampSpeed(start);
            for (int tries = 0; tries < 12; tries++)
            {
                float end = RampZoneEnd(prefab, start, speed);
                if (end > _nextZ - 1f) return; // the landing is not built yet: try again with the next chunk
                if (!RampZoneFree(start - 6f, end, out float after)) { start = Mathf.Max(start + 10f, after + 4f); continue; }
                if (_rampRng.NextDouble() >= T.rampChance) { _nextRampZ = start + 40f; return; }
                PlaceRamp(prefab, start, LaneFor(prefab, start, _rampRng.Next(3) - 1), speed, end);
                _nextRampZ = end + Mathf.Lerp(T.rampSpacing.x, Mathf.Max(T.rampSpacing.x, T.rampSpacing.y), (float)_rampRng.NextDouble()) / freq;
                return;
            }
            _nextRampZ = start; // nothing fitted: look further on next time
        }

        /// <summary>
        /// Programs ("do:ramp"): a ramp at least <paramref name="ahead"/> metres in front of the rider, in the lane
        /// nearest the rider, with its landing zone cleared. False when there is no room in the built road right now.
        /// </summary>
        public bool TryPlaceRampAhead(float ahead)
        {
            if (rider == null) return false;
            Ramp[] choices = RampChoices(rider.position.z + ahead);
            if (choices == null || choices.Length == 0) choices = theme != null ? theme.ramps : null;
            if (choices == null || choices.Length == 0) return false;
            Ramp prefab = choices[_rampRng.Next(choices.Length)];
            if (prefab == null) return false;
            float start = rider.position.z + ahead;
            float speed = ExpectedRampSpeed(start);
            int lane = Mathf.Clamp(Mathf.RoundToInt(rider.position.x / RampLaneX), -1, 1);
            for (int tries = 0; tries < 10; tries++)
            {
                float end = RampZoneEnd(prefab, start, speed);
                if (end > _nextZ - 1f) return false;
                if (!RampZoneFree(start - 6f, end, out float after)) { start = Mathf.Max(start + 10f, after + 4f); continue; }
                PlaceRamp(prefab, start, LaneFor(prefab, start, lane), speed, end);
                Physics.SyncTransforms();
                return true;
            }
            return false;
        }

        /// <summary>Places a ramp prefab at world z (tests, scripted moments): run-up from <paramref name="startZ"/>, in the
        /// lane at <paramref name="x"/>, landing zone cleared for the rider's expected speed. Null when off the built road.</summary>
        public Ramp PlaceRampAt(Ramp prefab, float startZ, float x)
        {
            if (prefab == null || ActiveAt(startZ) == null || startZ < _active[0].StartZ) return null;
            float speed = ExpectedRampSpeed(startZ);
            Ramp r = PlaceRamp(prefab, startZ, x, speed, Mathf.Min(_nextZ, RampZoneEnd(prefab, startZ, speed)));
            Physics.SyncTransforms();
            return r;
        }

        float ExpectedRampSpeed(float z)
        {
            if (_riderController == null && rider != null) _riderController = rider.GetComponent<RiderController>();
            float now = _riderController != null ? _riderController.Speed : 0f;
            float run = _riderController != null ? _riderController.RunTime : 0f;
            float target = Mathf.Lerp(T.startSpeed, T.MaxSpeedAt(run), T.Difficulty01(z - _runStartZ));
            return Mathf.Max(T.startSpeed, now, target);
        }

        // The predicted flight off this ramp at this speed (what RiderController does, RampFlight): seconds in the
        // air, take-off speed upwards, gravity, speed at the lip (the climb costs some), metres flown.
        float PredictFlight(Ramp r, float speed, out float vy, out float g, out float lipSpeed, out float metres)
        {
            g = RampFlight.Gravity(r, T);
            metres = RampFlight.Distance(r, speed, T, out float seconds, out vy, out lipSpeed);
            return seconds;
        }

        float RampZoneEnd(Ramp r, float start, float speed)
        {
            PredictFlight(r, speed, out _, out _, out _, out float metres);
            return start + r.Length + metres + T.rampLandingMargin;
        }

        float LaneFor(Ramp r, float start, int lane)
        {
            if (r.lanes != null && r.lanes.Length > 0 && System.Array.IndexOf(r.lanes, lane) < 0) lane = r.lanes[_rampRng.Next(r.lanes.Length)];
            float half = HalfWidthAt(start), x = Mathf.Clamp(lane, -1, 1) * RampLaneX, room = Mathf.Max(0f, half - r.width * 0.5f - 0.1f);
            // 4-lane road (district or boulevard): lane centres at +-2 and +-6, never on the painted median.
            if (half > StreetHalfWidth + 0.01f) x = lane < 0 ? (_rampRng.Next(2) == 0 ? -6f : -2f) : lane > 0 ? (_rampRng.Next(2) == 0 ? 2f : 6f) : (_rampRng.Next(2) == 0 ? -2f : 2f);
            return Mathf.Clamp(x, -room, room);
        }

        // Free for a ramp: no width change, no hazard pattern, no other ramp or road gap nearby. after = where to try next.
        bool RampZoneFree(float from, float to, out float after)
        {
            after = from;
            // Never on a boulevard transition, and never on a district chunk with its own (authored) ramp or gap.
            foreach (Active c in _active)
                if (c.StartZ < to && c.StartZ + c.Chunk.length > from && (c.Chunk.ChangesWidth || HasAuthoredAir(c.Key)))
                { after = c.StartZ + c.Chunk.length; return false; }
            if (from < _reservedUntil + T.rampMinGap * 0.5f && to > _reservedFrom - T.rampMinGap * 0.5f) { after = _reservedUntil + T.rampMinGap * 0.5f; return false; }
            foreach (Vector2 p in _patternZones)
                if (from < p.y + T.rampMinGap * 0.5f && to > p.x - T.rampMinGap * 0.5f) after = Mathf.Max(after, p.y + T.rampMinGap * 0.5f);
            foreach (PatternItem it in _patternItems)
            {
                float z = it.Instance != null ? it.Instance.transform.position.z : float.MinValue;
                if (z > from - 8f && z < to + 8f) { after = Mathf.Max(after, z + 8f); }
            }
            foreach (PendingEntry e in _pending) if (e.Z > from - 8f && e.Z < to + 8f) after = Mathf.Max(after, e.Z + 8f);
            foreach (PendingEntry e in _pendingMovers) if (e.MeetZ > from - 8f && e.MeetZ < to + 8f) after = Mathf.Max(after, e.MeetZ + 8f);
            if (after > from) return false;
            if (RampZoneOverlap(from - T.rampMinGap, to + T.rampMinGap, out float zoneEnd)) { after = zoneEnd + T.rampMinGap; return false; }
            for (int i = 0; i < Ramp.Active.Count; i++)
            {
                Ramp r = Ramp.Active[i];
                if (r == null) continue;
                float r0 = Mathf.Min(r.transform.position.z, r.LipZ), r1 = Mathf.Max(r.transform.position.z, r.LipZ) + 40f; // about a flight beyond
                if (r1 + T.rampMinGap > from && r0 - T.rampMinGap < to) { after = r1 + T.rampMinGap; return false; }
            }
            for (int i = 0; i < RoadGap.Active.Count; i++)
            {
                RoadGap g = RoadGap.Active[i];
                if (g == null) continue;
                float g0 = g.transform.position.z, g1 = g.EndZ;
                if (g1 + T.rampMinGap > from && g0 - T.rampMinGap < to) { after = g1 + T.rampMinGap; return false; }
            }
            return true;
        }

        readonly Dictionary<object, bool> _authoredAir = new Dictionary<object, bool>();

        // The chunk prefab itself contains a Ramp or RoadGap (e.g. CHN_Matilda_Inlet), not counting ramps the generator
        // put on an instance of it.
        bool HasAuthoredAir(object chunkKey)
        {
            if (!(chunkKey is GameObject prefab) || prefab == null) return false;
            if (!_authoredAir.TryGetValue(chunkKey, out bool has))
                _authoredAir[chunkKey] = has = prefab.GetComponentInChildren<Ramp>(true) != null || prefab.GetComponentInChildren<RoadGap>(true) != null;
            return has;
        }

        // Does [from, to] overlap a generated ramp's zone? zoneEnd = the end of the last one it overlaps.
        bool RampZoneOverlap(float from, float to, out float zoneEnd)
        {
            zoneEnd = float.MinValue;
            foreach (Vector2 zone in _rampZones)
                if (from < zone.y && to > zone.x) zoneEnd = Mathf.Max(zoneEnd, zone.y);
            return zoneEnd > float.MinValue;
        }

        Ramp PlaceRamp(Ramp prefab, float start, float x, float speed, float end)
        {
            Active a = ActiveAt(start);
            if (a == null) return null;
            float lip = start + prefab.Length;
            // Nothing on the ramp or just before it in its lane, and no obstacles anywhere on the road where it lands.
            foreach (Active c in _active)
                for (int i = c.Spawned.Count - 1; i >= 0; i--)
                {
                    GameObject go = c.Spawned[i].Instance;
                    if (go == null || go.GetComponent<Ramp>() != null) continue;
                    Vector3 p = go.transform.position;
                    if (p.z < start - 6f || p.z > end) continue;
                    bool onRamp = p.z <= lip + 1.5f && Mathf.Abs(p.x - x) < prefab.width * 0.5f + 1.2f;
                    bool landing = p.z > lip && Mathf.Abs(p.x) <= HalfWidthAt(p.z) + 0.5f && go.GetComponent<Obstacle>() != null;
                    if (!onRamp && !landing) continue;
                    Release(c.Spawned[i].Key, go);
                    c.Spawned.RemoveAt(i);
                }
            float local = start - a.StartZ;
            Place(a, prefab.gameObject, new Vector3(x, a.Chunk.HeightAt(local) + a.Chunk.SidewalkOffsetAt(x, local), local), 0f, true);
            Ramp placed = a.Spawned[a.Spawned.Count - 1].Instance.GetComponent<Ramp>();
            PlaceAirGifts(prefab, lip, x, speed);
            var zone = new Vector2(start - 6f, end);
            _rampZone = zone;
            _rampZones.Add(zone);
            if (_rampZones.Count > 16) _rampZones.RemoveAt(0);
            RampsPlaced++;
            _collisionTransformsDirty = true;
            RampPlaced?.Invoke(placed, zone);
            return placed;
        }

        // Gifts along the predicted flight, only where the rider's feet are 2.5 m or more above the road (a normal jump
        // reaches about 1.1 m): collectable only by flying.
        void PlaceAirGifts(Ramp r, float lip, float x, float speed)
        {
            int n = T.rampAirGifts;
            if (n <= 0) return;
            float total = PredictFlight(r, speed, out float vy, out float g, out float lipSpeed, out _);
            for (int k = 1; k <= n; k++)
            {
                float t = total * k / (n + 1f);
                float feet = r.height + vy * t - 0.5f * g * t * t;
                if (feet < 2.5f) continue;
                float z = lip + RampFlight.Forward(lipSpeed, t, T);
                Active c = ActiveAt(z);
                if (c == null || z >= _nextZ) continue;
                float local = z - c.StartZ;
                Place(c, RampGiftKey(), new Vector3(x, c.Chunk.HeightAt(local) + c.Chunk.SidewalkOffsetAt(x, local) + feet + 0.35f, local));
            }
        }

        object RampGiftKey()
        {
            if (theme != null && theme.gifts != null && theme.gifts.Length > 0)
            {
                Gift g = theme.gifts[_rampRng.Next(theme.gifts.Length)];
                if (g != null) return g.gameObject;
            }
            return "placeholder-gift";
        }

        // ---------------- ---- Districts (v0.3): the UWA -> Perth CBD route, districts in District.order, looping.
        // A ride starts inside the start district (tuning.startDistrict); each district lasts District.length metres,
        // then its exit chunk, the next district's entry chunk (with its gateway) and that district's own chunks.
        // One district alone has no boundaries: its street goes on like the theme's own street did.

        /// <summary>One district's stretch of road: from where it starts (StartZ, the boundary) to the next one (EndZ).</summary>
        public struct DistrictSpan
        {
            public District District;
            /// <summary>0 for the start district, then +1 at each boundary (a district that comes round again is a new span).</summary>
            public int Serial;
            public float StartZ, EndZ;
        }

        enum DistrictPhase { Entry, Body, Leaving }

        District[] _route;
        int _dIndex;
        float _dEnd;
        DistrictPhase _dPhase;
        bool _gatewayDue;
        RoadChunk _forcedChunk;
        District _filling;
        System.Random _districtRng = new System.Random(1), _districtTurnRng = new System.Random(2);
        readonly List<DistrictSpan> _spans = new List<DistrictSpan>();

        /// <summary>Start district id chosen on the start screen (keys 1-7); null = GameTuning.startDistrict.</summary>
        [System.NonSerialized] public string startDistrictOverride;

        /// <summary>True when this ride goes through the theme's districts (theme.HasDistricts and GameTuning.districts).</summary>
        public bool DistrictsOn => _route != null;
        /// <summary>The districts in route order (null when districts are off).</summary>
        public IReadOnlyList<District> Route => _route;
        /// <summary>The district this ride started in (null when districts are off).</summary>
        public District StartDistrict { get; private set; }
        /// <summary>Spans planned so far, in order; the last one is still open (EndZ = +infinity).</summary>
        public IReadOnlyList<DistrictSpan> DistrictSpans => _spans;
        /// <summary>Boundaries planned since the last reset (tests).</summary>
        public int DistrictBoundaries { get; private set; }
        /// <summary>A boundary was planned: (district before, district after, world z of the boundary).</summary>
        public event System.Action<District, District, float> DistrictBoundaryPlanned;

        /// <summary>The district under the rider (null when districts are off).</summary>
        public District CurrentDistrict => DistrictAt(rider != null ? rider.position.z : _runStartZ);
        /// <summary>The district at world z: planned spans, so also beyond the built track (null when districts are off).</summary>
        public District DistrictAt(float z) => TryGetDistrictSpan(z, out DistrictSpan s) ? s.District : null;
        /// <summary>The district of the BUILT chunk under world z (ramp and prop placement); null when off or not built.</summary>
        public District DistrictForChunkAt(float z)
        {
            Active a = ActiveAt(z);
            return a != null ? a.District : null;
        }
        /// <summary>World z of the next boundary ahead of the rider; +infinity when none is planned yet.</summary>
        public float NextBoundaryZ => NextBoundaryAfter(rider != null ? rider.position.z : _runStartZ);
        public float NextBoundaryAfter(float z)
        {
            for (int i = 0; i < _spans.Count; i++) if (_spans[i].StartZ > z) return _spans[i].StartZ;
            return float.PositiveInfinity;
        }

        public bool TryGetDistrictSpan(float z, out DistrictSpan span)
        {
            span = default;
            if (_spans.Count == 0) return false;
            for (int i = _spans.Count - 1; i >= 0; i--)
                if (z >= _spans[i].StartZ) { span = _spans[i]; return true; }
            span = _spans[0];
            return true;
        }

        static T[] Own<T>(T[] own, T[] fallback) => own != null && own.Length > 0 ? own : fallback;

        // Ramps and road gaps authored in a chunk (jetty, river inlet, the end of a raised walkway, a loading-dock kicker):
        // random obstacles, street life and hazard pattern entries are never put on them, and no hazard pattern starts on
        // such a chunk. Looked up once per chunk instance when it is laid (its own children only, not what is placed on it).
        sealed class Authored { public RoadGap[] Gaps; public Ramp[] Ramps; public bool Any => Gaps.Length > 0 || Ramps.Length > 0; }
        readonly Dictionary<RoadChunk, Authored> _authored = new Dictionary<RoadChunk, Authored>();
        const float StreetHalfWidth = 4f; // the standard street (District contract); wider = 4-lane rules

        Authored AuthoredOn(RoadChunk c)
        {
            if (!_authored.TryGetValue(c, out Authored a))
                _authored[c] = a = new Authored { Gaps = c.GetComponentsInChildren<RoadGap>(true), Ramps = c.GetComponentsInChildren<Ramp>(true) };
            return a;
        }

        /// <summary>True where an authored ramp (with 1 m around it and 3 m after the lip) or road gap of this chunk is.</summary>
        bool Blocked(RoadChunk c, float localX, float localZ)
        {
            if (c == null) return false;
            Authored a = AuthoredOn(c);
            if (!a.Any) return false;
            Vector3 world = c.transform.TransformPoint(new Vector3(localX, 0f, localZ));
            foreach (RoadGap g in a.Gaps)
            {
                if (g == null) continue;
                Vector3 p = g.transform.InverseTransformPoint(world);
                if (g.Contains(p.x, p.z)) return true;
            }
            foreach (Ramp r in a.Ramps)
            {
                if (r == null) continue;
                Vector3 p = r.transform.InverseTransformPoint(world);
                if (Mathf.Abs(p.x) <= r.width * 0.5f + 1f && p.z >= -1f && p.z <= r.Length + 3f) return true;
            }
            return false;
        }

        /// <summary>4-lane road at world z: a planned boulevard section, or a chunk that is wider than the street
        /// (e.g. Mounts Bay Road's divided road). Hazard patterns there use the boulevard rules.</summary>
        public bool IsWideAt(float z) => OnBoulevard(z) || HalfWidthAt(z) > StreetHalfWidth + 0.01f;

        // Pedestrian malls (RoadChunk.roadType) have no moving traffic: patterns with cars are not put there.
        readonly Dictionary<HazardPattern, bool> _hasMovers = new Dictionary<HazardPattern, bool>();
        bool HasMovers(HazardPattern p)
        {
            if (p == null) return false;
            if (!_hasMovers.TryGetValue(p, out bool movers))
            {
                foreach (HazardPattern.Entry e in p.entries)
                    if ((e.prefab != null && e.prefab.GetComponent<Mover>() != null) || (e.mirrorPrefab != null && e.mirrorPrefab.GetComponent<Mover>() != null)) { movers = true; break; }
                _hasMovers[p] = movers;
            }
            return movers;
        }
        static bool IsMall(RoadChunk c) => c != null && c.roadType == RoadType.PedestrianMall;
        bool MallAt(float z)
        {
            Active a = _active.Count > 0 && z >= _active[0].StartZ && z < _nextZ ? ActiveAt(z) : null;
            return a != null && IsMall(a.Chunk);
        }

        // Animals on the sidewalks (peacocks, swans: Obstacle.animal) keep their prefab's own slow pace; people stroll
        // at 0.7-1.5 m/s as before.
        static float AnimalPace(GameObject prefab, float walk)
        {
            if (walk == 0f || prefab == null) return walk;
            Obstacle o = prefab.GetComponentInChildren<Obstacle>(true);
            if (o == null || !o.animal) return walk;
            Pedestrian own = prefab.GetComponent<Pedestrian>();
            return own != null ? Mathf.Sign(walk) * Mathf.Abs(own.walkSpeed) : walk;
        }

        void ResetDistricts(float startZ)
        {
            _spans.Clear();
            _forcedChunk = null;
            _filling = null;
            _lastEndHalfWidth = float.NaN;
            _gatewayDue = false;
            DistrictBoundaries = 0;
            StartDistrict = null;
            _route = theme != null && theme.HasDistricts && T.districts ? DistrictRoute.Sort(theme.districts) : null;
            if (_route == null || _route.Length == 0) { _route = null; return; }
            _districtRng = new System.Random(_runSeed ^ 0x2c1b3c6d);
            _districtTurnRng = new System.Random(_runSeed ^ 0x7f4a7c15);
            int start = DistrictRoute.IndexOf(_route, !string.IsNullOrEmpty(startDistrictOverride) ? startDistrictOverride : T.startDistrict);
            if (start < 0) start = 0;
            StartDistrict = _route[start];
            _spans.Add(new DistrictSpan { District = StartDistrict, Serial = 0, StartZ = float.MinValue, EndZ = float.PositiveInfinity });
            BeginDistrict(start, startZ);
            _dPhase = DistrictPhase.Body; // a ride starts inside its district, not at the gateway
            _gatewayDue = false;
        }

        void BeginDistrict(int index, float z)
        {
            _dIndex = index;
            District d = _route[index];
            float min = Mathf.Max(40f, Mathf.Min(d.length.x, d.length.y)), max = Mathf.Max(min, Mathf.Max(d.length.x, d.length.y));
            _dEnd = z + Mathf.Lerp(min, max, (float)_districtRng.NextDouble());
            _dPhase = DistrictPhase.Entry;
            _streetIndex = 0; // the district's own chunk order starts from its first chunk
            _sectionStreetIndex = int.MinValue;
            _dOverrun = 0;
            _gatewayDue = true;
        }

        static float ExitLength(District d) => d.exit != null ? Mathf.Max(1f, d.exit.length) : 0f;

        int _dOverrun;

        // Half width where the next piece starts: this district's exit, else the next district's entry (else its first
        // chunk), normally the standard street (4).
        float NextStartHalfWidth(District d)
        {
            if (d.exit != null) return d.exit.HalfWidthAt(0f);
            District next = _route[(_dIndex + 1) % _route.Length];
            if (next.entry != null) return next.entry.HalfWidthAt(0f);
            return next.chunks != null && next.chunks.Length > 0 && next.chunks[0] != null ? next.chunks[0].HalfWidthAt(0f) : StreetHalfWidth;
        }

        // Lead-in, entry and exit chunks; null = one of the district's own chunks (PickChunk).
        RoadChunk NextDistrictChunk()
        {
            if (_forcedChunk != null)
            {
                RoadChunk lead = _forcedChunk;
                _forcedChunk = null;
                return lead;
            }
            for (int guard = 0; guard < 4; guard++)
            {
                District d = _route[_dIndex];
                switch (_dPhase)
                {
                    case DistrictPhase.Leaving:
                        BeginDistrict((_dIndex + 1) % _route.Length, _nextZ);
                        continue;
                    case DistrictPhase.Entry:
                        _dPhase = DistrictPhase.Body;
                        if (d.entry != null) return d.entry;
                        continue;
                    default:
                        if (_route.Length < 2) return null; // one district: no boundaries
                        float exit = ExitLength(d);
                        if (_nextZ + exit < _dEnd) return null;
                        // The road must be as wide where the exit (or the next district) starts as where the last chunk
                        // ended: CBD may be in its 4-lane part when its length runs out. Keep laying its own chunks
                        // (at most one pass of its cycle) until the widths meet.
                        float need = NextStartHalfWidth(d);
                        if (!float.IsNaN(_lastEndHalfWidth) && Mathf.Abs(_lastEndHalfWidth - need) > 0.01f)
                        {
                            if (_dOverrun++ < Mathf.Max(1, d.chunks.Length)) return null;
                            Debug.LogWarning("[Districts] " + d.id + ": its chunks never end " + need + " m wide (half width) for its exit / the next district; road width jumps at z " + _nextZ);
                        }
                        PlanBoundary(_nextZ + exit);
                        _dPhase = DistrictPhase.Leaving;
                        if (d.exit != null) return d.exit;
                        continue;
                }
            }
            return null;
        }

        void PlanBoundary(float z)
        {
            int next = (_dIndex + 1) % _route.Length;
            DistrictSpan last = _spans[_spans.Count - 1];
            last.EndZ = z;
            _spans[_spans.Count - 1] = last;
            _spans.Add(new DistrictSpan { District = _route[next], Serial = last.Serial + 1, StartZ = z, EndZ = float.PositiveInfinity });
            DistrictBoundaries++;
            // The new district appears round a corner: a fake turn starts a little before the boundary.
            FakeTurnDirector turns = FakeTurnDirector.Instance;
            if (T.fakeTurns && turns != null)
            {
                Vector2 lead = T.districtTurnLead;
                float before = Mathf.Lerp(Mathf.Min(lead.x, lead.y), Mathf.Max(lead.x, lead.y), (float)_districtTurnRng.NextDouble());
                int direction = Mathf.Abs(turns.Heading) > 10f * Mathf.Deg2Rad ? (turns.Heading > 0f ? -1 : 1) : (_districtTurnRng.NextDouble() < 0.5 ? -1 : 1);
                // The sky turns to the next district's direction (District.skyYaw) across this boundary: turn the road
                // the same way, so the skyline swings round with the corner.
                float skyTurn = Mathf.DeltaAngle(_route[_dIndex].skyYaw, _route[next].skyYaw);
                if (Mathf.Abs(skyTurn) > 5f) direction = skyTurn > 0f ? 1 : -1;
                turns.RequestTurn(z - before, direction, 0.75f, Mathf.Max(T.turnMinLength, before + 80f));
            }
            DistrictBoundaryPlanned?.Invoke(_route[_dIndex], _route[next], z);
        }

        // The new district's gateway across the road where it starts (decoration, recycled with its chunk).
        void PlaceGateway(Active a)
        {
            if (!_gatewayDue || a.District == null) return;
            _gatewayDue = false;
            if (a.District.gateway == null) return;
            Place(a, a.District.gateway, new Vector3(0f, a.Chunk.HeightAt(0.5f), 0.5f));
        }

        // Boulevards only in districts that allow them, whole sections before the district's exit.
        bool DistrictSectionRoom(out int cycle, out float room)
        {
            District d = _route[_dIndex];
            cycle = Mathf.Max(1, d.chunks.Length);
            room = float.MaxValue;
            if (_dPhase != DistrictPhase.Body || !d.allowBoulevard) return false;
            if (_route.Length > 1) room = _dEnd - ExitLength(d) - _nextZ;
            return true;
        }

        // Hazard patterns stay inside their district's own chunks: never on entry / exit chunks or across a boundary.
        bool DistrictPatternRoom(Active a, float start, ref float room)
        {
            if (_route == null || _route.Length < 2 || a.District == null) return true;
            District d = a.District;
            if ((d.entry != null && ReferenceEquals(a.Key, d.entry.gameObject)) || (d.exit != null && ReferenceEquals(a.Key, d.exit.gameObject))) return false;
            if (d != _route[_dIndex] || _dPhase == DistrictPhase.Leaving) return false;
            room = Mathf.Min(room, _dEnd - ExitLength(d) - start);
            return room > 0f;
        }

        void TrimDistrictSpans(float riderZ)
        {
            while (_spans.Count > 2 && _spans[0].EndZ < riderZ - 600f) _spans.RemoveAt(0);
        }
        // ---- end Districts (v0.3)

        void Shuffle(List<SpawnSlot> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
