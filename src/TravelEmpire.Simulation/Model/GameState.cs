using TravelEmpire.Simulation.Content;
using TravelEmpire.Simulation.Geography;
using TravelEmpire.Simulation.Views;

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

    public CityDemandView BuildCityDemand(City city, IReadOnlyList<City> cities, GameState state)
    {
        double local = 0;
        double intercity = 0;
        double air = 0;
        double busWeight = 0;
        double railWeight = 0;
        double airWeight = 0;

        foreach (var other in cities)
        {
            if (other.Id.Value == city.Id.Value) continue;
            var od = PassengersPerDay(city.Id, other.Id) + PassengersPerDay(other.Id, city.Id);
            if (od <= 0) continue;

            var distance = state.DistanceKm(city.Id, other.Id);
            var bothAirports = city.HasAirport && other.HasAirport;
            var longEnoughForAir = distance >= state.Config.MinAirDistanceKm;

            if (bothAirports && longEnoughForAir)
            {
                air += od * 0.35;
                airWeight += od * 0.35;
                var land = od * 0.65;
                if (distance < 100)
                    local += land;
                else
                    intercity += land;
                AccumulateModeWeights(city, other, land, ref busWeight, ref railWeight, ref airWeight);
            }
            else if (distance < 100)
            {
                local += od;
                AccumulateModeWeights(city, other, od, ref busWeight, ref railWeight, ref airWeight);
            }
            else
            {
                intercity += od;
                AccumulateModeWeights(city, other, od, ref busWeight, ref railWeight, ref airWeight);
            }
        }

        var modeTotal = busWeight + railWeight + airWeight;
        return new CityDemandView
        {
            LocalTransportPerDay = local,
            IntercityPerDay = intercity,
            AirTravelPerDay = air,
            BusShare = modeTotal <= 0 ? 0 : busWeight / modeTotal,
            RailShare = modeTotal <= 0 ? 0 : railWeight / modeTotal,
            AirShare = modeTotal <= 0 ? 0 : airWeight / modeTotal
        };
    }

    private static void AccumulateModeWeights(
        City a,
        City b,
        double weight,
        ref double bus,
        ref double rail,
        ref double air)
    {
        bus += weight;
        if (a.HasRailStation && b.HasRailStation)
            rail += weight * 0.7;
        if (a.HasAirport && b.HasAirport)
            air += weight * 0.4;
    }
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
    public IReadOnlyList<MapLabelDefinition> Labels { get; init; } = [];
    public string? BackgroundAsset { get; init; }
    public double? MinLatitude { get; init; }
    public double? MaxLatitude { get; init; }
    public double? MinLongitude { get; init; }
    public double? MaxLongitude { get; init; }
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
