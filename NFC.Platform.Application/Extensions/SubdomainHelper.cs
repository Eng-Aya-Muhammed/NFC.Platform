namespace NFC.Platform.Application.Extensions;

public static class SubdomainHelper
{
    public static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "user";

        var normalized = input.Trim().ToLowerInvariant().Replace(" ", "-");
        var cleaned = new string(normalized.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

        while (cleaned.Contains("--"))
        {
            cleaned = cleaned.Replace("--", "-");
        }

        var result = cleaned.Trim('-');
        return string.IsNullOrWhiteSpace(result) ? "user" : result;
    }
}
