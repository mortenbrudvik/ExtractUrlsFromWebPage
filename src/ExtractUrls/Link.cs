using LanguageExt;

namespace ExtractUrls;

public record Link(string Href, Option<string> Title, string Description = "");