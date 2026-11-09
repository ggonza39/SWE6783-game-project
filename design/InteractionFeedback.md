# Pixel Rebellion: Shadow Protocol – Interaction Feedback

This document defines the proposed player-facing feedback for important interactions. The goal is to make actions easy to understand without requiring the player to guess whether an input worked.

| Interaction | When Player Is Nearby | Player Input | Feedback After Action |
|---|---|---|---|
| Evidence | `E - Collect Evidence` | E | Evidence counter increases and a short confirmation appears |
| Terminal | `E - Use Terminal` | E | Terminal state changes and the player receives confirmation |
| Hiding Area | `E - Hide` | E | Player receives a clear visual indication that they are hidden |
| Extraction Zone - Locked | `Extraction Locked` | — | Player is told required objectives are incomplete |
| Extraction Zone - Available | `E - Extract` | E | Extraction activates and the level completion sequence begins |

## Feedback Guidelines

- Interaction prompts should appear only when the player is close enough to interact.
- The same interaction key (`E`) should be used consistently.
- Successful actions should provide immediate visual feedback.
- Locked or unavailable actions should explain why they cannot be completed.
- Important state changes should use the established color language:
  - Green = safe, available, or completed
  - Yellow = warning or attention
  - Red = danger, locked, or failed state
  - Blue = objectives, evidence, terminals, and general information
- Prompts should disappear when the player moves away from the interactable object.
