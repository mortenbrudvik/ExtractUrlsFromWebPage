namespace ExtractUrls;

public static class LinkUtils
{
    // NOTE: Will insert spaces where line breaks are found to ensure that words are separated by spaces.
    public static string? TrimTitle(string? input) => 
        !string.IsNullOrWhiteSpace(input) ? input.Trim().Replace("\r", " ").Replace("\n", " ") : null;
}