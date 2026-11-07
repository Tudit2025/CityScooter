using System;
using System.Collections.Generic;
using CityScooter.Devices;
using CityScooter.Game;
using CityScooter.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CityScooter.Audio
{
    /// <summary>Bounded voice pool, category gains and gameplay-driven loops. Uses Web-compatible AudioSource APIs.</summary>
    [DisallowMultipleComponent]
    public sealed class CityAudioDirector : MonoBehaviour
    {
        public CityAudioBank bank;
        public GameController game;
        public RiderController rider;
        public bool loadPlayerPreferences=true;
        public float Master {get;private set;}
        public float Music {get;private set;}
        public float Ambience {get;private set;}
        public float Effects {get;private set;}
        public bool Muted {get;private set;}
        public string LastCueId {get;private set;}
        public int PlayedCueCount {get;private set;}
        public int ActiveOneShots
        {
            get {int count=0;foreach(var v in voices)if(v.cue!=null&&v.until>Time.unscaledTime)count++;return count;}
        }
        const string Pref="CityScooter.Audio.";
        sealed class LoopVoice {public AudioSource source;public CityAudioCue cue;public float level;}
        sealed class Voice {public AudioSource source;public CityAudioCue cue;public float gain,until;public Vector3 velocity;}
        readonly Dictionary<string,CityAudioCue> cues=new Dictionary<string,CityAudioCue>();
        readonly Dictionary<string,LoopVoice> loops=new Dictionary<string,LoopVoice>();
        readonly Dictionary<string,float> cooldowns=new Dictionary<string,float>();
        readonly Dictionary<string,int> variants=new Dictionary<string,int>();
        Voice[] voices=Array.Empty<Voice>();
        System.Random random;
        bool initialized,engaged,everRun,wasAirborne,suspended,focusPaused,appPaused;
        float nextHorn,nextTraffic,nextScrape,airTime,suspendAt,duckUntil;

        void Awake()=>Initialize();
        void OnEnable()
        {
            Initialize();
            GameEvents.RunStarted+=OnRunStarted;GameEvents.Crashed+=OnCrash;GameEvents.GiftCollected+=OnGift;
        }
        void OnDisable()
        {
            GameEvents.RunStarted-=OnRunStarted;GameEvents.Crashed-=OnCrash;GameEvents.GiftCollected-=OnGift;
            foreach(var l in loops.Values){l.source.Stop();l.source.volume=0;l.level=0;}
            foreach(var v in voices){v.source.Stop();v.cue=null;}
            wasAirborne=false;suspended=false;
        }
        void Initialize()
        {
            if(initialized||bank==null)return;
            if(game==null)game=GetComponentInParent<GameController>();
            if(rider==null&&game!=null)rider=game.rider;
            random=new System.Random(bank.randomSeed);
            Master=bank.master;Music=bank.music;Ambience=bank.ambience;Effects=bank.effects;
            if(loadPlayerPreferences)
            {
                Master=ReadGain("Master",Master);Music=ReadGain("Music",Music);Ambience=ReadGain("Ambience",Ambience);Effects=ReadGain("Effects",Effects);
                try { Muted=PlayerPrefs.GetInt(Pref+"Muted",0)!=0; } catch(Exception) { Muted=false; }
            }
            foreach(var cue in bank.cues)
            {
                if(cue==null||string.IsNullOrEmpty(cue.id)||cue.clip==null)continue;
                cues[cue.id]=cue;
                if(cue.loop)
                {
                    var source=Source("Loop_"+cue.id);source.clip=cue.clip;source.loop=true;source.priority=cue.priority;
                    loops[cue.id]=new LoopVoice{source=source,cue=cue};
                }
            }
            voices=new Voice[Mathf.Clamp(bank.oneShotVoices,4,12)];
            for(int i=0;i<voices.Length;i++)voices[i]=new Voice{source=Source("Voice_"+i)};
            initialized=true;
            #if !UNITY_WEBGL || UNITY_EDITOR
            engaged=true;
            #endif
            nextHorn=Time.unscaledTime+Range(bank.hornInterval);nextTraffic=Time.unscaledTime+Range(bank.trafficInterval);
        }
        AudioSource Source(string name)
        {
            var child=new GameObject(name);child.transform.SetParent(transform,false);
            var source=child.AddComponent<AudioSource>();source.playOnAwake=false;source.volume=0;
            source.dopplerLevel=0;source.minDistance=6;source.maxDistance=90;source.rolloffMode=AudioRolloffMode.Linear;
            return source;
        }
        static float ReadGain(string key,float fallback)
        {
            try
            {
                float value=PlayerPrefs.GetFloat(Pref+key,fallback);
                return float.IsNaN(value)||float.IsInfinity(value)?fallback:Mathf.Clamp01(value);
            }
            catch(Exception) { return fallback; } // Browser storage can be unavailable.
        }
        float Range(Vector2 r)=>Mathf.Lerp(r.x,r.y,(float)random.NextDouble());
        float Bus(CityAudioBus bus)=>bus==CityAudioBus.Music?Music:bus==CityAudioBus.Ambience?Ambience:Effects;
        public void Engage(){Initialize();engaged=true;}
        public void SetVolumes(float master,float music,float ambience,float effects,bool persist=true)
        {
            Master=Mathf.Clamp01(master);Music=Mathf.Clamp01(music);Ambience=Mathf.Clamp01(ambience);Effects=Mathf.Clamp01(effects);
            if(persist)StorePreferences();ApplyVolumes();
        }
        public void SetMuted(bool muted,bool persist=true)
        {
            Muted=muted;Engage();if(persist)StorePreferences();ApplyVolumes();
        }
        void StorePreferences()
        {
            if(!loadPlayerPreferences)return;
            try
            {
                PlayerPrefs.SetFloat(Pref+"Master",Master);PlayerPrefs.SetFloat(Pref+"Music",Music);
                PlayerPrefs.SetFloat(Pref+"Ambience",Ambience);PlayerPrefs.SetFloat(Pref+"Effects",Effects);PlayerPrefs.SetInt(Pref+"Muted",Muted?1:0);
            }
            catch(Exception) { /* Keep the session mix when persistent storage is blocked. */ }
        }
        public void SavePreferences()
        {
            if(!loadPlayerPreferences)return;
            StorePreferences();try { PlayerPrefs.Save(); } catch(Exception) { /* Session settings still work. */ }
        }
        static bool Loaded(AudioClip clip)=>clip!=null&&clip.loadState==AudioDataLoadState.Loaded;
        public bool PlayCue(string id,float gain=1,Vector3? position=null,Vector3 velocity=default)
        {
            if(!initialized||!isActiveAndEnabled||suspended||!cues.TryGetValue(id,out var cue)||cue.loop)return false;
            float now=Time.unscaledTime;
            if(cooldowns.TryGetValue(id,out float next)&&now<next)return false;
            Voice slot=null;
            foreach(var v in voices)
            {
                if(v.cue==null||v.until<=now){slot=v;break;}
                if(v.cue.priority>=cue.priority&&(slot==null||v.cue.priority>slot.cue.priority||v.cue.priority==slot.cue.priority&&v.until<slot.until))slot=v;
            }
            if(slot==null)return false;
            slot.source.Stop();slot.cue=cue;slot.gain=Mathf.Clamp01(gain);slot.velocity=velocity;
            slot.source.clip=cue.clip;slot.source.pitch=Range(new Vector2(cue.pitchMin,cue.pitchMax));
            slot.source.priority=cue.priority;slot.source.spatialBlend=position.HasValue?cue.spatialBlend:0;
            slot.source.transform.position=position??(rider!=null?rider.transform.position:transform.position);
            // Web builds decode audio asynchronously: length is 0 (with a warning) until the clip has loaded, which would
            // mark this voice free at once and let the next cue cut it off. Assume one second until then.
            float length=Loaded(cue.clip)?cue.clip.length:1f;
            slot.until=now+length/slot.source.pitch;cooldowns[id]=now+cue.cooldown;
            LastCueId=id;PlayedCueCount++;engaged=true;ApplyVolumes();slot.source.Play();return true;
        }
        void Variant(string prefix,int count,float gain=1,Vector3? position=null,Vector3 velocity=default)
        {
            int pick=random.Next(count);
            if(count>1&&variants.TryGetValue(prefix,out int last)&&pick==last)pick=(pick+1)%count;
            variants[prefix]=pick;PlayCue(prefix+(pick+1).ToString("D2"),gain,position,velocity);
        }
        void OnRunStarted()
        {
            if(!initialized)return;
            Engage();everRun=true;wasAirborne=false;airTime=0;duckUntil=0;cooldowns.Clear();
            foreach(var v in voices){v.source.Stop();v.cue=null;}
            // Stop() rewinds to the start. (Setting time=0 here warned in Web builds while the music was still decoding.)
            if(loops.TryGetValue("MUS_HotRoadway_160_Loop",out var music)){music.source.Stop();music.source.pitch=1;}
            nextHorn=Time.unscaledTime+Range(bank.hornInterval);nextTraffic=Time.unscaledTime+Range(bank.trafficInterval);nextScrape=Time.unscaledTime+1;
            PlayCue("UI_Start");
        }
        void OnCrash()
        {
            if(!initialized)return;
            wasAirborne=false;duckUntil=Time.unscaledTime+.8f;Variant("SCO_Crash_",2);
        }
        void OnGift(){if(initialized)Variant("UI_Pickup_",2);}
        void LateUpdate()
        {
            if(!initialized)return;
            // In the web shell's Simulator mode the keyboard drives the virtual micro:bit (hold M = shout) and the shell
            // turns game keys off (DeviceHub.KeyboardEnabled); M must not also toggle mute then.
            bool gameKeys=DeviceHub.Instance==null||DeviceHub.Instance.KeyboardEnabled;
            if(gameKeys&&Keyboard.current!=null&&Keyboard.current.mKey.wasPressedThisFrame)SetMuted(!Muted);
            SetSuspended(focusPaused||appPaused||Time.timeScale<=0);
            if(suspended)return;
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f),now=Time.unscaledTime;
            bool running=game!=null&&game.State==RunState.Running&&rider!=null&&rider.Running;
            float speed=running?Mathf.Clamp01(rider.Speed/Mathf.Max(1,rider.CurrentMaxSpeed)):0;
            bool airborne=running&&rider.Airborne,grounded=running&&!airborne;
            if(airborne&&!wasAirborne){PlayCue("SCO_Jump");airTime=0;}
            if(airborne)airTime+=dt;
            if(wasAirborne&&grounded)Variant("SCO_Land_",2,Mathf.Clamp(.5f+airTime*.5f,.5f,1));
            wasAirborne=airborne;
            if(grounded&&now>=nextScrape&&Mathf.Abs(rider.transform.position.x)>rider.track.RideableHalfWidthAt(rider.transform.position.z)-rider.radius-.04f)
            {Variant("SCO_Scrape_",2,.65f);nextScrape=now+1.2f;}
            float duck=now<duckUntil?.4f:1;
            Loop("AMB_City_Loop",engaged?(running?.7f:1):0,1,dt);
            Loop("SCO_Roll_Loop",grounded?(.2f+.8f*speed)*(rider.Braking?.7f:1):0,Mathf.Lerp(.75f,1.55f,speed),dt);
            Loop("SCO_Frame_Loop",grounded?speed*speed*.7f:0,Mathf.Lerp(.8f,1.3f,speed),dt);
            Loop("SCO_Wind_Loop",running?speed*speed:0,Mathf.Lerp(.8f,1.25f,speed),dt);
            Loop("SCO_Brake_Loop",grounded&&rider.Braking?Mathf.Lerp(.3f,1,speed):0,Mathf.Lerp(.85f,1.1f,speed),dt);
            float musicRate=Mathf.Lerp(1,Mathf.Clamp(bank.musicMaxPlaybackRate,1,1.5f),rider!=null?rider.SpeedGrowth01:0);
            Loop("MUS_HotRoadway_160_Loop",everRun?(running?Mathf.Lerp(.72f,1,speed):.2f)*duck:0,musicRate,dt);
            if(running&&now>=nextHorn){Variant("VEH_Horn_",2,1,TrafficPosition());nextHorn=now+Range(bank.hornInterval);}
            if(running&&now>=nextTraffic){Variant("VEH_Pass_",2,1,TrafficPosition(),Vector3.back*14);nextTraffic=now+Range(bank.trafficInterval);}
            foreach(var v in voices)
                if(v.cue!=null){if(v.until<=now){v.source.Stop();v.cue=null;}else v.source.transform.position+=v.velocity*dt;}
            ApplyVolumes();
        }
        Vector3 TrafficPosition()
        {
            float z=rider.transform.position.z+Range(new Vector2(18,45));
            return new Vector3((random.Next(2)==0?-1:1)*Range(new Vector2(18,28)),rider.track.SurfaceHeightAt(z)+2,z);
        }
        void Loop(string id,float target,float pitch,float dt)
        {
            if(!loops.TryGetValue(id,out var l))return;
            l.level=Mathf.Lerp(l.level,engaged?target:0,1-Mathf.Exp(-12*dt));
            l.source.pitch=Mathf.Lerp(l.source.pitch,pitch,1-Mathf.Exp(-5*dt));
            if(engaged&&!l.source.isPlaying&&l.level>.002f)l.source.Play();
            if(l.level<.001f&&target==0)l.source.Stop();
        }
        void ApplyVolumes()
        {
            if(!initialized)return;
            float gain=Muted||suspended||!engaged?0:Master,total=0;
            foreach(var l in loops.Values){l.source.volume=l.level*l.cue.volume*Bus(l.cue.bus)*gain;total+=l.source.volume;}
            foreach(var v in voices){v.source.volume=v.cue!=null?v.gain*v.cue.volume*Bus(v.cue.bus)*gain:0;total+=v.source.volume;}
            // Every mastered asset peaks below -3 dBFS. Bound the summed source gains before the hardware mix.
            float ceiling=bank.maximumGainSum/Mathf.Max(bank.maximumGainSum,total);
            if(ceiling<1){foreach(var l in loops.Values)l.source.volume*=ceiling;foreach(var v in voices)v.source.volume*=ceiling;}
        }
        void SetSuspended(bool value)
        {
            if(suspended==value)return;suspended=value;
            if(value)
            {
                suspendAt=Time.unscaledTime;
                foreach(var l in loops.Values)l.source.Pause();foreach(var v in voices)v.source.Pause();
            }
            else
            {
                float elapsed=Time.unscaledTime-suspendAt;nextHorn+=elapsed;nextTraffic+=elapsed;nextScrape+=elapsed;duckUntil+=elapsed;
                foreach(var l in loops.Values)l.source.UnPause();foreach(var v in voices){if(v.cue!=null)v.until+=elapsed;v.source.UnPause();}
            }
            ApplyVolumes();
        }
        void OnApplicationFocus(bool focus){focusPaused=!focus&&!Application.isBatchMode;}
        void OnApplicationPause(bool pause){appPaused=pause;if(pause&&initialized&&loadPlayerPreferences)SavePreferences();}
        void OnApplicationQuit(){if(initialized&&loadPlayerPreferences)SavePreferences();}
    }
}
