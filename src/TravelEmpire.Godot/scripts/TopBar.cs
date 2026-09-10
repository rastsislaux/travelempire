using Godot;

namespace TravelEmpire.Godot;

public partial class TopBar : PanelContainer
{
    public event Action<int>? SpeedChanged;
    public event Action? NewGamePressed;
    public event Action? MenuPressed;

    private Label _cashLabel = null!;
    private Label _dayLabel = null!;
    private Label _dateLabel = null!;
    private readonly Dictionary<int, Button> _speedButtons = new();
    private static readonly DateTime Epoch = new(2030, 4, 5);

    public override void _Ready()
    {
        AddThemeStyleboxOverride("panel", ThemeFactory.Flat(Palette.BgPanel, Palette.Border, 0, 0));
        CustomMinimumSize = new Vector2(0, 52);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        AddChild(margin);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 16);
        margin.AddChild(row);

        var brand = new VBoxContainer();
        brand.AddThemeConstantOverride("separation", 0);
        row.AddChild(brand);
        var title = new Label { Text = "TravelEmpire" };
        title.AddThemeColorOverride("font_color", Palette.BrandAmber);
        title.AddThemeFontSizeOverride("font_size", 18);
        brand.AddChild(title);
        var tagline = new Label { Text = "Connect People. Grow Further.", Name = "Tagline" };
        tagline.AddThemeColorOverride("font_color", Palette.TextDim);
        tagline.AddThemeFontSizeOverride("font_size", 10);
        brand.AddChild(tagline);

        _cashLabel = new Label();
        _cashLabel.AddThemeColorOverride("font_color", Palette.CashGreen);
        _cashLabel.AddThemeFontSizeOverride("font_size", 16);
        row.AddChild(_cashLabel);

        var clock = new VBoxContainer();
        clock.AddThemeConstantOverride("separation", 0);
        row.AddChild(clock);
        _dayLabel = new Label();
        _dayLabel.AddThemeColorOverride("font_color", Palette.TextPrimary);
        clock.AddChild(_dayLabel);
        _dateLabel = new Label();
        _dateLabel.AddThemeColorOverride("font_color", Palette.TextMuted);
        _dateLabel.AddThemeFontSizeOverride("font_size", 11);
        clock.AddChild(_dateLabel);

        row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        var speeds = new HBoxContainer();
        speeds.AddThemeConstantOverride("separation", 2);
        row.AddChild(speeds);
        foreach (var (text, speed) in new (string, int)[] { ("||", 0), ("1x", 1), ("2x", 2), ("4x", 4) })
        {
            var btn = new Button { Text = text, ToggleMode = true, CustomMinimumSize = new Vector2(44, 28) };
            var captured = speed;
            btn.Pressed += () =>
            {
                SetActiveSpeed(captured);
                SpeedChanged?.Invoke(captured);
            };
            speeds.AddChild(btn);
            _speedButtons[speed] = btn;
        }

        var newGame = new Button { Text = "New", TooltipText = "New game / map pack" };
        newGame.Pressed += () => NewGamePressed?.Invoke();
        row.AddChild(newGame);

            foreach (var (text, tip) in new[] { ("S", "Stats (soon)"), ("*", "Settings (soon)"), ("=", "Menu (soon)") })
            {
                var btn = new Button { Text = text, TooltipText = tip, CustomMinimumSize = new Vector2(32, 28) };
                if (text == "=")
                    btn.Pressed += () => MenuPressed?.Invoke();
                row.AddChild(btn);
            }

        SetActiveSpeed(1);
    }

    public void UpdateFrom(long? cashMinor, double simHours, int speed)
    {
        var cash = (cashMinor ?? 0) / 100.0;
        _cashLabel.Text = $"${cash:N0}";
        _cashLabel.AddThemeColorOverride("font_color", cash < 0 ? Palette.Danger : Palette.CashGreen);

        var day = simHours / 24.0;
        _dayLabel.Text = $"Day {day:0.0}";
        var date = Epoch.AddHours(simHours);
        _dateLabel.Text = date.ToString("MMM d, yyyy");
        SetActiveSpeed(speed);

        if (FindChild("Tagline", recursive: true) is Label tagline)
            tagline.Visible = Size.X >= 1100;
    }

    public void FlashCashError()
    {
        _cashLabel.AddThemeColorOverride("font_color", Palette.Danger);
    }

    private void SetActiveSpeed(int speed)
    {
        foreach (var (value, button) in _speedButtons)
        {
            var active = value == speed;
            button.ButtonPressed = active;
            button.AddThemeStyleboxOverride("normal", ThemeFactory.Flat(
                active ? Palette.BrandAmber : Palette.BgElevated,
                active ? Palette.BrandAmber : Palette.Border,
                4, 1));
            button.AddThemeColorOverride("font_color", active ? Palette.BgDeep : Palette.TextPrimary);
        }
    }
}
