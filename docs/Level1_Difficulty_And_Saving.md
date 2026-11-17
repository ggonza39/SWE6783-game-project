# Level 1 modes, mission menu, and local progress

Open `src` in Unity **6000.2.2f1** and play
`Assets/Scenes/UXFacilityExpanded.unity`. Choose **Easy** or **Hard**, select
**Start Level 1**, review the briefing, then select **Deploy**.

## Mode differences

Both modes use the same facility, objectives, hiding bays, and 100 health.
Easy hits cost 25 health; Hard hits cost 34, so three separated hits end a Hard
run. Shared protection still prevents simultaneous guards from causing instant
failure. Breaking sight and hiding clears pursuit; Hard searches last longer.

| Behavior | Easy | Hard |
| --- | --- | --- |
| Patrol speed relative to original | 0.85x | 1.55x |
| Guard cone angle relative to original | 0.85x | 1.45x |
| Camera cone angle relative to original | 0.90x | 1.50x |
| Alert growth relative to original | 0.75x | 1.80x |
| Guard aiming time before first shot | 1.00s | 0.45s |
| Guard firing interval | 1.60s | 0.90s |
| Damage per hit | 25 | 34 |
| Evidence hold time | 2.50s | 5.00s |
| Security-terminal hold time | 4.00s | 8.00s |
| Exit door override | None | One required door; 8.00s |

Hard mode has one usable extraction door at the west service entrance. A solid
bulkhead seals the north approach, making the west door unavoidable. The exit
control is on the accessible side and marked on the Tab map. Collect all data
and hack the terminal first, then hold E continuously for eight seconds at the
exit control. Releasing E or moving away resets the override. Opening the door
removes its collider and updates guard navigation; the north bulkhead stays
closed. Easy retains its original two extraction approaches.

The selected mode is fixed for the duration of a run. A replay or retry starts
a fresh mission in the selected mode, while completed-run records remain saved.

## Presentation and camera polish

The difficulty selector contains only **Easy Mode** and **Hard Mode**. Selection
uses a highlighted outline. The briefing and pause guide describe mission goals
and controls without revealing difficulty values or the exit solution. Contextual
hold prompts still give the information needed at the interaction itself.

Vision rays ignore actors but still stop at solid walls, crates, doors, and hiding
bay shells. This prevents a passing guard from cutting a moving hole in a camera
cone. Mesh edges receive extra samples at occluder silhouettes; fixed mesh arrays
are reused each frame. Hacked cameras stop sweeping and their cones disappear.
Camera sweeps use a local clock that freezes during pause rather than jumping to
the global clock. Difficulty ranges, angles, sweep speed, damage, and mission
requirements retain the approved Hard tuning.

Takedowns are outside the current demo scope. Active patrols support its stealth
loop; completing objectives and reaching extraction remain the player's goals.

## Saved data

`pixel_rebellion_progress_v1.json` is stored in Unity's per-user persistent-data
folder. It stores the selected difficulty, fullscreen preference, optional
control reminders, completion counts, and best successful time for each mode.
Stable level IDs allow future missions to add records without replacing Level 1.
The original Level 1 update contained one playable level; the current campaign
adds Crimson Depths. See [campaign behavior](Crimson_Campaign.md) for schema-2
checkpoints, continuation and restart scope.

Level 1 resumes at its entry; Level 2 now has secured-sector checkpoints. The existing anonymous
player ID and telemetry files are retained. Telemetry records the actual mode
chosen at the start of each session.

Save writes use a temporary file and keep a valid previous version as a backup.
An unreadable primary file can recover from that backup. An unsupported future
version or an unreadable file without a valid backup is preserved rather than
overwritten; the menu reports a save problem and the current run can still play.

## Validation and Unity acceptance

Isolated checks cover longer/cancellable/paused interactions, the reachable
exit control, an unavoidable door with a sealed alternate route, increased guard pressure, shared
hit protection, one completion record per victory, and save reload/recovery.
Home/settings layout bounds are checked at 960x540, 1280x720, 1920x1080, and
2560x1440. Real Unity EditMode tests for save data and difficulty rules are
included in `Level1ProgressTests.cs`; isolated stand-ins do not replace an
engine playtest.

Before committing this update:

1. Complete an Easy run; return to the menu and check its completion/best time.
2. Stop Play mode, start it again, and confirm the saved selection and record.
3. Play Hard, checking the wider cones, stronger guard response, and longer
   hold-E bars. Release E once and check the reset.
4. Reach the exit control without crossing the door. Confirm it stays locked
   before the terminal hack, requires eight seconds to open, and is the only
   route into extraction. The northern bulkhead must remain solid after opening.
5. Retry after taking damage; check a fresh health/mission state while preserving
   previously completed-run records.
6. Check Esc, the briefing's Back button, settings, and the existing hiding bays.
   Test fullscreen/windowed behavior and Exit Game in a standalone build.

The scene and its serialized objects are not rewritten by the installer. The
exit door and northern bulkhead are created at runtime only in Hard mode.
