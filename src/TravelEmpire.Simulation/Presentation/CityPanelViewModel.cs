using TravelEmpire.Simulation.Views;

namespace TravelEmpire.Simulation.Presentation;

public enum MapInputMode
{
    Select,
    PlanRoute
}

public sealed class UiSelection
{
    public string? SelectedCityId { get; set; }
    public string? SelectedCatalogTypeId { get; set; }
    public long? SelectedVehicleId { get; set; }
    public long? SelectedRouteId { get; set; }
    public TransportMode BuyModeFilter { get; set; } = TransportMode.Bus;
    public TransportMode DraftMode { get; set; } = TransportMode.Bus;
    public bool ShowAllFleet { get; set; }
    public bool ShowAllRoutes { get; set; }
    public MapInputMode InputMode { get; set; } = MapInputMode.Select;
    public List<string> DraftStops { get; } = [];
    public int ActiveTab { get; set; }
}

public sealed class CatalogRowVm
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required TransportMode Mode { get; init; }
    public required int Capacity { get; init; }
    public required long RunningCostPerDayMinor { get; init; }
    public required long PriceMinor { get; init; }
    public required bool IsUnlocked { get; init; }
    public required bool CanAfford { get; init; }
    public string? LockReason { get; init; }
}

public sealed class FleetRowVm
{
    public required long Id { get; init; }
    public required string TypeId { get; init; }
    public required TransportMode Mode { get; init; }
    public required string StateLabel { get; init; }
    public required string RouteLabel { get; init; }
    public required string LoadLabel { get; init; }
}

public sealed class RouteRowVm
{
    public required long Id { get; init; }
    public required string Name { get; init; }
    public required TransportMode Mode { get; init; }
    public required string StopsLabel { get; init; }
    public required int VehicleCount { get; init; }
    public required string LoadLabel { get; init; }
}

public sealed class CityPanelViewModel
{
    public required bool HasSelection { get; init; }
    public required string HeaderTitle { get; init; }
    public required string? CityId { get; init; }
    public required string? CityName { get; init; }
    public required string PopulationLabel { get; init; }
    public required string GrowthLabel { get; init; }
    public required string HappinessLabel { get; init; }
    public required string EconomyLabel { get; init; }
    public required string AirportLabel { get; init; }
    public required string PortLabel { get; init; }
    public required string LocalDemandLabel { get; init; }
    public required string IntercityDemandLabel { get; init; }
    public required string AirDemandLabel { get; init; }
    public required double LocalDemandFill { get; init; }
    public required double IntercityDemandFill { get; init; }
    public required double AirDemandFill { get; init; }
    public required double BusShare { get; init; }
    public required double RailShare { get; init; }
    public required double AirShare { get; init; }
    public required IReadOnlyList<CatalogRowVm> CatalogRows { get; init; }
    public required IReadOnlyList<FleetRowVm> FleetRows { get; init; }
    public required IReadOnlyList<RouteRowVm> RouteRows { get; init; }
    public required bool BuyEnabled { get; init; }
    public required string? BuyDisabledReason { get; init; }
    public required string Tip { get; init; }
    public required string CompanySummary { get; init; }

    public static CityPanelViewModel From(GameSnapshot snap, UiSelection selection)
    {
        var city = selection.SelectedCityId is null
            ? null
            : snap.Cities.FirstOrDefault(c => c.Id == selection.SelectedCityId);

        var cash = snap.Company?.CashMinor ?? 0;
        var catalog = snap.Catalog
            .Where(t => t.Mode == selection.BuyModeFilter)
            .Select(t =>
            {
                string? lockReason = null;
                if (!t.IsUnlocked)
                {
                    var parts = new List<string>();
                    if (t.RequiresCitiesServed > 0)
                        parts.Add($"{t.RequiresCitiesServed} cities");
                    if (t.RequiresCumulativePassengers > 0)
                        parts.Add($"{t.RequiresCumulativePassengers:N0} pax");
                    lockReason = parts.Count == 0 ? "Locked" : $"Needs {string.Join(" · ", parts)}";
                }

                return new CatalogRowVm
                {
                    Id = t.Id,
                    DisplayName = t.DisplayName,
                    Mode = t.Mode,
                    Capacity = t.CapacityPassengers,
                    RunningCostPerDayMinor = t.OperatingCostPerHourMinor * 24,
                    PriceMinor = t.PurchaseCostMinor,
                    IsUnlocked = t.IsUnlocked,
                    CanAfford = cash >= t.PurchaseCostMinor,
                    LockReason = lockReason
                };
            })
            .ToList();

        var selectedType = catalog.FirstOrDefault(r => r.Id == selection.SelectedCatalogTypeId);
        string? buyReason = null;
        var buyEnabled = true;
        if (selectedType is null)
        {
            buyEnabled = false;
            buyReason = "Select a vehicle type.";
        }
        else if (!selectedType.IsUnlocked)
        {
            buyEnabled = false;
            buyReason = selectedType.LockReason ?? "Locked.";
        }
        else if (!selectedType.CanAfford)
        {
            buyEnabled = false;
            buyReason = "Insufficient funds.";
        }

        var fleet = snap.Vehicles.AsEnumerable();
        var routes = snap.Routes.AsEnumerable();
        if (city is not null && !selection.ShowAllFleet)
        {
            fleet = fleet.Where(v =>
                v.AtCityId == city.Id
                || v.FromCityId == city.Id
                || v.ToCityId == city.Id
                || (v.AssignedRouteId is long rid
                    && snap.Routes.FirstOrDefault(r => r.Id == rid)?.StopIds.Contains(city.Id) == true));
        }

        if (city is not null && !selection.ShowAllRoutes)
            routes = routes.Where(r => r.StopIds.Contains(city.Id));

        var demand = city?.Demand;
        var maxDemand = Math.Max(1.0,
            Math.Max(demand?.LocalTransportPerDay ?? 0,
                Math.Max(demand?.IntercityPerDay ?? 0, demand?.AirTravelPerDay ?? 0)));

        var company = snap.Company;
        var summary = company is null
            ? "No company yet."
            : $"{company.Name}\nCash ${company.CashMinor / 100.0:N0}\n" +
              $"Fleet {snap.Vehicles.Count} · Routes {snap.Routes.Count}\n" +
              $"Cities served {company.CitiesServed} · Pax {company.CumulativePassengers:N0}";

        return new CityPanelViewModel
        {
            HasSelection = city is not null,
            HeaderTitle = city is null ? "Empire" : $"City — {city.Name}",
            CityId = city?.Id,
            CityName = city?.Name,
            PopulationLabel = city is null ? "—" : $"{city.Population:N0}",
            GrowthLabel = "—",
            HappinessLabel = "—",
            EconomyLabel = "—",
            AirportLabel = city is null ? "—" : (city.HasAirport ? "Yes" : "No"),
            PortLabel = city is null ? "—" : (city.HasSeaport ? "Yes" : "No"),
            LocalDemandLabel = Qualitative(demand?.LocalTransportPerDay ?? 0, maxDemand),
            IntercityDemandLabel = Qualitative(demand?.IntercityPerDay ?? 0, maxDemand),
            AirDemandLabel = Qualitative(demand?.AirTravelPerDay ?? 0, maxDemand),
            LocalDemandFill = (demand?.LocalTransportPerDay ?? 0) / maxDemand,
            IntercityDemandFill = (demand?.IntercityPerDay ?? 0) / maxDemand,
            AirDemandFill = (demand?.AirTravelPerDay ?? 0) / maxDemand,
            BusShare = demand?.BusShare ?? 0,
            RailShare = demand?.RailShare ?? 0,
            AirShare = demand?.AirShare ?? 0,
            CatalogRows = catalog,
            FleetRows = fleet.Select(v => new FleetRowVm
            {
                Id = v.Id,
                TypeId = v.TypeId,
                Mode = v.Mode,
                StateLabel = FormatState(v.State),
                RouteLabel = v.AssignedRouteId is null ? "—" : $"#{v.AssignedRouteId}",
                LoadLabel = $"{v.OnboardPassengers}/{v.CapacityPassengers}"
            }).ToList(),
            RouteRows = routes.Select(r => new RouteRowVm
            {
                Id = r.Id,
                Name = r.Name,
                Mode = r.Mode,
                StopsLabel = string.Join(" → ", r.StopIds.Select(ShortCity)),
                VehicleCount = r.VehicleIds.Count,
                LoadLabel = r.LiveCapacityPassengers <= 0
                    ? "—"
                    : $"{r.LiveLoadPassengers}/{r.LiveCapacityPassengers}"
            }).ToList(),
            BuyEnabled = buyEnabled,
            BuyDisabledReason = buyReason,
            Tip = BuildTip(snap, selection),
            CompanySummary = summary
        };
    }

    private static string BuildTip(GameSnapshot snap, UiSelection selection)
    {
        if (selection.InputMode == MapInputMode.PlanRoute)
            return "Planning route — click cities in order, then Create. Esc cancels.";

        var locked = snap.Catalog.FirstOrDefault(t => !t.IsUnlocked);
        if (locked is not null && (snap.Company?.CitiesServed ?? 0) < locked.RequiresCitiesServed)
            return $"Serve {locked.RequiresCitiesServed} cities to unlock {locked.DisplayName}.";

        return "Connect cities, assign vehicles, and grow ridership to unlock higher tiers.";
    }

    private static string Qualitative(double value, double max)
    {
        if (max <= 0 || value <= 0) return "Low";
        var ratio = value / max;
        if (ratio >= 0.66) return "High";
        if (ratio >= 0.33) return "Medium";
        return "Low";
    }

    private static string FormatState(VehicleState state) => state switch
    {
        VehicleState.Idle => "Idle",
        VehicleState.InTransit => "In transit",
        VehicleState.Turnaround => "Turnaround",
        _ => state.ToString()
    };

    private static string ShortCity(string cityId) =>
        cityId.StartsWith("city.", StringComparison.Ordinal) ? cityId["city.".Length..] : cityId;
}
