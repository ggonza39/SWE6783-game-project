# Pixel Rebellion: Shadow Protocol — Development Backlog & User Stories

This document outlines the user stories and technical task breakdowns in logical build order for *Pixel Rebellion: Shadow Protocol*. Tasks are structured sequentially across five major implementation milestones.

---

## Milestone 1: Project Setup & Core Player Movement (Weeks 1–2)

### User Story 1.1: Core Unity Environment Setup - Story Points 3
> *As a developer, I want a clean, structured Unity project configuration so that the team can build components independently without merge conflicts.*

* **Task 1.1.1:** Initialize the Unity 2D project with Universal Render Pipeline (URP) inside the `/src` directory.
* **Task 1.1.2:** Configure `vercel.json` and WebGL build settings to support proper MIME headers for WebGL asset loading.
* **Task 1.1.3:** Establish the repository directory structure (`/documentation`, `/design`, `/src`, `/builds`, `/tests`) and set up feature-branch workflows.

### User Story 1.2: Basic Player Movement & Physics - Story Points 3
> *As a player, I want to move my rebel character smoothly using WASD or arrow keys so that I can navigate the game environment.*

* **Task 1.2.1:** Create the `Player` GameObject with a 2D Rigidbody, Box Collider 2D, and top-down sprite.
* **Task 1.2.2:** Implement a `PlayerController.cs` script supporting 8-directional movement mapped to WASD and Arrow Keys.
* **Task 1.2.3:** Add sprint functionality bound to `Shift` (increasing movement speed).
* **Task 1.2.4:** Build wall collision bounds to prevent the player from moving out-of-bounds or through obstacles.

---

## Milestone 2: Security Forces & Detection Systems (Weeks 2–3)

### User Story 2.1: Guard Patrol Routes & Detection Cones - Story Points 5
> *As a player, I want to see guard vision cones and patrol paths so that I can time my movements to avoid detection.*

* **Task 2.1.1:** Create `Guard.cs` enemy prefabs that move along predefined waypoint arrays (Patrol state).
* **Task 2.1.2:** Implement a visual vision cone using a 2D Mesh or Polygon Collider to represent the field-of-view.
* **Task 2.1.3:** Create raycasting checks to detect if the player enters the vision cone without line-of-sight blockage.

### User Story 2.2: Fixed Camera Detection & Hiding Mechanics - Story Points 5
> *As a player, I want fixed security cameras and hiding spots so that I have environmental hazards to evade and safe zones to recover.*

* **Task 2.2.1:** Develop `SecurityCamera.cs` with a sweeping cone angle that rotates over a fixed interval.
* **Task 2.2.2:** Create `HidingSpot.cs` objects (e.g., alcoves or cover) that temporarily suppress detection while occupied.
* **Task 2.2.3:** Build an `AlertManager.cs` system that increases the global Alert Level percentage when detected and slowly decays it when hidden.

---

## Milestone 3: Mission Objectives & Level Logic (Weeks 3–4)

### User Story 3.1: Evidence Collection & Terminal Interaction - Story Points 3
> *As a player, I want to gather evidence items and hack security terminals so that I can progress toward completing the mission.*

* **Task 3.1.1:** Create `Evidence.cs` collectibles with trigger colliders; pressing `E` near evidence increments the evidence counter and score.
* **Task 3.1.2:** Implement `Terminal.cs` objects that disable specific cameras or open locked pathways upon pressing `E`.
* **Task 3.1.3:** Add pickup feedback for evidence collection and terminal activation.

### User Story 3.2: Extraction Zone & Win/Loss Conditions - Story Points 3
> *As a player, I want a functional extraction point and clear win/loss rules so that I know when I have cleared or failed the level.*

* **Task 3.2.1:** Implement `ExtractionZone.cs` that remains inactive until all required evidence items are collected.
* **Task 3.2.2:** Trigger Level Victory when the player enters the activated extraction zone.
* **Task 3.2.3:** Trigger Game Over if Alert Level reaches 100% or player lives reach zero.

---

## Milestone 4: User Interface, Audio & Controls (Weeks 4–5)

### User Story 4.1: Persistent In-Game HUD (GUI) - Story Points 3
> *As a player, I want a persistent status bar on my screen so that I can monitor my score, lives, alert level, and evidence count in real time.*

* **Task 4.1.1:** Construct the top Unity Canvas HUD with a pixel-art style and high-contrast text labels.
* **Task 4.1.2:** Bind `AlertManager` and player stats to HUD UI elements (**SCORE**, **LIVES**, **ALERT %**, **EVIDENCE X/Y**).
* **Task 4.1.3:** Apply the color palette: Green (Safe/Exit), Yellow (Warning), Red (Danger/Alert), Blue (Evidence).

### User Story 4.2: Menus & Error Prevention - Story Points 3
> *As a player, I want clear start, pause, and menu screens so that I can manage my game session without accidental loss of progress.*

* **Task 4.2.1:** Create Main Menu UI with **Start Game**, **Difficulty**, **How to Play**, **Settings**, and **Exit** options.
* **Task 4.2.2:** Build a Pause Menu bound to `ESC` displaying control mappings and audio controls.
* **Task 4.2.3:** Add confirmation pop-ups before restarting (`R`) or exiting to the main menu to prevent accidental progress loss.

---

## Milestone 5: Difficulty Modes, Telemetry & Polish (Weeks 5–6)

### User Story 5.1: Difficulty Scaling & Usability Polish - Story Points 8
> *As a player, I want clear environmental cues, understandable stealth mechanics, and difficulty options so that I can navigate the level, recognize hazards and objectives, and choose a challenge suited to my experience.*

* **Task 5.1.1:** Implement Easy Mode with slower guard patrols, narrower vision cones, and slower alert growth.
* **Task 5.1.2:** Implement Hard Mode with faster guard patrols, wider sweeping camera cones, and increased detection sensitivity.
* **Task 5.1.3:** Add interaction logging (completion time, detection counts, evidence metrics) to evaluate usability test results.

**Revision Tasks:**

* **Task 5.1.4:** Player Sprite Visual Cleanup

Remove the black rectangular background surrounding the player sprite and ensure proper transparency. Verify that the character displays correctly during movement and does not obscure nearby environmental objects.

**Evaluation finding:** Students reported that the dark background interfered with environmental visibility.

* **Task 5.1.5:** Level Layout & Navigation Refinement

Refine the existing facility layout with appropriate walls, corridors, corners, and pathways to improve navigation and create meaningful stealth opportunities. Prioritize targeted adjustments to the current level rather than a complete redesign.

**Evaluation finding:** Some participants considered the level too short or insufficiently challenging.

* **Task 5.1.6:** Guard & Camera Detection Clarity

Verify that guard and security-camera detection behavior matches the visible direction and field-of-view indicators. Improve detection cones and visual warning cues so players can recognize hazardous areas and understand when detection occurs.

**Evaluation finding:** Participants reported unclear detection ranges, and telemetry showed a strong relationship between recorded detections and unsuccessful sessions.

* **Task 5.1.7:** Navigation, Objective Cues & Progress

Improve environmental navigation cues and objective indicators so players can identify evidence, terminals, hiding locations, and extraction points. Provide clear contextual guidance and objective-progress information without interrupting gameplay.

**Evaluation finding:** Participants sometimes understood the overall mission but struggled to identify specific objectives or determine their next action.

* **Task 5.1.8:** Stealth Environment Placement

Review the placement of guards, cameras, hiding locations, evidence, terminals, and extraction points to encourage intentional stealth decisions. Make targeted adjustments to the existing level where necessary to improve challenge and reduce unintended shortcuts.

**Evaluation finding:** Some participants completed the mission quickly without needing to use the available stealth mechanics.

* **Task 5.1.9:** Interactive Object Visibility & Feedback

Apply consistent colors, icons, outlines, or visual indicators to evidence, terminals, hiding locations, and extraction points. Provide clear interaction prompts and confirmation when evidence is collected, terminals are activated, or objectives are completed.

**Evaluation finding:** Students reported difficulty recognizing interactive objects and confirming successful interactions.

* **Task 5.1.10:**  HUD Readability & Responsive Scaling (NEW)

Improve the readability of HUD labels, objective information, and gameplay status indicators. Adjust text size, contrast, spacing, and Unity Canvas scaling to maintain readability across common screen resolutions, including high-resolution displays.

**Evaluation finding:** Participants reported small or difficult-to-read interface text, particularly on high-resolution monitors.

* **Task 5.1.11:** Tutorial & Contextual Instruction Refinement (NEW)

Simplify the introductory instructions while preserving essential information about controls, objectives, and stealth mechanics. Use concise contextual prompts during gameplay to explain relevant actions without overwhelming or interrupting players.

**Evaluation finding:** Students generally appreciated the mission briefing but sometimes considered it text-heavy.

* **Task 5.1.12:**  Core Gameplay Verification & Regression Testing (NEW)

Validate the revised gameplay mechanics and interface through focused testing. Confirm that guard and camera detection, sprint functionality, hiding, evidence collection, terminal interactions, objective progression, extraction, and win/loss conditions behave consistently. Verify that changes do not introduce new collision, control, or interface problems.

**Evaluation finding:** Student feedback identified possible inconsistencies in detection behavior, sprint responsiveness, and mission feedback that require direct verification.


### User Story 5.2: Final WebGL Build & Deployment - Story Points 3
> *As a team lead, I want a fully tested WebGL build hosted on Vercel so that evaluators can test the game online.*

* **Task 5.2.1:** Conduct usability playtesting passes to refine mechanics and resolve collision or UI issues.
* **Task 5.2.2:** Export the compiled WebGL project to `/builds` and deploy static assets to Vercel production.
* **Task 5.2.3:** Finalize all documentation, screenshots, and UX research findings in `/documentation`.
