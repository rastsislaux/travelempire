using TravelEmpire.Simulation;
using TravelEmpire.Simulation.Commands;
using TravelEmpire.Simulation.Geography;

namespace TravelEmpire.Simulation.Tests;

public class GeoMathTests
{
    [Fact]
    public void Haversine_same_point_is_zero()
    {
        Assert.Equal(0, GeoMath.HaversineKm(53.9, 27.57, 53.9, 27.57), 5);
    }

    [Fact]
    public void Haversine_known_distance_is_plausible()
    {
        // Aurel -> Port Haven roughly 180-220 km
        var km = GeoMath.HaversineKm(53.90, 27.57, 54.69, 25.28);
        Assert.InRange(km, 150, 250);
    }
}

public class CommandTests
{
    [Fact]
    public void BuyVehicle_deducts_cash_and_adds_fleet()
    {
        var sim = BuiltInContent.CreateDefaultGame();
        var before = sim.GetSnapshot().Company!.CashMinor;

        var result = sim.Apply(new BuyVehicleCommand { TypeId = new VehicleTypeId("bus.standard") });
        Assert.True(result.Success, result.ErrorMessage);

        var snap = sim.GetSnapshot();
        Assert.Single(snap.Vehicles);
        Assert.Equal(before - 25_000_000, snap.Company!.CashMinor);
    }

    [Fact]
    public void BuyVehicle_fails_without_funds()
    {
        var sim = BuiltInContent.CreateDefaultGame();
        while (sim.Apply(new BuyVehicleCommand { TypeId = new VehicleTypeId("air.regional_jet") }).Success)
        {
        }

        var result = sim.Apply(new BuyVehicleCommand { TypeId = new VehicleTypeId("air.regional_jet") });
        Assert.False(result.Success);
        Assert.Equal("vehicle.insufficient_funds", result.ErrorCode);
    }

    [Fact]
    public void CreateTrainRoute_without_rail_edge_fails()
    {
        var sim = BuiltInContent.CreateDefaultGame();
        var result = sim.Apply(new CreateRouteCommand
        {
            Name = "No Track",
            Mode = TransportMode.Train,
            Stops = [new CityId("city.capital"), new CityId("city.university")]
        });

        Assert.False(result.Success);
        Assert.Equal("route.rail_station", result.ErrorCode);
    }

    [Fact]
    public void CreateTrainRoute_missing_rail_link_fails()
    {
        var sim = BuiltInContent.CreateDefaultGame();
        // industrial and border both have stations but no direct edge
        var result = sim.Apply(new CreateRouteCommand
        {
            Name = "Missing Link",
            Mode = TransportMode.Train,
            Stops = [new CityId("city.industrial"), new CityId("city.border")]
        });

        Assert.False(result.Success);
        Assert.Equal("route.rail_edge", result.ErrorCode);
    }

    [Fact]
    public void CreateAirRoute_requires_airports()
    {
        var sim = BuiltInContent.CreateDefaultGame();
        var result = sim.Apply(new CreateRouteCommand
        {
            Name = "No Runway",
            Mode = TransportMode.Air,
            Stops = [new CityId("city.capital"), new CityId("city.industrial")]
        });

        Assert.False(result.Success);
        Assert.Equal("route.airport", result.ErrorCode);
    }

    [Fact]
    public void AssignVehicle_mode_mismatch_fails()
    {
        var sim = BuiltInContent.CreateDefaultGame();
        Assert.True(sim.Apply(new BuyVehicleCommand { TypeId = new VehicleTypeId("bus.standard") }).Success);
        Assert.True(sim.Apply(new CreateRouteCommand
        {
            Name = "Sky Link",
            Mode = TransportMode.Air,
            Stops = [new CityId("city.capital"), new CityId("city.port")]
        }).Success);

        var vehicleId = new VehicleId(sim.GetSnapshot().Vehicles[0].Id);
        var routeId = new RouteId(sim.GetSnapshot().Routes[0].Id);
        var result = sim.Apply(new AssignVehicleCommand { VehicleId = vehicleId, RouteId = routeId });

        Assert.False(result.Success);
        Assert.Equal("vehicle.mode_mismatch", result.ErrorCode);
    }

    [Fact]
    public void CreateBusRoute_between_cities_succeeds()
    {
        var sim = BuiltInContent.CreateDefaultGame();
        var result = sim.Apply(new CreateRouteCommand
        {
            Name = "Capital Express",
            Mode = TransportMode.Bus,
            Stops = [new CityId("city.capital"), new CityId("city.university")]
        });
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Single(sim.GetSnapshot().Routes);
    }
}

public class TickIntegrationTests
{
    [Fact]
    public void Bus_shuttle_generates_revenue_over_ticks()
    {
        var sim = BuiltInContent.CreateDefaultGame();
        Assert.True(sim.Apply(new BuyVehicleCommand { TypeId = new VehicleTypeId("bus.standard") }).Success);
        Assert.True(sim.Apply(new CreateRouteCommand
        {
            Name = "Aurel–Scholar",
            Mode = TransportMode.Bus,
            Stops = [new CityId("city.capital"), new CityId("city.university")],
            PricePerKm = Money.FromMajor(0.12m)
        }).Success);

        var snap = sim.GetSnapshot();
        Assert.True(sim.Apply(new AssignVehicleCommand
        {
            VehicleId = new VehicleId(snap.Vehicles[0].Id),
            RouteId = new RouteId(snap.Routes[0].Id)
        }).Success);

        var cashAfterBuy = sim.GetSnapshot().Company!.CashMinor;
        sim.Tick(2_000);
        var after = sim.GetSnapshot();

        Assert.True(after.Company!.CumulativePassengers > 0, "Expected passengers to be carried.");
        Assert.True(after.Company.CumulativeRevenueMinor > 0, "Expected revenue to be collected.");
        // Cash may be above or below post-purchase depending on op costs; revenue path must move money.
        Assert.NotEqual(cashAfterBuy, after.Company.CashMinor);
    }

    [Fact]
    public void Legal_train_and_air_routes_can_be_created()
    {
        var sim = BuiltInContent.CreateDefaultGame();
        Assert.True(sim.Apply(new CreateRouteCommand
        {
            Name = "Capital Rail",
            Mode = TransportMode.Train,
            Stops = [new CityId("city.capital"), new CityId("city.port")]
        }).Success);

        Assert.True(sim.Apply(new CreateRouteCommand
        {
            Name = "Coast Hopper",
            Mode = TransportMode.Air,
            Stops = [new CityId("city.capital"), new CityId("city.resort")]
        }).Success);
    }
}
