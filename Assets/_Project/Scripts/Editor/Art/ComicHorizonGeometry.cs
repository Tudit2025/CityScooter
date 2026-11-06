using CityScooter.Rendering;
using UnityEngine;

namespace CityScooter.EditorTools.Art
{
    /// <summary>Only the road continues beyond the playable range. All sky/city silhouettes live in the panorama.</summary>
    internal static class ComicHorizonGeometry
    {
        public static ComicMesh Road(ComicCityRecipe r)
        {
            var mesh=new ComicMesh();
            // The live street ends 320..360 m ahead. This strip overlaps underneath it to avoid z-fighting.
            const float start=280;float end=r.horizonTaperEnd+40;
            for(float z=start;z<end;z+=4)
            {
                float next=Mathf.Min(end,z+4);
                void Strip(float left,float right,float height,Color color)
                {
                    Vector3 P(float x,float at)=>new Vector3(x,-r.downhillGrade*at+height-.08f,at);
                    mesh.Quad(P(left,z),P(left,next),P(right,next),P(right,z),color,false);
                }
                Strip(-r.roadHalfWidth,r.roadHalfWidth,0,r.asphalt);
                for(int side=-1;side<=1;side+=2)
                {
                    float inner=side*r.roadHalfWidth,outer=side*(r.roadHalfWidth+r.sidewalkWidth);
                    Strip(Mathf.Min(inner,outer),Mathf.Max(inner,outer),r.curbHeight,r.pavement);
                    float edge=side*(r.roadHalfWidth-.15f);
                    Strip(edge-.04f,edge+.04f,.025f,r.cream);
                    if(((int)z/4)%2==0)Strip(side*.12f-.05f,side*.12f+.05f,.03f,r.yellow);
                }
            }
            return mesh;
        }
    }
}
