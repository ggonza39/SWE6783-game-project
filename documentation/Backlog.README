 # Pixel Rebellion: Shadow Protocol — Development Backlog & User Stories

This document outlines the user stories and technical task breakdowns in logical build order for *Pixel Rebellion: Shadow Protocol*. Tasks are structured sequentially across five major implementation milestones.

---

## Milestone 1: Project Setup & Core Player Movement (Weeks 1–2)

### User Story 1.1: Core Unity Environment Setup
> *As a developer, I want a clean, structured Unity project configuration so that the team can build components independently without merge conflicts.*

* **Task 1.1.1:** Initialize the Unity 2D project with Universal Render Pipeline (URP) inside the `/src` directory[cite: 1, 3].
* **Task 1.1.2:** Configure `vercel.json` and WebGL build settings to support proper MIME headers for WebGL asset loading.
* **Task 1.1.3:** Establish the repository directory structure (`/documentation`, `/design`, `/src`, `/builds`, `/tests`) and set up feature-branch workflows[cite: 1, 3].

### User Story 1.2: Basic Player Movement & Physics
> *As a player, I want to move my rebel character smoothly using WASD or arrow keys so that I can navigate the game environment.*

* **Task 1.2.1:** Create the `Player` GameObject with a 2D Rigidbody, Box Collider 2D, and top-down sprite[cite: 1, 2, 3].
* **Task 1.2.2:** Implement a `PlayerController.cs` script supporting 8-directional movement mapped to WASD and Arrow Keys[cite: 1, 3].
* **Task 1.2.3:** Add sprint functionality bound to `Shift` (increasing movement speed)[cite: 1, 3].
* **Task 1.2.4:** Build wall collision bounds to prevent the player from moving out-of-bounds or through obstacles[cite: 1, 2].

---

## Milestone 2: Security Forces & Detection Systems (Weeks 2–3)

### User Story 2.1: Guard Patrol Routes & Detection Cones
> *As a player, I want to see guard vision cones and patrol paths so that I can time my movements to avoid detection.*

* **Task 2.1.1:** Create `Guard.cs` enemy prefabs that move along predefined waypoint arrays (Patrol state)[cite: 1, 2, 3].
* **Task 2.1.2:** Implement a visual vision cone using a 2D Mesh or Polygon Collider to represent the field-of-view[cite: 1, 2].
* **Task 2.1.3:** Create raycasting checks to detect if the player enters the vision cone without line-of-sight blockage[cite: 1, 2].

### User Story 2.2: Fixed Camera Detection & Hiding Mechanics
> *As a player, I want fixed security cameras and hiding spots so that I have environmental hazards to evade and safe zones to recover.*

* **Task 2.2.1:** Develop `SecurityCamera.cs` with a sweeping cone angle that rotates over a fixed interval[cite: 1, 2, 3].
* **Task 2.2.2:** Create `HidingSpot.cs` objects (e.g., alcoves or cover) that temporarily suppress detection while occupied[cite: 1, 2, 3].
* **Task 2.2.3:** Build an `AlertManager.cs` system that increases the global Alert Level percentage when detected and slowly decays it when hidden[cite: 1, 2, 3].

---

## Milestone 3: Mission Objectives & Level Logic (Weeks 3–4)

### User Story 3.1: Evidence Collection & Terminal Interaction
> *As a player, I want to gather evidence items and hack security terminals so that I can progress toward completing the mission.*

* **Task 3.1.1:** Create `Evidence.cs` collectibles with trigger colliders; pressing `E` near evidence increments the evidence counter and score[cite: 1, 2, 3].
* **Task 3.1.2:** Implement `Terminal.cs` objects that disable specific cameras or open locked pathways upon pressing `E`[cite: 1, 2, 3].
* **Task 3.1.3:** Add pickup feedback for evidence collection and terminal activation[cite: 1, 2, 3].

### User Story 3.2: Extraction Zone & Win/Loss Conditions
> *As a player, I want a functional extraction point and clear win/loss rules so that I know when I have cleared or failed the level.*

* **Task 3.2.1:** Implement `ExtractionZone.cs` that remains inactive until all required evidence items are collected[cite: 1, 2, 3].
* **Task 3.2.2:** Trigger Level Victory when the player enters the activated extraction zone[cite: 1, 2, 3].
* **Task 3.2.3:** Trigger Game Over if Alert Level reaches 100% or player lives reach zero[cite: 1, 2, 3].

---

## Milestone 4: User Interface, Audio & Controls (Weeks 4–5)

### User Story 4.1: Persistent In-Game HUD (GUI)
> *As a player, I want a persistent status bar on my screen so that I can monitor my score, lives, alert level, and evidence count in real time.*

* **Task 4.1.1:** Construct the top Unity Canvas HUD with a pixel-art style and high-contrast text labels[cite: 1, 2, 3].
* **Task 4.1.2:** Bind `AlertManager` and player stats to HUD UI elements (**SCORE**, **LIVES**, **ALERT %**, **EVIDENCE X/Y**)[cite: 1, 2, 3].
* **Task 4.1.3:** Apply the color palette: Green (Safe/Exit), Yellow (Warning), Red (Danger/Alert), Blue (Evidence)[cite: 1, 2, 3].

### User Story 4.2: Menus & Error Prevention
> *As a player, I want clear start, pause, and menu screens so that I can manage my game session without accidental loss of progress.*

* **Task 4.2.1:** Create Main Menu UI with **Start Game**, **Difficulty**, **How to Play**, **Settings**, and **Exit** options[cite: 1, 2, 3].
* **Task 4.2.2:** Build a Pause Menu bound to `ESC` displaying control mappings and audio controls[cite: 1, 2, 3].
* **Task 4.2.3:** Add confirmation pop-ups before restarting (`R`) or exiting to the main menu to prevent accidental progress loss[cite: 1, 2, 3].

---

## Milestone 5: Difficulty Modes, Telemetry & Polish (Weeks 5–6)

### User Story 5.1: Difficulty Scaling & Usability Polish
> *As a player, I want Easy and Hard difficulty modes so that I can choose a challenge level suited to my experience.*

* **Task 5.1.1:** Implement Easy Mode with slower guard patrols, narrower vision cones, and slower alert growth[cite: 1, 2, 3].
* **Task 5.1.2:** Implement Hard Mode with faster guard patrols, wider sweeping camera cones, and increased detection sensitivity[cite: 1, 2, 3].
* **Task 5.1.3:** Add interaction logging (completion time, detection counts, evidence metrics) to evaluate usability test results[cite: 1, 2, 3].

### User Story 5.2: Final WebGL Build & Deployment
> *As a team lead, I want a fully tested WebGL build hosted on Vercel so that evaluators can test the game online.*

* **Task 5.2.1:** Conduct usability playtesting passes to refine mechanics and resolve collision or UI issues[cite: 1, 2, 3].
* **Task 5.2.2:** Export the compiled WebGL project to `/builds` and deploy static assets to Vercel production[cite: 1, 3].
* **Task 5.2.3:** Finalize all documentation, screenshots, and UX research findings in `/documentation`[cite: 1, 2, 3].
