using TravelEmpire.Simulation.Commands;
using TravelEmpire.Simulation.Content;
using TravelEmpire.Simulation.Geography;
using TravelEmpire.Simulation.Model;
using TravelEmpire.Simulation.Views;

namespace TravelEmpire.Simulation;

public sealed class Simulation
{
    private readonly MapPack _mapPack;
    private readonly VehicleCatalog _catalog;
    private readonly SimConfig _config;
    private GameState? _state;
    private string? _mapPackName;

    public Simulation(MapPack mapPack, VehicleCatalog catalog, SimConfig? config = null)
    {
        _mapPack = mapPack;
        _catalog = catalog;
        _config = config ?? new SimConfig();
        _mapPackName = mapPack.Name;
    }

    public bool HasGame => _state?.Company is not null;

    public CommandResult Apply(ICommand command) => command switch
    {
        NewGameCommand c => ApplyNewGame(c),
        BuyVehicleCommand c => ApplyBuyVehicle(c),
        CreateRouteCommand c => ApplyCreateRoute(c),
        UpdateRouteCommand c => ApplyUpdateRoute(c),
        AssignVehicleCommand c => ApplyAssignVehicle(c),
        UnassignVehicleCommand c => ApplyUnassignVehicle(c),
        GrantCashCommand c => ApplyGrantCash(c),
        _ => CommandResult.Fail("command.unknown", $"Unknown command type {command.GetType().Name}.")
    };

    public void Tick(int tickCount = 1)
    {
        if (_state?.Company is null) return;
        for (var i = 0; i < tickCount; i++)
            TickOnce(_state);
    }

    public GameSnapshot GetSnapshot()
    {
        if (_state is null)
        {
            return new GameSnapshot
            {
                MapPackId = _mapPack.Id,
                MapPackName = _mapPackName ?? _mapPack.Name,
                TickIndex = 0,
                SimHours = 0,
                Company = null,
                Cities = _mapPack.Cities.Select(c => new CityView
                {
                    Id = c.Id,
                    Name = c.Name,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude,
                    Population = c.Population,
                    HasBusTerminal = c.HasBusTerminal,
                    HasRailStation = c.HasRailStation,
                    HasAirport = c.HasAirport
                }).ToList(),
                Routes = [],
                Vehicles = [],
                Catalog = _catalog.Types.Select(ToTypeView).ToList(),
                RailEdges = _mapPack.RailEdges.Select(e => new RailEdgeView
                {
                    CityA = e.CityA,
                    CityB = e.CityB,
                    LengthKm = e.LengthKm ?? 0
                }).ToList()
            };
        }

        var company = _state.Company;
        return new GameSnapshot
        {
            MapPackId = _state.MapPackId,
            MapPackName = _mapPackName ?? _mapPack.Name,
            TickIndex = _state.Clock.TickIndex,
            SimHours = _state.Clock.SimHours,
            Company = company is null ? null : new CompanyView
            {
                Id = company.Id.Value,
                Name = company.Name,
                CashMinor = company.Cash.MinorUnits,
                CumulativePassengers = company.CumulativePassengers,
                CumulativeRevenueMinor = company.CumulativeRevenue.MinorUnits
            },
            Cities = _state.Cities.Select(c => new CityView
            {
                Id = c.Id.Value,
                Name = c.Name,
                Latitude = c.Latitude,
                Longitude = c.Longitude,
                Population = c.Population,
                HasBusTerminal = c.HasBusTerminal,
                HasRailStation = c.HasRailStation,
                HasAirport = c.HasAirport
            }).ToList(),
            Routes = company?.Routes.Select(r => new RouteView
            {
                Id = r.Id.Value,
                Name = r.Name,
                Mode = r.Mode,
                StopIds = r.Stops.Select(s => s.Value).ToList(),
                PricePerKmMinor = r.PricePerKm.MinorUnits,
                VehicleIds = r.VehicleIds.ToList(),
                IsActive = r.IsActive
            }).ToList() ?? [],
            Vehicles = company?.Vehicles.Select(v => new VehicleView
            {
                Id = v.Id.Value,
                TypeId = v.TypeId.Value,
                Mode = v.Mode,
                CapacityPassengers = v.CapacityPassengers,
                CruiseSpeedKmh = v.CruiseSpeedKmh,
                AssignedRouteId = v.AssignedRouteId?.Value,
                State = v.State,
                AtCityId = v.AtCityId?.Value,
                FromCityId = v.FromCityId?.Value,
                ToCityId = v.ToCityId?.Value,
                Progress = v.Progress,
                OnboardPassengers = v.OnboardPassengers
            }).ToList() ?? [],
            Catalog = _catalog.Types.Select(ToTypeView).ToList(),
            RailEdges = _state.RailEdges.Select(e => new RailEdgeView
            {
                CityA = e.A.Value,
                CityB = e.B.Value,
                LengthKm = e.LengthKm
            }).ToList()
        };
    }

    public CommandResult ValidateCreateRoute(TransportMode mode, IReadOnlyList<CityId> stops)
    {
        EnsureGame();
        return RouteRules.ValidateStops(_state!, mode, stops);
    }

    private CommandResult ApplyNewGame(NewGameCommand command)
    {
        var cities = _mapPack.Cities.Select(c => new City
        {
            Id = new CityId(c.Id),
            Name = c.Name,
            Latitude = c.Latitude,
            Longitude = c.Longitude,
            Population = c.Population,
            HasBusTerminal = c.HasBusTerminal,
            HasRailStation = c.HasRailStation,
            HasAirport = c.HasAirport
        }).ToList();

        var rails = _mapPack.RailEdges.Select(e =>
        {
            var a = new CityId(e.CityA);
            var b = new CityId(e.CityB);
            var length = e.LengthKm ?? GeoMath.HaversineKm(
                cities.First(x => x.Id.Value == e.CityA).Latitude,
                cities.First(x => x.Id.Value == e.CityA).Longitude,
                cities.First(x => x.Id.Value == e.CityB).Latitude,
                cities.First(x => x.Id.Value == e.CityB).Longitude);
            return new RailEdge { A = a, B = b, LengthKm = length };
        }).ToList();

        _state = new GameState
        {
            MapPackId = _mapPack.Id,
            Config = _config,
            Catalog = _catalog,
            Cities = cities,
            RailEdges = rails,
            Demand = DemandModel.Build(cities, _config),
            Clock = new GameClock(),
            RngSeed = command.Seed,
            Company = new Company
            {
                Id = new CompanyId("player"),
                Name = string.IsNullOrWhiteSpace(command.CompanyName) ? "TravelEmpire Co." : command.CompanyName.Trim(),
                Cash = _config.StartingCash
            }
        };
        _mapPackName = _mapPack.Name;
        return CommandResult.Ok();
    }

    private CommandResult ApplyBuyVehicle(BuyVehicleCommand command)
    {
        var guard = EnsureGame();
        if (!guard.Success) return guard;
        var state = _state!;
        var company = state.Company!;

        if (!state.Catalog.TryGet(command.TypeId, out var type))
            return CommandResult.Fail("vehicle.unknown_type", $"Unknown vehicle type '{command.TypeId}'.");

        var cost = new Money(type.PurchaseCostMinor);
        if (company.Cash < cost)
            return CommandResult.Fail("vehicle.insufficient_funds",
                $"Need {cost} but only have {company.Cash}.");

        company.Cash -= cost;
        var home = state.Cities[0].Id;
        var vehicle = new Vehicle
        {
            Id = new VehicleId(state.NextVehicleId++),
            TypeId = new VehicleTypeId(type.Id),
            Mode = type.Mode,
            CapacityPassengers = type.CapacityPassengers,
            CruiseSpeedKmh = type.CruiseSpeedKmh,
            OperatingCostPerKm = new Money(type.OperatingCostPerKmMinor),
            OperatingCostPerHour = new Money(type.OperatingCostPerHourMinor),
            MaxRangeKm = type.MaxRangeKm,
            State = VehicleState.Idle,
            AtCityId = home
        };
        company.Vehicles.Add(vehicle);
        return CommandResult.Ok();
    }

    private CommandResult ApplyCreateRoute(CreateRouteCommand command)
    {
        var guard = EnsureGame();
        if (!guard.Success) return guard;
        var state = _state!;
        var company = state.Company!;

        var stops = command.Stops.ToList();
        var validation = RouteRules.ValidateStops(state, command.Mode, stops);
        if (!validation.Success) return validation;

        if (string.IsNullOrWhiteSpace(command.Name))
            return CommandResult.Fail("route.name", "Route name is required.");

        var route = new Route
        {
            Id = new RouteId(state.NextRouteId++),
            Name = command.Name.Trim(),
            Mode = command.Mode,
            Stops = stops,
            PricePerKm = command.PricePerKm ?? state.Config.DefaultPricePerKm,
            IsActive = true
        };
        company.Routes.Add(route);
        return CommandResult.Ok();
    }

    private CommandResult ApplyUpdateRoute(UpdateRouteCommand command)
    {
        var guard = EnsureGame();
        if (!guard.Success) return guard;
        var company = _state!.Company!;
        var route = company.Routes.FirstOrDefault(r => r.Id.Value == command.RouteId.Value);
        if (route is null)
            return CommandResult.Fail("route.not_found", $"Route {command.RouteId} not found.");

        if (command.Stops is not null)
        {
            var validation = RouteRules.ValidateStops(_state, route.Mode, command.Stops);
            if (!validation.Success) return validation;
            route.Stops = command.Stops.ToList();
        }

        if (command.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(command.Name))
                return CommandResult.Fail("route.name", "Route name is required.");
            route.Name = command.Name.Trim();
        }

        if (command.PricePerKm is not null)
            route.PricePerKm = command.PricePerKm.Value;

        return CommandResult.Ok();
    }

    private CommandResult ApplyAssignVehicle(AssignVehicleCommand command)
    {
        var guard = EnsureGame();
        if (!guard.Success) return guard;
        var company = _state!.Company!;

        var vehicle = company.Vehicles.FirstOrDefault(v => v.Id.Value == command.VehicleId.Value);
        if (vehicle is null)
            return CommandResult.Fail("vehicle.not_found", $"Vehicle {command.VehicleId} not found.");

        var route = company.Routes.FirstOrDefault(r => r.Id.Value == command.RouteId.Value);
        if (route is null)
            return CommandResult.Fail("route.not_found", $"Route {command.RouteId} not found.");

        if (vehicle.Mode != route.Mode)
            return CommandResult.Fail("vehicle.mode_mismatch",
                $"Cannot assign {vehicle.Mode} vehicle to {route.Mode} route.");

        if (vehicle.AssignedRouteId is not null && vehicle.AssignedRouteId.Value.Value != route.Id.Value)
        {
            var previous = company.Routes.FirstOrDefault(r => r.Id.Value == vehicle.AssignedRouteId.Value.Value);
            previous?.VehicleIds.Remove(vehicle.Id.Value);
            previous?.VehicleProgress.Remove(vehicle.Id.Value);
        }

        vehicle.AssignedRouteId = route.Id;
        route.VehicleIds.Add(vehicle.Id.Value);

        var startCity = route.Stops[0];
        vehicle.AtCityId = startCity;
        vehicle.FromCityId = null;
        vehicle.ToCityId = null;
        vehicle.Progress = 0;
        vehicle.OnboardPassengers = 0;
        vehicle.State = VehicleState.Turnaround;
        vehicle.TurnaroundHoursRemaining = 0;
        route.VehicleProgress[vehicle.Id.Value] = new VehicleRouteProgress { StopIndex = 0, Direction = 1 };

        return CommandResult.Ok();
    }

    private CommandResult ApplyUnassignVehicle(UnassignVehicleCommand command)
    {
        var guard = EnsureGame();
        if (!guard.Success) return guard;
        var company = _state!.Company!;
        var vehicle = company.Vehicles.FirstOrDefault(v => v.Id.Value == command.VehicleId.Value);
        if (vehicle is null)
            return CommandResult.Fail("vehicle.not_found", $"Vehicle {command.VehicleId} not found.");

        if (vehicle.AssignedRouteId is not null)
        {
            var route = company.Routes.FirstOrDefault(r => r.Id.Value == vehicle.AssignedRouteId.Value.Value);
            route?.VehicleIds.Remove(vehicle.Id.Value);
            route?.VehicleProgress.Remove(vehicle.Id.Value);
        }

        vehicle.AssignedRouteId = null;
        vehicle.State = VehicleState.Idle;
        vehicle.FromCityId = null;
        vehicle.ToCityId = null;
        vehicle.Progress = 0;
        vehicle.OnboardPassengers = 0;
        vehicle.TurnaroundHoursRemaining = 0;
        return CommandResult.Ok();
    }

    private CommandResult ApplyGrantCash(GrantCashCommand command)
    {
        var guard = EnsureGame();
        if (!guard.Success) return guard;
        _state!.Company!.Cash += command.Amount;
        return CommandResult.Ok();
    }

    private CommandResult EnsureGame()
    {
        if (_state?.Company is null)
            return CommandResult.Fail("game.not_started", "Start a new game first.");
        return CommandResult.Ok();
    }

    private static void TickOnce(GameState state)
    {
        var company = state.Company!;
        var dt = state.Config.HoursPerTick;
        state.Clock.TickIndex++;
        state.Clock.SimHours += dt;

        foreach (var vehicle in company.Vehicles)
        {
            if (vehicle.AssignedRouteId is null) continue;
            var route = company.Routes.FirstOrDefault(r => r.Id.Value == vehicle.AssignedRouteId.Value.Value);
            if (route is null || !route.IsActive) continue;

            if (vehicle.State == VehicleState.Turnaround || vehicle.State == VehicleState.Idle)
            {
                vehicle.TurnaroundHoursRemaining -= dt;
                if (vehicle.TurnaroundHoursRemaining > 0) continue;
                Depart(state, company, route, vehicle);
                continue;
            }

            if (vehicle.State != VehicleState.InTransit || vehicle.FromCityId is null || vehicle.ToCityId is null)
                continue;

            var edgeKm = state.DistanceKm(vehicle.FromCityId.Value, vehicle.ToCityId.Value);
            edgeKm = Math.Max(edgeKm, 0.1);
            var distanceThisTick = vehicle.CruiseSpeedKmh * dt;
            var progressDelta = distanceThisTick / edgeKm;
            var previousProgress = vehicle.Progress;
            vehicle.Progress = Math.Min(1.0, vehicle.Progress + progressDelta);

            var traveledFraction = vehicle.Progress - previousProgress;
            var traveledKm = traveledFraction * edgeKm;
            var opCost = vehicle.OperatingCostPerKm * traveledKm
                         + vehicle.OperatingCostPerHour * dt;
            company.Cash -= opCost;

            if (vehicle.Progress < 1.0) continue;

            Arrive(state, company, route, vehicle);
        }
    }

    private static void Depart(GameState state, Company company, Route route, Vehicle vehicle)
    {
        if (!route.VehicleProgress.TryGetValue(vehicle.Id.Value, out var progress))
        {
            progress = new VehicleRouteProgress { StopIndex = 0, Direction = 1 };
            route.VehicleProgress[vehicle.Id.Value] = progress;
        }

        var fromIndex = progress.StopIndex;
        var nextIndex = fromIndex + progress.Direction;
        if (nextIndex < 0 || nextIndex >= route.Stops.Count)
        {
            progress.Direction *= -1;
            nextIndex = fromIndex + progress.Direction;
        }

        if (nextIndex < 0 || nextIndex >= route.Stops.Count || nextIndex == fromIndex)
            return;

        var from = route.Stops[fromIndex];
        var to = route.Stops[nextIndex];
        Board(state, route, vehicle, from, to);

        vehicle.AtCityId = null;
        vehicle.FromCityId = from;
        vehicle.ToCityId = to;
        vehicle.Progress = 0;
        vehicle.State = VehicleState.InTransit;
        progress.StopIndex = nextIndex;
    }

    private static void Arrive(GameState state, Company company, Route route, Vehicle vehicle)
    {
        var destination = vehicle.ToCityId!.Value;
        if (vehicle.OnboardPassengers > 0)
        {
            var revenue = vehicle.CollectedFarePerPassenger * vehicle.OnboardPassengers;
            company.Cash += revenue;
            company.CumulativeRevenue += revenue;
            company.CumulativePassengers += vehicle.OnboardPassengers;
            vehicle.OnboardPassengers = 0;
            vehicle.CollectedFarePerPassenger = Money.Zero;
        }

        vehicle.AtCityId = destination;
        vehicle.FromCityId = null;
        vehicle.ToCityId = null;
        vehicle.Progress = 0;
        vehicle.State = VehicleState.Turnaround;
        vehicle.TurnaroundHoursRemaining = state.Config.TurnaroundHours(route.Mode);
    }

    private static void Board(GameState state, Route route, Vehicle vehicle, CityId from, CityId to)
    {
        var distance = state.DistanceKm(from, to);
        var fare = FareFor(state.Config, route.PricePerKm, distance);
        var priceMajor = route.PricePerKm.ToMajor();
        var refPrice = (decimal)state.Config.ReferencePricePerKmMajor;
        var fareMultiplier = refPrice <= 0 ? 1.0 : (double)(refPrice / Math.Max(priceMajor, 0.01m));
        fareMultiplier = Math.Clamp(fareMultiplier, 0.25, 1.75);

        var available = state.Demand.PassengersPerDay(from, to)
                        * (state.Config.HoursPerTick / 24.0)
                        * fareMultiplier;
        // Scale up boarding window: vehicles board for the demand accumulated over roughly turnaround+leg isn't tracked;
        // use a practical multiplier so infrequent vehicles still fill (demand per departure ~ half-day slice).
        available *= Math.Max(1.0, 6.0 / Math.Max(state.Config.HoursPerTick, 0.01));

        var boarded = (int)Math.Floor(Math.Min(available, vehicle.CapacityPassengers));
        boarded = Math.Max(0, boarded);
        vehicle.OnboardPassengers = boarded;
        vehicle.CollectedFarePerPassenger = fare;
    }

    private static Money FareFor(SimConfig config, Money pricePerKm, double distanceKm)
    {
        var raw = pricePerKm * distanceKm;
        return raw < config.MinFare ? config.MinFare : raw;
    }

    private static VehicleTypeView ToTypeView(VehicleTypeDefinition type) => new()
    {
        Id = type.Id,
        Mode = type.Mode,
        DisplayName = type.DisplayName,
        CapacityPassengers = type.CapacityPassengers,
        CruiseSpeedKmh = type.CruiseSpeedKmh,
        PurchaseCostMinor = type.PurchaseCostMinor,
        OperatingCostPerKmMinor = type.OperatingCostPerKmMinor
    };
}
