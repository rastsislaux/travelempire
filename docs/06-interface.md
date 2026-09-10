# Interface (Godot)

The interface layer is a **Godot 4** project using **C#** scripts. It presents simulation state and translates input into commands. It does not own economy or route legality.

## Responsibilities

| Does | Does not |
|------|----------|
| Draw map, cities, routes, vehicles | Decide travel times or fares math (asks sim / shared pure helpers) |
| Pan/zoom camera | Mutate `Cash` directly |
| Route editor UI | Bypass mode access rules |
| HUD: cash, time, selected entity | Advance world inside `_Draw` |
| Audio/VFX feedback on events | Persist UI-only fields into sim save blindly |

## Scene map (MVP)

```text
Main
├── MapViewport                # hero map (cities, routes, vehicles)
├── Hud
│   ├── TopBar                 # cash, clock, speed controls
│   └── SidePanel
│         ├── Selection        # compact city inspector
│         └── TabContainer
│               ├── Buy        # catalog table + buy action
│               ├── Fleet      # owned vehicles table + assign
│               └── Routes     # routes table + create-route form
└── SimulationHost             # Node owning Simulation instance
```

Buy / Fleet / Routes use column tables (`Tree`). List rebuilds happen on command results only so selection is not cleared every sim tick.

`SimulationHost` is the only node that should construct and tick `Simulation`.

## SimulationHost pattern

```text
_Ready:
  create Simulation, apply NewGameCommand with selected map pack

_PhysicsProcess or timer:
  if not paused:
    accumulate real time * speed multiplier
    while accumulated >= SecondsPerSimTick:
      Simulation.Tick()
      accumulated -= SecondsPerSimTick
  refresh bindings if dirty
```

On button actions: build command → `Apply` → show error toast or refresh.

## Map presentation

- Project lat/lon → 2D plane (equirectangular for small countries is fine; or use a preprojected map texture with calibrated anchors).
- City marker click → select city → side panel shows population, terminals, routes serving city.
- Route polyline: straight segments or simple curves between stops; color by mode (bus / rail / air).
- Vehicle icon: lerp between last snapshot positions for visual smoothness; snap if desynced.

## Route editor UX (MVP)

1. Choose mode.
2. Click cities in order (validate each extension live via a dry-run validate API or local mirror of rules that call sim `ValidateCreateRoute`).
3. Set name and price per km.
4. Confirm → `CreateRouteCommand`.
5. Assign vehicles from fleet list.

Invalid edges (e.g. train without rail link) show clear error text from `CommandResult`.

## HUD requirements

- Always-visible cash and sim time.
- Pause / speed.
- Entry points: Fleet, Routes list, Buy vehicle.
- Selected entity inspector.

Avoid dashboard clutter on the first viewport: the **map is the hero**. Panels are contextual, not a wall of stats.

## Input

- Mouse/touch: pan, zoom, select, place route stops.
- Keyboard shortcuts (**Later**): pause, speed, escape closes dialogs.

## Presentation state

OK to store in interface only:

- Camera position/zoom
- Selected city/route id
- Open dialog
- Animation tweens
- Unsaved draft route stops (until Confirm issues command)

Draft routes must not affect simulation until committed.

## Theming & assets

- Distinct mode colors and icons.
- Readable city labels at multiple zoom levels (declutter when zoomed out).
- No dependency on specific art pipeline beyond Godot import for MVP (placeholders OK).

## Localization

- **MVP:** English strings in UI.
- Structure UI text for later localization (Godot translation keys).

## Accessibility (**Later**)

- Scalable UI, colorblind-safe mode colors (patterns, not color alone).
