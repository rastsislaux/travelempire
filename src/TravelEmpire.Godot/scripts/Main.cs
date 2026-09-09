using Godot;
using TravelEmpire.Simulation;
using TravelEmpire.Simulation.Commands;
using TravelEmpire.Simulation.Views;

namespace TravelEmpire.Godot;

public partial class Main : Control
{
    private SimulationHost _host = null!;
    private MapView _map = null!;
    private Label _cashLabel = null!;
    private Label _timeLabel = null!;
    private Label _speedLabel = null!;
    private Label _statusLabel = null!;
    private Label _inspectorLabel = null!;
    private Label _draftLabel = null!;
    private ItemList _catalogList = null!;
    private ItemList _fleetList = null!;
    private ItemList _routeList = null!;
    private LineEdit _routeNameEdit = null!;
    private OptionButton _modeOption = null!;
    private readonly List<string> _draftStops = [];
    private TransportMode _draftMode = TransportMode.Bus;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _host = new SimulationHost();
        AddChild(_host);
        BuildUi();
        RefreshAll("Welcome to TravelEmpire — Republic of Aurelia.");
    }

    public override void _Process(double delta)
    {
        if (_host.ConsumeDirty())
            RefreshAll();
    }

    private void BuildUi()
    {
        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 0);
        AddChild(root);
        root.AddChild(BuildTopBar());

        var body = new HBoxContainer();
        body.SizeFlagsVertical = SizeFlags.ExpandFill;
        body.AddThemeConstantOverride("separation", 0);
        root.AddChild(body);

        var mapPanel = new PanelContainer();
        mapPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        mapPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
        body.AddChild(mapPanel);

        _map = new MapView();
        _map.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _map.SizeFlagsVertical = SizeFlags.ExpandFill;
        _map.CityClicked += OnCityClicked;
        mapPanel.AddChild(_map);

        var side = new PanelContainer { CustomMinimumSize = new Vector2(340, 0) };
        body.AddChild(side);

        var margin = new MarginContainer();
        foreach (var (key, value) in new Dictionary<string, int>
                 {
                     ["margin_left"] = 10, ["margin_right"] = 10,
                     ["margin_top"] = 10, ["margin_bottom"] = 10
                 })
            margin.AddThemeConstantOverride(key, value);
        side.AddChild(margin);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 8);
        margin.AddChild(col);

        col.AddChild(Section("Inspector"));
        _inspectorLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, Text = "Click a city." };
        col.AddChild(_inspectorLabel);

        col.AddChild(Section("Buy vehicle"));
        _catalogList = new ItemList { CustomMinimumSize = new Vector2(0, 90) };
        col.AddChild(_catalogList);
        var buy = new Button { Text = "Buy selected" };
        buy.Pressed += OnBuy;
        col.AddChild(buy);

        col.AddChild(Section("Fleet"));
        _fleetList = new ItemList { CustomMinimumSize = new Vector2(0, 90) };
        col.AddChild(_fleetList);

        col.AddChild(Section("Routes"));
        _routeList = new ItemList { CustomMinimumSize = new Vector2(0, 80) };
        col.AddChild(_routeList);
        var assign = new Button { Text = "Assign vehicle → route" };
        assign.Pressed += OnAssign;
        col.AddChild(assign);

        col.AddChild(Section("Create route"));
        _routeNameEdit = new LineEdit { Text = "New Service", PlaceholderText = "Route name" };
        col.AddChild(_routeNameEdit);
        _modeOption = new OptionButton();
        _modeOption.AddItem("Bus", (int)TransportMode.Bus);
        _modeOption.AddItem("Train", (int)TransportMode.Train);
        _modeOption.AddItem("Air", (int)TransportMode.Air);
        _modeOption.ItemSelected += id =>
        {
            _draftMode = (TransportMode)(int)id;
            _draftStops.Clear();
            UpdateDraft();
        };
        col.AddChild(_modeOption);
        _draftLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        col.AddChild(_draftLabel);
        UpdateDraft();

        var row = new HBoxContainer();
        col.AddChild(row);
        var create = new Button { Text = "Create", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        create.Pressed += OnCreateRoute;
        row.AddChild(create);
        var clear = new Button { Text = "Clear stops" };
        clear.Pressed += () => { _draftStops.Clear(); UpdateDraft(); };
        row.AddChild(clear);

        _statusLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(0, 48)
        };
        col.AddChild(_statusLabel);
    }

    private Control BuildTopBar()
    {
        var bar = new PanelContainer();
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        bar.AddChild(margin);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        margin.AddChild(row);

        var title = new Label { Text = "TravelEmpire" };
        title.AddThemeColorOverride("font_color", new Color(0.95f, 0.85f, 0.45f));
        row.AddChild(title);

        _cashLabel = new Label();
        row.AddChild(_cashLabel);
        _timeLabel = new Label();
        row.AddChild(_timeLabel);
        _speedLabel = new Label();
        row.AddChild(_speedLabel);
        row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        foreach (var (text, speed) in new (string, int)[] { ("Pause", 0), ("1x", 1), ("2x", 2), ("4x", 4) })
        {
            var btn = new Button { Text = text };
            var s = speed;
            btn.Pressed += () =>
            {
                _host.SetSpeed(s);
                RefreshTop(_host.GetSnapshot());
            };
            row.AddChild(btn);
        }

        return bar;
    }

    private static Label Section(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeColorOverride("font_color", new Color(0.70f, 0.85f, 0.95f));
        return label;
    }

    private void OnCityClicked(string cityId)
    {
        var city = _host.GetSnapshot().Cities.First(c => c.Id == cityId);
        _inspectorLabel.Text =
            $"{city.Name}\nPop {city.Population:N0}\nBus {(city.HasBusTerminal ? "yes" : "no")} · Rail {(city.HasRailStation ? "yes" : "no")} · Air {(city.HasAirport ? "yes" : "no")}";

        if (_draftStops.Count == 0 || _draftStops[^1] != cityId)
            _draftStops.Add(cityId);

        UpdateDraft();
        _map.SelectedCityId = cityId;
        _map.QueueRedraw();
    }

    private void UpdateDraft()
    {
        var snap = _host.GetSnapshot();
        var names = _draftStops
            .Select(id => snap.Cities.FirstOrDefault(c => c.Id == id)?.Name ?? id)
            .ToList();
        _draftLabel.Text = names.Count == 0
            ? $"Stops ({_draftMode}): click cities"
            : $"Stops ({_draftMode}): {string.Join(" → ", names)}";
    }

    private void OnBuy()
    {
        var selected = _catalogList.GetSelectedItems();
        if (selected.Length == 0)
        {
            SetStatus("Select a vehicle type.");
            return;
        }

        var typeId = (string)_catalogList.GetItemMetadata(selected[0]);
        var result = _host.Apply(new BuyVehicleCommand { TypeId = new VehicleTypeId(typeId) });
        SetStatus(result.Success ? $"Bought {typeId}." : result.ErrorMessage ?? "Buy failed.");
        RefreshAll();
    }

    private void OnCreateRoute()
    {
        var result = _host.Apply(new CreateRouteCommand
        {
            Name = string.IsNullOrWhiteSpace(_routeNameEdit.Text) ? "Route" : _routeNameEdit.Text.Trim(),
            Mode = _draftMode,
            Stops = _draftStops.Select(id => new CityId(id)).ToList()
        });
        SetStatus(result.Success ? "Route created." : result.ErrorMessage ?? "Create failed.");
        if (result.Success)
        {
            _draftStops.Clear();
            UpdateDraft();
        }

        RefreshAll();
    }

    private void OnAssign()
    {
        var vehicles = _fleetList.GetSelectedItems();
        var routes = _routeList.GetSelectedItems();
        if (vehicles.Length == 0 || routes.Length == 0)
        {
            SetStatus("Select a vehicle and a route.");
            return;
        }

        var result = _host.Apply(new AssignVehicleCommand
        {
            VehicleId = new VehicleId((long)_fleetList.GetItemMetadata(vehicles[0])),
            RouteId = new RouteId((long)_routeList.GetItemMetadata(routes[0]))
        });
        SetStatus(result.Success ? "Vehicle assigned." : result.ErrorMessage ?? "Assign failed.");
        RefreshAll();
    }

    private void RefreshAll(string? status = null)
    {
        var snap = _host.GetSnapshot();
        RefreshTop(snap);
        _map.UpdateFromSnapshot(snap);
        RefreshLists(snap);
        if (status is not null)
            SetStatus(status);
    }

    private void RefreshTop(GameSnapshot snap)
    {
        var cash = snap.Company?.CashMinor / 100.0 ?? 0;
        _cashLabel.Text = $"Cash ${cash:N0}";
        _timeLabel.Text = $"Day {snap.SimHours / 24.0:0.00}";
        _speedLabel.Text = _host.SpeedMultiplier == 0 ? "Paused" : $"{_host.SpeedMultiplier}x";
    }

    private void RefreshLists(GameSnapshot snap)
    {
        _catalogList.Clear();
        foreach (var type in snap.Catalog)
        {
            var idx = _catalogList.AddItem(
                $"{type.DisplayName} (${type.PurchaseCostMinor / 100.0:N0}) · {type.Mode}");
            _catalogList.SetItemMetadata(idx, type.Id);
        }

        _fleetList.Clear();
        foreach (var vehicle in snap.Vehicles)
        {
            var assign = vehicle.AssignedRouteId is null ? "idle" : $"route #{vehicle.AssignedRouteId}";
            var idx = _fleetList.AddItem(
                $"#{vehicle.Id} {vehicle.TypeId} · {vehicle.State} · {assign} · pax {vehicle.OnboardPassengers}");
            _fleetList.SetItemMetadata(idx, vehicle.Id);
        }

        _routeList.Clear();
        foreach (var route in snap.Routes)
        {
            var stops = string.Join("-", route.StopIds.Select(s => s.Replace("city.", "")));
            var idx = _routeList.AddItem(
                $"#{route.Id} {route.Name} [{route.Mode}] {stops} · {route.VehicleIds.Count} veh");
            _routeList.SetItemMetadata(idx, route.Id);
        }
    }

    private void SetStatus(string text) => _statusLabel.Text = text;
}
