using System.Collections.Frozen;
using System.Globalization;

namespace PartnerIntegration.Application.Validation;

public static class Iso4217Currencies
{
    public static FrozenSet<string> Codes { get; } = CultureInfo
        .GetCultures(CultureTypes.SpecificCultures)
        .Select(culture =>
        {
            try
            {
                return new RegionInfo(culture.Name).ISOCurrencySymbol;
            }
            catch (ArgumentException)
            {
                return null;
            }
        })
        .Where(code => !string.IsNullOrWhiteSpace(code))
        .Cast<string>()
        .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static bool IsValid(string? currency) =>
        !string.IsNullOrWhiteSpace(currency) && Codes.Contains(currency);
}
