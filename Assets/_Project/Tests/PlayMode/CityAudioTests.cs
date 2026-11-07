using System.Collections;
using System.Linq;
using CityScooter.Audio;
using CityScooter.Devices;
using CityScooter.Game;
using CityScooter.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CityScooter.Tests
{
    public class CityAudioTests
    {
        Scene scene;
        GameController game;
        CityAudioDirector audio;
        AudioSource Loop(string id)=>audio.GetComponentsInChildren<AudioSource>().Single(s=>s.name=="Loop_"+id);

        [UnitySetUp]
        public IEnumerator Setup()
        {
            DeviceHub.Instance?.Mixer.Reset();
            yield return SceneManager.LoadSceneAsync("Main",LoadSceneMode.Additive);
            scene=SceneManager.GetSceneByName("Main");SceneManager.SetActiveScene(scene);
            yield return null;
            game=GameController.Instance;audio=game.GetComponentInChildren<CityAudioDirector>();
            Assert.IsNotNull(audio);Assert.AreEqual(21,audio.bank.cues.Length);
            audio.loadPlayerPreferences=false;audio.SetMuted(false,false);audio.SetVolumes(.75f,.45f,.5f,.85f,false);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale=1;
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            DeviceHub.Instance?.Mixer.Reset();
        }

        [UnityTest]
        public IEnumerator Riding_Braking_Jumping_AndLandingDriveTheMix()
        {
            game.StartRun();Assert.AreEqual("UI_Start",audio.LastCueId);
            yield return new WaitForSeconds(.25f);
            var roll=Loop("SCO_Roll_Loop");var brake=Loop("SCO_Brake_Loop");
            Assert.Greater(roll.volume,.05f);Assert.Greater(Loop("MUS_HotRoadway_160_Loop").volume,.05f);
            float rolling=roll.volume;
            DeviceHub.Instance.Receive("audio-test","br");
            yield return new WaitForSeconds(.12f);
            Assert.IsTrue(game.rider.Braking);Assert.Greater(brake.volume,.02f);
            yield return new WaitForSeconds(.3f);
            DeviceHub.Instance.Receive("audio-test","jp");
            yield return new WaitForSeconds(.2f);
            Assert.IsTrue(game.rider.Airborne);Assert.AreEqual("SCO_Jump",audio.LastCueId);
            Assert.Less(roll.volume,rolling*.4f);Assert.Less(brake.volume,.01f);
            yield return new WaitForSeconds(.6f);
            Assert.IsFalse(game.rider.Airborne);StringAssert.StartsWith("SCO_Land_",audio.LastCueId);
            Assert.Greater(roll.volume,.05f);
        }

        [UnityTest]
        public IEnumerator Pickup_Crash_AndRestartUseActualGameplayEvents()
        {
            game.StartRun();
            float z=game.rider.transform.position.z+4;
            game.track.PlaceAt("placeholder-gift",new Vector3(0,game.track.SurfaceHeightAt(z),z));
            float until=Time.time+2;
            while(game.Gifts==0&&Time.time<until)yield return null;
            Assert.AreEqual(1,game.Gifts);StringAssert.StartsWith("UI_Pickup_",audio.LastCueId);
            z=game.rider.transform.position.z+4;
            game.track.PlaceAt("placeholder-obstacle-1",new Vector3(0,game.track.SurfaceHeightAt(z),z));
            until=Time.time+2;
            while(game.State==RunState.Running&&Time.time<until)yield return null;
            Assert.AreEqual(RunState.Crashed,game.State);StringAssert.StartsWith("SCO_Crash_",audio.LastCueId);
            yield return new WaitForSeconds(.5f);
            Assert.Less(Loop("SCO_Roll_Loop").volume,.005f);
            game.StartRun();Assert.AreEqual("UI_Start",audio.LastCueId);
            yield return new WaitForSeconds(.2f);
            Assert.Greater(Loop("SCO_Roll_Loop").volume,.05f);
        }

        [UnityTest]
        public IEnumerator VoiceBudget_Mute_Pause_AndReenableStayStable()
        {
            game.StartRun();yield return new WaitForSeconds(.2f);
            Assert.AreEqual(14,audio.GetComponentsInChildren<AudioSource>().Length);
            Assert.AreEqual(1,Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length);
            foreach(string id in new[]{"VEH_Horn_01","VEH_Horn_02","VEH_Pass_01","VEH_Pass_02","SCO_Scrape_01","SCO_Scrape_02","SCO_Land_01"})
                Assert.IsTrue(audio.PlayCue(id));
            Assert.IsFalse(audio.PlayCue("VEH_Horn_01"),"Horn cooldown must suppress a repeated request.");
            Assert.IsTrue(audio.PlayCue("SCO_Crash_01"),"A crash must replace a less important voice in a full pool.");
            Assert.LessOrEqual(audio.ActiveOneShots,8);
            for(int i=0;i<50;i++)audio.PlayCue("UI_Click");
            Assert.AreEqual(14,audio.GetComponentsInChildren<AudioSource>().Length);
            Assert.LessOrEqual(audio.GetComponentsInChildren<AudioSource>().Sum(s=>s.volume),audio.bank.maximumGainSum+.001f);
            audio.SetMuted(true,false);
            Assert.IsTrue(audio.GetComponentsInChildren<AudioSource>().All(s=>s.volume==0));
            audio.SetMuted(false,false);audio.SetVolumes(1,0,.5f,1,false);
            Assert.AreEqual(0,Loop("MUS_HotRoadway_160_Loop").volume);Assert.Greater(Loop("SCO_Roll_Loop").volume,0);
            Time.timeScale=0;yield return null;yield return null;
            Assert.IsTrue(audio.GetComponentsInChildren<AudioSource>().All(s=>s.volume==0));
            Time.timeScale=1;yield return null;yield return null;
            Assert.Greater(Loop("SCO_Roll_Loop").volume,0);
            audio.enabled=false;audio.enabled=true;
            int count=audio.PlayedCueCount;GameEvents.RaiseGiftCollected();
            Assert.AreEqual(count+1,audio.PlayedCueCount,"Exactly one event subscription must survive re-enable.");
            Assert.AreEqual(14,audio.GetComponentsInChildren<AudioSource>().Length);
        }

        [UnityTest]
        public IEnumerator SoundPanel_ControlsTheLiveMix()
        {
            var canvas=audio.GetComponentInChildren<Canvas>();Assert.IsNotNull(canvas);
            var sound=canvas.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Sound");
            Assert.IsNotNull(EventSystem.current);Assert.IsNotNull(EventSystem.current.currentInputModule);
            ExecuteEvents.Execute(sound.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
            var panel=canvas.transform.Find("Sound Settings");Assert.IsTrue(panel.gameObject.activeSelf);
            var slider=panel.GetComponentsInChildren<Slider>().Single(s=>s.name=="Music Volume");
            slider.value=.24f;Assert.AreEqual(.24f,audio.Music,.001f);
            game.StartRun();yield return new WaitForSeconds(.2f);
            CapturePanel(canvas);
        }

        static void CapturePanel(Canvas canvas)
        {
            var camera=Camera.main;var oldMode=canvas.renderMode;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            var target=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB,4);
            var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;Texture2D image=null;
            try
            {
                camera.targetTexture=target;Canvas.ForceUpdateCanvases();
                var request=new RenderPipeline.StandardRequest{destination=target};
                if(RenderPipeline.SupportsRenderRequest(camera,request))RenderPipeline.SubmitRenderRequest(camera,request);else camera.Render();
                RenderTexture.active=target;image=new Texture2D(1600,900,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
                string folder=System.IO.Path.Combine(Application.dataPath,"../audio-src/comic-city/previews");System.IO.Directory.CreateDirectory(folder);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,"Audio_Controls.png"),image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=oldTarget;RenderTexture.active=oldActive;canvas.renderMode=oldMode;canvas.worldCamera=null;
                RenderTexture.ReleaseTemporary(target);if(image!=null)Object.Destroy(image);
            }
        }
    }
}
