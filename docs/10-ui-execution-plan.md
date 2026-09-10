# UI execution plan — main operations screen

The detailed execution plan for **Track 1** of the [next roadmap](09-next-roadmap.md). That document sets the direction across UI, catalogue and map content; this one is the buildable UI plan: named scenes, work items, acceptance checks and the decisions a developer needs before opening the Godot editor.

Reference mockup: [`references/mockup-01-main-operations.png`](references/mockup-01-main-operations.png) — the **main operations screen**, and the north star for this iteration. More mockups (finance, fleet detail, route detail) arrive later; this document only commits to screen #1.

Current state being replaced: the prototype HUD built imperatively in `src/TravelEmpire.Godot/scripts/Main.cs` (`BuildUi()`), a 460 px right sidebar containing a `Selection` label, three `Tree` tables in a `TabContainer`, and a status `Label`, plus the procedural grid map in `MapView.cs`.

**This iteration changes presentation only.** The simulation keeps its command/snapshot contract. Where the mockup needs data the sim does not have, this document marks it and defers it — it never smuggles UI concerns into `Simulation`.

### Where this plan takes a position

Two deliberate departures from, and three answers to, the open questions in [Track 1](09-next-roadmap.md):

- **`Tree` is retired, not wrapped.** Track 1 proposes a `DataTable` wrapper around `Tree`. `Tree` cannot deliver the mockup's per-row icons, right-aligned currency columns and hover styling without fighting it, and its `Clear()` + re-`Select(0)` rebuild cycle is the direct cause of today's selection fragility. Tables here are tens of rows, so a styled row list is cheaper to build *and* cheaper to reason about (§4).
- **Phase A starts with a theme resource and a scene split**, before any restyling. Restyling the imperative `BuildUi()` in place would have to be undone immediately afterwards.
- **Port** becomes a first-class `HasSeaport` flag, because the mockup shows it in both the legend and the stats grid (§5).
- **Happiness and growth ship as `—`**, never as invented numbers (§7).
- **Aurelia's placenames and coordinates are re-authored to match the mockup**, but only in Phase C alongside the terrain art, so content and screenshots land together and nothing else waits on them (§5).

---

## 1. Design principles

1. **The map is the product.** Terrain, routes and vehicles own ~70 % of the pixels. Every panel is a guest on that screen and must justify its width. No second dockable panel, no bottom bar, no floating windows.
2. **One context at a time.** The right panel answers a single question: *"what about this city?"*. It is not a dashboard. When nothing is selected it shows one empire summary, not an empty frame.
3. **Read on the map, act in the panel.** Network state (who connects to whom, where vehicles are, what mode) is legible without clicking. Mutations (buy, assign, create) happen only in the panel, behind explicit buttons.
4. **Mode identity is absolute.** Bus = green solid, Rail = amber double-line, Air = blue dashed. The same three colours and the same three glyphs are used on lines, legend, chips, demand bars, table rows and vehicle badges. A player must never have to ask which mode something is.
5. **Numbers are typed, not stringly.** Cash, population, capacity, cost and price get consistent formatting, alignment (numerics right-aligned) and colour semantics (green = income/good, amber = neutral/warning, red = loss). No `Day 12.40` style debug output in front of the player.
6. **Cards over walls.** Related facts sit in a bordered surface with a small caps label. Between cards there is space. A screen that needs a scrollbar in the first fold has failed.
7. **Every control states its state.** The active speed, the active tab, the active mode chip, the selected row, and an unaffordable Buy button are all visually distinct. The current UI has four identical speed buttons and a separate text label — that is the bug class to eliminate.
8. **Failures speak in place.** Command failures surface as a transient toast near the action that caused them, carrying `CommandResult.ErrorMessage`. Never a permanent label that keeps stale text.

---

## 2. Information architecture

### Top bar (global, always visible, ~52 px)

| Zone | Content | Notes |
|------|---------|-------|
| Left | Crown mark + `TravelEmpire` wordmark + tagline "Connect People. Grow Further." | Tagline hides below 1100 px width |
| Centre-left | Cash, green, `$4,775,000` | Flashes red on a failed purchase, dims when negative |
| Centre | Calendar glyph + `Day 12.4` + `Apr 17, 2030` | Day from `SimHours / 24`; date derived UI-side from a content epoch |
| Right | Segmented `⏸ / 1x / 2x / 4x` | One active state, mirrors `SimulationHost.SpeedMultiplier` |
| Far right | Stats, settings, menu icon buttons | Stubs this iteration (see non-goals) |

### Map (hero)

Owns: terrain and water, national border, neighbour-country labels, country wordmark, city markers with population, route polylines styled by mode, vehicle badges in motion, selection highlight, hover highlight, route-draft preview.

Overlays anchored inside the map, floating above terrain: **legend card** (top-left), **compass + km scale** (bottom-left), **toast stack** (bottom-centre).

Does not own: any table, any form, any commit action.

### Right panel (contextual, 360 px at 1280 baseline)

Top-to-bottom, exactly as the mockup:

1. Header — `City — Rivermere` + close `✕`. Close clears selection and returns the panel to **Empire** mode; it does not hide the panel.
2. City hero image.
3. Stats grid — Population, Demand (three mini bars), Growth, Happiness, Economy, Airport, Port.
4. Tabs — Buy / Fleet / Routes.
5. Tab body — mode chips + data table + primary CTA.
6. City Demand detail — Local Transport / Intercity / Air Travel with bar + qualitative label.
7. Tip footer.

**Scope decision (resolves the city-vs-global tab ambiguity): the three tabs are global lists with a context filter.** `Buy` is always the full catalogue; `Fleet` and `Routes` default to *serving the selected city* with a `Show all` chip, and are unfiltered in Empire mode. Rationale: the sim owns one company-wide fleet and route list (`Company.Vehicles`, `Company.Routes`), and vehicles are delivered to `state.Cities[0]` regardless of selection. Pretending the tabs are city-scoped would be a lie the sim cannot back.

### Future screens (stubs only)

Finance/stats modal, settings, main menu, save/load, route detail editor, fleet detail. Each gets an entry point in the top bar or panel now and a real screen when its mockup lands.

---

## 3. Phased implementation plan

### Phase A — foundation and quick wins

*No new simulation data. Everything here is achievable against today's `GameSnapshot`.*

| # | Work item | Where |
|---|-----------|-------|
| A1 | Create `res://ui/theme/travelempire.tres`: colour roles, `StyleBoxFlat` for panel/card/chip/primary-button/tab, `Label` type variations (`H1`, `H2`, `StatLabel`, `StatValue`, `Caption`, `Mono`). Add a static `Palette` class mirroring the same colours for `_Draw` code. Assign the theme on `Main`. | new `ui/theme/` |
| A2 | Split the imperative `BuildUi()` into scenes: `Main.tscn` composes `TopBar.tscn`, `MapView.tscn`, `MapOverlay.tscn`, `SidePanel.tscn`, `ToastLayer.tscn`. One script per scene, children exposed via `[Export]` node references, upward communication via C# events. `SimulationHost` is untouched. | `scenes/`, `scripts/` |
| A3 | Rebuild the top bar per §2: wordmark, formatted cash, `Day X.X` + derived date, segmented speed control with an active state, icon button stubs. Delete `_speedLabel`. | `TopBar` |
| A4 | Introduce a `MapCamera` helper: cursor-anchored zoom, clamped pan, `FitToContent()`. Today `ToScreen` is `world * zoom + pan + Size * 0.08f`, which zooms toward the origin and drifts. | `MapView.cs` |
| A5 | Map line styling: bus solid 3 px, rail amber double-line (two offset polylines), air blue dashed. Draw mode order air → rail → bus so bus reads on top. Rail edges become a subdued track texture, not a brown line. | `MapView.cs` |
| A6 | City markers: white ring + dark core, name above, population below, selected state larger with a glow ring, hover state. Declutter labels below a zoom threshold (population-ranked). | `MapView.cs` |
| A7 | Vehicle badges: replace `DrawCircle` with a rounded mode-coloured badge carrying the mode glyph, oriented along the leg, position lerped between snapshots. | `MapView.cs` |
| A8 | Legend card + compass + km scale as `MapOverlay` children with anchors. Legend rows: Bus, Rail, Air, City, Airport, Port, National Border. | `MapOverlay` |
| A9 | **Break the city-click / route-draft coupling.** Today `OnCityClicked` selects a city, appends it to `_draftStops`, *and* force-switches to the Routes tab. Split into two input modes: `Select` (default — inspect only) and `PlanRoute` (entered from the Routes tab; clicks append stops, `Esc` exits). Selection state moves to a pure `UiSelection` type. | `Main.cs`, `MapView.cs` |
| A10 | Replace `_statusLabel` with a toast stack: 3-second auto-dismiss, error/success variants, max 3 stacked. | `ToastLayer` |
| A11 | Cache one snapshot per frame. `OnCityClicked` → `UpdateDraft` currently calls `GetSnapshot()` twice per click, and `GetSnapshot()` rebuilds every view object via LINQ. | `Main.cs` |

**Phase A acceptance checks**

- No `AddThemeColorOverride` / inline `StyleBox` construction remains in `Main.cs` or panel scripts; all styling resolves through the theme.
- Clicking a city with the Routes tab open does **not** add a stop; the tab does not change; entering Plan Route mode and clicking three cities produces exactly three stops in click order.
- Scroll-zooming keeps the point under the cursor fixed within 1 px; panning cannot lose the map off-screen; `FitToContent` frames all cities with margin.
- The active speed is visually unambiguous at a glance; pressing a speed button updates both the segment and `SimulationHost.SpeedMultiplier`.
- A failed buy (insufficient funds) raises a red toast carrying the sim's error text and leaves no residual message after 3 s.
- Legend, compass and scale stay correctly anchored at 1280×720 and 1920×1080; the scale bar's km label is correct after zooming.
- A colourblind reviewer can distinguish the three modes by line pattern alone (solid / double / dashed) with colour removed.

### Phase B — the city panel

*Needs three small, additive `Views` changes (§5). Nothing in the tick loop or command set changes.*

| # | Work item | Where |
|---|-----------|-------|
| B1 | `SidePanel.tscn` with two states: **City** (header `City — <name>`, close ✕) and **Empire** (header `Empire`, company summary replacing the hero + stats block). Same tabs in both. | `SidePanel` |
| B2 | `CityHero.tscn`: 16:9 image slot, rounded corners, bottom gradient scrim. Falls back to a deterministic gradient + city initial when no art exists, keyed by city id so it is stable across sessions. | new |
| B3 | `StatGrid.tscn`: two-column label/value rows, right column for Growth / Happiness / Economy / Airport / Port, left for Population + the three demand mini-bars. Values that the sim cannot supply yet render as `—`, never as a fake number. | new |
| B4 | `DemandBar.tscn`: mode glyph + label + track + fill + qualitative tag (`High` / `Medium` / `Low`). Used both in the stats grid (compact) and the City Demand section (full width). | new |
| B5 | `ModeChipRow.tscn`: Bus / Train / Plane toggle group, active chip tinted with its mode colour. Filters the Buy table and pre-sets the mode for route planning. | new |
| B6 | `DataTable.tscn` + `DataRow.tscn`: header row, scrollable body, typed cells (icon, text, right-aligned numeric, currency), single-row toggle-group selection, hover state, empty state. **Retires all three `Tree` instances.** Selection is held as an id in the view-model and re-applied after a rebuild. | new |
| B7 | Wire Buy: mode-filtered catalogue as Vehicle / Capacity / Running Cost / Price; a full-width amber `Buy Vehicle` CTA that disables with a reason tooltip when cash < price or no row is selected. | `SidePanel` |
| B8 | Wire Fleet: rows show vehicle, mode, state, route, load `12/50`; context-filtered to the selected city with a `Show all` chip; primary action `Assign to route…`. | `SidePanel` |
| B9 | Wire Routes: rows show name, mode, stop chain, vehicle count, and a live-load indicator; primary action `Plan new route` which enters map Plan Route mode. Validate each candidate stop live through the existing, currently-unused `Simulation.ValidateCreateRoute` and show the rejection inline before the player commits. | `SidePanel`, `MapView` |
| B10 | City Demand section + tip footer. | `SidePanel` |
| B11 | Extract a pure `CityPanelViewModel` (no Godot types, in `TravelEmpire.Simulation`-adjacent or a new `TravelEmpire.Presentation` project) mapping `GameSnapshot` + `UiSelection` → panel rows, and unit-test it in the existing headless test project. | new |

**Phase B acceptance checks**

- Selecting each of the six cities populates every stats field with real or explicitly-unknown values; no placeholder ever displays a number the sim did not produce.
- Buy → the bought vehicle appears in Fleet without a full-panel flicker, cash decreases, and the previously selected catalogue row stays selected.
- Ticking the sim for 200 ticks while a table row is selected never clears that selection (the current `Tree` rebuild + `Select(0)` path is the regression this replaces).
- Assigning a vehicle to a route of another mode shows the sim's `vehicle.mode_mismatch` message as an inline error, not a silent no-op.
- Planning an air route between two cities where one lacks an airport is rejected *at click time*, with the reason, before any command is issued.
- Panel content fits without scrolling at 1280×720 with a 6-row table; longer tables scroll inside the table body only, never the whole panel.
- `CityPanelViewModel` tests cover: no selection, selected city with zero routes, selected city with routes of all three modes, unaffordable catalogue entry.

### Phase C — polish and content parity

| # | Work item | Where |
|---|-----------|-------|
| C1 | Georeferenced terrain art: map pack gains a background texture plus explicit `bounds` (min/max lat/lon) so projection and art agree. `MapView` drops the procedural grid. | content + `ContentModels`, `MapView` |
| C2 | Water/region/neighbour labels and the dashed national border, authored as map-pack data (label text, anchor lat/lon, style) rather than hard-coded strings. | content |
| C3 | Content parity with the mockup: rename/re-place cities to the mockup set, add a third mode of demand-relevant flags, and grow the catalogue to ~3 types per mode (City Bus / Coach / Articulated, etc.). | `content/` |
| C4 | Motion polish: animated flow dashes along active lines, panel slide-in, cash tick-up, selection ring pulse. Budget-capped; disabled when speed is 4x. | `MapView`, `SidePanel` |
| C5 | Hover tooltips for city markers, route lines and truncated table cells. | `MapView`, `DataTable` |
| C6 | Keyboard: `Space` pause, `1/2/3` speeds, `Esc` cancels plan mode / clears selection, `F` fit map. | `Main.cs` |
| C7 | Resolution QA + a `content_scale_factor` pass; verify 1280×720, 1366×768, 1920×1080, 2560×1440. | `project.godot` |
| C8 | Perf pass: profile `_Draw` and panel refresh at 4x with 50 vehicles; move per-tick numeric updates to targeted cell writes (the pattern `RefreshFleetLive` already uses) instead of table rebuilds. | all |

**Phase C acceptance checks**

- Cities sit on plausible terrain (no city in the sea) at every zoom level; the border and water labels track pan/zoom without jitter.
- Frame time stays under 8 ms at 4x with 50 vehicles and all three tabs exercised; no per-frame allocation spike from snapshot rebuilds.
- All strings the player sees come from one place and are ready for translation keys; no `ToString()` of an enum reaches the screen (`VehicleState.InTransit` → "In transit").
- Side-by-side screenshot against the mockup at 1920×1080 differs only in art fidelity, not in layout, hierarchy or labelling.

---

## 4. Component inventory

| Component | Godot base | Status | Notes |
|-----------|-----------|--------|-------|
| `travelempire.tres` theme + `Palette` | `Theme` | **New** | Single source of colour/spacing/type; `Palette` mirrors it for `_Draw` |
| `TopBar` | `PanelContainer` + `HBoxContainer` | **Rewrite** | Replaces the current label-soup bar |
| `SpeedControl` | `HBoxContainer` of toggle `Button`s | **New** | Segmented, one active state |
| `IconButton` | `Button` | **New** | Flat icon button with hover/pressed/tooltip |
| `MapView` | `Control` + `_Draw` | **Extend** | Keep custom drawing; add camera, styled lines, markers, badges |
| `MapCamera` | plain C# | **New** | Zoom/pan/fit/clamp + world↔screen; unit-testable |
| `MapOverlay` | `Control` | **New** | Anchored host for legend, compass, scale |
| `LegendCard` | `PanelContainer` | **New** | Seven rows, data-driven from the mode registry |
| `CompassScale` | `Control` | **New** | Compass rose + km scale bar that reacts to zoom |
| `ToastLayer` / `Toast` | `Control` / `PanelContainer` | **New** | Replaces `_statusLabel` |
| `SidePanel` | `PanelContainer` | **Rewrite** | City / Empire states, hosts tabs |
| `CityHero` | `TextureRect` + `Control` | **New** | Image slot with deterministic fallback |
| `StatGrid` / `StatRow` | `GridContainer` | **New** | Label/value pairs, `—` for unknown |
| `DemandBar` | `HBoxContainer` + `ProgressBar` | **New** | Compact and full variants |
| `SectionHeader` | `Label` (`H2` variation) | **New** | Replaces the ad-hoc `Section()` helper |
| `TabStrip` | `TabBar` (styled) | **Rewrite** | Not `TabContainer`; the panel owns body swapping |
| `ModeChipRow` / `ModeChip` | `HBoxContainer` / `Button` | **New** | Also reused as a map filter later |
| `DataTable` / `DataRow` / cells | `VBoxContainer` + `ScrollContainer` | **New** | Retires `Tree`; id-based selection |
| `PrimaryButton` | `Button` | **New** | Amber CTA with disabled + reason state |
| `TipFooter` | `PanelContainer` | **New** | Info glyph + wrapped hint text |
| `CityPanelViewModel`, `UiSelection` | plain C# | **New** | Pure, headless-testable presentation logic |
| `SimulationHost` | `Node` | **Reuse as-is** | Ticking and dirty-flag contract already correct |

**Opinion on `Tree`:** retire it. `Tree` resists per-cell iconography, right-alignment and row styling, and its rebuild/`Select(0)` cycle is the source of the selection fragility in `Main.cs`. Table sizes here are tens of rows, not thousands, so a `VBoxContainer` of styled rows is both cheaper to style and cheaper to reason about.

---

## 5. Gaps between MVP and mockup

### UI-only (no sim or content dependency)

| Gap | Resolution |
|-----|-----------|
| Calendar date `Apr 17, 2030` | Derive UI-side: content epoch date + `SimHours`. The sim clock stays `TickIndex` / `SimHours` |
| Day formatted `12.4` not `12.40` | Formatting |
| Running cost shown as `$/day` | Derive from operating cost per hour once exposed (below); no new sim concept |
| `High` / `Medium` / `Low` demand tags | Thresholds applied to the demand numbers the sim already computes |
| Mode glyphs, legend, compass, km scale, terrain-free polish | Presentation |
| Enum text (`InTransit`, `Train`) | Display-name mapping table in the presentation layer |
| Vehicle load `12/50` | Both fields already in `VehicleView` |

### Needs additive `Views` changes (small, no tick-loop or command impact)

| Gap | Change |
|-----|--------|
| City demand bars (Bus / Rail / Air, Local / Intercity / Air) | `DemandModel` already holds a full pairwise matrix but never leaves the sim. Add `CityDemandView` (per-mode intensity, plus local vs intercity split) to `CityView` |
| Running cost per day | `VehicleTypeView` exposes `OperatingCostPerKmMinor` only; add `OperatingCostPerHourMinor` and `MaxRangeKm` (both already on `VehicleTypeDefinition`) |
| Port yes/no + port legend icon | `City` has bus/rail/air flags only; add `HasSeaport` to the content model, entity and view |
| Per-route live load / revenue for the Routes table | Add aggregate fields to `RouteView` computed at snapshot time |

### Needs simulation work (defer; do not block this UI pass)

| Gap | Note |
|-----|------|
| Growth `+2.1 %` | No population dynamics exist; MVP explicitly excluded "fancy population growth". Render `—` until a growth model lands |
| Happiness `78 %` | No concept in the model. Needs a service-quality/coverage model. Render `—` |
| Economy `Strong` | Could ship as an authored content tier first, then become simulated. Content-authored value is acceptable and honest; a fabricated one is not |
| "Buy delivered to selected city" | `ApplyBuyVehicle` hard-codes `home = state.Cities[0].Id`. Needs an optional `HomeCityId` on `BuyVehicleCommand` |
| Unlocks implied by the tip footer | No progression system; keep the tip as static copy |

### Needs content or art

| Gap | Note |
|-----|------|
| Terrain, water, mountains | Requires a georeferenced background texture plus `bounds` in the map pack (C1) |
| City hero images | Six images. Deterministic gradient fallback ships first so the panel never looks broken |
| Mode / legend / vehicle icons | Small SVG set; needed as early as Phase A8 |
| Border, water and neighbour labels | Authored map-pack data (C2) |
| Catalogue breadth | One type per mode today; the mockup's Buy table needs ~3 per mode |
| City naming and coordinates | Current cities (Aurel, Port Haven, Forgeford…) sit on real-world Belarus coordinates; the mockup names a different, fictional set. Re-author both together with the terrain art so screenshots and docs agree |

---

## 6. Risks and mitigations

| Risk | Why it bites | Mitigation |
|------|--------------|-----------|
| **City-vs-global tab ambiguity** — the mockup shows Buy/Fleet/Routes under a city header, but the sim owns one company-wide fleet | Players assume "Buy" buys *for Rivermere*; the sim delivers to `Cities[0]` | Decide once (§2): tabs are global, context-*filtered*, with a visible `Show all` chip. Buy stays global until `BuyVehicleCommand` gains a home city |
| **Table selection loss on rebuild** — the existing failure mode: `Tree.Clear()` each dirty tick, then re-`Select(0)`, which re-enters `ItemSelected` | Mis-buys, mis-assignments, selection flicker at 4x | Hold selection as an id in `UiSelection`; rebuild only on structural change; update live numerics per-cell; cover with `CityPanelViewModel` tests |
| **Panel/map overflow** — long city names, six-digit populations, four-column tables in 360 px, future localisation | Clipped text, horizontal scrollbars, broken layout on 1366×768 | Fixed column ratios with clip + ellipsis + tooltip; `AutowrapMode` on prose; panel min 320 / max 420 px; map guaranteed ≥ 60 % width; QA at four resolutions (C7) |
| **Two input modes on one map** — inspect vs plan-route | The current code conflates them and silently mutates a draft on every city click | Explicit mode with a persistent map banner ("Planning bus route — click cities, Esc to cancel"), a different cursor, and stop-order badges on chosen cities |
| **Theme drift** — `_Draw` colours diverging from theme colours | Map and panel stop looking like one product | One `Palette` class is the single source; the theme resource reads from the same values; a review check forbids raw `Color(...)` literals outside `Palette` |
| **Scene refactor regression** — moving from imperative `BuildUi()` to `.tscn` files | Broken node references, silent null nodes | Refactor scene-by-scene behind working builds; commit per component; keep `SimulationHost` and command wiring untouched throughout |
| **Art dependency stalls Phase B** | Hero images and terrain are the long-lead items | Every art slot ships with a deterministic procedural fallback; Phase B must be reviewable with zero final art |
| **Perf at 4x** — full snapshot rebuild plus full panel rebuild per dirty frame | Stutter with a large fleet | Cache one snapshot per frame, region dirty flags, targeted cell updates, profile in C8 |
| **Mockup fidelity treated as pixel law** | Endless polish on a screen whose data is not simulated yet | Fidelity target is layout, hierarchy and labelling; art fidelity follows the content pipeline |

---

## 7. Non-goals for this pass

- **No simulation rewrite.** No changes to the tick loop, economy, demand formula or command semantics. Only additive, read-only fields on the view types listed in §5.
- **No new screens.** Finance/stats, settings, main menu, save/load, route detail and fleet detail get entry-point stubs only.
- **No infrastructure-building UI** (airports, stations, track laying) — the sim cannot build any of it.
- **No timetables, schedules or freight UI.**
- **No fabricated stats.** Happiness, growth and unlocks stay `—` until something computes them.
- **No 3D, shaders or animated terrain.** A calibrated 2D texture with data-driven labels is the ceiling.
- **No localisation pass.** Structure strings for it; do not translate.
- **No controller, touch or mobile layout;** mouse and keyboard at desktop resolutions only.
- **No accessibility programme** beyond colourblind-safe line patterns (principle 4) and readable contrast.
- **No multi-select, drag-and-drop assignment, or map-drawn route dragging.** Click-to-add stops only.
- **No custom UI framework.** Godot `Control` nodes, one `Theme` resource, plain scenes.

---

## Ownership

As phases land, update [Interface](06-interface.md) (scene map, component list, input modes) and [Data model](04-data-model.md) / [Simulation](05-simulation.md) if any §5 view change ships. Keep this file's phase tables checked off rather than rewriting them.
