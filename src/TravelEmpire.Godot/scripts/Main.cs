using Godot;
using TravelEmpire.Simulation;
using TravelEmpire.Simulation.Commands;
using TravelEmpire.Simulation.Presentation;
using TravelEmpire.Simulation.Views;

namespace TravelEmpire.Godot;

public partial class Main : Control
{
    private SimulationHost _host = null!;
    private TopBar _topBar = null!;
    private MapView _map = null!;
    private SidePanel _side = null!;
    private ToastLayer _toasts = null!;
    private readonly UiSelection _selection = new();
    private GameSnapshot? _frameSnap;
    private bool _listsDirty = true;
    private Window? _newGameDialog;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Theme = ThemeFactory.Create();

        _host = new SimulationHost();
        AddChild(_host);

        BuildUi();
        WireEvents();
        _listsDirty = true;
        RefreshAll();
        _toasts.ShowToast("Welcome to TravelEmpire.");
    }

    public override void _Process(double delta)
    {
        if (!_host.ConsumeDirty())
            return;

        _frameSnap = _host.GetSnapshot();
        RefreshTop(_frameSnap);
        _map.UpdateFromSnapshot(_frameSnap);
        SyncMapSelection();

        if (_listsDirty)
            RefreshPanel(_frameSnap);
        else
            RefreshPanelLive(_frameSnap);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key)
            return;

        switch (key.Keycode)
        {
            case Key.Space:
                _host.SetSpeed(_host.SpeedMultiplier == 0 ? 1 : 0);
                RefreshTop(Snap());
                GetViewport().SetInputAsHandled();
                break;
            case Key.Key1:
                _host.SetSpeed(1);
                RefreshTop(Snap());
                GetViewport().SetInputAsHandled();
                break;
            case Key.Key2:
                _host.SetSpeed(2);
                RefreshTop(Snap());
                GetViewport().SetInputAsHandled();
                break;
            case Key.Key3:
                _host.SetSpeed(4);
                RefreshTop(Snap());
                GetViewport().SetInputAsHandled();
                break;
            case Key.Escape:
                if (_selection.InputMode == MapInputMode.PlanRoute)
                    ExitPlanMode();
                else if (_selection.SelectedCityId is not null)
                    ClearCitySelection();
                GetViewport().SetInputAsHandled();
                break;
            case Key.F:
                _map.FitToContent();
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    private void BuildUi()
    {
        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 0);
        AddChild(root);

        _topBar = new TopBar();
        root.AddChild(_topBar);

        var body = new HBoxContainer();
        body.SizeFlagsVertical = SizeFlags.ExpandFill;
        body.AddThemeConstantOverride("separation", 0);
        root.AddChild(body);

        var mapHost = new Control();
        mapHost.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        mapHost.SizeFlagsVertical = SizeFlags.ExpandFill;
        body.AddChild(mapHost);

        _map = new MapView();
        _map.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        mapHost.AddChild(_map);

        _toasts = new ToastLayer();
        mapHost.AddChild(_toasts);

        _side = new SidePanel();
        body.AddChild(_side);
    }

    private void WireEvents()
    {
        _topBar.SpeedChanged += speed =>
        {
            _host.SetSpeed(speed);
            RefreshTop(Snap());
        };
        _topBar.NewGamePressed += OpenNewGameDialog;

        _map.CityClicked += OnCityClicked;

        _side.ClosePressed += ClearCitySelection;
        _side.BuyModeChanged += mode =>
        {
            _selection.BuyModeFilter = mode;
            _side.BindSelection(_selection);
            _listsDirty = true;
            RefreshPanel(Snap());
        };
        _side.CatalogSelected += id =>
        {
            _selection.SelectedCatalogTypeId = id;
            RefreshPanel(Snap());
        };
        _side.VehicleSelected += id =>
        {
            _selection.SelectedVehicleId = id;
        };
        _side.RouteSelected += id =>
        {
            _selection.SelectedRouteId = id;
        };
        _side.BuyPressed += OnBuy;
        _side.AssignPressed += OnAssign;
        _side.PlanRoutePressed += TogglePlanMode;
        _side.CreateRoutePressed += OnCreateRoute;
        _side.ClearDraftPressed += () =>
        {
            _selection.DraftStops.Clear();
            SyncMapSelection();
            RefreshPanel(Snap());
        };
        _side.ShowAllFleetChanged += show =>
        {
            _selection.ShowAllFleet = show;
            _listsDirty = true;
            RefreshPanel(Snap());
        };
        _side.ShowAllRoutesChanged += show =>
        {
            _selection.ShowAllRoutes = show;
            _listsDirty = true;
            RefreshPanel(Snap());
        };
        _side.DraftModeChanged += mode =>
        {
            _selection.DraftMode = mode;
            _selection.DraftStops.Clear();
            SyncMapSelection();
            RefreshPanel(Snap());
        };
    }

    private void OnCityClicked(string cityId)
    {
        _selection.SelectedCityId = cityId;
        _map.SelectedCityId = cityId;

        if (_selection.InputMode == MapInputMode.PlanRoute)
        {
            if (_selection.DraftStops.Count == 0 || _selection.DraftStops[^1] != cityId)
            {
                var candidate = _selection.DraftStops.Append(cityId).Select(id => new CityId(id)).ToList();
                var validation = _host.Sim.ValidateCreateRoute(_selection.DraftMode, candidate);
                if (!validation.Success && candidate.Count >= 2)
                {
                    _toasts.ShowToast(validation.ErrorMessage ?? "Invalid stop.", error: true);
                }
                else
                {
                    _selection.DraftStops.Add(cityId);
                }
            }
        }

        _listsDirty = true;
        RefreshPanel(Snap());
        SyncMapSelection();
        _map.QueueRedraw();
    }

    private void OnBuy()
    {
        var typeId = _selection.SelectedCatalogTypeId;
        if (string.IsNullOrEmpty(typeId))
        {
            _toasts.ShowToast("Select a vehicle type.", error: true);
            _side.CurrentTab = 0;
            return;
        }

        var result = _host.Apply(new BuyVehicleCommand { TypeId = new VehicleTypeId(typeId) });
        if (result.Success)
        {
            _toasts.ShowToast($"Bought {typeId}.");
            _side.CurrentTab = 1;
        }
        else
        {
            _toasts.ShowToast(result.ErrorMessage ?? "Buy failed.", error: true);
            if (result.ErrorCode == "vehicle.insufficient_funds")
                _topBar.FlashCashError();
        }

        _listsDirty = true;
        RefreshAll();
    }

    private void OnCreateRoute()
    {
        var result = _host.Apply(new CreateRouteCommand
        {
            Name = string.IsNullOrWhiteSpace(_side.RouteName) ? "Route" : _side.RouteName.Trim(),
            Mode = _selection.DraftMode,
            Stops = _selection.DraftStops.Select(id => new CityId(id)).ToList()
        });

        if (result.Success)
        {
            _toasts.ShowToast("Route created.");
            _selection.DraftStops.Clear();
            _selection.InputMode = MapInputMode.Select;
        }
        else
        {
            _toasts.ShowToast(result.ErrorMessage ?? "Create failed.", error: true);
        }

        _listsDirty = true;
        SyncMapSelection();
        RefreshAll();
    }

    private void OnAssign()
    {
        if (_selection.SelectedVehicleId is null || _selection.SelectedRouteId is null)
        {
            _toasts.ShowToast("Select a vehicle and a route first.", error: true);
            return;
        }

        var result = _host.Apply(new AssignVehicleCommand
        {
            VehicleId = new VehicleId(_selection.SelectedVehicleId.Value),
            RouteId = new RouteId(_selection.SelectedRouteId.Value)
        });
        _toasts.ShowToast(result.Success ? "Vehicle assigned." : result.ErrorMessage ?? "Assign failed.",
            error: !result.Success);
        _listsDirty = true;
        RefreshAll();
    }

    private void TogglePlanMode()
    {
        if (_selection.InputMode == MapInputMode.PlanRoute)
        {
            ExitPlanMode();
            return;
        }

        _selection.InputMode = MapInputMode.PlanRoute;
        _selection.DraftStops.Clear();
        _side.CurrentTab = 2;
        SyncMapSelection();
        RefreshPanel(Snap());
        _toasts.ShowToast("Plan route: click cities in order.");
    }

    private void ExitPlanMode()
    {
        _selection.InputMode = MapInputMode.Select;
        SyncMapSelection();
        RefreshPanel(Snap());
    }

    private void ClearCitySelection()
    {
        _selection.SelectedCityId = null;
        _map.SelectedCityId = null;
        _listsDirty = true;
        RefreshPanel(Snap());
        _map.QueueRedraw();
    }

    private void OpenNewGameDialog()
    {
        if (_newGameDialog is not null)
        {
            _newGameDialog.PopupCentered();
            return;
        }

        _newGameDialog = new Window
        {
            Title = "New Game",
            Size = new Vector2I(420, 280),
            Unresizable = true
        };
        _newGameDialog.CloseRequested += () => _newGameDialog.Hide();

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        _newGameDialog.AddChild(margin);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 10);
        margin.AddChild(col);

        col.AddChild(new Label { Text = "Choose a map pack" });
        var packOption = new OptionButton();
        var packs = _host.ListMapPacks();
        for (var i = 0; i < packs.Count; i++)
        {
            var pack = packs[i];
            var label = pack.IsTutorial
                ? $"{pack.Name} (tutorial) — {pack.CityCount} cities"
                : $"{pack.Name} — {pack.CityCount} cities";
            packOption.AddItem(label, i);
            packOption.SetItemMetadata(i, pack.Id);
        }

        col.AddChild(packOption);

        col.AddChild(new Label { Text = "Company name" });
        var nameEdit = new LineEdit { Text = "TravelEmpire Co." };
        col.AddChild(nameEdit);

        var start = new Button { Text = "Start" };
        start.AddThemeStyleboxOverride("normal", ThemeFactory.PrimaryButton());
        start.AddThemeColorOverride("font_color", Palette.BgDeep);
        start.Pressed += () =>
        {
            var idx = packOption.Selected;
            var packId = packOption.GetItemMetadata(idx).AsString();
            var company = string.IsNullOrWhiteSpace(nameEdit.Text) ? "TravelEmpire Co." : nameEdit.Text.Trim();
            _host.StartNewGame(packId, company);
            _selection.SelectedCityId = null;
            _selection.SelectedCatalogTypeId = null;
            _selection.SelectedVehicleId = null;
            _selection.SelectedRouteId = null;
            _selection.DraftStops.Clear();
            _selection.InputMode = MapInputMode.Select;
            _listsDirty = true;
            _map.FitToContent();
            RefreshAll();
            _toasts.ShowToast($"New game on {packId}.");
            _newGameDialog.Hide();
        };
        col.AddChild(start);

        AddChild(_newGameDialog);
        _newGameDialog.PopupCentered();
    }

    private void RefreshAll()
    {
        _frameSnap = _host.GetSnapshot();
        RefreshTop(_frameSnap);
        _map.UpdateFromSnapshot(_frameSnap);
        SyncMapSelection();
        RefreshPanel(_frameSnap);
    }

    private void RefreshTop(GameSnapshot snap) =>
        _topBar.UpdateFrom(snap.Company?.CashMinor, snap.SimHours, _host.SpeedMultiplier);

    private void RefreshPanel(GameSnapshot snap)
    {
        _listsDirty = false;
        _side.BindSelection(_selection);
        var vm = CityPanelViewModel.From(snap, _selection);
        _side.Render(vm, _selection, CurrentDraftValidation(snap));
    }

    private void RefreshPanelLive(GameSnapshot snap)
    {
        var vm = CityPanelViewModel.From(snap, _selection);
        _side.UpdateFleetLive(vm);
        RefreshTop(snap);
    }

    private string? CurrentDraftValidation(GameSnapshot snap)
    {
        if (_selection.InputMode != MapInputMode.PlanRoute || _selection.DraftStops.Count < 2)
            return null;
        var result = _host.Sim.ValidateCreateRoute(
            _selection.DraftMode,
            _selection.DraftStops.Select(id => new CityId(id)).ToList());
        return result.Success ? null : result.ErrorMessage;
    }

    private void SyncMapSelection()
    {
        _map.SelectedCityId = _selection.SelectedCityId;
        _map.InputMode = _selection.InputMode;
        _map.DraftStops = _selection.DraftStops.ToList();
        _map.DraftMode = _selection.DraftMode;
        _map.QueueRedraw();
    }

    private GameSnapshot Snap() => _frameSnap ??= _host.GetSnapshot();
}
