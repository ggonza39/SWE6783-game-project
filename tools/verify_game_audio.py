"""Validate shipped samples and representative worst-case mixing, without Unity or telemetry."""
from pathlib import Path
import hashlib,json,re
import numpy as np
from scipy.io import wavfile
from scipy.signal import resample_poly
r=Path(__file__).resolve().parents[1];bank=r/'src/Assets/Resources/UXAudio'
m=json.loads((r/'docs/game_audio_assets.json').read_text());samples={}
for name,meta in m.items():
 p=bank/(name+'.wav');assert hashlib.sha256(p.read_bytes()).hexdigest()==meta['sha256'],name+' hash'
 sr,data=wavfile.read(p);assert sr==32000 and data.dtype==np.int16,name+' format'
 x=data.astype(np.float64)/32768;samples[name]=x
 assert np.isfinite(x).all() and .0005<np.sqrt(np.mean(x*x)),name+' silent/non-finite'
 assert np.max(np.abs(x))<.90,name+' clipping/headroom'
 assert np.max(np.abs(resample_poly(x,4,1,axis=0)))<.99,name+' inter-sample peak'
 assert abs(len(x)/sr-meta['seconds'])<.00011,name+' duration'
 assert (1 if x.ndim==1 else x.shape[1])==meta['channels'],name+' channel count'
 if name.endswith('_loop') or name.startswith('music_'):
  assert np.max(np.abs(x[0]-x[-1]))<.0001,name+' loop seam'
 assert p.with_suffix('.wav.meta').exists(),name+' meta'
script=(r/'src/Assets/Scripts/UXGameAudio.cs').read_text()
expected=re.search(r'ClipNames = \{(.*?)\};',script,re.S).group(1)
expected=re.findall('"([a-z_]+)"',expected)+['shot_alt','step_alt','music_facility','music_crimson','interaction_loop','alarm_loop']
assert set(expected)==set(m),'Runtime/asset inventory differs'
# Simultaneous loud, centered effects with both controls at 100%; match the bounded mixer.
n=32000*3;out=np.zeros((n,2))
for theme in ['music_facility','music_crimson']:
 for names in [['shot','shot_alt','mine_blast','laser_burn','hit','guard_death','mine_death','failed','checkpoint','alert'],['mine_armed','mine_blast','laser_burn']]:
  out.fill(0);music=samples[theme];out[:min(n,len(music))]+=music[:n]*.30
  gains=[1 if k in ['mine_blast','laser_burn'] else .78 if k.startswith('shot') else .65 for k in names]
  total=sum(gains)+.10+.14;unit=.62/max(1,total)
  for name,gain in list(zip(names,gains))+[('alarm_loop',.10),('interaction_loop',.14)]:
   x=samples[name];k=min(len(x),n);out[:k]+=x[:k,None]*gain*unit
  assert np.max(np.abs(resample_poly(out,4,1,axis=0)))<.95,'Representative mix clips'
print('PASS: 32 matched, non-silent WAVs; 32kHz PCM; recorded gunshots; sample/true-peak headroom; four seamless loops; two maximum-volume stress mixes per theme.')
