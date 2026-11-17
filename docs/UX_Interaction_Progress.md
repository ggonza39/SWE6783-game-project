# Hold-E interaction feedback

The expanded Level 1 evidence downloads and terminal hack now share a clearly
outlined progress meter. The meter has an opaque dark track, bright cyan fill,
white leading edge, and a percentage label placed above the fill. It follows
the director's actual progress each frame; interaction durations and rules are
unchanged.

- Ready: empty track, `0%`, and a hold-E instruction for the nearby objective.
- Holding: `DOWNLOADING EVIDENCE` or `HACKING SECURITY`, a cyan meter, and a
  reminder that releasing E or leaving the objective cancels the action.
- Success: green `100%` and an explicit completion label for 0.9 seconds,
  triggered by the confirmed evidence-count or terminal-activation change.
- Cancellation: the meter resets without displaying success.

The expanded scene reserves footer space for the meter, keeping the camera
viewport above it. The legacy scene's footer stays at its original height.

## Unity review

Open `src` in Unity **6000.2.2f1**, then play
`Assets/Scenes/UXFacilityExpanded.unity`.

1. Hold E at each data cache and watch the fill and percentage advance together.
2. Release E halfway through; verify reset without green completion. Start again.
3. Walk out of range while holding E; verify cancellation.
4. Finish all three downloads, then hack the terminal. Check green completion,
   camera shutdown, and successful extraction.
5. Open Esc during an interaction, resume, and verify feedback matches the actual
   interaction. Retry the mission and confirm that old completion feedback clears.
6. Inspect the footer at 960x540, 1280x720, and 1920x1080, including a window resize.

This patch is prepared from main commit `14caca3`. A Unity playtest of the new
meter is required before committing and rebuilding the next release.
