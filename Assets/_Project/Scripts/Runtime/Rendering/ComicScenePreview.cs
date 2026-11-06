using UnityEngine;

namespace CityScooter.Rendering
{
    /// <summary>Authored editor-only view of the pooled runtime street. Never duplicates it during play.</summary>
    public sealed class ComicScenePreview : MonoBehaviour
    {
        void Awake() => gameObject.SetActive(false);
    }
}
