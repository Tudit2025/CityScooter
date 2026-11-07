"""Deterministic audio edit/master pipeline. Requires Python + numpy and FFmpeg on PATH.
Sources are listed in audio-src/comic-city/sources.json; no network calls here.
"""
import csv, hashlib, json, math, pathlib, subprocess, wave
import numpy as np

ROOT=pathlib.Path(__file__).resolve().parents[1]
SRC=ROOT/'audio-src/comic-city'; OUT=ROOT/'Assets/_Project/Audio/ComicCity'
RATE=48000; RNG=np.random.default_rng(7301); ROWS=[]; AUDIO={}

def run(args):return subprocess.run(args,check=True,capture_output=True)
def load(name,channels=1,start=0,duration=None,filters='highpass=f=55,lowpass=f=11000'):
    path=next((SRC/'originals').glob(name+'.*'))
    args=['ffmpeg','-v','error','-ss',str(start),'-i',str(path)]
    if duration is not None:args+=['-t',str(duration)]
    args+=['-af',filters,'-ar',str(RATE),'-ac',str(channels),'-f','f32le','pipe:1']
    return np.frombuffer(run(args).stdout,dtype='<f4').reshape(-1,channels).copy()
def mono(x):return x.mean(axis=1) if x.ndim==2 else x
def fade(x,attack=.008,release=.025):
    x=x.copy();a=min(int(attack*RATE),len(x)//2);b=min(int(release*RATE),len(x)//2)
    if a:x[:a]*=np.linspace(0,1,a)[:,None] if x.ndim==2 else np.linspace(0,1,a)
    if b:x[-b:]*=np.linspace(1,0,b)[:,None] if x.ndim==2 else np.linspace(1,0,b)
    return x
def bridge(x,seconds=.002):
    x=x.copy();n=min(int(seconds*RATE),len(x)//8);w=(.5-.5*np.cos(np.linspace(0,np.pi,n))).astype('float32')
    if x.ndim==2:w=w[:,None]
    middle=(x[0]+x[-1])/2
    x[:n]=middle*(1-w)+x[:n]*w;x[-n:]=x[-n:]*(1-w)+middle*w
    return x
def loop(x,overlap=.15):
    n=min(int(overlap*RATE),len(x)//5);w=np.linspace(0,1,n)
    if x.ndim==2:w=w[:,None]
    return bridge(np.concatenate([x[n:-n],x[-n:]*(1-w)+x[:n]*w]))
def noise(seconds,low=80,high=9000):
    n=round(seconds*RATE);f=np.fft.rfftfreq(n,1/RATE)
    shape=(1-np.exp(-(f/max(1,low))**4))*np.exp(-(f/high)**4)/np.sqrt(np.maximum(f,80))
    x=np.fft.irfft(np.fft.rfft(RNG.normal(size=n))*shape,n)
    return (x/(np.std(x)+1e-8)).astype('float32')
def tone(freq,seconds,decay=8,amp=1):
    t=np.arange(round(seconds*RATE))/RATE
    return (amp*np.sin(2*np.pi*freq*t)*np.exp(-decay*t)).astype('float32')
def add(target,clip,at,gain=1):
    start=round(at*RATE);end=min(len(target),start+len(clip))
    if start<0 or end<=start:return
    target[start:end]+=clip[:end-start]*gain
def trim(x):
    energy=np.max(np.abs(x),axis=1) if x.ndim==2 else np.abs(x)
    active=np.flatnonzero(energy>max(.0002,float(energy.max())*.018))
    return x[max(0,active[0]-240):min(len(x),active[-1]+1200)] if len(active) else x
def save(cue,label,bus,x,looped=False,sources=(),volume=1,pitch=(1,1),priority=100,spatial=0,cooldown=.08,rms=-21):
    if x.ndim==1:x=x[:,None]
    x=x.astype('float64');x-=x.mean(axis=0)
    if looped:x=bridge(x)
    else:x=fade(x)
    x*=min(10**(rms/20)/max(1e-9,np.sqrt(np.mean(x*x))),10**(-3/20)/max(1e-9,np.max(np.abs(x))))
    channels=x.shape[1];folder={'Music':'Music','Ambience':'Ambience','Effects':'SFX'}[bus]
    filename=f'{folder}/{cue}.wav';path=OUT/filename;path.parent.mkdir(parents=True,exist_ok=True)
    dither=(RNG.random(x.shape)-RNG.random(x.shape))/65536
    pcm=np.clip(np.rint((x+dither)*32767),-32768,32767).astype('<i2')
    with wave.open(str(path),'wb') as f:f.setnchannels(channels);f.setsampwidth(2);f.setframerate(RATE);f.writeframes(pcm.tobytes())
    audio=pcm.astype('float32')/32768;AUDIO[cue]=audio
    row=dict(id=cue,label=label,bus=bus,file=filename,loop=looped,volume=volume,pitchMin=pitch[0],pitchMax=pitch[1],priority=priority,spatialBlend=spatial,cooldown=cooldown,sources=list(sources),duration=len(x)/RATE,channels=channels,sampleRate=RATE,bits=16,peakDb=20*np.log10(max(1e-9,np.max(np.abs(audio)))),rmsDb=20*np.log10(max(1e-9,np.sqrt(np.mean(audio*audio)))),loopBoundaryDelta=float(np.max(np.abs(audio[-1]-audio[0]))) if looped else 0,sha256=hashlib.sha256(path.read_bytes()).hexdigest())
    row={k:float(v) if isinstance(v,np.floating) else v for k,v in row.items()}
    ROWS.append(row);print(cue,round(row['duration'],3),'s',flush=True)

def build():
    OUT.mkdir(parents=True,exist_ok=True)
    city=load('city',2,5,26,filters='highpass=f=100,lowpass=f=6500')
    save('AMB_City_Loop','城市远处交通底噪','Ambience',loop(city,2),True,['city'],volume=.7,rms=-26)
    rolling=mono(load('rolling'))
    save('SCO_Roll_Loop','轮子接地滚动','Effects',loop(rolling,.08),True,['rolling'],volume=.8,rms=-23)
    wind=noise(8,140,2200);t=np.arange(len(wind))/RATE
    wind*=.8+.12*np.sin(2*np.pi*.375*t)+.08*np.sin(2*np.pi*.625*t)
    save('SCO_Wind_Loop','速度风噪','Effects',loop(wind,.25),True,['original-synthesis'],volume=.46,rms=-25)
    frame=np.zeros(4*RATE,dtype='float32')
    for at in np.arange(.05,3.9,.17)+RNG.uniform(-.035,.035,23):
        hit=tone(720+RNG.uniform(-50,50),.09,55,.2)+tone(1741,.09,70,.1)
        add(frame,hit,float(at),float(RNG.uniform(.4,1)))
    save('SCO_Frame_Loop','车架轻微震动','Effects',loop(frame,.1),True,['original-synthesis'],volume=.3,rms=-27)
    friction=noise(3,550,5700);t=np.arange(len(friction))/RATE
    friction=friction*(.75+.2*np.sin(2*np.pi*13*t))+.2*np.sin(2*np.pi*(1580*t+8*np.sin(2*np.pi*3*t)))
    save('SCO_Brake_Loop','刹车持续摩擦','Effects',loop(friction,.2),True,['original-synthesis'],volume=.55,rms=-24)
    for i,key in enumerate(['horn_a','horn_b'],1):
        save('VEH_Horn_0'+str(i),'远处车辆喇叭 '+str(i),'Ambience',trim(mono(load(key))),sources=[key],volume=.48,pitch=(.94,1.04),priority=170,spatial=1,cooldown=4,rms=-21)
    scrape=trim(mono(load('scrape',filters='highpass=f=200,lowpass=f=6800')))
    for i in range(2):
        x=scrape if i==0 else np.interp(np.arange(0,len(scrape),.84),np.arange(len(scrape)),scrape)
        save('SCO_Scrape_0'+str(i+1),'金属刮擦 '+str(i+1),'Effects',x,sources=['scrape'],volume=.55,pitch=(.94,1.06),priority=70,cooldown=.7,rms=-22)
    for i in range(2):
        seconds=3.5+i*.4;n=noise(seconds,60,4500);t=np.arange(len(n))/RATE
        engine=np.sin(2*np.pi*(130*t-8*t*t))+.25*np.sin(2*np.pi*(255*t-14*t*t))
        x=(.7*n+.3*engine)*np.sin(np.pi*t/seconds)**2
        save('VEH_Pass_0'+str(i+1),'遮挡后的经过车辆 '+str(i+1),'Ambience',x,sources=['original-synthesis'],volume=.45,pitch=(.9,1.07),priority=190,spatial=1,cooldown=3,rms=-24)
    for i in range(2):
        seconds=.45+i*.08;t=np.arange(round(seconds*RATE))/RATE
        x=noise(seconds,70,6200)*np.exp(-t*28)*.26+tone(95+i*13,seconds,18,.6)+tone(830+i*61,seconds,28,.14)
        save('SCO_Land_0'+str(i+1),'落地轮胎与车架 '+str(i+1),'Effects',x,sources=['original-synthesis'],volume=.7,pitch=(.94,1.08),priority=55,cooldown=.08,rms=-21)
        seconds=1.3;t=np.arange(round(seconds*RATE))/RATE
        x=noise(seconds,65,7000)*np.exp(-t*9)*.65+tone(78,seconds,10,.7)
        for freq in [443,719,1103,1943,2791]:x+=tone(freq*(1+i*.11),seconds,7+freq/500,.16)
        add(x,scrape,.08,.2);add(x,tone(135,.25,17,.2),.25)
        save('SCO_Crash_0'+str(i+1),'碰撞与金属散响 '+str(i+1),'Effects',x,sources=['original-synthesis','scrape'],volume=.8,pitch=(.96,1.03),priority=10,cooldown=.4,rms=-20)
    n=noise(.27,250,8000);n*=np.sin(np.linspace(0,np.pi,len(n)))**2
    save('SCO_Jump','起跳短促掠风','Effects',n,sources=['original-synthesis'],volume=.45,priority=65,rms=-23)
    for i in range(2):
        x=np.zeros(round(.48*RATE),dtype='float32')
        for j,f in enumerate([659.25,830.61,987.77]):add(x,tone(f*2**(i/12),.24,17,.55)+tone(f*2*2**(i/12),.24,26,.13),j*.065)
        save('UI_Pickup_0'+str(i+1),'礼物拾取 '+str(i+1),'Effects',x,sources=['original-synthesis'],volume=.53,priority=45,cooldown=.07,rms=-22)
    x=np.zeros(round(.8*RATE),dtype='float32')
    for j,f in enumerate([329.63,493.88,659.25,987.77]):add(x,tone(f,.32,12,.4)+tone(f*2,.32,19,.08),j*.105)
    save('UI_Start','开始与重开提示','Effects',x,sources=['original-synthesis'],volume=.6,priority=40,cooldown=.15,rms=-23)
    save('UI_Click','音量按钮确认','Effects',tone(880,.07,65,.3)+tone(1320,.07,80,.12),sources=['original-synthesis'],volume=.35,priority=100,cooldown=.04,rms=-25)
    # 32 complete bars from the source's middle, at 160 BPM. Keep the beat length exact.
    music=load('music',2,48,48,filters='highpass=f=25,lowpass=f=18000')
    save('MUS_HotRoadway_160_Loop','Hot Roadway · 160 BPM · 32 小节','Music',bridge(music),True,['music'],volume=1,priority=25,rms=-20)
    manifest={'version':1,'sampleRate':RATE,'musicBpm':160,'musicBars':32,'entries':ROWS}
    (OUT/'catalog.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    (SRC/'catalog.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    with (SRC/'measurements.csv').open('w',newline='',encoding='utf-8') as f:
        columns=['id','duration','channels','sampleRate','bits','peakDb','rmsDb','loopBoundaryDelta','sha256'];w=csv.DictWriter(f,columns,extrasaction='ignore');w.writeheader();w.writerows(ROWS)
    demo=np.zeros((RATE*30,2),dtype='float32');demo+=AUDIO['MUS_HotRoadway_160_Loop'][:len(demo)]*.34
    def bed(key,gain,mod=None):
        clip=AUDIO[key];clip=np.tile(clip,(math.ceil(len(demo)/len(clip)),1))[:len(demo)]
        if clip.shape[1]==1:clip=np.repeat(clip,2,axis=1)*.707
        demo[:]+=clip*gain*(1 if mod is None else mod[:,None])
    speed=np.clip(np.arange(len(demo))/RATE/18,.1,1);ground=np.ones(len(demo));ground[12*RATE:round(12.65*RATE)]=0;ground[26*RATE:]=0
    bed('AMB_City_Loop',.5);bed('SCO_Roll_Loop',.55,speed*ground);bed('SCO_Wind_Loop',.28,speed**2)
    for key,at,gain,pan in [('UI_Start',.2,.6,0),('VEH_Horn_01',6,.35,-.6),('UI_Pickup_01',9,.6,0),('SCO_Jump',12,.5,0),('SCO_Land_01',12.65,.6,0),('VEH_Pass_01',17,.4,.5),('SCO_Scrape_01',22,.5,-.1),('SCO_Crash_01',26,.7,0)]:
        clip=AUDIO[key].mean(axis=1);stereo=np.column_stack([clip*np.sqrt((1-pan)/2),clip*np.sqrt((1+pan)/2)]);add(demo,stereo,at,gain)
    brake=np.tile(AUDIO['SCO_Brake_Loop'],(2,1))[:2*RATE];add(demo,np.repeat(brake,2,axis=1),21,.28)
    demo[26*RATE:]*=.5;demo=fade(demo,.08,.7);demo*=min(1,10**(-3/20)/np.max(np.abs(demo)))
    preview=SRC/'previews';preview.mkdir(exist_ok=True)
    subprocess.run(['ffmpeg','-v','error','-y','-f','f32le','-ar',str(RATE),'-ac','2','-i','pipe:0','-c:a','libvorbis','-q:a','5',str(preview/'CityScooter_Audio_Demo.ogg')],input=demo.astype('<f4').tobytes(),check=True)
    for row in ROWS:
        assert row['peakDb']<=-2.99 and row['duration']>0 and np.isfinite(row['rmsDb']),row['id']
        assert not row['loop'] or row['loopBoundaryDelta']<.00015,row['id']
    (SRC/'validation.txt').write_text(f'{len(ROWS)} clips; 48 kHz / PCM16; no clipped samples; loop endpoint delta < 0.00015; music exactly 32 bars at 160 BPM.\nPeak/RMS measurements in measurements.csv. Freesound sources are lossy previews.\n',encoding='utf-8')
if __name__=='__main__':build()
