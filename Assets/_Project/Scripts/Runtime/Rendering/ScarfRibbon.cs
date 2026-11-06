using UnityEngine;

namespace CityScooter.Rendering
{
    /// <summary>Lightweight world-space Verlet cloth strip: wind, gravity, length constraints and teleport reset.</summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ScarfRibbon : MonoBehaviour
    {
        [Range(6, 32)] public int segments = 18;
        public float length = 3.2f;
        public float width = 0.28f;
        public Color color = new Color(0.9f, 0.045f, 0.07f);
        public float wind = 22f;
        Vector3[] _points, _previous, _vertices;
        Color[] _colors;
        Mesh _mesh;
        Vector3 _lastAnchor;
        float _accumulator;

        void OnEnable() => Initialize();

        void Initialize()
        {
            segments = Mathf.Clamp(segments, 6, 32);
            _points = new Vector3[segments + 1];
            _previous = new Vector3[segments + 1];
            _vertices = new Vector3[(segments + 1) * 4];
            _colors = new Color[_vertices.Length];
            var triangles = new int[segments * 12]; // two-sided; no transparent cloth
            for (int i = 0; i < segments; i++)
            {
                int a = i * 4, t = i * 12;
                int[] face = { a, a + 4, a + 1, a + 1, a + 4, a + 5, a + 3, a + 6, a + 2, a + 7, a + 6, a + 3 };
                for (int j = 0; j < 12; j++) triangles[t + j] = face[j];
            }
            for (int i = 0; i < _colors.Length; i++) _colors[i] = color.linear;
            _mesh = new Mesh { name = "ScarfRibbon_Runtime" };
            _mesh.MarkDynamic();
            _mesh.vertices = _vertices;
            _mesh.colors = _colors;
            _mesh.triangles = triangles;
            var inkUV = new System.Collections.Generic.List<Vector3>(_vertices.Length);
            for(int i=0;i<=segments;i++)
            {
                float t=(float)i/segments;
                inkUV.Add(new Vector3(0,t,1));inkUV.Add(new Vector3(1,t,1));
                inkUV.Add(new Vector3(0,t,1));inkUV.Add(new Vector3(1,t,1));
            }
            _mesh.SetUVs(0,inkUV);
            GetComponent<MeshFilter>().sharedMesh = _mesh;
            ResetCloth();
            UpdateMesh();
        }

        public void ResetCloth()
        {
            if (_points == null) return;
            _lastAnchor = transform.position;
            _accumulator = 0f;
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                _points[i] = transform.TransformPoint(new Vector3(Mathf.Sin(t * 5f) * 0.35f, -0.25f * t, -length * t));
                _previous[i] = _points[i];
            }
        }

        void LateUpdate()
        {
            if (_mesh == null) return;
            Vector3 anchor = transform.position;
            if ((anchor - _lastAnchor).sqrMagnitude > 36f) ResetCloth();
            _lastAnchor = anchor;
            _accumulator = Mathf.Min(_accumulator + Time.deltaTime, 0.05f);
            const float step = 1f / 90f;
            while (_accumulator >= step)
            {
                Simulate(step);
                _accumulator -= step;
            }
            _points[0] = anchor;
            UpdateMesh();
        }

        void Simulate(float dt)
        {
            float rest = length / segments;
            Vector3 force = -transform.forward * wind + Vector3.down * 4f + transform.right * (Mathf.Sin(Time.time * 4.5f) * 6f);
            _points[0] = transform.position;
            for (int i = 1; i <= segments; i++)
            {
                Vector3 old = _points[i];
                _points[i] += (_points[i] - _previous[i]) * 0.975f + force * (dt * dt);
                _previous[i] = old;
            }
            for (int k = 0; k < 7; k++)
            {
                _points[0] = transform.position;
                for (int i = 1; i <= segments; i++)
                {
                    Vector3 delta = _points[i] - _points[i - 1];
                    Vector3 correction = delta * (1f - rest / Mathf.Max(0.0001f, delta.magnitude));
                    if (i == 1) _points[i] -= correction;
                    else { _points[i] -= correction * 0.5f; _points[i - 1] += correction * 0.5f; }
                    _points[i].y = Mathf.Max(_points[i].y, transform.position.y - 1.3f);
                }
            }
        }

        void UpdateMesh()
        {
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 across = (transform.right + Vector3.up * Mathf.Sin(t * 9f + Time.time * 3f) * 0.6f).normalized;
                float half = width * (1f - t * 0.2f) * 0.5f;
                _vertices[i * 4] = _vertices[i * 4 + 2] = transform.InverseTransformPoint(_points[i] - across * half);
                _vertices[i * 4 + 1] = _vertices[i * 4 + 3] = transform.InverseTransformPoint(_points[i] + across * half);
            }
            _mesh.vertices = _vertices;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }

        void OnDisable()
        {
            if (_mesh != null) { if (Application.isPlaying) Destroy(_mesh); else DestroyImmediate(_mesh); }
            _mesh = null;
        }
    }
}
