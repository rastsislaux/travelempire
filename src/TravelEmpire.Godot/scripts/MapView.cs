using Godot;
using TravelEmpire.Simulation;
using TravelEmpire.Simulation.Views;

namespace TravelEmpire.Godot;

public partial class MapView : Control
{
    public event Action<string>? CityClicked;

    public string? SelectedCityId { get; set; }

    private GameSnapshot? _snapshot;
    private readonly Dictionary<string, Vector2> _positions = new();
    private Vector2 _pan;
    private float _zoom = 1f;
    private bool _dragging;
    private Vector2 _dragStart;
    private Vector2 _panStart;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        ClipContents = true;
    }

    public void UpdateFromSnapshot(GameSnapshot snapshot)
    {
        _snapshot = snapshot;
        RebuildPositions();
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.10f, 0.16f, 0.20f));
        var grid = new Color(0.15f, 0.22f, 0.26f);
        for (var x = 0f; x < Size.X; x += 48)
            DrawLine(new Vector2(x, 0), new Vector2(x, Size.Y), grid);
        for (var y = 0f; y < Size.Y; y += 48)
            DrawLine(new Vector2(0, y), new Vector2(Size.X, y), grid);

        if (_snapshot is null)
            return;

        foreach (var edge in _snapshot.RailEdges)
        {
            if (!_positions.TryGetValue(edge.CityA, out var a) || !_positions.TryGetValue(edge.CityB, out var b))
                continue;
            DrawLine(ToScreen(a), ToScreen(b), new Color(0.45f, 0.40f, 0.30f), 2);
        }

        foreach (var route in _snapshot.Routes)
        {
            var color = ColorFor(route.Mode);
            for (var i = 0; i < route.StopIds.Count - 1; i++)
            {
                if (!_positions.TryGetValue(route.StopIds[i], out var from)
                    || !_positions.TryGetValue(route.StopIds[i + 1], out var to))
                    continue;
                DrawLine(ToScreen(from), ToScreen(to), color, 3);
            }
        }

        foreach (var vehicle in _snapshot.Vehicles)
        {
            var pos = VehicleWorldPos(vehicle);
            if (pos is null) continue;
            DrawCircle(ToScreen(pos.Value), 5, ColorFor(vehicle.Mode));
        }

        var font = ThemeDB.FallbackFont;
        foreach (var city in _snapshot.Cities)
        {
            if (!_positions.TryGetValue(city.Id, out var world))
                continue;
            var screen = ToScreen(world);
            var selected = city.Id == SelectedCityId;
            DrawCircle(screen, selected ? 10 : 7, selected
                ? new Color(0.98f, 0.85f, 0.35f)
                : new Color(0.92f, 0.94f, 0.96f));
            DrawCircle(screen, selected ? 6 : 4, new Color(0.15f, 0.25f, 0.32f));
            DrawString(
                font,
                screen + new Vector2(10, 4),
                city.Name,
                HorizontalAlignment.Left,
                -1,
                14,
                Colors.White.Lightened(0.85f));
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouse:
            {
                var hit = HitTest(mouse.Position);
                if (hit is not null)
                {
                    CityClicked?.Invoke(hit);
                    AcceptEvent();
                    return;
                }

                _dragging = true;
                _dragStart = mouse.Position;
                _panStart = _pan;
                AcceptEvent();
                break;
            }
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }:
                _dragging = false;
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true }:
                _zoom = Math.Min(3f, _zoom * 1.1f);
                QueueRedraw();
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, Pressed: true }:
                _zoom = Math.Max(0.4f, _zoom / 1.1f);
                QueueRedraw();
                AcceptEvent();
                break;
            case InputEventMouseMotion motion when _dragging:
                _pan = _panStart + (motion.Position - _dragStart);
                QueueRedraw();
                AcceptEvent();
                break;
        }
    }

    private void RebuildPositions()
    {
        _positions.Clear();
        if (_snapshot is null || _snapshot.Cities.Count == 0)
            return;

        var minLon = _snapshot.Cities.Min(c => c.Longitude);
        var maxLon = _snapshot.Cities.Max(c => c.Longitude);
        var minLat = _snapshot.Cities.Min(c => c.Latitude);
        var maxLat = _snapshot.Cities.Max(c => c.Latitude);
        var lonSpan = Math.Max(0.01, maxLon - minLon);
        var latSpan = Math.Max(0.01, maxLat - minLat);

        foreach (var city in _snapshot.Cities)
        {
            var x = (float)((city.Longitude - minLon) / lonSpan);
            var y = (float)(1.0 - (city.Latitude - minLat) / latSpan);
            _positions[city.Id] = new Vector2(x * 900f, y * 600f);
        }
    }

    private Vector2 ToScreen(Vector2 world) => world * _zoom + _pan + Size * 0.08f;

    private string? HitTest(Vector2 screen)
    {
        foreach (var (id, world) in _positions)
        {
            if (ToScreen(world).DistanceTo(screen) <= 14f)
                return id;
        }

        return null;
    }

    private Vector2? VehicleWorldPos(VehicleView vehicle)
    {
        if (vehicle.AtCityId is not null && _positions.TryGetValue(vehicle.AtCityId, out var at))
            return at;

        if (vehicle.FromCityId is not null
            && vehicle.ToCityId is not null
            && _positions.TryGetValue(vehicle.FromCityId, out var from)
            && _positions.TryGetValue(vehicle.ToCityId, out var to))
        {
            return from.Lerp(to, (float)Math.Clamp(vehicle.Progress, 0, 1));
        }

        return null;
    }

    private static Color ColorFor(TransportMode mode) => mode switch
    {
        TransportMode.Bus => new Color(0.25f, 0.75f, 0.45f),
        TransportMode.Train => new Color(0.90f, 0.70f, 0.25f),
        TransportMode.Air => new Color(0.35f, 0.65f, 0.95f),
        _ => Colors.White
    };
}
