"""Rebuild Pixel Rebellion's original score and adapted CC0 effects.
Usage: python tools/build_game_audio.py /path/to/extracted/audio_sources
No runtime DSP or network requests. See docs/Game_Audio.md for provenance.
"""
import sys, json, hashlib, subprocess
from pathlib import Path
import numpy as np
from scipy import signal
from scipy.io import wavfile

SR=32000
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'src/Assets/Resources/UXAudio'
SRC=Path(sys.argv[1])
RNG=np.random.default_rng(6783)
MANIFEST={}

def pcm(path):
    raw=subprocess.check_output(['ffmpeg','-v','error','-i',str(path),'-f','f32le','-acodec','pcm_f32le','-ar',str(SR),'-ac','1','-'])
    return np.frombuffer(raw,dtype='<f4').astype(np.float64)

def source(pack,name):return pcm(SRC/pack/'Audio'/(name+'.ogg'))
def fade(x,attack=.003,release=.04):
    x=x.copy();a=min(len(x)//2,int(attack*SR));b=min(len(x)//2,int(release*SR))
    if a:x[:a]*=np.linspace(0,1,a)
    if b:x[-b:]*=np.linspace(1,0,b)
    return x

def limit(x,hz,high=False):return signal.sosfilt(signal.butter(2,hz,btype='highpass' if high else 'lowpass',fs=SR,output='sos'),x,axis=0)

def save(name,x,peak=.86,origin='Original procedural composition'):
    x=np.nan_to_num(x);x=x-np.mean(x,axis=0)
    top=np.max(np.abs(x))
    if top<.00001:raise ValueError('Silent asset '+name)
    x=x*(peak/top)
    OUT.mkdir(parents=True,exist_ok=True)
    p=OUT/(name+'.wav');wavfile.write(p,SR,np.round(np.clip(x,-1,1)*32767).astype('<i2'))
    MANIFEST[name]={'seconds':round(len(x)/SR,4),'channels':1 if x.ndim==1 else x.shape[1],
      'peak_dbfs':round(float(20*np.log10(np.max(np.abs(x)))),2),
      'rms_dbfs':round(float(20*np.log10(np.sqrt(np.mean(x*x)))),2),
      'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'origin':origin}

def mix(*xs):
    out=np.zeros(max(len(x) for x in xs))
    for x in xs:out[:len(x)]+=x
    return out

def tone(freq,seconds=.12,amp=1):
    t=np.arange(int(seconds*SR))/SR
    return fade(amp*(np.sin(2*np.pi*freq*t)+.18*np.sin(4*np.pi*freq*t))*np.exp(-t*13),.004,.03)

def notes(midi,spacing=.15,seconds=.35):
    out=np.zeros(int((spacing*(len(midi)-1)+seconds+.12)*SR))
    for i,n in enumerate(midi):
        f=440*2**((n-69)/12);t=np.arange(int(seconds*SR))/SR
        y=(np.sin(2*np.pi*f*t)+.18*np.sin(2*np.pi*f*2*t)+.05*np.sin(2*np.pi*f*3*t))*np.exp(-t*7)
        y=fade(y,.012,.08);start=int(i*spacing*SR);out[start:start+len(y)]+=.4*y
    return out

# Trim individual recorded .22 pistol shots at the actual transient, remove background and long pauses.
shots=pcm(SRC/'gunshots_kurt.wav')
for name,lo,hi in [('shot',.10,.45),('shot_alt',.55,.89)]:
    x=shots[int(lo*SR):int(hi*SR)]
    onset=np.flatnonzero(np.abs(x)>max(np.max(np.abs(x))*.23,.015))[0]
    x=x[max(0,onset-int(.003*SR)):];x=limit(limit(x,9500),90,True)
    save(name,fade(x,.001,.14),.87,'kurt: Gunshots / 22 Pistol.wav (CC0); trimmed, EQ, mono')

kenney=lambda pack,name:'Kenney '+pack+' / '+name+' (CC0); edited'
def adapted(name,pack,asset,peak=.82,seconds=None):
    x=source(pack,asset)
    if seconds is not None:x=x[:int(seconds*SR)]
    save(name,fade(x),peak,kenney(pack,asset))
for name,asset in [('ui','click_002'),('hide','back_001'),('reveal','select_002')]:
    adapted(name,'interface-sounds',asset,.65,seconds=.5)
for name,asset in [('step','footstep_concrete_000'),('step_alt','footstep_concrete_001'),('sprint_step','footstep_concrete_003')]:
    adapted(name,'impact-sounds',asset,.62,seconds=.38)
adapted('door','sci-fi-sounds','doorOpen_000',.82)
body=source('impact-sounds','impactSoft_heavy_001')
wet=source('sci-fi-sounds','slime_000')
save('hit',fade(body[:int(.36*SR)]),.80,kenney('impact-sounds','impactSoft_heavy_001'))
save('guard_death',fade(mix(body*.8,wet*.22)),.83,'Kenney CC0: impactSoft_heavy_001 + slime_000; body impact, no explosion')
save('mine_death',fade(mix(wet*.7,source('impact-sounds','impactSoft_heavy_003')*.55)),.82,'Kenney CC0: slime_000 + impactSoft_heavy_003; fragment impact layer')
boom=mix(source('sci-fi-sounds','explosionCrunch_002')*.85,source('sci-fi-sounds','lowFrequency_explosion_000')*.55)
save('mine_blast',fade(limit(boom,10000),.002,.2),.89,'Kenney CC0: explosionCrunch_002 + lowFrequency_explosion_000; layered blast')
electric=source('sci-fi-sounds','forceField_002')
crackle=source('sci-fi-sounds','laserLarge_004')
save('laser_burn',fade(mix(electric*.70,crackle*.65))[:int(1.4*SR)],.86,'Kenney CC0: forceField_002 + laserLarge_004; electric burn')
adapted('laser_charge','sci-fi-sounds','forceField_001',.72,seconds=.5)
adapted('laser_on','sci-fi-sounds','laserLarge_001',.75,seconds=.55)
# Warning is played only once the mine has actually armed, and stopped on detonation.
warning=np.zeros(int(1.14*SR))
for i,at in enumerate([0,.26,.48,.66,.81,.94,1.04]):
    x=tone(960+i*80,.055);start=int(at*SR);warning[start:start+len(x)]+=.5*x
save('mine_armed',warning,.78)
for name,ns,gap in [('deploy',[50,57,62,65],.12),('data',[74,81],.10),('keycard',[69,74,81],.12),
  ('security',[62,69,74],.13),('checkpoint',[62,65,69,74],.14),('level_clear',[62,65,69,74,77],.14),
  ('victory',[62,69,74,77,81,86],.23),('failed',[50,46,38],.22),('sprint_empty',[62,57,50],.10)]:
    save(name,notes(ns,gap,.40 if name=='level_clear' else .70 if name in ['victory','failed'] else .38),.80)
save('suspicion',mix(tone(493,.20),np.pad(tone(523,.14),(int(.16*SR),0))),.68)
save('alert',notes([74,74,77],.14,.24),.76)
# Small seamless diagnostic/terminal and security loops; music remains the foreground ambience.
loop=np.zeros(SR*2)
for i,at in enumerate([0,.18,.47,.72,1.12,1.42,1.65]):
    y=tone([620,930,770,1160][i%4],.065,.3);j=int(at*SR);loop[j:j+len(y)]+=y
save('interaction_loop',loop,.64)
t=np.arange(SR*3)/SR;freq=540+120*np.sin(2*np.pi*t/3)
y=np.sin(2*np.pi*np.cumsum(freq)/SR)*(.45-.35*np.cos(2*np.pi*t/3))
# Force exact loop boundary without a click; quiet transitions through zero.
y=fade(limit(y,2000),.025,.025);save('alarm_loop',y,.64)

def music(name,bpm,crimson):
    beat=60/bpm;seconds=beat*32;n=round(seconds*SR);out=np.zeros((n,2))
    def add(y,at,pan=0):
        j=round(at*SR);idx=(j+np.arange(len(y)))%n
        gains=np.array([np.sqrt((1-pan)/2),np.sqrt((1+pan)/2)])
        np.add.at(out,idx,y[:,None]*gains)
    # Eight-bar original minor-key score: slow pads, low pulses, sparse glints.
    roots=[38,34,41,36] if not crimson else [33,29,36,31]
    for bar in range(8):
        root=roots[bar//2]
        if bar%2==0:
            d=beat*8+.8;t=np.arange(round(d*SR))/SR
            env=np.minimum(t/.8,1)*np.minimum(np.maximum(d-t,0)/1.6,1)
            for k,offset in enumerate([0,7,15,19]):
                f=440*2**((root+12+offset-69)/12)
                pad=(np.sin(2*np.pi*f*t+.13*k)+.13*np.sin(2*np.pi*f*2.003*t))*env*.026
                add(pad,bar*beat*4,(-.25 if k%2 else .25))
        for b in range(4):
            at=(bar*4+b)*beat
            # Rounded bass pulse; no harsh lead competing with hazards.
            f=440*2**((root-69)/12);t=np.arange(round(beat*.78*SR))/SR
            bass=(np.sin(2*np.pi*f*t)+.22*np.sin(2*np.pi*2*f*t))*np.exp(-t/(beat*.24))
            add(fade(bass,.015,.12)*(.13 if crimson else .10),at)
            if b in ([0,1,2,3] if crimson else [0,2]):
                tt=np.arange(round(.22*SR))/SR
                kick=np.sin(2*np.pi*(43*tt+35*.022*(1-np.exp(-tt/.022))))*np.exp(-tt*22)
                add(kick*.065,at)
            nn=root+24+[0,7,10,14][(bar+b)%4]
            if b%2==0 or crimson:
                ping=notes([nn],0,beat*.8)*.035
                add(ping,at+beat*.5,(-.35 if b%2 else .35))
                add(ping*.32,at+beat*.5+.24,(.35 if b%2 else -.35))
            if crimson or b in [1,3]:
                noise=RNG.normal(0,1,round(.075*SR));noise=limit(noise,6200,True)
                env=np.exp(-np.arange(len(noise))/SR*60)
                add(fade(noise*env,.002,.02)*(.010 if crimson else .006),at+beat*.5,(-.2 if b%2 else .2))
    # Periodic air bed; zero endpoint slope, gentle high-pass then low-pass.
    air=limit(limit(RNG.normal(0,1,(n,2)),2800),100,True)*.005
    air[:1600]*=np.linspace(0,1,1600)[:,None];air[-1600:]*=np.linspace(1,0,1600)[:,None]
    out+=air
    # Repair sub-LSB seam drift introduced by sample rounding.
    drift=out[-1]-out[0];out-=np.linspace(0,1,n)[:,None]*drift
    peak=.55;save(name,out,peak,'Original eight-bar procedural score composed for Pixel Rebellion; no external music')

music('music_facility',78,False);music('music_crimson',92,True)
(ROOT/'docs/game_audio_assets.json').write_text(json.dumps(MANIFEST,indent=2)+'\n')
print('Created',len(MANIFEST),'non-silent WAV assets;',round(sum((OUT/(k+'.wav')).stat().st_size for k in MANIFEST)/1048576,2),'MiB')
