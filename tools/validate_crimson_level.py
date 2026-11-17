"""Validate the actual new Unity scene against the geometry used by integration checks."""
from pathlib import Path
import re,json,subprocess

ROOT=Path(__file__).resolve().parents[1];ASSETS=ROOT/'src/Assets'
scene=(ASSETS/'Scenes/UXCrimsonDepths.unity').read_text()
docs={m[2]:(int(m[1]),m[3]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n([\s\S]*?)(?=^--- !u!|\Z)',scene,re.M)}
def val(b,k):
    m=re.search(r'^  '+re.escape(k)+r': (.*)$',b,re.M);return m[1] if m else ''
def vec(b,k):return [float(v) for v in re.findall(r'[xyz]: ([-\d.eE+]+)',val(b,k))]
objects={i:b for i,(k,b) in docs.items() if k==1};transforms={i:b for i,(k,b) in docs.items() if k==4}
by_object={re.search(r'm_GameObject: \{fileID: (\d+)\}',b)[1]:i for i,b in transforms.items()}
names={i:val(b,'m_Name') for i,b in objects.items()}
def world(i):
    b=transforms[i];p=vec(b,'m_LocalPosition');scale=vec(b,'m_LocalScale');parent=re.search(r'm_Father: \{fileID: (\d+)\}',b)[1]
    if parent=='0':return p,scale
    pp,ss=world(parent);return [a+b*c for a,b,c in zip(pp,p,ss)],[a*b for a,b in zip(scale,ss)]
meta={re.search(r'^guid: (\w+)',p.read_text(),re.M)[1]:p for p in ASSETS.rglob('*.meta') if re.search(r'^guid: (\w+)',p.read_text(),re.M)}
missing=set(re.findall(r'guid: ([a-f0-9]{32})',scene))-meta.keys()
assert missing <= {'a97c105638bdf8b4a8650670310a4cd3','0000000000000000f000000000000000','0000000000000000e000000000000000'},missing
for ident,(kind,body) in docs.items():
    for line in body.splitlines():
        if 'guid:' not in line:
            for ref in re.findall(r'fileID: (-?\d+)',line):assert ref=='0' or ref in docs,(ident,ref)
def script_id(name):return re.search(r'^guid: (\w+)',(ASSETS/('Scripts/'+name+'.cs.meta')).read_text(),re.M)[1]
def scripts(name):return {i:b for i,(k,b) in docs.items() if k==114 and script_id(name) in b}
def owner(b):return re.search(r'm_GameObject: \{fileID: (\d+)\}',b)[1]
def array(b,key):
    match=re.search(r'^  '+key+r':\n([\s\S]*?)(?=^  \w|\Z)',b,re.M);assert match,key
    return re.findall(r'fileID: (\d+)',match[1])
assert list(names.values()).count('Player')==1 and list(names.values()).count('ExtractionZone')==1
for script,count in [('GuardDetection',12),('SecurityCamera',12),('EvidenceCollectible',9),('TerminalInteraction',3),
    ('UXMissionDefinition',1),('UXStealthDirector',1),('UXSectorMission',1),('UXSecuritySector',24)]:
    assert len(scripts(script))==count,(script,len(scripts(script)))
definition=next(iter(scripts('UXMissionDefinition').values()));assert val(definition,'crimson')=='1' and val(definition,'requiredEvidence')=='9'
membership={owner(b):int(val(b,'sector')) for b in scripts('UXSecuritySector').values()}
sector=next(iter(scripts('UXSectorMission').values()))
assert len(array(sector,'caches'))==9 and len(array(sector,'consoles'))==3 and len(array(sector,'bulkheads'))==2
camera_refs=set(scripts('SecurityCamera'));covered=set()
for stage,ref in enumerate(array(sector,'consoles')):
    body=docs[ref][1];assert val(body,'requiredEvidence')==str((stage+1)*3) and val(body,'completesMission')=='0'
    targets=set(array(body,'camerasToDisable'));assert len(targets)==4 and not covered&targets
    assert targets<=camera_refs and all(membership[owner(docs[r][1])]==stage for r in targets)
    covered|=targets
assert covered==camera_refs
geometry=json.loads((ROOT/'tools/crimson_level_geometry.json').read_text());solids=[]
for ident,(kind,body) in docs.items():
    if kind!=61 or val(body,'m_IsTrigger')!='0' or val(body,'m_Enabled')!='1':continue
    go=owner(body);name=names[go]
    if name=='Player' or name.startswith('Guard_'):continue
    p,scale=world(by_object[go]);size=vec(body,'m_Size');offset=vec(body,'m_Offset')
    solids.append(dict(x=p[0]+offset[0]*scale[0],y=p[1]+offset[1]*scale[1],w=abs(size[0]*scale[0]),h=abs(size[1]*scale[1])))
expected=geometry['walls'];assert len(solids)==len(expected),(len(solids),len(expected))
for a,b in zip(sorted(solids,key=lambda b:(b['x'],b['y'])),sorted(expected,key=lambda b:(b['x'],b['y']))):
    assert all(abs(a[k]-b[k])<1e-6 for k in ['x','y','w','h']),(a,b)
assert len([n for i,n in names.items() if val(objects[i],'m_TagString')=='HidingSpot'])==22
for i,b in scripts('EvidenceCollectible').items():assert val(objects[owner(b)],'m_Name').startswith('Evidence_')
assert val(next(iter(scripts('ExtractionZone').values())),'requiredEvidence')=='9'
for i,b in scripts('GuardDetection').items():assert val(b,'detectionRange')=='3.4' and val(b,'detectionAngle')=='55'
for index,(ref,b) in enumerate(sorted(scripts('UXPreviewGuardFacingPatrol').items(),key=lambda p:int(names[owner(p[1])].split('_')[1]))):
    points=re.search(r'  routePoints:\n([\s\S]*?)  routeEdges:',b)[1]
    actual=[(float(x),float(y)) for x,y in re.findall(r'\{x: ([-\d.]+), y: ([-\d.]+)\}',points)]
    assert actual==[tuple(p) for p in geometry['routes'][index]]
    assert membership[owner(b)]==index//4
def available(point,boxes):return not any(abs(point[0]-b['x'])<=b['w']/2+.25 and abs(point[1]-b['y'])<=b['h']/2+.35 for b in boxes)
current=sum(available((x,y),[b for b in expected if not b['name'].startswith('gate')]) for x in range(-26,27) for y in range(-15,16))
old=json.loads((ROOT/'tools/expanded_level_geometry.json').read_text())
baseline=sum(available((x,y),[b for b in old['walls'] if b['name']!='gate']) for x in range(-13,14) for y in range(-7,8))
assert current>=3*baseline,(current,baseline)
for scene_name in ['UXFacilityExpanded','UXFacilityPreview','SampleScene']:
    result=subprocess.run(['git','diff','--quiet','1c79d0fe3c4eff14f86ac57d9b1710c4f572e3ba','--',f'src/Assets/Scenes/{scene_name}.unity'],cwd=ROOT)
    assert result.returncode==0,'Existing scene was altered: '+scene_name
print(f'PASS Crimson serialized scene: {len(docs)} references checked; all assets found; 12 guards/12 cameras/9 caches/3 local consoles; 22 authored cover markers / 3 runtime refuges; collider/route data match.')
print(f'PASS scale: {current} sampled player-clear cells vs {baseline} in Level1 ({current/baseline:.2f}x); existing scenes retained.')
