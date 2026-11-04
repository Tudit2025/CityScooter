using UnityEngine;

namespace CityScooter.Rendering
{
    /// <summary>
    /// Put on the root of a one-shot VFX prefab. Each time the effect is enabled (spawned or taken from a pool) it
    /// scales every child particle system's bursts and emission rate by <see cref="QualityAutoTuner.VfxDensity"/>.
    /// Values authored in the prefab are remembered, so repeated enables never compound.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("CityScooter/Rendering/VFX Density Scaler")]
    public sealed class VfxDensityScaler : MonoBehaviour
    {
        struct Authored
        {
            public ParticleSystem System;
            public ParticleSystem.Burst[] Bursts;
            public float RateMultiplier;
        }

        Authored[] _authored;

        void OnEnable() => ApplyDensity(QualityAutoTuner.VfxDensity);

        public void ApplyDensity(float density)
        {
            if (_authored == null) Capture();
            density = Mathf.Clamp01(density);
            foreach (Authored a in _authored)
            {
                if (a.System == null) continue;
                ParticleSystem.EmissionModule emission = a.System.emission;
                emission.rateOverTimeMultiplier = a.RateMultiplier * density;
                if (a.Bursts.Length == 0) continue;
                var scaled = new ParticleSystem.Burst[a.Bursts.Length];
                for (int i = 0; i < scaled.Length; i++)
                {
                    ParticleSystem.Burst b = a.Bursts[i];
                    b.count = Scale(b.count, density);
                    scaled[i] = b;
                }
                emission.SetBursts(scaled);
            }
        }

        void Capture()
        {
            ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
            _authored = new Authored[systems.Length];
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem.EmissionModule emission = systems[i].emission;
                var bursts = new ParticleSystem.Burst[emission.burstCount];
                emission.GetBursts(bursts);
                _authored[i] = new Authored
                {
                    System = systems[i],
                    Bursts = bursts,
                    RateMultiplier = emission.rateOverTimeMultiplier,
                };
            }
        }

        // Keeps at least one particle per burst so an effect never disappears completely.
        static ParticleSystem.MinMaxCurve Scale(ParticleSystem.MinMaxCurve curve, float k)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    curve.constant = Mathf.Max(1f, Mathf.Round(curve.constant * k));
                    break;
                case ParticleSystemCurveMode.TwoConstants:
                    curve.constantMin = Mathf.Max(1f, Mathf.Round(curve.constantMin * k));
                    curve.constantMax = Mathf.Max(1f, Mathf.Round(curve.constantMax * k));
                    break;
                default:
                    curve.curveMultiplier *= k;
                    break;
            }
            return curve;
        }
    }
}
