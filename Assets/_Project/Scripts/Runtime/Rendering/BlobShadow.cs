using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CityScooter.Rendering
{
    /// <summary>
    /// Fake contact shadow: a soft dark quad kept on the road under a target (the rider). By default it only shows
    /// when real main-light shadows are off (e.g. after <see cref="QualityAutoTuner"/> drops a tier), so the rider
    /// never looks like it floats. Shrinks and fades while the target is in the air.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("CityScooter/Rendering/Blob Shadow")]
    public sealed class BlobShadow : MonoBehaviour
    {
        public enum Visibility
        {
            Always = 0,
            /// <summary>Only when URP main-light shadows are off (or the sun casts none).</summary>
            WhenRealShadowsOff = 1,
        }

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public Visibility visibility = Visibility.WhenRealShadowsOff;

        [Tooltip("What the blob sits under. Empty = the parent transform.")]
        public Transform target;

        [Tooltip("Road surface height (y = 0 by convention).")]
        public float groundHeight;

        [Min(0f), Tooltip("Lift above the road to avoid z-fighting.")]
        public float lift = 0.03f;

        [Tooltip("Width (x) and length (z) in metres.")]
        public Vector2 size = new Vector2(0.9f, 1.7f);

        [Tooltip("Shadow tint; alpha = darkness at the centre.")]
        public Color color = new Color(0.36f, 0.39f, 0.5f, 0.6f);

        [Min(0.01f), Tooltip("Height above the road at which the blob has faded out (jumps).")]
        public float fadeHeight = 3f;

        [Range(0f, 1f), Tooltip("Size at the top of a jump, relative to the ground size.")]
        public float minScaleInAir = 0.55f;

        [Tooltip("The quad that draws the blob (child, rotated to lie flat).")]
        public Renderer blobRenderer;

        MaterialPropertyBlock _mpb;

        void LateUpdate() => UpdateBlob();

        public void UpdateBlob()
        {
            if (blobRenderer == null) return;
            bool show = visibility == Visibility.Always || !RealShadowsActive();
            if (blobRenderer.enabled != show) blobRenderer.enabled = show;
            if (!show) return;

            Transform t = target != null ? target : transform.parent;
            float air = 0f;
            if (t != null)
            {
                Vector3 p = t.position;
                air = fadeHeight > 0f ? Mathf.Clamp01((p.y - groundHeight) / fadeHeight) : 0f;
                transform.SetPositionAndRotation(new Vector3(p.x, groundHeight + lift, p.z), Quaternion.Euler(0f, t.eulerAngles.y, 0f));
            }

            float k = Mathf.Lerp(1f, minScaleInAir, air);
            Vector3 world = new Vector3(size.x * k, 1f, size.y * k);
            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            transform.localScale = new Vector3(
                world.x / NonZero(parentScale.x), world.y / NonZero(parentScale.y), world.z / NonZero(parentScale.z));

            _mpb ??= new MaterialPropertyBlock();
            Color c = color;
            c.a *= 1f - air;
            _mpb.SetColor(BaseColorId, c);
            blobRenderer.SetPropertyBlock(_mpb);
        }

        /// <summary>True when the active URP asset renders main-light shadows and the sun casts them.</summary>
        public static bool RealShadowsActive()
        {
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)) return false;
            if (!urp.supportsMainLightShadows || urp.shadowDistance <= 0.01f) return false;
            Light sun = RenderSettings.sun;
            return sun == null || (sun.isActiveAndEnabled && sun.shadows != LightShadows.None);
        }

        static float NonZero(float v) => Mathf.Abs(v) < 1e-4f ? 1e-4f : v;
    }
}
