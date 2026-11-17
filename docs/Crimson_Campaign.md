# Crimson Depths campaign candidate — November 14, 2026

> Current behavior, November 16: Death > Replay restores 100 health and 10 seconds of stamina in both levels and modes. Live Continue keeps spent stamina and the current mine layout. New attempts shuffle concealed mines. Secured checkpoints, records and settings remain. Hard guard vision matches Easy; faster pursuit/fire remains. Mine hits knock the player back; fatal blasts scatter pixel fragments and floor splatter. Earlier dated notes below describe their historical behavior.
>
> Playtest: exhaust sprint, die, Replay; check full health/stamina and the same checkpoint. Test a surviving mine hit beside a wall, fatal mine fragments in Watch Again, different mine positions on Replay, and secured-sector traps remaining disabled.


This branch adds a second playable mission to the reviewed Level 1 source at
`1c79d0fe3c4eff14f86ac57d9b1710c4f572e3ba` (PR #25). It is ready for local Unity
acceptance testing, not a claim of completed course milestones or a published build.

Open `src` in **Unity 6000.2.2f1**. Start with
`Assets/Scenes/UXFacilityExpanded.unity`. The front screen offers Start Game or
Continue for the selected mode. Existing version-1 saves with a Level 1 clear
migrate directly to the new mission. Easy and Hard keep separate campaign slots.

## Mission

`Assets/Scenes/UXCrimsonDepths.unity` is a 55×33-tile red industrial facility:
Docks, Archives and Reactor Vault. It contains 12 guards, 12 cameras, nine data
caches, three independent consoles, three hiding refuges, one per sector; other containers remain solid cover.
The sampled player-clear floor area is 1,401 versus Level 1's 339 cells (4.13×),
using the checked serialized colliders; this is not an engine measurement.

Each sector has three caches and a console. Its console disables only that
sector's four cameras, secures the collected data and opens the next bulkhead.
Guards continue patrolling. The last console secures checkpoint 3; the player
must still override the single required extraction door and reach the lift.
The approved difficulty presets remain shared with Level 1. There are no takedowns.
Mode cards and the briefing do not describe difficulty effects.

## Progress and recovery

| Action | Result |
| --- | --- |
| Clear Level 1 | Record the clear and Level 2 entry before automatically loading Level 2 after a short title beat; no second Deploy |
| Hack a sector console | Save that sector's three caches, camera state, bulkhead and next checkpoint |
| Die, then Replay | Restore the latest secured checkpoint with 100 health, clear alert and a hidden bay spawn; repeat unfinished objectives |
| Close and reopen | Continue restores the same secured checkpoint; current run time and detections persist |
| Restart Level / R | Confirm loss of the current level's objectives/checkpoint/time/detections; earlier clears and mission records remain |
| Restart Game | Confirm erasure of both modes' level progress, checkpoints and mission records; return to Level 1 briefing |
| Cancel a restart | Preserve progress; Enter/Escape default to Cancel on the full-reset dialog |
| Complete Level 2 | Record the clear once; offer Replay Level 2 and the campaign menu; no placeholder Level 3 |

Checkpoint restoration does not preserve exact position, damage, open final exit
door or data gathered after the latest secured console. Time and detections span
failed attempts within the current level run, so a retry does not erase that cost.
Settings and the anonymous telemetry player identifier survive either restart.
Returning to the menu explains that unsecured objectives are not retained.

The existing file name `pixel_rebellion_progress_v1.json` is retained in
`Application.persistentDataPath`; its current contents use schema 4. Migration keeps
version-1/2 clears, best times, campaign checkpoints and settings. Writes use a checked temporary file,
atomic replacement where supported and a valid prior-file backup. Unsupported
newer saves are preserved. If saving fails, gameplay continues with a visible
save-unavailable notice; Continue after relaunch can only use the last disk save.
A failed full reset leaves existing in-memory records intact.

The shared `gameplay_metrics.csv` and Google Sheets payload keep their existing
fields. A separate local `campaign_events.csv` adds level/checkpoint/event,
cumulative seconds and detections, with PlayerID/SessionID for joining records.
Events cover level entry, sector secured, checkpoint retry, level clear, level
restart and campaign reset. Telemetry delivery still requires the existing
network endpoint; the isolated checks never send requests.

## Scene, import and performance changes

- Shared mission configuration supplies larger navigation and camera bounds.
- Security responses remain inside their sectors; a single sighting does not
  summon every guard in the map.
- Navigation reuses BFS and blocked-grid buffers; offscreen vision meshes skip
  rebuilding, and paused camera/guard feedback remains frozen.
- The tactical map includes the three consoles, current data, checkpoints,
  bulkheads and the mandatory exit door. A restored HUD always shows nine caches.
- Legacy automatic Editor setup helpers skip both campaign scenes and the
  authored preview. Importing Crimson must not insert legacy test objects.
- `UXCampaignSceneSetup` registers Level 1 first and Level 2 second for builds,
  retains other scene entries, and backs up changed EditorBuildSettings.
  Use **Pixel Rebellion → Campaign → Register Campaign For Builds** if needed.
- The original Level 1, preview and SampleScene scene files are unchanged.

The installer creates `victor/level2-crimson-depths` from the reviewed Level 1
commit, validates every touched source, backs up originals and supports no-op
reruns, resuming an interrupted write and guarded `-Rollback`. It does not commit,
push or overwrite ProjectSettings, packages, original scenes or unrelated work.
Rollback restores source only, retains existing saves, and keeps the new branch.
The older game preserves an unsupported newer save rather than deleting it;
reapply this update to resume those campaign saves. Editor build registration
is separately backed up and may leave a missing new-scene entry after rollback;
remove that entry in Unity Build Profiles or restore the recorded settings backup.

## Verification supplied

Run these from a terminal with PowerShell 7 (the installer supports Windows
PowerShell 5.1 as well):

```powershell
pwsh -NoProfile -File tools/verify_crimson_campaign.ps1
pwsh -NoProfile -File tools/verify_campaign_analytics.ps1
pwsh -NoProfile -File tools/verify_editor_scene_protection.ps1
pwsh -NoProfile -File tools/verify_level1_features.ps1
pwsh -NoProfile -File tools/verify_camera_polish.ps1
pwsh -NoProfile -File tools/verify_expanded_runtime.ps1
python tools/validate_crimson_level.py
```

These compile current runtime C# against Unity API stand-ins and use actual
serialized geometry for isolated checks. Both modes complete all objectives;
checks cover local camera groups, gate ordering, three enterable refuges, hold cancel
and pause, checkpoint disk reload, 100-health recovery, separate modes, migration,
restart scopes, missing-scene recovery and exactly one clear record. Two patrol
variants cover six simulated minutes without wall clipping; all required Hard
holds have clear windows in those variants. Menu, reset, briefing, map and HUD
bounds are checked at 960, 1280, 1920 and 2560 widths.

Unity EditMode save tests are included in `CampaignSaveTests.cs`, alongside the
existing tests. **Unity import, Test Runner execution, real physics/rendering,
frame rate, difficulty balance and standalone/WebGL builds have not been run in
this environment.** Offline art/layout renders and simulations are not Unity
captures or engine acceptance evidence.

## Unity acceptance before commit and builds

1. Import with a clear Console. Verify both scenes are enabled in Build Profiles,
   Level 1 first. Run EditMode tests in Test Runner.
2. Continue a migrated Level 1 clear into Level 2. Verify nine total caches, hidden
   entry spawn, readable art/vision cones and smooth camera movement at boundaries.
3. In both modes, collect three caches and hack each local console. Other sectors'
   cameras must remain on. Check each bulkhead, checkpoint toast and tactical map.
4. Die after each checkpoint; Replay must give 100 health, clear alert and the
   correct secured objectives. Gather an unfinished cache, stop Play, Continue and
   verify that cache returns. Confirm time/detections persist.
5. Finish all sectors, test the required exit door, then extract. Check the Level 2
   clear and best time. Relaunch and check both modes' records.
6. Cancel each restart. Test Restart Level separately. Use full Restart Game only
   when ready to deliberately erase both modes' records; confirm reset scope.
7. Start a fresh campaign, clear Level 1 and observe automatic Level 2 gameplay
   without another Deploy. Confirm Level 1's approved Hard door and timing remain.
8. Test narrow passages, guard pursuit and returns, hiding, pause/resume near
   cameras, held-E cancellation, settings and Exit in an actual standalone build.
   Check frame rate on the intended computer and complete the supported WebGL
   acceptance pass before publishing that build.

Milestone delivery still requires team review, usability evidence and supported
builds. Prioritize a complete, tested two-level campaign before considering a
third level in the remaining semester time.


## Hazard revision — November 14, 2026

The supplied Easy recording reaches extraction through all three sectors. This
revision adds new environmental mechanics without rewriting either scene:

- **12 proximity mines:** four per sector, recognizable amber diamond casings.
  Crossing their proximity radius arms a visible countdown and a full blast ring.
  Easy grants 1.60 seconds and a 60-health blast. Hard grants 0.60 seconds and a
  100-health blast. Sprinting or immediately retreating can escape; there is no
  random failure roll. The blast radius includes player-body clearance.
- **Six cycling laser barriers:** two per sector. Floor tracks and emitter lamps
  identify them even while safe. A yellow charging phase precedes the bright
  active beam. Easy cycles through 3.20s safe / .95s warning / 1.20s active;
  Hard uses 1.65s / .45s / 2.10s. Active contact costs 20 / 40 health respectively.
  Swept contact catches sprint crossings between rendered frames.
- **Local shutdown:** hacking a sector console neutralizes its four mines and
  two lasers along with its cameras; a green expanding pulse marks the shutdown.
  Armed mines are safely cancelled. Other sectors remain active.
- **Consequences and feedback:** blasts draw only the local patrol to the blast
  location. Solid walls and hiding bays protect the player. Shared hit protection
  prevents a trap and several guards stacking all their damage in one frame.
  Warning banners, ring countdowns, sparks, brief screen-edge flashes and specific
  death hints communicate what happened without shaking the camera.
- **Replay:** the death-screen button and keyboard hint say Replay. It restores
  the latest saved checkpoint; main-menu Continue and Restart Level keep their
  separate meanings. This revision does not erase existing saves or records.

Mine and laser clocks freeze during pause, deployment menus and result screens.
Replay restores disabled traps in secured sectors, while unsecured mines reset
for the next attempt. Individual spent mines are not an extra persistent
checkpoint. Existing save schema and shared telemetry fields are unchanged;
local campaign events add indexed MineArmed, MineHit/Protected/Escaped and LaserHit
entries. Guards do not trigger their own security traps.

`tools/verify_crimson_hazards.ps1` checks these rules against the current runtime
and serialized map. Its approach trials use the actual 2.8-unit movement speed
and 1.75× sprint multiplier, with a 150ms walking reaction before sprinting.
Geometry checks cover all eight approach directions at every mine, avoid required
hold points/bays, and check damage, solid-cover protection, local shutdown,
checkpoint restoration, pause and Replay behavior. Real Unity EditMode tests are
included in `CrimsonHazardTests.cs`; Unity physics/rendering/audio/frame-rate and
human balance acceptance still need an engine playtest.

In Unity, test one mine and laser on Easy first, pause an armed countdown, hack
that sector and confirm its traps turn green. Repeat on Hard to assess the short
escape window. Die and select Replay to check checkpoint restoration. Complete
both modes before committing; saved completion records from the earlier revision
remain valid but do not count as acceptance of these new hazards.


## Concealed mines and finite sprint — November 15, 2026

This revision follows the supplied Easy playtest of the mine/laser update.
Dormant mines now have **no visible hardware, glow, ring or map marker**. The
triggered mine reveals its warning ring/countdown and hardware so the player can
react. Other mines remain hidden; secured but never-triggered mines also stay
hidden. Spent, discovered hardware may remain visible until a replay.

The blast/body-clearance radius increases from **1.8 to 2.1 world units**. The
warning ring uses the same radius as damage. Easy keeps a 1.60-second warning;
Hard uses 0.70 seconds, allowing a short reaction before sprinting from a now
concealed, larger blast. Blast damage, laser rules, solid-cover protection and
local hack shutdown remain as in the hazard revision. An empty-bank warning
suggests backing away or finding cover instead of ordering an unavailable sprint.

Both campaign levels and both difficulties now use the same **24 seconds of
sprint per new level/run**. This is a finite reserve, not a regenerating meter:

| Event | Sprint behavior |
| --- | --- |
| Move with Shift | Spend one reserve second per physics second of requested sprint movement |
| Stand still holding Shift | No spending |
| Crouch, precision steps, hide, hold E, pause | No recharge and no sprint spending |
| Empty reserve | Shift gives no speed bonus; normal crouched movement remains available |
| Secure a sector / reach a checkpoint | Keep the current remaining reserve |
| Die and Replay; quit/reopen and Continue | Keep already-spent reserve, including a fully empty bank |
| Fresh level, Replay after a completed level, confirmed Restart Level/Game | Begin the new run with 24 seconds |

The HUD shows remaining sprint seconds, a colored bar and an explicit empty
message. Low reserve changes color. Sprint drains in the physics loop; its final
partial timestep pays only for the sprint fraction and uses crouch speed for the
rest. Holding against a wall with movement input is still a sprint attempt and
spends reserve. Existing preview/legacy scenes retain their prior controls.

Schema **3** adds remaining sprint seconds to each difficulty's campaign slot.
Schema-1/2 migration preserves level clears, best times, checkpoint, elapsed time,
detections and settings, and gives old slots the initial reserve once. Reloading
a schema-3 empty bank does not refill it. Saves snapshot the live balance at
one-second spend boundaries, sprint release/exhaustion, checkpoints and existing
pause/quit/death/menu transitions; the current balance is retained in memory if
writing fails. A forced process termination between snapshots can lose less than
one second of the latest spend; normal gameplay transitions flush it. Source
rollback retains schema-3 files, which the older game preserves as unsupported;
reapply this revision to resume those saves.

`tools/verify_sprint_conceal.ps1` covers migration, persistence, both-mode parity,
actual PlayerController physics-time drain, idle/precision/pause behavior, the
partial last step, exhaustion movement, no checkpoint/replay refill, new-run
refill, concealed hardware, larger-radius damage and HUD bounds. The updated
hazard checks sample all eight mine approaches, try straight/reverse/sideways
escapes, clamp movement at map solids, and require a surviving sprint response.
All mandatory objectives remain reachable without triggering a mine, and laser
gaps can be crossed at crouch speed. These are source/geometry checks, not a Unity
physics or frame-rate acceptance pass. Included Unity EditMode tests have not
been executed by an engine runner here.

The 24-second budget is a first balance setting for the next human playtest.
Test a fresh Easy Replay Level 2, spend a small amount, pause/hide, die and Replay,
then stop/start Play and Continue. Confirm the balance stays spent. Exhaust the
reserve and check crouched movement still works. Compare Hard's same starting
bank, larger blast escape, and the full campaign before committing this revision.


## Pressure balance revision — November 15, 2026

This section supersedes the earlier count, timing and 24-second budget notes.
The current Level 2 run has **24 concealed mines (eight per sector)**, six lasers
and **three hiding bays (one per sector)**. The other 19 authored bay locations
remain ordinary solid cover, with no hiding trigger, blue glow or HIDE label.
They still block movement and line of sight. Level 1's hiding layout is retained.

Sprint capacity is **10 seconds per level/run in both modes**, down from 24.
No recharge or checkpoint replay refill is added. Exhaustion still leaves normal
crouched movement. Confirmed Restart Level and a new level/run start a fresh bank.
Schema4 migrates the prior schema 3 bank proportionally: 12/24 becomes5/10, a full
24 becomes 10, and zero stays zero. Existing records, checkpoints, time/detections
and settings are retained; migration keeps a valid previous-file backup. Older
schema 1/2 profiles receive the new initial budget once. Source rollback retains
schema 4 saves; reapply this update to resume them in a compatible version.

The mine blast/body-clearance radius is now **3.2 world units** (formerly 2.1),
with a matching warning ring. Warning time is **1.10 seconds on Easy / .90 on
Hard**, so a moving player has time for a quick sprint while walking through the
open blast area fails after the tested 150ms reaction. Walls and ordinary cover
can still shield the player. Damage remains 60/100. Mines do not detonate other
mines; placements are separated to avoid an immediate second trigger on the
first escape. All required objectives have routes that avoid triggering mines;
with the scarce bank, players must plan routes and conserve sprint rather than
triggering every mine. Sector hacks neutralize their eight local mines and two
lasers alongside their cameras.

Four logical checkpoint states are retained. The entry/Docks refuge serves CP0,
the Archive refuge serves CP1, and the Vault refuge serves CP2 and CP3. The last
security hack does not add another nearby hiding bay. A CP3 replay returns to
that same Vault refuge, safely hidden with secured objectives restored. The
map and prompt show only the relevant checkpoint label at the shared position.

`tools/verify_pressure_balance.ps1` verifies the exact mine/refuge/retained-cover
counts, one refuge per sector, all four checkpoint restores, shared marker/prompt
behavior, mine spacing and proportional migration. The updated hazard checks
exercise eight approaches at every mine with candidate straight/reverse/diagonal
sprint escapes and solid-collision clamping, plus open-lane walking failures.
The campaign/guard checks retain mission reachability and patrol return coverage.
These are isolated source/geometry checks; real Unity physics, frame rate and
human difficulty acceptance remain the next playtest.

Use a fresh Easy Replay Level 2 to review the new full 10-second bank and three
refuges, then compare Hard. Check ordinary containers do not grant HIDDEN, both
Vault checkpoint states restore safely, and empty/replayed stamina stays spent.
Review a complete run before committing the new balance.


## Impact and extraction polish — November 15, 2026

This revision is applied after Pressure Balance. It keeps the 10-second equal-mode
sprint bank, 24 concealed mines, 3.2-unit blast radius and three hiding refuges.
Existing saves, mode slots, records and checkpoints keep their schema-4 contents.

| Mechanic | Easy | Hard |
| --- | --- | --- |
| Mine fuse | 1.10s, unchanged | 1.10s, increased from 0.90s |
| Mine damage | 60, unchanged | 85, reduced from 100; full health leaves 15 |
| Energized laser contact | Lethal | Lethal |
| Laser safe / warning / active cycle | 3.20 / .95 / 1.20s | 1.65 / .45 / 2.10s |
| Sprint reserve | 10s, no recharge | 10s, no recharge |

An injured player can still die from a mine. Mines and bullets keep their shared
brief hit protection; energized lasers deliberately bypass that protection so a
recent shot cannot make a lethal beam safe. Walls, hidden refuges, local shutdown,
swept contact and safe/warning phases retain their existing rules.

Stamina continuously blends green at full reserve through yellow at half to red
at empty. A white meter outline, decimal seconds and LOW / CRITICAL / EMPTY copy
reinforce the color signal. Neither mode gains extra energy.

Fatal hits display KILLED BY MINE, FRIED BY LASER or GUNNED DOWN BY GUARDS. Gameplay
and campaign time stop immediately, while a 1.05-second presentation beat shows
the impact before the death replay. A laser death adds electrical arcs, sparks,
a charred/slumped character and smoke. The hit is recorded only once; the reveal
does not delay the saved checkpoint recovery record.

Hard Crimson adds wall machinery with faint vapor and amber status lamps. The
extraction lift has metal rails, caution paint, chasing perimeter lights and
inward floor guides. It turns amber when the last console restores power and
green when the exit override opens. The HUD confirms the live extraction state;
a Hard clear is titled EXTRACTION CONFIRMED. These effects have no colliders,
new objectives or detection logic, and they freeze during pause. Renderer lists
are cached; animations create no per-frame objects or scene searches.

Additional isolated verification:

```powershell
pwsh -NoProfile -File tools/verify_mission_effects.ps1
pwsh -NoProfile -File tools/verify_crimson_hazards.ps1
pwsh -NoProfile -File tools/verify_pressure_balance.ps1
pwsh -NoProfile -File tools/verify_sprint_conceal.ps1
```

Unity acceptance before committing this candidate:

1. In Hard, trigger a mine at 100 health without another source hitting you.
   Confirm the 1.1-second warning and 15 health after the blast; sprint away in
   another attempt. Easy mine timing/damage should match the prior revision.
2. In each mode, verify safe and warning laser phases allow crossing, while
   active contact is lethal, including immediately after another hit. A wall
   must still block blast/beam line of sight and hacked sectors stay neutralized.
3. Watch a laser death: the player burns/slumps visibly, the headline says FRIED
   BY LASER, and Replay appears after the short effect. Check the mine and guard
   headlines separately. Replay must retain your checkpoint and spent stamina.
4. Watch stamina at 10, 5, 2 and 0 seconds in both modes. Confirm the continuous
   colors, readable outline and low-reserve copy, including a small game window.
5. In Hard, inspect the machinery and lift details; pause must freeze their
   motion. Hack all sectors, override the exit and extract. The lift signals and
   HUD must reflect the actual objective state and record one clear.
6. Check Console, frame rate and art layering in Unity, then the supported build.
   The source/geometry tests and offline renders are not Unity engine evidence.


### Recorded death replay

Both modes now retain a rolling clip of the actual rendered Game view: at most
48 downsampled GPU frames, twelve samples per second (about four seconds). The
buffer includes the approach and impact, guards/shots, mine warnings, laser phase,
HUD and camera view. It never moves actors back in time or reruns gameplay.

A fatal hit freezes gameplay and saves recovery immediately. After the short
impact beat, the clip plays automatically at 1.35x speed; Enter, Space or Escape
skips it. The Mission Failed screen then loops the clip. WATCH AGAIN rewinds only
the clip; REPLAY resumes the secured game checkpoint with spent stamina retained.
A recent hit produces less than four seconds of footage when less is available.

Frames remain in graphics memory only, bounded at 480x270 each while preserving
aspect ratio. The full-size staging buffer is released when the clip freezes;
all frames are released on scene destruction. Pause/menu screens are excluded,
resize discards mismatched frames, and capture freezes before replay UI appears,
so a clip cannot record itself. Allocation/capture failure falls back to the
normal failure screen and must not block Replay. There is no CPU pixel readback,
video encoding, disk recording or network transmission by this replay component.

Capture uses the documented Unity 6 end-of-frame flow:
[ScreenCapture.CaptureScreenshotIntoRenderTexture](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/screencapture/capturescreenshotintorendertexture)
and [Graphics.Blit](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/graphics/blit).

Additional Unity acceptance:

- Die after several seconds in each mode. Confirm the actual lead-up and fatal
  impact are visible and correctly oriented, with no black/recursive frames.
- Skip the automatic replay. Confirm it opens Mission Failed without restarting
  the level. WATCH AGAIN must rewind the clip; REPLAY must resume the checkpoint.
- Check frame rate and graphics-memory behavior while recording, paused, after a
  window resize and across several death/reload cycles in the intended build.
  The GPU capture itself needs this real Unity check; stand-ins validate the
  bounded ring, chronology, control paths, cleanup and failure fallback only.


## Replay orientation and distinct death effects — November 15, 2026

Applied after the Impact and Extraction Polish candidate. The supplied Windows
recording showed a vertically flipped replay and orange burst particles on a
guard death. The replay renderer now supplies explicit, backend-aware texture
UVs in the shared automatic/failed-screen drawing path, preserving aspect ratio.
Top-origin graphics backends flip the captured image once; bottom-origin
backends retain their orientation. The UI outside the footage is unchanged.

Death effects now have separate, exclusive profiles in both modes:

| Cause | Presentation |
| --- | --- |
| Guard fire | Quick collapse, small red droplets and an irregular blood pool spreading on the floor beneath the body; no fireball, blast debris, electrical arcs or smoke |
| Proximity mine | Expanding pixel fireball, shrapnel and smoke; no gunfire blood pool or electrical effects |
| Laser barrier | Electrical arcs and sparks, a charcoal corpse, dark ground scorch, residual embers and rising smoke; no mine explosion or blood pool |

The blood/scorch stays below actors and solid scenery. The camera response and
impact flash are small for bullets, electrical for lasers and stronger for mines.
All effects use cached renderers and stop animating after settling. Gameplay roots,
colliders, checkpoints, recordings and damage rules keep their existing ownership.

**Mine blast radius is reduced by 25% in both modes: 3.2 → 2.4 world units.**
The warning ring and existing burst expansion use this same radius. Trigger radius,
fuses, damage and laser cycles are unchanged: mines still deal 60 Easy / 85 Hard,
and both fuses are 1.10s. A prompt retreat can now clear the ring in the tested open
lane; the placement/navigation checks still cover collision-safe sprint escape.

Run the supplied mission-effects, Crimson-hazard, pressure-balance and sprint
verification scripts. They check both backend UV branches and both replay views,
exclusive effect profiles in each mode, blood-pool growth/layering, charred laser
color, ring/damage boundary just inside/outside 2.4 units, saved recovery and
recording controls. They use API stand-ins; actual graphics orientation and visual
quality still require a Unity test.

Unity acceptance for this fix:

1. Die to guard fire in Easy and Hard. Verify a fallen body and visible blood pool,
   with no orange explosion or smoke. Confirm GUNNED DOWN BY GUARDS.
2. Die to a mine while already injured. Verify the fireball, debris and smoke, then
   KILLED BY MINE. At full health, verify the existing 40 / 15 health outcome.
3. Touch an active laser in each mode. Verify electrical frying, a blackened body
   and scorch with smoke, then FRIED BY LASER.
4. Watch automatic playback and the loop on Mission Failed. The recorded HUD text,
   level and characters must be upright. WATCH AGAIN and checkpoint REPLAY must
   keep their separate functions. Test the intended standalone graphics backend.
5. Verify the smaller warning ring matches the mine's actual blast reach; sprint,
   save records/checkpoints and local security shutdown still behave correctly.

API references: [SystemInfo.graphicsUVStartsAtTop](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SystemInfo-graphicsUVStartsAtTop.html)
and [GUI.DrawTextureWithTexCoords](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/GUI.DrawTextureWithTexCoords.html).


## Clear color camera replay and action controls — November 15, 2026

Applied after the Death Replay Fix. Both modes now capture up to 1280x720 at a
20Hz target, versus480x270 at12Hz. The bounded80-frame history retains about four
seconds. Capture never upscales a smaller native Game view. Playback uses normal
speed and full color; no blur, grain or monochrome effect is applied.

The recorded camera viewport is retained with the clip. Playback composes that
viewport crop with the established backend-aware orientation UVs, so the image
keeps its aspect and the captured HUD/footer are excluded. Full-frame GPU capture
and blit remain the same proven path. The brief fatal caption now occupies the
reserved footer, outside the camera viewport. Viewport/resolution changes clear
mixed frames; pause and failure fallback retain existing behavior.

The automatic replay and failure card share a metal camera housing with cyan
corner brackets, CAM01/02 incident-archive labels, a COLOR/RECORDED status,
playback progress and a timestamp sampled with each real frame. Time of death is
captured once from the player's local clock when the fatal hit starts; the mission
elapsed time is retained separately. WATCH AGAIN never changes those values.

Below the camera housing, a separate opaque action deck says CHOOSE YOUR NEXT
ACTION. REPLAY uses a bright cyan button with RESUME SAVED CHECKPOINT beneath;
MAIN MENU uses an outlined button with RETURN TO MISSION MENU beneath. These
controls are outside the footage and camera bezel. WATCH AGAIN remains a clip
control in the bezel footer. Automatic playback also has a clickable SKIP REPLAY.

The uncompressed color cache can use about281MiB at the maximum dimensions/frame
count, plus staging/driver memory. It remains GPU-only, releases staging at freeze,
and frees frames on scene destruction. There is no video encoding, pixel readback,
recording file or replay network transmission. Check actual frame rate/memory on
the target computer and build; isolated tests do not measure GPU performance.

Acceptance:

1. In each mode, play several seconds and die. Confirm noticeably clearer color
   footage with normal-speed playback, correct orientation and a camera frame.
2. Verify the timestamp moves with recorded frames, while TIME OF DEATH and the
   mission time stay fixed when WATCH AGAIN is selected.
3. Confirm the recorded top HUD/footer are absent from the picture and the main
   buttons are visibly separated below it. Check mouse and Enter/Space/Escape.
4. Skip the automatic clip, watch again, then use REPLAY: checkpoint and spent
   stamina must remain. MAIN MENU must return to the mission menu.
5. Review small-window text, aspect fitting, live resize, repeated deaths and
   capture/playback frame rate in Unity before committing this candidate.
