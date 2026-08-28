# SWE6783-game-project

## Team Name
- Pixel Rebellion

## Team Roster & Roles
- **Team Lead**
  - Gilberto Gonzalez (Project Management, Requirements Engineering)
- **Developer**
  - TBD (Core Unity Architecture, C# Game Logic & Physics Implementation)
- **UX Designer**
  - TBD (User Journeys, GUI Mockups, Control Mapping & Usability Testing)
- **Data Scientist**
  - TBD (Player Interaction Telemetry, Gameplay Analytics & Difficulty Models)

## Project Overview
This project focuses on the design and implementation of a simple 2D interactive arcade game built using the **Unity** engine. Rather than focusing on complex gamification mechanics or high-end graphics, the primary goal is to leverage Unity’s built-in physics engine, Asset Store components, and input mapping to explore core user interface (UI) design, player feedback loops, and ergonomic control mechanisms.

The game places players in a 2D grid-based environment where they must navigate obstacles, collect target items, and manage an interactive control interface under simple constraint rules. The primary objective is to evaluate how clean GUI design and intuitive control UI can make basic gameplay responsive, accessible, and enjoyable, even for simple or straightforward game concepts.

## Tools & Collaboration

The project uses the following tools to support design, development, accessibility testing, and project management:

- **Version Control & Project Management:** GitHub  
  GitHub is used to host the Unity source code repository, track issues, manage the product backlog, and document project artifacts. GitHub Issues and Projects serve as the foundation for user stories, tasks, and milestone deliverables.

- **UI/UX & Interface Design:** Figma & PlantUML  
  Figma and PlantUML are used to build control flow diagrams, wireframe the graphical user interface (GUI), map out control UI interaction states, and design player feedback loops prior to scene implementation.

- **Game Engine & Programming:** Unity 2D (C#)  
  Unity serves as the core 2D game engine, utilizing Unity UI (Canvas), 2D Physics (Rigidbodies, Colliders), C# scripting, and standard Unity Asset Store resources for rapid scene composition and prototyping.

- **Input & Control UI Mapping:** Unity Input System  
  Configured to support accessible, multi-device control options including keyboard, mouse point-and-click, and gamepad controllers to evaluate control ergonomics.

- **Usability & UX Testing:** In-Engine Telemetry & Observational Testing  
  Qualitative usability testing logs and basic player interaction telemetry are recorded during playtesting sessions to evaluate GUI clarity, task completion rates, and input friction.

- **Build Pipeline & Web Hosting:** Unity WebGL & Vercel  
  Unity is configured to target WebGL builds. Vercel is used to host the compiled static WebGL deployment for easy, browser-accessible testing and submission reviews.

- **Team Communication:** Microsoft Teams  
  Used for weekly meetings, asynchronous team coordination, sprint reviews, and development updates.

## Repository Structure
- `/documentation` — Project design proposal PDF, research questions, and architecture docs
- `/design` — Wireframes, paper prototypes, GUI mockups, and control flow diagrams
- `/src` — Core Unity project directory (Assets, Scenes, Prefabs, and C# Scripts)
- `/builds` — Compiled Unity WebGL exports ready for deployment
- `/tests` — Usability test plans, playtesting logs, and control interaction evaluations
