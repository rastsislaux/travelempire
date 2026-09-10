using Godot;

namespace TravelEmpire.Godot;

public partial class ToastLayer : Control
{
    private readonly List<(PanelContainer Panel, double Remaining)> _toasts = [];

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    public override void _Process(double delta)
    {
        for (var i = _toasts.Count - 1; i >= 0; i--)
        {
            var item = _toasts[i];
            item.Remaining -= delta;
            if (item.Remaining <= 0)
            {
                item.Panel.QueueFree();
                _toasts.RemoveAt(i);
            }
            else
            {
                _toasts[i] = item;
            }
        }
    }

    public void ShowToast(string message, bool error = false)
    {
        while (_toasts.Count >= 3)
        {
            _toasts[0].Panel.QueueFree();
            _toasts.RemoveAt(0);
        }

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", ThemeFactory.Flat(
            error ? new Color(0.35f, 0.12f, 0.12f, 0.95f) : new Color(0.10f, 0.18f, 0.14f, 0.95f),
            error ? Palette.Danger : Palette.CashGreen,
            6, 1));

        var label = new Label
        {
            Text = message,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(280, 0)
        };
        label.AddThemeColorOverride("font_color", Palette.TextPrimary);
        panel.AddChild(label);
        AddChild(panel);
        _toasts.Add((panel, 3.0));
        LayoutToasts();
    }

    private void LayoutToasts()
    {
        var y = Size.Y - 24;
        for (var i = _toasts.Count - 1; i >= 0; i--)
        {
            var panel = _toasts[i].Panel;
            panel.ResetSize();
            var size = panel.GetCombinedMinimumSize();
            panel.Position = new Vector2((Size.X - size.X) * 0.5f, y - size.Y);
            y -= size.Y + 8;
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
            LayoutToasts();
    }
}
