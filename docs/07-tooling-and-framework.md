# Tooling & framework

## Engine

| Choice | Detail |
|--------|--------|
| Engine | **Godot 4.7** .NET (`Godot.NET.Sdk/4.7.2`, `project.godot` features `4.7`) |
| Language | **C#** (.NET) for simulation and Godot scripts |
| GDScript | Avoid for game rules; optional for throwaway editor glue only |

Rationale: Godot fits 2D map + UI tooling; C# enables a testable headless simulation and familiar tooling for larger domains.

## .NET / C#

| Item | Guidance |
|------|----------|
| SDK | .NET **8+** (10.x supported). `global.json` pins a minimum of 8.0.100 with `rollForward: latestMajor` so newer installed SDKs are used |
| Project TFM | `net8.0` for Simulation, Tests, ConsoleHost, and Godot (Godot 4.7 minimum) |
| Solution | `TravelEmpire.sln` spanning Simulation, Godot, Tests, ConsoleHost |
| Nullable | Enable nullable reference types in simulation |
| Style | EditorConfig; prefer records for DTOs/commands |

## Project references

```text
TravelEmpire.Godot  →  TravelEmpire.Simulation
TravelEmpire.Simulation.Tests  →  TravelEmpire.Simulation
TravelEmpire.Simulation  →  (no Godot reference)
```

Optional `TravelEmpire.Content` for shared pack DTOs if we want zero Godot types even in pack parsing.

## Content formats

| Content | Format (MVP) | Loaded by |
|---------|--------------|-----------|
| Cities / rail edges | JSON in map pack folder | Sim bootstrap (via interface file read or embedded) |
| Vehicle catalog | JSON | Sim |
| Map background | Texture (Godot import) | Interface only |
| Translations | Later | Interface |

Example map pack layout:

```text
content/maps/example_country/
  pack.json          # id, name, projection hints
  cities.json
  rail_edges.json
  preview.png        # interface
```

## Testing

| Stack | Use |
|-------|-----|
| `dotnet test` | xUnit or NUnit — pick one and standardize |
| FluentAssertions (optional) | Readable asserts |
| Godot integration tests | Later; not blocking MVP sim |

**Must-have early tests:**

- Distance / travel time calculation
- Buy vehicle cash deduction
- Create route validation (rail without edge fails)
- Assign vehicle mode mismatch fails
- Multi-tick shuttle moves and generates revenue with stub demand

## IDE & editor

- Godot Editor for scenes, nodes, import.
- Rider / VS / VS Code + C# Dev Kit for simulation and tests.
- Keep scene files and C# in sync; avoid duplicating node paths as magic strings (export NodePath / unique names).

## Source control

- Commit `project.godot`, `.csproj`, content JSON, docs.
- Ignore `.godot/`, build outputs, user-specific editor files per Godot/.NET defaults.
- `.gitattributes` for line endings on scripts.

## CI (when code exists)

Pipeline sketch:

1. Restore / build solution.
2. Run unit tests.
3. (Optional) Headless Godot script smoke — Later.
4. Artifact: export templates Later.

## Debugging tools

- Simulation debug commands (grant cash, set speed).
- Overlay: show OD demand on selected city pair.
- Logging: sim events to Godot `Log` / standard `ILogger` abstraction in sim (no Godot logger types inside sim).

## Balance tooling (**Later**)

- Spreadsheet or small CLI to recompute demand matrix and expected revenue for a route template.
- Golden scenario JSON: commands + expected cash after N ticks.

## Documentation tooling

- Markdown in `/docs` (this set).
- Update docs in the same PR as behavior changes that invalidate them.

## Non-goals for tooling MVP

- Custom Godot modules in C++.
- ECS frameworks (default OOP/systems in sim is enough until profiling says otherwise).
- Mandatory DI container — constructor injection by hand is fine initially.
