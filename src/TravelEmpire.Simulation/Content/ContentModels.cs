namespace TravelEmpire.Simulation.Content;

public sealed class MapPack
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<CityDefinition> Cities { get; init; }
    public required IReadOnlyList<RailEdgeDefinition> RailEdges { get; init; }
}

public sealed class CityDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required int Population { get; init; }
    public bool HasBusTerminal { get; init; } = true;
    public bool HasRailStation { get; init; }
    public bool HasAirport { get; init; }
}

public sealed class RailEdgeDefinition
{
    public required string CityA { get; init; }
    public required string CityB { get; init; }
    public double? LengthKm { get; init; }
}

public sealed class VehicleTypeDefinition
{
    public required string Id { get; init; }
    public required TransportMode Mode { get; init; }
    public required string DisplayName { get; init; }
    public required int CapacityPassengers { get; init; }
    public required double CruiseSpeedKmh { get; init; }
    public required long PurchaseCostMinor { get; init; }
    public required long OperatingCostPerKmMinor { get; init; }
    public long OperatingCostPerHourMinor { get; init; }
    public double? MaxRangeKm { get; init; }
}

public sealed class VehicleCatalog
{
    public required IReadOnlyList<VehicleTypeDefinition> Types { get; init; }

    public VehicleTypeDefinition Get(VehicleTypeId id) =>
        Types.FirstOrDefault(t => t.Id == id.Value)
        ?? throw new KeyNotFoundException($"Unknown vehicle type '{id.Value}'.");

    public bool TryGet(VehicleTypeId id, out VehicleTypeDefinition type)
    {
        type = Types.FirstOrDefault(t => t.Id == id.Value)!;
        return type is not null;
    }
}
