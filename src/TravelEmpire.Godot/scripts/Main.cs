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
    private Tree _catalogTree = null!;
    private Tree _fleetTree = null!;
    private Tree _routeTree = null!;
    private LineEdit _routeNameEdit = null!;
    private OptionButton _modeOption = null!;
    private TabContainer _tabs = null!;
    private readonly List<string> _draftStops = [];
    private TransportMode _draftMode = TransportMode.Bus;
    private string? _selectedCatalogTypeId;
    private long? _selectedVehicleId;
    private long? _selectedRouteId;
    private bool _listsDirty = true;

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
        if (!_host.ConsumeDirty())
            return;

        var snap = _host.GetSnapshot();
        RefreshTop(snap);
        _map.UpdateFromSnapshot(snap);
        if (_listsDirty)
            RefreshLists(snap);
        else
            RefreshFleetLive(snap);
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

        var side = new PanelContainer { CustomMinimumSize = new Vector2(460, 0) };
        body.AddChild(side);

        var sideMargin = new MarginContainer();
        sideMargin.AddThemeConstantOverride("margin_left", 10);
        sideMargin.AddThemeConstantOverride("margin_right", 10);
        sideMargin.AddThemeConstantOverride("margin_top", 10);
        sideMargin.AddThemeConstantOverride("margin_bottom", 10);
        side.AddChild(sideMargin);

        var sideCol = new VBoxContainer();
        sideCol.AddThemeConstantOverride("separation", 10);
        sideMargin.AddChild(sideCol);

        sideCol.AddChild(Section("Selection"));
        _inspectorLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Text = "Click a city on the map."
        };
        sideCol.AddChild(_inspectorLabel);

        _tabs = new TabContainer();
        _tabs.SizeFlagsVertical = SizeFlags.ExpandFill;
        _tabs.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        sideCol.AddChild(_tabs);

        _tabs.AddChild(BuildBuyTab());
        _tabs.SetTabTitle(0, "Buy");
        _tabs.AddChild(BuildFleetTab());
        _tabs.SetTabTitle(1, "Fleet");
        _tabs.AddChild(BuildRoutesTab());
        _tabs.SetTabTitle(2, "Routes");

        _statusLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(0, 36)
        };
        sideCol.AddChild(_statusLabel);
    }

    private Control BuildBuyTab()
    {
        var page = new VBoxContainer();
        page.Name = "Buy";
        page.AddThemeConstantOverride("separation", 8);

        var hint = new Label
        {
            Text = "Select a vehicle type, then buy.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        page.AddChild(hint);

        _catalogTree = CreateTable(5, ["Type", "Mode", "Cap.", "Speed", "Price"]);
        _catalogTree.SizeFlagsVertical = SizeFlags.ExpandFill;
        _catalogTree.ItemSelected += OnCatalogSelected;
        page.AddChild(_catalogTree);

        var buy = new Button { Text = "Buy selected vehicle" };
        buy.Pressed += OnBuy;
        page.AddChild(buy);
        return page;
    }

    private Control BuildFleetTab()
    {
        var page = new VBoxContainer();
        page.Name = "Fleet";
        page.AddThemeConstantOverride("separation", 8);

        var hint = new Label
        {
            Text = "Select a vehicle here and a route on the Routes tab, then assign.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        page.AddChild(hint);

        _fleetTree = CreateTable(6, ["#", "Type", "Mode", "State", "Route", "Pax"]);
        _fleetTree.SizeFlagsVertical = SizeFlags.ExpandFill;
        _fleetTree.ItemSelected += OnFleetSelected;
        page.AddChild(_fleetTree);

        var assign = new Button { Text = "Assign selected vehicle → selected route" };
        assign.Pressed += OnAssign;
        page.AddChild(assign);
        return page;
    }

    private Control BuildRoutesTab()
    {
        var page = new VBoxContainer();
        page.Name = "Routes";
        page.AddThemeConstantOverride("separation", 8);

        _routeTree = CreateTable(5, ["#", "Name", "Mode", "Stops", "Vehicles"]);
        _routeTree.CustomMinimumSize = new Vector2(0, 140);
        _routeTree.SizeFlagsVertical = SizeFlags.ExpandFill;
        _routeTree.ItemSelected += OnRouteSelected;
        page.AddChild(_routeTree);

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        page.AddChild(scroll);

        var form = new VBoxContainer();
        form.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        form.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(form);

        form.AddChild(Section("Create route"));
        form.AddChild(new Label
        {
            Text = "Pick mode, click cities on the map in order, then create.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        _routeNameEdit = new LineEdit { Text = "New Service", PlaceholderText = "Route name" };
        form.AddChild(_routeNameEdit);

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
        form.AddChild(_modeOption);

        _draftLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        form.AddChild(_draftLabel);
        UpdateDraft();

        var row = new HBoxContainer();
        form.AddChild(row);
        var create = new Button { Text = "Create route", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        create.Pressed += OnCreateRoute;
        row.AddChild(create);
        var clear = new Button { Text = "Clear stops" };
        clear.Pressed += () =>
        {
            _draftStops.Clear();
            UpdateDraft();
        };
        row.AddChild(clear);

        return page;
    }

    private static Tree CreateTable(int columns, string[] titles)
    {
        var tree = new Tree
        {
            Columns = columns,
            ColumnTitlesVisible = true,
            HideRoot = true,
            SelectMode = Tree.SelectModeEnum.Row,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 160)
        };

        for (var i = 0; i < columns; i++)
        {
            tree.SetColumnTitle(i, titles[i]);
            tree.SetColumnClipContent(i, true);
            tree.SetColumnExpand(i, i == 0 || i == titles.Length - 2);
            if (i == 0)
                tree.SetColumnExpandRatio(i, 3);
            else
                tree.SetColumnCustomMinimumWidth(i, 56);
        }

        return tree;
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

        if (_tabs.CurrentTab != 2)
            _tabs.CurrentTab = 2;
    }

    private void UpdateDraft()
    {
        if (_draftLabel is null)
            return;

        var snap = _host.GetSnapshot();
        var names = _draftStops
            .Select(id => snap.Cities.FirstOrDefault(c => c.Id == id)?.Name ?? id)
            .ToList();
        _draftLabel.Text = names.Count == 0
            ? $"Stops ({_draftMode}): click cities on the map"
            : $"Stops ({_draftMode}): {string.Join(" → ", names)}";
    }

    private void OnCatalogSelected()
    {
        var item = _catalogTree.GetSelected();
        if (item is null)
            return;
        _selectedCatalogTypeId = item.GetMetadata(0).AsString();
    }

    private void OnFleetSelected()
    {
        var item = _fleetTree.GetSelected();
        if (item is null)
            return;
        _selectedVehicleId = item.GetMetadata(0).AsInt64();
    }

    private void OnRouteSelected()
    {
        var item = _routeTree.GetSelected();
        if (item is null)
            return;
        _selectedRouteId = item.GetMetadata(0).AsInt64();
    }

    private void OnBuy()
    {
        var typeId = _selectedCatalogTypeId;
        if (string.IsNullOrEmpty(typeId))
        {
            var item = _catalogTree.GetSelected();
            if (item is not null)
                typeId = item.GetMetadata(0).AsString();
        }

        if (string.IsNullOrEmpty(typeId))
        {
            SetStatus("Select a vehicle type in the Buy table.");
            _tabs.CurrentTab = 0;
            return;
        }

        var result = _host.Apply(new BuyVehicleCommand { TypeId = new VehicleTypeId(typeId) });
        SetStatus(result.Success ? $"Bought {typeId}." : result.ErrorMessage ?? "Buy failed.");
        _listsDirty = true;
        RefreshAll();
        if (result.Success)
            _tabs.CurrentTab = 1;
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

        _listsDirty = true;
        RefreshAll();
    }

    private void OnAssign()
    {
        var vehicleId = _selectedVehicleId;
        var routeId = _selectedRouteId;

        if (vehicleId is null)
        {
            var item = _fleetTree.GetSelected();
            if (item is not null)
                vehicleId = item.GetMetadata(0).AsInt64();
        }

        if (routeId is null)
        {
            var item = _routeTree.GetSelected();
            if (item is not null)
                routeId = item.GetMetadata(0).AsInt64();
        }

        if (vehicleId is null || routeId is null)
        {
            SetStatus("Select a vehicle (Fleet) and a route (Routes), then assign.");
            return;
        }

        var result = _host.Apply(new AssignVehicleCommand
        {
            VehicleId = new VehicleId(vehicleId.Value),
            RouteId = new RouteId(routeId.Value)
        });
        SetStatus(result.Success ? "Vehicle assigned." : result.ErrorMessage ?? "Assign failed.");
        _listsDirty = true;
        RefreshAll();
    }

    private void RefreshAll(string? status = null)
    {
        var snap = _host.GetSnapshot();
        RefreshTop(snap);
        _map.UpdateFromSnapshot(snap);
        if (_listsDirty)
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
        _listsDirty = false;

        RebuildCatalog(snap);
        RebuildFleet(snap);
        RebuildRoutes(snap);
    }

    private void RebuildCatalog(GameSnapshot snap)
    {
        _catalogTree.Clear();
        var root = _catalogTree.CreateItem();
        TreeItem? toSelect = null;

        foreach (var type in snap.Catalog)
        {
            var item = _catalogTree.CreateItem(root);
            item.SetText(0, type.DisplayName);
            item.SetText(1, type.Mode.ToString());
            item.SetText(2, type.CapacityPassengers.ToString());
            item.SetText(3, $"{type.CruiseSpeedKmh:0} km/h");
            item.SetText(4, $"${type.PurchaseCostMinor / 100.0:N0}");
            item.SetMetadata(0, type.Id);

            if (type.Id == _selectedCatalogTypeId)
                toSelect = item;
        }

        if (toSelect is not null)
            toSelect.Select(0);
        else if (_selectedCatalogTypeId is not null)
            _selectedCatalogTypeId = null;
    }

    private void RebuildFleet(GameSnapshot snap)
    {
        _fleetTree.Clear();
        var root = _fleetTree.CreateItem();
        TreeItem? toSelect = null;

        foreach (var vehicle in snap.Vehicles)
        {
            var item = _fleetTree.CreateItem(root);
            WriteFleetRow(item, vehicle);

            if (vehicle.Id == _selectedVehicleId)
                toSelect = item;
        }

        if (toSelect is not null)
            toSelect.Select(0);
        else if (_selectedVehicleId is not null && snap.Vehicles.All(v => v.Id != _selectedVehicleId))
            _selectedVehicleId = null;
    }

    private void RefreshFleetLive(GameSnapshot snap)
    {
        var byId = snap.Vehicles.ToDictionary(v => v.Id);
        var item = _fleetTree.GetRoot()?.GetFirstChild();
        while (item is not null)
        {
            var id = item.GetMetadata(0).AsInt64();
            if (byId.TryGetValue(id, out var vehicle))
                WriteFleetRow(item, vehicle);
            item = item.GetNext();
        }
    }

    private static void WriteFleetRow(TreeItem item, VehicleView vehicle)
    {
        item.SetText(0, vehicle.Id.ToString());
        item.SetText(1, vehicle.TypeId);
        item.SetText(2, vehicle.Mode.ToString());
        item.SetText(3, vehicle.State.ToString());
        item.SetText(4, vehicle.AssignedRouteId is null ? "—" : $"#{vehicle.AssignedRouteId}");
        item.SetText(5, vehicle.OnboardPassengers.ToString());
        item.SetMetadata(0, vehicle.Id);
    }

    private void RebuildRoutes(GameSnapshot snap)
    {
        _routeTree.Clear();
        var root = _routeTree.CreateItem();
        TreeItem? toSelect = null;

        foreach (var route in snap.Routes)
        {
            var stops = string.Join("→", route.StopIds.Select(ShortCity));
            var item = _routeTree.CreateItem(root);
            item.SetText(0, route.Id.ToString());
            item.SetText(1, route.Name);
            item.SetText(2, route.Mode.ToString());
            item.SetText(3, stops);
            item.SetText(4, route.VehicleIds.Count.ToString());
            item.SetMetadata(0, route.Id);

            if (route.Id == _selectedRouteId)
                toSelect = item;
        }

        if (toSelect is not null)
            toSelect.Select(0);
        else if (_selectedRouteId is not null && snap.Routes.All(r => r.Id != _selectedRouteId))
            _selectedRouteId = null;
    }

    private static string ShortCity(string cityId) =>
        cityId.StartsWith("city.", StringComparison.Ordinal) ? cityId["city.".Length..] : cityId;

    private void SetStatus(string text) => _statusLabel.Text = text;
}
