using System.Collections;
using System.Linq;
using CityScooter.Audio;
using CityScooter.Devices;
using CityScooter.Gameplay;
using CityScooter.Track;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityScooter.Tests
{
    public class RideProgressionTests
    {
        Scene main;
        GameController game;
        GameTuning tuning;
        CityAudioDirector audio;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            DeviceHub.Instance?.Mixer.Reset();
            yield return SceneManager.LoadSceneAsync("Main",LoadSceneMode.Additive);
            main=SceneManager.GetSceneByName("Main");SceneManager.SetActiveScene(main);yield return null;
            game=GameController.Instance;audio=game.GetComponentInChildren<CityAudioDirector>();audio.loadPlayerPreferences=false;
            tuning=Object.Instantiate(game.tuning);game.tuning=game.rider.tuning=game.track.tuning=tuning;
            tuning.obstacleChanceStart=tuning.obstacleChanceMax=tuning.giftChance=0;
            tuning.safeStartDistance=100000;
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale=1;
            if(main.IsValid()&&main.isLoaded)yield return SceneManager.UnloadSceneAsync(main);
            if(tuning!=null)Object.Destroy(tuning);DeviceHub.Instance?.Mixer.Reset();yield return null;
        }
        void ClearTestPath()
        {
            foreach(var obstacle in game.track.GetComponentsInChildren<Obstacle>())obstacle.gameObject.SetActive(false);
        }
        IEnumerator Steer(float direction,float seconds)
        {
            float until=Time.time+seconds;
            while(Time.time<until){DeviceHub.Instance.Receive("sidewalk-test",direction>0?"st:100":"st:-100");yield return null;}
        }
        AudioSource Music()=>audio.GetComponentsInChildren<AudioSource>().Single(s=>s.name=="Loop_MUS_HotRoadway_160_Loop");

        [UnityTest]
        public IEnumerator Sidewalks_AreRideable_AtCorrectHeight_WithBoundedCameraRotation()
        {
            var follow=Camera.main.GetComponent<FollowCamera>();Assert.IsTrue(follow.directionalFollow);
            game.StartRun();ClearTestPath();
            yield return Steer(1,1.5f);
            Assert.AreEqual(RunState.Running,game.State);Assert.Greater(game.rider.transform.position.x,7.4f);
            CheckFootpathAndCamera(1);
            Capture("09-sidewalk-right.png");
            yield return Steer(-1,2.65f);
            Assert.AreEqual(RunState.Running,game.State);Assert.Less(game.rider.transform.position.x,-7.4f);
            CheckFootpathAndCamera(-1);
            Capture("10-sidewalk-left.png");
            DeviceHub.Instance.Mixer.Reset();DeviceHub.Instance.Receive("sidewalk-test","jp");
            yield return new WaitForSeconds(.15f);Assert.IsTrue(game.rider.Airborne);
            yield return new WaitForSeconds(.7f);Assert.IsFalse(game.rider.Airborne);
            CheckFootpathAndCamera(-1);
            game.StartRun();
            Assert.AreEqual(0,Camera.main.transform.position.x,.001f);
            Assert.AreEqual(0,Mathf.DeltaAngle(0,Camera.main.transform.eulerAngles.y),.001f);
        }
        void CheckFootpathAndCamera(int side)
        {
            Vector3 p=game.rider.transform.position;var follow=Camera.main.GetComponent<FollowCamera>();
            Assert.AreEqual(8,game.track.RideableHalfWidthAt(p.z),.001f);
            Assert.LessOrEqual(Mathf.Abs(p.x),8-game.rider.radius+.001f);
            Assert.AreEqual(.2f,p.y-game.track.SurfaceHeightAt(p.z),.002f);
            Assert.AreEqual(follow.offset.x,Camera.main.transform.position.x,.002f);
            float yaw=Mathf.DeltaAngle(0,Camera.main.transform.eulerAngles.y);
            Assert.Greater(yaw*side,10);Assert.LessOrEqual(Mathf.Abs(yaw),follow.maxYaw+.02f);
            Vector3 screen=Camera.main.WorldToViewportPoint(p+Vector3.up);
            Assert.That(screen.x,Is.InRange(.05f,.95f));Assert.That(screen.y,Is.InRange(.05f,.95f));
        }

        [UnityTest]
        public IEnumerator TimedLimit_DrivesMusic_ContinuesAfterRamp_AndResetsOnRestart()
        {
            Assert.AreEqual(44,tuning.maxSpeed);Assert.AreEqual(5,tuning.maxSpeedMultiplier);
            Assert.AreEqual(600,tuning.speedRampSeconds);Assert.AreEqual(.1f,tuning.postRampMultiplierPerMinute);
            // Compress the clock so the actual update/audio paths exercise the ten-minute transition.
            tuning.speedRampSeconds=1;tuning.difficultyDistance=1;tuning.acceleration=1000;
            game.StartRun();Assert.AreEqual(1,Music().pitch,.001f);
            yield return new WaitForSeconds(.55f);
            Assert.Greater(game.rider.CurrentMaxSpeed,120);Assert.Greater(Music().pitch,1.08f);
            float limit=game.rider.CurrentMaxSpeed,rate=Music().pitch,speed=game.rider.Speed;
            DeviceHub.Instance.Receive("progression-test","br");yield return new WaitForSeconds(.15f);
            Assert.Less(game.rider.Speed,speed);Assert.Greater(game.rider.CurrentMaxSpeed,limit);Assert.Greater(Music().pitch,rate);
            yield return new WaitForSeconds(1);
            Assert.Greater(game.rider.CurrentMaxSpeed,220);Assert.That(Music().pitch,Is.InRange(1.49f,1.5f));
            Assert.LessOrEqual(game.rider.Speed,game.rider.CurrentMaxSpeed+.01f);Assert.That(game.rider.SpeedLevel,Is.InRange(0,9));
            Time.timeScale=0;float paused=game.rider.RunTime;
            yield return new WaitForSecondsRealtime(.12f);Assert.AreEqual(paused,game.rider.RunTime);
            Time.timeScale=1;game.rider.Stop();float stopped=game.rider.CurrentMaxSpeed;
            yield return new WaitForSeconds(.1f);Assert.AreEqual(stopped,game.rider.CurrentMaxSpeed);
            game.StartRun();Assert.AreEqual(44,game.rider.CurrentMaxSpeed,.001f);Assert.AreEqual(0,game.rider.RunTime);Assert.AreEqual(1,Music().pitch,.001f);
        }

        [UnityTest]
        public IEnumerator HighSpeedSidewalkCollision_IsSweptAcrossTheFrame()
        {
            tuning.startSpeed=tuning.maxSpeed=220;tuning.maxSpeedMultiplier=1;tuning.postRampMultiplierPerMinute=0;
            game.startPosition=new Vector3(6,0,2);game.StartRun();ClearTestPath();
            float z=game.rider.transform.position.z+2;
            game.track.PlaceAt("placeholder-obstacle-1",new Vector3(6,game.track.SurfaceHeightAt(6,z),z));
            yield return new WaitForSeconds(.12f);
            Assert.AreEqual(RunState.Crashed,game.State,"The rider must not tunnel through an obstacle between frames at 220 m/s.");
            StringAssert.StartsWith("SCO_Crash_",audio.LastCueId);
        }

        [UnityTest]
        public IEnumerator RecycledDressing_KeepsCollisionAligned_WithoutGrowingThePool()
        {
            game.StartRun();game.rider.Stop();
            var dressing=game.track.GetComponentsInChildren<StreetDressing>().First(d=>d.layouts.Length==10);
            int max=dressing.collisionLayouts.Max(l=>l.obstacles.Length);
            for(int i=0;i<60;i++)
            {
                dressing.ApplyLayout(7301,i);Physics.SyncTransforms();
                Assert.AreSame(dressing.layouts[dressing.CurrentLayout],dressing.surface.sharedMesh);
                var expected=dressing.collisionLayouts[dressing.CurrentLayout].obstacles;
                // Only the dressing's own sidewalk props: street-life people (v0.2) are chunk children too.
                var active=dressing.GetComponentsInChildren<Obstacle>().Where(o=>o.name.StartsWith("SidewalkObstacle_")).ToArray();Assert.AreEqual(expected.Length,active.Length);
                foreach(var obstacle in active)
                {
                    var box=obstacle.GetComponent<BoxCollider>();Assert.IsNotNull(box);
                    Assert.Contains(box,Physics.OverlapBox(box.bounds.center,Vector3.one*.01f));
                }
                Assert.LessOrEqual(dressing.CollisionPoolSize,max);
            }
            yield return null;
        }

        static void Capture(string name)
        {
            var camera=Camera.main;var rt=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB,4);
            var old=camera.targetTexture;var active=RenderTexture.active;Texture2D image=null;
            try
            {
                camera.targetTexture=rt;var request=new RenderPipeline.StandardRequest{destination=rt};
                if(RenderPipeline.SupportsRenderRequest(camera,request))RenderPipeline.SubmitRenderRequest(camera,request);else camera.Render();
                RenderTexture.active=rt;image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
                string folder=System.IO.Path.Combine(Application.dataPath,"../art-src/comic-city/previews");System.IO.Directory.CreateDirectory(folder);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,name),image.EncodeToPNG());
            }
            finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);if(image!=null)Object.Destroy(image);}
        }
    }
}
