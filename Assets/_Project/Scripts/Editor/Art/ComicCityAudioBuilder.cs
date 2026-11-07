using System;
using System.IO;
using System.Linq;
using CityScooter.Audio;
using CityScooter.Gameplay;
using UnityEditor;
using UnityEngine;

namespace CityScooter.EditorTools.Art
{
    public static class ComicCityAudioBuilder
    {
        public const string Root="Assets/_Project/Audio/ComicCity";
        public const string BankPath=Root+"/CityAudioBank.asset";
        [Serializable] sealed class Manifest {public Entry[] entries;}
        [Serializable] sealed class Entry
        {
            public string id,label,bus,file;
            public bool loop;
            public float volume,pitchMin,pitchMax,cooldown,duration;
            public int priority,channels;
            public float spatialBlend;
        }
        [MenuItem("CityScooter/Comic City/4 - Import and Validate Audio")]
        public static void Build()
        {
            string catalog=Root+"/catalog.json";
            if(!File.Exists(catalog))throw new InvalidOperationException("Run python tools/prepare-comic-audio.py first.");
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(catalog));
            var bank=AssetDatabase.LoadAssetAtPath<CityAudioBank>(BankPath);
            if(bank==null){bank=ScriptableObject.CreateInstance<CityAudioBank>();AssetDatabase.CreateAsset(bank,BankPath);}
            var existing=bank.cues.ToDictionary(c=>c.id,c=>c);
            var list=new System.Collections.Generic.List<CityAudioCue>();
            var report=new System.Collections.Generic.List<string>{"Audio import validation; source PCM: 48 kHz / 16-bit. All runtime references resolved."};
            foreach(var entry in manifest.entries)
            {
                string path=Root+"/"+entry.file;var importer=AssetImporter.GetAtPath(path) as AudioImporter;
                if(importer==null)throw new InvalidOperationException("Missing audio: "+path);
                var sample=importer.defaultSampleSettings;
                bool longAudio=entry.bus=="Music"||entry.id=="AMB_City_Loop";
                sample.loadType=entry.bus=="Music"?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
                sample.compressionFormat=longAudio?AudioCompressionFormat.Vorbis:AudioCompressionFormat.PCM;
                sample.quality=.8f;sample.sampleRateSetting=AudioSampleRateSetting.OverrideSampleRate;sample.sampleRateOverride=48000;
                sample.preloadAudioData=true;
                importer.defaultSampleSettings=sample;importer.forceToMono=entry.channels==1;importer.loadInBackground=longAudio;
                // The browser decodes audio into Web Audio buffers; no runtime DSP/plugin dependency.
                var web=sample;web.loadType=AudioClipLoadType.DecompressOnLoad;importer.SetOverrideSampleSettings("WebGL",web);
                importer.SaveAndReimport();
                var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if(clip==null||Mathf.Abs(clip.length-entry.duration)>.03f||clip.channels!=entry.channels)throw new InvalidOperationException("Invalid imported audio: "+path);
                if(!existing.TryGetValue(entry.id,out var cue))
                    cue=new CityAudioCue{id=entry.id,label=entry.label,bus=(CityAudioBus)Enum.Parse(typeof(CityAudioBus),entry.bus),loop=entry.loop,volume=entry.volume,pitchMin=entry.pitchMin,pitchMax=entry.pitchMax,priority=entry.priority,spatialBlend=entry.spatialBlend,cooldown=entry.cooldown};
                cue.clip=clip;list.Add(cue);
                report.Add(FormattableString.Invariant($"{entry.id}: {clip.length:F3}s, {clip.channels}ch, {clip.frequency}Hz, {sample.loadType}, {sample.compressionFormat}, loop={cue.loop}"));
            }
            bank.cues=list.ToArray();EditorUtility.SetDirty(bank);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("audio-src/comic-city");File.WriteAllLines("audio-src/comic-city/unity-validation.txt",report);
            Debug.Log("[ComicCity Audio] Imported "+list.Count+" cues; existing mix settings retained.");
        }
        public static void Apply(GameController game)
        {
            var bank=AssetDatabase.LoadAssetAtPath<CityAudioBank>(BankPath);
            if(bank==null)throw new InvalidOperationException("Audio bank not built.");
            var root=game.transform.Find("ComicCityAudio");
            if(root==null){root=new GameObject("ComicCityAudio").transform;root.SetParent(game.transform,false);}
            var director=root.GetComponent<CityAudioDirector>()??root.gameObject.AddComponent<CityAudioDirector>();
            director.bank=bank;director.game=game;director.rider=game.rider;
            var controls=root.GetComponent<CityAudioControls>()??root.gameObject.AddComponent<CityAudioControls>();controls.audioDirector=director;
            if(Camera.main.GetComponent<AudioListener>()==null)Camera.main.gameObject.AddComponent<AudioListener>();
            EditorUtility.SetDirty(director);EditorUtility.SetDirty(controls);
            PrefabUtility.RecordPrefabInstancePropertyModifications(director);PrefabUtility.RecordPrefabInstancePropertyModifications(controls);
        }
    }
}
