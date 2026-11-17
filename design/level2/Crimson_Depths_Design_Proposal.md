# Level 2 proposal: Crimson Depths

Status: approved design, implemented as a local playtest candidate on November 14,
2026. This document retains the design rationale and working targets; current
implementation and acceptance results are in [Crimson_Campaign.md](../../docs/Crimson_Campaign.md).
The scene uses 1,401 player-clear sampled floor cells (4.13× Level 1) after added
console cover. The planning diagrams below remain concept references.
Development branch: `victor/level2-crimson-depths`, based on Level 1 commit
`1c79d0fe3c4eff14f86ac57d9b1710c4f572e3ba` in PR #25.

## Experience and scale

The Level 1 extraction lift descends into a restricted industrial complex.
Charcoal floors, burgundy bulkheads, red emergency strips, server lights and a
reactor glow give this mission a different identity. The player, vision cones,
objectives, cover and exit retain clear silhouettes and labels. Environmental
red must remain distinguishable from active danger feedback.

The proposed map is 55 by 33 tiles. Its initial planning grid contains 1,410
connected floor cells, compared with 339 sampled Level 1 floor cells: approximately
4.16 times the area under that grid comparison. The acceptance requirement is
at least three times Level 1's traversable area after final collision and cover
placement. The planning-grid ratio is not an engine-measured floor area.

| Element | Level 1 | Level 2 target |
| --- | --- | --- |
| Guards | 3 | 12; four per sector |
| Cameras | 3 | 12; four per sector |
| Data caches | 3 | 9; three per sector |
| Camera consoles | 1 | 3 independent consoles |
| Mission areas | One facility | Docks, Archives, Reactor Vault |
| Mid-mission recovery | Restart the mission | Entry checkpoint and three sector checkpoints |

Target first-run length is roughly 10–15 minutes, to be tuned through real
playtesting. The added size should contain useful routes, cover and decisions,
not long empty corridors. Controls remain familiar. Takedowns remain outside
this version.

## Three sectors

**A — Infiltration Docks.** Cargo stacks, loading lanes, blind corners and a
covered service route establish the new setting. Collect the three Dock caches,
reach console A, disable only the Dock cameras and open the Archive bulkhead.
Checkpoint 1 is a protected maintenance room beyond that bulkhead.

**B — Forbidden Archives.** Server aisles, secure laboratories, intersecting
patrol routes and a loop that avoids repeated backtracking. Collect the three
Archive caches and use console B. Only Archive cameras switch off; Vault cameras
remain active. Open the next bulkhead and save at checkpoint 2.

**C — Reactor Vault.** Navigate the reactor's inner and outer routes, gather the
three remaining caches, and hack console C. Save at checkpoint 3 in a protected
control alcove, then reach and override the final extraction door. All nine data
IDs and all three console IDs are required. There is no bypass around the exit
gate. Guards remain active after cameras are hacked.

Each sector's console is accessible before opening the gate it controls. No
required cache is placed behind its own still-locked progression gate. Caches
within an accessible sector may be collected in any order. Successful hacks
open useful shortcuts where the final layout supports them.

Four guards per sector have distinct routes: perimeter, interior aisles, a
crossing route and an objective approach. Responses are local to the sector;
one sighting must not summon every guard in the level to a single point. Patrol
timing must leave genuine opportunities to complete required holds. The approved
Easy/Hard presets remain the starting point for balancing, including shared hit
protection. Mode selection continues to show only **Easy Mode / Hard Mode**.

## Campaign and checkpoint behavior

Level 1 extraction records the clear and a valid Level 2 entry checkpoint before
loading the next scene. A short fade and level-title banner bridge the scenes;
the player does not need to choose Retry, Main Menu or Deploy between levels.
If the game is closed during the transition, Continue still starts at Level 2.

| Event | Required behavior |
| --- | --- |
| First launch in a mode | Start Game begins Level 1 |
| Resume a campaign | Continue loads the latest checkpoint in the selected mode |
| Complete Level 1 | Save its clear; automatically enter Level 2 at checkpoint 0 |
| Secure a sector | Save its caches, terminal, gate states and next checkpoint |
| Die in Level 2 | Continue restores the last safe checkpoint; Level 1 stays cleared |
| Quit and reopen | Continue restores the same saved checkpoint |
| No mid-level checkpoint yet | Resume at that level's entry; do not replay cleared levels |
| Complete Level 2 | Record its clear; show completion until the next real scene exists |

Checkpoint restoration gives 100 health, clears the local alert and returns
guards to valid patrol positions. It restores only objectives secured at that
checkpoint; later unsaved objectives must be repeated. Checkpoints must be
behind solid cover, outside camera sight, and reachable without spawning inside
a collider. The four green map markers are proposed anchors; their safe room
enclosures and exact clearances must be verified in Unity.

Easy and Hard have separate campaign slots, consistent with their existing
separate mission records. A running campaign retains its chosen difficulty.
Existing version-1 saves migrate without losing completion counts, best times
or settings. A mode that already cleared Level 1 receives access to Level 2's
entry checkpoint. Saving is atomic; failed or unsupported saves are retained
and reported, never silently erased.

## Continue and restart screens

The front screen uses **Continue** as the primary action when that mode has saved
progress, otherwise **Start Game**. It also offers **Restart Game**, the two mode
labels, settings and exit. Continue does not replay the full initial briefing.
New campaigns still receive the Level 1 briefing before deployment.

Restart Game requires a modal confirmation:

> Restart the game? All level progress, checkpoints and saved mission records
> in Easy and Hard will be erased. You will start again at Level 1.
>
> Cancel / Restart Game

Cancel is the default. Settings and the anonymous research-player identifier
are retained. The confirmation's wording must match the actual reset scope.

Pause and the R shortcut use **Restart Level**, a separate action. Its warning
states that current-level progress and checkpoints will be lost while previously
cleared levels and their records remain. On death, the primary button is
**Continue**; restarting the level is secondary. Returning to the main menu
explains that progress after the last checkpoint is not retained.

## Implementation order and acceptance

1. Add level definitions, campaign progression, save migration, checkpoints and
   the Continue/Restart flows. Verify interruption between Level 1's clear save
   and Level 2's load. Keep a complete playable Level 1 checkpoint available.
2. Build the large Level 2 scene with sector objectives, camera groups, gates,
   cover and patrols. Generalize Level 1's fixed bounds and three-cache/single-
   terminal assumptions through level configuration; avoid a second copy of
   its gameplay controller.
3. Apply the red industrial art treatment, readable lighting and contextual
   feedback. Retain the current cone-occlusion fixes and distinct danger cues.
4. Playtest complete Easy and Hard campaigns. Test death and app relaunch at
   each checkpoint, both restart scopes, all locked-gate prerequisites, save
   migration and persistence. Measure collisions, frame rate and routes in a
   standalone/WebGL build, not only in the Editor.

Cache static navigation bounds, reuse vision meshes, and update active security
without unnecessary searches or allocations each frame. Preserve anonymous
metrics and add level/checkpoint context and retry events. A successful level
clear must be recorded exactly once, including after a reload. Respawning must
not reset the elapsed clock within the current run.

The proposal generator verifies connected floor, reachability of every proposed
marker, access to each sector's data and console before opening its gate, and
the absence of a final-gate bypass. These are planning checks, not physics,
rendering, patrol timing or balance validation.

## Milestones and working targets

The live repository lists Milestone 4 for **November 15, 2026** and Milestone 5
for **November 29, 2026**. Stories 4.1, 4.2, 5.1 and 5.2 remain open at the time
of this draft; PRs #24 and #25 await review. The original UX plan scoped one
complete level, so this campaign expansion is additional work rather than a
completed original milestone.

Proposed working targets, subject to the team's actual submission requirements:

- November 14–15: maintain the Level 1 review/build evidence and finalize this
  expansion design; begin campaign/checkpoint foundations in the separate branch.
- November 16–20: implement and playtest all three Level 2 sectors and progression.
- November 21–23: campaign regression testing and difficulty/visual refinement.
- November 24–29: final usability evidence, supported builds/WebGL delivery and
  documentation. Add a third level only when this required delivery work fits.

This is a proposed allocation, not a claim that the outstanding review,
deployment or course milestones have already been completed.
