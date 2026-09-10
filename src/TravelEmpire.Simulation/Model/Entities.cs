namespace TravelEmpire.Simulation.Model;

public sealed class City
{
    public required CityId Id { get; init; }
    public required string Name { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required int Population { get; init; }
    public bool HasBusTerminal { get; init; } = true;
    public bool HasRailStation { get; init; }
    public bool HasAirport { get; init; }
    public bool HasSeaport { get; init; }
}

public sealed class RailEdge
{
    public required CityId A { get; init; }
    public required CityId B { get; init; }
    public required double LengthKm { get; init; }

    public bool Connects(CityId x, CityId y) =>
        (A.Value == x.Value && B.Value == y.Value) || (A.Value == y.Value && B.Value == x.Value);
}

public sealed class Vehicle
{
    public required VehicleId Id { get; init; }
    public required VehicleTypeId TypeId { get; init; }
    public required TransportMode Mode { get; init; }
    public required int CapacityPassengers { get; init; }
    public required double CruiseSpeedKmh { get; init; }
    public required Money OperatingCostPerKm { get; init; }
    public required Money OperatingCostPerHour { get; init; }
    public double? MaxRangeKm { get; init; }
    public RouteId? AssignedRouteId { get; set; }
    public VehicleState State { get; set; } = VehicleState.Idle;
    public CityId? AtCityId { get; set; }
    public CityId? FromCityId { get; set; }
    public CityId? ToCityId { get; set; }
    public double Progress { get; set; }
    public double TurnaroundHoursRemaining { get; set; }
    public int OnboardPassengers { get; set; }
    public Money CollectedFarePerPassenger { get; set; }
}

public sealed class Route
{
    public required RouteId Id { get; init; }
    public required string Name { get; set; }
    public required TransportMode Mode { get; init; }
    public required List<CityId> Stops { get; set; }
    public required Money PricePerKm { get; set; }
    public HashSet<long> VehicleIds { get; } = [];
    public bool IsActive { get; set; } = true;

    /// <summary>Outbound index into Stops for next departure after turnaround; direction +1 or -1 for out-and-back.</summary>
    public Dictionary<long, VehicleRouteProgress> VehicleProgress { get; } = new();
}

public sealed class VehicleRouteProgress
{
    public int StopIndex { get; set; }
    public int Direction { get; set; } = 1;
}

public sealed class Company
{
    public required CompanyId Id { get; init; }
    public required string Name { get; init; }
    public Money Cash { get; set; }
    public List<Vehicle> Vehicles { get; } = [];
    public List<Route> Routes { get; } = [];
    public long CumulativePassengers { get; set; }
    public Money CumulativeRevenue { get; set; }
    public HashSet<string> ServedCityIds { get; } = [];

    public int CitiesServed => ServedCityIds.Count;
}

public sealed class GameClock
{
    public long TickIndex { get; set; }
    public double SimHours { get; set; }
}
