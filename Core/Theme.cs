using System.Globalization;

namespace HomeStream.Core;

public static class Theme
{
    public const string BgColorHex = "#0e0e12";
    public const string TextMutedHex = "#9a9aae";
    public const string TextDimHex = "#55556a";
    public const string AccentDarkHex = "#2a2540";

    public static readonly bool IsKo =
        CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ko";

    public static string S(string ko, string en) => IsKo ? ko : en;
}
