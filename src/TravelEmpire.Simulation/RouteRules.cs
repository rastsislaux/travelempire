using TravelEmpire.Simulation.Commands;
using TravelEmpire.Simulation.Model;

namespace TravelEmpire.Simulation;

internal static class RouteRules
{
    public static CommandResult ValidateStops(GameState state, TransportMode mode, IReadOnlyList<CityId> stops)
    {
        if (stops.Count < 2)
            return CommandResult.Fail("route.too_few_stops", "A route needs at least two stops.");

        for (var i = 0; i < stops.Count; i++)
        {
            if (!state.TryGetCity(stops[i], out _))
                return CommandResult.Fail("route.unknown_city", $"Unknown city '{stops[i]}'.");
            if (i > 0 && stops[i].Value == stops[i - 1].Value)
                return CommandResult.Fail("route.duplicate_stop", "Consecutive stops cannot be the same city.");
        }

        for (var i = 0; i < stops.Count - 1; i++)
        {
            var result = ValidateLeg(state, mode, stops[i], stops[i + 1]);
            if (!result.Success) return result;
        }

        return CommandResult.Ok();
    }

    public static CommandResult ValidateLeg(GameState state, TransportMode mode, CityId from, CityId to)
    {
        var a = state.GetCity(from);
        var b = state.GetCity(to);

        switch (mode)
        {
            case TransportMode.Bus:
                if (!a.HasBusTerminal || !b.HasBusTerminal)
                    return CommandResult.Fail("route.bus_terminal", "Both cities need a bus terminal.");
                return CommandResult.Ok();

            case TransportMode.Train:
                if (!a.HasRailStation || !b.HasRailStation)
                    return CommandResult.Fail("route.rail_station", "Both cities need a rail station.");
                if (!state.HasRailLink(from, to))
                    return CommandResult.Fail("route.rail_edge", $"No rail link between {a.Name} and {b.Name}.");
                return CommandResult.Ok();

            case TransportMode.Air:
                if (!a.HasAirport || !b.HasAirport)
                    return CommandResult.Fail("route.airport", "Both cities need an airport.");
                var distance = state.DistanceKm(from, to);
                if (distance < state.Config.MinAirDistanceKm)
                    return CommandResult.Fail("route.air_too_short",
                        $"Air routes must be at least {state.Config.MinAirDistanceKm:0} km (this leg is {distance:0} km).");
                return CommandResult.Ok();

            default:
                return CommandResult.Fail("route.mode", "Unknown transport mode.");
        }
    }
}
