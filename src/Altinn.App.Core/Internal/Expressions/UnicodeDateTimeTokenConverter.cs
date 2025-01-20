using System.Globalization;
using System.Text;

namespace Altinn.App.Core.Internal.Expressions;

/// <summary>
/// A class used for converting LDML/unicode date formats to .NET date formats and format a date accordingly.
/// <see href="https://www.unicode.org/reports/tr35/tr35-dates.html#dfst-era"/>
/// </summary>
internal static class UnicodeDateTimeTokenConverter
{
    /// <summary>
    /// A mapping table from LDML date format tokens to .NET date format tokens
    /// </summary>
    private static readonly Dictionary<string, string> _tokenTable = new()
    {
        // Era
        { "G", "gg" },
        { "GG", "gg" },
        { "GGG", "gg" },
        { "GGGG", "gg" },
        { "GGGGG", "gg" },
        // Year
        { "y", "yyyy" },
        { "yy", "yyyy" },
        { "yyy", "yyyy" },
        { "yyyy", "yyyy" },
        // Extended year (we just map it to the same as year)
        { "u", "yyyy" },
        { "uu", "yyyy" },
        { "uuu", "yyy" },
        { "uuuu", "yyyy" },
        // Month
        { "M", "MM" },
        { "MM", "MM" },
        { "MMM", "MMM" },
        { "MMMM", "MMMM" },
        // Day of month
        { "d", "dd" },
        { "dd", "dd" },
        // Day of week (names, not numbers)
        { "E", "ddd" },
        { "EE", "ddd" },
        { "EEE", "ddd" },
        { "EEEE", "dddd" },
        { "EEEEE", "ddd" }, // This one probably needs special treatment
        // AM/PM
        { "a", "tt" },
        // Hour
        { "h", "hh" },
        { "hh", "hh" },
        { "H", "HH" },
        { "HH", "HH" },
        // Minute
        { "m", "mm" },
        { "mm", "mm" },
        // Second
        { "s", "ss" },
        { "ss", "ss" },
        // Fractional second
        { "S", "ff" },
        { "SS", "ff" },
        { "SSS", "fff" },
    };

    public static string? Format(DateTime? when, string? ldmlFormat, string language)
    {
        if (when is null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(ldmlFormat))
        {
            ldmlFormat = language switch
            {
                "nb" => "dd.MM.yyyy",
                "nn" => "dd.MM.yyyy",
                _ => "M/d/yy",
            };
        }

        CultureInfo culture = new CultureInfo(language);
        StringBuilder sb = new StringBuilder();
        int i = 0;
        while (i < ldmlFormat.Length)
        {
            // Seek forwards to find the longest chain of consecutive identical characters
            int j = i;
            while (j < ldmlFormat.Length && ldmlFormat[j] == ldmlFormat[i])
            {
                j++;
            }

            string token = ldmlFormat.Substring(i, j - i);
            if (_tokenTable.TryGetValue(token, out string? dotNetToken))
            {
                var converted = when.Value.ToString(dotNetToken, culture);

                if (token == "EEEEE")
                {
                    // This does not exist in .NET, but it's just the first letter of the day name.
                    converted = converted.Substring(0, 1).ToUpper(culture);
                }
                else if (dotNetToken == "ddd")
                {
                    // The LDML format does not produce trailing periods for day names, but .NET does.
                    converted = converted.TrimEnd('.');
                }
                else if (token == "yy")
                {
                    // Remove the century from the year
                    converted = converted.Substring(converted.Length - 2);
                }
                else if (token == "S")
                {
                    // Only show one digit of fractional seconds
                    converted = converted.Substring(0, 1);
                }
                else if (token.Length == 1 && dotNetToken.Length > 1)
                {
                    // If the token is single-length, in the LDML format that means it should not have leading
                    // zeroes, but in .NET it means a standard format. Let's trim the leading zeroes here.
                    converted = converted.TrimStart('0');
                }
                else if (token == "GGGG")
                {
                    // .NET does not have a way to format the era in the same way as LDML, so we'll hard-code our
                    // supported languages here.
                    switch (language)
                    {
                        case "nb":
                        case "nn":
                            converted = when.Value.Year > 0 ? "etter Kristus" : "før Kristus";
                            break;
                        default:
                            converted = when.Value.Year > 0 ? "Anno Domini" : "Before Christ";
                            break;
                    }
                }
                else if (token == "GGGGG" && language == "en")
                {
                    // At this point, even the JS library we use gives up. Only the english era names support a narrow
                    // format, so we'll just hard-code those.
                    converted = when.Value.Year > 0 ? "A" : "B";
                }

                sb.Append(converted);
            }
            else
            {
                sb.Append(token);
            }

            i = j;
        }

        return sb.ToString();
    }
}
