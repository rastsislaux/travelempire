# Game mechanics

This document describes **player-facing systems**. Implementation details live in [Simulation](05-simulation.md) and [Data model](04-data-model.md).

## 1. World & map

### 1.1 Geographic basis

- The world is a **map pack**: a set of cities (and later regions, borders, infrastructure layers) with geographic coordinates.
- **MVP:** one country pack (e.g. a mid-sized country with 4–8 cities). Coordinates use a consistent CRS (recommend WGS84 lat/lon stored; projected to map x/y for display and distance).
- Distance between cities for gameplay uses **great-circle** or projected Euclidean distance in kilometers — pick one formula and keep it in the simulation.

### 1.2 Cities

Each city has at least:

| Field | Purpose |
|-------|---------|
| Id | Stable identifier |
| Name | Display name |
| Latitude / Longitude | Position |
| Population | Drives demand magnitude |
| Tags / region | Filtering, later bonuses |

**MVP:** cities are fixed content; the player does not found cities.

**Later:** grow population with economy; city levels unlock airport/rail grades.

### 1.3 Terminals (access points)

Modes need access points in a city:

| Mode | MVP access | Later |
|------|------------|--------|
| Bus | Implicit city bus terminal (always available) | Build/upgrade depots, capacity caps |
| Train | Station present or not (flag on city) | Build tracks between cities, station tiers |
| Air | Airport present or not (flag on city) | Runways, slots, hub bonuses |

**MVP rule:** every city has a bus terminal. A subset of cities have rail stations and/or airports defined in the map pack. Player cannot yet build missing airports (**Later**).

## 2. Company

The player controls one **Company**:

- **Cash** — primary resource.
- **Fleet** — owned vehicles.
- **Routes** — operated services.
- **Reputation / satisfaction** (**Later**) — affects demand.

### 2.1 Cash flow

**Income**

- Ticket revenue when a trip completes (or continuously accrued — MVP: on trip completion / per tick proportional to boarding).

**Expenses**

- Vehicle purchase (one-time).
- Vehicle operating cost (per distance and/or per tick while assigned).
- Route fixed cost (**Later**: licenses, station fees).
- Infrastructure construction (**Later**).

**Bankruptcy:** if cash < 0 beyond a grace rule, company is bankrupt → game over or restructuring (**MVP:** soft fail — block purchases, show warning; hard game-over optional).

## 3. Demand & passengers

### 3.1 Origin–destination (OD) demand

Demand is between **city pairs**, not “city wants buses” in the abstract.

**MVP model (simple):**

For each ordered pair `(A, B)` where `A ≠ B`:

```text
BaseDemand(A→B) = f(Population(A), Population(B), Distance(A,B))
```

Example shape (tunable, not final balance):

```text
gravity = Population(A) * Population(B) / Distance(A,B)^p
BaseDemand = k * gravity
```

Demand is expressed as **passengers per day** (or per sim-day) willing to travel if service exists.

### 3.2 Capture by routes

A route captures demand for pairs covered by its stop sequence:

- Direct service `A→B` captures `A→B` demand (full or high share).
- Multi-stop routes may capture intermediate pairs with penalties (**MVP:** only adjacent stop pairs, or only endpoint pairs — choose **adjacent consecutive stops** for clarity).

**Load factor:** passengers assigned cannot exceed remaining vehicle capacity on that segment.

Unserved demand is lost (no backlog **MVP**; **Later:** waiting queues).

### 3.3 Mode preference

Passengers prefer faster / cheaper journeys. **MVP:** single attractiveness score:

```text
Attractiveness = ComfortBias(mode) / (TravelTime * FareFactor)
```

If multiple routes serve the same pair, split demand by attractiveness share (**MVP may** give all demand to the single best route to simplify).

## 4. Modes of transport

Shared concepts: **speed**, **capacity**, **purchase cost**, **op cost**, **range**, **required terminal type**.

### 4.1 Bus

- Lowest purchase and op cost.
- Lowest speed; best for short/medium distances.
- Requires road connectivity — **MVP:** assume all cities are road-connected (complete graph for buses).
- **Later:** road quality, congestion, border buses.

### 4.2 Train

- Medium–high capacity; medium–high speed.
- Requires both cities to have stations and a **rail link**.
- **MVP:** rail links are predefined edges in the map pack (player cannot lay track yet). Routes may only use existing rail edges.
- **Later:** build track, electrification, freight cars.

### 4.3 Airplane

- Highest speed; high purchase/op cost; airport fees (**Later**).
- Requires airports at endpoints.
- **MVP:** complete graph among airports (airspace always connected), with min distance soft rule (discourage tiny hops via economics).
- **Later:** slots, hubs, long-haul vs regional aircraft types.

## 5. Vehicles

A **Vehicle** is an owned instance of a **VehicleType** (catalog).

| Concept | Meaning |
|---------|---------|
| VehicleType | Blueprint: mode, capacity, speed, costs, range |
| Vehicle | Instance: id, type, condition, assignment |

**Assignment:** a vehicle is assigned to at most one route (**MVP**). Unassigned vehicles cost storage (**Later**) or small idle cost (**MVP:** idle cost optional).

**MVP fleet actions:** buy from catalog; assign/unassign to route; sell at depreciation (**Later:** sell; MVP may omit sell).

## 6. Routes

A **Route** is the player’s designed service.

### 6.1 Definition

| Field | Description |
|-------|-------------|
| Id / Name | Identity |
| Mode | Bus, Train, or Air |
| Stops | Ordered list of city ids (min 2) |
| Assigned vehicles | 0..N vehicles of matching mode |
| Fare policy | Flat fare per passenger or per km (**MVP:** flat per passenger-trip or per km — pick per-km for fairness) |
| Service intensity | How aggressively vehicles loop (**MVP:** “as fast as possible” with turnaround time) |

### 6.2 Validation rules (MVP)

- ≥ 2 stops, no consecutive duplicate cities.
- All consecutive pairs legal for the mode (road/rail edge/airports).
- All assigned vehicles match route mode and are owned/unassigned elsewhere.
- Company can afford creation if there is a creation fee (**MVP:** free to create; costs are vehicles + ops).

### 6.3 Operation loop

For each assigned vehicle:

1. Depart current stop toward next (circular or round-trip).
2. Travel for `Distance / Speed` sim time (+ fixed turnaround).
3. On arrival: unload passengers (revenue), board new passengers up to capacity, proceed.

**MVP topology:** circular loop through stops, or out-and-back. Recommend **out-and-back** on an ordered list (A→B→C→B→A…) or simple **two-stop shuttle** for the first build. Multi-stop loops can follow immediately after shuttles work.

### 6.4 Schedule abstraction

**MVP:** no clock-face timetable. Vehicles run continuously with turnaround delays.

**Later:** departure slots, timed connections, night layovers.

## 7. Travel time & movement

```text
TravelTime = DistanceKm / CruiseSpeedKmh + TurnaroundHours
```

Simulation moves vehicles along a logical edge; interface draws along a geodesic or map path polyline.

## 8. Fares & revenue (MVP formula)

```text
Fare = Max(MinFare, PricePerKm * DistanceKm)
Revenue = PassengersBoarded * Fare   // collected on arrival or boarding — pick one; recommend on arrival
```

Player may set `PricePerKm` per route within clamps. Higher fare → lower demand capture (**MVP:** linear penalty).

## 9. Progression & goals

**MVP:** sandbox — grow cash and network with soft goals (connect N cities, reach cash X).

**Later:** scenarios, campaigns, rival companies, contracts (“serve city X with rail by date Y”).

## 10. Failure & friction

Meaningful constraints that create decisions:

- Capacity vs demand (buy more vehicles vs raise fares).
- Mode choice vs distance (bus cheap but slow; air fast but expensive).
- Locked infrastructure (cannot air-serve a city without airport).
- Operating costs on empty runs (frequency without demand burns cash).

## 11. Cheats / debug (dev only)

Not player features: grant cash, spawn vehicles, force demand multipliers, skip time. Exposed via debug UI or console commands that still go through simulation commands.
