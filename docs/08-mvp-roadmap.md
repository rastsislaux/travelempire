# MVP roadmap

Scope for the **first playable vertical slice**. Documentation is complete before scaffolding; this file defines what “MVP” means when implementation starts.

## MVP goal

A player can start a new game on **one hand-authored country**, see **several cities** on a map, **buy vehicles**, **create routes** between cities with legal mode constraints, **assign vehicles**, run time forward, and **earn or lose money** from passenger service.

## Included

### Content

- 1 map pack, **4–8 cities** with coordinates, population, terminal flags.
- Predefined **rail edges** between a subset of cities.
- Vehicle catalog: ≥1 bus type, ≥1 train type, ≥1 aircraft type.

### Simulation

- New game, cash, buy vehicle, create route, assign/unassign vehicle.
- Tick loop with movement, boarding, revenue, operating costs.
- Demand from gravity model; consecutive-stop capture.
- Save/load **nice-to-have** for MVP; if cut, New Game only is acceptable for first internal playable.

### Interface

- Map with cities and route lines.
- Top bar: cash, time, pause/speed.
- Route editor and fleet/buy panels.
- Errors from failed commands shown to player.

### Quality bar

- Simulation unit tests for core commands and a multi-tick revenue smoke test.
- No Godot types inside Simulation project.
- Placeholder art acceptable.

## Excluded from MVP

- Building airports, stations, or tracks.
- Timetables / clock-face schedules.
- Freight.
- AI competitors.
- Multi-country / world map.
- Multiplayer.
- Fancy population growth.
- Selling vehicles, maintenance, crashes.
- Campaign scenarios.

## Implementation sequence (when coding starts)

1. Scaffold solution + Godot project; empty `Simulation` façade.
2. Cities + distance + demand matrix + snapshot queries.
3. Commands: buy vehicle, create route, assign.
4. Tick movement + revenue for two-stop bus shuttle.
5. Godot map + HUD wired to sim.
6. Train/air constraints + extra catalog units.
7. Polish editor UX; expand to 6+ cities.
8. Save/load if time permits.

## Acceptance checklist

- [x] Headless tests pass without Godot runtime.
- [x] Player can operate at least one bus route and see cash change over time.
- [x] Illegal train route (missing rail edge) is rejected with a clear message.
- [x] Air route requires airports at both ends.
- [x] Pause stops sim advancement; speed changes tick rate.
- [x] Docs still match implemented command set.

## Post-MVP directions (ordered suggestions)

1. Save/load + richer stats graphs.
2. Buildable infrastructure (airport upgrade, rail construction).
3. Multi-stop routes with segment capacity.
4. Second map pack / neighboring country.
5. Freight and contracts.
6. Rival companies.
7. World map stitching / zoom levels.

## Documentation ownership

When implementation begins, update:

- [Data model](04-data-model.md) if entities diverge.
- [Mechanics](03-game-mechanics.md) if formulas change.
- This checklist as features land.
