namespace TravelEmpire.Simulation;

/// <summary>Money in minor units (cents) to avoid floating-point cash bugs.</summary>
public readonly record struct Money(long MinorUnits) : IComparable<Money>
{
    public static Money Zero => new(0);

    public static Money FromMajor(decimal major) =>
        new((long)Math.Round(major * 100m, MidpointRounding.AwayFromZero));

    public decimal ToMajor() => MinorUnits / 100m;

    public int CompareTo(Money other) => MinorUnits.CompareTo(other.MinorUnits);

    public static Money operator +(Money a, Money b) => new(a.MinorUnits + b.MinorUnits);
    public static Money operator -(Money a, Money b) => new(a.MinorUnits - b.MinorUnits);
    public static Money operator *(Money a, int count) => new(a.MinorUnits * count);
    public static Money operator *(Money a, double factor) =>
        new((long)Math.Round(a.MinorUnits * factor, MidpointRounding.AwayFromZero));

    public static bool operator <(Money a, Money b) => a.MinorUnits < b.MinorUnits;
    public static bool operator >(Money a, Money b) => a.MinorUnits > b.MinorUnits;
    public static bool operator <=(Money a, Money b) => a.MinorUnits <= b.MinorUnits;
    public static bool operator >=(Money a, Money b) => a.MinorUnits >= b.MinorUnits;

    public override string ToString() => ToMajor().ToString("0.00");
}
