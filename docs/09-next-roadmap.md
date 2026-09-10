# Next roadmap — improve the existing setup

North star for the main screen: [`references/mockup-01-main-operations.png`](references/mockup-01-main-operations.png).  
MVP is playable; this roadmap focuses on **UI quality**, **catalog/progression depth**, and **real geography content** without rewriting the simulation architecture.

## Current baseline

| Area | Today | Gap vs mockup / vision |
|------|--------|-------------------------|
| UI | Map + right tabs (Buy / Fleet / Routes) as crude tables | No city-centric panel, no terrain/legend/mode strokes, bulky chrome |
| Vehicles | 1 bus, 1 train, 1 jet | Mockup shows multiple bus tiers; need progression & filters |
| Map | Fictional Aurelia, 6 abstract cities, flat markers | Need real countries, richer city metadata, map presentation |
| Sim | Commands, OD gravity, routes, revenue | Growth/happiness/economy are UI fiction until sim supports them |

**Hard constraint:** keep **Simulation ↔ Interface** separation. UI redesign must not put rules in Godot scripts.

---

## Track 1 — UI toward mockup #1

Buildable detail for this track — scene-by-scene work items, acceptance checks and component inventory — lives in the [UI execution plan](10-ui-execution-plan.md).

### Design principles (from mockup)

1. **Map is the hero** — first viewport is geography + network, not a control dashboard.
2. **One selected subject** — side panel is primarily *City — {Name}*, not a permanent global toolbox soup.
3. **Mode language is color + stroke** — bus green solid, rail amber (double), air blue dashed; legend required.
4. **Tables, not card piles** — Buy/Fleet/Routes are structured grids with one primary CTA.
5. **Mode filter before list** — Bus / Train / Plane chips narrow the catalog.
6. **Calm dark ops chrome** — slate/navy + amber brand; avoid purple glow / cream-terracotta clichés.
7. **Progressive disclosure** — advanced stats (happiness, demand breakdown) appear in context; tip footers teach, don’t nag.

### Information architecture

| Surface | Owns |
|---------|------|
| **Map** | Terrain/background, cities, terminals icons, routes, vehicles, legend, scale/compass |
| **Top bar** | Brand, cash, sim clock/date, speed, entry points (stats / settings / menu — stubs OK) |
| **Side panel** | Selected city header + stats; tabs Buy / Fleet / Routes scoped by selection where sensible |
| **Later screens** | Global finances, new-game country picker, pause menu (not in Track 1) |

**Selection model (decision):**

- Clicking a city sets `SelectedCityId` and opens/focuses the city panel.
- Buy catalog is **global** but filtered by mode chips (and later by unlock tier).
- Fleet / Routes tabs default to **all company**, with optional “serving this city” filter chip (Phase B).
- Creating a route still uses map clicks for stops; panel shows the draft strip.

### Phased UI work

#### Phase A — Visual & layout pass (no new sim fields)

Goal: stop looking like a debug tool; match mockup *composition*.

- Restyle top bar: wordmark + tagline, cash, day, speed group, placeholder icon buttons.
- Widen/restructure side panel: city header block (name, close), then tabs.
- Buy tab: mode filter chips → catalog `Tree` → primary **Buy Vehicle** button.
- Map: mode-colored strokes (solid / thicker / dashed), simple legend, population under city labels.
- Keep selection stable across ticks (already partly fixed; regression-test).

**Accept:** Screenshot comparable to mockup layout at 1280×720; buy still works without deselection; create-route form never clipped off-screen.

#### Phase B — City-centric panel

Goal: panel matches mockup *information hierarchy*.

- City hero slot (placeholder texture pack per city or biome).
- Stats grid: population (live); growth / happiness / economy as **placeholders or derived readouts** until sim exists.
- Terminal chips: Airport / Station / Port from existing flags (`HasAirport`, `HasRailStation`; port = new flag or reuse later).
- Demand bars: UI aggregation from OD demand touching this city (query API on sim — thin addition OK).
- Fleet/Routes: “In this city / Serving this city” filter.

**Accept:** Selecting Rivermere-equivalent city shows header + stats + filtered buy; demand bars move when routes appear.

#### Phase C — Map presentation polish

Goal: map readability approaches mockup.

- Background map image or layered polygons for land/water (content pipeline).
- Airport/port icons on cities; vehicle icons by mode.
- Compass + scale bar from projection meters-per-pixel.
- Optional neighbor region labels (cosmetic).

**Accept:** New player can identify modes from color/stroke alone; scale bar roughly matches haversine distances.

### Component inventory

| Component | Priority | Notes |
|-----------|----------|--------|
| `TopBar` | A | Brand, cash, clock, speed |
| `MapLegend` | A | Mode + city/airport/port |
| `ModeFilterChips` | A | Bus/Train/Air |
| `DataTable` (Tree wrapper) | A | Shared Buy/Fleet/Routes |
| `PrimaryButton` | A | Buy / Create / Assign |
| `CityHeader` | B | Name, hero, close |
| `StatGrid` | B | Pop / growth / … |
| `DemandBars` | B | Per-mode fill |
| `RouteStrokeRenderer` | A/C | Style by mode |
| `MapDecor` | C | Terrain, scale, compass |

### UI-only vs needs sim/data

| Mockup element | Track |
|----------------|--------|
| Brand, layout, chips, tables, legend, strokes | UI only |
| Population, airport/rail flags | Already in sim |
| Cash, day, speed | Already in sim (date formatting UI-only) |
| Demand bars from OD | Thin sim **query** (no new rules) |
| Growth %, happiness, economy strength | **Later sim** — show “—” or hide until ready |
| City hero photos | **Content** assets |
| Terrain / water / neighbors | **Content** map pack art |
| Running cost “/ day” column | Derive from op costs in UI or add catalog field |

### UI non-goals (this track)

- Full redesign of simulation commands.
- Separate finance screen, tech tree UI, multiplayer lobby.
- Pixel-perfect clone of mockup photography.
- Mobile layout.

---

## Track 2 — Populate vehicles & progression

### Catalog depth (content + balance)

Expand `content/vehicles/catalog.json` (and `BuiltInContent` mirror) toward mockup tiers:

**Bus**

| Id | Role | Cap | Speed | Notes |
|----|------|-----|-------|--------|
| `bus.city` | Short hop | ~40 | 60 | Cheap, low range comfort |
| `bus.coach` | Intercity | ~55 | 80 | Default workhorse |
| `bus.articulated` | High capacity | ~85 | 55 | City-heavy demand |

**Train**

| Id | Role |
|----|------|
| `train.regional` | Existing EMU |
| `train.intercity` | Faster, pricier |
| `train.high_capacity` | Commuter double-set |

**Air**

| Id | Role |
|----|------|
| `air.regional_jet` | Existing |
| `air.turboprop` | Short runway / cheaper, slower |
| `air.narrowbody` | Higher cap, higher cost, min distance |

Each type keeps: mode, capacity, cruise speed, purchase, op cost/km, op cost/hour, optional `maxRangeKm`, plus new optional fields below.

### Progression model (minimal, sim-light)

Prefer **unlock gates** over RPG XP:

1. **Cash gate** — always (already).
2. **Company milestones** — unlock types when cumulative passengers or cities served ≥ N (`SimConfig` + `Company` stats already track passengers).
3. **Optional tier field** on vehicle types: `tier: 1|2|3`; New Game starts with tier-1 visible; commands reject locked buys with clear error.

**Phase order:**

1. Add 2–3 vehicles per mode (no locks) — pure content.
2. Add `tier` / `requiresCitiesServed` and filter Buy list + validate `BuyVehicleCommand`.
3. Soft tutorial tips in UI when a new tier unlocks.

### Balance loop

- Keep money in minor units.
- Spreadsheet or console scenario: buy coach → shuttle capital–secondary → 500 ticks → cash Δ must be positive for tier-1; tier-3 should be investment.
- Extend unit tests: locked vehicle fails; unlocked succeeds.

### Non-goals

- Vehicle maintenance/crash/aging (later).
- Manufacturer brands / liveries (later).
- Freight rolling stock (later).

---

## Track 3 — Real countries & cities

### Strategy

Stay with **hand-authored map packs** (JSON), not live OSM downloads in-engine for v1 content.

```text
content/maps/{packId}/
  pack.json          # id, name, bbox, projection hints, background asset ref
  cities.json        # real WGS84, population, terminals
  rail_edges.json    # curated edges (not full national graph)
  meta.json          # optional: sources, license notes
  art/               # background, city heroes (optional)
```

### Pack pipeline

1. **Pick country** — start with one mid-sized, data-friendly country (e.g. Lithuania, Czechia, Portugal, Switzerland — final pick TBD).
2. **Select 8–15 cities** — capital + regional hubs + port/airport variety; populations from a cited public dataset (Natural Earth / national stats / GeoNames — document source in `meta.json`).
3. **Curate rail edges** — only major corridors that make gameplay decisions interesting (not every branch line).
4. **Airports** — flag cities with real commercial airports; bus everywhere.
5. **Validate** — content loader tests: unique ids, coordinates inside bbox, rail endpoints exist, haversine sanity.

### Engine / UI hooks

- New Game: choose map pack (even if only 2 packs: Aurelia tutorial + Country A).
- `ContentLoader.LoadMapPack(path)` already exists — wire Godot to load from `res://` or copied `content/` instead of only `BuiltInContent`.
- Projection: keep equirectangular for small countries; store `pack.anchor` for background image alignment later.
- Deprecate Aurelia as “sandbox / tutorial” or regenerate it to match mockup placenames once art exists — **don’t block** real packs on renaming fiction.

### Licensing checklist

- Population & coordinates: public-domain or openly licensed sources only; record attribution.
- Map background art: original, CC0, or licensed; no Google/Apple tiles in redistributed builds.
- City photos: pack-specific assets with rights cleared.

### Non-goals

- Whole-Earth continuous zoom.
- Auto-import of full OpenStreetMap road/rail graphs.
- Real operator liveries / real airline fleets.

---

## Suggested execution order

Work that improves the *current* setup with least architecture risk:

| Step | Track | Outcome |
|------|-------|---------|
| 1 | UI-A | Mockup-like chrome; mode strokes; Buy chips + CTA |
| 2 | Vehicles-1 | Multi-tier catalog (unlocked) |
| 3 | Maps-1 | First real-country pack + pack picker |
| 4 | UI-B | City panel + demand query |
| 5 | Vehicles-2 | Unlock tiers / milestones |
| 6 | Maps-2 | Background art + icons; second country |
| 7 | UI-C | Legend/scale/compass polish |

Parallelism: content (2, 3) can proceed beside UI-A if owners differ.

---

## Acceptance for “setup improved”

- [ ] Side panel matches mockup hierarchy (city header → tabs → table → one CTA).
- [ ] At least **3 vehicles per mode** in catalog; Buy filters by mode.
- [ ] At least **one real-country map pack** playable with ≥8 cities and curated rail.
- [ ] New Game can start on Aurelia *or* real pack.
- [ ] Headless tests still pass; no Godot types in Simulation.
- [ ] Mockup #1 stored under `docs/references/`; later mockups append as `mockup-02-…`.

---

## Open decisions (need product call)

1. First real country pack identity.
2. Whether Port is a first-class terminal flag now or later.
3. Whether happiness/growth ship as stubs in UI-B or wait for sim.
4. Keep fictional Aurelia as tutorial vs replace names to match mockup (Rivermere, Westport, …).

---

## References

- Mockup #1: [`references/mockup-01-main-operations.png`](references/mockup-01-main-operations.png)
- Interface doc: [`06-interface.md`](06-interface.md)
- MVP (done): [`08-mvp-roadmap.md`](08-mvp-roadmap.md)
- Data model: [`04-data-model.md`](04-data-model.md)
