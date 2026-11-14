"""Build the additive demo scene from the reviewed preview's serialized objects/art.

Never writes UXFacilityPreview or SampleScene. Run from any working directory.
"""
from pathlib import Path
import re
import math
import json
import uuid

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'src/Assets'
source = (ASSETS / 'Scenes/UXFacilityPreview.unity').read_text()
docs = {m[2]: (int(m[1]), m[3]) for m in re.finditer(
    r'^--- !u!(\d+) &(-?\d+)[^\n]*\n([\s\S]*?)(?=^--- !u!|\Z)', source, re.M)}
objects = {i: b for i, (k, b) in docs.items() if k == 1}
names = {}
for ident, body in objects.items():
    names.setdefault(re.search(r'^  m_Name: (.*)$', body, re.M)[1], []).append(ident)
transforms = {i: b for i, (k, b) in docs.items() if k == 4}
by_object = {re.search(r'm_GameObject: \{fileID: (\d+)\}', b)[1]: i for i, b in transforms.items()}
children = {}
for ident, body in transforms.items():
    parent = re.search(r'm_Father: \{fileID: (\d+)\}', body)[1]
    children.setdefault(parent, []).append(re.search(r'm_GameObject: \{fileID: (\d+)\}', body)[1])

def field(body, key, value):
    body, count = re.subn(r'^  ' + re.escape(key) + r':[^\n]*$', '  ' + key + ': ' + str(value), body, flags=re.M)
    if not count: body += '  ' + key + ': ' + str(value) + '\n'
    return body

def vector(x, y, z=0): return '{x: %s, y: %s, z: %s}' % (x, y, z)
def guid(path): return re.search(r'^guid: (\w+)', (ASSETS / (path + '.meta')).read_text(), re.M)[1]
out = {i: (k, b) for i, (k, b) in docs.items() if k in [29, 104, 157, 196]}
next_id = 3000000000
roots = []
wall_records = []

def clone(template, name, x, y, *, scale=None, angle=0, descendants=False, sprite=None,
          color=None, order=None, trigger=None, size=None, scripts=None):
    global next_id
    original = names[template][0] if not template.isdigit() else template
    selected = []
    def visit(go):
        selected.append(go)
        selected.extend(re.findall(r'component: \{fileID: (\d+)\}', objects[go]))
        if descendants:
            for child in children.get(by_object[go], []): visit(child)
    visit(original)
    mapping = {}
    for ident in selected:
        mapping[ident] = str(next_id); next_id += 1
    result = []
    for old in selected:
        kind, body = docs[old]
        body = re.sub(r'fileID: (-?\d+)', lambda m: 'fileID: ' + mapping.get(m[1], m[1]), body)
        if kind == 1 and old == original:
            body = field(body, 'm_Name', name)
            body = field(body, 'm_IsActive', 1)
        if kind == 4 and old == by_object[original]:
            body = field(body, 'm_LocalPosition', vector(x, y))
            if scale: body = field(body, 'm_LocalScale', vector(*scale))
            body = field(body, 'm_LocalRotation', '{x: 0, y: 0, z: %s, w: %s}' % (
                math.sin(math.radians(angle) / 2), math.cos(math.radians(angle) / 2)))
            body = field(body, 'm_LocalEulerAnglesHint', vector(0, 0, angle))
            body = field(body, 'm_Father', '{fileID: 0}')
            if not descendants: body = re.sub(r'  m_Children:[\s\S]*?(?=  m_Father:)', '  m_Children: []\n', body)
        if kind == 212 and old in re.findall(r'component: \{fileID: (\d+)\}', objects[original]):
            if sprite: body = field(body, 'm_Sprite', '{fileID: 21300000, guid: %s, type: 3}' % guid(sprite))
            if color: body = field(body, 'm_Color', '{r: %s, g: %s, b: %s, a: %s}' % color)
            if order is not None: body = field(body, 'm_SortingOrder', order)
        if kind == 61:
            if trigger is not None: body = field(body, 'm_IsTrigger', int(trigger))
            if size: body = field(body, 'm_Size', '{x: %s, y: %s}' % size)
        if kind == 114 and scripts:
            for script, values in scripts.items():
                if guid('Scripts/' + script + '.cs') in body:
                    for key, value in values.items(): body = field(body, key, value)
        out[mapping[old]] = (kind, body)
        result.append(mapping[old])
    roots.append(mapping[by_object[original]])
    return mapping[original]

def add_script(go, script, fields=''):
    global next_id
    ident = str(next_id); next_id += 1
    kind, body = out[go]
    body = body.replace('  m_Layer:', '  m_Layer:', 1)
    body = re.sub(r'(?=  m_Layer:)', '  - component: {fileID: %s}\n' % ident, body, count=1)
    out[go] = kind, body
    out[ident] = (114, '''MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: %s}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: %s, type: 3}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::%s
%s''' % (go, guid('Scripts/' + script + '.cs'), script, fields))

art = 'Resources/UXArtDeluxe/'
camera = clone('Main Camera', 'Main Camera', -7, -2)
for ident, (kind, body) in list(out.items()):
    if kind == 20:
        body = field(body, 'orthographic size', 5.4)
        out[ident] = kind, body
    if kind == 4 and re.search(r'm_GameObject: \{fileID: ' + camera + r'\}', body):
        out[ident] = kind, field(body, 'm_LocalPosition', vector(-7, -2, -10))
add_script(camera, 'UXFacilityCameraFollow')
player = clone('Player', 'Player', -12, -6, descendants=True,
    scripts={'PlayerController': {'moveSpeed': 2.8}})
clone('GameManager', 'GameManager', 0, 0)
director = clone('EvidenceManager', 'EvidenceManager', 0, 0)
add_script(director, 'UXStealthDirector')

# Reuse the Deluxe tile and renderer templates; floor tiles carry no colliders.
for y in range(-8, 9):
    for x in range(-14, 15):
        asset = 'deluxe_floor_alt' if (x + 2 * y) % 7 == 0 else 'deluxe_floor'
        tint = (0.78, 0.89, 1, 1) if x < -3 else ((0.83, 0.93, 1, 1) if y >= 0 else (0.86, 0.94, 0.98, 1))
        clone('Floor', 'Floor', x, y, sprite=art + asset + '.png', order=-20, color=tint)

walls = set()
for x in range(-14, 15): walls.update([(x, -8), (x, 8)])
for y in range(-7, 8): walls.update([(-14, y), (14, y)])
walls.update((-3, y) for y in range(-7, 8) if y not in [-4, -3])
walls.update((x, -3) for x in range(-13, -3) if x not in [-6, -5])
walls.update((x, 2) for x in range(-13, -3) if x not in [-10, -9])
walls.update((x, 0) for x in range(-2, 14) if x not in [2, 3, 10, 11])
walls.update((x, 4) for x in range(0, 7) if x not in [3, 4])
walls.update((7, y) for y in range(1, 7) if y not in [4, 5])
walls.update((8, y) for y in range(-7, -1) if y not in [-5, -4])
walls.update((x, -2) for x in range(8, 14) if x not in [11, 12])
for x, y in sorted(walls):
    clone('ServerRoomWall', 'FacilityWall', x, y, color=(0.78, 0.92, 1, 1))
    wall_records.append(dict(name='wall', x=x, y=y, w=.98, h=.98))
    if (x + y) % 4 == 0:
        clone('Wall Fixture', 'WallLight', x, y + .18, scale=(.65, .65, 1), order=7)

cover = [(-12,-4), (-8,1), (-11,5), (1,-2), (6,-6), (4,2), (1,6), (10,4), (12,-4)]
for index, (x, y) in enumerate(cover):
    clone('ServerRack' if index % 2 else 'CoverCrate', 'SolidCover', x, y)
    wall_records.append(dict(name='cover', x=x, y=y, w=.98, h=.98))
for x, y in [(-11,-5), (-7,0), (-12,4), (-1,-6), (6,-4), (1,2), (5,6), (10,1), (10,-6)]:
    clone('HidingSpot', 'HidingSpot', x, y, color=(.25,.75,1,1))
    clone('Blue Perimeter', 'HidingGlow', x, y-.2, scale=(1.2,1.2,1), order=-4)

# Open passage widths are two tiles. Cyan chevrons sit on floor, never on solid walls.
passages = [(-5.5,-3),(-9.5,2),(-3,-3.5),(2.5,0),(10.5,0),(3.5,4),(7,4.5),(8,-4.5),(11.5,-2)]
for x, y in passages:
    vertical_wall = x in [-3,7,8]
    clone('Hazard Threshold', 'OpenPassageThreshold', x, y, scale=(1.7,.85,1),
        angle=90 if vertical_wall else 0, color=(.25,1,1,1))
    clone('Cyan Floor Route', 'OpenPassageChevrons', x, y-.3, scale=(.7,.7,1),
        angle=0 if vertical_wall else 90, order=-5)
for x,y in [(-12,-6),(-8,-5),(-5,-1),(-11,3),(0,-4),(4,-4),(5,1),(8,5),(11,-3)]:
    clone('Floor Vent', 'FloorVent', x,y,scale=(.58,.58,1))

gate = clone('ServerRoomWall','AccessGate',-3,-3.5,scale=(1,2,1),color=(1,.19,.3,1))
wall_records.append(dict(name='gate',x=-3,y=-3.5,w=.98,h=1.96))
key = clone('HidingSpot','AccessKeycard',-11,6,sprite=art+'deluxe_evidence.png',color=(1,.77,.15,1),order=12)
# The key is not an evidence item and does not conceal the player.
kind, body = out[key]; out[key] = kind, field(body,'m_TagString','Untagged')
data_positions = [(3,6),(4,-6),(12,3)]
for n,(x,y) in enumerate(data_positions):
    go=clone('Evidence','Evidence_'+str(n+1),x,y,sprite=art+'deluxe_evidence.png',order=12)
    add_script(go,'UXExpandedInteraction')
terminal=clone('Terminal','Terminal',12,6,sprite=art+'deluxe_terminal.png',order=12,
    scripts={'TerminalInteraction': {'requiredEvidence':3}})
add_script(terminal,'UXExpandedInteraction')
clone('ExtractionZone','ExtractionZone',11,-6,sprite=art+'deluxe_exit.png',order=10,
      scripts={'ExtractionZone':{'requiredEvidence':3}})
clone('Extraction Door','ExtractionArch',11,-6,scale=(1.3,1.3,1),order=9)
clone('Extraction Glow','ExtractionGlow',11,-6,scale=(2.2,2.2,1))

routes = [
    [(-11,-1),(-6,-1),(-6,0),(-11,0)],
    [(0,-5),(5,-5),(5,-3),(0,-3)],
    [(9,6),(12,6),(12,2),(9,2)],
]
for n, route in enumerate(routes):
    go=clone('Guard','Guard_'+str(n+1),*route[0],descendants=True,
        scripts={'GuardDetection':{'detectionRange':3.4,'detectionAngle':55}})
    for ident,(kind,body) in list(out.items()):
        if kind!=114 or not re.search(r'm_GameObject: \{fileID: '+go+r'\}',body):continue
        if guid('Scripts/UXPreviewGuardFacingPatrol.cs') in body:
            body=re.sub(r'  routePoints:[\s\S]*','',body)
            body=field(body,'speed',1.2);body=field(body,'turnDuration',.75)
            body=field(body,'minimumPause',1);body=field(body,'maximumPause',1.5)
            body+='  routePoints:\n'+''.join('  - {x: %s, y: %s}\n'%p for p in route)
            body+='  routeEdges:\n'+''.join('  - {x: %s, y: %s}\n'%(i,(i+1)%len(route)) for i in range(len(route)))
            out[ident]=kind,body
for n,(x,y,angle) in enumerate([(-8,6,225),(5,7,225),(12,2,270)]):
    clone('SecurityCamera','SecurityCamera_'+str(n+1),x,y,angle=angle,
        scripts={'SecurityCamera':{'rotationSpeed':24,'sweepAngle':35,'detectionRange':5.4,'detectionAngle':44}})

scene='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'
scene+=''.join('--- !u!%d &%s\n%s'%(kind,ident,body) for ident,(kind,body) in out.items())
scene+='--- !u!1660057539 &9223372036854775807\nSceneRoots:\n  m_ObjectHideFlags: 0\n  m_Roots:\n'
scene+=''.join('  - {fileID: '+i+'}\n' for i in roots)
target=ASSETS/'Scenes/UXFacilityExpanded.unity'
target.write_text('\n'.join(line.rstrip() for line in scene.splitlines()) + '\n')
scene_guid=uuid.uuid5(uuid.NAMESPACE_URL,'pixel-rebellion/expanded-level1-scene').hex
Path(str(target)+'.meta').write_text('fileFormatVersion: 2\nguid: '+scene_guid+'\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
(ROOT/'tools/expanded_level_geometry.json').write_text(json.dumps(dict(
    walls=wall_records,spawn=[-12,-6],key=[-11,6],gate=[-3,-3.5],data=data_positions,
    terminal=[12,6],exit=[11,-6],routes=routes,passages=passages),indent=2)+'\n')
print('Created',target.relative_to(ROOT),len(out),'objects/components,',len(walls),'wall tiles')
