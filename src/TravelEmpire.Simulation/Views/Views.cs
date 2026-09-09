namespace TravelEmpire.Simulation.Views;

public sealed class GameSnapshot
{
    public required string MapPackId { get; init; }
    public required string MapPackName { get; init; }
    public required long TickIndex { get; init; }
    public required double SimHours { get; init; }
    public required CompanyView? Company { get; init; }
    public required IReadOnlyList<CityView> Cities { get; init; }
    public required IReadOnlyList<RouteView> Routes { get; init; }
    public required IReadOnlyList<VehicleView> Vehicles { get; init; }
    public required IReadOnlyList<VehicleTypeView> Catalog { get; init; }
    public required IReadOnlyList<RailEdgeView> RailEdges { get; init; }
}

public sealed class CompanyView
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required long CashMinor { get; init; }
    public required long CumulativePassengers { get; init; }
    public required long CumulativeRevenueMinor { get; init; }
}

public sealed class CityView
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required int Population { get; init; }
    public required bool HasBusTerminal { get; init; }
    public required bool HasRailStation { get; init; }
    public required bool HasAirport { get; init; }
}

public sealed class RouteView
{
    public required long Id { get; init; }
    public required string Name { get; init; }
    public required TransportMode Mode { get; init; }
    public required IReadOnlyList<string> StopIds { get; init; }
    public required long PricePerKmMinor { get; init; }
    public required IReadOnlyList<long> VehicleIds { get; init; }
    public required bool IsActive { get; init; }
}

public sealed class VehicleView
{
    public required long Id { get; init; }
    public required string TypeId { get; init; }
    public required TransportMode Mode { get; init; }
    public required int CapacityPassengers { get; init; }
    public required double CruiseSpeedKmh { get; init; }
    public required long? AssignedRouteId { get; init; }
    public required VehicleState State { get; init; }
    public required string? AtCityId { get; init; }
    public required string? FromCityId { get; init; }
    public required string? ToCityId { get; init; }
    public required double Progress { get; init; }
    public required int OnboardPassengers { get; init; }
}

public sealed class VehicleTypeView
{
    public required string Id { get; init; }
    public required TransportMode Mode { get; init; }
    public required string DisplayName { get; init; }
    public required int CapacityPassengers { get; init; }
    public required double CruiseSpeedKmh { get; init; }
    public required long PurchaseCostMinor { get; init; }
    public required long OperatingCostPerKmMinor { get; init; }
}

public sealed class RailEdgeView
{
    public required string CityA { get; init; }
    public required string CityB { get; init; }
    public required double LengthKm { get; init; }
}
