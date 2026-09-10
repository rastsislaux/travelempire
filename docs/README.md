# Documentation index

TravelEmpire design documentation. Read in order for onboarding; jump by topic when implementing.

## Reading order

1. [Vision & scope](01-vision-and-scope.md) — what the game is and is not
2. [Architecture](02-architecture.md) — simulation / interface split and repo layout
3. [Game mechanics](03-game-mechanics.md) — player-facing systems
4. [Data model](04-data-model.md) — entities and relationships
5. [Simulation](05-simulation.md) — headless core, ticks, commands
6. [Interface](06-interface.md) — Godot presentation layer
7. [Tooling & framework](07-tooling-and-framework.md) — Godot, C#, tests, tooling
8. [MVP roadmap](08-mvp-roadmap.md) — first country, cities, routes
9. [Next roadmap](09-next-roadmap.md) — UI polish, catalog progression, real countries
10. [UI execution plan](10-ui-execution-plan.md) — buildable plan for the main operations screen (Track 1 detail)

Visual references: [`references/`](references/).

## Conventions

- **Must** / **should** / **may** follow RFC 2119 intent.
- Simulation types are described as pure C# concepts; Godot types appear only in interface docs.
- MVP content is marked **MVP**; later content is marked **Later**.
- Currency is abstract company cash (`Money`); no real-world currency branding required.
