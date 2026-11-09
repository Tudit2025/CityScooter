using CityScooter.Rendering;
using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// Radio race (v0.4): another rider drawn on our road. A copy of the rider's model with no controller and no
    /// colliders (nothing can hit it), tinted in the rider's colour with a MaterialPropertyBlock on the existing
    /// materials: the comic vertex colours times _BaseColor, the ink outline's _InkColor (a coloured outline), the scarf in
    /// that colour. Grey while the signal is lost. No new materials or shaders. RaceDirector places it every frame.
    /// </summary>
    public sealed class RaceGhost : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int InkColorId = Shader.PropertyToID("_InkColor");
        static readonly int OutlineId = Shader.PropertyToID("_OutlinePixels");

        static readonly Color GreyBody = new Color(0.56f, 0.58f, 0.62f);
        static readonly Color GreyInk = new Color(0.3f, 0.31f, 0.34f);

        public string RiderName { get; private set; }
        /// <summary>The model is drawn (not hidden because it would sit right on top of you).</summary>
        public bool ModelShown { get; private set; } = true;
        /// <summary>Drawn grey (no message for 2 s).</summary>
        public bool Grey => _grey;
        /// <summary>The body tint now (tests).</summary>
        public Color BodyTint { get; private set; } = Color.white;

        Renderer[] _all, _bodies, _outlines, _scarves;
        MaterialPropertyBlock _block;
        Color _colour;
        bool _grey, _looked;

        /// <summary>A ghost under <paramref name="parent"/>: a copy of <paramref name="template"/> (the rider's model), or a
        /// grey-box rider when there is none.</summary>
        public static RaceGhost Create(Transform parent, Transform template, string riderName, Color colour)
        {
            var root = new GameObject("Ghost " + riderName);
            root.SetActive(false); // nothing on the copy wakes up before it is cleaned and coloured
            root.transform.SetParent(parent, false);
            var ghost = root.AddComponent<RaceGhost>();
            ghost.RiderName = riderName;
            Transform model;
            if (template != null)
            {
                model = Instantiate(template.gameObject, root.transform, false).transform;
                model.localPosition = template.localPosition;
                model.localScale = template.localScale;
            }
            else model = PlaceholderTrack.CreateRiderModel(root.transform).transform;
            model.name = "Model";
            model.localRotation = Quaternion.identity;
            model.gameObject.SetActive(true);
            foreach (Collider c in model.GetComponentsInChildren<Collider>(true)) { c.enabled = false; Destroy(c); }
            foreach (MonoBehaviour b in model.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (b is ScarfRibbon scarf) { scarf.color = colour; continue; }
                b.enabled = false;
                Destroy(b);
            }
            var all = new System.Collections.Generic.List<Renderer>(model.GetComponentsInChildren<Renderer>(true));
            var bodies = new System.Collections.Generic.List<Renderer>();
            var outlines = new System.Collections.Generic.List<Renderer>();
            var scarves = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer r in all)
            {
                r.enabled = true;
                Material m = r.sharedMaterial;
                if (r.GetComponent<ScarfRibbon>() != null) scarves.Add(r);
                else if (m != null && m.HasProperty(OutlineId)) outlines.Add(r);
                else bodies.Add(r);
            }
            ghost._all = all.ToArray();
            ghost._bodies = bodies.ToArray();
            ghost._outlines = outlines.ToArray();
            ghost._scarves = scarves.ToArray();
            ghost._block = new MaterialPropertyBlock();
            ghost.SetLook(colour, false);
            root.SetActive(true);
            return ghost;
        }

        /// <summary>Stand at <paramref name="position"/> (feet), nose up/down with the road, leaning; the model is hidden
        /// (only the name tag shows) when <paramref name="showModel"/> is false.</summary>
        public void Place(Vector3 position, float pitch, float lean, bool showModel)
        {
            transform.SetPositionAndRotation(position, Quaternion.Euler(pitch, 0f, lean));
            if (showModel == ModelShown) return;
            ModelShown = showModel;
            // Renderers only: switching the model off and on would rebuild the scarf's mesh every time.
            foreach (Renderer r in _all) if (r != null) r.enabled = showModel;
        }

        /// <summary>The rider's colour, or grey while the signal is lost.</summary>
        public void SetLook(Color colour, bool grey)
        {
            if (_looked && grey == _grey && colour == _colour) return;
            _looked = true;
            _colour = colour;
            _grey = grey;
            BodyTint = grey ? GreyBody : Color.Lerp(Color.white, colour, 0.55f);
            foreach (Renderer r in _bodies) Tint(r, BaseColorId, BodyTint);
            foreach (Renderer r in _outlines)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_block);
                _block.SetColor(InkColorId, grey ? GreyInk : colour);
                _block.SetFloat(OutlineId, grey ? 1.8f : 3f);
                r.SetPropertyBlock(_block);
            }
            foreach (Renderer r in _scarves) Tint(r, BaseColorId, grey ? new Color(0.5f, 0.5f, 0.5f) : Color.white);
        }

        void Tint(Renderer r, int id, Color c)
        {
            if (r == null) return;
            r.GetPropertyBlock(_block);
            _block.SetColor(id, c);
            r.SetPropertyBlock(_block);
        }
    }
}
