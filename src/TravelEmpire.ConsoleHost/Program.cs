using TravelEmpire.Simulation;
using TravelEmpire.Simulation.Commands;

namespace TravelEmpire.ConsoleHost;

public static class Program
{
    public static int Main(string[] args)
    {
        var ticks = 500;
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] is "--ticks" or "-t" && int.TryParse(args[i + 1], out var parsed))
                ticks = parsed;
        }

        var sim = BuiltInContent.CreateDefaultGame("Console Lines");
        var snap = sim.GetSnapshot();
        Console.WriteLine("TravelEmpire — console host");
        Console.WriteLine($"Map: {snap.MapPackName} ({snap.Cities.Count} cities)");
        foreach (var city in snap.Cities)
            Console.WriteLine($"  - {city.Name} pop={city.Population:N0}");
        Console.WriteLine();

        Must(sim.Apply(new BuyVehicleCommand { TypeId = new VehicleTypeId("bus.standard") }));
        Must(sim.Apply(new CreateRouteCommand
        {
            Name = "Capital Shuttle",
            Mode = TransportMode.Bus,
            Stops = [new CityId("city.capital"), new CityId("city.university")]
        }));

        snap = sim.GetSnapshot();
        Must(sim.Apply(new AssignVehicleCommand
        {
            VehicleId = new VehicleId(snap.Vehicles[0].Id),
            RouteId = new RouteId(snap.Routes[0].Id)
        }));

        var cashBefore = sim.GetSnapshot().Company!.CashMinor;
        Console.WriteLine($"Running {ticks} ticks…");
        sim.Tick(ticks);
        snap = sim.GetSnapshot();

        Console.WriteLine();
        Console.WriteLine($"Company : {snap.Company!.Name}");
        Console.WriteLine($"Cash    : ${snap.Company.CashMinor / 100.0:N2} (Δ {(snap.Company.CashMinor - cashBefore) / 100.0:N2})");
        Console.WriteLine($"Pax     : {snap.Company.CumulativePassengers:N0}");
        Console.WriteLine($"Revenue : ${snap.Company.CumulativeRevenueMinor / 100.0:N2}");
        Console.WriteLine($"Sim day : {snap.SimHours / 24.0:0.00}");
        Console.WriteLine($"Routes  : {snap.Routes.Count}, Vehicles: {snap.Vehicles.Count}");
        foreach (var vehicle in snap.Vehicles)
        {
            Console.WriteLine(
                $"  Vehicle #{vehicle.Id} {vehicle.State} pax={vehicle.OnboardPassengers} " +
                $"at={vehicle.AtCityId ?? "-"} {vehicle.FromCityId}->{vehicle.ToCityId} prog={vehicle.Progress:0.00}");
        }

        return snap.Company.CumulativePassengers > 0 ? 0 : 2;
    }

    private static void Must(CommandResult result)
    {
        if (!result.Success)
            throw new InvalidOperationException($"{result.ErrorCode}: {result.ErrorMessage}");
    }
}
