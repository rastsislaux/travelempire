using Godot;

namespace TravelEmpire.Godot;

/// <summary>Cursor-anchored zoom/pan camera for map world space.</summary>
public sealed class MapCamera
{
    public Vector2 Pan { get; private set; }
    public float Zoom { get; private set; } = 1f;
    public float MinZoom { get; set; } = 0.35f;
    public float MaxZoom { get; set; } = 4f;

    public Vector2 WorldToScreen(Vector2 world) => world * Zoom + Pan;

    public Vector2 ScreenToWorld(Vector2 screen) => (screen - Pan) / Zoom;

    public void ZoomAt(Vector2 screenPoint, float factor)
    {
        var before = ScreenToWorld(screenPoint);
        Zoom = Mathf.Clamp(Zoom * factor, MinZoom, MaxZoom);
        var after = ScreenToWorld(screenPoint);
        Pan += (after - before) * Zoom;
    }

    public void BeginPan(Vector2 screen) => _panAnchor = (screen, Pan);

    public void UpdatePan(Vector2 screen)
    {
        Pan = _panAnchor.Pan + (screen - _panAnchor.Screen);
    }

    public void FitToContent(Rect2 worldBounds, Vector2 viewport, float margin = 48f)
    {
        if (worldBounds.Size.X <= 0.001f || worldBounds.Size.Y <= 0.001f)
            return;

        var usable = viewport - new Vector2(margin * 2f, margin * 2f);
        usable = new Vector2(Mathf.Max(usable.X, 1), Mathf.Max(usable.Y, 1));
        var zoom = Mathf.Min(usable.X / worldBounds.Size.X, usable.Y / worldBounds.Size.Y);
        Zoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
        var centre = worldBounds.Position + worldBounds.Size * 0.5f;
        Pan = viewport * 0.5f - centre * Zoom;
    }

    public void ClampToContent(Rect2 worldBounds, Vector2 viewport)
    {
        if (worldBounds.Size == Vector2.Zero)
            return;

        var min = WorldToScreen(worldBounds.Position);
        var max = WorldToScreen(worldBounds.Position + worldBounds.Size);
        var content = new Rect2(min, max - min);
        var pad = viewport * 0.25f;

        if (content.End.X < pad.X)
            Pan += new Vector2(pad.X - content.End.X, 0);
        if (content.Position.X > viewport.X - pad.X)
            Pan -= new Vector2(content.Position.X - (viewport.X - pad.X), 0);
        if (content.End.Y < pad.Y)
            Pan += new Vector2(0, pad.Y - content.End.Y);
        if (content.Position.Y > viewport.Y - pad.Y)
            Pan -= new Vector2(0, content.Position.Y - (viewport.Y - pad.Y));
    }

    public float KmPerPixel(double latSpanDegrees, float worldHeightPx)
    {
        if (worldHeightPx <= 0 || Zoom <= 0)
            return 1f;
        var km = (float)(latSpanDegrees * 111.32);
        return km / (worldHeightPx * Zoom);
    }

    private (Vector2 Screen, Vector2 Pan) _panAnchor;
}
