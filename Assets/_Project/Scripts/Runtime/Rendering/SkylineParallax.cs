using System;
using UnityEngine;

namespace CityScooter.Rendering
{
    /// <summary>
    /// Distant skyline: layers drawn on arcs (cylinder segments) centred on the camera, far beyond the fog, no extra
    /// camera. The rig follows the camera so the skyline never gets closer, and scrolls each layer sideways by
    /// distance travelled × master rate × the layer's parallax factor (art suggestion: far 0.15, mid 0.4, near 1).
    /// All layers share one vertical mapping, exactly like the stacked art: the texture's horizon line (e.g. the Perth
    /// waterline 84.5% down) sits <see cref="horizonAngle"/> degrees from eye level, and one texture width covers
    /// <see cref="degreesPerRepeat"/> degrees. The same mapping is published to the sky shader (_CS_SkyMapping) so the
    /// sky gradient lines up with the layers.
    /// Textures come from the active <see cref="SceneLook"/> (its Skyline section); a layer's own Texture is the fallback.
    /// Uses the CityScooter/Skyline Layer shader; per-layer values go through a MaterialPropertyBlock.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("CityScooter/Rendering/Skyline Parallax")]
    public sealed class SkylineParallax : MonoBehaviour
    {
        public enum DistanceSource
        {
            /// <summary>Measure how far the camera moves along the rig's forward axis.</summary>
            CameraMotion = 0,
            /// <summary>Gameplay calls <see cref="AddDistance"/> (use when the world moves instead of the camera).</summary>
            Manual = 1,
        }

        public enum LookSlot
        {
            /// <summary>Always use the layer's own texture.</summary>
            None = -1,
            Far = 0,
            Mid = 1,
            Near = 2,
        }

        [Serializable]
        public sealed class Layer
        {
            public string name = "Layer";

            [Tooltip("Which skyline texture of the active Scene Look this layer shows. None = always use 'Texture'.")]
            public LookSlot lookSlot = LookSlot.Far;

            [Tooltip("Fallback art (RGBA, tiles horizontally) used when the Scene Look has no texture for this slot.")]
            public Texture2D texture;

            [Tooltip("Arc renderer for this layer (a child of the rig with MeshFilter + MeshRenderer).")]
            public MeshRenderer renderer;

            public Color tint = Color.white;

            [Range(0f, 1f), Tooltip("Extra fade towards the look's skyline haze colour. The delivered art already has " +
                                    "atmospheric perspective, so keep 0 unless a layer pops too much.")]
            public float haze;

            [Range(0f, 1f), Tooltip("Extra haze on the lower part of the layer (0..1 of its height). Keep 0 for the delivered art.")]
            public float bottomHaze;

            [Min(20f), Tooltip("Distance from the camera in metres: beyond the fog end and any road chunk, inside the far plane. " +
                               "Only affects what can stand in front of it; the on-screen size does not change.")]
            public float radius = 500f;

            [Tooltip("Scroll speed relative to the master rate (far 0.15, mid 0.4, near 1). 0 = static.")]
            public float parallax = 1f;
        }

        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int HazeId = Shader.PropertyToID("_Haze");
        static readonly int BottomHazeId = Shader.PropertyToID("_BottomHaze");

        /// <summary>Global read by the sky shader: x = tan(horizon angle), y = tan(angle of the texture top row), w = 1 when set.</summary>
        public static readonly int SkyMappingId = Shader.PropertyToID("_CS_SkyMapping");

        const int ArcSegments = 32;

        [Tooltip("Camera to follow. Empty = Camera.main.")]
        public Camera targetCamera;

        [Header("Placement (shared by all layers, like the stacked art)")]
        [Range(-5f, 5f), Tooltip("Where the texture's horizon line sits, in degrees from eye level (0 = the road's vanishing " +
                                 "line, negative = lower). Line it up with where the road disappears into the fog.")]
        public float horizonAngle = -0.4f;

        [Range(30f, 180f), Tooltip("Degrees one texture width covers. 90 = one width is about the screen width at 16:9, " +
                                   "matching the art composite. Lower = bigger skyline.")]
        public float degreesPerRepeat = 90f;

        [Range(0.3f, 1f), Tooltip("Horizon line inside the textures as a fraction from the top, used when the Scene Look " +
                                  "has no skyline (the look's value wins otherwise).")]
        public float horizonFromTop = 0.845f;

        [Range(0f, 1f), Tooltip("Texture U that sits straight ahead at the start (0.47 = Perth CBD in the delivered art).")]
        public float centerU = 0.47f;

        [Range(90f, 240f), Tooltip("Width of every layer's arc in degrees. Must be wider than the camera's horizontal view.")]
        public float arcDegrees = 150f;

        [Tooltip("Turn the rig with the camera's heading (only needed if the road ever turns).")]
        public bool followCameraYaw;

        [Tooltip("Keep the rig on the game camera while editing.")]
        public bool followInEditMode = true;

        [Header("Scrolling")]
        public DistanceSource distanceSource = DistanceSource.CameraMotion;

        [Tooltip("Texture widths scrolled per metre travelled for a layer with parallax 1 (negative = other direction). " +
                 "0.0015 at ~15 m/s gives the art's suggested ~0.02 widths per second for the near layer.")]
        public float scrollPerMetre = 0.0015f;

        [Min(0.1f), Tooltip("Camera moves bigger than this in one frame (respawn, floating-origin shift) do not count as travel.")]
        public float maxStepPerFrame = 25f;

        [Header("Layers (far first, drawn in this order)")]
        public Layer[] layers = Array.Empty<Layer>();

        Mesh _arcMesh;
        float _arcMeshDegrees = -1f;
        MaterialPropertyBlock _mpb;
        double _distance;
        Vector3 _lastCameraPosition;
        bool _hasLastCamera;
        bool _warnedFarPlane;
        Vector4 _skyMapping;

        /// <summary>Metres travelled that drive the scroll.</summary>
        public double Distance => _distance;

        /// <summary>Add travelled metres (for <see cref="DistanceSource.Manual"/>, or extra drift).</summary>
        public void AddDistance(float metres) => _distance += metres;

        public void ResetDistance() => _distance = 0d;

        void OnEnable()
        {
            _hasLastCamera = false;
            ApplySceneLook.LookApplied += OnLookApplied;
            Refresh();
        }

        void OnDisable()
        {
            ApplySceneLook.LookApplied -= OnLookApplied;
            Shader.SetGlobalVector(SkyMappingId, Vector4.zero); // the sky falls back to its material values
            if (_arcMesh == null) return;
            if (Application.isPlaying) Destroy(_arcMesh);
            else DestroyImmediate(_arcMesh);
            _arcMesh = null;
            _arcMeshDegrees = -1f;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // Assigning meshes inside OnValidate sends messages Unity complains about; refresh on the next tick.
            UnityEditor.EditorApplication.delayCall -= DelayedRefresh;
            UnityEditor.EditorApplication.delayCall += DelayedRefresh;
        }

        void DelayedRefresh()
        {
            if (this != null && isActiveAndEnabled) Refresh();
        }
#endif

        void OnLookApplied(SceneLook look)
        {
            if (this != null && isActiveAndEnabled) Refresh();
        }

        void LateUpdate()
        {
            Camera cam = ResolveCamera();
            bool playing = Application.isPlaying;
            if (cam != null && (playing || followInEditMode)) FollowCamera(cam);
            if (cam != null && playing) TrackDistance(cam);
            ApplyLayerProperties();
        }

        /// <summary>Re-layout every layer, snap to the camera and re-apply textures. Call after changing layers by script.</summary>
        public void Refresh()
        {
            LayoutLayers();
            Camera cam = ResolveCamera();
            if (cam != null && (Application.isPlaying || followInEditMode)) FollowCamera(cam);
            ApplyLayerProperties();
        }

        /// <summary>The texture a layer shows right now (Scene Look first, then the layer's own texture).</summary>
        public Texture2D EffectiveTexture(Layer layer)
        {
            SceneLook look = ApplySceneLook.ActiveLook;
            Texture2D fromLook = look != null && layer.lookSlot != LookSlot.None ? look.GetSkyline((int)layer.lookSlot) : null;
            return fromLook != null ? fromLook : layer.texture;
        }

        /// <summary>Horizon line inside the textures (fraction from the top) currently in use.</summary>
        public float EffectiveHorizonFromTop
        {
            get
            {
                SceneLook look = ApplySceneLook.ActiveLook;
                return look != null && look.HasSkyline ? look.skylineHorizonFromTop : horizonFromTop;
            }
        }

        Camera ResolveCamera() => targetCamera != null ? targetCamera : Camera.main;

        void FollowCamera(Camera cam)
        {
#if UNITY_EDITOR
            // Prefab mode / editor tools: never chase a camera from another scene.
            if (UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(gameObject.scene)) return;
#endif
            Transform c = cam.transform;
            Vector3 target = c.position; // layers are placed relative to eye level
            Quaternion rotation = followCameraYaw ? Quaternion.Euler(0f, c.eulerAngles.y, 0f) : Quaternion.identity;
            if ((transform.position - target).sqrMagnitude > 1e-6f) transform.position = target;
            if (Quaternion.Angle(transform.rotation, rotation) > 0.01f) transform.rotation = rotation;

            if (!_warnedFarPlane && Application.isPlaying)
            {
                float needed = 0f;
                foreach (Layer layer in layers)
                {
                    if (layer != null) needed = Mathf.Max(needed, layer.radius);
                }
                if (cam.farClipPlane < needed * 1.05f)
                {
                    Debug.LogWarning("[CityScooter] Camera far plane (" + cam.farClipPlane + " m) cuts the skyline (radius up to "
                        + needed + " m). Raise the far plane or lower the layer radius.", this);
                }
                _warnedFarPlane = true;
            }
        }

        void TrackDistance(Camera cam)
        {
            Vector3 p = cam.transform.position;
            if (!_hasLastCamera)
            {
                _lastCameraPosition = p;
                _hasLastCamera = true;
                return;
            }
            Vector3 delta = p - _lastCameraPosition;
            _lastCameraPosition = p;
            if (distanceSource != DistanceSource.CameraMotion) return;
            if (delta.sqrMagnitude > maxStepPerFrame * maxStepPerFrame) return; // teleport, not travel
            _distance += Vector3.Dot(delta, transform.forward);
        }

        void LayoutLayers()
        {
            if (layers == null) return;
            Mesh mesh = GetArcMesh();
            float horizonRow = EffectiveHorizonFromTop;
            float tanHorizon = Mathf.Tan(horizonAngle * Mathf.Deg2Rad);
            float repeatRadians = degreesPerRepeat * Mathf.Deg2Rad;
            float aspectForSky = 0.25f;
            bool aspectFound = false;

            for (int i = 0; i < layers.Length; i++)
            {
                Layer layer = layers[i];
                if (layer == null || layer.renderer == null) continue;

                if (layer.renderer.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != mesh) filter.sharedMesh = mesh;

                Texture2D tex = EffectiveTexture(layer);
                float aspect = tex != null && tex.width > 0 ? tex.height / (float)tex.width : 0.25f;
                if (!aspectFound && tex != null)
                {
                    aspectForSky = aspect;
                    aspectFound = true;
                }

                // Same angles for every radius: height and base scale with the radius.
                float height = layer.radius * repeatRadians * aspect;
                float baseY = layer.radius * tanHorizon - (1f - horizonRow) * height;

                Transform t = layer.renderer.transform;
                var localPosition = new Vector3(0f, baseY, 0f);
                var localScale = new Vector3(layer.radius, height, layer.radius);
                if (t.localPosition != localPosition) t.localPosition = localPosition;
                if (t.localRotation != Quaternion.identity) t.localRotation = Quaternion.identity;
                if (t.localScale != localScale) t.localScale = localScale;

                // Far layers first; negative so the skyline draws before every other transparent object.
                int order = i - layers.Length;
                if (layer.renderer.sortingOrder != order) layer.renderer.sortingOrder = order;
            }

            float tanTop = tanHorizon + horizonRow * repeatRadians * aspectForSky;
            _skyMapping = new Vector4(tanHorizon, tanTop, 0f, 1f);
        }

        void ApplyLayerProperties()
        {
            if (layers == null) return;
            Shader.SetGlobalVector(SkyMappingId, _skyMapping);
            _mpb ??= new MaterialPropertyBlock();
            float tiling = arcDegrees / Mathf.Max(1f, degreesPerRepeat);
            for (int i = 0; i < layers.Length; i++)
            {
                Layer layer = layers[i];
                if (layer == null || layer.renderer == null) continue;
                double scroll = centerU - 0.5 * tiling + _distance * scrollPerMetre * layer.parallax;
                float offset = (float)(scroll - Math.Floor(scroll)); // wrap to 0..1 so precision never degrades

                _mpb.Clear();
                Texture2D tex = EffectiveTexture(layer);
                if (tex != null) _mpb.SetTexture(BaseMapId, tex);
                _mpb.SetVector(BaseMapStId, new Vector4(tiling, 1f, offset, 0f));
                _mpb.SetColor(BaseColorId, layer.tint);
                _mpb.SetFloat(HazeId, layer.haze);
                _mpb.SetFloat(BottomHazeId, layer.bottomHaze);
                layer.renderer.SetPropertyBlock(_mpb);
            }
        }

        // Unit-radius arc, 1 m tall, centred on +Z; each layer scales it by (radius, height, radius).
        Mesh GetArcMesh()
        {
            if (_arcMesh != null && Mathf.Approximately(_arcMeshDegrees, arcDegrees)) return _arcMesh;
            if (_arcMesh == null)
            {
                _arcMesh = new Mesh { name = "SkylineArc (generated)", hideFlags = HideFlags.HideAndDontSave };
            }
            _arcMeshDegrees = arcDegrees;

            var vertices = new Vector3[(ArcSegments + 1) * 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[ArcSegments * 6];
            float half = arcDegrees * 0.5f * Mathf.Deg2Rad;
            for (int i = 0; i <= ArcSegments; i++)
            {
                float t = i / (float)ArcSegments;
                float angle = Mathf.Lerp(-half, half, t);
                var p = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                vertices[i * 2] = p;
                vertices[i * 2 + 1] = p + Vector3.up;
                uvs[i * 2] = new Vector2(t, 0f);
                uvs[i * 2 + 1] = new Vector2(t, 1f);
            }
            for (int i = 0; i < ArcSegments; i++)
            {
                int v = i * 2;
                int k = i * 6;
                // Clockwise as seen from the centre, so the inside faces the camera.
                triangles[k] = v;
                triangles[k + 1] = v + 1;
                triangles[k + 2] = v + 2;
                triangles[k + 3] = v + 2;
                triangles[k + 4] = v + 1;
                triangles[k + 5] = v + 3;
            }
            _arcMesh.Clear();
            _arcMesh.vertices = vertices;
            _arcMesh.uv = uvs;
            _arcMesh.triangles = triangles;
            _arcMesh.RecalculateBounds();
            return _arcMesh;
        }
    }
}
