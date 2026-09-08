# Vision & scope

## Elevator pitch

You run a transport company on a geographic map. Connect cities with **buses**, **trains**, and **airplanes**, design **routes**, assign **vehicles**, and earn revenue from passenger (and later freight) demand. Grow from a handful of cities in one country toward a multi-country — eventually world — network.

## Fantasy

The player is a transport magnate: not a vehicle driver, not a city mayor. The fantasy is **network design and operations** — where to place capacity, which modes to use, how to price and schedule, and how to expand without going bankrupt.

Comparable spirit (not clones): *Transport Tycoon / OpenTTD*, *Airline Tycoon*, *Cities: Skylines* transit layers, *Mini Metro* (clarity of routes), *Industry Giant* (company growth). TravelEmpire leans toward **map-based company sim** with readable routes over pixel-perfect vehicle micromanagement.

## Design pillars

1. **Readable geography** — Cities sit on a real (or plausible) map. Distance and mode choice matter.
2. **Routes as the product** — The main creative act is defining a route and staffing it with vehicles.
3. **Modes feel different** — Bus, rail, and air share one economic model but differ in speed, capacity, cost, infrastructure, and access rules.
4. **Simulation is truth** — UI never invents money, demand, or positions; it only displays and commands.
5. **Start small, scale out** — One country, few cities → more cities → more countries → world map.

## Player fantasy loop

```text
Observe demand / bottlenecks
        ↓
Build or extend infrastructure (MVP: terminals exist; Later: stations, tracks, airports)
        ↓
Create or edit a route (stops, mode, schedule intensity)
        ↓
Buy / assign vehicles
        ↓
Watch cash flow, load factors, satisfaction
        ↓
Reinvest or expand to new cities
```

## In scope (product vision)

- Geographic map with cities (coordinates, population, metadata).
- Company cash, expenses, and passenger revenue.
- Vehicles: buses, trains, airplanes (distinct stats and constraints).
- Routes with ordered stops, assigned vehicles, and simulated travel.
- Time progression (simulation clock, pause/speed).
- Save / load of company and world state.
- Clear UI for map, city info, route editor, fleet, and finances.

## Out of scope (near term)

- Multiplayer / MMO.
- Real-time combat or combat-adjacent systems.
- Full city building (zoning, roads as a city sim).
- Photoreal 3D vehicle physics.
- Licensing of real airline/bus brand fleets (generic vehicle classes only unless licensed later).
- Procedural entire-Earth generation on day one (hand-authored country packs first).

## Success criteria for documentation phase

- A new contributor can explain the sim/UI boundary in one paragraph.
- MVP entities (City, Company, Vehicle, Route, Trip) are named and related.
- Tooling choices (Godot + C#, test strategy) are explicit enough to scaffold a project without re-debating basics.

## Open product questions (deferred)

These are acknowledged but not decided in MVP docs:

- Freight vs passengers-only for v1.1+.
- Competitive AI companies vs sandbox monopoly.
- Real timetable scheduling vs abstracted “service frequency.”
- Political / regulation systems (concessions, night bans).
- Climate / seasonality affecting demand and operations.
