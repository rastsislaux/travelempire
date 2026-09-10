# TravelEmpire

A transport-empire management game built with **Godot** and **C#**. Build bus, rail, and air networks between cities, create routes, assign fleets, and grow a company across a geographic map.

## Status

Documentation-first. Implementation has not started. See [`docs/`](docs/README.md) for design, architecture, and tooling.

## Core principles

- **Simulation ↔ interface separation** — game rules and state live in a headless C# simulation; Godot is the presentation and input shell.
- **Geographic map** — cities with real coordinates; start with one country, expand toward a world map.
- **Multi-modal transport** — buses, trains, and airplanes sharing the same economic and routing model.

## Docs quick links

| Document | Topic |
|----------|--------|
| [Vision & scope](docs/01-vision-and-scope.md) | Fantasy, pillars, out-of-scope |
| [Architecture](docs/02-architecture.md) | Layers, boundaries, project layout |
| [Game mechanics](docs/03-game-mechanics.md) | Economy, demand, routes, vehicles |
| [Data model](docs/04-data-model.md) | Entities, IDs, persistence shapes |
| [Simulation](docs/05-simulation.md) | Tick loop, commands, queries |
| [Interface](docs/06-interface.md) | Godot UI, map, presentation |
| [Tooling & framework](docs/07-tooling-and-framework.md) | Engine, .NET, tests, CI |
| [MVP roadmap](docs/08-mvp-roadmap.md) | First playable slice |

## License

TBD.
