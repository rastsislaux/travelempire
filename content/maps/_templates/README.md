# Map pack template

Copy to `content/maps/{packId}/` and fill.

## pack.json

```json
{
  "id": "lt",
  "name": "Lithuania",
  "bbox": { "minLat": 53.8, "maxLat": 56.5, "minLon": 20.9, "maxLon": 26.9 },
  "background": "art/background.png"
}
```

## cities.json

Each city: `id`, `name`, `latitude`, `longitude`, `population`, `hasBusTerminal`, `hasRailStation`, `hasAirport`.  
Optional later: `hasPort`, `heroImage`, `regionId`.

## rail_edges.json

Undirected pairs `{ "cityA": "...", "cityB": "..." }` plus optional `lengthKm`.

## meta.json

Source URLs, retrieval date, license notes for population/coordinates/art.
