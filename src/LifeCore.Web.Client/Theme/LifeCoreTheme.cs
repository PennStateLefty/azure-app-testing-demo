using MudBlazor;

namespace LifeCore.Web.Client.Theme;

public static class LifeCoreTheme
{
    public const string Purple = "#A100FF";
    public const string DarkPurple = "#7500C0";
    public const string DeepPurple = "#460073";
    public const string Black = "#000000";
    public const string White = "#FFFFFF";
    public const string Neutral = "#F2F2F2";
    public const string Border = "#E6E6E6";
    public const string TextSecondary = "#5A5A5A";

    private static readonly string[] FontStack = ["Graphik", "Inter", "Arial", "sans-serif"];

    public static MudTheme Theme { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = Purple,
            PrimaryContrastText = White,
            Secondary = DarkPurple,
            SecondaryContrastText = White,
            Tertiary = DeepPurple,
            TertiaryContrastText = White,
            Black = Black,
            White = White,
            TextPrimary = Black,
            TextSecondary = TextSecondary,
            Background = Neutral,
            BackgroundGray = Neutral,
            Surface = White,
            DrawerBackground = White,
            DrawerText = Black,
            DrawerIcon = TextSecondary,
            AppbarBackground = Black,
            AppbarText = White,
            LinesDefault = Border,
            LinesInputs = Border,
            TableLines = Border,
            TableHover = "#F7EEFF",
            Divider = Border,
            Success = "#107C10",
            Warning = "#F7630C",
            Error = "#D13438",
            Info = Purple,
            ActionDefault = Black,
            ActionDisabled = "#8A8A8A",
            ActionDisabledBackground = "#EDEDED",
            HoverOpacity = 0.08,
            BorderOpacity = 1
        },
        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = FontStack, FontSize = "0.95rem", LineHeight = "1.45" },
            H1 = new H1Typography { FontFamily = FontStack, FontWeight = "700", FontSize = "2.4rem", LineHeight = "1.1" },
            H2 = new H2Typography { FontFamily = FontStack, FontWeight = "700", FontSize = "1.9rem", LineHeight = "1.15" },
            H3 = new H3Typography { FontFamily = FontStack, FontWeight = "700", FontSize = "1.45rem", LineHeight = "1.2" },
            H4 = new H4Typography { FontFamily = FontStack, FontWeight = "700", FontSize = "1.2rem", LineHeight = "1.25" },
            H5 = new H5Typography { FontFamily = FontStack, FontWeight = "700", FontSize = "1.05rem", LineHeight = "1.25" },
            H6 = new H6Typography { FontFamily = FontStack, FontWeight = "700", FontSize = "0.95rem", LineHeight = "1.3" },
            Button = new ButtonTypography { FontFamily = FontStack, FontWeight = "700", TextTransform = "none" },
            Body1 = new Body1Typography { FontFamily = FontStack },
            Body2 = new Body2Typography { FontFamily = FontStack },
            Caption = new CaptionTypography { FontFamily = FontStack, FontSize = "0.78rem" }
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "3px",
            DrawerWidthLeft = "252px",
            DrawerWidthRight = "420px",
            AppbarHeight = "64px"
        }
    };
}
