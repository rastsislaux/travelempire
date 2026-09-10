using TravelEmpire.Simulation.Content;
using TravelEmpire.Simulation.Geography;
using TravelEmpire.Simulation.Model;

namespace TravelEmpire.Simulation.Model;

public sealed class DemandModel
{
    private readonly Dictionary<(string From, string To), double> _passengersPerDay = new();

    public static DemandModel Build(IReadOnlyList<City> cities, SimConfig config)
    {
        var model = new DemandModel();
        for (var i = 0; i < cities.Count; i++)
        {
            for (var j = 0; j < cities.Count; j++)
            {
                if (i == j) continue;
                var a = cities[i];
                var b = cities[j];
                var distance = GeoMath.HaversineKm(a.Latitude, a.Longitude, b.Latitude, b.Longitude);
                distance = Math.Max(distance, 1.0);
                var gravity = (double)a.Population * b.Population / Math.Pow(distance, config.DemandGravityExponent);
                var demand = config.DemandGravityScalar * gravity;
                model._passengersPerDay[(a.Id.Value, b.Id.Value)] = demand;
            }
        }

        return model;
    }

    public double PassengersPerDay(CityId from, CityId to) =>
        _passengersPerDay.TryGetValue((from.Value, to.Value), out var v) ? v : 0;
}

public sealed class GameState
{
    public required string MapPackId { get; init; }
    public required SimConfig Config { get; init; }
    public required VehicleCatalog Catalog { get; init; }
    public required IReadOnlyList<City> Cities { get; init; }
    public required IReadOnlyList<RailEdge> RailEdges { get; init; }
    public required DemandModel Demand { get; init; }
    public required GameClock Clock { get; init; }
    public Company? Company { get; set; }
    public long NextVehicleId { get; set; } = 1;
    public long NextRouteId { get; set; } = 1;
    public int RngSeed { get; set; }

    private Dictionary<string, City>? _cityById;
    private HashSet<(string, string)>? _railPairs;

    public City GetCity(CityId id)
    {
        _cityById ??= Cities.ToDictionary(c => c.Id.Value);
        return _cityById[id.Value];
    }

    public bool TryGetCity(CityId id, out City city)
    {
        _cityById ??= Cities.ToDictionary(c => c.Id.Value);
        return _cityById.TryGetValue(id.Value, out city!);
    }

    public bool HasRailLink(CityId a, CityId b)
    {
        _railPairs ??= RailEdges
            .SelectMany(e => new[] { (e.A.Value, e.B.Value), (e.B.Value, e.A.Value) })
            .ToHashSet();
        return _railPairs.Contains((a.Value, b.Value));
    }

    public double DistanceKm(CityId a, CityId b)
    {
        var edge = RailEdges.FirstOrDefault(e => e.Connects(a, b));
        if (edge is not null) return edge.LengthKm;
        var ca = GetCity(a);
        var cb = GetCity(b);
        return GeoMath.HaversineKm(ca.Latitude, ca.Longitude, cb.Latitude, cb.Longitude);
    }
}
