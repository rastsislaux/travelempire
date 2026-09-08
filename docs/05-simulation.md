# Simulation

The simulation is a **pure C# class library** with no dependency on Godot. It is the source of truth for the game world.

## Public surface

Suggested façade:

```csharp
public sealed class Simulation
{
    public CommandResult Apply(ICommand command);
    public void Tick(int tickCount = 1);
    public GameSnapshot GetSnapshot();
    // Typed queries as needed:
    public IReadOnlyList<RouteView> GetRoutes();
    public CityView GetCity(CityId id);
    public CompanyView GetCompany();
}
```

Views/snapshots are immutable DTOs safe for the UI thread to read. Mutation only via `Apply` / `Tick`.

## Initialization

1. Load map pack DTO (cities, rail edges, terminal flags).
2. `Apply(new NewGameCommand(...))` creates company with starting cash and empty fleet.
3. Build demand matrix from populations/distances.
4. Interface begins rendering snapshot.

## Tick pipeline (MVP)

Each tick:

1. **Advance clock** by configured Δt.
2. **Move vehicles** in transit; complete arrivals when progress ≥ 1.
3. **On arrival:** disembark → collect revenue → update cash → emit events.
4. **Board passengers** from local demand remaining for this tick/day slice.
5. **Charge operating costs** (per-km for distance traveled this tick; per-hour idle/assigned as designed).
6. **Refresh aggregates** (load factor stats, daily revenue counters).

Order must be documented in code and kept stable for tests.

## Movement

Logical only:

```text
progress += (CruiseSpeedKmh * HoursPerTick) / EdgeLengthKm
```

When `progress ≥ 1`, snap to destination city, reset for turnaround timer, then start next leg.

## Demand sampling

Per tick, for each active route segment currently boarding:

```text
availableDemand = OdPassengersPerDay(from,to) * (HoursPerTick / 24) * FareDemandMultiplier(price)
boarded = Min(availableDemand, remainingCapacity)
```

**MVP simplification:** consume demand without a global OD reservation table (routes independently sample — can oversubscribe). Acceptable for prototype; **Later:** central allocator.

## Command validation

Commands validate against current state and return errors without partial mutation (transactional apply).

Example: `BuyVehicle` fails if `Cash < PurchaseCost`.

## Configuration

Balance knobs live in data (JSON) or `SimConfig`:

- Starting cash
- Hours per tick
- Gravity demand exponent and scalar
- Min fare, default price per km
- Turnaround hours by mode
- Op cost multipliers

Interface must not hardcode different numbers for “display estimates”; ask simulation for quotes (`EstimateRouteRevenue` query **Later**, or shared pure functions in Simulation).

## Headless harnesses

Support:

```text
dotnet test
dotnet run --project tools/TravelEmpire.SimBench -- ticks 10000
```

Bench/scenario tools help profile tick cost before Godot is involved.

## Threading

**MVP:** single-threaded simulation. Interface calls `Tick`/`Apply` on main thread or a dedicated sim thread with a clear marshaling story. Do not share mutable `GameState` across threads without a lock/queue. Prefer main-thread sim until proven necessary.

## Modding / packs (**Later**)

Map packs and vehicle catalogs as additive content. Simulation loads via interfaces (`IMapPack`, `IVehicleCatalog`) so Godot Resource importers can feed DTOs without leaking Godot types inward.
