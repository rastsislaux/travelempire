using Godot;
using TravelEmpire.Simulation;
using TravelEmpire.Simulation.Presentation;

namespace TravelEmpire.Godot;

public partial class SidePanel : PanelContainer
{
    public event Action? ClosePressed;
    public event Action<TransportMode>? BuyModeChanged;
    public event Action<string>? CatalogSelected;
    public event Action<long>? VehicleSelected;
    public event Action<long>? RouteSelected;
    public event Action? BuyPressed;
    public event Action? AssignPressed;
    public event Action? PlanRoutePressed;
    public event Action? CreateRoutePressed;
    public event Action? ClearDraftPressed;
    public event Action<bool>? ShowAllFleetChanged;
    public event Action<bool>? ShowAllRoutesChanged;
    public event Action<TransportMode>? DraftModeChanged;
    public event Action<string>? RouteNameChanged;

    private Label _headerLabel = null!;
    private Button _closeButton = null!;
    private Control _cityBlock = null!;
    private Control _empireBlock = null!;
    private Label _empireSummary = null!;
    private ColorRect _heroRect = null!;
    private Label _heroInitial = null!;
    private Label _popValue = null!;
    private Label _growthValue = null!;
    private Label _happyValue = null!;
    private Label _economyValue = null!;
    private Label _airportValue = null!;
    private Label _portValue = null!;
    private ProgressBar _localBar = null!;
    private ProgressBar _intercityBar = null!;
    private ProgressBar _airBar = null!;
    private Label _localTag = null!;
    private Label _intercityTag = null!;
    private Label _airTag = null!;
    private readonly Dictionary<TransportMode, Button> _modeChips = new();
    private DataTable _buyTable = null!;
    private DataTable _fleetTable = null!;
    private DataTable _routeTable = null!;
    private Button _buyButton = null!;
    private Button _assignButton = null!;
    private Button _planButton = null!;
    private Button _createButton = null!;
    private Button _showAllFleet = null!;
    private Button _showAllRoutes = null!;
    private Label _draftLabel = null!;
    private Label _validationLabel = null!;
    private Label _tipLabel = null!;
    private LineEdit _routeNameEdit = null!;
    private OptionButton _draftModeOption = null!;
    private TabContainer _tabs = null!;
    private Label _demandLocalFull = null!;
    private Label _demandInterFull = null!;
    private Label _demandAirFull = null!;
    private ProgressBar _demandLocalFullBar = null!;
    private ProgressBar _demandInterFullBar = null!;
    private ProgressBar _demandAirFullBar = null!;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(360, 0);
        AddThemeStyleboxOverride("panel", ThemeFactory.Flat(Palette.BgPanel, Palette.Border, 0, 1));

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        AddChild(margin);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 8);
        margin.AddChild(col);

        var headerRow = new HBoxContainer();
        col.AddChild(headerRow);
        _headerLabel = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _headerLabel.AddThemeColorOverride("font_color", Palette.TextPrimary);
        _headerLabel.AddThemeFontSizeOverride("font_size", 16);
        headerRow.AddChild(_headerLabel);
        _closeButton = new Button { Text = "✕", CustomMinimumSize = new Vector2(28, 28) };
        _closeButton.Pressed += () => ClosePressed?.Invoke();
        headerRow.AddChild(_closeButton);

        _empireBlock = BuildEmpireBlock();
        col.AddChild(_empireBlock);
        _cityBlock = BuildCityBlock();
        col.AddChild(_cityBlock);

        _tabs = new TabContainer();
        _tabs.SizeFlagsVertical = SizeFlags.ExpandFill;
        _tabs.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        col.AddChild(_tabs);
        _tabs.AddChild(BuildBuyTab());
        _tabs.SetTabTitle(0, "Buy");
        _tabs.AddChild(BuildFleetTab());
        _tabs.SetTabTitle(1, "Fleet");
        _tabs.AddChild(BuildRoutesTab());
        _tabs.SetTabTitle(2, "Routes");

        var demandCard = new PanelContainer();
        demandCard.AddThemeStyleboxOverride("panel", ThemeFactory.Card());
        col.AddChild(demandCard);
        var demandCol = new VBoxContainer();
        demandCard.AddChild(demandCol);
        demandCol.AddChild(Caption("City Demand"));
        (_demandLocalFull, _demandLocalFullBar) = AddDemandRow(demandCol, "Local Transport");
        (_demandInterFull, _demandInterFullBar) = AddDemandRow(demandCol, "Intercity");
        (_demandAirFull, _demandAirFullBar) = AddDemandRow(demandCol, "Air Travel");

        var tip = new PanelContainer();
        tip.AddThemeStyleboxOverride("panel", ThemeFactory.Flat(Palette.BgCard, Palette.Border, 6, 1));
        col.AddChild(tip);
        _tipLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _tipLabel.AddThemeColorOverride("font_color", Palette.TextMuted);
        _tipLabel.AddThemeFontSizeOverride("font_size", 11);
        tip.AddChild(_tipLabel);
    }

    public int CurrentTab
    {
        get => _tabs.CurrentTab;
        set => _tabs.CurrentTab = value;
    }

    public string RouteName => _routeNameEdit.Text;

    public void BindSelection(UiSelection selection)
    {
        _showAllFleet.ButtonPressed = selection.ShowAllFleet;
        _showAllRoutes.ButtonPressed = selection.ShowAllRoutes;
        _draftModeOption.Selected = (int)selection.DraftMode;
        foreach (var (mode, chip) in _modeChips)
            StyleChip(chip, mode, mode == selection.BuyModeFilter);
    }

    public void Render(CityPanelViewModel vm, UiSelection selection, string? draftValidation)
    {
        _headerLabel.Text = vm.HeaderTitle;
        _closeButton.Visible = vm.HasSelection;
        _cityBlock.Visible = vm.HasSelection;
        _empireBlock.Visible = !vm.HasSelection;
        _empireSummary.Text = vm.CompanySummary;

        if (vm.HasSelection)
        {
            _popValue.Text = vm.PopulationLabel;
            _growthValue.Text = vm.GrowthLabel;
            _happyValue.Text = vm.HappinessLabel;
            _economyValue.Text = vm.EconomyLabel;
            _airportValue.Text = vm.AirportLabel;
            _portValue.Text = vm.PortLabel;
            _localBar.Value = vm.LocalDemandFill * 100;
            _intercityBar.Value = vm.IntercityDemandFill * 100;
            _airBar.Value = vm.AirDemandFill * 100;
            _localTag.Text = vm.LocalDemandLabel;
            _intercityTag.Text = vm.IntercityDemandLabel;
            _airTag.Text = vm.AirDemandLabel;
            _demandLocalFull.Text = vm.LocalDemandLabel;
            _demandInterFull.Text = vm.IntercityDemandLabel;
            _demandAirFull.Text = vm.AirDemandLabel;
            _demandLocalFullBar.Value = vm.LocalDemandFill * 100;
            _demandInterFullBar.Value = vm.IntercityDemandFill * 100;
            _demandAirFullBar.Value = vm.AirDemandFill * 100;
            var hue = HashHue(vm.CityId ?? "");
            _heroRect.Color = Color.FromHsv(hue, 0.35f, 0.35f);
            _heroInitial.Text = string.IsNullOrEmpty(vm.CityName) ? "?" : vm.CityName[..1].ToUpperInvariant();
        }

        _buyTable.SetSelectedId(selection.SelectedCatalogTypeId);
        _buyTable.SetRows(vm.CatalogRows.Select(r => (
            r.Id,
            new[]
            {
                r.IsUnlocked ? r.DisplayName : $"[locked] {r.DisplayName}",
                r.Capacity.ToString(),
                $"${r.RunningCostPerDayMinor / 100.0:N0}",
                $"${r.PriceMinor / 100.0:N0}"
            })).ToList());

        _fleetTable.SetSelectedId(selection.SelectedVehicleId?.ToString());
        _fleetTable.SetRows(vm.FleetRows.Select(r => (
            r.Id.ToString(),
            new[] { $"#{r.Id}", r.TypeId, FormatMode(r.Mode), r.StateLabel, r.RouteLabel, r.LoadLabel }
        )).ToList());

        _routeTable.SetSelectedId(selection.SelectedRouteId?.ToString());
        _routeTable.SetRows(vm.RouteRows.Select(r => (
            r.Id.ToString(),
            new[] { r.Name, FormatMode(r.Mode), r.StopsLabel, r.VehicleCount.ToString(), r.LoadLabel }
        )).ToList());

        _buyButton.Disabled = !vm.BuyEnabled;
        _buyButton.TooltipText = vm.BuyDisabledReason ?? "Buy vehicle";
        _tipLabel.Text = vm.Tip;

        var names = selection.DraftStops;
        _draftLabel.Text = names.Count == 0
            ? (selection.InputMode == MapInputMode.PlanRoute
                ? $"Planning {selection.DraftMode} — click cities"
                : "Press Plan new route to start")
            : $"{selection.DraftMode}: {string.Join(" → ", names)}";
        _validationLabel.Text = draftValidation ?? "";
        _validationLabel.Visible = !string.IsNullOrEmpty(draftValidation);
        _planButton.Text = selection.InputMode == MapInputMode.PlanRoute ? "Cancel plan" : "Plan new route";
    }

    public void UpdateFleetLive(CityPanelViewModel vm)
    {
        _fleetTable.SetRows(vm.FleetRows.Select(r => (
            r.Id.ToString(),
            new[] { $"#{r.Id}", r.TypeId, FormatMode(r.Mode), r.StateLabel, r.RouteLabel, r.LoadLabel }
        )).ToList());
        _routeTable.SetRows(vm.RouteRows.Select(r => (
            r.Id.ToString(),
            new[] { r.Name, FormatMode(r.Mode), r.StopsLabel, r.VehicleCount.ToString(), r.LoadLabel }
        )).ToList());
    }

    private Control BuildEmpireBlock()
    {
        var card = new PanelContainer();
        card.AddThemeStyleboxOverride("panel", ThemeFactory.Card());
        _empireSummary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _empireSummary.AddThemeColorOverride("font_color", Palette.TextMuted);
        card.AddChild(_empireSummary);
        return card;
    }

    private Control BuildCityBlock()
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 8);

        var hero = new PanelContainer();
        hero.CustomMinimumSize = new Vector2(0, 96);
        hero.AddThemeStyleboxOverride("panel", ThemeFactory.Flat(Palette.BgDeep, Palette.Border, 8, 1));
        box.AddChild(hero);
        var heroStack = new Control { CustomMinimumSize = new Vector2(0, 96) };
        hero.AddChild(heroStack);
        _heroRect = new ColorRect { Color = Palette.BgElevated };
        _heroRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        heroStack.AddChild(_heroRect);
        _heroInitial = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _heroInitial.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _heroInitial.AddThemeFontSizeOverride("font_size", 42);
        _heroInitial.AddThemeColorOverride("font_color", Palette.TextPrimary);
        heroStack.AddChild(_heroInitial);

        var grid = new GridContainer { Columns = 2 };
        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 4);
        box.AddChild(grid);

        (_popValue, _) = AddStat(grid, "Population");
        (_localBar, _localTag) = AddMiniDemand(grid, "Demand");
        (_growthValue, _) = AddStat(grid, "Growth");
        (_happyValue, _) = AddStat(grid, "Happiness");
        (_economyValue, _) = AddStat(grid, "Economy");
        (_airportValue, _) = AddStat(grid, "Airport");
        (_portValue, _) = AddStat(grid, "Port");
        (_intercityBar, _intercityTag) = AddMiniDemand(grid, "Intercity");
        (_airBar, _airTag) = AddMiniDemand(grid, "Air");

        return box;
    }

    private Control BuildBuyTab()
    {
        var page = new VBoxContainer { Name = "Buy" };
        page.AddThemeConstantOverride("separation", 8);

        var chips = new HBoxContainer();
        chips.AddThemeConstantOverride("separation", 6);
        page.AddChild(chips);
        foreach (var mode in new[] { TransportMode.Bus, TransportMode.Train, TransportMode.Air })
        {
            var label = mode switch
            {
                TransportMode.Bus => "Bus",
                TransportMode.Train => "Train",
                _ => "Plane"
            };
            var chip = new Button { Text = label, ToggleMode = true };
            var captured = mode;
            chip.Pressed += () => BuyModeChanged?.Invoke(captured);
            chips.AddChild(chip);
            _modeChips[mode] = chip;
            StyleChip(chip, mode, mode == TransportMode.Bus);
        }

        _buyTable = new DataTable();
        page.AddChild(_buyTable);
        _buyTable.Configure([
            new DataTableColumn { Title = "Vehicle", ExpandRatio = 2.2f },
            new DataTableColumn { Title = "Cap.", RightAlign = true, ExpandRatio = 0.7f },
            new DataTableColumn { Title = "$/day", RightAlign = true, ExpandRatio = 0.9f },
            new DataTableColumn { Title = "Price", RightAlign = true, ExpandRatio = 1.0f }
        ]);
        _buyTable.RowSelected += id => CatalogSelected?.Invoke(id);

        _buyButton = new Button { Text = "Buy Vehicle" };
        _buyButton.AddThemeStyleboxOverride("normal", ThemeFactory.PrimaryButton());
        _buyButton.AddThemeColorOverride("font_color", Palette.BgDeep);
        _buyButton.Pressed += () => BuyPressed?.Invoke();
        page.AddChild(_buyButton);
        return page;
    }

    private Control BuildFleetTab()
    {
        var page = new VBoxContainer { Name = "Fleet" };
        page.AddThemeConstantOverride("separation", 8);
        _showAllFleet = new Button { Text = "Show all", ToggleMode = true };
        _showAllFleet.Pressed += () => ShowAllFleetChanged?.Invoke(_showAllFleet.ButtonPressed);
        page.AddChild(_showAllFleet);

        _fleetTable = new DataTable();
        page.AddChild(_fleetTable);
        _fleetTable.Configure([
            new DataTableColumn { Title = "#", ExpandRatio = 0.6f },
            new DataTableColumn { Title = "Type", ExpandRatio = 1.4f },
            new DataTableColumn { Title = "Mode", ExpandRatio = 0.7f },
            new DataTableColumn { Title = "State", ExpandRatio = 1.0f },
            new DataTableColumn { Title = "Route", ExpandRatio = 0.7f },
            new DataTableColumn { Title = "Load", RightAlign = true, ExpandRatio = 0.8f }
        ], "No vehicles yet.");
        _fleetTable.RowSelected += id =>
        {
            if (long.TryParse(id, out var vehicleId))
                VehicleSelected?.Invoke(vehicleId);
        };

        _assignButton = new Button { Text = "Assign to route…" };
        _assignButton.Pressed += () => AssignPressed?.Invoke();
        page.AddChild(_assignButton);
        return page;
    }

    private Control BuildRoutesTab()
    {
        var page = new VBoxContainer { Name = "Routes" };
        page.AddThemeConstantOverride("separation", 8);
        _showAllRoutes = new Button { Text = "Show all", ToggleMode = true };
        _showAllRoutes.Pressed += () => ShowAllRoutesChanged?.Invoke(_showAllRoutes.ButtonPressed);
        page.AddChild(_showAllRoutes);

        _routeTable = new DataTable();
        _routeTable.CustomMinimumSize = new Vector2(0, 120);
        page.AddChild(_routeTable);
        _routeTable.Configure([
            new DataTableColumn { Title = "Name", ExpandRatio = 1.2f },
            new DataTableColumn { Title = "Mode", ExpandRatio = 0.6f },
            new DataTableColumn { Title = "Stops", ExpandRatio = 1.6f },
            new DataTableColumn { Title = "Veh", RightAlign = true, ExpandRatio = 0.5f },
            new DataTableColumn { Title = "Load", RightAlign = true, ExpandRatio = 0.7f }
        ], "No routes yet.");
        _routeTable.RowSelected += id =>
        {
            if (long.TryParse(id, out var routeId))
                RouteSelected?.Invoke(routeId);
        };

        _planButton = new Button { Text = "Plan new route" };
        _planButton.Pressed += () => PlanRoutePressed?.Invoke();
        page.AddChild(_planButton);

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        page.AddChild(scroll);
        var form = new VBoxContainer();
        form.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(form);

        _routeNameEdit = new LineEdit { Text = "New Service", PlaceholderText = "Route name" };
        _routeNameEdit.TextChanged += text => RouteNameChanged?.Invoke(text);
        form.AddChild(_routeNameEdit);

        _draftModeOption = new OptionButton();
        _draftModeOption.AddItem("Bus", (int)TransportMode.Bus);
        _draftModeOption.AddItem("Train", (int)TransportMode.Train);
        _draftModeOption.AddItem("Air", (int)TransportMode.Air);
        _draftModeOption.ItemSelected += id => DraftModeChanged?.Invoke((TransportMode)(int)id);
        form.AddChild(_draftModeOption);

        _draftLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        form.AddChild(_draftLabel);
        _validationLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _validationLabel.AddThemeColorOverride("font_color", Palette.Danger);
        form.AddChild(_validationLabel);

        var row = new HBoxContainer();
        form.AddChild(row);
        _createButton = new Button { Text = "Create route", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _createButton.AddThemeStyleboxOverride("normal", ThemeFactory.PrimaryButton());
        _createButton.AddThemeColorOverride("font_color", Palette.BgDeep);
        _createButton.Pressed += () => CreateRoutePressed?.Invoke();
        row.AddChild(_createButton);
        var clear = new Button { Text = "Clear" };
        clear.Pressed += () => ClearDraftPressed?.Invoke();
        row.AddChild(clear);

        return page;
    }

    private static (Label Value, Label _) AddStat(GridContainer grid, string name)
    {
        grid.AddChild(Caption(name));
        var value = new Label { Text = "—", HorizontalAlignment = HorizontalAlignment.Right };
        value.AddThemeColorOverride("font_color", Palette.TextPrimary);
        grid.AddChild(value);
        return (value, value);
    }

    private static (ProgressBar Bar, Label Tag) AddMiniDemand(GridContainer grid, string name)
    {
        grid.AddChild(Caption(name));
        var wrap = new HBoxContainer();
        grid.AddChild(wrap);
        var bar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = 0,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(60, 10),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter
        };
        wrap.AddChild(bar);
        var tag = new Label { Text = "—" };
        tag.AddThemeColorOverride("font_color", Palette.TextMuted);
        tag.AddThemeFontSizeOverride("font_size", 10);
        wrap.AddChild(tag);
        return (bar, tag);
    }

    private static (Label Tag, ProgressBar Bar) AddDemandRow(VBoxContainer parent, string title)
    {
        var row = new HBoxContainer();
        parent.AddChild(row);
        var label = new Label { Text = title, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        label.AddThemeColorOverride("font_color", Palette.TextMuted);
        label.AddThemeFontSizeOverride("font_size", 11);
        row.AddChild(label);
        var tag = new Label { Text = "—" };
        tag.AddThemeColorOverride("font_color", Palette.TextPrimary);
        tag.AddThemeFontSizeOverride("font_size", 11);
        row.AddChild(tag);
        var bar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 8)
        };
        parent.AddChild(bar);
        return (tag, bar);
    }

    private static Label Caption(string text)
    {
        var label = new Label { Text = text.ToUpperInvariant() };
        label.AddThemeColorOverride("font_color", Palette.TextDim);
        label.AddThemeFontSizeOverride("font_size", 10);
        return label;
    }

    private static void StyleChip(Button chip, TransportMode mode, bool active)
    {
        var color = Palette.Mode(mode);
        chip.ButtonPressed = active;
        chip.AddThemeStyleboxOverride("normal", ThemeFactory.Flat(
            active ? color : Palette.BgElevated,
            color,
            12, 1));
        chip.AddThemeColorOverride("font_color", active ? Palette.BgDeep : Palette.TextPrimary);
    }

    private static string FormatMode(TransportMode mode) => mode switch
    {
        TransportMode.Bus => "Bus",
        TransportMode.Train => "Rail",
        TransportMode.Air => "Air",
        _ => mode.ToString()
    };

    private static float HashHue(string id)
    {
        unchecked
        {
            var hash = 17;
            foreach (var c in id)
                hash = hash * 31 + c;
            return Math.Abs(hash % 1000) / 1000f;
        }
    }
}
