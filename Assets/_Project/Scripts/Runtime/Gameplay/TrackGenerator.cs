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
            if (buildLeadIn && theme != null && theme.orderedChunks && theme.HasChunks)
            {
                RoadChunk previous = theme.chunks[theme.chunks.Length - 1];
                if (previous != null)
                {
                    _nextZ -= Mathf.Max(1f, previous.length);
                    _nextY = -previous.endHeight;
                    _chunkCounter = -1;
                }
            }
            _streetIndex = _chunkCounter;
            BuildAhead(startZ);
            SyncCollisionTransforms();
        }

        void Update()
        {
            if (rider == null) return;
            float z = rider.position.z;
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
            object key = planned != null ? planned.gameObject : PickChunk();
            if (planned == null) _streetIndex++; // boulevard chunks do not advance the street rhythm
            GameObject go = Get(key, () => CreateChunk(key));
            var chunk = go.GetComponent<RoadChunk>();
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(new Vector3(0f, _nextY, _nextZ), Quaternion.identity);
            go.GetComponent<StreetDressing>()?.ApplyLayout(_runSeed, _chunkCounter);
            _collisionTransformsDirty=true;
            var active = new Active { Key = key, Chunk = chunk, StartZ = _nextZ };
            _active.Add(active);
            Populate(active);
            PlacePending(active);
            PlaceStreetLife(active);
            _nextZ += Mathf.Max(1f, chunk.length);
            _nextY += chunk.endHeight;
            _chunkCounter++;
            _lastChunkKey = key;
        }

        object PickChunk()
        {
            if (theme == null || !theme.HasChunks)
            {
                int variant = _chunkCounter % 3 == 2 ? 2 : _chunkCounter % 2;
                return "placeholder-chunk-" + variant;
            }
            int level = T.DifficultyLevel(_nextZ - _runStartZ);
            if (theme.orderedChunks)
            {
                for (int i = 0; i < theme.chunks.Length; i++)
                {
                    RoadChunk candidate = theme.chunks[((_streetIndex + i) % theme.chunks.Length + theme.chunks.Length) % theme.chunks.Length];
                    if (candidate != null && candidate.minDifficulty <= level) return candidate.gameObject;
                }
                return FirstChunk();
            }
            float total = 0f;
            foreach (RoadChunk c in theme.chunks)
                if (c != null && c.minDifficulty <= level && (object)c.gameObject != _lastChunkKey) total += Mathf.Max(0f, c.weight);
            if (total <= 0f) return FirstChunk();
            double pick = _rng.NextDouble() * total;
            foreach (RoadChunk c in theme.chunks)
            {
                if (c == null || c.minDifficulty > level || (object)c.gameObject == _lastChunkKey) continue;
                pick -= Mathf.Max(0f, c.weight);
                if (pick <= 0) return c.gameObject;
            }
            return FirstChunk();
        }

        object FirstChunk()
        {
            foreach (RoadChunk c in theme.chunks)
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
            // Only where the ordered street cycle starts again, so its near / open rhythm stays whole; never two
            // sections back to back (after the Out chunk the street index is still on the same boundary).
            if (_streetIndex <= 0 || _streetIndex % theme.chunks.Length != 0 || _streetIndex == _sectionStreetIndex) return;
            if (_nextZ - _runStartZ < T.boulevardMinDistance || _nextZ < _nextSectionZ) return;
            // A hazard pattern authored for the street still runs into this stretch: try again next cycle.
            if (_reservedUntil >= _nextZ) return;
            if (_sectionRng.NextDouble() >= T.boulevardChance) return;
            _sectionStreetIndex = _streetIndex;
            int min = Mathf.Max(1, T.boulevardChunks.x), count = _sectionRng.Next(min, Mathf.Max(min, T.boulevardChunks.y) + 1);
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
                    if (s.kind != SpawnKind.JumpObstacle) _rowScratch.Add(s);
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
            if (theme != null && theme.obstacles != null && theme.obstacles.Length > 0)
            {
                Obstacle o = PickWeighted(theme.obstacles);
                if (o != null) return o.gameObject;
            }
            return _rng.NextDouble() < 0.7 ? "placeholder-obstacle-0" : "placeholder-obstacle-1";
        }

        object JumpObstacleKey()
        {
            if (theme != null && theme.jumpObstacles != null && theme.jumpObstacles.Length > 0)
            {
                Obstacle o = PickWeighted(theme.jumpObstacles);
                if (o != null) return o.gameObject;
            }
            return "placeholder-railing";
        }

        object GiftKey()
        {
            if (theme != null && theme.gifts != null && theme.gifts.Length > 0)
            {
                Gift g = theme.gifts[_rng.Next(theme.gifts.Length)];
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
            // Not on boulevard transitions: the sidewalk moves sideways under people walking along it.
            if (theme == null || theme.pedestrians == null || theme.pedestrians.Length == 0 || c.sidewalkWidth <= 1f || c.ChangesWidth) return;
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
                Vector3 local = new Vector3(x, c.HeightAt(z) + c.SidewalkOffsetAt(x), z);
                Vector3 world = c.transform.TransformPoint(local);
                // Not inside a bench, a bin, a bus shelter or another person.
                if (Physics.OverlapBoxNonAlloc(world + Vector3.up * 0.95f, new Vector3(0.5f, 0.8f, 0.6f), _overlap, Quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Ignore) > 0) continue;
                GameObject prefab = theme.pedestrians[_rng.Next(theme.pedestrians.Length)];
                if (prefab == null) continue;
                Place(a, prefab, local, 0f, true);
                GameObject go = a.Spawned[a.Spawned.Count - 1].Instance;
                float walk = _rng.NextDouble() < T.sidewalkPeopleWalking ? (_rng.NextDouble() < 0.5 ? -1f : 1f) * Mathf.Lerp(0.7f, 1.5f, (float)_rng.NextDouble()) : 0f;
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
            bool wide = OnBoulevard(a.StartZ);
            HazardPattern[] list = wide && theme.boulevardHazards != null && theme.boulevardHazards.Length > 0 ? theme.boulevardHazards : theme.hazards;
            if (list == null || list.Length == 0) return;
            if (a.StartZ < _nextHazardZ) return;
            if (_rng.NextDouble() >= Mathf.Lerp(T.hazardChanceStart, T.hazardChanceMax, d01))
            {
                _nextHazardZ = a.StartZ + Mathf.Max(1f, a.Chunk.length);
                return;
            }
            float start = a.StartZ + 5f;
            HazardPattern p = PickPattern(list, level, a.Chunk.halfWidth, wide ? _wideUntil - 6f - start : float.MaxValue);
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
                bool moved = false;
                // Built chunks can be checked; chunks laid later never start a boulevard section inside a reserved pattern.
                for (float z = start - 6f; z <= end + 6f && z < _nextZ; z += 5f)
                {
                    RoadChunk c = ChunkAt(z, out float chunkStart);
                    if (c == null || !c.ChangesWidth) continue;
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
        HazardPattern PickPattern(HazardPattern[] list, int level, float roadHalfWidth, float room)
        {
            bool Fits(HazardPattern h) => h != null && h.minLevel <= level && h.roadHalfWidth <= roadHalfWidth + 0.01f && h.length <= room;
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
                float speed = e.Mover.fractionOfRiderSpeed > 0f ? riderSpeed * e.Mover.fractionOfRiderSpeed : e.Mover.speed;
                float t = Mathf.Max(0.5f, (e.MeetZ - riderZ) / riderSpeed);
                // Keep it inside the stretch where cars live (a slow rider makes the meeting time long), and never on top of the rider.
                float z = Mathf.Clamp(e.Z - speed * t, riderZ + 40f, riderZ + buildAhead + 100f);
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
