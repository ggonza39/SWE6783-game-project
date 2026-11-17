# SWE6783-game-project: Pixel Rebellion — Shadow Protocol

## Two-level campaign candidate — November 17, 2026

Open `src` with **Unity 6000.2.2f1**, then open
`Assets/Scenes/UXFacilityExpanded.unity` and press Play. Choose **Easy Mode** or
**Hard Mode**. **New Game** starts at Level 1; **Continue Level 1/2** explicitly
resumes the highlighted card and checkpoint. New Game asks before clearing an existing
campaign. Existing Level 1 clears unlock Level 2 when their saved records migrate.

- **Level 1 / Facility Breach:** keycard, three caches, security hack and extraction.
  The approved stronger Hard challenge and required exit door are retained.
- **Level 2 / Crimson Depths:** a 55×33 dark red industrial complex with Docks,
  Archives and Reactor Vault; 12 guards, 12 cameras, nine caches and three local
  camera consoles. Twenty-four concealed proximity mines and six cycling laser barriers add
  timing hazards; sector hacks neutralize their local traps. Only one hiding bay
  remains in each sector; other containers are solid cover. The checked
  clear-floor grid is 4.13× Level 1's size.
- **Campaign:** Level 1 extraction automatically enters Level 2. Menu Continue
  and Replay after death restore
  the latest secured sector/checkpoint with earlier level clears intact. Both
  modes retain separate progress and best mission times.
- **Sprint:** a non-refilling 10-second reserve in Level 1 and 20-second reserve in Level 2, equal in Easy and Hard.
  Death replays restore full health and stamina at the secured checkpoint; empty reserves leave crouched movement during an attempt.
- **Controls:** WASD/arrows move crouched; C/CTRL extra slow; SHIFT sprint; hold E
  to download/hack; hold TAB for the map; ESC pause; R opens Restart Level.
- **Restart:** Restart Level preserves earlier clears and records. The main-menu
  Restart Game confirmation erases both modes' campaign progress and records,
  while retaining display/control settings and the anonymous player identifier.
- **Status:** isolated campaign, persistence, route, UI and regression checks are
  supplied. Unity import, engine playtesting, balance and builds are the next
  acceptance step; this source candidate is not a published build.

See [Crimson campaign and acceptance](docs/Crimson_Campaign.md) for checkpoints,
migration, telemetry, validation and Unity playtest steps. The [Level 1 notes](docs/Level1_Difficulty_And_Saving.md)
and [demo handoff](docs/UX_EXPANDED_LEVEL1_DEMO.md) retain the earlier review context.


The latest impact polish makes active lasers lethal in both modes, adds explicit
mine/laser/guard death feedback, a recorded death replay and progressive green/yellow/red sprint colors.
Hard mines now leave 15 health from full with a 1.10s fuse. Hard extraction gains
industrial machinery detail and an animated lift signal; see the campaign guide.

## Team Name
- Pixel Rebellion

## Team Roster & Roles
- **Team Lead & Requirements Engineer**
  - Gilberto Gonzalez (Project coordination, requirements management, meeting scheduling, milestone tracking, final submission preparation)
- **Game Developer**
  - Elvin Chi (Unity architecture, C# game logic, player controls, collisions, scoring, alert system, automated testing support, game builds)
- **UX Designer**
  - Victor Nnadi (User journeys, GUI prototypes, control mappings, visual assets, usability testing plans)
- **Data Scientist & Game Analyst**
  - Jazmine Harvey (Gameplay metrics definition, user-testing analysis, performance-based difficulty tuning)

## Project Overview
*Pixel Rebellion: Shadow Protocol* is a top-down 2D stealth and action game built using the **Unity** engine. Set in a futuristic city controlled by an automated security force, players take on the role of a rebel seeking to expose a government surveillance program by retrieving digital evidence and reaching an extraction point undetected.

Rather than relying on overly complex mechanics, the project focuses on user interaction engineering principles. It explores intuitive control schemes, persistent status feedback, visual hierarchy, and progressive difficulty balancing to deliver an accessible yet engaging player experience.

### Key Game Elements
- **Genre & Perspective:** Top-down 2D Stealth / Action
- **Core Gameplay Mechanics:** Player movement, guard vision cones, camera detection areas, hiding spots, evidence collection, terminal interactions, and extraction zones
- **Control Interface:** Primary WASD movement (with arrow-key options evaluated during usability testing), 'E' for interaction, 'Shift' to sprint, 'ESC' to pause, and 'R' for level restart
- **Target Audience:** College students and young adults (ages 18–35), featuring both Easy and Hard modes to support varying player experience levels

## Tools & Collaboration

- **Version Control & Project Management:** GitHub  
  Hosts the Unity source code repository, project backlogs, and documentation. The team utilizes feature branches and pull requests to maintain stability on the `main` branch.

- **UI/UX & Interface Design:** Wireframing & Prototyping  
  Used to construct control flow diagrams, wireframe the graphical user interface (GUI), map control interactions, and evaluate visual hierarchy prior to in-engine implementation.

- **Game Engine & Programming:** Unity 2D (C#)  
  Serves as the primary 2D game engine, utilizing Unity UI (Canvas), 2D Physics (Rigidbodies, Colliders), C# scripting, and asset configurations.

- **Input & Control UI Mapping:** Unity Input System  
  Configured to support intuitive keyboard and mouse controls with persistent UI control reminders in the tutorial and pause menus.

- **Usability & UX Testing:** In-Engine Telemetry & User Playtesting  
  Log files and player performance metrics (completion time, detection counts, evidence gathered) are collected and analyzed across testing cycles to drive Version 2 refinements.

- **Build Pipeline & Web Hosting:** Unity WebGL & Vercel  
  Configured to target WebGL static exports deployed via Vercel for browser-accessible playtesting and submission reviews.

- **Team Communication:** Microsoft Teams & GitHub Issues  
  Utilized for weekly progress syncs, task tracking, sprint planning, and development updates.

## Repository Structure
- `/documentation` — Project proposal reports, UX research questions, and meeting notes
- `/design` — GUI mockups, wireframes, visual assets, and control layout schematics
- `/src` — Core Unity source project directory (Assets, Scenes, Prefabs, and C# Scripts)
- `/builds` — Compiled Unity WebGL exports ready for deployment
- `/tests` — Automated testing scripts, playtesting logs, and usability evaluation plans

The replay/death-effects fix corrects vertical playback orientation and separates
guard blood-pool deaths, mine explosions and laser burns. Mine blast radius is
now 2.4 units in both modes (25% smaller); fuse and damage values are retained.

Replay clarity now uses a higher-resolution20Hz color archive, camera-style frame
and recorded timestamps. A separate action deck keeps checkpoint Replay and Main
Menu visibly outside the footage. See the campaign guide for Unity acceptance.
