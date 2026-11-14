# SWE6783-game-project: Pixel Rebellion — Shadow Protocol

## Level 1 modes and saved progress — November 14, 2026

Open `src` with **Unity 6000.2.2f1**, then open
`Assets/Scenes/UXFacilityExpanded.unity` and select **Play → Easy/Hard → Start Level 1 → Deploy**.

- **Mission:** collect the gold keycard, download three evidence caches, hack the terminal, then extract.
- **Controls:** WASD/arrows move crouched; C/CTRL extra slow; SHIFT sprint; hold E to download/hack; hold TAB for the blueprint; ESC pause; R restart confirmation.
- **Security:** marked cover conceals the player. Full detection starts pursuit; breaking sight and hiding lets search clear. Health does not regenerate.
- **Difficulty:** Easy has slower patrols and more forgiving detection. Hard has faster patrol/pursuit, wider detection, three-hit failure, five-second data downloads, eight-second hacks, and one required eight-second exit door; the alternative entrance is sealed.
- **Saved progress:** selected mode, display/control-reminder settings, completed runs, and best successful time per mode are stored on the current device. Retry starts a fresh mission and retains completed-run records.
- **Status:** the supplied Windows recording completes Easy and the first Hard preset. The stronger Hard revision has isolated route/rule checks and needs a fresh Unity balance playtest before committing or rebuilding.

See [Level 1 difficulty and saving](docs/Level1_Difficulty_And_Saving.md) for mode
timings, the required Hard exit door, saved-data behavior, and the acceptance pass.

See [the demo handoff and recording plan](docs/UX_EXPANDED_LEVEL1_DEMO.md) for the
verified behavior, remaining checks and team review responsibilities.

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
