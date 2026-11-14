# Expanded Level 1 demo candidate — November 12, 2026

This is a separate scene in the existing demo candidate, based on main `ddd87ea` and
Victor's Deluxe art `d5b209e`, after the briefing and default crouch updates.

Open `Assets/Scenes/UXFacilityExpanded.unity` in **Unity 6000.2.2f1**.
The installer makes this the first enabled build scene and preserves the other entries.
`UXFacilityPreview.unity`, `SampleScene.unity`, packages and unrelated settings are preserved.

## Implemented in this candidate

- Facility expanded from 19 × 11 to 29 × 17 tiles. A following camera keeps characters readable.
- Two-tile passages carry bright threshold strips and directional floor chevrons.
- A gold access keycard opens the red security gate; it does not count as evidence.
- Three evidence caches require holding E for 2.5 seconds each. Releasing E or leaving range cancels progress.
- Holding E at the terminal for 4 seconds requires all three caches. The existing terminal code disables cameras and unlocks extraction.
- The extraction annex has one service entrance and one alternate east entrance.
- Three guards use slow patrols with pauses and narrower, wall-clipped cones. Three cameras sweep slowly.
- Full detection starts shared pursuit. Breaking sight leads to yellow search, then patrol recovery.
- Guards aim before shooting. Each hit removes 25 of 100 health; shared brief protection prevents simultaneous hits from all guards.
- Marked hiding spots conceal the player. Health does not regenerate. Ordinary crouching does not grant invisibility.
- Four-direction armed guard animation, default crouching player animation, upright sprint, updated briefing, health/alert HUD and hold-Tab blueprint.

Knife takedowns, bodies and the later dark security wing remain outside this recording checkpoint.

## Verification completed outside Unity

- Parsed all C# sources; type-checked runtime sources against isolated API stand-ins.
- Executed the pure alert, damage and navigation rule cases.
- Executed isolated interaction checks for key gating, cancelled downloads, pause handling and authoritative terminal activation.
- Executed pursuit and return across the saved facility geometry, checking swept clearance at every movement step.
- Checked scene file references, asset GUIDs, mandatory gate, every objective and passage, patrol edges and the two extraction entrances.
- Rendered actual briefing draw calls at 960 × 540, 1280 × 720 and 1920 × 1080; checked bounds and text fit.
- Executed the PowerShell installer on LF/CRLF copies: apply, rerun, backups, rollback, conflicting edits, Unity-expanded metadata and interrupted-install resume.

These checks do **not** establish a Unity editor compile, Unity physics result, platform build or playable mission duration.

## Windows playtest observed on November 12

Victor supplied `20261112-1956-42.7449127.mp4` (5 minutes 17.7 seconds).
The recording shows the updated briefing, crouched animated player and armed guards,
visible patrol/camera cones, the keycard opening the gate, hiding and the blueprint,
all three evidence downloads, the terminal hack, cameras going offline, **MISSION COMPLETE**,
and a return to the briefing. This establishes a successful playable mission in Unity
6000.2.2f1 on Victor's Windows machine.

The visible run retains 100 health and does not establish full pursuit/damage/search recovery.
Those features retain the isolated rule/integration checks above and need one focused Unity
check before final release builds. No Windows/macOS standalone build has been certified here.

The seven legacy editor startup callbacks now skip the expanded scene. Their original
name lookups (`Guard`, `SecurityCamera`, `Evidence`) could otherwise create prototype objects
when reopening this scene, which uses numbered actors. Isolated callbacks confirm zero
expanded-scene mutations and preserved setup behavior in the original sample scene.

## Unity acceptance pass before recording

1. Confirm the expanded scene name, matching editor and no red Console errors.
2. Deploy. Check default crouch, careful movement, sprint, camera follow and legible HUD.
3. Confirm the red gate blocks the route; collect the key, then pass through the visibly open gate.
4. Download all three caches; release E once to confirm cancellation. Confirm the terminal stays locked before the third cache.
5. Let one camera or guard fully detect you. Confirm pursuit and damage, break sight, enter marked cover and wait through yellow search. Guards should resume their routes without wall clipping.
6. Complete the terminal hack, confirm every camera is disabled, and extract. Repeat after restarting; check pause/resume.
7. Use both extraction entrances. Adjust pacing only after observing an entire clean run. Target a concise, enjoyable recorded demonstration; a duration has not been certified.

## Demo sequence (maximum 3 minutes)

- 0:00–0:15: Briefing, mission objectives and updated character presentation.
- 0:15–0:50: Sneak past the first security zone, collect the key, show the gate opening.
- 0:50–1:40: Evidence downloads, marked passages, cover and one controlled detection/recovery if practical.
- 1:40–2:30: Final cache, terminal hack, cameras going offline and extraction.
- Leave the completion screen visible for a few seconds. Rehearse the route before recording.

Timing is an editing plan, not a measured playthrough. Fix any blocking Unity error before recording.

## Team integration and builds

Victor owns the UX presentation and demonstration candidate. Elvin should review the added pursuit,
health and interaction rules before team integration and validate the gameplay/builds. Jazmine owns
analytics review (existing detection/collection/terminal/end-session calls are retained). Gilberto
coordinates approval, demo submission and the Windows/macOS deliverables.

Do not merge the preview PR or submit builds as final until this candidate passes Unity playtesting.
Use Unity's platform build workflow with the matching Windows/macOS support modules; validate each
resulting executable on its target platform. No platform builds were produced in this environment.

The update creates `.git/pixel-rebellion-expanded-level1.json` and verified file backups. To undo only
this pass, close the editor and rerun the same downloaded script with `-Rollback`. Rollback refuses to
overwrite subsequent source edits. Earlier preview, briefing and default crouch work remains available.
