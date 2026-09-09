using Godot;
using TravelEmpire.Simulation;
using TravelEmpire.Simulation.Commands;
using TravelEmpire.Simulation.Views;

namespace TravelEmpire.Godot;

public partial class SimulationHost : Node
{
    public Simulation.Simulation Sim { get; private set; } = null!;
    public int SpeedMultiplier { get; private set; } = 1;

    private double _accumulator;
    private const double SecondsPerSimTick = 0.25;
    private bool _dirty = true;

    public override void _Ready()
    {
        Sim = BuiltInContent.CreateDefaultGame("Aurelia Transit");
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
}
