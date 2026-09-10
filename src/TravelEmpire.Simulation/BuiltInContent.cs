using TravelEmpire.Simulation.Content;

namespace TravelEmpire.Simulation;

public static class BuiltInContent
{
    public static IEnumerable<MapPack> AllMapPacks()
    {
        yield return AureliaMapPack();
        yield return LithuaniaMapPack();
    }

    public static MapPack AureliaMapPack() => ContentLoader.LoadMapPackFromJson(
        """
        { "id": "aurelia", "name": "Republic of Aurelia", "minLatitude": 52.0, "maxLatitude": 56.2, "minLongitude": 23.0, "maxLongitude": 31.5 }
        """,
        """
        [
          { "id": "city.capital", "name": "Aurel", "latitude": 53.90, "longitude": 27.57, "population": 1800000, "hasBusTerminal": true, "hasRailStation": true, "hasAirport": true, "hasSeaport": false },
          { "id": "city.port", "name": "Port Haven", "latitude": 54.69, "longitude": 25.28, "population": 900000, "hasBusTerminal": true, "hasRailStation": true, "hasAirport": true, "hasSeaport": true },
          { "id": "city.industrial", "name": "Forgeford", "latitude": 52.43, "longitude": 31.00, "population": 600000, "hasBusTerminal": true, "hasRailStation": true, "hasAirport": false, "hasSeaport": false },
          { "id": "city.university", "name": "Scholar's Rest", "latitude": 53.12, "longitude": 26.02, "population": 250000, "hasBusTerminal": true, "hasRailStation": false, "hasAirport": false, "hasSeaport": false },
          { "id": "city.border", "name": "Westgate", "latitude": 53.68, "longitude": 23.83, "population": 180000, "hasBusTerminal": true, "hasRailStation": true, "hasAirport": false, "hasSeaport": false },
          { "id": "city.resort", "name": "Silver Coast", "latitude": 55.72, "longitude": 27.32, "population": 120000, "hasBusTerminal": true, "hasRailStation": false, "hasAirport": true, "hasSeaport": false }
        ]
        """,
        """
        [
          { "cityA": "city.capital", "cityB": "city.port" },
          { "cityA": "city.capital", "cityB": "city.industrial" },
          { "cityA": "city.capital", "cityB": "city.border" },
          { "cityA": "city.port", "cityB": "city.border" }
        ]
        """);

    public static MapPack LithuaniaMapPack() => ContentLoader.LoadMapPackFromJson(
        """
        {
          "id": "lithuania",
          "name": "Republic of Lithuania",
          "minLatitude": 53.8,
          "maxLatitude": 56.5,
          "minLongitude": 20.8,
          "maxLongitude": 26.9,
          "backgroundAsset": "art/background.png"
        }
        """,
        """
        [
          { "id": "city.vilnius", "name": "Vilnius", "latitude": 54.6872, "longitude": 25.2797, "population": 581000, "hasBusTerminal": true, "hasRailStation": true, "hasAirport": true, "hasSeaport": false },
          { "id": "city.kaunas", "name": "Kaunas", "latitude": 54.8985, "longitude": 23.9036, "population": 299000, "hasBusTerminal": true, "hasRailStation": true, "hasAirport": true, "hasSeaport": false },
          { "id": "city.klaipeda", "name": "Klaipėda", "latitude": 55.7033, "longitude": 21.1443, "population": 152000, "hasBusTerminal": true, "hasRailStation": true, "hasAirport": false, "hasSeaport": true },
          { "id": "city.siauliai", "name": "Šiauliai", "latitude": 55.9349, "longitude": 23.3137, "population": 101000, "hasBusTerminal": true, "hasRailStation": true, "hasAirport": false, "hasSeaport": false },
          { "id": "city.panevezys", "name": "Panevėžys", "latitude": 55.7344, "longitude": 24.3578, "population": 85000, "hasBusTerminal": true, "hasRailStation": true, "hasAirport": false, "hasSeaport": false },
          { "id": "city.alytus", "name": "Alytus", "latitude": 54.3964, "longitude": 24.0459, "population": 50000, "hasBusTerminal": true, "hasRailStation": true, "hasAirport": false, "hasSeaport": false },
          { "id": "city.marijampole", "name": "Marijampolė", "latitude": 54.5599, "longitude": 23.3541, "population": 35000, "hasBusTerminal": true, "hasRailStation": true, "hasAirport": false, "hasSeaport": false },
          { "id": "city.mazeikiai", "name": "Mažeikiai", "latitude": 56.3092, "longitude": 22.3397, "population": 32000, "hasBusTerminal": true, "hasRailStation": true, "hasAirport": false, "hasSeaport": false },
          { "id": "city.jonava", "name": "Jonava", "latitude": 55.08, "longitude": 24.28, "population": 27000, "hasBusTerminal": true, "hasRailStation": true, "hasAirport": false, "hasSeaport": false },
          { "id": "city.utena", "name": "Utena", "latitude": 55.4977, "longitude": 25.5994, "population": 25000, "hasBusTerminal": true, "hasRailStation": false, "hasAirport": false, "hasSeaport": false },
          { "id": "city.kedainiai", "name": "Kėdainiai", "latitude": 55.2889, "longitude": 23.9722, "population": 23000, "hasBusTerminal": true, "hasRailStation": true, "hasAirport": false, "hasSeaport": false },
          { "id": "city.palanga", "name": "Palanga", "latitude": 55.9172, "longitude": 21.0686, "population": 18000, "hasBusTerminal": true, "hasRailStation": false, "hasAirport": true, "hasSeaport": false }
        ]
        """,
        """
        [
          { "cityA": "city.vilnius", "cityB": "city.kaunas" },
          { "cityA": "city.kaunas", "cityB": "city.klaipeda" },
          { "cityA": "city.vilnius", "cityB": "city.panevezys" },
          { "cityA": "city.panevezys", "cityB": "city.siauliai" },
          { "cityA": "city.siauliai", "cityB": "city.mazeikiai" },
          { "cityA": "city.kaunas", "cityB": "city.marijampole" },
          { "cityA": "city.kaunas", "cityB": "city.alytus" },
          { "cityA": "city.vilnius", "cityB": "city.jonava" },
          { "cityA": "city.jonava", "cityB": "city.kedainiai" },
          { "cityA": "city.kedainiai", "cityB": "city.siauliai" }
        ]
        """,
        """
        [
          { "text": "Latvia", "latitude": 56.35, "longitude": 24.5, "style": "neighbour" },
          { "text": "Belarus", "latitude": 54.2, "longitude": 26.5, "style": "neighbour" },
          { "text": "Poland", "latitude": 54.05, "longitude": 22.8, "style": "neighbour" },
          { "text": "Baltic Sea", "latitude": 55.6, "longitude": 20.95, "style": "water" }
        ]
        """);

    public static VehicleCatalog DefaultCatalog() => ContentLoader.LoadVehicleCatalogFromJson(
        """
        [
          {
            "id": "bus.city",
            "mode": "bus",
            "displayName": "City Bus",
            "capacityPassengers": 40,
            "cruiseSpeedKmh": 60,
            "purchaseCostMinor": 12000000,
            "operatingCostPerKmMinor": 55,
            "operatingCostPerHourMinor": 1100,
            "maxRangeKm": 180,
            "tier": 1
          },
          {
            "id": "bus.coach",
            "mode": "bus",
            "displayName": "Intercity Coach",
            "capacityPassengers": 55,
            "cruiseSpeedKmh": 80,
            "purchaseCostMinor": 25000000,
            "operatingCostPerKmMinor": 80,
            "operatingCostPerHourMinor": 1500,
            "maxRangeKm": 600,
            "tier": 1
          },
          {
            "id": "bus.articulated",
            "mode": "bus",
            "displayName": "Articulated Bus",
            "capacityPassengers": 85,
            "cruiseSpeedKmh": 55,
            "purchaseCostMinor": 42000000,
            "operatingCostPerKmMinor": 95,
            "operatingCostPerHourMinor": 1900,
            "maxRangeKm": 220,
            "tier": 2,
            "requiresCitiesServed": 3
          },
          {
            "id": "train.regional",
            "mode": "train",
            "displayName": "Regional EMU",
            "capacityPassengers": 220,
            "cruiseSpeedKmh": 140,
            "purchaseCostMinor": 180000000,
            "operatingCostPerKmMinor": 350,
            "operatingCostPerHourMinor": 8000,
            "tier": 1
          },
          {
            "id": "train.intercity",
            "mode": "train",
            "displayName": "Intercity Express",
            "capacityPassengers": 280,
            "cruiseSpeedKmh": 180,
            "purchaseCostMinor": 310000000,
            "operatingCostPerKmMinor": 480,
            "operatingCostPerHourMinor": 12000,
            "tier": 2,
            "requiresCitiesServed": 3
          },
          {
            "id": "train.high_capacity",
            "mode": "train",
            "displayName": "High-Capacity Set",
            "capacityPassengers": 420,
            "cruiseSpeedKmh": 130,
            "purchaseCostMinor": 480000000,
            "operatingCostPerKmMinor": 620,
            "operatingCostPerHourMinor": 16000,
            "tier": 3,
            "requiresCitiesServed": 5,
            "requiresCumulativePassengers": 5000
          },
          {
            "id": "air.turboprop",
            "mode": "air",
            "displayName": "Turboprop",
            "capacityPassengers": 60,
            "cruiseSpeedKmh": 480,
            "purchaseCostMinor": 160000000,
            "operatingCostPerKmMinor": 520,
            "operatingCostPerHourMinor": 14000,
            "maxRangeKm": 1200,
            "tier": 1
          },
          {
            "id": "air.regional_jet",
            "mode": "air",
            "displayName": "Regional Jet",
            "capacityPassengers": 90,
            "cruiseSpeedKmh": 720,
            "purchaseCostMinor": 320000000,
            "operatingCostPerKmMinor": 900,
            "operatingCostPerHourMinor": 25000,
            "maxRangeKm": 2500,
            "tier": 1
          },
          {
            "id": "air.narrowbody",
            "mode": "air",
            "displayName": "Narrowbody",
            "capacityPassengers": 160,
            "cruiseSpeedKmh": 820,
            "purchaseCostMinor": 680000000,
            "operatingCostPerKmMinor": 1400,
            "operatingCostPerHourMinor": 42000,
            "maxRangeKm": 4500,
            "tier": 3,
            "requiresCitiesServed": 5,
            "requiresCumulativePassengers": 8000
          }
        ]
        """);

    public static Simulation CreateDefaultGame(string companyName = "Aurelia Transit", string? contentRoot = null) =>
        CreateGame("aurelia", companyName, contentRoot);

    public static Simulation CreateGame(string mapPackId, string companyName = "TravelEmpire Co.", string? contentRoot = null)
    {
        var pack = MapPackRegistry.Load(mapPackId, contentRoot);
        var catalog = LoadCatalog(contentRoot);
        var sim = new Simulation(pack, catalog);
        var result = sim.Apply(new Commands.NewGameCommand { CompanyName = companyName, Seed = 42 });
        if (!result.Success)
            throw new InvalidOperationException(result.ErrorMessage);
        return sim;
    }

    public static VehicleCatalog LoadCatalog(string? contentRoot = null)
    {
        if (!string.IsNullOrWhiteSpace(contentRoot))
        {
            var path = Path.Combine(contentRoot, "vehicles", "catalog.json");
            if (File.Exists(path))
                return ContentLoader.LoadVehicleCatalog(path);
        }

        return DefaultCatalog();
    }
}
