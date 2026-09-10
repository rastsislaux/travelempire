# TravelEmpire

Godot 4 + C# transport-empire game. Build bus, rail, and air networks across a geographic map.

## Status

**MVP v1** is implemented:

- Headless C# simulation (commands, demand, movement, revenue)
- Unit/integration tests
- Godot 4 map UI (buy vehicles, create routes, assign fleet, pause/speed)
- Console host for headless smoke demos

Docs: [`docs/`](docs/README.md)

## Quick start

### Requirements

- .NET SDK **8+** (10.x is fine; `global.json` rolls forward)
- Godot **4.7** .NET build

### Tests & console demo

```bash
dotnet test
dotnet run --project src/TravelEmpire.ConsoleHost -- -t 500
```

### Godot UI

1. Open `src/TravelEmpire.Godot/project.godot` in Godot 4.7 .NET.
2. Wait for C# restore/build if prompted.
3. Press Play.

Gameplay loop:

1. Buy a **Standard Coach** from the catalog.
2. Click two cities (e.g. Aurel → Scholar's Rest) and **Create** a bus route.
3. Select the vehicle + route → **Assign**.
4. Unpause / set speed and watch cash, passengers, and vehicles move.

## Architecture

```
TravelEmpire.Godot / ConsoleHost  →  commands + snapshots
                ↓
        TravelEmpire.Simulation   (pure C#, no Godot)
```

See [docs/02-architecture.md](docs/02-architecture.md).

## Content

Built-in pack **Republic of Aurelia** (6 cities). JSON mirrors live under `content/`.

## License

TBD.
