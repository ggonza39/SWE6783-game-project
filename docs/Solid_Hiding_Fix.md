# Enterable hiding bays

The expanded demo's hiding locations are open maintenance bays. Walk through the opening marked by cyan floor steps to enter. The HUD changes from HIDE BAY to a green HIDDEN label above the player; the top status and the bay's status light also turn green. Walking back out removes the hidden state immediately.

Each bay has a solid guard exclusion volume and three solid metal sides. Only the player ignores the interior exclusion volume, so the player can walk inside through the open face while guards route around the entire bay. The side and back frames still block the player normally. Hiding activates when the player's center is inside the marked bay, rather than from a grazing trigger contact or an invisible spot beside a crate.

Normal patrols, pursuit, search, and return use the same navigation and swept movement checks. Guards cannot shove the player with contact forces and return safely when their old patrol anchor is occupied.

## Verification

- Actual runtime C# compiles against the isolated verification host, which does not simulate Unity physics.
- All nine bays have a clear player entry path, a player exception for the exclusion volume, and solid frames. Guard navigation remains blocked through their interiors.
- Isolated checks cover entry, exit, grazing contact, HIDE BAY/HIDDEN HUD cues, repeated setup, 18 simulated patrol minutes with occupied bays, pursuit, search, and occupied-anchor return.
- Mission interaction, damage protection, alert recovery, scene geometry, and briefing bounds remain covered by the existing checks.
- Engine EditMode tests check the actual selective collision settings and player/frame clearance. They require Unity to execute.

## Unity check before recording

Use Unity 6000.2.2f1 and `Assets/Scenes/UXFacilityExpanded.unity`.

1. Find an open blue maintenance bay. Follow the cyan floor steps through its open mouth and walk inside.
2. Confirm the green HIDDEN label above the player, STATUS HIDDEN in the HUD, and a green bay light.
3. Stay inside through two guard passes. Guards should walk around the bay without entering it or moving the player.
4. Walk out. The HIDDEN label should disappear immediately. Get detected, return inside, and wait for red to change to yellow and then normal patrol.
5. Complete the keycard, three downloads, terminal, and extraction sequence before recording.

The installer updates scripts and verification files without replacing scenes, packages, or project settings. It builds on the previous solid-cover update and backs up the current files before replacing them. Commit and push this focused fix to the existing branch after the Unity check passes.
