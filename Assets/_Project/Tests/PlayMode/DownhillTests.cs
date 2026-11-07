using System.Collections;
using CityScooter.Devices;
using CityScooter.Gameplay;
using CityScooter.Rendering;
using CityScooter.Track;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace CityScooter.Tests
{
    public class DownhillTests
    {
        GameController game;
        GameTuning tuning;
        TrackTheme theme;
        GameObject first,second;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            tuning=ScriptableObject.CreateInstance<GameTuning>();
            tuning.obstacleChanceStart=tuning.obstacleChanceMax=tuning.giftChance=tuning.jumpObstacleChance=0;
            tuning.startSpeed=12;
            theme=ScriptableObject.CreateInstance<TrackTheme>();
            first=new GameObject("Test slope A");second=new GameObject("Test slope B");
            var a=first.AddComponent<RoadChunk>();a.length=40;a.endHeight=-10;
            var b=second.AddComponent<RoadChunk>();b.length=40;b.endHeight=-4;
            theme.orderedChunks=true;theme.chunks=new[]{a,b};
            DeviceHub.Instance?.Mixer.Reset();
            game=GameSystemsFactory.Create(tuning,theme).GetComponent<GameController>();game.track.seed=42;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Object.Destroy(game.gameObject);Object.Destroy(first);Object.Destroy(second);Object.Destroy(theme);Object.Destroy(tuning);
            DeviceHub.Instance?.Mixer.Reset();yield return null;
        }

        [Test]
        public void AlternatingGrades_JoinWithoutGaps_AndResetToSameHeight()
        {
            Assert.AreEqual(-10,game.track.SurfaceHeightAt(40),.001f);
            Assert.AreEqual(-14,game.track.SurfaceHeightAt(80),.001f);
            Assert.AreEqual(-24,game.track.SurfaceHeightAt(120),.001f);
            Assert.Less(Mathf.Abs(game.track.SurfaceHeightAt(40.001f)-game.track.SurfaceHeightAt(39.999f)),.001f);
            game.track.ResetTrack(0);
            Assert.AreEqual(-24,game.track.SurfaceHeightAt(120),.001f,"pooled chunks must not accumulate vertical offsets");
        }

        [UnityTest]
        public IEnumerator Rider_StaysOnSurface_AcrossChunkJoin_AndRestarts()
        {
            game.StartRun();
            game.rider.transform.position=new Vector3(0,game.track.SurfaceHeightAt(39),39);
            yield return new WaitForSeconds(.35f);
            Assert.Greater(game.rider.transform.position.z,40);
            Assert.AreEqual(game.track.SurfaceHeightAt(game.rider.transform.position.z),game.rider.transform.position.y,.001f);
            game.StartRun();
            Assert.AreEqual(-.5f,game.rider.transform.position.y,.001f);
            Assert.AreEqual(0,game.rider.Distance,.001f);
        }

        [Test]
        public void LeadIn_CoversCameraBehindStart_WithoutMovingOrigin()
        {
            game.track.buildLeadIn=true;game.track.ResetTrack(0);
            Assert.AreEqual(0,game.track.SurfaceHeightAt(0),.001f);
            Assert.AreEqual(.8f,game.track.SurfaceHeightAt(-8),.001f);
            Assert.AreEqual(-10,game.track.SurfaceHeightAt(40),.001f);
            RoadChunk behind=System.Array.Find(game.track.GetComponentsInChildren<RoadChunk>(),c=>c.transform.position.z<0);
            Assert.IsNotNull(behind);
        }

        [Test]
        public void ChaseCamera_FollowsGroundElevation_AndIgnoresJumpHeight()
        {
            var go=new GameObject("Test chase camera");go.AddComponent<Camera>();var follow=go.AddComponent<FollowCamera>();follow.target=game.rider.transform;
            try
            {
                game.rider.transform.position=new Vector3(0,-11,50);follow.SnapToTarget();
                Assert.AreEqual(-11+follow.offset.y,go.transform.position.y,.001f);
                Quaternion rotation=go.transform.rotation;
                game.rider.transform.position+=Vector3.up*2;follow.SnapToTarget();
                Assert.AreEqual(-11+follow.offset.y,go.transform.position.y,.001f);
                Assert.Less(Quaternion.Angle(rotation,go.transform.rotation),.001f);
                Assert.Greater(Vector3.Dot(go.transform.forward,Vector3.down),0);
            }
            finally{Object.DestroyImmediate(go);}
        }

        [UnityTest]
        public IEnumerator SlopeGift_CollectsAtActualSurfaceHeight()
        {
            game.StartRun();float z=game.rider.transform.position.z+6;
            game.track.PlaceAt("placeholder-gift",new Vector3(0,game.track.SurfaceHeightAt(z),z));
            float time=0;while(game.Gifts==0&&time<2){time+=Time.deltaTime;yield return null;}
            Assert.AreEqual(1,game.Gifts);Assert.AreEqual(RunState.Running,game.State);
        }

        [UnityTest]
        public IEnumerator SlopeObstacle_CrashesAtActualSurfaceHeight()
        {
            game.StartRun();float z=game.rider.transform.position.z+7;
            game.track.PlaceAt("placeholder-obstacle-1",new Vector3(0,game.track.SurfaceHeightAt(z),z));
            float time=0;while(game.State==RunState.Running&&time<2){time+=Time.deltaTime;yield return null;}
            Assert.AreEqual(RunState.Crashed,game.State);
        }

        [UnityTest]
        public IEnumerator DownhillJump_ClearsRailing_AndLandsOnSlope()
        {
            game.StartRun();float z=game.rider.transform.position.z+14;
            game.track.PlaceAt("placeholder-railing",new Vector3(0,game.track.SurfaceHeightAt(z),z));
            float time=0;while(game.rider.transform.position.z<z-4&&time<2){time+=Time.deltaTime;yield return null;}
            DeviceHub.Instance.Receive("downhill-test","jp");
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(RunState.Running,game.State);Assert.Greater(game.rider.transform.position.z,z+3);
            Assert.IsFalse(game.rider.Airborne);
            Assert.AreEqual(game.track.SurfaceHeightAt(game.rider.transform.position.z),game.rider.transform.position.y,.001f);
        }

        [UnityTest]
        public IEnumerator Scarf_TeleportReset_KeepsMeshFiniteAndLocal()
        {
            var go=new GameObject("Test scarf");var scarf=go.AddComponent<ScarfRibbon>();
            yield return null;go.transform.position=new Vector3(0,-90,400);
            yield return null;
            Mesh mesh=go.GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual((scarf.segments+1)*4,mesh.vertexCount);
            foreach(Vector3 v in mesh.vertices){Assert.IsFalse(float.IsNaN(v.x+v.y+v.z));Assert.Less(v.magnitude,scarf.length+1);}
            Object.Destroy(go);yield return null;
        }
    }
}
