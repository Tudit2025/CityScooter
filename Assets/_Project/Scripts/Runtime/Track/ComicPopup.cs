using UnityEngine;

namespace CityScooter.Track
{
    /// <summary>"POW!", "DING!", "!" ...: pops up facing the camera, floats up a little, then hides itself.</summary>
    public sealed class ComicPopup : MonoBehaviour
    {
        public float life = 0.75f;
        public float rise = 0.8f;
        float _age;
        Vector3 _start;
        Transform _follow;
        Vector3 _offset;

        public void Show(Vector3 position, Transform follow = null)
        {
            _follow = follow;
            _offset = follow != null ? position - follow.position : Vector3.zero;
            _start = position;
            _age = 0f;
            gameObject.SetActive(true);
            Pose();
        }

        void LateUpdate()
        {
            _age += Time.deltaTime;
            if (_age >= life) { gameObject.SetActive(false); return; }
            Pose();
        }

        void Pose()
        {
            float t = Mathf.Clamp01(_age / Mathf.Max(0.05f, life));
            float pop = _age < 0.12f ? Mathf.Lerp(0.3f, 1.2f, _age / 0.12f) : Mathf.Lerp(1.2f, 1f, Mathf.Clamp01((_age - 0.12f) / 0.1f));
            Vector3 basePos = _follow != null && _follow.gameObject.activeInHierarchy ? _follow.position + _offset : _start;
            transform.position = basePos + Vector3.up * rise * t;
            transform.localScale = Vector3.one * pop;
            Camera cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation; // the text faces -Z, towards the camera
        }
    }
}
