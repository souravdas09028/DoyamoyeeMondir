namespace DoyamoyeeMondir.Web.Models;

public static class BengaliDigits
{
    public static string? SerialKey(string? value)
    {
        var normalized = Normalize(value);
        if (normalized is null) return null;
        return string.Concat(normalized.Select(c => c is >= '০' and <= '৯'
            ? (char)('0' + c - '০') : c)).PadLeft(50, '0');
    }

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return string.Concat(value.Trim().Select(c => c is >= '0' and <= '9'
            ? (char)('০' + c - '0') : c));
    }
}
