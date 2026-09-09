using TravelEmpire.Simulation.Content;

namespace TravelEmpire.Simulation;

public static class BuiltInContent
{
    public static MapPack AureliaMapPack() => ContentLoader.LoadMapPackFromJson(
        """
        { "id": "aurelia", "name": "Republic of Aurelia" }
        """,
        """
        [
          { "id": "city.capital", "name": "Aurel", "latitude": 53.90, "longitude": 27.57, "population": 1800000, "hasBusTerminal": true, "hasRailStation": true, "hasAirport": true },
          { "id": "city.port", "name": "Port Haven", "latitude": 54.69, "longitude": 25.28, "population": 900000, "hasBusTerminal": true, "hasRailStation": true, "hasAirport": true },
          { "id": "city.industrial", "name": "Forgeford", "latitude": 52.43, "longitude": 31.00, "population": 600000, "hasBusTerminal": true, "hasRailStation": true, "hasAirport": false },
          { "id": "city.university", "name": "Scholar's Rest", "latitude": 53.12, "longitude": 26.02, "population": 250000, "hasBusTerminal": true, "hasRailStation": false, "hasAirport": false },
          { "id": "city.border", "name": "Westgate", "latitude": 53.68, "longitude": 23.83, "population": 180000, "hasBusTerminal": true, "hasRailStation": true, "hasAirport": false },
          { "id": "city.resort", "name": "Silver Coast", "latitude": 55.72, "longitude": 27.32, "population": 120000, "hasBusTerminal": true, "hasRailStation": false, "hasAirport": true }
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

    public static VehicleCatalog DefaultCatalog() => ContentLoader.LoadVehicleCatalogFromJson(
        """
        [
          {
            "id": "bus.standard",
            "mode": "bus",
            "displayName": "Standard Coach",
            "capacityPassengers": 50,
            "cruiseSpeedKmh": 80,
            "purchaseCostMinor": 25000000,
            "operatingCostPerKmMinor": 80,
            "operatingCostPerHourMinor": 1500
          },
          {
            "id": "train.regional",
            "mode": "train",
            "displayName": "Regional EMU",
            "capacityPassengers": 220,
            "cruiseSpeedKmh": 140,
            "purchaseCostMinor": 180000000,
            "operatingCostPerKmMinor": 350,
            "operatingCostPerHourMinor": 8000
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
            "maxRangeKm": 2500
          }
        ]
        """);

    public static Simulation CreateDefaultGame(string companyName = "Aurelia Transit")
    {
        var sim = new Simulation(AureliaMapPack(), DefaultCatalog());
        var result = sim.Apply(new Commands.NewGameCommand { CompanyName = companyName, Seed = 42 });
        if (!result.Success)
            throw new InvalidOperationException(result.ErrorMessage);
        return sim;
    }
}
