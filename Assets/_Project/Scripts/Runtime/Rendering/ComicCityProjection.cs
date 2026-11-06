using UnityEngine;

namespace CityScooter.Rendering
{
    /// <summary>Authoring/report counterpart to ComicWorldBend.hlsl; physics never calls this.</summary>
    public static class ComicCityProjection
    {
        public static float Rise(ComicCityRecipe r,float distance)
        {
            float d=Mathf.Max(0,distance-r.cityBendStart),a=Mathf.Max(0,r.curveTransitionDistance-r.cityBendStart);
            if(d<=a)return d*d*r.cityBendStrength;
            float t=d-a,finalSlope=r.horizonRiseSlope+r.downhillGrade;
            return a*a*r.cityBendStrength+finalSlope*t+(2*a*r.cityBendStrength-finalSlope)*r.curveEaseLength*(1-Mathf.Exp(-t/r.curveEaseLength));
        }
        public static float Scale(ComicCityRecipe r,float distance)
        {
            float t=Mathf.Clamp01((distance-r.horizonTaperStart)/(r.horizonTaperEnd-r.horizonTaperStart));
            return Mathf.Lerp(1,r.horizonMinimumScale,t*t*(3-2*t));
        }
        public static Vector3 Position(ComicCityRecipe r,Vector3 world,Vector3 origin)
        {
            float d=world.z-origin.z,scale=Scale(r,d),ground=origin.y-d*r.downhillGrade;
            return new Vector3(world.x*scale,ground+Rise(r,d)+(world.y-ground)*scale,world.z);
        }
        public static Vector4 SkyMapping(ComicCityRecipe r)=>new Vector4(r.skyYaw*Mathf.Deg2Rad,r.skyElevation*Mathf.Deg2Rad,r.skyVerticalScale,r.skyPanorama!=null?1:0);
        public static Color SkyHaze(ComicCityRecipe r)=>new Color(.60f,.74f,.83f,r.skyHaze);
    }
}
