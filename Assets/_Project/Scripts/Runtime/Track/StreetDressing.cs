using UnityEngine;

namespace CityScooter.Track
{
    /// <summary>Switches baked roadside arrangements on pooled chunks, without spawning more renderers or colliders.</summary>
    public sealed class StreetDressing : MonoBehaviour
    {
        public MeshFilter surface;
        public MeshFilter outline;
        public Mesh[] layouts;

        public void ApplyLayout(int seed, int chunkIndex)
        {
            if(layouts==null||layouts.Length==0)return;
            uint hash;
            unchecked
            {
                hash=(uint)seed ^ ((uint)chunkIndex*0x9e3779b9u);
                hash^=hash>>16;hash*=0x7feb352du;hash^=hash>>15;hash*=0x846ca68bu;hash^=hash>>16;
            }
            Mesh mesh=layouts[hash%(uint)layouts.Length];
            if(surface!=null)surface.sharedMesh=mesh;
            if(outline!=null)outline.sharedMesh=mesh;
        }
    }
}
