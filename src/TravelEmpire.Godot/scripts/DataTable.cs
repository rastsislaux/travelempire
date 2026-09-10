using Godot;

namespace TravelEmpire.Godot;

public sealed class DataTableColumn
{
    public required string Title { get; init; }
    public float ExpandRatio { get; init; } = 1f;
    public bool RightAlign { get; init; }
    public float MinWidth { get; init; } = 48f;
}

public partial class DataTable : VBoxContainer
{
    public event Action<string>? RowSelected;

    private readonly HBoxContainer _header = new();
    private readonly ScrollContainer _scroll = new();
    private readonly VBoxContainer _body = new();
    private readonly List<DataTableColumn> _columns = [];
    private readonly Dictionary<string, Button> _rows = new();
    private string? _selectedId;
    private string _emptyText = "Nothing here yet.";

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 0);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;

        _header.AddThemeConstantOverride("separation", 0);
        AddChild(_header);

        _scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        _scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        AddChild(_scroll);

        _body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _body.AddThemeConstantOverride("separation", 2);
        _scroll.AddChild(_body);
    }

    public void Configure(IReadOnlyList<DataTableColumn> columns, string emptyText = "Nothing here yet.")
    {
        _columns.Clear();
        _columns.AddRange(columns);
        _emptyText = emptyText;
        RebuildHeader();
    }

    public void SetSelectedId(string? id)
    {
        _selectedId = id;
        foreach (var (rowId, button) in _rows)
            StyleRow(button, rowId == _selectedId);
    }

    public void ClearRows()
    {
        foreach (var child in _body.GetChildren())
            child.QueueFree();
        _rows.Clear();
    }

    public void SetRows(IReadOnlyList<(string Id, string[] Cells)> rows)
    {
        ClearRows();
        if (rows.Count == 0)
        {
            var empty = new Label
            {
                Text = _emptyText,
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            empty.AddThemeColorOverride("font_color", Palette.TextDim);
            _body.AddChild(empty);
            return;
        }

        foreach (var (id, cells) in rows)
        {
            var button = new Button
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Alignment = HorizontalAlignment.Left,
                ToggleMode = true,
                ButtonPressed = id == _selectedId
            };
            button.AddThemeStyleboxOverride("normal", ThemeFactory.Flat(Palette.BgDeep, Palette.Border, 4, 1));
            button.AddThemeStyleboxOverride("hover", ThemeFactory.Flat(Palette.BgElevated, Palette.BrandAmber, 4, 1));
            button.AddThemeStyleboxOverride("pressed", ThemeFactory.Flat(Palette.BgElevated, Palette.BrandAmber, 4, 1));
            StyleRow(button, id == _selectedId);

            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);
            row.MouseFilter = MouseFilterEnum.Ignore;
            button.AddChild(row);

            for (var i = 0; i < _columns.Count; i++)
            {
                var col = _columns[i];
                var text = i < cells.Length ? cells[i] : "";
                var label = new Label
                {
                    Text = text,
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    SizeFlagsStretchRatio = col.ExpandRatio,
                    HorizontalAlignment = col.RightAlign ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                    ClipText = true,
                    CustomMinimumSize = new Vector2(col.MinWidth, 0),
                    MouseFilter = MouseFilterEnum.Ignore
                };
                label.AddThemeColorOverride("font_color", Palette.TextPrimary);
                label.AddThemeFontSizeOverride("font_size", 12);
                row.AddChild(label);
            }

            var captured = id;
            button.Pressed += () =>
            {
                _selectedId = captured;
                foreach (var (rowId, btn) in _rows)
                {
                    btn.ButtonPressed = rowId == _selectedId;
                    StyleRow(btn, rowId == _selectedId);
                }

                RowSelected?.Invoke(captured);
            };

            _body.AddChild(button);
            _rows[id] = button;
        }
    }

    private void RebuildHeader()
    {
        foreach (var child in _header.GetChildren())
            child.QueueFree();

        foreach (var col in _columns)
        {
            var label = new Label
            {
                Text = col.Title.ToUpperInvariant(),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsStretchRatio = col.ExpandRatio,
                HorizontalAlignment = col.RightAlign ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                CustomMinimumSize = new Vector2(col.MinWidth, 0),
                ClipText = true
            };
            label.AddThemeColorOverride("font_color", Palette.TextDim);
            label.AddThemeFontSizeOverride("font_size", 10);
            _header.AddChild(label);
        }
    }

    private static void StyleRow(Button button, bool selected)
    {
        button.AddThemeStyleboxOverride("normal", ThemeFactory.Flat(
            selected ? Palette.BgElevated : Palette.BgDeep,
            selected ? Palette.BrandAmber : Palette.Border,
            4, 1));
    }
}
