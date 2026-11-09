# Pixel Rebellion: Shadow Protocol — UX Design & Usability Plan

## Purpose

This document outlines the UX design work planned for *Pixel Rebellion: Shadow Protocol*. The goal is to make the game easy to understand, easy to control, and clear during stealth gameplay without adding unnecessary complexity.

The UX work will focus on the player journey, GUI layout, control mapping, interaction feedback, usability testing, and Version 1 to Version 2 improvements. The design will stay consistent with the current project scope: one complete top-down 2D stealth/action level with Easy and Hard modes.

---

## UX Goals

The interface and controls should help players:

- Understand the mission and basic controls quickly.
- Recognize guards, cameras, hazards, evidence, terminals, and extraction points.
- Know when they are safe, suspicious, detected, or recovering from detection.
- Keep track of score, lives, alert level, and evidence progress without clutter.
- Understand when an interaction is available and what action to take.
- Recover from accidental restart or exit actions.
- Complete the level without needing outside instructions.
- Experience a noticeable but reasonable difference between Easy and Hard modes.

---

## Player Journey

The planned player journey is:

1. **Main Menu**
   - Start Game
   - Difficulty
   - How to Play
   - Settings
   - Exit

2. **Difficulty Selection**
   - Choose Easy or Hard.
   - Show a short description of the difference between modes.

3. **Mission Briefing / How to Play**
   - Show the mission objective.
   - Introduce movement and interaction controls.
   - Explain the basic stealth and detection idea.

4. **Gameplay**
   - Navigate the level.
   - Avoid guard and camera detection.
   - Use hiding locations when needed.
   - Collect evidence.
   - Interact with terminals.
   - Monitor alert level and remaining lives.
   - Reach extraction after completing the required objectives.

5. **Pause / Help**
   - Review controls.
   - Adjust audio/settings.
   - Resume, restart, or return to the main menu.
   - Require confirmation before destructive actions such as restart or exit.

6. **Results**
   - Show whether the level was completed.
   - Show final score, evidence collected, detections, completion time, and other available results.
   - Allow replay or return to the main menu.

---

## Planned Core Screens

### 1. Main Menu

The Main Menu should keep the most important actions visible and easy to scan.

**Planned options:**
- Start Game
- Difficulty
- How to Play
- Settings
- Exit

**UX focus:**
- Simple hierarchy.
- Consistent button placement.
- Clear selected/hover states.
- No unnecessary information.

### 2. Difficulty Selection

The player chooses between Easy and Hard.

**Easy:**
- Slower guards.
- Less sensitive detection.
- More forgiving stealth experience.

**Hard:**
- Faster guards.
- More sensitive detection.
- Greater stealth challenge.

**UX focus:**
- Explain the difference before the player chooses.
- Avoid making the player guess what “Easy” and “Hard” change.

### 3. Mission Briefing / How to Play

This screen should introduce only the information needed to begin playing.

**Planned information:**
- Main objective.
- Movement controls.
- Interact control.
- Sprint control.
- Basic explanation of guards, cameras, hiding, evidence, and extraction.

**UX focus:**
- Keep instructions short.
- Use icons plus text where useful.
- Avoid a long wall of instructions.

### 4. Gameplay HUD

The HUD should remain visible without blocking the play area.

**Planned HUD information:**
- SCORE
- LIVES
- ALERT %
- EVIDENCE X/Y

**Planned color meanings:**
- Green — safe areas / completed objectives / extraction
- Yellow — warning / increasing suspicion
- Red — danger / detection / health or life-related warning
- Blue — evidence / terminals / mission objectives

**UX focus:**
- High contrast and readable text.
- Consistent location of each HUD element.
- Important changes should receive immediate visual feedback.
- Color should be supported by labels/icons rather than being the only indicator.

### 5. Pause / Help Menu

**Planned options:**
- Resume
- Controls
- Audio / Settings
- Restart
- Return to Main Menu

**UX focus:**
- Keep controls visible for players who forget them.
- Add confirmation before Restart or Return to Main Menu.
- Make Resume the easiest option to identify.

### 6. Results Screen

**Planned information:**
- Level result
- Final score
- Evidence collected
- Detection count
- Completion time
- Replay
- Return to Main Menu

**UX focus:**
- Make the outcome immediately clear.
- Keep performance information easy to scan.
- Avoid overloading the player with unnecessary statistics.

---

## Control Mapping

| Action | Planned Control | UX Reason |
| --- | --- | --- |
| Move | WASD / Arrow Keys | Familiar movement options for keyboard players |
| Interact | E | Simple single-key interaction for evidence and terminals |
| Sprint | Shift | Common movement modifier |
| Restart | R | Easy shortcut, but confirmation is required |
| Pause | ESC | Familiar pause/menu control |
| Menu Selection | Mouse | Simple menu navigation |

WASD and Arrow Keys can be compared during usability testing to determine whether either option creates confusion or control problems.

---

## Interaction States and Feedback

### Detection / Alert

The player should be able to understand the current detection state without guessing.

**Planned states:**
- Safe
- Warning / Suspicion
- Detected
- Recovering / Alert decreasing
- Safe again

The Alert indicator should provide clear visual feedback when the player moves between these states.

### Evidence

**Planned states:**
- Evidence available
- Player close enough to interact
- Evidence collected
- Required evidence complete

The evidence counter should update immediately after collection.

### Terminals

**Planned states:**
- Terminal available
- Player in interaction range
- Terminal activated
- Result of activation shown to player

### Extraction

**Planned states:**
- Locked / unavailable
- Objectives complete
- Extraction available
- Level completed

The game should clearly communicate why extraction is unavailable before the objectives are complete.

### Hiding

The player should receive feedback when entering or leaving a valid hiding location so that the player knows whether the hiding mechanic is active.

---

## UX Design Principles

The UX work will follow the design principles already established for the project:

### Simplicity
Show only the information and controls needed at the current moment.

### Consistency
Use consistent colors, labels, icons, button behavior, and control patterns across screens.

### Visibility
Keep important game information such as lives, alert level, and evidence progress visible.

### Feedback
Respond clearly to collection, detection, hiding, terminal activation, extraction, and other important actions.

### Learnability
Introduce controls and stealth mechanics in a short and understandable way.

### Accessibility
Use readable text, high contrast, text labels with icons, and more than one movement-key option.

### Error Prevention
Ask for confirmation before actions such as restarting or leaving the current game.

### Progressive Difficulty
Make Easy and Hard noticeably different without making Hard unnecessarily frustrating.

---

## Usability Testing Plan

Usability testing will begin after a playable Version 1 build is available.

### Target Participants

The project targets college students and young adults, including players with different levels of gaming experience.

### Planned Test Tasks

Participants may be asked to:

1. Start the game and choose a difficulty.
2. Identify the main mission objective.
3. Move through the environment.
4. Avoid or recover from detection.
5. Use a hiding location.
6. Collect evidence.
7. Interact with a terminal.
8. Determine when extraction becomes available.
9. Pause the game and locate the controls.
10. Complete or attempt to complete the level.

### UX Observation Questions

During testing, the UX review will look for:

- Did the player understand the controls without repeated help?
- Did the player understand the mission objective?
- Did the player notice the Alert indicator?
- Did the player understand when they were detected?
- Did the player recognize guard and camera hazards?
- Did the player recognize evidence and terminals?
- Did the player understand when hiding was active?
- Did the player know why extraction was locked or unlocked?
- Did the player accidentally restart or exit?
- Which interface elements caused hesitation or confusion?
- Did Easy and Hard feel meaningfully different?
- Did any interface element block or distract from gameplay?

---

## Coordination With Gameplay Metrics

UX observations will be reviewed alongside the gameplay metrics collected by the Data Scientist / Game Analyst.

Examples:

| Gameplay Result | UX Question |
| --- | --- |
| High detection count | Are players missing the vision cone, alert feedback, or stealth instructions? |
| High lives lost | Are players misunderstanding danger states or recovery options? |
| Evidence frequently missed | Are evidence items or objective indicators visually clear enough? |
| High restart count | Is the level frustrating, or are players accidentally using Restart? |
| Low terminal use | Do players understand what terminals are and when they can interact? |
| Long completion time | Are players lost, confused about objectives, or simply playing cautiously? |
| Low hiding count | Is the hiding mechanic unclear or unnecessary to players? |

The metrics will help identify where to look, while usability observations will help explain why the problem may be happening.

---

## Version 1 to Version 2 UX Improvement Process

For each meaningful UX issue found during testing, the following format will be used:

**Problem:**  
What confused the player or made an action difficult?

**Evidence:**  
What was observed during testing, and what gameplay data supports it?

**Possible Solutions:**  
What reasonable design options could address the problem?

**Decision:**  
Which change was selected and why?

**Version 2 Change:**  
What was changed in the interface, controls, or feedback?

**Result:**  
If retested, did the change improve the experience?

### Example

**Problem:** Players do not notice that the Alert level is increasing.  
**Evidence:** Multiple players continue moving normally after detection and do not react to the Alert indicator.  
**Possible Solutions:** Larger percentage text, stronger color change, warning icon, or a short visual/audio warning.  
**Decision:** Add a stronger warning state while keeping the HUD simple.  
**Version 2 Change:** Increase emphasis when Alert reaches the warning/danger range.  
**Result:** To be completed after retesting.

---

## Planned UX Deliverables

The UX Designer will maintain the following project artifacts:

- Player journey / interaction flow.
- Core screen wireframes and GUI prototypes.
- HUD layout.
- Control mapping.
- UI-related visual assets such as HUD icons, menu elements, and interface indicators as needed.
- Usability test plan.
- Usability observation notes.
- Version 1 to Version 2 UX change log.
- Before-and-after screenshots for major UX changes.
- Final UX findings for project documentation.

Wireframes and interface assets should be stored in `/design`. Written UX plans, test findings, and final UX documentation should be stored in `/documentation`.

---

## Current Status

- Player journey — Planned
- Core screen wireframes — Planned
- HUD design — Planned
- Control mapping — Initial plan defined
- Usability test plan — Initial plan defined
- Usability testing — Waiting for playable Version 1
- UX observations — Waiting for testing
- Version 1 to Version 2 changes — Waiting for testing
- Final UX findings — Waiting for testing
