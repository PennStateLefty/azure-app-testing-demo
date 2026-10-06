using System.Globalization;

namespace LifeCore.Web.Client.Theme;

public static class DisplayFormat
{
    private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

    public static string Money(decimal value) => value.ToString("C0", Us);
    public static string Number(double value, string format = "0.0") => value.ToString(format, Us);
    public static string Date(DateOnly value) => value.ToString("MMM d, yyyy", Us);
    public static string DateTime(DateTimeOffset value) => value.ToLocalTime().ToString("MMM d, h:mm tt", Us);
    public static string EnumLabel<T>(T value) where T : struct, Enum => string.Concat(value.ToString().Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
}
