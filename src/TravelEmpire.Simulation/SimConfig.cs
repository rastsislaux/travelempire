namespace TravelEmpire.Simulation;

public sealed class SimConfig
{
    public Money StartingCash { get; init; } = Money.FromMajor(5_000_000m);
    public double HoursPerTick { get; init; } = 0.25;
    public double DemandGravityExponent { get; init; } = 1.2;
    public double DemandGravityScalar { get; init; } = 1.2e-7;
    public Money MinFare { get; init; } = Money.FromMajor(5m);
    public Money DefaultPricePerKm { get; init; } = Money.FromMajor(0.12m);
    public double ReferencePricePerKmMajor { get; init; } = 0.12;
    public double TurnaroundHoursBus { get; init; } = 0.5;
    public double TurnaroundHoursTrain { get; init; } = 0.75;
    public double TurnaroundHoursAir { get; init; } = 1.0;
    public double MinAirDistanceKm { get; init; } = 80;

    public double TurnaroundHours(TransportMode mode) => mode switch
    {
        TransportMode.Bus => TurnaroundHoursBus,
        TransportMode.Train => TurnaroundHoursTrain,
        TransportMode.Air => TurnaroundHoursAir,
        _ => TurnaroundHoursBus
    };
}
