"""Generate Crimson Depths using existing reviewed sprites and serialized actors.

Writes only the new scene, its deterministic metadata and a validation geometry
file. Level 1, the original preview, project settings and art files are retained.
"""
from pathlib import Path
import re,json,uuid,math

ROOT=Path(__file__).resolve().parents[1];ASSETS=ROOT/'src/Assets'
for folder in [ASSETS/'Scripts',ASSETS/'Editor']:
    for source in folder.glob('*.cs'):
        meta=Path(str(source)+'.meta')
        if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL,'pixel-rebellion/'+source.name).hex+'\n')
base=ROOT/'tools/build_expanded_level.py'
prefix=base.read_text().split("art = 'Resources/UXArtDeluxe/'")[0]
env={'__file__':str(base)};exec(compile(prefix,str(base),'exec'),env)
clone,add_script,field,guid,vector=(env[k] for k in ['clone','add_script','field','guid','vector'])
out=env['out'];art='Resources/UXArtDeluxe/'
spec=json.loads((ROOT/'design/level2/Crimson_Depths_Layout_Proposal.json').read_text())
def world(p):return (p[0]-27,16-p[1])
def component(go,script):
    script_guid=guid('Scripts/'+script+'.cs')
    return next(i for i,(kind,body) in out.items() if kind==114 and
        ('m_GameObject: {fileID: '+go+'}') in body and script_guid in body)
def refs(values):return '\n'+''.join('  - {fileID: '+v+'}\n' for v in values).rstrip('\n')
def set_script(go,script,values):
    ident=component(go,script);kind,body=out[ident]
    for key,value in values.items():body=field(body,key,value)
    out[ident]=kind,body
def member(go,sector):add_script(go,'UXSecuritySector','  sector: '+str(sector)+'\n')

camera=clone('Main Camera','Main Camera',*world(spec['spawn']))
for ident,(kind,body) in list(out.items()):
    if kind==20:out[ident]=kind,field(body,'orthographic size',5.4)
    if kind==4 and ('m_GameObject: {fileID: '+camera+'}') in body:
        out[ident]=kind,field(body,'m_LocalPosition',vector(*world(spec['spawn']),-10))
add_script(camera,'UXFacilityCameraFollow')
clone('Player','Player',*world(spec['spawn']),descendants=True,scripts={'PlayerController':{'moveSpeed':2.8}})
clone('GameManager','GameManager',0,0)
director=clone('EvidenceManager','EvidenceManager',0,0)
add_script(director,'UXMissionDefinition','  crimson: 1\n  requiredEvidence: 9\n  halfWidth: 27.5\n  halfHeight: 16.5\n')
add_script(director,'UXStealthDirector')

wall_records=[]
for gy in range(spec['height']):
    for gx in range(spec['width']):
        tint=(.82,.30,.34,1) if gx<18 else ((.72,.26,.36,1) if gx<36 else (.92,.30,.37,1))
        asset='deluxe_floor_alt' if (gx+2*gy)%9==0 else 'deluxe_floor'
        clone('Floor','CrimsonFloor',*world((gx,gy)),sprite=art+asset+'.png',order=-20,color=tint)
console_cover=[(x,24) for x in (14,15,16)]+[(x,5) for x in (32,33,34)]+[(50,y) for y in (24,25,26)]
for gx,gy in sorted({tuple(p) for p in spec['walls']}|set(console_cover)):
    x,y=world((gx,gy));clone('ServerRoomWall','CrimsonWall',x,y,color=(1,.43,.49,1),size=(1,1))
    wall_records.append(dict(name='wall',x=x,y=y,w=1,h=1))
    if (gx+gy)%4==0:clone('Wall Fixture','RedWallLight',x,y+.18,scale=(.65,.65,1),order=7,color=(1,.22,.3,1))
for i,(gx,gy,w,h) in enumerate(spec['props']):
    x,y=world((gx+(w-1)/2,gy+(h-1)/2))
    clone('ServerRack' if i>4 else 'CoverCrate','CrimsonSolidCover',x,y,scale=(w,h,1),color=(.9,.42,.49,1))
    wall_records.append(dict(name='cover',x=x,y=y,w=.98*w,h=.98*h))
    if i==10:clone('Extraction Glow','ReactorGlow',x,y,scale=(4,6,1),color=(1,.17,.25,.65),order=-7)
for index,p in enumerate(spec['hiding']+spec['checkpoints']):
    x,y=world(p);checkpoint=index>=len(spec['hiding'])
    clone('HidingSpot',('Checkpoint_' + str(index-len(spec['hiding']))) if checkpoint else 'HidingSpot',x,y,color=(.25,.75,1,1))
    clone('Blue Perimeter','HidingGlow',x,y,scale=(1.2,1.2,1),order=-4,
        color=(.4,1,.65,1) if checkpoint else (.25,.75,1,1))
for gx in range(3,53,5):
    for gy in range(3,30,7):
        if [gx,gy] not in spec['walls']:clone('Floor Vent','CrimsonVent',*world((gx,gy)),scale=(.55,.55,1),color=(.85,.43,.5,1))

doors=[]
for index,p in enumerate([(18,26),(36,6)]):
    x,y=world(p);go=clone('ServerRoomWall','SectorBulkhead_'+str(index),x,y,scale=(1,3,1),size=(1,1),color=(1,.24,.34,1),order=7)
    collider=next(i for i,(kind,b) in out.items() if kind==61 and ('m_GameObject: {fileID: '+go+'}') in b)
    doors.append(collider);wall_records.append(dict(name='gate'+str(index),x=x,y=y,w=1,h=3))
    clone('Hazard Threshold','OpenPassageThreshold',x,y,scale=(2.7,.85,1),angle=90,color=(1,.45,.4,1))

cache_refs=[]
for i,p in enumerate(spec['data']):
    go=clone('Evidence','Evidence_'+str(i+1),*world(p),sprite=art+'deluxe_evidence.png',color=(1,.8,.6,1),order=12)
    add_script(go,'UXExpandedInteraction');cache_refs.append(component(go,'EvidenceCollectible'))
camera_refs=[];camera_specs=[]
targets=[(7,7),(11,8),(7,15),(11,27),(25,7),(30,8),(25,21),(30,26),(44,7),(48,8),(43,18),(48,24)]
for i,(p,target) in enumerate(zip(spec['cameras'],targets)):
    x,y=world(p);tx,ty=world(target);angle=math.degrees(math.atan2(ty-y,tx-x))
    go=clone('SecurityCamera','SecurityCamera_'+str(i+1),x,y,angle=angle,
        color=(1,.7,.72,1),scripts={'SecurityCamera':{'rotationSpeed':24,'sweepAngle':35,'detectionRange':5.4,'detectionAngle':44}})
    member(go,i//4);camera_refs.append(component(go,'SecurityCamera'))
    camera_specs.append(dict(position=[x,y],angle=angle,sector=i//4))
console_refs=[]
for i,p in enumerate(spec['terminals']):
    go=clone('Terminal','Terminal_'+chr(65+i),*world(p),sprite=art+'deluxe_terminal.png',order=12,
        scripts={'TerminalInteraction':{'requiredEvidence':(i+1)*3,'completesMission':0,
            'camerasToDisable':refs(camera_refs[i*4:i*4+4])}})
    add_script(go,'UXExpandedInteraction');console_refs.append(component(go,'TerminalInteraction'))
x,y=world(spec['exit'])
clone('ExtractionZone','ExtractionZone',x,y,sprite=art+'deluxe_exit.png',order=10,
    scripts={'ExtractionZone':{'requiredEvidence':9}})
clone('Extraction Door','ExtractionArch',x,y,scale=(1.3,1.3,1),order=9)
clone('Extraction Glow','ExtractionGlow',x,y,scale=(2.2,2.2,1))

routes_grid=[[(7,3),(14,3),(14,8),(7,8)],[(3,12),(8,12),(8,18),(3,18)],
    [(8,23),(15,23),(15,29),(8,29)],[(12,12),(16,12),(16,17),(12,17)],
    [(20,4),(26,4),(26,10),(20,10)],[(30,4),(34,4),(34,10),(30,10)],
    [(30,14),(34,14),(34,21),(30,21)],[(20,26),(26,26),(26,30),(20,30)],
    [(42,3),(49,3),(49,8),(42,8)],[(51,12),(52,12),(52,24),(51,24)],
    [(39,26),(45,26),(45,30),(39,30)],[(38,10),(41,10),(41,20),(38,20)]]
routes=[]
for i,points in enumerate(routes_grid):
    route=[world(p) for p in points];routes.append(route)
    go=clone('Guard','Guard_'+str(i+1),*route[0],descendants=True,
        scripts={'GuardDetection':{'detectionRange':3.4,'detectionAngle':55}})
    member(go,i//4)
    ident=component(go,'UXPreviewGuardFacingPatrol');kind,body=out[ident]
    body=re.sub(r'  routePoints:[\s\S]*','',body)
    for key,value in {'speed':1.2,'turnDuration':.75,'minimumPause':1.1,'maximumPause':1.65}.items():body=field(body,key,value)
    body+='  routePoints:\n'+''.join('  - {x: %s, y: %s}\n'%p for p in route)
    body+='  routeEdges:\n'+''.join('  - {x: %s, y: %s}\n'%(j,(j+1)%len(route)) for j in range(len(route)))
    out[ident]=kind,body
fields='  caches:'+refs(cache_refs)+'\n  consoles:'+refs(console_refs)+'\n  bulkheads:'+refs(doors)+'\n'
fields+='  checkpointPositions:\n'+''.join('  - {x: %s, y: %s}\n'%world(p) for p in spec['checkpoints'])
add_script(director,'UXSectorMission',fields)
scene='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'
scene+=''.join('--- !u!%d &%s\n%s'%(kind,ident,body) for ident,(kind,body) in out.items())
scene+='--- !u!1660057539 &9223372036854775807\nSceneRoots:\n  m_ObjectHideFlags: 0\n  m_Roots:\n'
scene+=''.join('  - {fileID: '+i+'}\n' for i in env['roots'])
target=ASSETS/'Scenes/UXCrimsonDepths.unity'
target.write_text('\n'.join(line.rstrip() for line in scene.splitlines())+'\n')
Path(str(target)+'.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL,'pixel-rebellion/crimson-depths-scene').hex+
    '\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
(ROOT/'tools/crimson_level_geometry.json').write_text(json.dumps(dict(walls=wall_records,
    spawn=world(spec['spawn']),data=[world(p) for p in spec['data']],terminals=[world(p) for p in spec['terminals']],
    exit=world(spec['exit']),routes=routes,hiding=[world(p) for p in spec['hiding']],
    checkpoints=[world(p) for p in spec['checkpoints']],cameras=camera_specs),indent=2)+'\n')
print('Created',target.relative_to(ROOT),len(out),'serialized objects/components;',len(routes),'guards; 12 cameras; 9 caches; 3 consoles')
