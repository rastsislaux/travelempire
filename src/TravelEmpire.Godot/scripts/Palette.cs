using Godot;

namespace TravelEmpire.Godot;

public static class Palette
{
    public static readonly Color BgDeep = new("0B1220");
    public static readonly Color BgPanel = new("121A2B");
    public static readonly Color BgCard = new("182338");
    public static readonly Color BgElevated = new("1E2A42");
    public static readonly Color Border = new("2A3A55");
    public static readonly Color TextPrimary = new("E8EEF8");
    public static readonly Color TextMuted = new("9AA8C0");
    public static readonly Color TextDim = new("6E7C94");
    public static readonly Color BrandAmber = new("E6B84A");
    public static readonly Color CashGreen = new("3DCC8A");
    public static readonly Color Danger = new("E05A5A");
    public static readonly Color Bus = new("3CBB6E");
    public static readonly Color Rail = new("E0A84A");
    public static readonly Color Air = new("4A8FE0");
    public static readonly Color MapLand = new("152033");
    public static readonly Color MapWater = new("0E1A2E");
    public static readonly Color MapGrid = new("1A2740");
    public static readonly Color CityCore = new("101826");
    public static readonly Color CityRing = new("F2F5FA");
    public static readonly Color Selection = new("F0C95A");
    public static readonly Color Track = new("4A5568");

    public static Color Mode(TravelEmpire.Simulation.TransportMode mode) => mode switch
    {
        TravelEmpire.Simulation.TransportMode.Bus => Bus,
        TravelEmpire.Simulation.TransportMode.Train => Rail,
        TravelEmpire.Simulation.TransportMode.Air => Air,
        _ => TextPrimary
    };

    public static string ModeGlyph(TravelEmpire.Simulation.TransportMode mode) => mode switch
    {
        TravelEmpire.Simulation.TransportMode.Bus => "B",
        TravelEmpire.Simulation.TransportMode.Train => "R",
        TravelEmpire.Simulation.TransportMode.Air => "A",
        _ => "?"
    };
}
