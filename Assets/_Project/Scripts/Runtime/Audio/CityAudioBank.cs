using System;
using UnityEngine;

namespace CityScooter.Audio
{
    public enum CityAudioBus { Music, Ambience, Effects }

    [Serializable]
    public sealed class CityAudioCue
    {
        public string id,label;
        public AudioClip clip;
        public CityAudioBus bus;
        public bool loop;
        [Range(0,1)] public float volume=1;
        [Range(.25f,2)] public float pitchMin=1,pitchMax=1;
        [Range(0,256)] public int priority=100;
        [Range(0,1)] public float spatialBlend;
        [Min(0)] public float cooldown=.08f;
    }

    [CreateAssetMenu(menuName="CityScooter/City Audio Bank")]
    public sealed class CityAudioBank : ScriptableObject
    {
        public int schemaVersion=1;
        [Header("Default category gains; player preferences override these")]
        [Range(0,1)] public float master=.75f,music=.45f,ambience=.5f,effects=.85f;
        [Header("Mix and traffic")]
        [Range(4,12)] public int oneShotVoices=8;
        [Range(.4f,1.2f)] public float maximumGainSum=1.1f;
        public Vector2 hornInterval=new Vector2(12,24);
        public Vector2 trafficInterval=new Vector2(8,17);
        public int randomSeed=7301;
        [Range(1,1.5f)] public float musicMaxPlaybackRate=1.5f;
        public CityAudioCue[] cues=Array.Empty<CityAudioCue>();
    }
}
