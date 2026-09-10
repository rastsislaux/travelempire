using Godot;
using TravelEmpire.Simulation;
using TravelEmpire.Simulation.Presentation;
using TravelEmpire.Simulation.Views;

namespace TravelEmpire.Godot;

public partial class MapView : Control
{
    public event Action<string>? CityClicked;

    public string? SelectedCityId { get; set; }
    public string? HoveredCityId { get; set; }
    public MapInputMode InputMode { get; set; } = MapInputMode.Select;
    public IReadOnlyList<string> DraftStops { get; set; } = [];
    public TransportMode DraftMode { get; set; } = TransportMode.Bus;

    private GameSnapshot? _snapshot;
    private readonly Dictionary<string, Vector2> _positions = new();
    private readonly MapCamera _camera = new();
    private bool _dragging;
    private bool _fitted;
    private Rect2 _worldBounds;
    private double _latSpan = 1;
    private float _worldHeight = 600f;
    private Texture2D? _background;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        ClipContents = true;
    }

    public void UpdateFromSnapshot(GameSnapshot snapshot)
    {
        var packChanged = _snapshot?.MapPackId != snapshot.MapPackId;
        _snapshot = snapshot;
        TryLoadBackground(snapshot);
        RebuildPositions();
        if (packChanged)
            _fitted = false;
        if (!_fitted && _positions.Count > 0 && Size.X > 1)
        {
            _camera.FitToContent(_worldBounds, Size);
            _fitted = true;
        }

        QueueRedraw();
    }

    public void FitToContent()
    {
        if (_positions.Count == 0) return;
        _camera.FitToContent(_worldBounds, Size);
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), Palette.MapWater);

        if (_background is not null)
        {
            var dest = new Rect2(
                _camera.WorldToScreen(_worldBounds.Position),
                _worldBounds.Size * _camera.Zoom);
            DrawTextureRect(_background, dest, false, new Color(1, 1, 1, 0.85f));
        }
        else
        {
            var land = new Rect2(
                _camera.WorldToScreen(_worldBounds.Position),
                _worldBounds.Size * _camera.Zoom);
            DrawRect(land, Palette.MapLand);
            DrawGrid();
        }

        if (_snapshot is null)
            return;

        DrawLabels();
        DrawBorder();

        foreach (var edge in _snapshot.RailEdges)
        {
            if (!_positions.TryGetValue(edge.CityA, out var a) || !_positions.TryGetValue(edge.CityB, out var b))
                continue;
            DrawLine(_camera.WorldToScreen(a), _camera.WorldToScreen(b), Palette.Track, 2.5f);
        }

        foreach (var route in _snapshot.Routes.OrderBy(r => ModeDrawOrder(r.Mode)))
            DrawRoute(route);

        if (InputMode == MapInputMode.PlanRoute && DraftStops.Count > 0)
            DrawDraft();

        foreach (var vehicle in _snapshot.Vehicles)
            DrawVehicle(vehicle);

        var ranked = _snapshot.Cities.OrderByDescending(c => c.Population).ToList();
        var showLabels = _camera.Zoom >= 0.7f;
        var labelBudget = _camera.Zoom >= 1.2f ? ranked.Count : Math.Min(8, ranked.Count);

        for (var i = 0; i < ranked.Count; i++)
        {
            var city = ranked[i];
            if (!_positions.TryGetValue(city.Id, out var world))
                continue;
            DrawCity(city, _camera.WorldToScreen(world), showLabels && i < labelBudget);
        }

        DrawLegend();
        DrawCompassAndScale();
        if (InputMode == MapInputMode.PlanRoute)
            DrawPlanBanner();
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
                _camera.BeginPan(mouse.Position);
                AcceptEvent();
                break;
            }
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }:
                _dragging = false;
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true } up:
                _camera.ZoomAt(up.Position, 1.12f);
                _camera.ClampToContent(_worldBounds, Size);
                QueueRedraw();
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, Pressed: true } down:
                _camera.ZoomAt(down.Position, 1f / 1.12f);
                _camera.ClampToContent(_worldBounds, Size);
                QueueRedraw();
                AcceptEvent();
                break;
            case InputEventMouseMotion motion when _dragging:
                _camera.UpdatePan(motion.Position);
                _camera.ClampToContent(_worldBounds, Size);
                QueueRedraw();
                AcceptEvent();
                break;
            case InputEventMouseMotion hover:
            {
                var hit = HitTest(hover.Position);
                if (hit != HoveredCityId)
                {
                    HoveredCityId = hit;
                    QueueRedraw();
                }

                break;
            }
        }
    }

    private void TryLoadBackground(GameSnapshot snapshot)
    {
        if (string.IsNullOrEmpty(snapshot.BackgroundAsset) || string.IsNullOrEmpty(snapshot.MapPackId))
        {
            _background = null;
            return;
        }

        var path = $"res://content/maps/{snapshot.MapPackId}/{snapshot.BackgroundAsset}";
        if (ResourceLoader.Exists(path))
            _background = ResourceLoader.Load<Texture2D>(path);
        else
            _background = null;
    }

    private void RebuildPositions()
    {
        _positions.Clear();
        if (_snapshot is null || _snapshot.Cities.Count == 0)
            return;

        var minLon = _snapshot.MinLongitude ?? _snapshot.Cities.Min(c => c.Longitude);
        var maxLon = _snapshot.MaxLongitude ?? _snapshot.Cities.Max(c => c.Longitude);
        var minLat = _snapshot.MinLatitude ?? _snapshot.Cities.Min(c => c.Latitude);
        var maxLat = _snapshot.MaxLatitude ?? _snapshot.Cities.Max(c => c.Latitude);
        var lonSpan = Math.Max(0.01, maxLon - minLon);
        var latSpan = Math.Max(0.01, maxLat - minLat);
        _latSpan = latSpan;
        _worldHeight = 620f;

        foreach (var city in _snapshot.Cities)
        {
            var x = (float)((city.Longitude - minLon) / lonSpan) * 920f;
            var y = (float)(1.0 - (city.Latitude - minLat) / latSpan) * _worldHeight;
            _positions[city.Id] = new Vector2(x, y);
        }

        _worldBounds = new Rect2(new Vector2(-20, -20), new Vector2(960, _worldHeight + 40));
    }

    private void DrawGrid()
    {
        var origin = _camera.WorldToScreen(Vector2.Zero);
        var step = 48f * _camera.Zoom;
        if (step < 12) return;
        for (var x = origin.X % step; x < Size.X; x += step)
            DrawLine(new Vector2(x, 0), new Vector2(x, Size.Y), Palette.MapGrid);
        for (var y = origin.Y % step; y < Size.Y; y += step)
            DrawLine(new Vector2(0, y), new Vector2(Size.X, y), Palette.MapGrid);
    }

    private void DrawBorder()
    {
        var rect = new Rect2(
            _camera.WorldToScreen(_worldBounds.Position),
            _worldBounds.Size * _camera.Zoom);
        DrawRect(rect, new Color(Palette.TextDim.R, Palette.TextDim.G, Palette.TextDim.B, 0.55f), false, 2);
    }

    private void DrawLabels()
    {
        if (_snapshot is null) return;
        var font = ThemeDB.FallbackFont;
        foreach (var label in _snapshot.Labels)
        {
            var world = LatLonToWorld(label.Latitude, label.Longitude);
            var screen = _camera.WorldToScreen(world);
            var color = label.Style == "water" ? Palette.Air.Lightened(0.2f) : Palette.TextDim;
            DrawString(font, screen, label.Text, HorizontalAlignment.Left, -1, 12, color);
        }

        if (!string.IsNullOrEmpty(_snapshot.MapPackName))
        {
            var titlePos = _camera.WorldToScreen(_worldBounds.Position + new Vector2(24, 28));
            DrawString(font, titlePos, _snapshot.MapPackName, HorizontalAlignment.Left, -1, 18,
                new Color(Palette.TextPrimary.R, Palette.TextPrimary.G, Palette.TextPrimary.B, 0.35f));
        }
    }

    private Vector2 LatLonToWorld(double lat, double lon)
    {
        if (_snapshot is null) return Vector2.Zero;
        var minLon = _snapshot.MinLongitude ?? _snapshot.Cities.Min(c => c.Longitude);
        var maxLon = _snapshot.MaxLongitude ?? _snapshot.Cities.Max(c => c.Longitude);
        var minLat = _snapshot.MinLatitude ?? _snapshot.Cities.Min(c => c.Latitude);
        var maxLat = _snapshot.MaxLatitude ?? _snapshot.Cities.Max(c => c.Latitude);
        var lonSpan = Math.Max(0.01, maxLon - minLon);
        var latSpan = Math.Max(0.01, maxLat - minLat);
        var x = (float)((lon - minLon) / lonSpan) * 920f;
        var y = (float)(1.0 - (lat - minLat) / latSpan) * _worldHeight;
        return new Vector2(x, y);
    }

    private void DrawRoute(RouteView route)
    {
        for (var i = 0; i < route.StopIds.Count - 1; i++)
        {
            if (!_positions.TryGetValue(route.StopIds[i], out var from)
                || !_positions.TryGetValue(route.StopIds[i + 1], out var to))
                continue;
            DrawModeStroke(_camera.WorldToScreen(from), _camera.WorldToScreen(to), route.Mode);
        }
    }

    private void DrawDraft()
    {
        for (var i = 0; i < DraftStops.Count - 1; i++)
        {
            if (!_positions.TryGetValue(DraftStops[i], out var from)
                || !_positions.TryGetValue(DraftStops[i + 1], out var to))
                continue;
            DrawModeStroke(_camera.WorldToScreen(from), _camera.WorldToScreen(to), DraftMode, 0.7f);
        }

        for (var i = 0; i < DraftStops.Count; i++)
        {
            if (!_positions.TryGetValue(DraftStops[i], out var world))
                continue;
            var screen = _camera.WorldToScreen(world);
            DrawCircle(screen + new Vector2(12, -12), 8, Palette.Mode(DraftMode));
            DrawString(ThemeDB.FallbackFont, screen + new Vector2(8, -16), (i + 1).ToString(),
                HorizontalAlignment.Left, -1, 11, Palette.BgDeep);
        }
    }

    private void DrawModeStroke(Vector2 a, Vector2 b, TransportMode mode, float alpha = 1f)
    {
        var color = Palette.Mode(mode);
        color.A = alpha;
        switch (mode)
        {
            case TransportMode.Bus:
                DrawLine(a, b, color, 3.2f);
                break;
            case TransportMode.Train:
            {
                var n = (b - a).Orthogonal().Normalized() * 2.2f;
                DrawLine(a + n, b + n, color, 2.0f);
                DrawLine(a - n, b - n, color, 2.0f);
                break;
            }
            case TransportMode.Air:
                DrawDashed(a, b, color, 2.4f, 10f, 7f);
                break;
        }
    }

    private void DrawDashed(Vector2 a, Vector2 b, Color color, float width, float dash, float gap)
    {
        var delta = b - a;
        var length = delta.Length();
        if (length < 1f) return;
        var dir = delta / length;
        var pos = 0f;
        while (pos < length)
        {
            var start = a + dir * pos;
            var end = a + dir * Math.Min(pos + dash, length);
            DrawLine(start, end, color, width);
            pos += dash + gap;
        }
    }

    private void DrawVehicle(VehicleView vehicle)
    {
        var pos = VehicleWorldPos(vehicle);
        if (pos is null) return;
        var screen = _camera.WorldToScreen(pos.Value);
        var color = Palette.Mode(vehicle.Mode);
        var rect = new Rect2(screen - new Vector2(10, 7), new Vector2(20, 14));
        DrawRect(rect, color);
        DrawRect(rect, Palette.BgDeep, false, 1);
        DrawString(ThemeDB.FallbackFont, screen + new Vector2(-4, 4), Palette.ModeGlyph(vehicle.Mode),
            HorizontalAlignment.Left, -1, 11, Palette.BgDeep);
    }

    private void DrawCity(CityView city, Vector2 screen, bool withLabel)
    {
        var selected = city.Id == SelectedCityId;
        var hovered = city.Id == HoveredCityId;
        var radius = selected ? 11f : hovered ? 9f : 7f;
        if (selected)
            DrawCircle(screen, radius + 4, new Color(Palette.Selection.R, Palette.Selection.G, Palette.Selection.B, 0.35f));
        DrawCircle(screen, radius, Palette.CityRing);
        DrawCircle(screen, radius - 3, Palette.CityCore);

        if (city.HasAirport)
            DrawAirportIcon(screen + new Vector2(10, -10));
        if (city.HasSeaport)
            DrawPortIcon(screen + new Vector2(-12, -10));

        if (!withLabel) return;
        var font = ThemeDB.FallbackFont;
        DrawString(font, screen + new Vector2(0, -16), city.Name, HorizontalAlignment.Center, -1, 13, Palette.TextPrimary);
        DrawString(font, screen + new Vector2(0, 16), FormatPop(city.Population), HorizontalAlignment.Center, -1, 10, Palette.TextMuted);
    }

    private void DrawAirportIcon(Vector2 at)
    {
        DrawCircle(at, 5, Palette.Air);
        DrawString(ThemeDB.FallbackFont, at + new Vector2(-3, 3), "A", HorizontalAlignment.Left, -1, 9, Palette.BgDeep);
    }

    private void DrawPortIcon(Vector2 at)
    {
        DrawCircle(at, 5, new Color(0.35f, 0.65f, 0.85f));
        DrawString(ThemeDB.FallbackFont, at + new Vector2(-3, 3), "P", HorizontalAlignment.Left, -1, 9, Palette.BgDeep);
    }

    private void DrawLegend()
    {
        var card = new Rect2(16, 16, 148, 168);
        DrawRect(card, new Color(Palette.BgPanel.R, Palette.BgPanel.G, Palette.BgPanel.B, 0.92f));
        DrawRect(card, Palette.Border, false, 1);
        var font = ThemeDB.FallbackFont;
        var y = card.Position.Y + 14;
        DrawString(font, new Vector2(card.Position.X + 10, y), "LEGEND", HorizontalAlignment.Left, -1, 10, Palette.TextDim);
        y += 18;
        foreach (var (label, mode) in new (string, TransportMode?)[]
                 {
                     ("Bus", TransportMode.Bus),
                     ("Rail", TransportMode.Train),
                     ("Air", TransportMode.Air),
                     ("City", null),
                     ("Airport", null),
                     ("Port", null),
                     ("Border", null)
                 })
        {
            var left = new Vector2(card.Position.X + 12, y);
            if (mode is TransportMode m)
                DrawModeStroke(left, left + new Vector2(28, 0), m);
            else if (label == "City")
            {
                DrawCircle(left + new Vector2(10, 0), 5, Palette.CityRing);
                DrawCircle(left + new Vector2(10, 0), 3, Palette.CityCore);
            }
            else if (label == "Airport")
                DrawAirportIcon(left + new Vector2(10, 0));
            else if (label == "Port")
                DrawPortIcon(left + new Vector2(10, 0));
            else
                DrawLine(left, left + new Vector2(28, 0), Palette.TextDim, 2);

            DrawString(font, left + new Vector2(36, 4), label, HorizontalAlignment.Left, -1, 11, Palette.TextPrimary);
            y += 20;
        }
    }

    private void DrawCompassAndScale()
    {
        var origin = new Vector2(28, Size.Y - 36);
        DrawCircle(origin, 16, Palette.BgPanel);
        DrawArc(origin, 16, 0, Mathf.Tau, 32, Palette.Border, 1);
        DrawLine(origin, origin + new Vector2(0, -12), Palette.BrandAmber, 2);
        DrawString(ThemeDB.FallbackFont, origin + new Vector2(-4, -18), "N", HorizontalAlignment.Left, -1, 10, Palette.BrandAmber);

        var kmPerPx = _camera.KmPerPixel(_latSpan, _worldHeight);
        var targetKm = NiceScale(kmPerPx * 80);
        var barPx = targetKm / Math.Max(kmPerPx, 0.0001f);
        var barPos = new Vector2(56, Size.Y - 28);
        DrawLine(barPos, barPos + new Vector2(barPx, 0), Palette.TextPrimary, 2);
        DrawLine(barPos, barPos + new Vector2(0, -6), Palette.TextPrimary, 2);
        DrawLine(barPos + new Vector2(barPx, 0), barPos + new Vector2(barPx, -6), Palette.TextPrimary, 2);
        DrawString(ThemeDB.FallbackFont, barPos + new Vector2(0, -10), $"{targetKm:0} km",
            HorizontalAlignment.Left, -1, 11, Palette.TextPrimary);
    }

    private void DrawPlanBanner()
    {
        var text = $"Planning {DraftMode} route — click cities, Esc to cancel";
        var size = new Vector2(Math.Min(420, Size.X - 40), 28);
        var pos = new Vector2((Size.X - size.X) * 0.5f, 12);
        DrawRect(new Rect2(pos, size), new Color(Palette.BrandAmber.R, Palette.BrandAmber.G, Palette.BrandAmber.B, 0.9f));
        DrawString(ThemeDB.FallbackFont, pos + new Vector2(12, 19), text, HorizontalAlignment.Left, -1, 12, Palette.BgDeep);
    }

    private static float NiceScale(float approx)
    {
        if (approx <= 5) return 5;
        if (approx <= 10) return 10;
        if (approx <= 25) return 25;
        if (approx <= 50) return 50;
        if (approx <= 100) return 100;
        return 200;
    }

    private static int ModeDrawOrder(TransportMode mode) => mode switch
    {
        TransportMode.Air => 0,
        TransportMode.Train => 1,
        TransportMode.Bus => 2,
        _ => 3
    };

    private string? HitTest(Vector2 screen)
    {
        foreach (var (id, world) in _positions)
        {
            if (_camera.WorldToScreen(world).DistanceTo(screen) <= 14f)
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

    private static string FormatPop(int pop) =>
        pop >= 1_000_000 ? $"{pop / 1_000_000.0:0.0}M" :
        pop >= 1_000 ? $"{pop / 1_000.0:0.0}k" :
        pop.ToString();
}
