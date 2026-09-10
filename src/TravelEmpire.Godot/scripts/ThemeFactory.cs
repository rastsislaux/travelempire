using Godot;

namespace TravelEmpire.Godot;

public static class ThemeFactory
{
    public static Theme Create()
    {
        var theme = new Theme();

        theme.SetColor("font_color", "Label", Palette.TextPrimary);
        theme.SetColor("font_color", "Button", Palette.TextPrimary);
        theme.SetColor("font_hover_color", "Button", Palette.TextPrimary);
        theme.SetColor("font_pressed_color", "Button", Palette.BgDeep);
        theme.SetColor("font_disabled_color", "Button", Palette.TextDim);
        theme.SetColor("font_color", "LineEdit", Palette.TextPrimary);
        theme.SetColor("font_placeholder_color", "LineEdit", Palette.TextDim);
        theme.SetColor("font_color", "TabBar", Palette.TextMuted);
        theme.SetColor("font_selected_color", "TabBar", Palette.TextPrimary);

        theme.SetStylebox("panel", "PanelContainer", Flat(Palette.BgPanel, Palette.Border, 0, 1));
        theme.SetStylebox("panel", "TabContainer", Flat(Palette.BgCard, Palette.Border, 6, 1));
        theme.SetStylebox("normal", "Button", Flat(Palette.BgElevated, Palette.Border, 4, 1));
        theme.SetStylebox("hover", "Button", Flat(Palette.BgElevated.Lightened(0.08f), Palette.BrandAmber, 4, 1));
        theme.SetStylebox("pressed", "Button", Flat(Palette.BrandAmber, Palette.BrandAmber, 4, 0));
        theme.SetStylebox("disabled", "Button", Flat(Palette.BgCard, Palette.Border, 4, 1));
        theme.SetStylebox("normal", "LineEdit", Flat(Palette.BgDeep, Palette.Border, 4, 1));
        theme.SetStylebox("focus", "LineEdit", Flat(Palette.BgDeep, Palette.BrandAmber, 4, 1));
        theme.SetStylebox("tab_selected", "TabContainer", Flat(Palette.BgElevated, Palette.BrandAmber, 4, 1));
        theme.SetStylebox("tab_unselected", "TabContainer", Flat(Palette.BgCard, Palette.Border, 4, 1));
        theme.SetStylebox("tab_hovered", "TabContainer", Flat(Palette.BgElevated, Palette.Border, 4, 1));

        return theme;
    }

    public static StyleBoxFlat Flat(Color bg, Color border, float radius, int borderWidth)
    {
        var box = new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = border,
            BorderWidthLeft = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = (int)radius,
            CornerRadiusTopRight = (int)radius,
            CornerRadiusBottomRight = (int)radius,
            CornerRadiusBottomLeft = (int)radius,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
        return box;
    }

    public static StyleBoxFlat PrimaryButton() =>
        Flat(Palette.BrandAmber, Palette.BrandAmber, 4, 0);

    public static StyleBoxFlat Card() =>
        Flat(Palette.BgCard, Palette.Border, 8, 1);
}
