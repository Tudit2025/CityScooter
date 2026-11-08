using System.Collections;
using CityScooter.Devices;
using CityScooter.Gameplay;
using CityScooter.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace CityScooter.Tests
{
    public class ComicMainSceneTests
    {
        Scene main;

        [UnityTest]
        public IEnumerator MainScene_UsesGeneratedKit_HidesEditorPreview_AndRidesDownhill()
        {
            DeviceHub.Instance?.Mixer.Reset();
            yield return SceneManager.LoadSceneAsync("Main",LoadSceneMode.Additive);
            main=SceneManager.GetSceneByName("Main");SceneManager.SetActiveScene(main);
            yield return null;
            var game=GameController.Instance;
            if(game!=null)ClassicStreet.Use(game); // v0.3: the comic street as before districts
            Assert.IsNotNull(game);
            Assert.IsTrue(game.track.theme.orderedChunks);
            Assert.IsTrue(game.track.buildLeadIn);
            Assert.AreEqual(4,game.track.theme.chunks.Length);
            var preview=Object.FindAnyObjectByType<ComicScenePreview>(FindObjectsInactive.Include);
            Assert.IsNotNull(preview);Assert.IsFalse(preview.gameObject.activeSelf);
            Assert.IsNotNull(game.rider.model.GetComponentInChildren<ScarfRibbon>());
            Assert.GreaterOrEqual(game.track.ActiveChunkCount,9);
            Assert.AreEqual(game.track.SurfaceHeightAt(2),game.rider.transform.position.y,.001f);
            game.StartRun();float start=game.rider.transform.position.y;
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(RunState.Running,game.State);
            Assert.Less(game.rider.transform.position.y,start-.5f);
            Assert.AreEqual(game.track.SurfaceHeightAt(game.rider.transform.position.z),game.rider.transform.position.y,.002f);
            Assert.Less(Camera.main.transform.position.y,4.2f);
        }

        [UnityTest]
        public IEnumerator CityCurl_FollowsRecycledTrackAndRestart_WithoutChangingGround()
        {
            DeviceHub.Instance?.Mixer.Reset();
            yield return SceneManager.LoadSceneAsync("Main",LoadSceneMode.Additive);
            main=SceneManager.GetSceneByName("Main");SceneManager.SetActiveScene(main);
            yield return null;
            var game=GameController.Instance;
            if(game!=null)ClassicStreet.Use(game); // v0.3: the comic street as before districts
            var sky=Object.FindAnyObjectByType<ComicCityBackdrop>();
            Assert.IsNotNull(sky);Assert.IsNotNull(sky.recipe);
            Assert.Greater(sky.recipe.cityBendStrength,0);
            Assert.IsEmpty(sky.GetComponentsInChildren<Collider>());
            Assert.AreEqual("CityScooter/Comic Panorama Sky",RenderSettings.skybox.shader.name);
            Assert.IsNotNull(RenderSettings.skybox.GetTexture("_ComicSkyTexture"));
            Assert.IsNull(sky.transform.Find("DistantCity"));Assert.IsNull(sky.transform.Find("ComicCloudShell"));
            Assert.AreEqual(1,sky.GetComponentsInChildren<MeshRenderer>().Length,"Only the visual road extension is geometry.");
            // Jump several pooled chunks ahead. Their physical height must remain the original straight slope.
            game.rider.ResetRider(new Vector3(0,0,242));
            yield return null;
            sky.Refresh();
            Vector4 bend=Shader.GetGlobalVector("_ComicWorldBend");
            Assert.AreEqual(242,bend.x,.001f);
            Assert.AreEqual(-242*sky.recipe.downhillGrade,game.rider.transform.position.y,.002f);
            Assert.AreEqual(game.rider.transform.position.y,sky.transform.position.y,.002f);
            Assert.AreEqual(-400*sky.recipe.downhillGrade,game.track.SurfaceHeightAt(400),.002f);
            Assert.GreaterOrEqual(game.track.ActiveChunkCount,9);
            Camera.main.GetComponent<FollowCamera>().SnapToTarget();
            CaptureRuntimeHorizon(Camera.main);
            game.StartRun();game.rider.Stop();
            yield return null;
            sky.Refresh();
            Assert.AreEqual(game.startPosition.z,Shader.GetGlobalVector("_ComicWorldBend").x,.001f);
            yield return SceneManager.UnloadSceneAsync(main);
            Assert.AreEqual(Vector4.zero,Shader.GetGlobalVector("_ComicWorldBend"),"Scene unload must not bend other themes or asset previews.");
            Assert.AreEqual(Vector4.zero,Shader.GetGlobalVector("_ComicHorizon"));
            Assert.AreEqual(Vector4.zero,Shader.GetGlobalVector("_ComicSkyMapping"));
        }

        static void CaptureRuntimeHorizon(Camera camera)
        {
            var rt=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB,4);
            RenderTexture previous=RenderTexture.active;RenderTexture oldTarget=camera.targetTexture;
            Texture2D image=null;
            try
            {
                camera.targetTexture=rt;
                var request=new RenderPipeline.StandardRequest{destination=rt};
                for(int pass=0;pass<2;pass++)
                    if(RenderPipeline.SupportsRenderRequest(camera,request))RenderPipeline.SubmitRenderRequest(camera,request);else camera.Render();
                RenderTexture.active=rt;image=new Texture2D(1600,900,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
                string folder=System.IO.Path.Combine(Application.dataPath,"../art-src/comic-city/previews");
                System.IO.Directory.CreateDirectory(folder);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,"07-runtime-after-recycle.png"),image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=previous;camera.targetTexture=oldTarget;
                RenderTexture.ReleaseTemporary(rt);if(image!=null)Object.Destroy(image);
            }
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if(main.IsValid()&&main.isLoaded)yield return SceneManager.UnloadSceneAsync(main);
            DeviceHub.Instance?.Mixer.Reset();
        }
    }
}
