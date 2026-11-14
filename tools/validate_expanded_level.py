"""Validate the saved scene, physical routes and its separation from the baseline."""
from pathlib import Path
from collections import deque
import re
import json
import math
import itertools
ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT/'src/Assets'
s = (ASSETS/'Scenes/UXFacilityExpanded.unity').read_text()
docs = {m[2]: (int(m[1]), m[3]) for m in re.finditer(
    r'^--- !u!(\d+) &(-?\d+)[^\n]*\n([\s\S]*?)(?=^--- !u!|\Z)',s,re.M)}
def val(body,key):
    m=re.search(r'^  '+re.escape(key)+r': (.*)$',body,re.M)
    return m[1] if m else ''
def vec(body,key):
    return [float(x) for x in re.findall(r'[xyz]: ([-\d.eE+]+)',val(body,key))]
objects={i:b for i,(k,b) in docs.items() if k==1}
transforms={i:b for i,(k,b) in docs.items() if k==4}
by_object={re.search(r'm_GameObject: \{fileID: (\d+)\}',b)[1]:i for i,b in transforms.items()}
def world(ident):
    t=transforms[ident];p=vec(t,'m_LocalPosition');scale=vec(t,'m_LocalScale')
    parent=re.search(r'm_Father: \{fileID: (\d+)\}',t)[1]
    if parent=='0':return p,scale
    pp,ss=world(parent)
    return [a+b*c for a,b,c in zip(pp,p,ss)],[a*b for a,b in zip(scale,ss)]
guids={re.search(r'^guid: (\w+)',p.read_text(),re.M)[1] for p in ASSETS.rglob('*.meta')
       if re.search(r'^guid: (\w+)',p.read_text(),re.M)}
missing=set(re.findall(r'guid: ([a-f0-9]{32})',s))-guids
# URP package's built-in Sprite-Lit material.
assert missing <= {'a97c105638bdf8b4a8650670310a4cd3',
    '0000000000000000f000000000000000', '0000000000000000e000000000000000'},missing
for ident,(kind,body) in docs.items():
    for line in body.splitlines():
        if 'guid:' in line:continue
        for ref in re.findall(r'fileID: (-?\d+)',line):
            assert ref=='0' or ref in docs,(ident,ref)
names=[val(b,'m_Name') for b in objects.values()]
assert names.count('Player')==1 and names.count('Terminal')==1 and names.count('ExtractionZone')==1
assert len([n for n in names if n.startswith('Guard_')])==3
assert len([n for n in names if n.startswith('SecurityCamera_')])==3
assert len([n for n in names if n.startswith('Evidence_')])==3
assert s.count('  requiredEvidence: 3')==2
walls=[]
for ident,(kind,body) in docs.items():
    if kind!=61 or val(body,'m_IsTrigger')!='0' or val(body,'m_Enabled')!='1':continue
    go=re.search(r'm_GameObject: \{fileID: (\d+)\}',body)[1]
    name=val(objects[go],'m_Name')
    if name=='Player' or name.startswith('Guard_'):continue
    pos,scale=world(by_object[go]);size=vec(body,'m_Size');offset=vec(body,'m_Offset')
    walls.append(dict(name='gate' if name=='AccessGate' else 'solid',
        x=pos[0]+offset[0]*scale[0], y=pos[1]+offset[1]*scale[1],
        w=abs(size[0]*scale[0]), h=abs(size[1]*scale[1])))
data=json.loads((ROOT/'tools/expanded_level_geometry.json').read_text())
assert len(walls)==len(data['walls'])
for a,b in zip(sorted(walls,key=lambda x:(x['x'],x['y'])), sorted(data['walls'],key=lambda x:(x['x'],x['y']))):
    for key in ['x','y','w','h']:assert abs(a[key]-b[key])<1e-6,(a,b)
# The director turns the saved hiding triggers into solid crates at runtime.
# Validate the playable geometry with those bodies present as well.
runtime_walls=list(walls)
for go,body in objects.items():
    if val(body,'m_TagString')!='HidingSpot':continue
    pos,_=world(by_object[go])
    runtime_walls.append(dict(name='hiding_cover',x=pos[0],y=pos[1],w=.98,h=.98))
assert len(runtime_walls)-len(walls)==9
points=[(x/2,y/2) for y in range(-15,16) for x in range(-27,28)]
def free(p, opened, half=(.25,.35)):
    return not any(abs(p[0]-b['x']) <= b['w']/2+half[0] and
                   abs(p[1]-b['y']) <= b['h']/2+half[1]
                   for b in runtime_walls if not(opened and b['name']=='gate'))
def path(a,b,opened,half=(.25,.35)):
    allowed={p for p in points if free(p,opened,half)}
    start=min(allowed,key=lambda p:math.dist(p,a));goal=min(allowed,key=lambda p:math.dist(p,b))
    queue=deque([start]);previous={start:None}
    while queue and goal not in previous:
        p=queue.popleft()
        for dx,dy in [(0,.5),(0,-.5),(.5,0),(-.5,0)]:
            next=p[0]+dx,p[1]+dy
            if next in allowed and next not in previous:previous[next]=p;queue.append(next)
    if goal not in previous:return []
    result=[]
    while goal is not None:result.append(goal);goal=previous[goal]
    return result[::-1]
assert path(data['spawn'],data['key'],False),'Key inaccessible before opening gate'
for target in [*data['data'],data['terminal'],data['exit']]:
    assert not path(data['spawn'],target,False),'Locked gate has a bypass'
    assert path(data['spawn'],target,True),'Objective unreachable after key'
    assert path(data['spawn'],target,True,(.28,.38)),'Guard navigation cannot reach objective'
for p in data['passages']:
    assert free(p,True),'Marked passage has a physical obstruction'
    assert path(data['spawn'],p,True),'Marked passage is disconnected'
for number,route in enumerate(data['routes']):
    for a,b in zip(route,route[1:]+route[:1]):
        assert a[0]==b[0] or a[1]==b[1]
        assert path(a,b,False,(.28,.38)),('Guard cannot route around solid cover',number,a,b)
# Extraction annex is bounded on four sides and has exactly two two-tile entrances.
annex_west={p for p in points if p[0]==8 and -7<=p[1]<=-2 and free(p,True)}
annex_north={p for p in points if p[1]==-2 and 8<=p[0]<=13 and free(p,True)}
assert annex_west and annex_north
assert min(y for _,y in annex_west)>=-5 and max(y for _,y in annex_west)<=-4
assert min(x for x,_ in annex_north)>=11 and max(x for x,_ in annex_north)<=12
best=math.inf
for sequence in itertools.permutations(data['data']):
    goals=[data['spawn'],data['key'],*sequence,data['terminal'],data['exit']]
    best=min(best,sum((len(path(a,b,True))-1)*.5 for a,b in zip(goals,goals[1:])))
print('PASS: scene references; 3 guards/cameras/caches; mandatory key gate; all objectives/passages reachable;')
print('      9 runtime solid hiding crates; patrol/pursuit routes around cover; exactly two extraction entrances.')
print('Shortest geometric mission route:',best,'world units (excludes security timing and downloads).')
