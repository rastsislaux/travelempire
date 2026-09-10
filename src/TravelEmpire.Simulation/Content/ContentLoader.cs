using System.Text.Json;
using System.Text.Json.Serialization;

namespace TravelEmpire.Simulation.Content;

public static class ContentLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static MapPack LoadMapPack(string mapPackDirectory)
    {
        var packPath = Path.Combine(mapPackDirectory, "pack.json");
        var citiesPath = Path.Combine(mapPackDirectory, "cities.json");
        var railPath = Path.Combine(mapPackDirectory, "rail_edges.json");

        var packMeta = JsonSerializer.Deserialize<PackMeta>(File.ReadAllText(packPath), JsonOptions)
                       ?? throw new InvalidOperationException($"Invalid pack.json at {packPath}");
        var cities = JsonSerializer.Deserialize<List<CityDefinition>>(File.ReadAllText(citiesPath), JsonOptions)
                     ?? throw new InvalidOperationException($"Invalid cities.json at {citiesPath}");
        var rails = File.Exists(railPath)
            ? JsonSerializer.Deserialize<List<RailEdgeDefinition>>(File.ReadAllText(railPath), JsonOptions) ?? []
            : [];

        return new MapPack
        {
            Id = packMeta.Id,
            Name = packMeta.Name,
            Cities = cities,
            RailEdges = rails
        };
    }

    public static MapPack LoadMapPackFromJson(string packJson, string citiesJson, string railJson)
    {
        var packMeta = JsonSerializer.Deserialize<PackMeta>(packJson, JsonOptions)
                       ?? throw new InvalidOperationException("Invalid pack JSON");
        var cities = JsonSerializer.Deserialize<List<CityDefinition>>(citiesJson, JsonOptions)
                     ?? throw new InvalidOperationException("Invalid cities JSON");
        var rails = JsonSerializer.Deserialize<List<RailEdgeDefinition>>(railJson, JsonOptions) ?? [];

        return new MapPack
        {
            Id = packMeta.Id,
            Name = packMeta.Name,
            Cities = cities,
            RailEdges = rails
        };
    }

    public static VehicleCatalog LoadVehicleCatalog(string catalogPath)
    {
        var types = JsonSerializer.Deserialize<List<VehicleTypeDefinition>>(File.ReadAllText(catalogPath), JsonOptions)
                    ?? throw new InvalidOperationException($"Invalid vehicle catalog at {catalogPath}");
        return new VehicleCatalog { Types = types };
    }

    public static VehicleCatalog LoadVehicleCatalogFromJson(string json)
    {
        var types = JsonSerializer.Deserialize<List<VehicleTypeDefinition>>(json, JsonOptions)
                    ?? throw new InvalidOperationException("Invalid vehicle catalog JSON");
        return new VehicleCatalog { Types = types };
    }

    private sealed class PackMeta
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
    }
}
