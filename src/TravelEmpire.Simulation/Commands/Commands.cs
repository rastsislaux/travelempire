namespace TravelEmpire.Simulation.Commands;

public interface ICommand
{
}

public sealed class CommandResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static CommandResult Ok() => new() { Success = true };

    public static CommandResult Fail(string code, string message) =>
        new() { Success = false, ErrorCode = code, ErrorMessage = message };
}

public sealed class NewGameCommand : ICommand
{
    public required string CompanyName { get; init; }
    public int Seed { get; init; } = 1;
}

public sealed class BuyVehicleCommand : ICommand
{
    public required VehicleTypeId TypeId { get; init; }
}

public sealed class CreateRouteCommand : ICommand
{
    public required string Name { get; init; }
    public required TransportMode Mode { get; init; }
    public required IReadOnlyList<CityId> Stops { get; init; }
    public Money? PricePerKm { get; init; }
}

public sealed class UpdateRouteCommand : ICommand
{
    public required RouteId RouteId { get; init; }
    public string? Name { get; init; }
    public Money? PricePerKm { get; init; }
    public IReadOnlyList<CityId>? Stops { get; init; }
}

public sealed class AssignVehicleCommand : ICommand
{
    public required VehicleId VehicleId { get; init; }
    public required RouteId RouteId { get; init; }
}

public sealed class UnassignVehicleCommand : ICommand
{
    public required VehicleId VehicleId { get; init; }
}

public sealed class GrantCashCommand : ICommand
{
    public required Money Amount { get; init; }
}
