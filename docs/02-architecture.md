# Architecture

## Goal

Keep a **hard boundary** between:

| Layer | Responsibility | Technology |
|-------|----------------|------------|
| **Simulation** | World state, rules, economy, movement, time | Pure C# (no Godot API) |
| **Interface** | Rendering, input, audio, scenes, HUD | Godot 4 + C# scripts |
| **Content** | Map packs, city tables, vehicle definitions | Data files (JSON/YAML/Godot resources) loaded by either side via adapters |

The simulation must be runnable **headless**: unit tests, CLI tools, and future server/AI experiments without opening the Godot editor.

## Dependency rule

```text
┌─────────────────────────────────────────┐
│  Interface (Godot)                      │
│  - scenes, UI, camera, map draw         │
│  - maps sim state → visuals             │
│  - maps player input → Commands         │
└──────────────────┬──────────────────────┘
                   │ depends on
                   ▼
┌─────────────────────────────────────────┐
│  Simulation (pure C#)                   │
│  - GameState, systems, commands         │
│  - no GodotEngine / Node references     │
└──────────────────┬──────────────────────┘
                   │ depends on
                   ▼
┌─────────────────────────────────────────┐
│  Shared contracts / content DTOs        │
│  - IDs, enums, serializable records     │
└─────────────────────────────────────────┘
```

**Forbidden:**

- Simulation projects referencing `GodotSharp` / `Godot.dll`.
- UI writing directly into simulation fields (must go through commands).
- Game rules implemented only in `_Process` / GDScript without a sim counterpart.

**Allowed:**

- Interface reading **immutable snapshots** or query APIs.
- Interface holding Godot-only presentation state (camera zoom, selected tab, animation lerp).
- Content loaders in interface or a thin bootstrap that feeds DTOs into simulation initialization.

## Command / query pattern

All player (and AI) intent enters the simulation as **commands**. All HUD data leaves as **queries** or **snapshots**.

```text
Player clicks “Create Route”
        → Interface builds CreateRouteCommand
        → Simulation.Apply(command) → Result (Ok | Error)
        → Interface refreshes from QueryRoutes / Snapshot
```

Commands are serializable. That enables:

- Undo/replay (**Later**)
- Multiplayer lockstep or server authority (**Later**)
- Deterministic tests (“apply these commands, assert cash”)

## Time model

- Simulation advances in discrete **ticks** (or fixed Δt steps).
- Interface may interpolate vehicle icons between sim positions for smoothness.
- Pause / 1x / 2x / 4x are interface requests that change how often the interface calls `Simulation.Tick()`.

See [Simulation](05-simulation.md) for tick contents.

## Suggested repository layout

```text
/
├── README.md
├── docs/                          # this documentation
├── src/
│   ├── TravelEmpire.Simulation/   # class library, pure C#
│   ├── TravelEmpire.Content/      # optional: shared content loaders/DTOs
│   └── TravelEmpire.Godot/        # Godot project (csproj + project.godot)
├── tests/
│   └── TravelEmpire.Simulation.Tests/
└── tools/                         # map importers, balance sheets, etc.
```

Exact folder names may adjust when scaffolding; the **Simulation vs Godot project split** must remain.

## Godot project responsibilities

- Host the main loop: each frame or fixed step, decide whether to tick simulation.
- Own `Node` tree: map viewport, panels, dialogs.
- Subscribe to simulation events (route created, vehicle arrived, bankruptcy warning).
- Never duplicate authoritative money or passenger counts.

## Simulation project responsibilities

- Own `GameState` (cities, company, fleet, routes, clock, RNG seed).
- Validate and apply commands.
- Produce events and query results.
- Persist/load game state (format owned by simulation or shared serializer).

## Determinism

**MVP should:**

- Use a seeded RNG inside the simulation for any stochastic demand/events.
- Avoid wall-clock time inside sim rules (use sim clock only).

**MVP may** tolerate floating-point drift for positions if tests use tolerances. Prefer integer meters / fixed-point for money and distances where practical.

## Testing pyramid (architecture view)

| Level | What | Where |
|-------|------|--------|
| Unit | Fare calculation, travel time, command validation | Simulation.Tests |
| Integration | Multi-tick route loop with passengers | Simulation.Tests |
| Manual / play | Map UX, route editor usability | Godot editor / builds |
| Later | Headless scenario fixtures, golden saves | tools + CI |

## Anti-patterns to avoid

1. **Godot Resource as sole source of truth** for money/fleet — resources are content or presentation, not live economy.
2. **Static global `Game` in UI** mutated from buttons without commands.
3. **Copy-pasted formulas** in UI tooltips that disagree with sim (UI must ask sim or share pure helpers in Simulation/Content).
4. **Tick logic in `_Draw`** — drawing must not advance the world.
