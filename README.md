# SWE6783-game-project

## Team Name
- UX2026

## Team Roster & Roles
- **Team Lead**
  - TBD (Project Management, Requirements Engineering & Sponsor Liaison)
- **Developer**
  - TBD (Core Web Architecture, Front-End Component Development & REST API Integration)
- **UX Designer**
  - TBD (User Journeys, Personas, Wireframes, Information Architecture & Usability Testing)
- **Data Scientist**
  - TBD (User Behavioral Analytics, Accessibility Data Evaluation & Mock Data Modeling)

## Project Overview
This project focuses on the design and implementation of a simple 2D interactive arcade game built using the **Unity** engine. Rather than focusing on complex gamification mechanics or high-end graphics, the primary goal is to leverage Unity’s built-in physics engine, Asset Store components, and input mapping to explore core user interface (UI) design, player feedback loops, and ergonomic control mechanisms.

The game places players in a 2D grid-based environment where they must navigate obstacles, collect target items, and manage an interactive control interface under simple constraint rules. The primary objective is to evaluate how clean GUI design and intuitive control UI can make basic gameplay responsive, accessible, and enjoyable—even for simple or straightforward game concepts.


## Tools & Collaboration

The project uses the following tools to support design, development, accessibility testing, and project management:

- **Version Control & Project Management:** GitHub  
  GitHub is used to host the source code repository, manage the product backlog, track issues, and document project artifacts. GitHub Issues and Projects serve as the foundation for user stories, tasks, and milestone deliverables (Milestones 1–3), maintaining clear alignment across the team.

- **UI/UX & Information Architecture Design:** Figma & PlantUML  
  Figma and PlantUML are used to build user personas, design site navigation layouts, and map key visitor journeys for "I Need Help" and "I Want to Help" audience pathways prior to major implementation.

- **Frontend Development:** Next.js (React + TypeScript) / Modern Web Framework  
  A modern, responsive front-end stack prioritizing low operational cost, rapid accessibility implementation, accessible UI components, and straightforward REST API integration.

- **Backend & API Infrastructure:** REST API & Mock API Endpoints  
  The website communicates via RESTful API integration. Postman is used to collaborate with Team 30 on the shared API contract. A mocked backend endpoint layer ensures the website team can build, validate, and test form submission workflows independently of the backend delivery timeline.

- **Accessibility & Usability Testing:** WAVE, axe DevTools, Screen Readers (NVDA/VoiceOver)  
  Comprehensive accessibility tooling to enforce WCAG standards, keyboard navigation, proper semantic structure, readable typography, high color contrast, and alt-text implementations for older adult users.

- **CI/CD Pipeline & Hosting:** GitHub Actions & Vercel  
  Automated testing and continuous integration via GitHub Actions enforce code quality standards before merging to `main`. Continuous deployment is powered by Vercel for preview and production builds.

- **Team Communication:** Microsoft Teams  
  Used for weekly meetings, sponsor check-ins, asynchronous team coordination, and development updates.

## Repository Structure
- `/documentation` — Requirements engineering document, API contract specs, and future-development roadmap
- `/design` — Wireframes, user journeys, information architecture maps, and persona assets
- `/src` — Core website source code (frontend layouts, components, admin interface)
- `/mocks` — Mock API server scripts and dummy data response schemas
- `/tests` — Usability testing logs and web accessibility test suites
