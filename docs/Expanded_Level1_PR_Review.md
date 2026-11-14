## Result

Adds a complete expanded Level 1 demo for Pixel Rebellion: Shadow Protocol, integrating Victor's Deluxe facility art with the team's main-branch gameplay baseline. A Windows Unity playtest reaches Mission Complete after the keycard, three downloads, terminal hack and extraction.

## Changes

- Larger facility, clear two-tile passages, a keycard-controlled gate and two extraction approaches.
- Default crouched movement, upright sprint, animated armed guards, improved briefing, HUD and blueprint map.
- Three patrols and cameras, recoverable alerts, guard aiming and health damage; expanded-scene interactions reuse the authoritative terminal/evidence/extraction systems.
- Wall-clipped vision cones and patrol navigation; legacy editor startup scripts skip the authored expanded scene to prevent duplicate prototype objects on reopen.
- Expanded scene first in the build list. SampleScene, package versions and unrelated project settings are preserved.

## Validation

- Victor's Windows Unity 6000.2.2f1 recording shows the gate opening, all three evidence downloads, cameras disabled by the terminal, Mission Complete and return to briefing.
- Isolated alert, damage, interaction, navigation, editor callback and installer checks pass; scene geometry/reference checks and briefing layout checks pass.
- The recorded run remains at 100 health. Full pursuit/damage/search recovery, restart-after-loss and Windows/macOS standalone builds still need focused Unity/target-platform checks.

## Integration

This candidate includes the Deluxe assets from Draft PR #21 and the subsequent integration, briefing, crouch and expanded-level work. Review its overlap with #21; merging both independently can reintroduce conflicting HUD code. Review this candidate as one proposed integration against main. No merge is requested by this draft.

Elvin: gameplay/AI and build review. Jazmine: analytics event review. Victor: UX and demo. Gilberto: submission coordination.

Gilberto's deliverables are a gameplay video of at most three minutes, revised Windows/macOS builds and a README update if needed. Knife takedowns, bodies and the later dark security wing are deferred.
