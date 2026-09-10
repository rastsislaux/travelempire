using Godot;
using TravelEmpire.Simulation;
using TravelEmpire.Simulation.Commands;
using TravelEmpire.Simulation.Content;
using TravelEmpire.Simulation.Views;

namespace TravelEmpire.Godot;

public partial class SimulationHost : Node
{
    public Simulation.Simulation Sim { get; private set; } = null!;
    public int SpeedMultiplier { get; private set; } = 1;
    public string? ContentRoot { get; private set; }

    private double _accumulator;
    private const double SecondsPerSimTick = 0.25;
    private bool _dirty = true;

    public override void _Ready()
    {
        ContentRoot = ResolveContentRoot();
        Sim = BuiltInContent.CreateDefaultGame("Aurelia Transit", ContentRoot);
        _dirty = true;
    }

    public IReadOnlyList<MapPackInfo> ListMapPacks() => MapPackRegistry.ListPacks(ContentRoot);

    public void StartNewGame(string mapPackId, string companyName)
    {
        Sim = BuiltInContent.CreateGame(mapPackId, companyName, ContentRoot);
        SpeedMultiplier = 1;
        _accumulator = 0;
        _dirty = true;
    }

    public override void _Process(double delta)
    {
        if (SpeedMultiplier <= 0)
            return;

        _accumulator += delta * SpeedMultiplier;
        var ticks = 0;
        while (_accumulator >= SecondsPerSimTick)
        {
            _accumulator -= SecondsPerSimTick;
            Sim.Tick(1);
            ticks++;
            if (ticks > 40)
                break;
        }

        if (ticks > 0)
            _dirty = true;
    }

    public void SetSpeed(int multiplier)
    {
        SpeedMultiplier = Math.Max(0, multiplier);
        _dirty = true;
    }

    public CommandResult Apply(ICommand command)
    {
        var result = Sim.Apply(command);
        _dirty = true;
        return result;
    }

    public GameSnapshot GetSnapshot() => Sim.GetSnapshot();

    public bool ConsumeDirty()
    {
        if (!_dirty)
            return false;
        _dirty = false;
        return true;
    }

    public void MarkDirty() => _dirty = true;

    private static string? ResolveContentRoot()
    {
        var fromRes = ProjectSettings.GlobalizePath("res://content");
        var candidates = new[]
        {
            fromRes,
            Path.GetFullPath(Path.Combine(fromRes, "..", "..", "..", "content")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "content")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "content")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "content"))
        };
        return MapPackRegistry.ResolveContentRoot(candidates);
    }
}
