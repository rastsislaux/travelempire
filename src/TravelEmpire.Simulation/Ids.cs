namespace TravelEmpire.Simulation;

public readonly record struct CityId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct VehicleId(long Value)
{
    public override string ToString() => Value.ToString();
}

public readonly record struct RouteId(long Value)
{
    public override string ToString() => Value.ToString();
}

public readonly record struct CompanyId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct VehicleTypeId(string Value)
{
    public override string ToString() => Value;
}
