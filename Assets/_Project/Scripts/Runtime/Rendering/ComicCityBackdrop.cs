using CityScooter.Gameplay;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityScooter.Rendering
{
    /// <summary>Image-sky parameters, visual road extension and world curl. Never modifies collision or track heights.</summary>
    [ExecuteAlways]
    public sealed class ComicCityBackdrop : MonoBehaviour
    {
        public RiderController rider;
        public ComicCityRecipe recipe;
        static readonly int BendId = Shader.PropertyToID("_ComicWorldBend");
        static readonly int HorizonId=Shader.PropertyToID("_ComicHorizon");
        static readonly int CurveId=Shader.PropertyToID("_ComicCurveTail");
        static readonly int SkyMappingId=Shader.PropertyToID("_ComicSkyMapping");
        static readonly int SkyTextureId=Shader.PropertyToID("_ComicSkyTexture");
        static readonly int SkyHazeId=Shader.PropertyToID("_ComicSkyHaze");
        static ComicCityBackdrop owner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetGlobals()
        {
            owner=null;Shader.SetGlobalVector(BendId,Vector4.zero);
            Shader.SetGlobalVector(HorizonId,Vector4.zero);Shader.SetGlobalVector(CurveId,Vector4.zero);
            Shader.SetGlobalVector(SkyMappingId,Vector4.zero);Shader.SetGlobalTexture(SkyTextureId,null);
        }

        void OnEnable() { RenderPipelineManager.beginCameraRendering += BeforeCamera; Refresh(); }
        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeforeCamera;
            if(owner==this)ResetGlobals();
        }
        void LateUpdate() => Refresh();
        void BeforeCamera(ScriptableRenderContext context, Camera camera) => Refresh();

        public void Refresh()
        {
            // Prefab import/asset previews have no rider: they must not affect the active game scene.
            if(rider==null||recipe==null)return;
            float z=rider.transform.position.z;
            float y=Application.isPlaying&&rider.track!=null?rider.track.SurfaceHeightAt(z):rider.transform.position.y;
            transform.position=new Vector3(0,y,z);
            owner=this;
            Shader.SetGlobalVector(BendId,new Vector4(z,recipe.cityBendStart,recipe.cityBendStrength,y));
            Shader.SetGlobalVector(HorizonId,new Vector4(recipe.horizonTaperStart,recipe.horizonTaperEnd,recipe.horizonMinimumScale,recipe.downhillGrade));
            Shader.SetGlobalVector(CurveId,new Vector4(recipe.curveTransitionDistance,recipe.horizonRiseSlope,recipe.curveEaseLength,0));
            Shader.SetGlobalTexture(SkyTextureId,recipe.skyPanorama);
            Shader.SetGlobalVector(SkyMappingId,ComicCityProjection.SkyMapping(recipe));
            Shader.SetGlobalColor(SkyHazeId,ComicCityProjection.SkyHaze(recipe).linear);
        }
    }
}
