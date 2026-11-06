using CityScooter.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace CityScooter.Tests
{
    public class SpeedCurveTests
    {
        [TestCase(0,44,0)]
        [TestCase(300,132,.5f)]
        [TestCase(600,220,1)]
        [TestCase(660,224.4f,1)]
        [TestCase(1200,264,1)]
        public void TenMinuteMilestones_AndSlowerContinuation(float seconds,float expectedLimit,float expectedMusicProgress)
        {
            var tuning=ScriptableObject.CreateInstance<GameTuning>();
            try
            {
                tuning.maxSpeed=44;tuning.maxSpeedMultiplier=5;tuning.speedRampSeconds=600;tuning.postRampMultiplierPerMinute=.1f;
                Assert.AreEqual(expectedLimit,tuning.MaxSpeedAt(seconds),.001f);
                Assert.AreEqual(expectedMusicProgress,tuning.SpeedGrowth01(seconds),.0001f);
            }
            finally{Object.DestroyImmediate(tuning);}
        }
    }
}
