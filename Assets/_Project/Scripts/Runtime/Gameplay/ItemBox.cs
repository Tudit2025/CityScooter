using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// Item race (v0.5, docs/item-race.md): one question-mark box of a row of three on the race road. It only turns and
    /// bobs; RaceItems decides where the rows are (by distance alone, so every game of a race has them in the same
    /// places), puts the boxes there and notices when the rider rides through one. No collider: nothing to crash into.
    /// Built by <see cref="RaceItemArt.Box"/>.
    /// </summary>
    public sealed class ItemBox : MonoBehaviour
    {
        public float degreesPerSecond = 75f;
        public float bobHeight = 0.13f;
        public float bobSeconds = 1.7f;

        /// <summary>Which row this box is in now (0 = the first row, 150 m after the start), and which lane (0..2).</summary>
        public int Row { get; internal set; } = -1;
        public int Lane { get; private set; }

        Transform _art;
        float _phase;

        internal void Init(Transform art, int lane)
        {
            _art = art;
            Lane = lane;
            _phase = lane * 0.37f;
            Pose();
        }

        void Update() => Pose();

        void Pose()
        {
            if (_art == null) return;
            float t = Time.time;
            // Tipped onto an edge and turning, like a prize in a comic.
            _art.localRotation = Quaternion.Euler(0f, t * degreesPerSecond + _phase * 360f, 0f) * Quaternion.Euler(16f, 0f, 12f);
            _art.localPosition = Vector3.up * (Mathf.Sin((t / Mathf.Max(0.1f, bobSeconds) + _phase) * Mathf.PI * 2f) * bobHeight);
        }
    }
}
