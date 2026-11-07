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

        /// <summary>Clears everything and builds fresh track starting at <paramref name="startZ"/>.</summary>
        public void ResetTrack(float startZ)
        {
            for (int i = _active.Count - 1; i >= 0; i--) Recycle(i);
            _runSeed = seed != 0 ? seed : System.Environment.TickCount;
            _rng = new System.Random(_runSeed);
            _nextZ = startZ;
            _nextY = 0f;
            _runStartZ = startZ;
            _chunkCounter = 0;
            _lastChunkKey = null;
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
            BuildAhead(startZ);
            SyncCollisionTransforms();
        }

        void Update()
        {
            if (rider == null) return;
            float z = rider.position.z;
            BuildAhead(z);
            while (_active.Count > 1 && _active[0].StartZ + _active[0].Chunk.length < z - keepBehind) Recycle(0);
            SyncCollisionTransforms();
        }

        void SyncCollisionTransforms()
        {
            if(!_collisionTransformsDirty)return;
            Physics.SyncTransforms();_collisionTransformsDirty=false;
        }

        void BuildAhead(float riderZ)
        {
            int guard = 0;
            while (_nextZ < riderZ + buildAhead && guard++ < 64) SpawnChunk();
        }

        /// <summary>Half width of the road, excluding sidewalks.</summary>
        public float HalfWidthAt(float z)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                Active a = _active[i];
                if (z >= a.StartZ && z < a.StartZ + a.Chunk.length) return a.Chunk.halfWidth;
            }
            return PlaceholderTrack.HalfWidth;
        }

        RoadChunk SurfaceChunkAt(float z)
        {
            if(_active.Count==0)return null;
            for(int i=0;i<_active.Count;i++)if(z<_active[i].StartZ+_active[i].Chunk.length)return _active[i].Chunk;
            return _active[_active.Count-1].Chunk;
        }
        public float RideableHalfWidthAt(float z)=>SurfaceChunkAt(z)?.RideableHalfWidth??PlaceholderTrack.HalfWidth;
        public float SidewalkOffsetAt(float x,float z)=>SurfaceChunkAt(z)?.SidewalkOffsetAt(x)??0;
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
            object key = PickChunk();
            GameObject go = Get(key, () => CreateChunk(key));
            var chunk = go.GetComponent<RoadChunk>();
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(new Vector3(0f, _nextY, _nextZ), Quaternion.identity);
            go.GetComponent<StreetDressing>()?.ApplyLayout(_runSeed, _chunkCounter);
            _collisionTransformsDirty=true;
            var active = new Active { Key = key, Chunk = chunk, StartZ = _nextZ };
            _active.Add(active);
            Populate(active);
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
                    RoadChunk candidate = theme.chunks[(_chunkCounter + i + theme.chunks.Length) % theme.chunks.Length];
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

        void Place(Active a, object key, Vector3 localPos)
        {
            GameObject go = Get(key, () => CreateSpawnable(key));
            go.transform.SetParent(a.Chunk.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.SetActive(true);
            a.Spawned.Add(new Placed { Key = key, Instance = go });
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
