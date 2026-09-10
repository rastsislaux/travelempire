using TravelEmpire.Simulation;
using TravelEmpire.Simulation.Commands;
using TravelEmpire.Simulation.Content;
using TravelEmpire.Simulation.Geography;
using TravelEmpire.Simulation.Presentation;

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

        var result = sim.Apply(new BuyVehicleCommand { TypeId = new VehicleTypeId("bus.coach") });
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
    public void BuyVehicle_locked_tier_fails_until_cities_served()
    {
        var sim = BuiltInContent.CreateDefaultGame();
        var locked = sim.Apply(new BuyVehicleCommand { TypeId = new VehicleTypeId("bus.articulated") });
        Assert.False(locked.Success);
        Assert.Equal("vehicle.locked", locked.ErrorCode);

        Assert.True(sim.Apply(new CreateRouteCommand
        {
            Name = "A",
            Mode = TransportMode.Bus,
            Stops = [new CityId("city.capital"), new CityId("city.university")]
        }).Success);
        Assert.True(sim.Apply(new CreateRouteCommand
        {
            Name = "B",
            Mode = TransportMode.Bus,
            Stops = [new CityId("city.capital"), new CityId("city.port")]
        }).Success);
        Assert.True(sim.Apply(new CreateRouteCommand
        {
            Name = "C",
            Mode = TransportMode.Bus,
            Stops = [new CityId("city.port"), new CityId("city.border")]
        }).Success);

        Assert.True(sim.GetSnapshot().Company!.CitiesServed >= 3);
        var unlocked = sim.Apply(new BuyVehicleCommand { TypeId = new VehicleTypeId("bus.articulated") });
        Assert.True(unlocked.Success, unlocked.ErrorMessage);
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
        Assert.True(sim.Apply(new BuyVehicleCommand { TypeId = new VehicleTypeId("bus.coach") }).Success);
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
        Assert.True(sim.Apply(new BuyVehicleCommand { TypeId = new VehicleTypeId("bus.coach") }).Success);
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

public class ContentAndDemandTests
{
    [Fact]
    public void Catalog_has_three_types_per_mode()
    {
        var catalog = BuiltInContent.DefaultCatalog();
        Assert.Equal(3, catalog.Types.Count(t => t.Mode == TransportMode.Bus));
        Assert.Equal(3, catalog.Types.Count(t => t.Mode == TransportMode.Train));
        Assert.Equal(3, catalog.Types.Count(t => t.Mode == TransportMode.Air));
    }

    [Fact]
    public void Lithuania_pack_has_enough_cities_and_ports()
    {
        var pack = BuiltInContent.LithuaniaMapPack();
        Assert.True(pack.Cities.Count >= 8);
        Assert.Contains(pack.Cities, c => c.HasSeaport);
        Assert.Contains(pack.Cities, c => c.HasAirport);
        Assert.True(pack.RailEdges.Count >= 5);

        var sim = BuiltInContent.CreateGame("lithuania", "Baltijos Linijos");
        var snap = sim.GetSnapshot();
        Assert.Equal("lithuania", snap.MapPackId);
        Assert.Equal(pack.Cities.Count, snap.Cities.Count);
        Assert.Contains(snap.Cities, c => c.HasSeaport);
        Assert.NotNull(snap.Cities[0].Demand);
    }

    [Fact]
    public void MapPackRegistry_lists_aurelia_and_lithuania()
    {
        var packs = MapPackRegistry.ListPacks();
        Assert.Contains(packs, p => p.Id == "aurelia" && p.IsTutorial);
        Assert.Contains(packs, p => p.Id == "lithuania" && !p.IsTutorial);
    }

    [Fact]
    public void CityPanelViewModel_empire_and_city_states()
    {
        var sim = BuiltInContent.CreateDefaultGame();
        var snap = sim.GetSnapshot();
        var selection = new UiSelection();

        var empire = CityPanelViewModel.From(snap, selection);
        Assert.False(empire.HasSelection);
        Assert.Equal("Empire", empire.HeaderTitle);
        Assert.Equal("—", empire.GrowthLabel);

        selection.SelectedCityId = "city.port";
        selection.BuyModeFilter = TransportMode.Bus;
        var city = CityPanelViewModel.From(snap, selection);
        Assert.True(city.HasSelection);
        Assert.Equal("City — Port Haven", city.HeaderTitle);
        Assert.Equal("Yes", city.PortLabel);
        Assert.Contains(city.CatalogRows, r => r.Id == "bus.coach");
        Assert.DoesNotContain(city.CatalogRows, r => r.Mode != TransportMode.Bus);
    }

    [Fact]
    public void CityPanelViewModel_buy_disabled_when_unaffordable_or_locked()
    {
        var sim = BuiltInContent.CreateDefaultGame();
        var selection = new UiSelection
        {
            BuyModeFilter = TransportMode.Bus,
            SelectedCatalogTypeId = "bus.articulated"
        };
        var vm = CityPanelViewModel.From(sim.GetSnapshot(), selection);
        Assert.False(vm.BuyEnabled);
        Assert.Contains("cities", vm.BuyDisabledReason ?? "", StringComparison.OrdinalIgnoreCase);
    }
}
